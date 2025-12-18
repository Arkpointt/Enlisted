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

Terminology (important):
- **Main Menu (text)**: `enlisted_status` (a Bannerlord `GameMenu`/`WaitGameMenu`)
- **Camp Menu (text)**: `enlisted_camp_hub` (opened from Main Menu → “Camp”)
- **Camp Bulletin (Gauntlet overlay)**: opened from the Camp Menu “Reports” option
- **Camp Management Screen (Gauntlet)**: `CampManagementScreen` (tabbed deep management UI)

Implementation mapping (where this lives in code)
- **Main Menu (text) wiring**: `src/Features/Interface/Behaviors/EnlistedMenuBehavior.cs`
- **Camp Menu (text) wiring**: `src/Features/Camp/CampMenuHandler.cs`
- **Camp News (daily brief + dispatches)**: `src/Features/Interface/Behaviors/EnlistedNewsBehavior.cs`
- **Decision pipeline**:
  - Behavior + queueing: `src/Features/Lances/Events/Decisions/DecisionEventBehavior.cs`
  - Evaluator + pacing: `src/Features/Lances/Events/Decisions/DecisionEventEvaluator.cs`
  - Content: `ModuleData/Enlisted/Events/events_decisions.json` and `events_player_decisions.json`
- **Reports surfaces**:
  - Camp Bulletin overlay: `src/Features/Camp/UI/Bulletin/CampBulletinScreen.cs` + `CampBulletinVM.cs`
  - Camp Management reports tab (Gauntlet): `src/Features/Camp/UI/Management/Tabs/CampReportsVM.cs`

Navigation intent (key change):
- **Camp Activities are a high-frequency action surface** and should be **one-click accessible** from **Camp Menu (text)** (`enlisted_camp_hub`), not hidden inside deep management UI.
- **Duties are a low-frequency configuration** and should be managed from **Camp Management** (not the main loop).

## Purpose
- Make the camp feel alive via a **daily digest + short updates** on the main menu.
- Ensure “something happened” moments are handled via **Decisions**, not by forcing the player into deep screens.
- Keep complex configuration (schedules, lance management, reports) in **Camp Management**, visited periodically.
- Maintain a consistent navigation model (avoid duplicated menus / conflicting menu ownership).

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
  - Keep an entry to **Camp** (Camp Menu (text): fast access to Camp Activities + Medical Tent).
  - Keep an entry to **Decisions** (react).
  - Keep an entry to **Camp Management** (deep, configure/manage).
  - Keep an entry to **My Lance** (fast glance/interactions).
  - Keep an entry to **Duties** but route it to **Camp Management** (because duties are configuration, not a frequent action).

#### Main menu navigation list (always shown, grey-out with tooltip)
We want the main menu to feel “complete” and predictable. The menu should always list the same navigation entries in the same order.

Rule:
- **All main navigation entries are always listed** in `enlisted_status`.
- If the player cannot access an entry right now, it is **greyed out** and shows a **specific tooltip** explaining why and how to unlock it.

Recommended main menu navigation entries (order):
- **Camp** (opens the Camp Menu (text); contains Camp Activities + Medical Tent).
- **Decisions** (react now; shows pending count).
- **My Lance** (roster + interactions).
- **Camp Management** (Gauntlet deep screen: orders/reports/army/duties).
- **Duties** (shortcut into Camp Management duty assignment view).
- **Medical Attention** (greyed if not injured/ill).
- **Quartermaster** (greyed if not in a context where it makes sense / not enlisted).
- **Service Records** (greyed if not enlisted).
- **Leave / Discharge / Desert** (always shown; eligibility varies).

Tooltip examples (keep short and explicit):
- Camp locked: “You must be enlisted to access Camp.”
- Duties locked: “Duties are assigned in Camp Management. Enlist to access duties.”
- Medical locked: “You are in good health. No treatment needed.”
- Quartermaster locked: “You can only visit the quartermaster when the camp is established / when permitted by rank.”

#### Camp Menu (text) navigation list (always shown, grey-out with tooltip)
Inside the **Camp Menu (text)** (`enlisted_camp_hub`), we want “do something now” actions to be immediately visible.

Rule:
- **All Camp Menu (text) entries are always listed**.
- If an entry is not usable right now, it is **greyed out** with a tooltip explaining why.

Recommended Camp Menu (text) entries (order):
- **Camp Activities** (primary “do something now”).
- **Medical Tent** (greyed if not injured/ill).
- **Reports** (opens Camp Bulletin overlay: daily report / archive / categories).
- **Camp Management** (deep config: orders/reports/army/duties).
- **Service Records** (posting/faction/lifetime summary).

Implementation notes:
- `EnlistedNewsBehavior` already supports:
  - A **Daily Brief** (stable per day) and persistence helpers for “once-per-day” generation.
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

#### Compatibility / safety constraints (do not break other systems)
We want this feature to be “high-impact gameplay, low-risk integration”.

Rules:
- **No hard overrides of native AI**: the Company “intelligence” layer should primarily **observe, summarize, and recommend**.
- **Bounded world effects only**: when we do alter world state (combat/intel ops), effects must be **local, small, and temporary** (TTL measured in hours/days).
- **Save-safe state**: persist only primitives (strings/ints) and bounded lists; avoid storing complex engine objects.
- **UI does not author decisions**: UI reads state and routes actions; it never creates/queues decisions directly.
- **Safe-to-show gating**: pushed decisions obey the existing “safe moment” checks (not in encounter/conversation, etc.).

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

CampLifeLedger (recommended minimal shape):
- **Cadence**: one entry per in-game day (append/rollover once per day).
- **Size**: fixed 30 (or 31) entries; overwrite oldest (ring buffer).
- **Fields** (ints):
  - `lostToday` (deaths)
  - `woundedToday`
  - `sickToday`
  - `trainingIncidentsToday`
  - (optional) `decisionsResolvedToday` / `highSignalDispatchesToday`
- **Derived views**:
  - rolling sum `lost30`, `wounded30`, `sick30`, `trainingIncidents30`
  - current `woundedNow`, `sickNow` (from live state, not ledger)

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

WorldEffects layer (recommended safety design):
- Stores temporary effects as primitives only:
  - `effectId`, `targetPartyId` (string), `expiresAtDay/hour` (int), `magnitude` (int/float), `sourceEventId` (string)
- Enforces strict caps:
  - max active effects (e.g., 10–20)
  - max TTL per effect (e.g., 6–48 hours)
  - max magnitude (small numbers only)
- Prefers **modelled** effects (movement penalty, morale pressure, intel visibility) over direct “force AI behavior” where possible.

#### Decision option “impact preview” (make consequences legible)
To make the system engaging, every decision option should communicate the stakes consistently.

Convention:
- Use existing authored fields where possible (`costs`, `rewards`, `risk`, `success_chance`, injury/illness risk).
- Add an authoring-only “impact tag” in `metadata` (schema already includes a metadata block) when needed:
  - Examples: `impact:slow_enemy`, `impact:intel_reveal`, `impact:discipline_risk`, `impact:lance_rep_gain`

UI rule:
- The Decisions tooltip (and Reports “Opportunities” list) should render a compact line:
  - “Cost: … | Risk: … | Impact: …”

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
  - In-depth Reports (lance/company/kingdom categories) (may mirror Camp Bulletin categories later).
- Duties configuration / assignment (moved here).
- **Frequency**: the player should visit “every so often,” not as the default loop.

### D) Menu ownership and navigation consistency
We must avoid conflicting routing for the Camp Menu (text) (`enlisted_camp_hub`).

Current status:
- The **Camp Menu (text)** id (`enlisted_camp_hub`) must have a single canonical owner.
- In the current codebase, this menu is owned by the interface/menu behavior and other systems should only register submenus routed from it.

Rule:
- There must be **one canonical owner** of the Camp Menu (text) id (`enlisted_camp_hub`).
- All other systems should register *submenus* and be routed to, but not re-register the menu.

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
- **Main menu navigation is predictable**:
  - `enlisted_status` always shows the full navigation list (no “missing menu options”).
  - Inaccessible items are greyed out and provide tooltips explaining why.
- **Decisions are the “react now” layer**:
  - When time-sensitive events occur, the player can resolve them via `enlisted_decisions` without opening Camp Management.
  - Resolution posts a short follow-up line to the digest.
- **Camp Management is the “configure/manage” layer**:
  - Lance schedule toggles and multi-lance management live in Camp Management.
  - Camp Reports provide an in-depth view beyond the digest.
- **No duplicate Camp Menu (text)**:
  - Only one system registers `enlisted_camp_hub`; navigation is consistent.

## Decisions Locked In (implementation defaults)

These were open questions; we’ve now locked defaults so implementation can proceed without ambiguity.

1) **News tone (Daily Report + excerpt)**:
   - **Operational + light RP** (clear info with a little flavor).

2) **Decision cadence / queueing**:
   - **Queue a few pending decisions** so the player can handle them when ready.
   - **Cap:** 5 pending decisions (FIFO by “time added”; discard/merge duplicates via dedupe keys where possible).

## Implementation Plan (Company Daily Report + Rumors + Decision hooks)
This plan is scoped to the player’s **Company** (enlisted lord’s party) and the player’s **Lance**, with kingdom dispatch as context.

### Phase 0 — Alignment + guardrails (before building features)
- Confirm the Decisions schema fields we will rely on are already present (they are today):
  - `delivery.menu_section` for categories
  - `narrative_source` + tier gates for rank-gated delegation
  - `metadata` for authoring hints (impact tags)
- Confirm we can keep the system safe:
  - Pushed decisions already have safe-to-show gating and pacing protections.
  - We will not introduce AI overrides; any world effects will be TTL and bounded.

### Phase 0.5 — Menu reshape (prep for “fleshed out, organized” UI)
Goal: restructure the navigation so the main loop is fast and the deep configuration lives in Camp Management.

Deliverables:
- Update `enlisted_status` to show the full navigation list (always shown, grey-out with tooltip).
- Keep **Camp** as the main-menu entry point for camp-facing actions.
- Inside the **Camp Menu (text)** (`enlisted_camp_hub`), list **Camp Activities** and **Medical Tent** as primary options (always shown, tooltip-gated).
- Move **Duties** management into **Camp Management** (and make the main menu Duty entry route there).
- Keep menu ownership consistent: the Camp Menu (text) (`enlisted_camp_hub`) remains single-owner; other systems route through it.

Implementation checklist (Phase 0.5)
- **Main Menu (text)** (`enlisted_status`):
  - Add/show the navigation entries listed above (even when disabled).
  - Use grey-out + tooltips to explain availability (do not hide).
- **Camp Menu (text)** (`enlisted_camp_hub`):
  - Ensure “Camp Activities” and “Medical Tent” are explicit menu options (always shown; tooltip-gated).
  - Ensure “Reports” clearly opens the Camp Bulletin overlay (not a different menu).
  - Keep this menu single-owner (`EnlistedMenuBehavior` owns the menu id; other systems only add submenus).

### Phase 1 — Data model + template library
- **Daily snapshot struct**:
  - Create `DailyReportSnapshot` and small enums/bands (`ThreatBand`, `FoodBand`, `MoraleBand`, `HealthDeltaBand`, `TrainingTag`, etc.).
  - Store only what we need to summarize; keep it serializable / save-safe.
- **CampLifeLedger**:
  - Implement the 30-day rolling ledger for deaths/sick/wounded/training incidents (primitives only).
- **Template library**:
  - Create a small template set per section/category: Lance health, Lance training, Company movement/needs, Company threat/battle prep, Kingdom headline, Rumor.
  - Templates accept placeholders (lord name, settlement, counts, etc.).
  - Add `Confidence → hedge phrase` mapping table.

Implementation checklist (Phase 1)
- **Add minimal types (save-safe, primitives-first)**:
  - Put them under a Camp News namespace (example): `src/Features/Interface/News/Models/`
  - Keep them independent of engine objects (no `Hero`, `MobileParty`, `Settlement`, etc. stored inside state).

Suggested minimal data shapes:

```csharp
// Bands keep the prose stable; we avoid dumping raw numbers into the main menu.
public enum ThreatBand { Unknown = 0, Low, Medium, High }
public enum FoodBand { Unknown = 0, Plenty, Thin, Low, Critical }
public enum MoraleBand { Unknown = 0, High, Steady, Low, Breaking }

// The factual “input” for one in-game day. This is NOT player-facing text yet.
public sealed class DailyReportSnapshot
{
    public int DayNumber { get; set; }                 // (int)CampaignTime.Now.ToDays
    public string LordPartyId { get; set; }            // string id only (if needed for dedupe)

    // Lance deltas (day-over-day)
    public int WoundedDelta { get; set; }
    public int SickDelta { get; set; }
    public int DeadDelta { get; set; }
    public int ReplacementsDelta { get; set; }

    // Company bands (best-effort)
    public ThreatBand Threat { get; set; }
    public FoodBand Food { get; set; }
    public MoraleBand Morale { get; set; }

    // Optional tags (string enums are OK too)
    public string ObjectiveTag { get; set; }           // e.g. "Traveling", "Besieging"
    public string LastStopTag { get; set; }            // e.g. "resupply", "recruit"
}

// 30-day rolling ledger (ring buffer). All ints.
public sealed class CampLifeLedger
{
    public int Capacity { get; set; } = 30;
    public int HeadIndex { get; set; }                 // current write position
    public DailyAggregate[] Days { get; set; }         // fixed array length = Capacity
}

public struct DailyAggregate
{
    public int DayNumber;
    public int LostToday;
    public int WoundedToday;
    public int SickToday;
    public int TrainingIncidentsToday;
    public int DecisionsResolvedToday;
}
```

- **Template library structure**:
  - Create a small “template id → string format” table per category (operational + light RP).
  - Do not generate free-form text; select templates + fill placeholders.

```csharp
public enum ConfidenceBand { Low, Medium, High }

public sealed class NewsTemplate
{
    public string Id { get; set; }           // stable id for debugging/telemetry
    public string Format { get; set; }       // e.g. "Rations look {FOOD_DESC}. {HEDGE} we march toward {SETTLEMENT}."
    public string Category { get; set; }     // "lance" | "company" | "kingdom" | "rumor"
}
```

### Phase 2 — Daily generation + persistence (no spam)
- Add a “generate-once-per-day” entry point (reusing `EnlistedNewsBehavior` daily infrastructure):
  - Generate and persist **Today’s Daily Report** (a small list of lines + optional metadata).
  - Ensure “once per in-game day” gating survives save/load.
- Add the candidate-fact selection logic:
  - Build candidates from current state + deltas since yesterday (stored snapshot).
  - Pick best N candidates using priority + severity + freshness until we fill the excerpt/report limits.
  - Convert candidate → template → localized string using confidence hedging.

Implementation checklist (Phase 2)
- **Persisted state (save-safe, bounded)**:
  - Put under `src/Features/Interface/News/State/` (or keep nested inside `EnlistedNewsBehavior` if you prefer one behavior).
  - Persist only primitives and short arrays/lists.

Suggested persisted state shape:

```csharp
public sealed class CampNewsState
{
    // “Generate once per in-game day” gate.
    public int LastGeneratedDayNumber { get; set; } = -1;

    // A small archive (bounded). Keep last N day reports.
    public int ArchiveCapacity { get; set; } = 7;
    public DailyReportRecord[] Archive { get; set; }   // fixed length; overwrite oldest
    public int ArchiveHeadIndex { get; set; }

    public CampLifeLedger Ledger { get; set; }         // from Phase 1
}

public sealed class DailyReportRecord
{
    public int DayNumber { get; set; }
    public string[] Lines { get; set; }                // already templated/localized strings
}
```

- **Generation entry point**:
  - Trigger on a deterministic cadence (recommended: daily tick).
  - If you must generate lazily (on menu open), still gate by `LastGeneratedDayNumber`.

- **Candidate selection**:
  - Use a small candidate struct with `severity`, `freshness`, `confidence`, `dedupeKey`.
  - After selecting candidates, template them into final `Lines[]`.

### Phase 3 — Main menu surface (Today’s Report paragraph excerpt)
- Ensure `enlisted_status` displays Today’s Report excerpt as a **single paragraph** via `EnlistedNewsBehavior` formatting.
- If needed, add a short “Recent updates” header under the Daily Summary later; do not expand beyond readability limits.

Implementation checklist (Phase 3)
- **Rendering rule**:
  - Main menu excerpt should be generated from the `DailyReportRecord.Lines` archive entry for “today”.
  - Render as **one paragraph** (join 2–5 short sentences) and cap length.

Suggested formatting rules:
- Join lines with spaces and convert any “category headers” into gentle separators (no bullet lists).
- Cap at a hard limit (example: 350–500 chars) and truncate with `…` if needed.
- If no report exists yet (early init), show a stable fallback line (“Quiet day. Drills and routine.”).

### Phase 3.5 — Reports surface (full Daily Report)
- Add/extend the Reports UI to show the **full Daily Report** (multi-line / categorized), and optionally a small archive.

Implementation checklist (Phase 3.5)
- **Which “Reports” this refers to**:
  - For this spec, “Reports” means the **Camp Bulletin overlay** opened from Camp Menu → “Reports”.
  - Camp Management Reports tab can mirror later, but the Bulletin is the primary “read the report” surface.

- **Minimum UI requirement**:
  - Show today’s full `DailyReportRecord.Lines` as a multi-line view.
  - Optional: show last N days (archive) with a simple day selector.

### Phase 4 — News events feeding the report (facts)
- Add fact producers that populate the snapshot inputs:
  - **Company**: settlement stops, food days band, morale band, threat band, objective tag, attachment context.
  - **Lance**: wounded/sick/dead deltas, training tag (drill/inspection), discipline tag.
  - **Kingdom**: reuse kingdom feed headlines already tracked.
- Keep producers read-only where possible; they should *observe* and *summarize*, not override AI.

Implementation checklist (Phase 4)
- **Use a producer pattern** so adding new facts doesn’t sprawl:

```csharp
public interface IDailyReportFactProducer
{
    void Contribute(DailyReportSnapshot snapshot, CampNewsContext context);
}

public sealed class CampNewsContext
{
    public int DayNumber { get; set; }
    // Live engine objects can exist here (not persisted).
    // e.g. MobileParty current lord party, player status, etc.
}
```

- **Start with 3 producers** (enough to ship):
  - Company movement/objective producer (best-effort).
  - Lance health delta producer (wounded/sick/dead/replacements).
  - Kingdom headline producer (reusing existing feed logic).

### Phase 5 — Decision hooks (RPG/Text events)
- Define a small set of “situation flags” derived from the same snapshot (examples):
  - `company_food_critical`, `company_threat_high`, `lance_fever_spike`, `lance_short_handed`, `battle_imminent`.
- `DecisionEventBehavior` evaluates those flags to:
  - Queue **player-initiated** decisions in `enlisted_decisions`, and/or
  - Push **automatic** inquiry decisions for urgent situations (per `docs/StoryBlocks/decision-events-spec.md`).
- When a decision resolves, it should:
  - Update underlying state (fatigue/morale/health/etc.).
  - Post a short dispatch follow-up (high-signal), and influence next day’s Daily Report.

Implementation checklist (Phase 5)
- **Don’t create decisions in UI code**. Use a small “situation flag” provider that updates evaluator context.

```csharp
public sealed class SituationFlags
{
    public bool CompanyFoodCritical { get; set; }
    public bool CompanyThreatHigh { get; set; }
    public bool LanceShortHanded { get; set; }
    // ... keep it small and stable
}
```

- **Where to set flags**:
  - Prefer: compute from `DailyReportSnapshot` (Phase 4 output) + current live state (for “right now” conditions).
  - Expose into the decision evaluator context in one place (Behavior-level).

- **Outcome logging (required for “Recent Outcomes”)**:
  - Add a small ring buffer of resolved decision outcomes (save-safe strings only).

```csharp
public sealed class DecisionOutcomeLog
{
    public int Capacity { get; set; } = 20;
    public int HeadIndex { get; set; }
    public DecisionOutcomeRecord[] Items { get; set; }
}

public sealed class DecisionOutcomeRecord
{
    public int DayNumber { get; set; }
    public string EventId { get; set; }
    public string OptionId { get; set; }           // or option index if that’s what we have
    public string ResultText { get; set; }         // short, player-facing
}
```

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

### Phase 5.6 — Centralize “dynamic availability” via trigger tokens (avoid scattered logic)
Many of the most fun, dynamic decisions (e.g., “screen a nearby enemy party”) should appear based on real context.

Rule:
- Add new availability conditions as **trigger tokens** (evaluated by the existing token evaluator) rather than one-off UI checks.

Examples (conceptual tokens to add):
- `nearby_enemy_party` / `nearby_enemy_party:range<XX`
- `nearby_enemy_party:can_intercept`
- `company_threat:high` (derived band)

This keeps authoring consistent and avoids fragile duplication across UI/behaviors.

### Phase 6 — Acceptance checks (behavioral)
- Daily Report is generated once/day and stays stable through the day.
- Daily Report includes grounded Lance/Company/Kingdom+Rumor content.
- No dispatch spam: repeated checks update one story via dedupe keys; only threshold crossings emit new high-signal items.
- Decisions are driven by situation flags; UI code does not create decisions.


