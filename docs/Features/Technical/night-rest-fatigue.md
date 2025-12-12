# Night Rest & Fatigue (Design Draft — Not Implemented)

## Overview
Make lord-led parties respect nightfall by pausing long marches in safe conditions and, when they cannot safely rest, apply a light temporary fatigue penalty to enlisted players. No global time control changes.

## Purpose
- Immersion: armies pause at night rather than marching endlessly.
- Pacing: reduces overnight whiplash travel.
- Gameplay trade-off: forced night marches mildly tax enlisted soldiers when rest is impossible.

## Inputs/Outputs
- Inputs: `CampaignTime.Now.GetHourOfDay()`, party battle/siege/raid state, enlistment state, army membership, sea/raid status.
- Outputs: Per-party “night rest” hold state; per-lord no-rest counter; optional fatigue flag on enlisted hero; logs.

## Behavior
1) Night window: default 22:00–06:00 (configurable).
2) Tick cadence: Hourly (`CampaignEvents.HourlyTickEvent`) to stay cheap.
3) Eligibility:
   - Kingdom lords and army leaders only (skip caravans/villagers/bandits).
   - Skip entirely if party is in/starting: `MapEvent`, `SiegeEvent`, raid/hostile action, naval travel/assault, or any army member is in `MapEvent`.
4) Rest action (safe night):
   - Issue a soft hold: `MobileParty.Ai.SetMoveModeHold()`.
   - Do **not** change `IsActive/IsVisible` or time control.
   - Set `IsNightResting = true`; reset `ConsecutiveNoRestHours`.
5) Release (dawn or danger):
   - Clear `IsNightResting`; allow native AI to re-plan (no custom pathing).
6) Forced march tracking:
   - During night, if unsafe -> increment `ConsecutiveNoRestHours` (per lord/army leader).
7) Enlisted fatigue:
   - If player is enlisted under that lord and `ConsecutiveNoRestHours >= 4`, apply a temporary fatigue flag (e.g., lasts 8 in-game hours).
   - Fatigue effect (light): reduce passive HP regen and/or small skill penalty (e.g., Athletics/Riding -1 while active). Clears after a successful night rest or after duration expires.

## Edge Cases
- Sieges/assaults/raids/naval: never rest; tracking continues.
- Army battles spanning multiple parties: require no `MapEvent` on leader **and** none on army parties before resting.
- Reserve menus: do not touch reserve flow; only movement holds.
- Player captivity/leave: skip fatigue application when player is prisoner or on leave.
- Config toggles: global enable, fatigue enable, night window, required consecutive no-rest hours.

## Acceptance Criteria
- At 22:00–06:00, a marching lord/army leader with no battles/sieges/raids/naval state pauses; resumes at ≥06:00 or when danger appears.
- Fatigue applies to enlisted player only after missed rest (e.g., ≥4 night hours forced march), and clears after a rest.
- No changes to campaign time control; no crashes entering/exiting battles, sieges, or reserve menus.
- Hourly tick remains performant; no visible hitching in campaign.

## References (vanilla API, 1.3.4)
- `CampaignTime.Now.GetHourOfDay()`
- `MobileParty.Ai.SetMoveModeHold()`
- `MobileParty.Party.MapEvent`, `Party.SiegeEvent`
- `CampaignEvents.HourlyTickEvent`
- Enlisted enlistment state: `EnlistmentBehavior.Instance`

## Phased Execution Plan

Phase 0 – Discovery & Config (safe)
- Add config keys (night window, enable flags, fatigue enable, no-rest threshold, fatigue duration) to `enlisted_config.json` with defaults but leave feature disabled.
- Log-only prototype: hourly tick logs eligibility and would-be rest decisions (no AI mutation).

Phase 1 – Core Night Hold (guarded)
- Implement `NightRestBehavior` (new behavior) with hourly tick:
  - Filter to kingdom lords/army leaders.
  - Skip when in MapEvent/SiegeEvent/raid/naval/army member in battle.
  - During night window: issue `SetMoveModeHold()`, set `IsNightResting`.
  - Dawn/danger: clear flag; no pathing override.
- Telemetry: log state transitions (enter/exit rest), counts, and skips; throttle logs.
- Default: feature disabled behind config until validated.

Phase 2 – Safety Hardening
- Add conservative fallbacks: if detection is uncertain, skip resting.
- Add watchdog to clear `IsNightResting` if party enters a MapEvent/SiegeEvent unexpectedly.
- Ensure no interaction with reserve menus or enlisted activation flags.

Phase 3 – Enlisted Fatigue (optional but requested)
- In `EnlistmentBehavior` hourly tick: read no-rest counter from `NightRestBehavior` for current lord.
- Apply fatigue when threshold reached; clear on rest or duration expiry.
- Effects kept light and temporary; logged when applied/cleared.
- Config-toggle fatigue; default off until validated.

Phase 4 – Polish & UX
- Config exposure (if desired) via settings.
- Minimal UI hint (optional): status line in enlisted menu when fatigued.
- Final logging level tuning.

## Player fatigue (current system)
Enlisted already has an implemented fatigue counter used by pay muster choices and probation. See:
- [Camp Fatigue](../Core/camp-fatigue.md)

