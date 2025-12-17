# Feature Spec: Camp News + Decisions + Lance Management Surfaces

## Overview
Players spend most of their enlisted time in the **Enlisted Main Menu** (`enlisted_status`). That screen must always show a compact, readable “what’s happening” update for the player’s lance and the broader camp/company.

At the same time, the game needs:
- A place to **configure** longer-term settings (“toggles”) like lance schedule policy and camp management.
- A place to **react quickly** when events occur (cover requests, incidents, shortages, discipline problems).

This spec defines a three-surface model:
- **Enlisted Main Menu (`enlisted_status`)**: “Status + digest” (light RP, fast scanning).
- **Decisions Menu (`enlisted_decisions`)**: “React now” (quick choices).
- **Camp Management (Gauntlet: `CampManagementScreen`)**: “Configure / manage” (in-depth, occasional).

## Purpose
- Make the camp feel alive via a **daily digest + short updates** on the main menu.
- Ensure “something happened” moments are handled via **Decisions**, not by forcing the player into deep screens.
- Keep complex configuration (schedules, lance management, reports) in **Camp Management**, visited periodically.
- Maintain a consistent navigation model (avoid duplicated hubs / conflicting menu ownership).

## Inputs / Outputs

### Inputs
- **Lance simulation signals** (from multi-lance, lord-party-derived simulation):
  - Roster changes: wounded/dead/replacements.
  - Availability signals: “cover needed”, “short-handed”, etc.
  - (Optional) morale/needs deltas per lance.
- **Camp/Company state**:
  - Lord party movement (settlement, march, army join/leave).
  - Camp Life snapshot (pay tension, logistics strain, contraband heat).
  - Schedule state (current block, missed/complete blocks, orders).
- **Player state**:
  - Enlistment tier (T1–T9), formation/duty, fatigue, escalation tracks, conditions.

### Outputs
- **Main menu digest block** (short, stable, readable):
  - “Today’s Brief” (once per in-game day).
  - “Recent updates” (since last check, capped).
  - “Pending decisions” count and top 1–3 headlines.
- **Decision items** (queue of actionable prompts):
  - Each decision has: title, short summary, 2–4 options, consequences.
  - Decisions can be initiated by lance simulation, camp state, or narrative events.
- **Camp toggles / management state**:
  - Schedule policy toggles and any management knobs.
  - Multi-lance roster viewing and reports.

## Behavior

### A) Enlisted Main Menu (`enlisted_status`) — “Status + Digest”
- **Always visible**:
  - Lightweight **camp/lance news** (short headlines, light RP).
  - “Pending decisions” indicator (count + top headlines).
- **No deep management here**:
  - The player should not need to open Camp Management for routine awareness.
  - The player should not need to manage multiple lances here.
- **Navigation**:
  - Keep an entry to “Camp” (hub) and/or “Visit Camp” (deep).
  - Keep an entry to “Decisions” (react).
  - Keep quick “My Lance” interactions (roster glance, talk, welfare) if we decide they belong in the main loop.

Implementation notes:
- `EnlistedNewsBehavior` already supports:
  - A **Daily Brief** (stable per day).
  - Kingdom + personal feeds (capped).
- We should extend/consume simulation outputs to generate:
  - Lance headlines (“Two men wounded in the 3rd Lance.”).
  - Camp/company headlines (“Paymaster delays payment again.”).

#### Company cadence (locked)
We are intentionally using a **two-tier cadence** to keep the game readable and to avoid spam:

- **Daily Report (authoritative)**:
  - Exactly **one Daily Report per in-game day** (“Today’s Report”).
  - The Daily Report can contain multiple short lines/sections (not limited to a single sentence).
  - This replaces the idea of “stable one-liners” as the primary surface; the report is the canonical daily summary.
  - The main menu shows a compact excerpt; the full report lives in the Reports surface.

- **High-signal dispatch items only** (personal feed):
  - Emit dispatch items only for major beats and threshold crossings:
    - **Resupply stop** (village/town) and outcome (food restored / couldn’t buy enough).
    - **Recruiting stop** (replacements taken / unable to recruit).
    - **Threat spike** (scouts report danger; likely avoid/flee; “keep distance” posture).
    - **Battle preparation / imminent clash** (enemy near; Company likely to engage or retreat).
  - Use dedupe keys (`StoryKey`) so repeated checks update a single story instead of adding new lines.

Scope note:
- All “Company” narration is centered on the enlisted lord’s `MobileParty`.
- If the Company is attached to a larger in-game Army, we acknowledge it only as context (“attached to Lord X’s host”), but we do not attempt army-wide simulation yet.

#### Daily Report + Rumors (once per in-game day)
Goal: make the camp feel alive with a single daily digest (in the main menu) and a richer report (in Reports), without spamming the player.

Deliverable:
- The main menu (`enlisted_status`) shows **Today’s Report (excerpt)** **once per in-game day** as a **single compact paragraph** (diary-style), optimized for low vertical space.
- The Reports surface shows the **full Daily Report** for today (multi-line / sectioned), and optionally a small archive of prior days.
- The Daily Report can include (when relevant):
  - **Lance**: wounded/sick/deaths, replacements, training events, discipline incidents.
  - **Company**: stops, needs (food/morale), threat posture, movement, battle/siege prep.
  - **Kingdom + Rumors**: grounded headlines plus uncertainty-framed camp talk when intel is weak.

Input model (facts, not prose):
- We generate a `DailyReportSnapshot` with small structured fields and scores:
  - **Lance facts**: `woundedDelta`, `sickDelta`, `deadDelta`, `replacementsDelta`, `trainingEventTag`, `disciplineTag`, `fatigueBand`.
  - **Company facts**: `objectiveTag`, `lastStopTag` (resupply/recruit), `foodDaysBand`, `moraleBand`, `threatBand`, `battleTag`, `attachedArmyTag`.
  - **Kingdom facts**: top `kingdomFeed` headlines, plus regional context (recent battle/siege nearby) if available.
  - Each candidate fact carries: `severity`, `freshness`, `confidence` (0–1), `dedupeKey`.

Selection rules (what makes it into the Daily Report):
- Prioritize “high-signal” over ambient:
  - **Battle/Siege/Capture/Death** > **Threat spike** > **Resupply/Recruit stop** > **Meaningful movement** > **Training/Drill** > **Ambient**.
- Use `dedupeKey` + threshold crossings so micro-changes are summarized instead of enumerated.
- Produce a **bounded list** (e.g., 3–6 lines for the main-menu excerpt, 6–12 lines for the full report).
- If nothing notable happened, emit an ambient line (e.g., drilling, march routine, camp grumbling) rather than silence.

Wording rules (how we safely express uncertainty)
- We do not generate free-form text; we choose from **template libraries** per category.
- Confidence maps to “hedge language”:
  - **High (≥ 0.75)**: declarative (“We’re marching to…”, “The Company will…”).
  - **Medium (0.45–0.75)**: likely/uncertain (“Likely…”, “Scouts think…”, “We may…”).
  - **Low (< 0.45)**: rumor framing (“Rumor has it…”, “Hard to say…”, “Camp talk says…”).
- Rumors must be grounded in at least one weak signal (nearby conflict, kingdom headline, nearby enemy presence, or attachment context). If no signals exist, use harmless ambient rumor.

Formatting guidance:
- **Main menu excerpt**: 1 paragraph, 2–5 sentences, with gentle separators (periods / em dashes). Avoid bullet lists in `enlisted_status`.
- **Reports full view**: can be multi-line and categorized (Lance / Company / Kingdom+Rumors) for scanning.

Example (main menu paragraph excerpt):
- “Today’s Report: Fever ran through the tents and two men were pulled from duty. Replacements arrived—green boys, but they listened. The Company is likely marching toward Jaculan; rations look thin. Rumor has it a siege tightens in the north, but no one can name which banner will break first.”

### B) Decisions Menu (`enlisted_decisions`) — “React Now”
- **Purpose**: resolve time-sensitive or narrative triggers without sending the player into management UI.
- **Primary UX goal**: the Decisions menu should feel like **Camp Life** first (what’s going on, what needs attention), and **choices** second (what can I do about it).
- **Sources**:
  - Lance simulation (cover requests, injuries, discipline incidents).
  - Camp life snapshot thresholds (pay tension spikes, logistics shortage).
  - Narrative events (lance mate asks help, lord request).
- **Rules**:
  - Decisions should be infrequent enough to avoid spam.
  - Decisions should be clearly connected to current context (lance/camp state).
- **Outcomes**:
  - Apply changes to internal state (escalation, fatigue, roster state, etc.).
  - Post a short “result” line back into the main menu digest.

#### Decisions menu layout (Camp Life first)
At the top of `enlisted_decisions`, show a compact **Camp Life dashboard** before listing choices.

Camp Life dashboard content (always at top):
- **News Summary**: today’s Daily Report excerpt (same paragraph shown on main menu) + 1–2 recent high-signal dispatch headlines (if any).
- **Company Events (30 days)**:
  - “Men lost (last 30 days): X”
  - “Currently wounded: Y” / “Currently sick: Z”
  - “Training incidents (last 30 days): N” (optional; derived from event outcomes)
- **Current pressure** (short labels): food pressure, morale pressure, threat pressure (best-effort bands).

Implementation note:
- The “last 30 days” stats require a small persistent **CampLifeLedger** (ring buffer of daily aggregates) so we can compute rolling totals cheaply and deterministically.

#### Categorized Decisions (organized by type)
Below Camp Life, show decisions grouped into categories so the player can browse like a “tab” even in a menu list.

Authoring rule:
- Use `delivery.menu_section` in the event JSON as the **canonical category**. This is already in the schema.

Recommended category taxonomy (initial):
- **camp_life**: shortages, sickness, discipline, pay tension, camp incidents
- **combat**: battle prep, volunteering, aftermath, salvage/risk
- **intel**: scouting, screening, patrol adjustments, enemy sightings (context-dependent)
- **training**: drills, mentorship, sparring, formation practice
- **social**: dice, drinks, letters, favors, disputes
- **logistics**: rations, requisitions, equipment upkeep, quartermaster favors

Display rules:
- Show categories in priority order (camp_life first).
- Within a category, sort by urgency/priority (critical/high/normal) and then by evaluator priority.
- Cap visible decisions per category (e.g., top 3) with a “more…” line if needed (later enhancement).

#### “Make it fun” mechanics: rank-gated delegation + real consequences
Many powerful actions should be possible at low rank, but **not directly**. The fun comes from negotiating up the chain.

Pattern: delegation decision
- If the player is low tier, the decision becomes: “Convince {LANCE_LEADER} to …”
- Success routes to the real action (chain event), failure carries social/discipline costs.
- Use `narrative_source: lance_leader` + tier gates to enforce social position.

Pattern: operations decisions (combat/intel)
- Decisions can create **real campaign consequences** (bounded, local, temporary):
  - **Slow** a nearby enemy party for a short window (hours) after a successful screen/harass.
  - **Skirmish casualties**: remove a small number of low-tier troops from a target party (with risk of injuries on our side).
  - **Intel reveal**: add a report entry that names the party and estimates strength/route (even if we avoid direct world edits at first).
- These should be implemented via a small “WorldEffects” layer with TTLs to avoid long-lived state and mod conflicts.

#### Linking Company high-signal beats to Decisions and Events
The high-signal Company states above are the primary “situation” inputs that can drive:
- **Pushed (automatic) decisions** (inquiry popups) when time-sensitive (see `docs/StoryBlocks/decision-events-spec.md`).
- **Player-initiated decisions** in the Decisions menu when not urgent (same spec).

Rules:
- Company narration should **not** create decisions directly inside UI code.
- Instead, narration updates should set/refresh **state flags and context variables** that the decision system can evaluate.
- When a decision resolves, it should post a short follow-up line back into the digest (as a dispatch update).

#### How decisions work today (code reality)
This section documents the current pipeline so we can extend it without guessing.

Surfaces:
- **Player-initiated decisions**:
  - Listed in `enlisted_decisions` menu via `DecisionEventBehavior.GetAvailablePlayerDecisions()`.
  - Selecting a decision calls `DecisionEventBehavior.FirePlayerDecision(eventId)` which opens the modern Lance Life Event UI (`ModernEventPresenter` → `LanceLifeEventScreen`).
- **Automatic (pushed) decisions**:
  - Evaluated on configured hours (default **08:00 / 14:00 / 20:00**) by `DecisionEventBehavior` hourly tick.
  - Eligible events are `LanceLifeEventDefinition` where `Category == "decision"` and `Delivery.Method == "automatic"`.
  - Selected via `DecisionEventEvaluator` which applies pacing protections (daily/weekly caps, min hours between events, cooldowns, story flags, mutual exclusion, tier gating, formation gating, etc.).
  - Selected events are **queued** and only fired when safe (not in encounter/conversation and not in certain paused time modes).

Key config levers (current):
- `DecisionEventConfig.Pacing`: max/day, max/week, min hours between, evaluation hours, quiet-day chance.
- `DecisionEventConfig.Activity`: boosts weights for events matching current activity/duty (supports “contextual” decisions).
- `DecisionEventConfig.Menu`: max visible decisions; show unavailable decisions (greyed) if desired.

Examples (conceptual):
- **Resupply stop**:
  - Decision: “Buy grain at a premium vs push on and ration.”
  - Outcome updates: `company:needs:food` story + daily brief next day.
- **Recruiting stop**:
  - Decision: “Take green recruits now vs wait for better volunteers.”
- **Threat spike**:
  - Decision: “Send scouts wide (fatigue cost) vs tighten formation (slower pace).”
- **Battle prep**:
  - Decision: “Volunteer for vanguard duty / prepare the men / rest before battle.”

### C) Camp Management (Gauntlet: `CampManagementScreen`) — “Configure / Manage”
- **Purpose**: the place for periodic “admin” actions:
  - Lance schedule policy toggles and knobs.
  - Multi-lance roster view (all lances in lord’s party).
  - In-depth Reports (lance/company/kingdom categories).
- **Frequency**: the player should visit “every so often,” not as the default loop.

### D) Menu ownership and navigation consistency
We must avoid conflicting routing for the camp hub.

Current status:
- The Camp hub menu id (`enlisted_camp_hub`) must have a single canonical owner.
- In the current codebase, the hub is owned by the interface/menu behavior and other systems should only register submenus routed from it.

Rule:
- There must be **one canonical owner** of the camp hub menu id (`enlisted_camp_hub`).
- All other systems should register *submenus* and be routed to, but not re-register the hub.

## Edge Cases
- **Decision spam**: too many prompts in a short time window.
- **No-lance / provisional lance**: main menu should still show camp news; lance lines may be generic.
- **Rapid party changes** (lord gains/loses troops frequently):
  - Digest should summarize changes instead of emitting one line per micro-change.
- **Save/load**:
  - Daily brief must not regenerate multiple times per day.
  - Pending decisions must not duplicate on load.
- **UI availability**:
  - Avoid showing decisions during unsafe moments (battle/encounter/captivity).
- **Tier gating**:
  - Management toggles should be restricted to the appropriate tiers (e.g., lance schedule authority).

## Acceptance Criteria
- **Main menu feels alive**:
  - `enlisted_status` shows a short digest that updates at least daily and includes lance/camp happenings.
  - Digest lines are readable, not walls of text, and remain stable for the day where appropriate.
- **Decisions are the “react now” layer**:
  - When time-sensitive events occur, the player can resolve them via `enlisted_decisions` without opening Camp Management.
  - Resolution posts a short follow-up line to the digest.
- **Camp is the “configure/manage” layer**:
  - Lance schedule toggles and multi-lance management live in Camp Management.
  - Camp Reports provide an in-depth view beyond the digest.
- **No duplicate camp hub**:
  - Only one system registers `enlisted_camp_hub`; navigation is consistent.

## Open Questions (answer before implementation)
1) **Main menu news tone**: should the digest be mostly:
   - **Diegetic** (“The lads grumble by the fires…”) OR
   - **Operational with light RP** (“Morale low — men grumble by the fires.”)?

2) **Decision cadence / queueing**: should we:
   - **Allow a small queue** (cap N pending, e.g. 3–5), OR
   - **Enforce only one pending decision at a time** (next decision waits)?

3) **Daily Summary detail**: should the once-per-day summary be:
   - **1 line total** (very compact), OR
   - **Daily Report excerpt (3–6 lines)** in `enlisted_status` + full report in Reports (recommended)?

## Implementation Plan (Company Daily Report + Rumors + Decision hooks)
This plan is scoped to the player’s **Company** (enlisted lord’s party) and the player’s **Lance**, with kingdom dispatch as context.

### Phase 1 — Data model + template library
- **Daily snapshot struct**:
  - Create `DailyReportSnapshot` and small enums/bands (`ThreatBand`, `FoodBand`, `MoraleBand`, `HealthDeltaBand`, `TrainingTag`, etc.).
  - Store only what we need to summarize; keep it serializable / save-safe.
- **Template library**:
  - Create a small template set per section/category: Lance health, Lance training, Company movement/needs, Company threat/battle prep, Kingdom headline, Rumor.
  - Templates accept placeholders (lord name, settlement, counts, etc.).
  - Add `Confidence → hedge phrase` mapping table.

### Phase 2 — Daily generation + persistence (no spam)
- Add a “generate-once-per-day” entry point (reusing `EnlistedNewsBehavior` daily infrastructure):
  - Generate and persist **Today’s Daily Report** (a small list of lines + optional metadata).
  - Ensure “once per in-game day” gating survives save/load.
- Add the candidate-fact selection logic:
  - Build candidates from current state + deltas since yesterday (stored snapshot).
  - Pick best N candidates using priority + severity + freshness until we fill the excerpt/report limits.
  - Convert candidate → template → localized string using confidence hedging.

### Phase 3 — Main menu surface (Today’s Report paragraph excerpt)
- Ensure `enlisted_status` displays Today’s Report excerpt as a **single paragraph** via `EnlistedNewsBehavior` formatting.
- If needed, add a short “Recent updates” header under the Daily Summary later; do not expand beyond readability limits.

### Phase 3.5 — Reports surface (full Daily Report)
- Add/extend the Reports UI to show the **full Daily Report** (multi-line / categorized), and optionally a small archive.

### Phase 4 — News events feeding the report (facts)
- Add fact producers that populate the snapshot inputs:
  - **Company**: settlement stops, food days band, morale band, threat band, objective tag, attachment context.
  - **Lance**: wounded/sick/dead deltas, training tag (drill/inspection), discipline tag.
  - **Kingdom**: reuse kingdom feed headlines already tracked.
- Keep producers read-only where possible; they should *observe* and *summarize*, not override AI.

### Phase 5 — Decision hooks (RPG/Text events)
- Define a small set of “situation flags” derived from the same snapshot (examples):
  - `company_food_critical`, `company_threat_high`, `lance_fever_spike`, `lance_short_handed`, `battle_imminent`.
- `DecisionEventBehavior` evaluates those flags to:
  - Queue **player-initiated** decisions in `enlisted_decisions`, and/or
  - Push **automatic** inquiry decisions for urgent situations (per `docs/StoryBlocks/decision-events-spec.md`).
- When a decision resolves, it should:
  - Update underlying state (fatigue/morale/health/etc.).
  - Post a short dispatch follow-up (high-signal), and influence next day’s Daily Summary.

### Phase 5.5 — Make Decisions feel “important” via Reports + Daily Report
Objective: the main menu Daily Report and Reports tab should *point at choices* and *show consequences*, so the player feels agency day-to-day.

Design additions:
- **Daily Report excerpt includes “Opportunities” when applicable**:
  - If `DecisionEventBehavior.GetAvailablePlayerDecisions()` returns any, append a short sentence:
    - Example: “Opportunities: {COUNT} matters await your decision. The most pressing: {TOP_TITLE}.”
  - Keep it one sentence max in the paragraph.
- **Reports tab adds an “Opportunities” category** (in Camp Reports):
  - List the top N available decisions (title + short setup blurb + costs/rewards hint).
  - Optionally show unavailable-but-relevant decisions if `DecisionEventConfig.Menu.ShowUnavailable` is enabled (with “why locked” messaging).
  - Selecting an item should route the player to the decision (open `enlisted_decisions` or fire directly via `FirePlayerDecision` if safe).
- **Reports tab adds a “Recent Outcomes” list**:
  - Record the last K resolved decision outcomes (eventId + chosen option + timestamp + result text).
  - Show these as short report items so the narrative feels continuous.

Implementation notes:
- Today, decision resolution applies effects but does not persist a readable “outcome log”. We will need to add a small persistent “decision outcome feed” (strings/ids only) to support “Recent Outcomes” and the Daily Report’s sense of continuity.

### Phase 6 — Acceptance checks (behavioral)
- Daily Summary appears once/day and stays stable through the day.
- Summary includes grounded Lance/Company/Kingdom+Rumor content.
- No dispatch spam: repeated checks update one story via dedupe keys; only threshold crossings emit new high-signal items.
- Decisions are driven by situation flags; UI code does not create decisions.


