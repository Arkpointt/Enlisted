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
### Wages, XP, and Reputation
- Daily tick grants wages and reputation/XP (see `Test.cs` → `TickDaily()`):
  - Wages: `GiveGoldAction.ApplyBetweenCharacters(null, Hero.MainHero, this.wage(), false)` and also to `Test.followingHero`.
  - Faction reputation: `Test.ChangeFactionRelation(Test.followingHero.MapFaction, 10)`.
  - Enlistment XP pool: `Test.xp += XPAmount` where `XPAmount = SubModule.settings?.DailyXP ?? 10`.
  - Lord relation: `Test.ChangeLordRelation(Test.followingHero, XPAmount)`.
- Role-based skill XP (see `GetXPForRole()`): adds skill XP per assignment (e.g., Athletics, Scouting, Steward, Riding, Leadership, Medicine, Engineering, Tactics) and occasionally grants focus/attribute.
- Event rewards can grant additional XP/relation (e.g., handing over prisoners → adds `Test.xp`, changes faction/lord relation proportionally to ransom value).
- Wage presentation in menu uses `this.wage()` and shows net with party wage when positive.

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
- Menu hub: `party_wait` ↔ potential optional Enlisted soldier hub (not mandatory).
- Settlement behavior: SAS forces hub; Enlisted currently does not — intentional difference.


