# Feature Spec: Camp Life Simulation

This spec proposes an **internal, mod-friendly** “camp life” simulation layer that reacts to the campaign and makes enlisted service feel like a lived-in army: shortages, bad news, delayed pay, corruption, and small camp incidents.

## Status (shipping)
- The daily **Camp Conditions Snapshot** is implemented and persisted (**enabled by default**; can be disabled via config).
- It currently drives:
  - Quartermaster mood/pricing flavor (stable per day)
  - Pay Muster text/options when pay tension is high (IOU/promissory path)
- It is surfaced to the player via the **enlisted status header** (camp status line + days from town).
- Research-only expansions (deep world scanning, full incident library) remain out of scope.

## Index
- [Overview](#overview)
- [Purpose](#purpose)
- [Design goals](#design-goals)
- [Mod-friendly constraints (how we keep this internal)](#mod-friendly-constraints-how-we-keep-this-internal)
- [Inputs / Outputs](#inputs--outputs)
- [Core model: Camp Conditions Snapshot](#core-model-camp-conditions-snapshot)
- [System integrations (what it drives)](#system-integrations-what-it-drives)
  - [Pay Muster: delayed pay and promissory notes](#pay-muster-delayed-pay-and-promissory-notes)
  - [Quartermaster: mood, prices, and stockouts](#quartermaster-mood-prices-and-stockouts)
  - [Duties & camp actions: foraging, errands, contraband](#duties--camp-actions-foraging-errands-contraband)
  - [Incidents: camp events and “small stories”](#incidents-camp-events-and-small-stories)
- [Phased plan](#phased-plan)
- [Edge Cases](#edge-cases)
- [Acceptance Criteria](#acceptance-criteria)
- [Technical implementation (high-level)](#technical-implementation-high-level)

## Overview
While enlisted, maintain a small set of **camp condition meters** (logistics, morale shock, territory pressure, pay tension, contraband heat). Update them mostly **once per day** and use them to:
- Vary Quartermaster mood/prices/availability.
- Add Pay Muster outcomes like **delayed pay** and **promissory notes**.
- Encourage “soldier survival” loops (forage when supply wagons are dry, smuggle drink when camp needs morale).

## Purpose
- Make enlisted play feel like you’re living in an army, not just clicking menus.
- Create story hooks that still respect Bannerlord’s systems (raids, sieges, settlement loss).
- Keep changes **contained** inside Enlisted so we don’t destabilize vanilla finance, trading, or AI.

## Design goals
- **Believable, not random**: mood and shortages should follow recognizable causes (recent losses, long time away from towns, villages raided).
- **Low overhead**: event-driven + daily aggregation; avoid scanning the world every tick.
- **Player agency**: shortages should create choices (forage, bribe, smuggle, take IOUs), not hard fails.
- **Non-invasive**: avoid Harmony patches unless absolutely required; prefer `CampaignEvents` + existing Enlisted menus/incidents.

## Mod-friendly constraints (how we keep this internal)
This feature should be implemented as a new Enlisted behavior that:
- **Only runs while enlisted**.
- Subscribes to **native campaign events** and keeps minimal state.
- Exposes a single “snapshot” API to Enlisted systems (Quartermaster, Pay Muster, incidents).
- Does **not** patch or replace:
  - `DefaultClanFinanceModel` (your pay system already avoids this).
  - The global item economy or town markets.
  - AI decision making.

In other words: we simulate “camp life” by changing **Enlisted-owned menus/outcomes**, not by rewriting Bannerlord.

## Localization (required)
All Camp Life player-facing text must be translatable:
- Use `{=string_id}` **TextObject** strings for:
  - incidents and inquiry prompts (titles/body/option text/hints)
  - menu headers and status lines
  - notifications/messages shown to the player
- Store translations in `ModuleData/Languages/enlisted_strings.xml`.
- Allow placeholders (e.g., `{PLAYER_NAME}`, `{LORD_NAME}`, `{SETTLEMENT}`) and resolve them at runtime.
- Raw text may exist only as a **fallback English** to prevent crashes if a string ID is missing.

## Inputs / Outputs

### Inputs

#### Native events (Bannerlord)
These exist in Bannerlord’s API (confirmed from decompiled `CampaignEvents`):
- `CampaignEvents.VillageBeingRaided`
- `CampaignEvents.VillageLooted`
- `CampaignEvents.VillageStateChanged`
- `CampaignEvents.OnSettlementOwnerChangedEvent`
- `CampaignEvents.OnSiegeEventStartedEvent`, `CampaignEvents.OnSiegeEventEndedEvent`
- `CampaignEvents.MapEventEnded` (battle completion)
- `CampaignEvents.SettlementEntered`, `CampaignEvents.OnSettlementLeftEvent` (town/castle/village visits)

Current implementation note:
- The shipping behavior stays event-driven + daily aggregation. We intentionally subscribe to a minimal subset (plus `DailyTickEvent`) and use clamps/decay rather than heavy scans.

#### Enlisted-local state
- Current enlisted lord / army context (already tracked by `EnlistmentBehavior`).
- Pay ledger state (pending pay, next payday, last outcome) from the existing Pay System.
- Party condition signals (morale, food) from `MobileParty.MainParty`.
- Retinue casualty tracking (already exists as a behavior in the mod).

### Outputs
- `CampConditionsSnapshot` (updated daily; see below)
- Derived flags for feature gating:
  - `IsSupplyTight` / `IsFoodOut`
  - `IsPayDisrupted`
  - `IsContrabandRiskHigh`
  - `QuartermasterMoodTier`
- Optional: a short `CampStatusLine` (1–3 lines) for UI flavor.

## Core model: Camp Conditions Snapshot
Define a small snapshot object, stored and saved with the campaign:
- **LogisticsStrain (0–100)**: supply line stretch; worsens when away from settlements, during sieges, after villages are looted, and when food is low.
- **MoraleShock (0–100)**: trauma/low spirits after recent battles/losses; decays over time.
- **TerritoryPressure (0–100)**: “we’re losing ground / hostile territory”; increases on nearby settlement loss, frequent raids, or siege failures.
- **PayTension (0–100)**: payroll disruption; increases when key fiefs are lost/raided and when payday is due but cash is tight.
- **ContrabandHeat (0–100)**: crackdowns and risk; rises after smuggling/bribes, falls with time or “laying low”.

### Local vs lord-holdings signals
Support two “signal scopes”:
- **Local-to-army** (default): events within a radius of the army’s current position matter most.
- **Lord-holdings** (optional): if the enlisted lord owns specific settlements/villages, losses/raids of those holdings directly spike `PayTension`.

This keeps the system believable without requiring constant world scans.

## System integrations (what it drives)

### Pay Muster: delayed pay and promissory notes
When `PayTension` is high (examples: key fief lost, villages looted, recent siege disaster):
- Add/replace Pay Muster options:
  - **Standard Pay** (may be reduced/partial, or unavailable in extreme cases)
  - **Promissory Note (IOU)**: records owed pay and pays later when conditions improve
  - **Demand a Recount** (existing corruption challenge): becomes more tempting but riskier
  - **Side Deal** (existing): can be reframed as “gear/coin under the table”

Design details:
- Keep “IOU” **internal**: store `PendingPromissoryPay` as a number in the muster ledger, shown in text.
- When conditions stabilize (pay tension drops or town access resumes), resolve IOUs at muster.

Shipping note:
- The Pay Muster surface remains inquiry-based; Camp Life only changes Enlisted-owned text/options and ledger fields.

### Quartermaster: mood, prices, and stockouts
Quartermaster becomes a camp-life barometer.

#### Mood
Derive `QuartermasterMoodTier` from the snapshot (mostly logistics + morale + pay tension):
- Fine / Tense / Sour / Predatory

Mood should be **stable for the day** (not rerolled every menu open).

#### Prices
Apply small multipliers (example tuning):
- Fine: purchase ×0.98, buyback ×1.00
- Tense: purchase ×1.00, buyback ×0.95
- Sour: purchase ×1.07, buyback ×0.85
- Predatory: purchase ×1.15, buyback ×0.75

#### Stockouts / gating
When `LogisticsStrain` is high:
- Quartermaster stops offering certain supply actions (e.g., “no food in the wagons”).
- Push the player into **foraging** or **town resupply** loops.

Important: stockouts should be framed as “camp supply reality” and backed by alternative actions (forage duty, rationing choices), not a hard soft-lock.

Shipping note:
- Current integrations are intentionally “minimum but real”: mood/pricing + localized intro dialogue, driven by the daily snapshot.

### Duties & camp actions: foraging, errands, contraband
Use the snapshot to make duties feel necessary:
- High `LogisticsStrain` increases the value of **forager** work (more food, better outcomes).
- High `MoraleShock` makes **revelry/comfort** actions tempting but costly.
- High `ContrabandHeat` increases risk of smuggling and increases consequences.

Examples of actions:
- **Forage**: convert time/fatigue into food/supplies when wagons are empty.
- **Smuggle drink**: “beer into revelry” event chain (Roguery/Charm gated), affects morale/fatigue.
- **Bribe the clerk**: reduces Quartermaster predatory pricing for the day, but raises contraband heat.

### Incidents: camp events and “small stories”
Add small incidents that fire rarely and only when conditions support them:
- “After the raid, the camp is hungry.”
- “Paymaster says the strongbox never arrived.”
- “Quartermaster offers an under-the-table bundle.”
- “The lads want drink after the last slaughter.”

Prefer existing incident UI patterns (inquiries/menus) over mission scenes to keep this mod-friendly.

### Lance Life (story packs)
Camp conditions should also feed **lance-specific stories** (drills, scrounging, contraband, theft chains) as a separate, modular layer.
- Doc: **[Lance Life](lance-life.md)**

## Phased plan

### Phase 0 — Documentation + scaffolding (safe)
- Add this spec.
- Add a new behavior shell: `CampLifeBehavior` (or similar) that can store a daily snapshot and log it.
- Shipping note: Camp Life is enabled by default in `ModuleData/Enlisted/enlisted_config.json`, and can be disabled safely via `camp_life.enabled`.

### Phase 1 — Data collection (event-driven; no gameplay changes)
- Subscribe to the relevant `CampaignEvents` and record “recent history”:
  - Last town visit time
  - Recent raids/loots near the army
  - Recent settlement owner changes (local radius or lord-holdings)
  - Recent sieges / battles
- Compute the snapshot daily and expose it to other Enlisted systems.

### Phase 2 — Quartermaster mood + price modifiers (small, contained)
- Drive `qm_intro_dialogue` (or new localized strings) from the daily mood.
- Apply purchase/buyback multipliers in Quartermaster price calculations.
- Add very light stockouts (start with food/supplies gating only).

### Phase 3 — Pay Muster: delayed pay + promissory notes (story hook)
- Add IOU tracking to the muster ledger and new Pay Muster options.
- Conditions-based text changes (short, grounded).
- Ensure resolution paths exist (IOUs eventually pay out when conditions ease).

Status: implemented baseline (enabled by default; can be disabled via config).

### Phase 4 — Incidents + contraband loops
- Add camp incidents tied to snapshot triggers.
- Add “smuggle beer” and “bribe the clerk” options with risks and consequences.
- Add small positive relief valves (successful foraging reduces strain; morale events reduce shock).

### Phase 5 — Polish + tuning
- Expose weights/thresholds in `enlisted_config.json`.
- Add short “camp report” lines in the Camp / status UI.
- Logging: one-session diagnostics in `Modules/Enlisted/Debugging` for balancing (optional).

## Edge Cases
- **Save/load**: snapshot and recent-history counters must not double-count on load.
- **Army teleport / naval transitions**: use simple radius checks and clamps; avoid fragile distance math.
- **Player not enlisted**: do nothing.
- **No current lord / weird states**: fall back to local-to-party signals only.
- **Avoid soft-lock**: if Quartermaster is “out of food”, always provide at least one alternate path (forage duty, town access when available, or a rationing choice).

## Acceptance Criteria
- When a nearby village is looted/raided, `LogisticsStrain` increases within 1 day and Quartermaster flavor reflects it.
- After heavy battle cadence, `MoraleShock` increases and decays over time (visible in at least one UI line).
- When a key settlement changes owner (local radius or lord-holdings), Pay Muster can produce **delayed pay** outcomes (IOU path).
- Quartermaster mood/pricing remains **stable for the day**, not per-visit RNG.
- All mechanics remain contained inside Enlisted menus/incidents; no global finance or market systems are rewritten.

## Technical implementation (high-level)
- New behavior: `CampLifeBehavior` (or similar) registered on session launch.
- Event subscriptions via `CampaignEvents.*.AddNonSerializedListener`.
- Storage:
  - Small “recent history” fields (timestamps, counters, last-known ownership changes).
  - Daily `CampConditionsSnapshot` saved with `SyncData`.
- Consumers:
  - Quartermaster: price multipliers + localized mood text.
  - Pay muster: additional options (IOU) and outcomes.
  - Incidents: condition-driven small events.

## Configuration (shipping)
Camp Life is gated by config:
- `camp_life.enabled` in `ModuleData/Enlisted/enlisted_config.json`


