# Bannerlord Map Incidents (1.3.10) — Integration Notes

## Index
- [What the native system does](#what-the-native-system-does)
- [Spawn probabilities & cooldowns](#spawn-probabilities--cooldowns)
- [Triggers & invocation points](#triggers--invocation-points)
- [Incident selection](#incident-selection)
- [Example native incident (plague)](#example-native-incident-plague)
- [How to integrate for Enlisted](#how-to-integrate-for-enlisted)
- [Key APIs (decompile verified, 1.3.10)](#key-apis-decompile-verified-1310)
- [Recommendations for Enlisted](#recommendations-for-enlisted)

## What the native system does
- Host behavior: `TaleWorlds.CampaignSystem.CampaignBehaviors.IncidentsCampaignBehaviour`.
- Registers listeners on: `HourlyTickEvent`, `GameMenuOpened`, `GameMenuOptionSelected`, `MapEventEnded`, `ConversationEnded`, `OnHeirSelectionOver`, `SettlementEntered/Left`, `OnIncidentResolved`, `OnNewGameCreated`.
- Uses `IncidentModel` for trigger probabilities and global cooldowns:
  - `GetIncidentTriggerGlobalProbability()` (50% roll per trigger).
  - `GetIncidentTriggerProbabilityDuringSiege()` (~14.3% per hourly tick in siege).
  - `GetIncidentTriggerProbabilityDuringWait()` (~14.3% per hourly tick while waiting).
  - `GetMinGlobalCooldownTime()` / `GetMaxGlobalCooldownTime()` (8–15 days window per fire).
- Cooldowns: per-incident cooldown plus a global cooldown (`_lastGlobalIncidentCooldown`). Resolved incidents go into `_incidentsOnCooldown` until expiry.
- Triggers (flags): `EnteringTown`, `LeavingTown`, `EnteringVillage`, `LeavingVillage`, `EnteringCastle`, `LeavingCastle`, `LeavingSettlement`, `LeavingBattle`, `LeavingEncounter`, `WaitingInSettlement`, `DuringSiege`.
- Invocation points:
  - Enter menus `town`, `village`, `castle` (if `CurrentMenuContext` is null when entering settlement, then checked on menu open).
  - Leave menu options `town_leave`, `castle:leave`, `village:leave` → triggers “leaving” incidents.
  - After battle ends (player map event, field/hideout, with winner) → `LeavingBattle` trigger.
  - After conversation end while leaving an encounter → `LeavingEncounter` trigger.
  - Hourly tick: if in siege → `DuringSiege`; if waiting menus (`town_wait_menus`/`village_wait_menus`) → `WaitingInSettlement`.
  - Settlement busy checks block incidents when the settlement is busy.

## Spawn probabilities & cooldowns
- Global chance per trigger: 0.5f.
- Siege/wait chance per hourly tick: 0.143f each.
- Global cooldown window: 8–15 days (randomized).
- Per-incident cooldown: defined per Incident (e.g., plague: 60 days).

## Triggers & invocation points
- EnteringTown/EnteringVillage/EnteringCastle → on menu open after settlement enter.
- LeavingTown/LeavingVillage/LeavingCastle/LeavingSettlement → on menu option select (leave).
- LeavingBattle → after player battle ends (field/hideout).
- LeavingEncounter → after conversation ended while leaving.
- WaitingInSettlement → while in `town_wait_menus`/`village_wait_menus` (hourly tick).
- DuringSiege → while besieged (hourly tick).
- Guarded by: settlement busy checks, prisoner state, active conversation, global cooldown.

## Incident selection
- Uses `MBObjectManager.Instance.GetObjectTypeList<Incident>()`, filters by trigger flag, not on cooldown, and `Incident.CanIncidentBeInvoked()`.
- Picks a random eligible incident and sets `MapState.NextIncident` to it; the map state later executes it.

## Example native incident (plague)
- Id: `incident_horsefly_plague` (Trigger: `LeavingVillage`, Type: `AnimalIllness`, Cooldown: 60 days).
- Condition: player has ≥4 horses in inventory.
- Options apply effects: horse isolation (reduce horse count), killing infected horses, ignore (wounds/loses horses).

## How to integrate for Enlisted
1) Consumption-only approach: subscribe to native events (e.g., `OnGameMenuOpened`, `OnGameMenuOptionSelected`, `MapEventEnded`) to add Enlisted-flavored logic when `MapState.NextIncident` is set, or react to `OnIncidentResolvedEvent`.
2) Authoring custom incidents:
   - Create your own `CampaignBehaviorBase` mirroring `IncidentsCampaignBehaviour.RegisterIncident` usage to `Game.Current.ObjectManager.RegisterPresumedObject<Incident>` and `Initialize(...)` with trigger flags, type, cooldown, and a condition.
   - Add options with `IncidentEffect` helpers (trait changes, morale, relation, gold, troop wounds/kills, item effects). Effects live in `TaleWorlds.CampaignSystem.Incidents` (see decompile for available `IncidentEffect` static builders).
   - Ensure you respect cooldowns: maintain your own `_incidentsOnCooldown` and `_lastGlobalIncidentCooldown` or reuse native by registering your incidents before the native behavior runs (riskier); safer to mirror the pattern in a dedicated Enlisted incident behavior.
3) Trigger considerations for enlisted flavor:
   - Use `EnteringTown/Village/Castle` for camp/status checks (e.g., fatigue, lance legacy hooks).
   - Use `LeavingTown/Village/Settlement` for enlistment leave/return story beats.
   - Use `LeavingBattle` to branch on duty/lance data after fights.
   - Use `WaitingInSettlement` for camp downtime stories.
4) Safety:
   - Guard against `Hero.MainHero.IsPrisoner`, `ConversationFlowActive`, and settlement busy checks as native does.
   - Use `CampaignTime.Now.NumTicks` for seeded randomness (native uses `RandomFloatWithSeed` with ticks).

## Key APIs (decompile verified, 1.3.10)
- Behavior: `TaleWorlds.CampaignSystem.CampaignBehaviors.IncidentsCampaignBehaviour`.
- Incident model: `Campaign.Current.Models.IncidentModel` for probabilities and cooldown ranges.
- Incident objects: `TaleWorlds.CampaignSystem.Incidents.Incident`; registered via `Game.Current.ObjectManager.RegisterPresumedObject<Incident>()` then `Initialize(...)`.
- Effects: `TaleWorlds.CampaignSystem.Incidents.IncidentEffect` static helpers (trait, morale, relation, gold, troop kill/wound, item/equipment changes).
- Hooks: `CampaignEvents.OnIncidentResolvedEvent`, `OnGameMenuOpened`, `OnGameMenuOptionSelected`, `MapEventEnded`, `SettlementEntered/Left`, `HourlyTickEvent`.

## Recommendations for Enlisted
- Implement an `EnlistedIncidentsBehavior` that mirrors native selection logic but sources Enlisted-specific incident IDs to avoid altering native pools.
- Keep a per-incident and global cooldown similar to native to avoid spam.
- Use Enlisted state for conditions/effects (lance id/storyId, duty, fatigue, enlisted tier).
- Log via existing session diagnostics for QA, and gate with a config toggle for debug/QA builds.

## Current Enlisted usage (implemented)
- `EnlistedIncidentsBehavior` registers the enlistment bag-check incident and triggers it by setting `MapState.NextIncident` (native map incident UI). If incidents are unavailable, it falls back to the inquiry prompt so enlistment never blocks.
- Bag check is scheduled ~12 in-game hours after enlistment and only fires when safe (no battle/encounter/captivity). Completion resets the schedule.
- Pay muster currently uses an **inquiry fallback** prompt (not native incident UI), so it does not rely on `MapState.NextIncident`.
- Incident suppression: while enlisted (not in grace), native incidents are suppressed via `IncidentsCampaignBehaviour.TryInvokeIncident` prefix to prevent inappropriate random incidents; the bag-check incident bypasses suppression because it is triggered directly by Enlisted.

