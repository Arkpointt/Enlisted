# Lance Assignments — Documentation

## Index
- [Overview](#overview)
- [Tier & Rank System](#tier--rank-system)
- [Culture-Specific Ranks](#culture-specific-ranks)
- [Data & Config](#data--config)
- [Cultural Archetypes & Culture Map](#cultural-archetypes--culture-map)
- [Runtime Behavior](#runtime-behavior)
- [UI](#ui)
- [Events & API Surfaces](#events--api-surfaces)
- [Diagnostics](#diagnostics)
- [Safety & Compatibility](#safety--compatibility)
- [References (API verification)](#references-api-verification)

## Overview
Lances give enlisted players a sub-unit identity from enlistment through promotion. Tier 1 assigns a provisional lance; Tier 2 finalizes it from culture/style-filtered options. Everything runs inside Enlisted (no native menu/time patches) and exposes IDs for story and future camp flavor.

## Tier & Rank System

The progression system supports 9 tiers with culture-specific rank names:

| Tier | Track | XP Required | Duration |
|------|-------|-------------|----------|
| 1 | Enlisted | 0 | 1-2 weeks |
| 2 | Enlisted | 800 | 2-4 weeks |
| 3 | Enlisted | 3,000 | 1-2 months |
| 4 | Enlisted | 6,000 | 2-3 months |
| 5 | Officer | 11,000 | 3-4 months |
| 6 | Officer | 19,000 | 4-6 months |
| 7 | Commander | 30,000 | 6+ months |
| 8 | Commander | 45,000 | Senior command |
| 9 | Commander | 65,000 | Endgame |

## Culture-Specific Ranks

Each culture has unique rank names that reflect their military traditions:

### Empire (Legion / Discipline)
| Tier | Rank | Role |
|------|------|------|
| T1 | Tiro | Raw recruit |
| T2 | Miles | Soldier |
| T3 | Immunes | Specialist |
| T4 | Principalis | Junior NCO |
| T5 | Evocatus | Veteran |
| T6 | Centurion | NCO Leader |
| T7 | Primus Pilus | First Spear / Captain |
| T8 | Tribune | Staff Officer |
| T9 | Legate | General / Warlord |

### Vlandia (Feudal / Chivalry)
| Tier | Rank | Role |
|------|------|------|
| T1 | Peasant | Raw recruit |
| T2 | Levy | Conscript |
| T3 | Footman | Soldier |
| T4 | Man-at-Arms | Professional |
| T5 | Sergeant | NCO |
| T6 | Knight Bachelor | Elite |
| T7 | Cavalier | Elite Knight |
| T8 | Banneret | Retinue Leader |
| T9 | Castellan | Lord's Deputy |

### Sturgia (Tribal / Shield Wall)
| Tier | Rank | Role |
|------|------|------|
| T1 | Thrall | Unfree recruit |
| T2 | Ceorl | Freeman |
| T3 | Fyrdman | Militia |
| T4 | Drengr | Warrior |
| T5 | Huskarl | Household troop |
| T6 | Varangian | Elite guard |
| T7 | Champion | The Elite |
| T8 | Thane | Local Chieftain |
| T9 | High Warlord | War leader |

### Khuzait (Steppe / Horde)
| Tier | Rank | Role |
|------|------|------|
| T1 | Outsider | Non-clan |
| T2 | Nomad | Wanderer |
| T3 | Noker | Servant warrior |
| T4 | Warrior | Proven |
| T5 | Veteran | Experienced |
| T6 | Bahadur | Hero |
| T7 | Arban | Leader of 10 |
| T8 | Zuun | Leader of 100 |
| T9 | Noyan | Noble Commander |

### Battania (Celtic / Guerrilla)
| Tier | Rank | Role |
|------|------|------|
| T1 | Woodrunner | Scout recruit |
| T2 | Clan Warrior | Tribesman |
| T3 | Skirmisher | Light fighter |
| T4 | Raider | Veteran |
| T5 | Oathsworn | Bound warrior |
| T6 | Fian | Elite champion |
| T7 | Highland Champion | War hero |
| T8 | Clan Chief | Leader |
| T9 | High King's Guard | Royal elite |

### Aserai (Desert / Mercantile)
| Tier | Rank | Role |
|------|------|------|
| T1 | Tribesman | Desert recruit |
| T2 | Skirmisher | Light fighter |
| T3 | Footman | Soldier |
| T4 | Veteran | Experienced |
| T5 | Guard | Professional |
| T6 | Faris | Knight |
| T7 | Emir's Chosen | Elite |
| T8 | Sheikh | Minor Leader |
| T9 | Grand Vizier | Strategic Leader |

### Mercenary / Universal (Generic)
| Tier | Rank | Role |
|------|------|------|
| T1 | Follower | Tagalong |
| T2 | Recruit | New hire |
| T3 | Free Sword | Mercenary |
| T4 | Veteran | Experienced |
| T5 | Blade | Skilled |
| T6 | Chosen | Elite |
| T7 | Captain | Leader |
| T8 | Commander | Senior officer |
| T9 | Marshal | Highest rank |

## Phase 5 extension: named lance personas (shipping, optional)
When enabled, Enlisted can generate a deterministic, text-only roster of "named personas" for lance roles (leader/second/veterans/soldiers/recruits). These are used for story flavor only (placeholders), not troop individuation.

Gate:
- `lance_personas.enabled` in `ModuleData/Enlisted/enlisted_config.json`

Data:
- `ModuleData/Enlisted/LancePersonas/name_pools.json`

## Data & Config
- Gate: `lances_enabled` in `ModuleData/Enlisted/enlisted_config.json`.
- Progression: `ModuleData/Enlisted/progression_config.json`
  - `tier_progression.requirements[]`: XP thresholds and generic rank names
  - `culture_ranks`: culture-specific rank names for all 9 tiers
- Catalog: `ModuleData/Enlisted/lances_config.json`
  - `style_definitions`: styles with `id`, `lances[]` of `{ id, name, roleHint (infantry/ranged/cavalry/horsearcher), storyId }`.
  - `culture_map`: culture StringId -> styleId; unknown cultures trigger an in-game style prompt.
- Selection count: `lance_selection_count` (default 3–5).
- Weighting toggle: `use_culture_weighting` (reserved; current implementation shuffles without weights).

## Cultural Archetypes & Culture Map
- Styles (examples from defaults):
  - Legion: The 4th Cohort, The Iron Column, The Emperor's Eyes, The Immortals, The Eagle's Talons.
  - Feudal: The Red Chevron, The Broken Lances, The White Gauntlets, The Black Pennant, The Iron Spur, The Gilded Lilies.
  - Tribal: The Wolf-Skins, The Frost-Eaters, The Stag's Blood, The Shield-Biters, The Bear's Paw, The Raven Feeders.
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
- `enlisted_lance` menu (My Lance):
  - Shows lance roster with player position based on tier
  - Displays culture-specific rank title
  - View Full Roster popup

## Events & API Surfaces
- Finalization event: `EnlistmentBehavior.OnLanceFinalized(lanceId, styleId, storyId)`.
- Rank title surfaces:
  - `EnlistmentBehavior.CurrentRankTitle` — culture-specific rank for current tier
  - `ConfigurationManager.GetCultureRankTitle(tier, cultureId)` — get any culture's rank
  - `ConfigurationManager.GetTierName(tier)` — generic/mercenary fallback
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

## Related docs
- [Lance Life](../Gameplay/lance-life.md)
- [Camp Life Simulation](../Gameplay/camp-life-simulation.md)
- [Menu Interface](../UI/menu-interface.md)
