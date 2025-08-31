## ServeAsSoldier Mod Reference (Decompiled Behavior Summary)

This document summarizes how ServeAsSoldier (SAS) implements its enlistment systems, based on decompiled analysis of files such as `ServeAsSoldier/Test.cs` and related event/menu patches.

### Core Philosophy
- Player roleplay as a soldier embedded in a lord’s party.
- Strong control via a persistent custom menu (`party_wait`) rather than native settlement menus.

### Persistent Menu: `party_wait`
- Registered via `campaignStarter.AddWaitGameMenu("party_wait", ...)` with `OnInit`, `OnCondition`, and `OnTick` handlers.
- Updated continuously through `wait_on_tick` → `updatePartyMenu(args)` to render dynamic details:
  - Party/Army objective (behavior text)
  - Enlistment time, tier, formation
  - Wage, XP, next level XP
  - Current assignment description
- Provides actions: change equipment/weaponsmith, train, toggle battle command verbosity, tournament participation, talk to members, show faction reputation, ask for leave, ask for new assignment, attack bandits, etc.

### Background Menu Implementation Details
- **Registration**: In `OnSessionLaunched()` method around line 1200-1400 in `Test.cs`:
  ```csharp
  campaignStarter.AddWaitGameMenu("party_wait", 
      "You are currently serving as a soldier in {COMMANDER_NAME}'s party...",
      new OnInitDelegate(party_wait_on_init),
      new OnConditionDelegate(party_wait_on_condition),
      new OnTickDelegate(party_wait_on_tick),
      GameMenu.MenuFlags.None,
      GameOverlays.MenuOverlayType.None);
  ```
- **Menu Flags**: Uses `GameMenu.MenuFlags.None` and `GameOverlays.MenuOverlayType.None` for clean background display
- **Time Control While Open**: After activation or in `party_wait_on_init`, SAS explicitly sets
  `Campaign.Current.TimeControlMode = CampaignTimeControlMode.StoppablePlay`, then calls
  `args.MenuContext.GameMenu.StartWait()` and `Campaign.Current.GameMenuManager.RefreshMenuOptions(...)`.
  This lets the top-left ribbon time controls work while the panel remains open/collapsible.
- **Activation**: Called via `GameMenu.ActivateGameMenu("party_wait")` when commander enters settlements
- **Time Control**: For towns, pauses time with `Campaign.Current.TimeControlMode = 0` to maintain menu focus
- **Menu Options**: Added via `campaignStarter.AddGameMenuOption("party_wait", ...)` for various soldier actions

### Settlement Entry Behavior
### Conversation / Enlistment Dialog
- Conversation hooks are added on session launch; SAS uses the lord conversation to set service state.
- On acceptance, it assigns:
  - `Test.followingHero = Hero.OneToOneConversationHero`
  - `Test.enlistTime = CampaignTime.Now`, `Test.EnlistTier = 1`
  - Stores player gear (`oldItems`, `oldGear`, `tournamentPrizes`) and equips soldier loadout
- Many subsequent soldier actions (assignment, leave, talk to, equipment) are invoked by opening conversations from `party_wait` using `Campaign.Current.ConversationManager.AddDialogFlow(...)` and `CampaignMapConversation.OpenConversation(...)` targeting the commander or notables.
- On commander entering settlements, SAS activates `party_wait` instead of native menus:
  - `GameMenu.ActivateGameMenu("party_wait")`
  - For towns, it pauses time: `Campaign.Current.TimeControlMode = 0`
- Additional “return to army camp” options are injected into many native menus to route back to `party_wait`.

### Attachment & Visibility
### Wages, XP, and Reputation (Decompiled specifics)
- Daily tick (see `Test.cs` → `TickDaily()`):
  - Pays wages to player and commander:
    - `GiveGoldAction.ApplyBetweenCharacters(null, Hero.MainHero, wage(), false)`
    - `GiveGoldAction.ApplyBetweenCharacters(null, Test.followingHero, wage(), false)`
  - Faction reputation: `Test.ChangeFactionRelation(Test.followingHero.MapFaction, 10)` per day.
  - Lord relation: `Test.ChangeLordRelation(Test.followingHero, XPAmount)` where `XPAmount = SubModule.settings?.DailyXP ?? 10`.
  - Enlistment XP pool: `Test.xp += XPAmount` (same `XPAmount` as above).
- Wage formula (see `wage()`):
  - `w = floor( max(0, perkMultiplier * min(Hero.MainHero.Level*2 + Test.xp / Settings.XPtoWageRatio, Settings.MaxWage)) + MobileParty.MainParty.TotalWage )`
  - `perkMultiplier = 1.2` if `DefaultPerks.Polearm.StandardBearer` is active; otherwise `1.0`.
- Rank progression and offers (see `Tick2()`):
  - Level thresholds read from `SubModule.settings.Level{1..7}XP` via `NextlevelXP[]`.
  - When `Test.xp > NextlevelXP[EnlistTier]`, `EnlistTier++` and a promotion dialog opens.
  - Vassalage offer when `GetFactionRelations(faction) >= Settings.PromotionToVassalXP`.
  - Retirement when `Test.xp > retirementXPNeeded` (per-faction `retirementXP` starts at `Settings.RetirementXP`, increases on re-enlist).
- Role-based skill XP (see `GetXPForRole()`):
  - Grants 100 XP daily in a skill tied to current assignment; rare chance to add +1 focus or +1 attribute in that skill’s attribute.
- Misc events influence XP/relation/wages:
  - Handing over prisoners, tournaments, assignments, and scripted events modify `Test.xp`, faction relation, lord relation, and may add gold.

## Rank, Promotion, and Equipment Selection (ServeAsSoldier — Decompiled specifics)
- Promotion trigger:
  - XP thresholds from `SubModule.settings` are loaded into `Test.NextlevelXP[]`.
  - In hourly tick `Tick2()`, while `Test.xp > NextlevelXP[EnlistTier]`, increments `EnlistTier` and flags `leveledUp`.
  - On level-up: sets `Test.conversation_type = "promotion"`, shows a promotion message, then opens a conversation using `CreatePromotionDialog()`.
- Promotion dialog behavior:
  - For tiers < 6: standard promotion line instructs to visit bladesmith/armourer for new gear.
  - At tier 6: alternate line offers command privileges and sponsored retinue.
  - After dialog, equipment selection is exposed through menu-driven options.
- Equipment selection UI:
  - Uses Gauntlet VM layer: `EquipmentSelectorBehavior.CreateVMLayer(items, slotKey)`.
  - Options include multiple slots: weapons (slots 1–4), Head, Cape, Body, Gloves, Leg, Horse, Harness.
  - Item pools computed by `Test.GetAvaliableGear(List<EquipmentIndex>)` for the requested slots.
  - Time is paused (`Campaign.Current.TimeControlMode = 0`) when the selector opens.
- User flow summary:
  - Gain XP → tier up in `Tick2()` → promotion dialog → choose equipment categories → open Gauntlet selector per category → confirm to apply.

## Engagement/Encounter Handling and Invisible Party Safety (SAS — Decompiled specifics)
- Map invisibility and activation:
  - Hides visuals via `PartyVisualManager`: `HumanAgentVisuals.GetEntity().SetVisibilityExcludeParents(false)` and similar for mounts in `hidePlayerParty()`; shows them back in `showPlayerParty()` by setting to `true`.
  - Detaches main party from world AI by toggling `MobileParty.MainParty.IsActive = false` when attached to commander; restored to `true` when needed for events/encounters.
- Preventing unwanted attacks on invisible/main party:
  - While the player is captive: `MobileParty.MainParty.IgnoreByOtherPartiesTill(CampaignTime.DaysFromNow(1))` throttles pursuers.
  - For scripted deserter events: created parties are forced to attack the commander, not the main party: `deserterParty.Ai.SetMoveEngageParty(Test.followingHero.PartyBelongedTo)` and `deserterParty.IgnoreByOtherPartiesTill(CampaignTime.Never)`.
  - On event cleanup, parties are reset: `IgnoreByOtherPartiesTill(CampaignTime.Now)` and allow decisions.
- Encounter routing:
  - For arena/tournament and special events, SAS re-activates the main party (`IsActive = true`), exits menus, and restarts player encounters targeting the event party (`PlayerEncounter.RestartPlayerEncounter(...)`) to ensure proper battle contexts.
- Camera/position coupling:
  - Keeps camera and position in sync with commander; re-shows party on transitions to ensure clean interactions with encounters.

## Harmony Patch Footprint (SAS) — Rationale and Grouping
SAS uses many Harmony patches to reshape vanilla flows for a soldier-first UX. Major categories:

- Menu/Encounter routing
  - `EncounterMenuPatch`, `AddCreationMenuPatch*`, `SettlementSpawnPatch`:
    - Force or augment menus (e.g., route into `party_wait`, inject or reorder menu options) when commander enters settlements or during encounters.
    - Why: keep the soldier hub persistent and avoid native menus hijacking flow.

- Nameplate/visibility and tracking
  - `HidePartyNamePlatePatch`, `PartyNamePlateTrackPatch`:
    - Suppress main party shield/nameplate and tracker visuals so the player remains visually hidden while enlisted.

- Restrictions and rule changes
  - `NoCaravanTradePatch`, `NoVillagerTradePatch`, `NoLootPatch`, `NoRetreatPatch`, `NoHorseSiegePatch`, `NoDisperseMessagePatch`, `DisableFireNorificationPatch`:
    - Remove or alter native mechanics that break the soldier fantasy or allow exploits (looting, retreating, etc.).

- Balance/model adjustments
  - `InfluenceCalculationPatch`, `XPMultiplierPatch`, `SkillsFromPartyPatch`, `SergentScorePatch`, `Effective*Patch` (Engineer/Quartermaster/Scout/Surgeon):
    - Adjust XP/Influence and skill sourcing to fit SAS progression and roles.

- Equipment/visual tweaks
  - `HeroEquipmentChangePatch`, `TownArmourPatch`, `ReplaceBannerPatch`, `PropsPatch`:
    - Control visuals and equipment presentation in towns/battles consistent with SAS rank/equipment.

- Conversations and recruitment
  - `Conversation*Patch` (Attack/Discuss/Hire/Quest), `VassalAndMercenaryOfferPatch`:
    - Insert or redirect specific dialog flows not easily reachable with public menu/dialog APIs.

- Party/mission control
  - `PartyLimitPatch`, `AttachPatch`, `AgentRemovePatch`, `MissionFightEndPatch`, `FreeWanderPatch`:
    - Constrain party sizes, attach behaviors, or alter mission lifecycle to maintain SAS constraints.

What Enlisted needs vs can skip:
- Keep minimal patches:
  - Nameplate/Tracker suppression (if public API attempts aren’t sufficient).
  - Narrow encounter/menu routing only if we must intercept a few native menus; otherwise prefer public `CampaignGameStarter` menus and status hub.
- Prefer behaviors over patches for:
  - Wages/XP/reputation, promotion/equipment, escort/camera/visibility.
  - Immediate/timed leave and deserter penalties.
- Skip (unless a future feature explicitly requires):
  - Broad economy/trade/retreat/loot restrictions, model multipliers, and town equipment cosmetics.

Reference: Harmony strategy and safety practices — https://docs.bannerlordmodding.lt/modding/harmony/

- Player party visibility can be hidden/shown with `PartyVisualManager` helpers (`hidePlayerParty` / `showPlayerParty`).
- Player follows commander; during certain actions (e.g., ambush lure) the mod temporarily re-activates the player party and exits menus.

### Battle/Army Handling
- Does not create an army at enlistment.
- In Tick logic, when the commander is in battle without an army, SAS may create/join an army and add the player for participation.
- Uses `battle_wait` wait menu for battle staging; transitions back to `party_wait` after.

### Time/Menu Control Patterns
- Frequent use of `GameMenu.ActivateGameMenu("party_wait")`, `GameMenu.SwitchToMenu(...)`, and loops to exit to last menu before state changes.
- Uses wait menus to maintain a controlled, immersive soldier experience.

### APIs and Patterns to Mirror
- If aiming for a soldier-first UX: a single persistent menu hub with dynamic status and actions.
- “Return to army camp” options added to various native menus for consistent navigation.

### APIs and Patterns to Avoid (for hybrid designs)
- Forcing `party_wait` on every settlement entry if you want players to access native town features.
- Excessive menu re-entrancy (multiple ExitToLast loops) unless strictly necessary.

### Integration Notes for Enlisted
- Your enlistment currently aligns more with Freelancer’s permissive approach; selectively adopt SAS features (e.g., a compact `party_wait`-like menu) without suppressing native settlement menus unless desired.

---

## Quick API/Event Index (ServeAsSoldier)
- Persistent Menu
  - `CampaignGameStarter.AddWaitGameMenu("party_wait", ...)`
  - `GameMenu.ActivateGameMenu("party_wait")`, `GameMenu.SwitchToMenu(...)`
  - `wait_on_init`, `wait_on_condition`, `wait_on_tick` updating via `updatePartyMenu(args)`
- Conversation
  - `Campaign.Current.ConversationManager.AddDialogFlow(...)`
  - `CampaignMapConversation.OpenConversation(...)`
- Party
  - `Test.followingHero = Hero.OneToOneConversationHero`
  - AI steering and temporary player party activation as needed
- Events
  - `CampaignEvents.SettlementEntered` used to route into `party_wait` (towns pause time)

## Implementation Checklist (Selective Adoption)
1) Enlist dialog: set commander ref (`followingHero` analogue), init service state.
2) Persistent soldier hub: optional minimal menu mirroring `party_wait` with status/actions.
3) Return-to-camp options sprinkled into common menus for navigation.
4) Battle staging: optional `battle_wait`-style wait with safe transitions.
5) Time control: only pause in controlled flows; avoid global forcing unless intended.

## Known Pitfalls / Gotchas
- Overriding settlement menus can reduce player agency; consider hybrid approach.
- Menu re-entrancy and nested transitions can cause instability; always exit menus first.
- Forcing army creation too early can conflict with engine logic; defer to battle context.

## Mapping to Our Enlisted Behavior
- Commander ref: `followingHero` ↔ `_state.Commander`.
- Menu hub: `party_wait` ↔ we keep a lightweight enlisted status/report menu and preserve native settlement menus by default.
- Settlement behavior: SAS forces hub; Enlisted does not — intentional difference.
- Army/battle handling: escort to commander (or army leader), join army blob when applicable, and gentle nudge for inclusion. We guard menus during encounters and skip joins while inside settlements.

## Leave, Absence, and Desertion (ServeAsSoldier — Decompiled specifics)
- Leave options:
  - From `party_wait`: "Ask commander for leave" triggers `Test.LeaveLordPartyAction(false)` and exits menus. This is an immediate discharge (not a timed absence) and typically preserves less gear than retirement paths.
  - Retirement/vassalage flows: dialogs in `RetirementCreateDialog()` and `KingdomJoinCreateDialog*()` provide honorable exit or transition; often grant relation and gold bonuses on re-enlist or retirement.
- Desertion and penalties:
  - SAS uses the game’s crime/relation gates to block enlistment when the player is a criminal or disliked:
    - Example: joining blocked if `MapFaction.MainHeroCrimeRating > 30` or lord relation ≤ −10 at enlist time.
  - While explicit timed leave→desert checks are not centralized like Freelancer’s `isInLeave`, SAS penalizes dishonorable exits and kingdom abandonment contexts by immediately calling `Test.LeaveLordPartyAction(false)` and updating diplomacy/crime state.
  - Relation changes on exit vary by path:
    - Retirement re-enlist: `ChangeRelationAction.ApplyPlayerRelation(Test.followingHero, +20)` and 25,000 gold.
    - Forced/cancelled enlistment (e.g., lord’s clan leaves kingdom): immediate `LeaveLordPartyAction(false)` without rewards.
  - Faction-wide impact: `Test.ChangeFactionRelation(faction, -100000)` is used in the “retire now” negative branch to tank faction standings before a clean exit.
- Implementation hooks to mirror:
  - Use an explicit “ask leave now” path for immediate discharge.
  - For honorable retirement: award gold and relation, and raise per-faction retirement XP threshold.
  - Block re-enlistment under high crime rating or poor lord relation until resolved.

## Commander Death/Imprisonment and Player Capture/Escape (SAS — Decompiled specifics)
- Commander death/imprisonment:
  - Many update loops guard on `Test.followingHero != null && Test.followingHero.IsAlive` before executing enlistment behaviors (wages, healing, promotions). If the commander dies or becomes invalid, the hub/menu logic unwinds and the player is detached back to normal play.
  - In tick: additional guards ensure the commander’s party exists and is not prisoner: `Test.followingHero.PartyBelongedTo != null && !Test.followingHero.IsPrisoner && !Hero.MainHero.IsPrisoner`.
  - If the lord’s clan leaves the kingdom or the party becomes invalid, code executes `Test.LeaveLordPartyAction(false)` (forced detachment without rewards).
- Player captured:
  - When `Hero.MainHero.IsPrisoner` is true, tick logic reduces interactions (e.g., ignores by other parties for a day) and prevents normal camp flows; the player resumes camp logic after release.
  - Prisoner-related interactions are supported in menus (e.g., handing over prisoners to commander via dialog for rewards/XP, not strictly about player capture but relevant to prisoner state changes).
- Recovery/escape:
  - Post-captivity, code restores `MobileParty.MainParty.IsActive` when appropriate and resumes enlisted state checks; invalidated commander/kingdom still triggers `LeaveLordPartyAction(false)` to detach cleanly.

## Save/Load and Persistence (SAS — Decompiled specifics)
- Save definer:
  - Custom `SaveDefiner : SaveableTypeDefiner` present (ID `1436500012`). Used to register saveable types and version if needed.
- Core enlisted state:
  - `Test.SyncData(IDataStore dataStore)` persists the primary SAS state; at the top it forces `MobileParty.MainParty.IsActive = true` (safety) before syncing fields.
  - Keys: `_following_hero` (Hero reference), `_assigned_role` (current assignment), `_vassal_offers` (list of factions that already offered), and other fields in nearby behaviors as appropriate.
- Event-specific state:
  - Extortion event persists transient parties/settlement: `_deserter_event_party`, `_deserter_event_settlement`.
  - Conversations behavior persists `_last_drink` timestamps.
- Pattern:
  - Keep enlisted-critical references (commander, role, offers) in the main behavior; feature/event-specific behaviors store their own keys. On load, reapply commander follow/invisibility as usual.

## SAS Enlisted Menu (party_wait) — Layout and Options
High-level UI (as shown in the provided screenshot) is driven by the persistent `party_wait` hub:

- Header content
  - Party Leader and Objective (behavior text)
  - Enlistment Time (season/year)
  - Enlistment Tier and Formation
  - Wage
  - Current Experience and Next-Level Experience
  - Assignment description (e.g., “grunt work” → passive Athletics XP)

- Core options (representative; availability conditioned at runtime)
  - Visit Weaponsmith → opens SAS Gauntlet equipment selector via `EquipmentSelectorBehavior.CreateVMLayer(...)`
  - Train with the troops → time-boxed training; grants XP
  - Battle Commands toggle → “Player Formation Only” vs “All Formations” (two reciprocal options)
  - Talk to... → opens conversation with commander/notables for further actions
  - Show reputation with factions → navigates to `faction_reputation` menu
  - Ask commander for leave → immediate discharge path (`Test.LeaveLordPartyAction(false)`)
  - Go to the tavern → settlement interaction when in town (uses `EnterSettlementAction` and mission controllers)
  - Ask for a different assignment → `Request for reassignment` (changes role; affects daily skill XP)
  - Abandon Party → soldier exit path (contextual consequences)

Implementation details
- Menu registration: `campaignStarter.AddWaitGameMenu("party_wait", ...)` with `OnInit`, `OnCondition`, `OnTick` handlers.
- Time control while open: set `Campaign.Current.TimeControlMode = StoppablePlay`, then `args.MenuContext.GameMenu.StartWait()` and `RefreshMenuOptions(...)`.
- Equipment UI: `EquipmentSelectorBehavior.CreateVMLayer(items, slotKey)` (see SASEquipment* VM files) with time paused when the selector opens.
- Conditional enable/disable: menu options compute `args.IsEnabled` and tooltips based on commander context (e.g., tournaments available, settlement status, assignment, etc.).


