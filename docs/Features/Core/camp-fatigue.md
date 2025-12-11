# Camp Fatigue â€” Documentation

## Overview
Fatigue is a simple counter used by camp actions. It displays on the main enlisted menu so players can see how much capacity remains. Current baseline is 24/24; consumption logic is not yet wired into camp actions.

## Data & State
- Stored on `EnlistmentBehavior`: `FatigueCurrent`, `FatigueMax` (default 24/24).
- Serialized via SyncData; reset to full on enlist/discharge; validated on load (clamped to >0, current <= max).

## APIs (for camp actions)
- `EnlistmentBehavior.TryConsumeFatigue(int amount, string reason = null)`: clamps 0..max, returns true if applied.
- `EnlistmentBehavior.RestoreFatigue(int amount = 0, string reason = null)`: restores by amount or to full when amount <= 0.
- Read-only surfaces: `FatigueCurrent`, `FatigueMax` (also shown in UI).

## UI
- `enlisted_status` shows `Fatigue : {Current}/{Max}` to keep players informed.

## Diagnostics
- Session logs on consume/restore with new values (single-line, non-spam).

## Safety & Compatibility
- No native menu/time patches; contained in EnlistedBehavior and UI.
- Defaults safe if config missing; currently hardcoded 24/24.

## Missing / To-Do
- Wire actual camp actions to call `TryConsumeFatigue` and `RestoreFatigue` with tuned amounts.
- Decide recovery rules (daily? rest events?), caps, and any culture/style modifiers if desired later.
- Make fatigue max configurable if needed.
