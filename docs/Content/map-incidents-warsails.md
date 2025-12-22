# Native Map Incidents (War Sails / 1.3.x)

**Summary:** Deep dive reference for Bannerlord's native Map Incidents system (map-level popup choices) from War Sails / NavalDLC era. Documents native implementation patterns, triggers, and structure for modders creating custom map incidents.

**Status:** 📚 Reference  
**Last Updated:** 2025-12-22  
**Related Docs:** [Event Catalog](event-catalog-by-system.md), [Content System Architecture](../Features/Content/content-system-architecture.md)

> **Note**: Written against local decompile in `C:\Dev\Enlisted\Decompile\` (see `docs/blueprint.md` → Native Reference).

---

## Overview

“Map Incidents” are **one-off incident popups** that appear during campaign map play (entering/leaving settlements, leaving battles/encounters, waiting, during sieges).

They are **not** the same thing as:
- “Issues” (notables’ quests)
- menu-driven decisions (town/castle/village menu options)
- conversation dialogs

Incidents are:
- **registered up-front** as `Incident` objects (by `IncidentsCampaignBehaviour`)
- **triggered probabilistically** based on player context and cooldowns
- **presented as a Gauntlet UI overlay** with selectable options
- resolved immediately, applying one or more `IncidentEffect`s

## Purpose

For Enlisted modding work, we care about:
- **when/why incidents trigger** (and when they are blocked, especially on sea travel)
- **the exact flow** from campaign tick → map screen → UI → resolution
- **how War Sails changes the system’s behavior** (naval exclusions + NavalDLC model override)

## Inputs / Outputs

### Inputs (what the native system reads)

- **Campaign events** (hourly tick, menu open, menu option selected, battle/encounter ended, etc.)
- **Player state**:
  - `Hero.MainHero.IsPrisoner`
  - whether a conversation is active
  - party/settlement context (current settlement, last visited settlement)
  - siege state
  - naval state (`MobileParty.MainParty.IsCurrentlyAtSea`, naval map events)
- **Game model tuning**:
  - `IncidentModel` (global chance + siege/wait chance + global cooldown window)
- **Per-incident conditions** (each incident has a condition; each option and effect can have their own conditions)
- **Cooldowns**:
  - a global cooldown (`_lastGlobalIncidentCooldown`)
  - per-incident cooldowns (`_incidentsOnCooldown`)

### Outputs (what the native system does)

- When triggered, the behavior assigns `MapState.NextIncident`.
- On the next campaign tick, `Campaign.Tick()` starts the incident via `MapState.StartIncident(incident)`.
- UI is shown (Gauntlet overlay); time is paused and locked.
- On selection + Done, the incident’s option effects are applied and quick-info messages are emitted.
- `CampaignEventDispatcher.Instance.OnIncidentResolved(incident)` fires.

## Behavior (end-to-end flow)

### 1) Registration

Incidents are registered once by `IncidentsCampaignBehaviour.InitializeIncidents()`.

Each `Incident` has:
- **ID** (`MBObjectBase(id)`)
- **Title** and **Description** (`TextObject`)
- **Trigger flags** (`IncidentTrigger`)
- **Type** (`IncidentType`) — drives UI styling/sounds and theme grouping
- **Cooldown** (`CampaignTime`)
- A **condition** delegate that determines if the incident is eligible
- A list of **options**; each option has text + effects (and optional condition/consequence delegates)

### 2) Trigger selection

`IncidentsCampaignBehaviour` listens to multiple campaign events and calls `TryInvokeIncident(triggerFlags)` when appropriate.

Key constraints before selecting an incident:
- settlement “busy” checks (enter/leave)
- not prisoner
- not in conversation
- global cooldown must be past
- per-incident cooldown entry must not exist

If eligible, it collects all incidents where:
- `(incident.Trigger & triggerFlags) != 0`
- `incident.CanIncidentBeInvoked()` returns true (incident + option + effect conditions)

Then it picks a random eligible incident and sets `MapState.NextIncident`.

### 3) MapState → MapScreen → UI

When `MapState.NextIncident != null`, `Campaign.Tick()` calls:
- `MapState.StartIncident(nextIncident)`
- then clears `NextIncident` regardless

Map screen shows the incident by:
- `MapScreen` (as `IMapStateHandler`) receiving `OnIncidentStarted(Incident incident)`
- adding a `MapIncidentView`, which is overridden by `GauntletMapIncidentView`

`GauntletMapIncidentView`:
- pauses campaign time (`CampaignTimeControlMode.Stop`) and **locks** time control
- pauses the engine
- creates `MapIncidentVM` (options + hints)
- loads `MapIncident` movie from `ui_map_incidents`
- on Confirm hotkey: invokes selected option and closes

### 4) Resolution

On “Done”, `Incident.InvokeOption(index)`:
- runs each `IncidentEffect.Consequence()` (subject to per-effect random chance)
- runs the option’s consequence delegate (if any)
- fires `OnIncidentResolved(incident)` (campaign event)

`IncidentsCampaignBehaviour.OnIncidentResolved`:
- puts the incident on cooldown
- applies global cooldown (`Now + random(min,max)`)

## War Sails specifics (NavalDLC + sea travel)

War Sails introduces a bunch of campaign-level naval concepts; Map Incidents adapt in three key ways:

### 1) NavalDLC can disable incidents during its storyline

`NavalDLCIncidentModel` overrides `IncidentModel` probabilities and returns **0** while the naval storyline is active:
- global probability
- siege probability
- wait probability

Result: during the Naval DLC storyline, **no incidents will fire** even if everything else would have allowed them.

### 2) Battle-triggered incidents skip naval map events

The battle-end trigger (`LeavingBattle`) has an explicit check that bails out for naval map events:
- `evt.IsNavalMapEvent` → no incident

Result: you don’t get “post-battle” incidents from naval battles.

### 3) Many incidents explicitly disallow being at sea

Multiple incidents use conditions like:
- `!MobileParty.MainParty.IsCurrentlyAtSea`

This is independent of the DLC storyline gating above.

Result: even outside the storyline, **sea travel reduces incident variety**, because many incidents self-filter out.

## Edge cases / gotchas

- **No partial “incident queue”**: `Campaign.Tick()` always clears `MapState.NextIncident` after it tries to start it (even if `CanIncidentBeInvoked()` fails on that tick).
- **Per-option & per-effect conditions matter**: `Incident.CanIncidentBeInvoked()` requires:
  - the incident condition passes
  - all option conditions (if present) pass
  - all effect conditions pass
  If any fail, the incident is not eligible for selection at all.
- **Random chance is effect-level**: an option can show hints for effects that are probabilistic; effects may not fire even after selection.
- **Cooldowns are layered**:
  - global cooldown (random between min/max, model-driven)
  - per-incident cooldown (defined per incident)

## Acceptance criteria (for this doc)

- Explains the native class graph and event flow (registration → trigger → UI → resolution).
- Captures War Sails’ naval-specific behavior changes (naval storyline gating, naval battle exclusion, at-sea condition filtering).
- Provides a reproducible way to enumerate all native incidents from the local decompile.

## Appendix: Extracting the full incident catalog

This repo includes a small extractor (kept in project tools):

- Script: `tools/extract_native_map_incidents.py` (if available)
- Source file: `Decompile/TaleWorlds.CampaignSystem/.../IncidentsCampaignBehaviour.cs`

Example:

```powershell
python tools/extract_native_map_incidents.py `
  --input "C:\Dev\Enlisted\Decompile\TaleWorlds.CampaignSystem\TaleWorlds\CampaignSystem\CampaignBehaviors\IncidentsCampaignBehaviour.cs" `
  --output "C:\Dev\Enlisted\Enlisted\Debugging\native-map-incidents.json" `
  --markdown-output "C:\Dev\Enlisted\Enlisted\Debugging\native-map-incidents-table.md"
```

The JSON output contains one entry per incident (id/title/description/trigger/type).


