# ADR-014: Enlisted Menu Guards During Map Events and Settlements

Status: Accepted
Date: 2025-08-31
Deciders: Enlisted Team

## Context

The enlisted status/report menus should not interfere with encounter UIs or settlement flows. Early designs risked nested menus and stuck time controls if encounters were active when switching.

## Decision

- Before opening enlisted menus: finish any active `PlayerEncounter` and drain `GameMenu.ExitToLast()` loops to a clean state; set `CampaignTimeControlMode = StoppablePlay`, then open the menu and call `StartWait()` in `OnInit`.
- If an encounter begins while our enlisted menu is open: remember that it was open, close it to allow the encounter UI, and optionally re-open after post-battle restore.
- Settlement guard: while the commander or player is inside town/castle, do not attempt to join/reroute encounters; skip until outside. Do not auto-open custom menus on entry (Freelancer parity) unless explicitly enabled by settings.

## Consequences

Positive:
- Prevents nested menu/encounter conflicts; preserves responsive time controls.
- Preserves native settlement UX by default.

Negative:
- Adds bookkeeping to remember whether our menu was open.

## Compliance

- Blueprint Sections: 4.5 (Menu & Time Control), 4.6 (Army & Encounters).
- Code: `EnlistmentBehavior` menu open/close, `OnMapEventStarted/Ended` guards.

## References

- ADR-012 (Menu & Time-Control behavior)
- Bannerlord API 1.2.12 `GameMenu`, `PlayerEncounter`


