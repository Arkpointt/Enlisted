# Lance Assignments — Documentation

## Index
- [Overview](#overview)
- [Data & Config](#data--config)
- [Cultural Archetypes & Culture Map](#cultural-archetypes--culture-map)
- [Runtime Behavior](#runtime-behavior)
- [UI](#ui)
- [Events & API Surfaces](#events--api-surfaces)
- [Diagnostics](#diagnostics)
- [Safety & Compatibility](#safety--compatibility)
- [References (API verification)](#references-api-verification)
- [Missing / To-Do](#missing--to-do)

## Overview
Lances give enlisted players a sub-unit identity from enlistment through promotion. Tier 1 assigns a provisional lance; Tier 2 finalizes it from culture/style-filtered options. Everything runs inside Enlisted (no native menu/time patches) and exposes IDs for story and future camp flavor.

## Data & Config
- Gate: `lances_enabled` in `ModuleData/Enlisted/enlisted_config.json`.
- Catalog: `ModuleData/Enlisted/lances_config.json`
  - `style_definitions`: styles with `id`, `lances[]` of `{ id, name, roleHint (infantry/ranged/cavalry/horsearcher), storyId }`.
  - `culture_map`: culture StringId -> styleId; unknown cultures trigger an in-game style prompt.
- Selection count: `lance_selection_count` (default 3–5).
- Weighting toggle: `use_culture_weighting` (reserved; current implementation shuffles without weights).

## Cultural Archetypes & Culture Map
- Styles (examples from defaults):
  - Legion: The 4th Cohort, The Iron Column, The Emperor’s Eyes, The Immortals, The Eagle’s Talons.
  - Feudal: The Red Chevron, The Broken Lances, The White Gauntlets, The Black Pennant, The Iron Spur, The Gilded Lilies.
  - Tribal: The Wolf-Skins, The Frost-Eaters, The Stag’s Blood, The Shield-Biters, The Bear’s Paw, The Raven Feeders.
  - Horde: The Wind-Riders, The Black Sky, The Lightning Hooves, The Cloud Lancers, The Steppe Sentinels.
  - Mercenary/Universal: The Mud-Eaters, The Second Sons, The Vanguard, The Free Arrows, The Coin Guard, The Steel Company.
- Culture mapping (defaults): empire/rome_reborn/calradia_empire -> legion; vlandia/culture_westerlands -> feudal; sturgia/battania/wildling -> tribal; khuzait/dothraki -> horde; aserai -> mercenary; unknown -> prompt for style.

## Runtime Behavior
- Enlist (Tier 1):
  - Provisional lance assigned via culture_map; fallback to mercenary/first available.
  - Unknown culture: prompt for style, then assign provisional.
  - Stored: id, name, style, storyId, provisional flag; serialized.
  - UI: `Lance (Provisional): {Name}`.
- Tier 2 promotion:
  - Inquiry with 3–5 options filtered by culture/style/formation; finalize lance.
  - Clears provisional; logs once; raises event (see Events).
  - UI: `Lance: {Name}` (or Legacy/Provisional variants).
- Legacy handling:
  - On load, if saved id is missing, keep saved name, mark legacy for UI, avoid crashes.
  - If found, refresh to latest name/style/storyId.
- Grace/resume:
  - Provisional/final state (including legacy/provisional/manual-style) survives grace saves.

## UI
- `enlisted_status` shows:
  - Lance line: Provisional, Final, or Legacy; hidden when none.
  - Fatigue line (Current/Max) as part of enlisted status.
  - Hint to finalize when provisional.

## Events & API Surfaces
- Finalization event: `EnlistmentBehavior.OnLanceFinalized(lanceId, styleId, storyId)`.
- Rank title surface: `EnlistmentBehavior.CurrentRankTitle` (from progression_config tier names).
- Registry helpers:
  - `LanceRegistry.ResolveLanceById(lanceId)` → latest name/style/storyId or null.
  - `LanceRegistry.GetCandidateLances(lord, formation, count, overrideStyleId)` → selection pool for UI.
  - `LanceRegistry.GetLancesForCultureAndRole(cultureId, roleHint)` → filtered list for consumers (story/camp).

## Diagnostics
- Session logs: provisional assignment, finalization, unknown-culture cancel, fatigue consume/restore.
- UI hint only when provisional. No Harmony patches; minimal, non-spam logging.

## Safety & Compatibility
- Contained in Enlisted behaviors/menus; no native menu/time patches.
- Fallbacks for missing config; unknown culture prompt; mercenary fallback retained.
- Party activation/time control unchanged outside existing guards.

## References (API verification)
- Culture: `Hero.MapFaction?.Culture?.StringId`, fallback `Hero.Culture?.StringId` (native Hero).
- Menus: `MBInformationManager.ShowMultiSelectionInquiry`, `GameMenu` APIs.
- Persistence: `CampaignBehaviorBase.SyncData(IDataStore)`.

## Missing / To-Do
- Optional: explicit legacy “disbanded” badge string (currently shows “Lance (Legacy)” only).
- Optional: style-weighted selection/count tuning via config if desired later.

## Related docs
- [Lances Catalog](lances-catalog.md)
- [Lance Life](../Gameplay/lance-life.md)
- [Camp Life Simulation](../Gameplay/camp-life-simulation.md)
