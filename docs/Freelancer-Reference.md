## Freelancer Mod Reference (Decompiled Behavior Summary)

This document summarizes how the Freelancer mod implements enlistment gameplay, based on decompiled analysis of `Freelancer/Behavior/FreelanceBehavior.cs`.

### Enlistment Flow
- Player enlists via conversation; the mod sets an internal reference to the lord's party:
  - `recruitedBy = Hero.OneToOneConversationHero.PartyBelongedTo` (or `PlayerEncounter.EncounteredMobileParty`).
- Player equipment/state is swapped and stored; role/tiers tracked (regular soldier → officer/bodyguard/general).
- Complex state: fame, loyalty per kingdom, service level, roles, temporary leaves, promotions.

### Conversation / Enlistment Dialog
- Conversations are registered in `FreelanceBehavior.AddGameMenus(...)` and related behavior init.
- Enlistment path is driven by normal lord conversation branches; upon acceptance, code sets:
  - `recruitedBy = Hero.OneToOneConversationHero.PartyBelongedTo`
  - Initializes `enlistTime`, `EnlistTier`, role/assignment defaults
  - Stores player items/equipment and applies service gear
- Rejoin/leave flows similarly attach to conversation options in the `freelance` menus (e.g., "Ask to leave army", "Ask for honorable discharge").

### Party Relationship
- Player is considered attached to the lord's party via `recruitedBy` field; not explicit engine-level party merge unless joining battles/armies.
- Camera often follows `recruitedBy.Party`.
- Player party visibility can be toggled separately (not always hidden like SAS).

### Settlement Behavior
- `OnSettlementEntered(MobileParty party, Settlement sett, Hero hero)` is present but empty; Freelancer does not force a custom menu here.
- Settlement access is permissive: normal town/village/castle menus remain available.
- Freelancer contributes its own menus:
  - Main: `freelance`
  - Camp: `freelance_camp`
  - Action/Report submenus
- Activation patterns use `GameMenu.SwitchToMenu("freelance")` / `...("freelance_camp")` from options; does not globally intercept settlement entry.

### Persistent Menus & Options
- Registers menus in `AddGameMenus(CampaignGameStarter)`:
  - Status, report, soldier camp, reassignment, leave/discharge, equipment requests, side activities (scout/train/hunt/gamble), mutiny.
- Enables/disables options based on roster counts, wounds, economy, cooldowns, and service state.

### Background Menu Implementation Details
- **Registration**: In `OnSessionLaunched()` method around line 3500-3700 in `FreelanceBehavior.cs`:
  ```csharp
  campaignStarter.AddGameMenu("freelance", 
      "You are currently serving as a soldier...",
      new OnInitDelegate(freelance_on_init),
      new OnConditionDelegate(freelance_on_condition),
      new OnConsequenceDelegate(freelance_on_consequence),
      GameMenu.MenuFlags.None,
      GameOverlays.MenuOverlayType.None);
  ```
- **Menu Flags**: Uses `GameMenu.MenuFlags.None` and `GameOverlays.MenuOverlayType.None` for clean background display
- **Activation**: Called via `GameMenu.SwitchToMenu("freelance")` from conversation options or other menus
- **Menu Options**: Added via `campaignStarter.AddGameMenuOption("freelance", ...)` for various soldier actions
- **Camp Menu**: Separate `freelance_camp` menu for camp-specific actions with similar registration pattern

### Battle/Army Handling
- Does not auto-create an army at enlistment.
- During battles: may form or join an army contextually; handles participation via event- and tick-driven checks.
- Uses normal encounter flow (`EncounterGameMenuBehavior`, `PlayerEncounter`, map events) rather than forcing custom encounters.

### Time/Menu Control
- Uses `GameMenu.SwitchToMenu(...)`, `GameMenu.ActivateGameMenu(...)` contextually.
- Keeps time controls enabled while panels are open by setting
  `Campaign.Current.TimeControlMode = CampaignTimeControlMode.StoppablePlay` and starting wait on the active
  menu (`args.MenuContext.GameMenu.StartWait();`) followed by `Campaign.Current.GameMenuManager.RefreshMenuOptions(...)`.
- Does not globally pause on settlement entry; panels remain collapsible via the chevron.

### APIs and Patterns to Mirror
- Event subscription via `CampaignEvents` (e.g., session launched, map/battle related).
- Menu registration using `CampaignGameStarter.AddGameMenu` and `AddGameMenuOption`.
- Attachment-by-reference: track commander party via field (e.g., `_state.Commander.PartyBelongedTo`) rather than forcing armies.
- Camera follow: `PartyBase.SetAsCameraFollowParty()`.

### APIs and Patterns to Avoid
- Forcing menus on every settlement entry (SAS style) if goal is hybrid player agency.
- Auto army creation at enlistment; Freelancer delays or avoids unless necessary.

### Notes for Enlisted Integration
- Your current escort approach (AI `SetMoveEscortParty`) aligns with modern stability.
- If mirroring Freelancer’s permissive settlement access: avoid overriding `SettlementEntered` to push custom menus; expose enlistment features via additional menus/actions.

---

## Quick API/Event Index (Freelancer)
- Conversation
  - `Campaign.Current.ConversationManager.AddDialogFlow(DialogFlow, ...)`
  - `CampaignMapConversation.OpenConversation(...)`
- Menus
  - `CampaignGameStarter.AddGameMenu`, `AddGameMenuOption`
  - `GameMenu.SwitchToMenu(...)`, `GameMenu.ActivateGameMenu(...)`, `StartWait/EndWait`
- Party/Attachment
  - `Hero.OneToOneConversationHero.PartyBelongedTo`
  - `PartyBase.SetAsCameraFollowParty()`
- Events
  - `CampaignEvents.OnSessionLaunchedEvent`
  - Battle/encounter-related events via `CampaignEvents` and `PlayerEncounter`

## Implementation Checklist (Porting Inspiration)
1) Conversation: add enlist line → on accept, set commander/party ref and initialize service state (time, tier, role).
2) Equipment: snapshot items/gear; apply service gear; restore on leave.
3) Menus: add `freelance`/`camp` equivalents (or integrate into our hub) for leave, assignment, report.
4) Movement: keep camera/AI follow behavior; no forced army at enlist.
5) Settlement: keep native menus; only add options where needed.
6) Persistence: save/load service state cleanly.

## Known Pitfalls / Gotchas
- De-synchronizing `recruitedBy` (null or invalid after lord death/kingdom switch) → add validation and auto-detach.
- Menu re-entry while in encounters → exit menus before switching.
- Overwriting player gear inconsistently → always symmetrical store/restore.

## Mapping to Our Enlisted Behavior
- Commander reference: `recruitedBy` ↔ `_state.Commander.PartyBelongedTo`.
- Enlist trigger: dialog accept ↔ `OnEnlistmentConfirmed()` path.
- Menus: `freelance`/`camp` ↔ our enlisted status/report menus; we preserve native settlement menus by default.
- Settlement: keep native menus ↔ our current permissive approach (match).
- Army/battle handling: escort to commander/army-leader, gentle nudge for inclusion, skip joins while inside settlements.

---

## Wages, XP, and Reputation (Freelancer — Decompiled specifics)
- Daily wage calculation:
  - Formula (see `CalculateDailyWage()`):
    - `wage = min(TroopWage + (playerKillCount * 2) + Tier, 900)`
    - If bodyguard: `wage = min(wage * (IsAtWar ? 3 : 2), 1200)`
    - Returns `wage + MobileParty.MainParty.TotalWage`
  - Applied daily via `EarnDailyWage()` using `GiveGoldAction.ApplyBetweenCharacters(null, Hero.MainHero, CalculateDailyWage(), false)`; any `pendingBonus` is also paid afterward.
- Weekly lump-sum wage:
  - `EarnDailyGeneralWage()` pays a random amount between 2000 and 5000 denars weekly.
- Service level progression:
  - `CalculateCurrentServiceLevel(killCount)` maps kill counts to service levels using thresholds `KILL_SL1..KILL_SL6` (higher levels require more total kills). Service level influences pay and certain dialogs.
- Loyalty and fame over time:
  - Daily increase while enlisted: `IncreaseDailyFameAndLoyality()` increments loyalty to the current faction by a random 0.1–0.4 (rounded; capped at 100).
  - Daily decay: `DecreaseDailyLoyalityAndFame(...)` reduces stored loyalty values over time for factions (non-current factions decay each day).
  - Fame values are tracked per-kingdom (e.g., `fameByKingdom`); thresholds (e.g., `FAME_LIMIT`) gate promotions and options.
- Relation adjustments (events):
  - Frequent calls to `ChangeRelationAction.ApplyPlayerRelation(recruitedBy.LeaderHero, delta)` increase or decrease relation from outcomes such as covering the lord, failing protection, desertion consequences, etc.
- Passive XP and rewards:
  - `GetDailyPassiveXP()` and various activity paths (scout/train/hunt/gamble) grant XP or gold; battle outcomes and role changes also award relation and fame.

## Rank, Promotion, and Equipment Selection (Freelancer — Decompiled specifics)
- Service level and promotion trigger:
  - Promotion is tied to kill count thresholds via `CalculateCurrentServiceLevel(killCount)` and related fame limits; when the computed level exceeds current, `InformPlayerAboutPromotion()` fires.
  - On promotion: shows message (`"You have been promoted…"`) and calls `PickSergeantAgentAndFormation()`, sets `Campaign.Current.TimeControlMode = StoppablePlay`, and `SetRequiredTexts()` for UI refresh.
- Equipment progression:
  - Base soldier gear is tied to `currentUnitData` (troop template). The mod snapshots and restores equipment when separating and applies new gear when changing role/class (e.g., nobility claim or reassignment).
  - Bodyguard path increases salary multiplier and can change role/class; nobility path (`Claim Noble Right`) upgrades `currentUnitData` along its upgrade tree and calls `FinalizeNobility()` which equips `RandomBattleEquipment` from the new template.
  - Manual reassignment: "Request for reassignment" opens a selection UI (`RoleSelectionOver` and related menu), changing sub-role and implicit equipment via `currentUnitData`.
- Gear application mechanics:
  - Equip: `Hero.MainHero.BattleEquipment.FillFrom(this.currentUnitData.RandomBattleEquipment, true)`.
  - Reserve/restore when separating: stored in `reserveEquipment` and `reservedItems`; on separation, `FillFrom(reserveEquipment)` and re-adds reserved items to party inventory.
- User interactions:
  - Promotion message only; no Gauntlet equipment picker. Role/gear changes flow through menu options (reassignment, nobility, buy gear/mount) rather than a rank-up picker screen.

## Leave, Absence, and Desertion (Freelancer — Decompiled specifics)
- Leave of absence:
  - Menu: `freelance_camp` → option "Request {DAYS_TO_ASK} days leave of absence".
  - Controlled by `ALLOWED_LEAVE_DAYS` (constructor parameter) and internal timers; sets `isInLeave = true` and starts a return mission.
  - While on leave, some in-battle relation rewards are suppressed; timed checks run against the allowed leave days window.
- Failure to return on time (desertion):
  - Detection: daily checks transition when leave timer expires; triggers deserter handling.
  - Consequences (from `FreelanceBehavior`):
    - Calls `ApplyPlayerLeaveArmy(true, false, false, false, false)` to detach.
    - Updates active return missions with failure: `UpdateStateOfActiveReturnMissions(2, "You have failed to return your army! You have been declared a deserter!")`.
    - Sets `isInLeave = false` and posts the same deserter message.
    - In encounter contexts, can `GameMenu.ExitToLast()` and then `TakePrisonerAction.Apply(mapEvent.Winner.LeaderParty, Hero.MainHero)` if caught leaving during combat.
  - Additional standing penalties:
    - Relation hits scale elsewhere (e.g., `ChangeRelationAction.ApplyPlayerRelation(this.recruitedBy.LeaderHero, -2 * playerServiceLevel)` in certain failure paths; larger hits for betrayal/mutiny cases shown in file).
- Other leave paths:
  - Ask to leave army (no honor): menu option `ask_to_leave` calls `game_menu_freelance_leave_on_consequence` (applies immediate separation; relation negatives may apply depending on context).
  - Ask for honorable discharge: `ask_to_discharge` path; generally less punitive.
  - Explicit desert action: menu option `freelance` → "Desert the army" executes `game_menu_encounter_desert_on_consequence`.
- Return-to-lord quest:
  - `Quest/ReturnToYourLord.cs` maintains a quest log to find/return to the lord and finalizes with success/fail/cancel depending on outcomes; supports encyclopedia "last seen" hints for returning.

## Commander Death/Imprisonment and Player Capture/Escape (Freelancer — Decompiled specifics)
- Army disbanded or kingdom switch:
  - If the army disbands: message shown and the player is detached: `ApplyPlayerLeaveArmy(false, false, false, false, false)`; active return missions updated accordingly.
  - If the army switches allegiance (no longer serving same kingdom): honorable discharge path executed and separation applied (`ApplyPlayerLeaveArmy(true, false, false, false, false)`), with quest state updated.
- Commander imprisonment/death:
  - Event hook present: `CampaignEvents.HeroPrisonerTaken` → `OnPrisonerTaken(...)` (method exists in behavior). In the decompiled build provided, the body is empty, so no explicit additional handling beyond broader army/kingdom/disband checks.
  - General refresh paths handle invalid commander/party state and fall back to separation/discharge flows described above.
- Player captured while enlisted:
  - Several flows can take the player prisoner (e.g., deserting during an encounter, mutiny failure): `TakePrisonerAction.Apply(..., Hero.MainHero)`.
  - While enlisted, some paths set `Hero.MainHero.NeverBecomePrisoner = true` to prevent capture during scripted transitions; this is cleared on separation (`NeverBecomePrisoner = false`).
  - `PlayerCaptivity.IsCaptive` is checked in daily/tick logic to branch behavior while captive; upon release, visibility/activation and party state are restored.

## Engagement/Encounter Handling and Invisible Party Safety (Freelancer — Decompiled specifics)
- Map invisibility and activation during service:
  - Sets `MobileParty.MainParty.IsVisible = false` and `IsActive = false` when attaching to the commander or entering certain flows; restores to visible/active when separating or entering events.
  - Updates visibility via `PartyBase.MainParty.UpdateVisibilityAndInspected(20f, true)` during transitions.
- Preventing attacks on the main party while enlisted:
  - By deactivating the main party (`IsActive = false`) and hiding it (`IsVisible = false`), world AI won’t target the main party. Engagements are performed through the commander’s party; camera follows the commander.
  - In bandit/low-life side paths, similar hide/deactivate toggles are used when joining another party temporarily; on exit, set `IsActive = true`, `IsVisible = true` and hold/restore camera.
- Encounter routing:
  - Starts battle/siege events explicitly with the main party when appropriate (`MapEventManager.StartBattleMapEvent`, `SiegeEventManager.StartSiegeEvent`) after re-enabling `IsActive = true` to ensure proper event ownership.
- Camera/position:
  - Positions main party at the commander (`PartyBase.MainParty.MobileParty.Position2D = recruitedBy.Position2D`) and turns off visibility; camera follow maintains player perspective without exposing the main party to AI.

## Freelancer Soldier Camp — Menu Actions (deep dive)
The Soldier Camp acts as a secondary hub with time-boxed actions and service requests:

- Time-boxed activities
  - Scout / Train / Hunt: `go_and_scout`, `go_and_train`, `go_and_hunt` style options with hours cost (SCOUT_TIME, TRAIN_TIME, HUNT_TIME). These schedule a `SimpleActionPair` and run via elapsed hours checks.
  - Request reassignment: opens a `MultiSelectionInquiryData` list for sub-roles (see `RoleSelectionOver` and `SUBROLES`), changing `currentSubrole` and implicit equipment/behavior.

- Paid services
  - Request new gear / mount: charges gold proportional to `Tier * ServiceLevel` and equips from `currentUnitData.RandomBattleEquipment` or mount pools.
  - Buy beer / medical supplies: party morale/health effects; gold cost displayed in menu.

- Leave/discharge
  - Ask for honorable discharge (less punitive exit) vs Ask to leave army (immediate leave with possible relation impact).
  - Request leave of absence: sets separation for `ALLOWED_LEAVE_DAYS` with a return mission and late-return deserter penalty (see leave/desertion section).

Implementation mechanics
- Registered via `campaignStarter.AddGameMenu("freelance_camp", ...)` and option adds.
- Scheduling: `SimpleActionPair(-1, CampaignTime.Never)` and timers within `OnTick` branches gate when an action completes.
- UI feedback: prices and timers are injected via `MBTextManager.SetTextVariable(...)` for dynamic labels.

## Save/Load and Persistence (Freelancer — Decompiled specifics)
- Save definer:
  - `BlocFreelancerSaveDefiner : SaveableTypeDefiner` (ID `18811073`) registers types/version for save system.
- Core behavior save:
  - `FreelanceBehavior.SyncData(IDataStore)` persists unit template and internal state, e.g., `freelanceCurrentUnitData` (CharacterObject), and related fields inside the method.
  - Other behaviors persist their own scoped state:
    - `CustomHealBehavior.SyncData`: `_freelanceOverflowedHealingForHeroes` (Dictionary of PartyBase → float)
    - `HiredBladeBehavior.SyncData`: `freelanceProtectedCaravan` (MobileParty), `freelanceIsGuarding` (bool)
    - `GeneralsCampaignBehavior.SyncData`: conditional sync when enabled
    - `LowLifeBehavior.SyncData`: `freelanceBanditJoinedBanditArmy` (MobileParty) and related fields
- Pattern:
  - Persist per-feature references with clear unique keys; upon load, re-establish visual/activation state (`IsVisible/IsActive`) and escort/camera as part of enlistment recovery logic.


