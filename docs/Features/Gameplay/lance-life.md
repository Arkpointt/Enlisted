# Feature Spec: Lance Life (Stories, Camp Events, and Progression)

This spec builds “life around the lance”: small stories, camp events, and morally gray choices that grow the player’s skills while enlisted. It is designed to be **mod-friendly** and **internal** to Enlisted by using event-driven state + Enlisted-owned incidents/menus (not rewriting vanilla economy/AI).

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
  - [Risk model: heat, discipline, and consequences](#risk-model-heat-discipline-and-consequences)
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
  - Camp menu entries (“My Camp”)
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

## Quick start (text-based, low complexity)
This feature is intentionally “Viking Conquest”-style: short text scenes with a few options.

To enable it:
- In `ModuleData/Enlisted/enlisted_config.json`:
  - Set `lances.lances_enabled = true` (so the player has a lance identity)
  - Set `lance_life.enabled = true`
- Add/edit story packs under `ModuleData/Enlisted/StoryPacks/LanceLife/`
  - Example: `core.json`, `training.json`, `corruption.json`, `logistics.json`

Initial implementation notes:
- Stories are gated by **tier** (`minTier`) and by whether the lance is **finalized** (`requireFinalLance`).
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

### Story packs (data-driven content)
Stories must be defined in a separate, additive **content pack** layer so we can expand safely without touching code.

**Required approach:**
- A dedicated folder for Lance Life story packs:
  - `ModuleData/Enlisted/StoryPacks/LanceLife/*.json`
- Packs are organized by theme (small files, not one giant file):
  - `core.json`, `training.json`, `corruption.json`, `morale.json`, `logistics.json`, etc.
- Each pack declares a `schemaVersion` and (optionally) a `packId` for diagnostics.
- Every event `id` is **namespaced** (recommended: `{packId}.{eventName}`) to prevent collisions.
- Events are defined using a stable schema:
  - **Requirements are declarative** (tier, final-lance, time-of-day, activity/AI-state, camp conditions).
  - **Options use small, composable effect types** (XP, fatigue, gold/ledger, heat, discipline, reputation).
  - We do **not** ship “special-case code” that exists only for a single event ID.

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

### Triggers and cooldowns
To avoid spam and keep realism:
- **Daily evaluation** only (no per-frame checking).
- Each story has:
  - `cooldownDays`
  - `maxTimesPerTerm` (optional)
  - `minDaysSinceEnlist` or `minDaysSinceLanceFinalized` (optional)
- A global limiter:
  - `maxLanceStoriesPerWeek` (default 1–2)

### Risk model: heat, discipline, and consequences
For theft/contraband, track two small internal meters:
- **ContrabandHeat** (camp crackdowns; impacts smuggling/theft risk)
- **DisciplineRisk** (how close you are to punishment/probation outcomes)

Consequences should stay internal and readable:
- Price hikes / buyback drops at Quartermaster for a time
- Fatigue penalties (extra duty)
- Wage penalty / probation-like modifier
- “Confiscation” event (lose a portion of stash)

Avoid:
- Permanent campaign-wide crime spirals
- Forced game-overs

## Story types (what we can ship)

### 1) Skills & drills
Short events that build “soldier skills”:
- Night lance drill (Leadership + Polearm XP, fatigue cost)
- Formation sparring (Athletics, weapon skill XP; risk of minor injury)
- Mounted practice (Riding, Polearm; only if cavalry/horse archer)

### 2) Logistics & scrounging
Stories driven by `LogisticsStrain`:
- “No bread in the wagons.” → forage choice (Steward/Scouting), fatigue cost, food gained
- “Broken tack and loose rivets.” → repair/scrounge (Engineering), small gear outcome later (future hook)

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
  - Smuggle beer (Roguery) → morale relief / fatigue relief, heat rises
  - Buy legally (gold) → smaller effect
  - Refuse → no heat, morale stays low

### 5) Rivalries and discipline
Stories that build relationships and hard choices:
- Rival lance accuses yours of theft (Charm/Leadership/Intimidation)
- Guard rotation dispute (Leadership/Charm; fatigue redistribution)
- “Pinned blame” incident (Roguery to evade; honor path to accept punishment)

## Phased plan

### Phase 0 — Spec + content format
- Finalize this spec.
- Decide on the story JSON format and folder placement.
- Add placeholder story pack file with 2–3 sample stories (disabled by default).

### Phase 1 — Engine: Lance Story Manager (internal behavior)
- New behavior that:
  - Reads story definitions
  - Evaluates triggers daily while enlisted
  - Fires a single incident/inquiry when a story triggers
  - Records cooldowns and outcomes in save data

### Phase 2 — Safe stories (skills + logistics)
- Add “drill” and “forage” stories first (low controversy, low risk).
- Tie into fatigue and skill XP only.

### Phase 3 — Corruption hooks (quartermaster + pay tension)
- Add corruption stories that:
  - Nudge Quartermaster mood/price modifiers
  - Add “promissory note” / delayed-pay narrative at pay muster (if Camp Life Phase 3 exists)

### Phase 4 — Theft/contraband loops with consequences
- Implement the “supply tent theft” chain with heat/discipline consequences.
- Ensure at least one non-criminal option per story.

### Phase 5 — Expansion and tuning
- Add more stories per lance style and role hint.
- Expose tuning and story enable/disable lists via `enlisted_config.json`.
- Add diagnostics logging (session-only) for balancing.

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
- Story triggers correlate with camp conditions (e.g., high logistics strain → forage story appears).
- Theft stories affect Quartermaster/pay outcomes in a readable way (mood/price change, small punishments), without breaking vanilla systems.

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


