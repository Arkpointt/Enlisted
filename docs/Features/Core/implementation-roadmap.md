# Master Plan: Implementation Roadmap (Camp Life + Lance Life)

This document is the **master implementation plan** for the “camp life” and “lance life” direction of Enlisted. It is intended to keep scope controlled and make sure new systems remain **mod-friendly** and **internal** to Enlisted.

## Index
- [Overview](#overview)
- [Purpose](#purpose)
- [Guiding principles (non-negotiables)](#guiding-principles-non-negotiables)
- [Source of truth policy](#source-of-truth-policy)
- [Major systems in scope](#major-systems-in-scope)
- [Configuration / feature flags](#configuration--feature-flags)
- [Phased rollout plan](#phased-rollout-plan)
  - [Phase 0 — Documentation & hard boundaries](#phase-0--documentation--hard-boundaries)
  - [Phase 1 — Data & triggers foundation](#phase-1--data--triggers-foundation)
  - [Phase 2 — Lance Life (text events) production pass](#phase-2--lance-life-text-events-production-pass)
  - [Phase 3 — Camp Life snapshot + integrations](#phase-3--camp-life-snapshot--integrations)
  - [Phase 4 — Escalation tracks (honor/heat/discipline) + consequences](#phase-4--escalation-tracks-honorheatdiscipline--consequences)
  - [Phase 5 — Deeper systems (optional, gated)](#phase-5--deeper-systems-optional-gated)
  - [Phase 6 — Polish, tuning, and stability](#phase-6--polish-tuning-and-stability)
- [Risks and how we avoid them](#risks-and-how-we-avoid-them)
- [Acceptance checklist (project-level)](#acceptance-checklist-project-level)
- [References](#references)

## Overview
We are building a **campaign-layer simulation** of enlisted camp life and lance identity using:
- **Text-based events** (“Viking Conquest style” popups) as the core narrative delivery mechanism.
- **Event-driven campaign signals** (battles, sieges, settlements, raids, pursuit) as triggers.
- **Internal meters** (camp conditions + corruption/discipline) to make consequences feel coherent.

We explicitly avoid building new scenes, new quest systems, or rewriting the global economy.

## Purpose
- Make enlisted service feel like a lived-in army: shortages, drills, morale, corruption, discipline.
- Make the player’s lance feel like a persistent identity with stories and progression.
- Keep everything **easy to extend or remove**: content-first, modular systems with feature flags.

## Guiding principles (non-negotiables)
- **Internal-only**: systems modify Enlisted-owned menus/popup flows and Enlisted state; avoid patching or replacing vanilla economy/AI.
- **Event-driven**: campaign events + daily/hourly ticks; avoid heavy scanning loops.
- **Stable UX**: popups only when safe (no battle/encounter/captivity) and rate-limited (cooldowns + weekly caps).
- **Content is data**: new events should be addable via JSON/story packs without code changes where possible.
- **No surprise item loss**: when we change gear/rosters, we stash/restore, we do not delete.

## Source of truth policy
We now have:
- **Feature Specs** under `docs/Features/**` (these should reflect intended implementation).
- **Research drafts** under `docs/research/**` (these may be aspirational, incomplete, or out of date).

Policy:
- **Code + Feature Specs** are the source of truth for behavior.
- **Research docs** are idea banks. We can adopt them, but only after translating them into Feature Spec acceptance criteria and then implementing safely.

## Major systems in scope
- **Lance assignments** (existing): identity + `storyId` surfaces.
  - Doc: `docs/Features/Core/lance-assignments.md`
- **Lance Life** (implemented baseline): text events gated by tier/final-lance with cooldowns.
  - Doc: `docs/Features/Gameplay/lance-life.md`
- **Camp Life Simulation** (spec): condition meters and integrations (Quartermaster mood/stockouts, delayed pay/IOUs, incidents).
  - Doc: `docs/Features/Gameplay/camp-life-simulation.md`
- **Quartermaster** (existing): trade menus and localized flavor.
  - Doc: `docs/Features/UI/quartermaster.md`
- **Pay system** (existing): muster ledger + pay muster prompt.
  - Doc: `docs/Features/Core/pay-system-rework.md`
- **Menu Interface** (existing): menus plus “popups and incidents” map.
  - Doc: `docs/Features/UI/menu-interface.md`

## Configuration / feature flags
All major systems must be gated by config to keep the rollout safe:
- `lances.lances_enabled` (existing)
- `lance_life.enabled` (added)
- Future: `camp_life.enabled`, `camp_life.debug_logging`, etc.

Rule: we prefer feature flags over half-implemented systems that silently affect the game.

## Phased rollout plan

### Phase 0 — Documentation & hard boundaries
Deliverables:
- Master plan (this doc) kept current.
- Each major subsystem has a Feature Spec (done for Camp Life + Lance Life).
- Research index is up to date and clearly labeled as “drafts”.

Acceptance:
- A new contributor can read `docs/Features/index.md` and understand what’s implemented vs planned.

### Phase 1 — Data & triggers foundation
Goal: build reliable, mod-friendly signals that higher-level stories can reference.

Deliverables:
- A single, shared “trigger vocabulary” used across systems:
  - Time-of-day (day/night/dawn windows)
  - Lord/army state (siege started/ongoing/ended, settlement entry/exit, pursuit/chase, recent battle)
  - Safety gating (no battle/encounter/prisoner)
- Minimal persistence for “recent history” (timestamps/counters) so triggers can be “after X” without scanning.
- Content foundation (required for long-term expansion):
  - Story packs are loaded from `ModuleData/Enlisted/StoryPacks/LanceLife/*.json`
  - Stable schema with `schemaVersion` + namespaced event IDs
  - Startup validation + diagnostics logging (duplicate IDs, malformed entries); invalid entries skipped safely
  - Contract doc (source of truth for pack format): **[Content Pack Contract — Lance Life Story Packs](story-pack-contract.md)**

Acceptance:
- We can confidently answer “why did this event trigger?” in logs without spamming.
- Content loading is predictable: packs load deterministically, and bad data does not crash campaigns.

### Phase 2 — Lance Life (text events) production pass
Goal: establish the story loop that builds skills and identity (low complexity, high value).

Deliverables:
- Expand story packs (multiple files, organized by theme) with a solid baseline set (content authoring).
- Add placeholder support in story text (player name, enemy faction, lord name) *only where safe/available*.
- Require story categorization (training/logistics/morale/corruption/discipline/medical) for balancing and enable/disable controls.

Acceptance:
- A player sees 1–2 meaningful Lance Life events per in-game week (configurable).
- Events feel tier-appropriate (T2–T5 spread) and always include a “safe” option.

### Phase 3 — Camp Life snapshot + integrations
Goal: make events feel connected to the campaign: shortages, raids, pay tension.

Deliverables:
- Implement CampConditionsSnapshot (daily updated) with a small set of meters:
  - LogisticsStrain, MoraleShock, TerritoryPressure, PayTension, ContrabandHeat
- Integrations:
  - Quartermaster mood/prices/stockouts driven by snapshot (small multipliers first).
  - Pay Muster text/options respond to PayTension (IOU/promissory notes path planned here).

Acceptance:
- After villages are looted / long time away from towns / heavy battle cadence, the mood/shortages feel consistent.
- No global economy systems are modified.

### Phase 4 — Escalation tracks (honor/heat/discipline) + consequences
Goal: make choices matter over time without becoming punitive or save-breaking.

Phase 4 acceptance gate:
- **[Phase 4 Checklist — Corruption / Heat / Discipline](corruption-phase-checklist.md)**

Deliverables:
- Internal escalation tracks:
  - Honor/Dishonor (or “Lance Reputation”)
  - Heat (contraband/corruption attention)
  - Discipline risk
- Consequences that remain internal (camp-scale, readable, and reversible):
  - Quartermaster consequences:
    - temporary price/buyback changes
    - “clerk approaches with bribe” / “ledger skim” style offers (optional, tempting)
    - shakedown checks (when Heat is high)
    - player-initiated “unguarded goods” opportunities (menu-based theft/temptation hooks)
    - “snitch / audit” consequence events that can follow repeated heat gain
  - Discipline consequences:
    - extra duty / fatigue penalties
    - “cover for a lance mate” dilemmas (discipline vs looking the other way)
    - formal discipline hearing events at high discipline risk (no instant hard fail)
    - promotion blocked temporarily at very high discipline (cooldown/clearing path required)
    - duty removal / reassignment as a high-discipline consequence (temporary; recovery path required)
  - Contraband/heat consequences:
    - heat thresholds with clear escalation:
      - Low: watched closely / warning text
      - Medium: periodic shakedown events (search, pressure, minor loss)
      - High: confiscation events and/or targeted audit events (still survivable)
    - heat can be gained from “small” social/camp choices (e.g., contraband drink, late-night sneak-out run) so the system has more than one on-ramp
  - Loot/aftermath discipline:
    - post-assault / post-battle “loot discipline” events tied to army operations (camp-scale moral choices)
  - Pay/IOU consequences:
    - debt/IOU flavor can be treated as PayTension inputs, but must remain internal to Enlisted’s ledger (no global economy changes)
    - “pay your debts” / gambling IOU beats can exist as camp-scale social pressure that interacts with PayTension and Discipline (no hard fail)
  - Lance social mitigation:
    - lance relationships can (sometimes) warn you about shakedowns or cover for you (reducing heat/discipline impact in limited, readable ways)

Acceptance:
- Repeated corrupt choices increase risk and change future options (audits/shakedowns/confiscation), but do not soft-lock progress.
- Heat/discipline escalation is:
  - predictable (players can recognize why consequences happened),
  - rate-limited (no spam),
  - and has relief valves (clean choices reduce risk over time).
- “Discipline vs looking the other way” dilemmas exist at multiple tiers and are not purely punitive.

### Phase 5 — Deeper systems (optional, gated)
Goal: adopt selected parts of the larger research drafts without taking on the whole world simulation.

Candidates (only if earlier phases are stable):
- Named lance role personas and replacement logic (text-only roster, not real troop individuation).
- Player condition system (injury/illness) as event consequences.
- Wanderers/camp followers as menu-based encounters.

Acceptance:
- Every added subsystem has:
  - a feature flag
  - a rollback plan (disable in config)
  - safe persistence behavior

### Phase 6 — Polish, tuning, and stability
Deliverables:
- Balance passes on XP/fatigue/coin so “story actions” are worthwhile but not dominant.
- Reduce spam: stronger rate limits, better trigger thresholds.
- Clean localization strategy for reusable lines (optional).

Acceptance:
- Players can run long campaigns without menu spam, save bloat, or instability.

## Risks and how we avoid them
- **Scope creep**: keep research drafts as “ideas” until translated into phase deliverables.
- **Popup spam**: strict rate limiting + safety gating + predictable daypart windows.
- **Save bloat**: store only small counters/timestamps, not large histories.
- **Breaking vanilla systems**: keep consequences internal (Enlisted-only), avoid economy/AI rewrites.

## Acceptance checklist (project-level)
- [ ] Everything is gated by config and can be disabled safely.
- [ ] No event fires in battle/encounter/prisoner states.
- [ ] Every event has a “safe” option.
- [ ] Story outcomes remain camp-scale (fatigue/XP/coin/internal meters).
- [ ] Debug/log output is helpful but not spammy.

## References
- Feature specs:
  - `docs/Features/Gameplay/lance-life.md`
  - `docs/Features/Gameplay/camp-life-simulation.md`
- Research index:
  - `docs/research/index.md`

