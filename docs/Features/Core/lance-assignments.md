# Lance Assignments – Technical Plan

## Index
- [Overview](#overview)
- [Purpose](#purpose)
- [Inputs/Outputs](#inputsoutputs)
- [How It Works (Planned)](#how-it-works-planned)
- [Data & Config (JSON + localization)](#data--config-json--localization)
- [Notes on Formation Grouping](#notes-on-formation-grouping)
- [Story Integration (registry, events)](#story-integration-registry-events)
- [Persistence](#persistence)
- [UI Integration](#ui-integration)
- [Safety & Mod Compatibility](#safety--mod-compatibility)
- [Edge Cases](#edge-cases)
- [Acceptance Criteria](#acceptance-criteria)
- [Phased Plan](#phased-plan)
- [References (1.3.4)](#references-134)
- [Historical Lance Notes](#historical-lance-notes)
- [Rank Reference (Tier 1–9)](#rank-reference-tier-1-9)
- [Retirement Considerations](#retirement-considerations)
- [Formation Labels by Style (10/20/30)](#formation-labels-by-style-102030)

## Overview
Replace the “all Levies” starter flow with style-flavored Lances. On enlist (Tier 1), the player receives a provisional Lance. On Tier 2 promotion, the player formally selects a Lance in need of recruits from a curated list per style. Styles are generic archetypes (legion/feudal/tribal/horde/mercenary) mapped from cultures via JSON, with an in-game fallback menu for unknown cultures. Lances remain contained in Enlisted systems (no native menu/time changes) and feed future story/camp activity hooks.

## Purpose
- Give enlisted soldiers a recognizable sub-unit identity from day one.
- Provide hooks for story organizer and camp activities.
- Keep the system isolated to Enlisted (no Harmony patches on native menus/time control).
- Make Lances first-class, addressable IDs for future story/camp event integration.

## Inputs/Outputs
- Inputs: enlistment state (tier, culture), lance catalog (per culture/style), random/weighted selection, config toggles.
- Outputs: stored lance id/name per player; display in `enlisted_status`; story-track id for future use; optional camp modifiers.

## How It Works (Planned)
1) Enlist (Tier 1):
   - Assign a provisional Lance immediately based on lord culture (fallback to neutral pool).
   - Store (id/name/style/storyId) on EnlistmentBehavior; serialize via SyncData.
   - Show in enlisted_status menu as “Lance (Provisional): {Name}”.
2) Tier 2 promotion:
   - Prompt “Select a Lance in need of recruits” (3–5 options filtered by culture/style).
   - Player chooses → Lance becomes final; stored and shown as “Lance: {Name}”.
   - Log selection; optional small relation/XP flavor (configurable later).
3) Story organizer hook:
   - Each Lance maps to a `storyId` for later narrative/camp events.
   - No runtime behavior yet; just stored mapping for future triggers.
4) Camp/Fatigue tie-in (future):
   - Camp activities may read lance tags (style/role) for flavor text or minor bonuses.
   - Fatigue system unchanged; Lances only gate/skin camp options if desired.
5) UI:
   - Add a line to `enlisted_status`: `Lance: {NAME}` (or `Lance (Provisional): {NAME}` at T1).
   - Hidden when not enlisted.

- ## Data & Config (JSON + localization)
- New JSON (proposed): `ModuleData/Enlisted/lances_config.json`
  - `style_definitions`: array of styles (archetypes), each with:
    - `id` (e.g., style_legion, style_feudal, style_tribal, style_horde, style_mercenary)
    - `lances`: list of lance entries `{ id, name, roleHint (infantry/ranged/cavalry/horse_archer), storyId }`
    - Formation rule: per style, supply two Lances per supported duty (Infantry, Ranged, Cavalry; Horse Archer if applicable). If a duty isn’t supported, we fall back at selection time.
  - `culture_map`: maps culture IDs to style IDs (supports modded cultures). Example:
    - `empire`, `rome_reborn`, `calradia_empire` -> `style_legion`
    - `vlandia`, `culture_westerlands` -> `style_feudal`
    - `sturgia`, `battania`, `wildling` -> `style_tribal`
    - `khuzait`, `dothraki` -> `style_horde`
    - Unknown cultures not in map trigger the in-game “choose a tradition” fallback menu.
  - Default style catalogs (seed list; still two per duty where applicable):
    - Legion Style (numbers/cohorts): The 4th Cohort, The Iron Column, The Porphyry Guard, The Emperor’s Eyes, The Immortals, The Eagle’s Talons
    - Feudal Style (heraldry/colors): The Red Chevron, The Broken Lances, The White Gauntlets, The Black Pennant, The Iron Spur, The Gilded Lilies
    - Tribal Style (animals/nature): The Wolf-Skins, The Frost-Eaters, The Stag’s Blood, The Shield-Biters, The Bear’s Paw, The Raven Feeders
    - Horde Style (speed/sky): The Wind-Riders, The Black Sky, The Lightning Hooves (add 3 more as needed per duty)
    - Mercenary/Universal Style (grim slang): The Mud-Eaters, The Second Sons, The Vanguard (extend to two-per-duty coverage)
- Config toggles (add to enlisted_config.json or separate):
  - `lances_enabled` (bool, default false until shipped)
  - `lance_selection_count` (int, e.g., 3–5)
  - `use_culture_weighting` (bool)
- Localization:
  - Names/story text use string IDs in `enlisted_strings.xml`; fall back to raw `name` if missing.
  - Keep lance names and story strings in XML to support translations and mods.
  - Hybrid persistence compatibility: keep names stable in XML; if ids are removed, UI falls back to saved name (see Persistence).
- Rank strings for UI/story should live in XML; tiers 1–9 titles per style are narrative only and should not change gear/formation rules.

## Notes on Formation Grouping
- Enlistment menu can filter Lance options by the player’s duty/formation (infantry, ranged, cavalry, horse archer).
- For cultures lacking a given formation (e.g., horse archers), fall back to the nearest role and log once (Info).
- Style-first: culture only maps to a style; selection pulls from the style’s pool. If culture is unknown, present an in-game “choose tradition” prompt to pick style (legion/feudal/tribal/horde/mercenary).

## Story Integration (registry, events)
- Lance registry: expose lookup by `id`, culture, and `roleHint`; load from lances_config.
- Each Lance carries a `storyId`; store on selection for downstream systems.
- Event surface (planned): raise `OnLanceSelected(lanceId, storyId)` when finalized at Tier 2.
- Story/camp systems can subscribe and pull `currentLanceId/storyId` from EnlistmentBehavior getters.
- Keep triggers opt-in and contained; no native menu/time patches.
- Expose current tier and rank title (from XML) to story systems so events can branch on tier/rank/lance.

## Persistence
- Hybrid persistence to survive config/name changes:
  - Store: `currentLanceId` (primary), `currentLanceName` (fallback display), `currentLanceStyle`, `currentLanceStoryId`, `isLanceProvisional`.
  - Load: try to resolve `currentLanceId` from lances_config; if found, use latest name/storyId. If missing, use saved `currentLanceName` and mark as legacy/disbanded for display (no crash).
  - Promotion flow uses stored provisional lance to pre-fill recommendation if present.
- Sync via SyncData; clear on discharge; restore on grace resume.

## UI Integration
- Extend `enlisted_status` text builder to include the lance line.
- Tier 2 promotion hook: inject a menu step to choose a Lance (reuse promotion UI/patterns).
- Respect existing menu safety checks (no battle/siege/encounter activation).

## Safety & Mod Compatibility
- No Harmony patches on native menus/time control.
- All logic contained in Enlisted behaviors and menus.
- If config missing, use neutral fallback list; log once (Info).
- If culture not matched, fall back to neutral pool.
- No visibility/IsActive changes; no party AI changes.

## Edge Cases
- Re-enlist after discharge: assign new provisional lance; selection redone at Tier 2.
- Grace period resume: restore stored lance.
- Prisoner/leave: lance persists, display continues.
- Missing config: neutral pool, throttled log.
- Tier < 2: provisional only, no menu prompt yet.
- Gear at Tier 7+: Player may retain Tier 6 issued kit but regains personal inventory and may wear any gear they choose.

## Retirement Considerations
- Preserve max achieved tier on discharge/retirement so cross-kingdom enlistment credit (Tier 7/8/9) remains available when re-enlisting elsewhere.
- On discharge, restore personal inventory and clear issued-gear enforcement (aligns with Tier 7+ gear freedom).
- Lance identity remains narrative-only; retirement payouts/retinue handoff unchanged.
- When re-enlisting after retirement, enforce the relations/criminality gate for higher-tier entry; block enlistment if relations < 0 or criminal status present.

## Formation Labels by Style (10/20/30)
- **Legion (Empire):** 10 Contubernium (Decanus), 20 Vexillation, 30 Turma
- **Feudal (Vlandia):** 10 Lance, 20 Retinue, 30 Banner (Conroi)
- **Tribal (Battania/Sturgia):** 10 Knot, 20 Crew, 30 Hearth (Hearth-Troop)
- **Horde (Khuzait):** 10 Arban, 20 Patrol, 30 Vanguard
- **Mercenary (Neutral):** 10 Detail, 20 Outfit, 30 Free Company

## Acceptance Criteria
- On enlist: a provisional lance is assigned and displayed in enlisted_status.
- On Tier 2 promotion: player is prompted to pick from culture-appropriate Lances; choice is stored and displayed.
- Lance data survives save/load, discharge/re-enlist logic, and grace resumption.
- No native menu/time interference; safe alongside other mods.

## Phased Plan (execution order)
- Phase 0: Add doc & config schema; add config loader and fallback; keep feature off by default.
- Phase 1: Data plumbing—EnlistmentBehavior fields + SyncData; provisional assignment on enlist; display in enlisted_status; feature still gated by config.
- Phase 2: Tier 2 selection prompt; style/role-filtered options; finalize lance; update display.
- Phase 3: Style-first unknown-culture prompt: if culture not mapped, present in-game “How does this army fight?” menu with style choices (Legion/Feudal/Tribal/Horde/Mercenary), then proceed with selection. Universal/Mercenary remains a fallback if the player cancels.
- Phase 4: Story organizer hook (store `storyId`; raise `OnLanceSelected` event; no runtime triggers yet); light logs.
- Phase 5: End-user diagnostics (lightweight, throttled): one-line state dump to Session log on enlist/promotion/fallback and a non-intrusive menu hint; no spam.
- Phase 6: Optional camp/fatigue flavor hooks (no balance changes to core systems).
- Story/event consumption: ensure rank title and tier are available (from XML + current tier) to narrative systems alongside lance id/storyId.

## Rank Reference (Tier 1–9)
Style-specific titles; Tier 7/8/9 align to command sizes ~10/20/30. Use these for UI and narrative; gear/formation logic stays unchanged.

- **Feudal (Vlandian/Western)**
  - T1 Peasant (none), T2 Levy (none), T3 Footman (none), T4 Man-at-Arms (none), T5 Sergeant (passive aura), T6 Knight Bachelor (passive aura), T7 Dizener (10), T8 Vintener (20), T9 Lieutenant (30)
- **Legion (Empire)**
  - T1 Tiro, T2 Miles, T3 Immunes, T4 Principalis, T5 Evocatus (passive aura), T6 Aquilifer (passive aura), T7 Decanus (10), T8 Optio (20), T9 Decurion (30)
- **Tribal (Sturgia/Battania)**
  - T1 Thrall, T2 Ceorl, T3 Fyrdman, T4 Drengr, T5 Huskarl (passive aura), T6 Varangian (passive aura), T7 Hase (10), T8 Styriman (20), T9 Hersir (30)
- **Horde (Khuzait)**
  - T1 Outsider, T2 Nomad, T3 Noker, T4 Warrior, T5 Veteran (passive aura), T6 Bahadur (passive aura), T7 Arban (10), T8 Keshig (20), T9 Cherbi (30)
- **Mercenary/Universal**
  - T1 Follower, T2 Recruit, T3 Free Sword, T4 Veteran, T5 Blade (passive aura), T6 Chosen (passive aura), T7 Corporal (10), T8 Sergeant (20), T9 Ensign (30)

## References (1.3.4)
- `CampaignEvents.OnSessionLaunchedEvent` for menu init
- `CampaignEvents.HourlyTickEvent` / `DailyTickEvent` if needed for logging only
- `GameMenu.ActivateGameMenu("enlisted_status")` (Enlisted menu only)
- Enlistment state: `EnlistmentBehavior.Instance`
- Native constructs available (no native Lance system exists):
  - Culture ID: `Hero.MapFaction?.Culture?.StringId` (from `CultureObject`)
  - Menus/UI: `CampaignGameStarter`, `GameMenu`, `InformationManager`/`InquiryData`, `MBTextManager.SetTextVariable`
  - Persistence: `CampaignBehaviorBase.SyncData(IDataStore)` for custom fields
  - No native Lance/style/culture-map or unknown-culture prompts—must be implemented in Enlisted only

## Historical Lance Notes
- Concept: A Lance (lance fournie) was a combined-arms micro-team (4–10 men), not uniform troops. Typical Burgundian model:
  - Man-at-Arms (boss): heavy cavalry.
  - Coustillier: light cav/infantry bodyguard.
  - 2–3 Archers: mounted infantry who dismount to fight.
  - Page/Valet: non-combatant servant.
- March vs. battle:
  - On the march: move together.
  - In battle: often split—knight to heavy cav line, archers dismount to archer line, pages stay with baggage. In raids/small wars, they might fight as a mixed squad.
- How we adapt (design guidance):
  - Narrative/camp flavor uses the combined-arms identity (stories, duties, camp events).
  - Tactical implementation stays within Bannerlord/Enlisted constraints:
    - Maintain formation-based deployment (infantry/ranged/cavalry/horse archer) so battle integrity and compatibility remain intact.
    - Use Lance identity for: UI display, story hooks, duty/camp tasks, small raid encounters (future), and equipment/role flavor.
    - For raids/micro-missions, we can optionally group the player’s companions/retinue under the Lance label without altering native formations; keep it contained to Enlisted behaviors.
  - No native battle formation overrides; avoid conflicts with other mods and preserve current formation-assignment system.


## Lightweight Diagnostics (non-spam, always on)
- Throttled Info logs only; minimal, single-line entries.
- Log points: provisional assignment, Tier 2 selection, fallback role/culture, missing localization.
- Single-line state dumps to Session log on:
  - Enlist (provisional lance assigned)
  - Tier 2 finalize (lance id/name, culture, roleHint)
  - Fallback used (role/culture)
- Menu hint: brief status line in enlisted_status when debugging context is relevant; no time control/menu overrides.

