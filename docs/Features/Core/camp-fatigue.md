# Camp Fatigue System

**Summary:** Fatigue is a stamina-like resource that gates certain enlisted actions and choices. Players accumulate fatigue from demanding activities and restore it through rest, with a default capacity of 24 points. Fatigue is displayed in the UI and affects availability of camp menu decisions, pay muster options, and event choices.

**Status:** ✅ Current  
**Last Updated:** 2025-12-22  
**Related Docs:** [Core Gameplay](core-gameplay.md), [Camp Life Simulation](../Campaign/camp-life-simulation.md)

---

## Index

- [Overview](#overview)
- [Data & State](#data--state)
- [Fatigue Sources](#fatigue-sources)
- [Restoration](#restoration)
- [Probation Effects](#probation-effects)
- [Integration](#integration)

---

## Overview
Fatigue is a stamina-like counter used to gate certain enlisted actions and choices. It displays in the enlisted UI so players can see how much capacity remains.

Fatigue is currently used by:
- Pay Muster options (e.g. Corruption Challenge, Side Deal)
- Probation (caps max fatigue while probation is active)
- Camp Activities (data-driven menu actions can cost or restore fatigue)
- Strategic Operations (high-intensity contexts like "Grand Campaign" or "Siege Works" may increase fatigue costs for certain actions)
- Lance Life Events (event options can cost or restore fatigue; menu availability considers fatigue)

## Data & State
- Stored on `EnlistmentBehavior`: `FatigueCurrent`, `FatigueMax` (default 24/24).
- Serialized via `SyncData`; validated on load (clamped to >0, current <= max).
- Probation may temporarily reduce `FatigueMax` (fatigue cap), and the max is restored when probation clears.

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
- Defaults safe if config missing; baseline is 24/24 unless modified by probation.

## Recovery
- Fatigue is restored by rest and/or specific systems (implementation-owned by enlistment behavior). 
- **Strategic Context Impact**: Recovery is faster during restful contexts like "Winter Camp" or "Garrison Duty" and slower during high-tempo operations like "Last Stand" or "Harrying the Lands."
- If fatigue is capped by probation, recovery cannot exceed the cap until probation ends.
