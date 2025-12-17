# Feature Spec: Lance Life (Stories, Camp Events, and Progression)

This spec builds “life around the lance”: small stories, camp events, and morally gray choices that grow the player’s skills while enlisted. It is designed to be **mod-friendly** and **internal** to Enlisted by using event-driven state + Enlisted-owned incidents/menus (not rewriting vanilla economy/AI).

## Status (shipping)
- **Two Lance Life content layers ship today (both data-driven):**
  - **Lance Life Stories (StoryPacks)**: `ModuleData/Enlisted/StoryPacks/LanceLife/*.json` (daily-tick story popups).
  - **Lance Life Events (Events Catalog)**: `ModuleData/Enlisted/Events/*.json` (109+ event definitions with delivery metadata).
- Phase 4 escalation is implemented (tracks + status UI + threshold consequences).
- Phase 5 is implemented:
  - Named lance role personas (text-only) are implemented and exposed as placeholders.
  - Player conditions (injury/illness + treatment + training gating) are implemented and can be applied via event/story options.
  - Exhaustion is persisted and can gate training when enabled.
  - Wanderers/camp followers are intentionally not implemented yet.
- Phase 7 is implemented:
  - **Proving events** for T1->T6 promotions with narrative choices
  - **Formation choice** during T1->T2 event
  - **Starter duty** auto-assignment based on formation
  - **Duty request system** with cooldowns and lance leader approval
  - Events file: `ModuleData/Enlisted/Events/events_promotion.json`
- Camp Activities (menu actions) exist as a separate, data-driven system for action-based XP/fatigue:
  - Data: `ModuleData/Enlisted/Activities/activities.json`

## Index
- [Overview](#overview)
- [Purpose](#purpose)
- [Design goals](#design-goals)
- [Mod-friendly constraints (internal to Enlisted)](#mod-friendly-constraints-internal-to-enlisted)
- [Inputs / Outputs](#inputs--outputs)
- [Core concepts](#core-concepts)
  - [Lance identity as a story anchor](#lance-identity-as-a-story-anchor)
  - [Story packs (data-driven content)](#story-packs-data-driven-content)
  - [Triggers and cooldowns](#triggers-and-cooldowns)
  - [Escalation: heat, discipline, reputation, medical risk](#escalation-heat-discipline-reputation-medical-risk)
  - [Player conditions: injury, illness, exhaustion](#player-conditions-injury-illness-exhaustion)
  - [Named lance personas (placeholders)](#named-lance-personas-placeholders)
- [Story types (what we can ship)](#story-types-what-we-can-ship)
  - [1) Skills & drills](#1-skills--drills)
  - [2) Logistics & scrounging](#2-logistics--scrounging)
  - [3) Corruption and theft (quartermaster tent)](#3-corruption-and-theft-quartermaster-tent)
  - [4) Morale and revelry (smuggling drink)](#4-morale-and-revelry-smuggling-drink)
  - [5) Rivalries and discipline](#5-rivalries-and-discipline)
- [Phased plan](#phased-plan)
- [Edge Cases](#edge-cases)
- [Acceptance Criteria](#acceptance-criteria)
- [Technical implementation (high-level)](#technical-implementation-high-level)

## Overview
“Lance Life” is a story layer that uses the player’s **lance identity** (from the existing lance system) and **camp conditions** (from Camp Life Simulation) to trigger small events with choices. Outcomes grant:
- Skill XP (Roguery, Charm, Leadership, Stewardship, Medicine, Athletics, Riding, Polearm, etc.)
- Fatigue changes (costs for extra work, or relief from revelry)
- Pay/ledger changes (IOUs, bribes, lost coin)
- Quartermaster mood/availability shifts (consequences for theft and corruption)

## Purpose
- Make the lance feel like a real sub-unit with routines, temptations, and consequences.
- Create a path to grow skills during enlistment that feels grounded (drills, errands, scrounging).
- Add “camp crime” stories (theft/contraband) in a way that is readable, optional, and doesn’t break the campaign.

## Design goals
- **Modular content**: stories are data-defined; add/remove without code edits.
- **Rare but memorable**: low frequency, strong flavor, clear causes (“after that raid…”, “we haven’t seen town in days…”).
- **Meaningful choices**: at least one safe option and one risky option; avoid forced punishment.
- **Internal integration**: outcomes only touch Enlisted-owned systems (Quartermaster menus, muster ledger, fatigue, incidents).

## Mod-friendly constraints (internal to Enlisted)
- Prefer **CampaignEvents** + daily tick aggregation.
- Prefer Enlisted-owned UI patterns:
  - Incidents/inquiry prompts (like bag check / pay muster prompts)
  - Camp menu entries (“Camp”)
  - Quartermaster menus
- Avoid changing global town markets, vanilla crime systems, or AI behaviors.
- No Harmony patches required for the story system itself; keep it contained in Enlisted behaviors.

## Inputs / Outputs

### Inputs
- **Lance identity** (existing):
  - `lanceId`, `styleId`, `storyId`, role hint, provisional/final state
  - Event hook: `EnlistmentBehavior.OnLanceFinalized(lanceId, styleId, storyId)`
- **Camp Life snapshot** (from `Camp Life Simulation`):
  - `LogisticsStrain`, `MoraleShock`, `PayTension`, `ContrabandHeat`, etc.
- **Campaign state**:
  - Recent battles, sieges, settlement changes, village raids/loot
  - Days since last settlement entry
- **Player state**:
  - Tier, formation, relevant skills (Roguery/Charm/etc.)
  - Fatigue and any existing probation/discharge flags
  - (Phase 4) Escalation tracks: Heat, Discipline, Lance Reputation, Medical Risk (when enabled)
  - (Phase 5) Player conditions: current injury/illness/exhaustion (when enabled)

## Quick start (text-based, low complexity)
This feature is intentionally “Viking Conquest”-style: short text scenes with a few options.

Configuration (rollback-friendly):
- The shipped `ModuleData/Enlisted/enlisted_config.json` enables the Lance Life layers by default.
- You can disable any subsystem safely for rollback/testing:
  - `lance_life.enabled` (StoryPacks daily stories)
  - `lance_life_events.enabled` (Events catalog)
  - `lance_life_events.automatic.enabled` (automatic scheduling)
  - `lance_life_events.player_initiated.enabled` (Camp Activities menu events)
  - `lance_life_events.onboarding.enabled` (onboarding state machine)
  - `lance_life_events.incident_channel.enabled` (native incident delivery channel)
  - `escalation.enabled`, `lance_personas.enabled`, `player_conditions.enabled`

Related system (shipping):
- Camp Activities menu actions are defined in JSON at `ModuleData/Enlisted/Activities/activities.json`.

Initial implementation notes:
- Stories are gated by **tier** (`tierMin`/`tierMax`) and by whether the lance is **finalized** (`requireFinalLance`).
- Stories fire on **daily tick** and only when safe (no battle/encounter/prisoner).

## Related research docs (design drafts)
These are longer, more free-form drafts that can inform future expansion:
- **[Lance Career System (draft)](../../research/lance_career_system.md)**
- **[Lance Life Doc Index (import)](../../research/lance_life_INDEX.md)**
- **[Story brainstorming prompt](../../research/story-writer-prompt.md)**

### Outputs
- Enlisted story incident choices (text + results)
- Small state adjustments:
  - Skill XP
  - Fatigue
  - Pending muster pay / promissory pay (ledger)
  - Quartermaster mood/stockout flags for the day
  - “Heat/discipline” risk accumulation

## Core concepts

### Lance identity as a story anchor
Every lance has a `storyId` already (catalog placeholder). Lance Life uses that as the primary hook:
- “This story belongs to *The Iron Spur*,” or
- “This story belongs to the *Feudal cavalry* archetype,” or
- “This story belongs to any lance with roleHint=cavalry.”

This prevents stories from feeling generic and makes the lance matter.

### Lance Life Stories (StoryPacks)
The “StoryPacks” layer is the original Lance Life system: daily-tick story popups sourced from a folder of themed JSON packs.

Shipping content path:
- `ModuleData/Enlisted/StoryPacks/LanceLife/*.json`

Organization (recommended):
- One theme per file (small packs, not one giant file): `training.json`, `logistics.json`, `morale.json`, `corruption.json`, `medical.json`, etc.

Rules (contracted):
- Each pack declares a `schemaVersion` and `packId`.
- Story IDs must be globally unique (namespacing recommended).
- Options are declarative (XP/fatigue/gold/escalation/condition rolls); no event-specific hard-coded logic.

**Validation and diagnostics (required):**
- On campaign start (or first load), we validate all packs and log a readable report:
  - duplicate IDs
  - missing required fields
  - invalid triggers/categories
  - invalid skill names / malformed effect payloads
- Bad packs/events should be **skipped safely** (do not crash the campaign).

**Localization (required):**
- Story packs must provide **string IDs** for all player-facing text (title, body, option text, option hints).
- Translations live in `ModuleData/Languages/enlisted_strings.xml`.
- Events may use placeholders like `{PLAYER_NAME}`, `{LORD_NAME}`, `{LANCE_NAME}` (resolved at runtime).
- Raw text fields may exist only as **fallback English** (so missing translations never crash).

**Contract (source of truth):**
- **[Content Pack Contract — Lance Life Story Packs](../Core/story-pack-contract.md)** defines the required folder layout, schema, ID rules, localization mapping, and validation behavior.

Each story entry should be self-contained and removable:
- `id`, `tags`, `lanceStoryIds[]` or `roleHints[]` or `styleIds[]`
- `requirements` (tier range, min skills, “must be enlisted”, “must be final lance”)
- `triggers` (cooldowns, chance, required camp conditions, “after battle”, “after raid”, “after X days away from town”)
- `options[]` with:
  - player-facing label + hint
  - optional skill checks
  - effects (XP, fatigue, gold/ledger adjustments, heat changes, relation impacts)

This structure keeps the system “easy to add on and take away.”

### Lance Life Events (Events Catalog)
The “Events Catalog” layer is the newer Lance Life content system: a unified event schema + loader + scheduler + delivery channels.

Shipping content path:
- `ModuleData/Enlisted/Events/*.json` (packs)
- `ModuleData/Enlisted/Events/schema_version.json` (schema version marker)

Key differences vs StoryPacks:
- Events have explicit **delivery metadata**:
  - `delivery.method`: `automatic` or `player_initiated`
  - `delivery.channel`: `inquiry`, `menu`, or `incident`
  - `delivery.incident_trigger`: only used for `channel: "incident"` moment events (e.g. `LeavingBattle`)
- Player-initiated events surface as **Camp Activities** menu options; automatic events are scheduled and shown when safe.
- All player-facing text is localized via string IDs in `ModuleData/Languages/enlisted_strings.xml` (fallback text allowed).

Schema and validation:
- Schema reference: **`docs/Features/Technical/lance_life_schemas.md`**
- Loader validates unique IDs, 2–4 options, and trigger token vocabulary; invalid entries are skipped safely.

### Triggers and cooldowns
To avoid spam and keep realism:
- **Daily evaluation** only (no per-frame checking).
- Each story has:
  - `cooldownDays`
  - `maxTimesPerTerm` (optional)
  - `minDaysSinceEnlist` or `minDaysSinceLanceFinalized` (optional)
- A global limiter:
  - `maxLanceStoriesPerWeek` (default 1–2)

### Escalation: heat, discipline, reputation, medical risk
Phase 4 adds Enlisted-owned escalation tracks that create readable, reversible long-term consequences:
- **Heat**: contraband/corruption attention
- **Discipline**: misconduct scrutiny and temporary progression friction
- **Lance Reputation**: social standing inside the lance
- **Medical Risk**: compounding risk tied to injuries/illnesses

How it works (shipping):
- Story options can apply deltas via `effects` (e.g., `effects.heat = +2`).
- Thresholds queue dedicated consequence stories (e.g., shakedown/audit/hearing) via the `escalation_thresholds.json` pack.
- Tracks decay over time (enabled by default; can be disabled via config) so “clean service” is a recovery path.

### Player conditions: injury, illness, exhaustion
Phase 5 adds a lightweight player condition layer (all internal to Enlisted):
- Story options can roll **injury** and/or **illness** blocks.
- A daily tick applies recovery (and possible worsening) and can integrate with **Medical Risk**.
- Certain categories (notably training) can be gated when the player has severe conditions.

### Named lance personas (placeholders)
Phase 5 adds deterministic, text-only personas for lance roles (leader/second/veterans/soldiers/recruits):
- Personas are not real troops and do not modify party composition.
- They exist to power story flavor via placeholders like `{LANCE_LEADER_RANK}`, `{LANCE_LEADER_NAME}`, `{SECOND_RANK}`, `{SECOND_NAME}`.

## Story types (what we can ship)

### 1) Skills & drills
Short events that build “soldier skills”:
- Night lance drill (Leadership + Polearm XP, fatigue cost)
- Formation sparring (Athletics, weapon skill XP; risk of minor injury)
- Mounted practice (Riding, Polearm; only if cavalry/horse archer)

### 2) Logistics & scrounging
Stories driven by `LogisticsStrain`:
- “No bread in the wagons.” -> forage choice (Steward/Scouting), fatigue cost, food gained
- “Broken tack and loose rivets.” -> repair/scrounge (Engineering), small gear outcome later (future hook)

### 3) Corruption and theft (quartermaster tent)
Stories driven by `PayTension` + `ContrabandHeat`:
- “Ledger skim”: quartermaster offers an under-the-table deal (Roguery/Charm)
- “Supply tent theft”: your lance mates propose lifting a crate
  - Options:
    - Refuse (discipline improves; maybe morale hit)
    - Participate (Roguery check; gain supplies/coin; heat rises)
    - Snitch/Report (Charm/Leadership; reduces heat; relation tradeoffs)

Critical design rule:
- Theft should be **tempting but risky**, and consequences should tie back into Quartermaster mood/prices and Pay Muster outcomes.

### 4) Morale and revelry (smuggling drink)
Stories driven by `MoraleShock`:
- “The lads need drink after losses.”
  - Smuggle beer (Roguery) -> morale relief / fatigue relief, heat rises
  - Buy legally (gold) -> smaller effect
  - Refuse -> no heat, morale stays low

### 5) Rivalries and discipline
Stories that build relationships and hard choices:
- Rival lance accuses yours of theft (Charm/Leadership/Intimidation)
- Guard rotation dispute (Leadership/Charm; fatigue redistribution)
- “Pinned blame” incident (Roguery to evade; honor path to accept punishment)

## Phased plan

This feature follows the master phased rollout plan:
- **[master roadmap](../../ImplementationPlans/master-implementation-roadmap.md)**

## Edge Cases
- Player has no lance (or only provisional): only allow “generic camp” stories; prefer final-lance gating for lance-specific stories.
- Player is on leave / grace / prisoner: never fire stories.
- Too many popups: enforce the weekly limiter and cooldowns strictly.
- Save/load: cooldowns and “last fired” timestamps must persist and not double-trigger on load.

## Acceptance Criteria
- Stories can be enabled/disabled by config without code changes.
- Story content is organized as multiple packs (folder-based) and validated on load (bad entries skipped safely).
- At least one story type exists for:
  - skill/drill, logistics, corruption, theft, morale
- Story triggers correlate with camp conditions (e.g., high logistics strain -> forage story appears).
- Theft stories affect Quartermaster/pay outcomes in a readable way (mood/price change, small punishments), without breaking vanilla systems.
- If escalation is enabled, some stories apply `effects` and threshold consequence stories can occur at appropriate track levels.
- If player conditions are enabled, some stories can apply injury/illness rolls and the status UI reflects it.

## Technical implementation (high-level)
- New behavior (example name): `LanceStoryBehavior`:
  - Subscribes to `CampaignEvents.DailyTickEvent` and evaluates triggers.
  - Reads story pack JSON from ModuleData.
  - Uses Enlisted incident UI to present choices.
  - Persists:
    - last-fired timestamps per story
    - per-term counts
    - internal meters (heat/discipline), if not owned by Camp Life
- Integration points:
  - `EnlistmentBehavior.OnLanceFinalized(...)` to unlock lance-specific story pool.
  - Camp Life snapshot as a dependency (optional but recommended).


