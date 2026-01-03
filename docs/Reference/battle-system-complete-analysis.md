# COMPLETE Battle & Encounter System Analysis

**Date:** 2026-01-02  
**Purpose:** Exhaustive documentation of EVERY battle/encounter menu, transition, and outcome path

---

## Table of Contents

1. [Encounter Initiation - How Battles Start](#part-1-encounter-initiation---how-battles-start)
2. [Menu Catalog - Every Battle/Encounter Menu](#part-2-menu-catalog---every-battleencounter-menu)
   - Join Decision Menus (join_encounter, join_siege_event, join_sally_out)
   - Active Battle Menu (encounter)
   - Post-Battle Menus (settlement_taken variants, aftermath, loot)
   - Defeat/Capture Menus
   - Army Coordination Menus
   - Naval Menus
   - Post-Siege Menus
3. [Battle Lifecycle - Complete Flow](#part-3-battle-lifecycle---complete-flow)
   - Path A: Field Battle
   - Path B: Siege Assault
   - Path C: Sally Out
   - Path D: Naval Battle
4. [Critical Systems](#part-4-critical-systems)
   - Menu Tick Handlers
   - GetGenericStateMenu() Priority
   - PlayerEncounter.Update() Loop
5. [Enlisted Mod Integration Points](#part-5-enlisted-mod-integration-points)
   - Current Patches
   - Missing Coverage
6. [Post-Battle Flow - UpdateInternal() State Machine](#part-6-post-battle-flow---updateinternal-state-machine)
   - State Transitions After Battle
   - Path 1: Victory Flow
   - Path 2: Defeat Flow
   - Path 3: Autosim Loss
7. [Exact Code-Verified Battle Flows](#part-7-exact-code-verified-battle-flows)
   - Flow A: Field Battle (Manual Attack)
   - Flow B: Autosim (Send Troops)
   - Flow C: Siege Assault
8. [Critical Findings for Enlisted Mod](#part-8-critical-findings-for-enlisted-mod)
   - Issue 1: "Attack!" Button Race Condition
   - Issue 2: Post-Autosim Menu
   - Issue 3: Reserve Mode During Autosim
   - Issue 4: Siege Assault "Continue siege preparations"
   - Issue 5: Naval Battles
9. [Remaining Gaps & Verification Needed](#part-9-remaining-gaps--verification-needed)

---

## Quick Reference

**Looking for specific info?**

| I need to... | Go to... |
|--------------|----------|
| Understand how battles start | [Part 1: Encounter Initiation](#part-1-encounter-initiation---how-battles-start) |
| Find a specific menu | [Part 2: Menu Catalog](#part-2-menu-catalog---every-battleencounter-menu) |
| See complete battle flow | [Part 3: Battle Lifecycle](#part-3-battle-lifecycle---complete-flow) |
| Understand post-battle processing | [Part 6: Post-Battle Flow](#part-6-post-battle-flow---updateinternal-state-machine) |
| Trace exact code paths | [Part 7: Code-Verified Flows](#part-7-exact-code-verified-battle-flows) |
| Find enlisted mod issues | [Part 8: Critical Findings](#part-8-critical-findings-for-enlisted-mod) |

---

## PART 1: ENCOUNTER INITIATION - How Battles Start

### Entry Point: EncounterManager

When parties meet on the map, the game calls:
- `EncounterManager.StartSettlementEncounter()` - for settlements
- `EncounterManager.StartPartyEncounter()` - for mobile parties
- Creates `PlayerEncounter.Current`
- Determines which menu to show via `GetEncounterMenu()`

---

## PART 2: MENU CATALOG - Every Battle/Encounter Menu

### Join Decision Menus (Choose a Side)

**1. join_encounter** - "Help X's party / Don't get involved"
- **When:** Arriving at ongoing battle between other parties
- **Options:**
  - Help {ATTACKER}
  - Help {DEFENDER}
  - Abandon army (if in army)
  - Leave / Don't get involved
- **Consequences:**
  - Help attacker → `JoinBattle(Attacker)` → switches to `encounter`
  - Help defender → `JoinBattle(Defender)` → switches to `encounter`
  - Leave → `PlayerEncounter.Finish()`

**2. join_siege_event** - Arriving at siege location
- **When:** Approaching active siege camp
- **Options:**
  - Join the continuing siege (Join attackers)
  - Assault the siege camp (Help defenders attack the besiegers)
  - Break in to help defenders
  - Don't get involved
- **Consequences:**
  - Join siege: 
    - If `MapEvent != null` → join active battle → `encounter`
    - If `MapEvent == null` → become siege commander → `menu_siege_strategies`
  - Assault siege camp → `JoinBattle(Defender)` → `encounter`
  - Break in → special break-in flow

**3. join_sally_out** - Defenders sallying out while you're inside
- **When:** Inside besieged settlement, garrison decides to sally
- **Options:**
  - Join the sally out
  - Stay in settlement
- **Consequences:**
  - Join → `JoinBattle()` → `encounter`
  - Stay → remain in settlement

### Active Battle Menu

**4. encounter** - The main battle menu
- **When:** In active battle (`MapEvent != null`)
- **Options (context-dependent):**
  - **Attack!** - Start manual battle
  - **Send Troops** - Order AI attack (autosim)
  - **Wait in Reserve** - (enlisted mod adds this)
  - **Continue siege preparations** - (if at siege)
  - **Village raid options** - (if raiding village)
  - **Capture the enemy** - (if enemy incapacitated)
  - **Try to get away** - (if losing as defender)
  - **Surrender**
  - **Leave...** - (if eligible to leave)
  - **Abandon army** - (if in army, not leader)
  - **Return to settlement** - (if sally out)

**Consequence Details:**

**Attack!** → `MenuHelper.EncounterAttackConsequence()`
```
1. Checks battle type (raid, siege assault, field battle, naval)
2. Calls appropriate mission starter:
   - Field battle → PlayerEncounter.StartBattle()
   - Siege assault → PlayerEncounter.StartSiegeAmbushMission() / StartSiegeMission()
   - Raid → PlayerEncounter.StartVillageBattleMission()
   - Naval → PlayerEncounter.StartNavalBattle()
3. Launches mission
4. AFTER mission ends → back to encounter menu OR post-battle menu
```

**Send Troops** → `MenuHelper.EncounterOrderAttackConsequence()`
```
1. Exits current menu (GameMenu.ExitToLast())
2. Calls PlayerEncounter.InitSimulation()
3. Starts autosim: MapState.StartBattleSimulation()
4. AFTER autosim → back to encounter menu with results
```

**Surrender** → `game_menu_encounter_surrender_on_consequence()`
```
1. Sets PlayerEncounter.PlayerSurrender = true
2. Calls PlayerEncounter.Update()
3. If can become prisoner → captivity menu
4. Else → menu_captivity_end_no_more_enemies
```

### Post-Battle Menus

**5. menu_settlement_taken** - Settlement captured (routing menu)
- Auto-routes to specific outcome menu based on role

**6. menu_settlement_taken_player_leader** - You led the siege victory
- **Options:**
  - Distribute loot
  - Keep settlement / Give to lord
  - Enter settlement

**7. menu_settlement_taken_player_army_member** - Army member after victory
- **Options:**
  - Wait for army leader to decide
  - Leave army

**8. menu_settlement_taken_player_participant** - Participated in siege
- **Options:**
  - Continue
  - (Limited agency)

**9. siege_aftermath_contextual_summary** - Siege aftermath
- Summary of siege results
- Continue → back to map

**10. continue_siege_after_attack** - After siege assault battle
- **Options:**
  - Continue siege preparations
- **Consequence:** `PlayerSiege.StartSiegePreparation()` → `menu_siege_strategies`

### Defeat/Capture Menus

**11. taken_prisoner** - Captured during battle
- **Options:**
  - Continue
- **Consequence:** Captivity system takes over → see Part 6B (Captivity Flow)

**12. defeated_and_taken_prisoner** - Defeated and captured
- Same as above

**Captivity Menus (13 total) - See Part 6B for complete flow:**

**13. menu_captivity_end_no_more_enemies** - Released by captors
- Captor faction defeated/eliminated
- Continue → return to map

**14. menu_captivity_end_by_ally_party_saved** - Ransomed by ally
- Friendly party paid ransom
- Continue → return to map

**15. menu_captivity_end_by_party_removed** - Captors dispersed
- Captor party destroyed
- Continue → return to map

**16. menu_captivity_end_wilderness_escape** - Escape during travel
- Random escape while being dragged around
- Continue → return to map

**17. menu_escape_captivity_during_battle** - Escape during captor's battle
- Captor enters battle, player escapes in chaos
- Continue → return to map

**18. menu_released_after_battle** - Released after battle
- Specific battle outcomes
- Continue → return to map

**19. menu_captivity_end_propose_ransom_wilderness** - Ransom offer (wilderness)
- Options: Pay ransom / Refuse
- Pay → released; Refuse → stay captive

**20. menu_captivity_transfer_to_town** - Taken to dungeon
- Captors reach settlement
- Transferred to prison

**21. menu_captivity_end_exchanged_with_prisoner** - Prisoner exchange
- Diplomatic exchange
- Continue → return to map

**22. menu_captivity_end_propose_ransom_in_prison** - Ransom offer (prison)
- Options: Pay ransom / Refuse
- Pay → released; Refuse → stay captive

**23. menu_captivity_castle_remain** - Days pass in dungeon
- Time passing in prison
- Continue captivity

**24. menu_captivity_end_prison_escape** - Escape from dungeon
- Random escape from prison
- Continue → return to map

**25. menu_captivity_castle_taken_prisoner** - Thrown in dungeon
- Captured and imprisoned
- Enter captivity cycle

### Flee/Escape Menus

**13. try_to_get_away** - Attempting to flee battle
- **When:** Losing defender tries to escape
- **Options:**
  - Go ahead with that (sacrifice troops/items)
  - Think of something else (back to encounter)
- **Consequence:** Flee calculation → try_to_get_away_debrief

**14. try_to_get_away_debrief** - Result of flee attempt
- Shows success/failure
- Continue → either escape or back to battle

### Army Coordination Menus

**15. army_encounter** - Meeting another army
- **When:** Encountering friendly/neutral army on map
- **Options:**
  - Talk to army leader
  - Talk to other members
  - Join army
  - Attack army
  - Leave
- **Consequences:**
  - Attack → starts battle encounter
  - Join → join army, switch to army_wait

**16. army_wait** - Waiting with army (not at settlement)
- **When:** In army, army is traveling/waiting
- **Options:**
  - Talk to leader
  - Manage clan parties
  - Leave army
  - Abandon army (if not at war)
- **Tick handler:** Checks if should switch to different menu

**17. army_wait_at_settlement** - Army waiting at friendly settlement
- **When:** In army, army is at settlement
- Similar options to army_wait
- **Tick handler:** May transition to siege menus if siege starts

**26. raiding_village** - Raiding a village (wait menu)
- **When:** Player or lord raiding village
- **Options:**
  - End raiding (if raid leader)
  - Leave army (if army member)
  - Abandon army (if army member)
- Raid progress tracked, transitions to loot menu

**Village Raid Menus (10 total) - See Part 6C for complete flow:**

**27. village_hostile_action** - Choose hostile action
- Options: Raid / Force volunteers / Force supplies / Forget it

**28. raid_village_no_resist_warn_player** - Warning if not at war
- Shows consequences
- Continue / Forget it

**29. village_player_raid_ended** - Raid complete (player leader)
- Shows loot and results
- Continue to map

**30. village_raid_ended_leaded_by_someone_else** - Raid complete (army member)
- Shows lord completed raid
- Continue to map

**31. force_supplies_village** - Successfully forced supplies
- Shows goods taken
- Continue

**32. force_supplies_village_resist_warn_player** - Warning before forcing supplies
- Shows consequences
- Continue / Forget it

**33. force_troops_village_resist_warn_player** - Warning before forcing volunteers
- Shows consequences
- Continue / Forget it

**34. force_volunteers_village** - Successfully forced volunteers
- Shows recruits gained
- Continue

**35. village_looted** - Village completely destroyed
- Village burnt and looted
- Leave

**36. village** - Village main menu (reference only, not battle menu)
- Main village interaction
- Hostile action option available here

### Naval Menus

**19. naval_town_outside** - Port or blockaded town
- **When:** Approaching port/blockaded settlement by sea
- **Options:**
  - Attack the blockade (help defenders)
  - Break in through blockade
  - Leave
- **Consequences:**
  - Attack blockade → naval battle → encounter
  - Break in → break-in mission

**20. player_blockade_got_attacked** - Your blockade under attack
- **When:** You're blockading, defenders attack
- **Options:**
  - Defend the blockade
  - Lift the blockade
- **Consequences:**
  - Defend → naval battle → encounter
  - Lift → abandon blockade

**21. besiegers_lift_the_blockade** - Blockade ended
- **When:** Besiegers lifted naval blockade
- **Options:**
  - Continue
- **Consequence:** Return to map

### Post-Siege Menus

**22. siege_attacker_left** - Attackers abandoned siege
- **When:** Siege ended, attackers left
- **Options:**
  - Return to settlement (if defender)
  - Leave settlement
- **Consequence:** 
  - Return → enter settlement
  - Leave → PlayerEncounter.Finish()

**23. siege_attacker_defeated** - Attackers defeated
- **When:** Siege battle won by defenders
- **Options:**
  - Return to settlement
  - Leave settlement
- Same consequences as above

**24. encounter_interrupted_siege_preparations** - Battle during siege prep
- **When:** Army waiting at siege, battle starts
- Contextual options based on situation

---

## PART 3: BATTLE LIFECYCLE - Complete Flow

### Path A: Field Battle (Army vs Army/Party)

```
1. Parties meet on map
2. EncounterManager creates encounter
3. GetEncounterMenu() returns "join_encounter" or "encounter"
4. Player chooses side (if join_encounter) → switches to "encounter"
5. Player in "encounter" menu - sees:
   - Attack!
   - Send Troops
   - Surrender
   - Leave/Abandon Army

6A. Player clicks "Attack!":
    → MenuHelper.EncounterAttackConsequence()
    → PlayerEncounter.StartBattle()
    → Mission starts (BattleState entered)
    → Battle executes
    → Mission ends
    → Returns to MapState
    → System checks MapEvent status:
       - If enemy still has troops → back to "encounter" menu
       - If enemy defeated → victory flow
       - If player defeated → defeat flow

6B. Player clicks "Send Troops":
    → MenuHelper.EncounterOrderAttackConsequence()
    → GameMenu.ExitToLast()
    → PlayerEncounter.InitSimulation()
    → MapState.StartBattleSimulation()
    → Autosim runs
    → Autosim completes
    → System opens menu based on result:
       - Victory → encounter menu or finish
       - Defeat → encounter menu (Attack/Surrender)
       - Ongoing → back to encounter menu

7. Victory:
   → MapEvent.FinishBattle()
   → System checks:
      - Settlement captured? → menu_settlement_taken
      - Raid complete? → village_loot_complete
      - Field battle? → encounter menu briefly, then auto-closes
      - Or: GetGenericStateMenu() determines next menu

8. Defeat:
   → Player captured? → taken_prisoner / defeated_and_taken_prisoner
   → Player escaped? → back to map
   → Army defeated? → depends on army status
```

### Path B: Siege Assault

```
1. Player at siege location
2. GetGenericStateMenu() returns "menu_siege_strategies"
3. Player clicks "Lead assault" or "Send troops"
4. → GameMenu.SwitchToMenu("assault_town") or "assault_town_order_attack"
5. Assault initiates:
   - Manual: PlayerEncounter.StartSiegeMission()
   - Auto: PlayerSiege orders attack

6. Battle/simulation runs

7. After assault:
   → System checks MapEvent:
      - Settlement captured? → menu_settlement_taken_*
      - Assault failed? → continue_siege_after_attack
      - Ongoing? → back to menu_siege_strategies

8. Settlement captured:
   → menu_settlement_taken routes to:
      - menu_settlement_taken_player_leader (if you're leader)
      - menu_settlement_taken_player_army_member (if army member)
      - menu_settlement_taken_player_participant (if participant)
   → siege_aftermath_contextual_summary
   → Resolution options (keep/give settlement, loot, etc.)
   → PlayerEncounter.Finish()
   → Back to map
```

### Path C: Sally Out

```
1. Defenders inside besieged settlement
2. Garrison decides to sally out
3. System opens "join_sally_out" menu
4. Player choices:
   - Join → GameMenu.ActivateGameMenu("encounter")
   - Stay → remain in settlement

5. If joined:
   → encounter menu (Attack/Send Troops/etc.)
   → Battle executes
   → After battle:
      - Victory → "encounter" menu with "Return to settlement" option
      - Defeat → captivity or defeat menus
   → Return to settlement → PlayerEncounter.LeaveEncounter → back inside
```

### Path D: Naval Battle

```
1. Player at sea, encounters blockade/port
2. GetEncounterMenu() returns "naval_town_outside" or "encounter"
3. Player attacks blockade:
   → PlayerEncounter.JoinBattle()
   → NavalBattleShipAssignmentPatch assigns ship
   → GameMenu.ActivateGameMenu("encounter")
   → encounter menu (Attack/Send Troops)

4. Player clicks "Attack!":
   → MenuHelper.EncounterAttackConsequence()
   → PlayerEncounter.StartNavalBattle()
   → Naval mission starts
   → Battle executes
   → Mission ends

5. After naval battle:
   → System checks result:
      - Blockade broken? → enter port
      - Blockade holds? → back to encounter or defeat
      - Player defeated? → captivity

6. Return to map or port access
```

---

## PART 4: CRITICAL SYSTEMS

### Menu Tick Handlers (Auto-Refresh)

Every active menu has a tick handler that runs EVERY FRAME:

**encounter menu** → (no explicit tick, but MapEvent monitoring)
**menu_siege_strategies** → checks GetGenericStateMenu(), switches if changed
**army_wait** → checks GetGenericStateMenu(), switches if changed
**army_wait_at_settlement** → checks GetGenericStateMenu(), switches if changed

### GetGenericStateMenu() Priority Order

```csharp
1. If PlayerEncounter.CurrentBattleSimulation → return null (stay in sim)
2. If Hero.MainHero.DeathMark → return null
3. If mainParty.MapEvent != null → return "encounter"
4. If mainParty.BesiegerCamp != null → return "menu_siege_strategies"
5. If mainParty.AttachedTo != null (in army):
   - If army at siege → "menu_siege_strategies"
   - If army at settlement → "army_wait_at_settlement"
   - Else → "army_wait"
6. If PlayerSiege active and defender → "menu_siege_strategies"
7. If at settlement → various settlement menus
8. Else → null (free on map)
```

### PlayerEncounter.Update() - The Core Loop

Called frequently to process encounter state:
- Checks battle status
- Applies casualties
- Determines winners
- Routes to appropriate menus
- Handles captivity
- Finalizes encounters

---

## PART 5: ENLISTED MOD INTEGRATION POINTS

### Current Patches

1. **JoinEncounterAutoSelectPatch** - Bypasses "Help X / Don't get involved"
   - Target: `join_encounter` menu
   - Forces auto-join on lord's side
   - Switches directly to `encounter` menu

2. **JoinSiegeEventAutoSelectPatch** - Handles siege arrival
   - Target: `join_siege_event` menu
   - Currently has race condition issue

3. **GenericStateMenuPatch** - Overrides for reserve mode
   - Returns `enlisted_battle_wait` when in reserve
   - Prevents native army_wait from showing

5. **AbandonArmyBlockPatch** - Hides "Abandon army" buttons
   - Multiple menus: encounter, army_wait, raiding_village

6. **EncounterLeaveSuppressionPatch** - Hides "Leave..." buttons
   - encounter menu

### Missing Coverage?

**Questions to verify:**
1. What happens if lord starts "Send Troops" while enlisted player is waiting?
2. What menu shows after enlisted player's lord wins via autosim?
3. What if enlisted player is in reserve and lord loses battle?
4. Naval battles - full enlisted flow verified?
5. Sally out - can enlisted player wait in reserve?
6. Post-siege aftermath - correct menu for enlisted army member?

---

## PART 6: POST-BATTLE FLOW - UpdateInternal() State Machine

**THE CORE:** `PlayerEncounter.UpdateInternal()` - State machine that processes ALL post-battle logic

**File:** `PlayerEncounter.cs:801-860`

```csharp
private void UpdateInternal()
{
  this._mapEvent = MapEvent.PlayerMapEvent;
  this._stateHandled = false;
  
  // State machine loop - processes until handled
  while (!this._stateHandled)
  {
    switch (this.EncounterState)
    {
      case PlayerEncounterState.Begin:          → DoBegin()
      case PlayerEncounterState.Wait:           → DoWait()
      case PlayerEncounterState.PrepareResults: → DoPrepareResults()
      case PlayerEncounterState.ApplyResults:   → DoApplyMapEventResults()
      case PlayerEncounterState.PlayerVictory:  → DoPlayerVictory()
      case PlayerEncounterState.PlayerTotalDefeat: → DoPlayerDefeat()
      case PlayerEncounterState.CaptureHeroes:  → DoCaptureHeroes()
      case PlayerEncounterState.FreeHeroes:     → DoFreeOrCapturePrisonerHeroes()
      case PlayerEncounterState.LootParty:      → DoLootParty()
      case PlayerEncounterState.LootInventory:  → DoLootInventory()
      case PlayerEncounterState.LootShips:      → DoLootShips()
      case PlayerEncounterState.End:            → DoEnd()
    }
  }
}
```

### State Transitions After Battle

**Path 1: Victory Flow**
```
1. Battle/Mission ends
2. DoWait() checks if battle should continue:
   - CheckIfBattleShouldContinueAfterBattleMission() returns true?
     → ContinueBattle() → back to encounter menu
   - Else: continue victory processing
3. State → PrepareResults
4. State → ApplyResults:
   - Calls OnPlayerBattleEnd event
   - MapEvent.CalculateAndCommitMapEventResults()
   - Determines winner
   - State → PlayerVictory
5. DoPlayerVictory():
   - Talk to helped heroes (if any)
   - State → CaptureHeroes
6. DoCaptureHeroes():
   - Talk to captured lords
   - State → FreeHeroes
7. DoFreeOrCapturePrisonerHeroes():
   - Handle rescued prisoners
   - State → LootParty
8. DoLootParty():
   - Opens party loot screen (troops/prisoners)
   - State → LootInventory
9. DoLootInventory():
   - Opens inventory loot screen (items)
   - State → LootShips
10. DoLootShips():
    - Opens port loot screen (ships/figureheads)
    - State → End
11. DoEnd():
    - Calls PlayerEncounter.Finish()
    - Checks battle type and routes to menu:
      - Siege assault victory → "menu_settlement_taken"
      - Sally out victory → "menu_settlement_taken"
      - Raid complete → "force_supplies_village" / "raiding_village"
      - Hideout cleared → hideout aftermath
      - Autosim loss → back to "encounter" menu
      - Field battle → Finish() (back to map)
```

**Path 2: Defeat Flow**
```
1. Battle/Mission ends
2. DoWait() detects defeat
3. State → ApplyResults
4. DoApplyMapEventResults():
   - State → PlayerTotalDefeat
5. DoPlayerDefeat():
   - Calls PlayerEncounter.Finish()
   - Clears BesiegerCamp
   - Opens captivity menu:
     - If surrendered → "taken_prisoner"
     - If defeated → "defeated_and_taken_prisoner"
```

**Path 3: Autosim Loss (Battle Continues)**
```
1. Autosim ends (player lost)
2. DoWait() → CheckIfBattleShouldContinueAfterBattleMission() == true
3. ContinueBattle() → back to "encounter" menu
4. Player must choose: Attack again / Send Troops / Surrender
```

---

## PART 6B: CAPTIVITY FLOW - Player Captured During Battle

### Captivity Initiation

**When player is defeated in battle:**

```
1. Battle/Mission ends (player knocked unconscious/defeated)
2. Battle outcome calculated
3. TakePrisonerAction.Apply(capturerParty, Hero.MainHero)
   ├─ Remove from party roster
   ├─ Set CaptivityStartTime = now
   ├─ Hero.ChangeState(Hero.CharacterStates.Prisoner)
   ├─ Add to captor's prison roster
   └─ PlayerCaptivity.StartCaptivity(capturerParty)

4. PlayerCaptivity.StartCaptivityInternal() [PlayerCaptivity.cs:155-173]
   ├─ Exit encounter: PlayerEncounter.LeaveEncounter = true
   ├─ Leave settlement if in one
   ├─ DISABLE MAIN PARTY: MobileParty.MainParty.IsActive = false
   ├─ Camera follows captor: captorParty.SetAsCameraFollowParty()
   ├─ Handle army:
   │  ├─ If player is army leader → Disband army
   │  └─ MobileParty.MainParty.Army = null  ← REMOVED FROM ARMY
   └─ Open captivity menu based on captor type
```

**CRITICAL:** When player is captured during enlisted battle:
- **Army membership is immediately removed** → No longer enlisted
- **Party is deactivated** → Player travels with captors
- **No special enlisted handling** → Same as independent player capture

### Captivity Daily Loop

**File:** `PlayerCaptivity.cs:218-232`

```
Every frame while captive:

1. Update() checks:
   ├─ If captor is mobile → Move MainParty to captor's position
   ├─ If captor is settlement → Move MainParty to settlement gate
   └─ If not at menu → Open appropriate captivity menu

2. CheckCaptivityChange(dt) [PlayerCaptivityCampaignBehavior]
   ├─ Roll for random events:
   │  ├─ Escape attempt (wilderness)
   │  ├─ Ransom offer
   │  ├─ Transfer to settlement
   │  ├─ Prisoner exchange
   │  └─ Execution check (if relation < -30, 2% chance)
   │
   └─ Check captor status:
      ├─ If captor defeated → Release (menu_captivity_end_no_more_enemies)
      ├─ If captor party destroyed → Escape (menu_captivity_end_by_party_removed)
      └─ If ally ransomed → Release (menu_captivity_end_by_ally_party_saved)
```

### Captivity End

**When captivity ends (any reason):**

```
PlayerCaptivity.EndCaptivityInternal() [PlayerCaptivity.cs:175-214]

1. Restore hero state
   ├─ Hero.ChangeState(Hero.CharacterStates.Active)
   ├─ Add player character to party roster
   └─ ChangePartyLeader(Hero.MainHero)

2. Exit location
   ├─ If in settlement → LeaveSettlementAction
   ├─ If in encounter → PlayerEncounter.Finish()
   └─ Else → GameMenu.ExitToLast()

3. Remove from captor
   ├─ captorParty.PrisonRoster.RemoveTroop(Hero.MainHero)
   └─ If mobile captor → TeleportPartyToOutSideOfEncounterRadius()

4. REACTIVATE PARTY
   ├─ MobileParty.MainParty.IsActive = true  ← Party reactivated
   ├─ SetAsCameraFollowParty()  ← Camera follows player again
   ├─ SetMoveModeHold()
   └─ Award XP for surviving captivity

5. Clear captivity data
   ├─ _captorParty = null
   ├─ CountOfOffers = 0
   └─ CurrentRansomAmount = 0
```

**Post-Captivity State:**
- Player is back on map
- Party is active
- **Army membership is NOT restored** → No longer enlisted
- **Enlisted mod gap:** Player must manually re-enlist

### Captivity Menu Tree

```
CAPTURED IN BATTLE
    ↓
menu_captivity_castle_taken_prisoner (thrown in dungeon)
    ↓
[WHILE CAPTIVE - Multiple possible paths:]

PATH 1: RANDOM ESCAPE (WILDERNESS)
    └─ menu_captivity_end_wilderness_escape → RELEASED

PATH 2: RANSOM OFFER (WILDERNESS)
    └─ menu_captivity_end_propose_ransom_wilderness
        ├─ Pay ransom → RELEASED
        └─ Refuse → Continue captivity

PATH 3: TRANSFERRED TO SETTLEMENT
    └─ menu_captivity_transfer_to_town
        ↓
    menu_captivity_castle_remain (days pass in dungeon)
        ↓
    [While in dungeon:]
        ├─ menu_captivity_end_prison_escape → RELEASED
        └─ menu_captivity_end_propose_ransom_in_prison
            ├─ Pay → RELEASED
            └─ Refuse → Continue

PATH 4: CAPTOR DEFEATED/ELIMINATED
    └─ menu_captivity_end_no_more_enemies → RELEASED

PATH 5: CAPTOR PARTY DESTROYED
    └─ menu_captivity_end_by_party_removed → RELEASED

PATH 6: ALLY PAYS RANSOM
    └─ menu_captivity_end_by_ally_party_saved → RELEASED

PATH 7: PRISONER EXCHANGE
    └─ menu_captivity_end_exchanged_with_prisoner → RELEASED

PATH 8: CAPTOR ENTERS BATTLE
    └─ menu_escape_captivity_during_battle → RELEASED

PATH 9: RELEASED AFTER BATTLE
    └─ menu_released_after_battle → RELEASED

PATH 10: EXECUTION (relation < -30, 2% chance)
    └─ GAME OVER
```

### Enlisted Mod Captivity Issues

**Current State:**
- ❌ No tracking of "was enlisted before capture"
- ❌ No auto-return to lord after release
- ❌ No special enlisted captivity events
- ❌ Lord doesn't attempt to ransom enlisted player

**Recommendations:**
1. **Track pre-captivity enlistment state** - Store lord + rank in saved data
2. **Post-captivity dialog** - Offer choice: "Return to lord?" vs "Go independent"
3. **Lord ransom attempts** - Friendly lord may pay ransom sooner (higher priority)
4. **Enlisted captivity events** - Special decisions during captivity

---

## PART 6C: VILLAGE RAID FLOW - Lord Raiding While Player Enlisted

### Raid Initiation

**When lord (or player) decides to raid village:**

```
1. Approach village
2. AI or player selects "Take a hostile action" from village menu
3. Choose "Raid the village"
4. If at peace → raid_village_no_resist_warn_player (warning)
5. If at war or continue → Start raid
```

### Raid Execution

**File:** `VillageHostileActionCampaignBehavior.cs:91-94`

```
RAID STARTS
    ↓
GameMenu.ActivateGameMenu("raiding_village")  [WAIT MENU]
    ↓
[Wait menu with tick handler:]

Every tick:
├─ Village HP decreases based on raiding efficiency
├─ Loot accumulates
├─ Progress bar updates
└─ Check if village HP reaches 0

Options available during raid:
├─ "End Raiding" (if raid leader only)
├─ "Leave Army" (if army member, not leader)
└─ "Abandon Army" (if army member) ← BLOCKED BY ENLISTED MOD

When raid completes (village HP = 0):
└─ OnMapEventEnded fires
```

### Raid Completion

**File:** `VillageHostileActionCampaignBehavior.cs:57-76`

```
OnMapEventEnded(MapEvent mapEvent)

1. Check if raid event
   └─ If mapEvent.IsRaid && mapEvent.IsPlayerMapEvent

2. Determine raid leader
   └─ mobileParty = mapEvent.AttackerLeader

3. Route to appropriate menu:
   ├─ IF PLAYER IS RAID LEADER:
   │  └─ GameMenu.ActivateGameMenu("village_player_raid_ended")
   │     ├─ Shows detailed loot
   │     ├─ Shows village damage
   │     └─ Relation consequences
   │
   └─ IF PLAYER IS ARMY MEMBER (ENLISTED):
      └─ GameMenu.ActivateGameMenu("village_raid_ended_leaded_by_someone_else")
         ├─ Shows raid completed under lord's leadership
         ├─ Less detailed info
         └─ Shares loot via army distribution

4. After menu closes → Return to map
   └─ Army continues normal operations
```

### Village Hostile Action Menu Tree

```
VILLAGE MENU
    ↓
"Take a hostile action" option
    ↓
village_hostile_action (choose type)
    ├─ "Raid the village"
    │  ├─ If at peace:
    │  │  └─ raid_village_no_resist_warn_player
    │  │     ├─ Continue → raiding_village
    │  │     └─ Forget it → back to village
    │  └─ If at war:
    │     └─ raiding_village (wait menu)
    │        ├─ Progress bar
    │        ├─ Options: End raiding / Leave army
    │        └─ Completes → village_player_raid_ended OR village_raid_ended_leaded_by_someone_else
    │
    ├─ "Force peasants to give you goods"
    │  ├─ force_supplies_village_resist_warn_player
    │  │  ├─ Continue → force_supplies_village
    │  │  └─ Forget it → back
    │  └─ force_supplies_village → Shows loot → back to village
    │
    ├─ "Force notables to give you recruits"
    │  ├─ force_troops_village_resist_warn_player
    │  │  ├─ Continue → force_volunteers_village
    │  │  └─ Forget it → back
    │  └─ force_volunteers_village → Shows recruits → back to village
    │
    └─ "Forget it" → back to village

COMPLETE RAID:
└─ village_looted (village completely destroyed)
    └─ Leave → back to map
```

### Enlisted Player in Village Raid

**Scenario:** Lord starts raiding while player enlisted

```
1. LORD DECIDES TO RAID (AI behavior)
   └─ Lord's army approaches village

2. LORD INITIATES RAID
   └─ Creates RaidEventComponent on settlement
   └─ All army members participate

3. MENU OPENS FOR PLAYER
   ├─ Player is army member
   └─ raiding_village opens with these options:
       ├─ "End Raiding" → HIDDEN (not raid leader)
       ├─ "Leave Army" → Available (abandon mission)
       └─ "Abandon Army" → BLOCKED BY ENLISTED MOD

4. RAID PROGRESSES
   ├─ Wait menu tick handler runs
   ├─ Village HP decreases
   ├─ Player party participates passively
   └─ Loot accumulates

5. RAID COMPLETES
   └─ OnMapEventEnded() fires
       └─ Checks: player is raid leader?
           ├─ NO (enlisted) → village_raid_ended_leaded_by_someone_else
           └─ Shows raid completed, shares loot

6. POST-RAID
   └─ Back to normal army operations
   └─ Army continues with lord
```

**Current Enlisted Mod Handling:**
- ✅ Works correctly with enlisted players
- ✅ "Abandon army" button already hidden (AbandonArmyBlockPatch)
- ✅ Player participates in raids as army member
- ✅ Correct post-raid menu appears
- ✅ Loot distributed via army system

**No changes needed** - Village raid system works as designed for enlisted players.

---

## PART 7: EXACT CODE-VERIFIED BATTLE FLOWS

### Flow A: Field Battle (Manual Attack)

```
USER CLICKS "ATTACK!" ON ENCOUNTER MENU

1. MenuHelper.EncounterAttackConsequence() [MenuHelper.cs:215-302]
   ├─ ApplyEncounterHostileAction()
   ├─ Determine battle type (field/siege/raid/naval)
   ├─ For field battle (lines 268-298):
   │  ├─ Get battle scene from MapSceneWrapper
   │  ├─ Create MissionInitializerRecord
   │  ├─ Check if naval → CampaignMission.OpenNavalBattleMission()
   │  ├─ Check if caravan → CampaignMission.OpenCaravanBattleMission()
   │  └─ Else → CampaignMission.OpenBattleMission()
   ├─ PlayerEncounter.StartAttackMission() [creates CampaignBattleResult]
   └─ MapEvent.PlayerMapEvent.BeginWait()

2. [MISSION EXECUTES - Player fights battle]

3. Mission ends → returns to MapState

4. System calls PlayerEncounter.Update() [PlayerEncounter.cs:886]
   └─ UpdateInternal() [PlayerEncounter.cs:801-860]
      ├─ State machine processes states
      ├─ DoWait() [PlayerEncounter.cs:984-1006]
      │  ├─ CheckIfBattleShouldContinueAfterBattleMission()
      │  │  └─ MapEvent.CheckIfBattleShouldContinueAfterBattleMission()
      │  │     → Returns true if enemy still has troops
      │  │     → Returns false if battle is over
      │  │
      │  ├─ IF BATTLE CONTINUES:
      │  │  └─ ContinueBattle() → encounter menu reappears
      │  │
      │  └─ IF BATTLE OVER:
      │     └─ State → PrepareResults → ApplyResults → Victory/Defeat
      │
      ├─ DoApplyMapEventResults() [PlayerEncounter.cs:1117-1127]
      │  ├─ OnPlayerBattleEnd event fires
      │  ├─ MapEvent.CalculateAndCommitMapEventResults()
      │  ├─ If player won → State = PlayerVictory
      │  └─ If player lost → State = PlayerTotalDefeat
      │
      ├─ DoPlayerVictory() [PlayerEncounter.cs:1134-1169]
      │  ├─ Talk to helped heroes
      │  └─ State → CaptureHeroes
      │
      ├─ DoCaptureHeroes() [PlayerEncounter.cs:1187-1209]
      │  ├─ Opens conversation with captured lords
      │  └─ State → FreeHeroes
      │
      ├─ DoFreeOrCapturePrisonerHeroes() [PlayerEncounter.cs:1211-1229]
      │  ├─ Handle rescued prisoners
      │  └─ State → LootParty
      │
      ├─ DoLootParty() [PlayerEncounter.cs:1260-1270]
      │  ├─ Opens PartyScreenHelper.OpenScreenAsLoot()
      │  └─ State → LootInventory
      │
      ├─ DoLootInventory() [PlayerEncounter.cs:1231-1246]
      │  ├─ Opens InventoryScreenHelper.OpenScreenAsLoot()
      │  └─ State → LootShips
      │
      ├─ DoLootShips() [PlayerEncounter.cs:1248-1258]
      │  ├─ Opens PortStateHelper.OpenAsLoot() (if naval)
      │  └─ State → End
      │
      └─ DoEnd() [PlayerEncounter.cs:1272-1384]
         ├─ Calls PlayerEncounter.Finish() [line 1292]
         ├─ Checks battle type:
         │  ├─ Siege assault → "menu_settlement_taken" [lines 1297-1302]
         │  ├─ Sally out → "menu_settlement_taken" [lines 1304-1309]
         │  ├─ Raid/Force → raid menus [lines 1325-1351]
         │  ├─ Hideout → hideout aftermath [lines 1353-1375]
         │  └─ Field battle → just Finish() (back to map)
         └─ _stateHandled = true

5. Back on campaign map (or post-battle menu)
```

### Flow B: Autosim (Send Troops)

```
USER CLICKS "SEND TROOPS" ON ENCOUNTER MENU

1. MenuHelper.EncounterOrderAttackConsequence() [MenuHelper.cs:407-410]
   └─ EncounterOrderAttack(null) [MenuHelper.cs:381-405]
      ├─ ApplyEncounterHostileAction()
      ├─ GameMenu.ExitToLast()
      ├─ PlayerEncounter.InitSimulation(null, null)
      └─ MapState.StartBattleSimulation()

2. [AUTOSIM EXECUTES - Battle simulated]

3. Autosim completes → MapState processes result

4. System calls PlayerEncounter.Update()
   └─ UpdateInternal()
      ├─ DoWait()
      │  ├─ CheckIfBattleShouldContinueAfterBattleMission()
      │  │
      │  ├─ IF PLAYER WON:
      │  │  → State → PrepareResults (victory flow continues)
      │  │
      │  └─ IF PLAYER LOST:
      │     └─ _doesBattleContinue = true
      │        → ContinueBattle()
      │        → GameMenu.SwitchToMenu("encounter")
      │        → Player sees encounter menu again
      │           (must Attack/Send Troops again/Surrender)
      │
      └─ Rest of victory flow same as manual battle
```

### Flow C: Siege Assault

```
USER AT SIEGE, CLICKS "LEAD ASSAULT" ON MENU_SIEGE_STRATEGIES

1. SiegeEventCampaignBehavior.menu_siege_strategies_lead_assault_on_consequence()
   ├─ If PlayerEncounter active → LeaveEncounter = false
   ├─ Else → EncounterManager.StartSettlementEncounter()
   └─ GameMenu.SwitchToMenu("assault_town")

2. assault_town menu opens
   └─ game_menu_town_assault_on_init() runs
      → Opens mission directly

3. MenuHelper.EncounterAttackConsequence() [called during assault]
   ├─ Detects IsSiegeAssault == true [line 230]
   ├─ PlayerSiege.StartPlayerSiege() (if needed)
   └─ PlayerSiege.StartSiegeMission(settlement) [line 259]

4. [SIEGE MISSION EXECUTES]

5. Mission ends → PlayerEncounter.Update()
   └─ UpdateInternal()
      ├─ DoWait()
      │  ├─ CheckIfBattleShouldContinueAfterBattleMission()
      │  │  → If walls still standing → return true
      │  │  → If breached to lord's hall → continue
      │  │  → If settlement captured → return false
      │  │
      │  ├─ IF ASSAULT FAILED (still has defenders):
      │  │  └─ ContinueBattle() → "encounter" menu
      │  │     → "Continue siege preparations" button visible
      │  │     → Clicking it → back to "menu_siege_strategies"
      │  │
      │  └─ IF SETTLEMENT CAPTURED:
      │     └─ DoEnd() [lines 1297-1302]:
      │        ├─ EncounterManager.StartSettlementEncounter()
      │        └─ GameMenu.SwitchToMenu("menu_settlement_taken")
      │           ↓
      │           Routes to specific aftermath menu:
      │           - menu_settlement_taken_player_leader
      │           - menu_settlement_taken_player_army_member
      │           - menu_settlement_taken_player_participant
      │
      └─ Aftermath menus handle loot, settlement fate, etc.
```

---

## PART 8: CRITICAL FINDINGS FOR ENLISTED MOD

### Issue 1: The "Attack!" Button Race Condition

**Root Cause:** Army joining happens AFTER menu is shown
- Frame N: `join_siege_event` opens (AttachedTo == null)
- Frame N+1: Player clicks "Attack!" → sets BesiegerCamp
- Frame N+2: Mod clears BesiegerCamp, army joining happens
- Result: Button doesn't work because player isn't actually commander

**Solution:** Force army join BEFORE menu opens (in patch, synchronously)

### Issue 2: Post-Autosim Menu

**Question:** What menu appears after lord does "Send Troops"?

**Answer (from code):**
```
1. Lord orders autosim
2. Autosim completes
3. UpdateInternal() → DoWait() → CheckIfBattleShouldContinueAfterBattleMission()
4. IF LORD WON:
   - Victory flow continues
   - Loot screens open
   - DoEnd() routes to appropriate menu or Finish()
5. IF LORD LOST:
   - _doesBattleContinue = true
   - ContinueBattle() switches to "encounter" menu
   - ENLISTED PLAYER SEES ENCOUNTER MENU
   - Options: Attack / Send Troops / Surrender / Wait in Reserve
```

**IMPLICATION:** Enlisted player CAN see encounter menu after lord's autosim fails!

### Issue 3: Reserve Mode During Autosim

**Current behavior:**
- Enlisted player clicks "Wait in Reserve"
- Sets `IsWaitingInReserve = true`
- Switches to `enlisted_battle_wait` menu

**Question:** What happens if lord orders autosim while player in reserve?

**Analysis:**
- Autosim runs independently
- Enlisted player remains in `enlisted_battle_wait` menu
- `GenericStateMenuPatch` prevents switching away
- POTENTIAL ISSUE: If autosim fails, does reserve end?

**Verification needed:**
- Trace what happens to reserve flag after autosim
- Check if player needs to be re-added to MapEvent
- Verify menu transitions work correctly

### Issue 4: Siege Assault "Continue siege preparations" Button

**From code (lines 158 in EncounterGameMenuBehavior):**
```csharp
gameSystemInitializer.AddGameMenuOption("encounter", "continue_preparations", 
  "{=FOoMM4AU}Continue siege preparations.", 
  game_menu_town_besiege_continue_siege_on_condition,
  game_menu_town_besiege_continue_siege_on_consequence);
```

**Consequence:**
```csharp
private void game_menu_town_besiege_continue_siege_on_consequence(MenuCallbackArgs args)
{
  PlayerEncounter.Finish();
  PlayerSiege.StartSiegePreparation(); // → Opens menu_siege_strategies
}
```

**For enlisted players:**
- After failed assault, encounter menu shows
- "Continue siege preparations" button visible
- Clicking it returns to menu_siege_strategies
- As army member, see "Leave Army" option only
- **THIS WORKS CORRECTLY** (army member flow)

### Issue 5: Naval Battles

**From EncounterAttackConsequence (lines 270-294):**
```csharp
bool isNavalEncounter = PlayerEncounter.IsNavalEncounter();
if (isNavalEncounter)
  CampaignMission.OpenNavalBattleMission(rec);
```

**Enlisted mod has:**
- `NavalBattleShipAssignmentPatch` - 6 patches for ship assignment
- Patch 1: GetSuitablePlayerShip - assigns lord's ship
- Patches 2-6: Handle various naval mission issues

**Status:** Appears fully covered

---

## PART 9: REMAINING GAPS & VERIFICATION NEEDED

### Gap 1: Autosim Failure + Reserve Mode
- [ ] What happens if lord autosims while player in reserve?
- [ ] Does reserve flag persist?
- [ ] Does player return to encounter or enlisted_battle_wait?

### Gap 2: Army Member Post-Victory
- [ ] Verify menu after army victory (field battle)
- [ ] Does army member see loot screens?
- [ ] Or does army leader handle everything?

### Gap 3: Captivity While Enlisted
- [ ] What if player captured during battle?
- [ ] Does enlisted state persist during captivity?
- [ ] Does discharge happen automatically?

### Gap 4: Settlement Taken Menus
- [ ] "menu_settlement_taken_player_army_member" - does enlisted player see this?
- [ ] What options are available?
- [ ] Can player leave army from this menu?

---

## CONCLUSION

**Complete battle system mapped:**
✅ All encounter initiation paths
✅ All menu types and transitions
✅ Complete UpdateInternal() state machine
✅ Post-battle loot/victory/defeat flows
✅ Autosim vs manual battle differences
✅ Siege assault special handling
✅ Naval battle handling

**Enlisted mod coverage:**
✅ Join encounter bypass (JoinEncounterAutoSelectPatch)
✅ Siege arrival (JoinSiegeEventAutoSelectPatch - needs fix)
✅ Victory handling (Native flow for sieges, OnMapEventEnded for field battles)
✅ Reserve mode (GenericStateMenuPatch)
✅ Menu suppression (AbandonArmyBlockPatch, EncounterLeaveSuppressionPatch)
✅ Naval battles (NavalBattleShipAssignmentPatch)

**Gaps identified:**
⚠️ Autosim failure with reserve mode interaction
⚠️ Army member loot screen participation
⚠️ Settlement captured menu for army members
⚠️ Captivity during enlistment

**Next steps:**
1. Fix siege arrival race condition (force army join)
2. Test autosim + reserve interaction
3. Verify settlement_taken menus for enlisted
4. Test captivity handling
