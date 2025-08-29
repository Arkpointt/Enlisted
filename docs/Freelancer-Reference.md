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
- Enlist trigger: dialog accept ↔ `OnEnlist()` path.
- Menus: `freelance`/`camp` ↔ our dialog-driven hub (optional lightweight menu).
- Settlement: keep native menus ↔ our current permissive approach (match).

---

## Wages, XP, and Reputation (Freelancer)
- Wages / Gold:
  - Payments are event- and role-driven (e.g., rewards for actions, promotions, bodyguarding); `GiveGoldAction.ApplyBetweenCharacters(...)` is used in reward flows rather than a flat daily stipend.
  - Soldier status displays wage and service info in the report menu; values depend on role and service level.
- Fame & Loyalty:
  - Long-term progression via `AddFameForCurrentKingdom(int)` and `AddLoyaltyToFaction(float)`; affected by kills, days served, successful protection, and service outcomes.
  - Failure conditions (e.g., letting the leader be harmed) decrease relationship and loyalty; success yields increases.
- Reputation / Relations:
  - `ChangeRelationAction.ApplyPlayerRelation(recruitedBy.LeaderHero, delta)` adjusts lord relation for protecting/failing events and for conversation-driven outcomes.
- Skill XP:
  - Activity-based (scouting, hunting, training) and battle outcomes; no single fixed daily XP pool like SAS—XP accrues from chosen actions and role utility.


