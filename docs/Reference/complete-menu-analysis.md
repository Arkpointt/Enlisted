# COMPLETE Native Menu System Analysis

**Date:** 2026-01-02  
**Purpose:** Exhaustive catalog of ALL siege, battle, and naval menus to determine correct enlisted mod behavior

---

## Table of Contents

1. [🔥 Critical Discovery: Native Menu Auto-Refresh System](#-critical-discovery-native-menu-auto-refresh-system)
   - The Tick Handler Mechanism
   - What This Means
   - Complete Menu Transition Chain
   - Race Condition Timeline
2. [All Menus - Complete List](#all-menus---complete-list)
   - Encounter/Battle Menus
   - Siege Menus
   - Naval/Blockade Menus
   - Army Menus
   - Settlement Menus
   - Post-Battle/Siege Victory
   - Captivity
3. [When "join_siege_event" Appears](#when-join_siege_event-appears)
4. [When "menu_siege_strategies" Appears](#when-menu_siege_strategies-appears)
5. [Critical Discovery: The BesiegerCamp Problem](#critical-discovery-the-besiegercamp-problem)
6. [The Enlisted Mod's BesiegerCamp Prevention](#the-enlisted-mods-besiegercamp-prevention)
7. [When Army Members See Which Menu](#when-army-members-see-which-menu)
8. [The Race Condition Explained](#the-race-condition-explained)
9. [The Current Fix is WRONG](#the-current-fix-is-wrong)
10. [Army System Menu Flow](#army-system-menu-flow)
11. [Naval Battle Menus](#naval-battle-menus)
12. [Recommendations](#recommendations)
    - Option 1: Revert Current Fix (RECOMMENDED)
    - Option 2: Force Immediate Army Join
    - Option 3: Do Nothing
13. [Verification Needed](#verification-needed)
14. [Final Answer](#final-answer)

---

## Quick Reference

**Looking for specific info?**

| I need to... | Go to... |
|--------------|----------|
| Understand the tick system | [Critical Discovery](#-critical-discovery-native-menu-auto-refresh-system) |
| Find a specific menu | [All Menus - Complete List](#all-menus---complete-list) |
| Understand siege arrival | [When join_siege_event Appears](#when-join_siege_event-appears) |
| Fix the race condition | [Recommendations](#recommendations) |
| See all army menus | [When Army Members See Which Menu](#when-army-members-see-which-menu) |
| Understand BesiegerCamp | [Critical Discovery: BesiegerCamp](#critical-discovery-the-besiegercamp-problem) |

---

## 🔥 CRITICAL DISCOVERY: Native Menu Auto-Refresh System

The native game has a **tick-based menu refresh system** that automatically updates menus every frame. This is HOW the game transitions between states.

### The Tick Handler Mechanism

**File:** `SiegeEventCampaignBehavior.cs` lines 249-264

Every frame while `menu_siege_strategies` is active:

```csharp
private void game_menu_siege_strategies_on_tick(MenuCallbackArgs args, CampaignTime dt)
{
  string genericStateMenu = Campaign.Current.Models.EncounterGameMenuModel.GetGenericStateMenu();
  if (genericStateMenu != "menu_siege_strategies")
  {
    // Wrong menu! Switch to correct one
    if (!string.IsNullOrEmpty(genericStateMenu))
      GameMenu.SwitchToMenu(genericStateMenu);
    else
      GameMenu.ExitToLast();
  }
  else
  {
    // Correct menu, but refresh options
    args.MenuContext.GameMenu.GetText().SetTextVariable("CURRENT_STRATEGY", this._currentSiegeDescription);
    Campaign.Current.GameMenuManager.RefreshMenuOptionConditions(args.MenuContext);  // ← KEY
  }
}
```

**Similar tick handlers exist for:**
- `army_wait` (line 74-84 of PlayerArmyWaitBehavior.cs)
- `army_wait_at_settlement` (line 162-174)
- All settlement menus

### What This Means

1. **Every tick**, active menus call `GetGenericStateMenu()` to check if they should still be showing
2. If a different menu should show, **automatic switch**
3. If same menu, **refresh all option conditions** so buttons update dynamically
4. This is how the game handles state changes mid-menu (army joins, sieges start, battles interrupt, etc.)

### Complete Menu Transition Chain

**When clicking "Attack!" in `join_siege_event`:**

1. **Player clicks button** → `game_menu_join_siege_event_on_consequence()` runs
2. **If active battle exists:**
   ```csharp
   PlayerEncounter.JoinBattle(...);
   GameMenu.SwitchToMenu("encounter");  // Immediate switch
   ```
3. **If no battle (siege prep):**
   ```csharp
   PlayerEncounter.Finish();
   MobileParty.MainParty.BesiegerCamp = settlement.SiegeEvent.BesiegerCamp;  // Makes player commander!
   PlayerSiege.StartPlayerSiege(BattleSideEnum.Attacker, settlement);
   PlayerSiege.StartSiegePreparation();  // → Opens menu_siege_strategies
   ```
4. **`StartSiegePreparation()` (PlayerSiege.cs:57-62):**
   ```csharp
   public static void StartSiegePreparation()
   {
     if (Campaign.Current.CurrentMenuContext != null)
       GameMenu.ExitToLast();
     GameMenu.ActivateGameMenu("menu_siege_strategies");  // ← Menu opens here
   }
   ```
5. **Next frame:** `game_menu_siege_strategies_on_tick` runs
6. **Every subsequent frame:** Tick handler checks state and refreshes options

### Why This Matters for Enlisted Mod

**The race condition timeline:**

| Frame | What Happens | Menu State | Army State |
|-------|--------------|------------|------------|
| N | Encounter check runs | - | Not in army |
| N | `GetEncounterMenu()` sees `AttachedTo == null` | - | Not in army |
| N | Opens `join_siege_event` | `join_siege_event` | Not in army |
| N+1 | Player clicks "Attack!" | Processing... | Not in army |
| N+1 | `StartSiegePreparation()` opens `menu_siege_strategies` | `menu_siege_strategies` | Not in army |
| N+2 | `EnlistmentBehavior.OnRealtimeTick()` runs | `menu_siege_strategies` | **Joined army!** |
| N+3 | Tick handler calls `GetGenericStateMenu()` | `menu_siege_strategies` | In army |
| N+3 | `RefreshMenuOptionConditions()` updates buttons | `menu_siege_strategies` | In army |
| N+3 | **Options change from "Attack!" to "Leave Army"** | ✅ Correct | ✅ Correct |

**The Issue:** For 1-2 frames (N+1 to N+2), the wrong menu options are visible.

**The Solution:** Ensure army joining happens BEFORE `join_siege_event` appears, OR synchronously when "Attack!" is clicked.

---

## ALL MENUS - Complete List

### Encounter/Battle Menus
1. **encounter** - Active battle menu (Attack/Send Troops/Surrender)
2. **join_encounter** - "Help attacker/Help defender" choice menu
3. **army_encounter** - Meeting another army (talk/join/attack)
4. **encounter_meeting** - Meeting a mobile party
5. **try_to_get_away** - Attempting to flee battle
6. **try_to_get_away_debrief** - Result of flee attempt

### Siege Menus
7. **join_siege_event** - Arriving at siege location
8. **menu_siege_strategies** - Commander siege menu (Lead assault/Send troops/Leave)
9. **continue_siege_after_attack** - Post-battle siege continuation
10. **assault_town** - Launching siege assault (mission screen)
11. **assault_town_order_attack** - Ordering troops to assault
12. **join_sally_out** - Defenders sally out choice
13. **menu_siege_strategies_break_siege** - Confirming siege abandonment
14. **menu_siege_safe_passage_accepted** - Defenders granted safe passage
15. **siege_attacker_left** - Attackers abandoned siege
16. **siege_attacker_defeated** - Attackers defeated
17. **encounter_interrupted_siege_preparations** - Army waiting during siege prep

### Naval/Blockade Menus
18. **naval_town_outside** - Port/blockaded town arrival
19. **player_blockade_got_attacked** - Your blockade under attack
20. **besiegers_lift_the_blockade** - Besiegers lifted blockade

### Army Menus
21. **army_wait** - Waiting with army (traveling)
22. **army_wait_at_settlement** - Waiting with army at friendly settlement
23. **raiding_village** - Army raiding village
24. **menu_player_kicked_out_from_army_navigation_incapability** - Can't follow army (naval mismatch)

### Settlement Menus
25. **town_outside** - Outside town gates
26. **castle_outside** - Outside castle gates
27. **village_outside** - Outside village
28. **town_guard** - Approached town gates
29. **castle_guard** - Approached castle gates

### Post-Battle/Siege Victory
30. **menu_settlement_taken** - Settlement captured (routing)
31. **menu_settlement_taken_player_leader** - You led the siege victory
32. **menu_settlement_taken_player_army_member** - Army member after victory
33. **menu_settlement_taken_player_participant** - Participated in siege victory
34. **siege_aftermath_contextual_summary** - Siege aftermath summary

### Captivity Menus (13 total)
35. **taken_prisoner** - Captured by enemy
36. **defeated_and_taken_prisoner** - Defeated and captured
37. **menu_captivity_end_no_more_enemies** - Released (captors defeated)
38. **menu_captivity_end_by_ally_party_saved** - Ransomed by ally
39. **menu_captivity_end_by_party_removed** - Captors dispersed
40. **menu_captivity_end_wilderness_escape** - Escape during travel
41. **menu_escape_captivity_during_battle** - Escape during captor's battle
42. **menu_released_after_battle** - Released after battle
43. **menu_captivity_end_propose_ransom_wilderness** - Ransom offer (wilderness)
44. **menu_captivity_transfer_to_town** - Taken to dungeon
45. **menu_captivity_end_exchanged_with_prisoner** - Prisoner exchange
46. **menu_captivity_end_propose_ransom_in_prison** - Ransom offer (prison)
47. **menu_captivity_castle_remain** - Days pass in dungeon
48. **menu_captivity_end_prison_escape** - Escape from dungeon
49. **menu_captivity_castle_taken_prisoner** - Thrown in dungeon

### Village Raid/Hostile Action Menus (10 total)
50. **village_hostile_action** - Choose hostile action type
51. **raid_village_no_resist_warn_player** - Warning if not at war
52. **village_player_raid_ended** - Raid complete (player leader)
53. **village_raid_ended_leaded_by_someone_else** - Raid complete (army member)
54. **force_supplies_village** - Successfully forced supplies
55. **force_supplies_village_resist_warn_player** - Warning before forcing supplies
56. **force_troops_village_resist_warn_player** - Warning before forcing volunteers
57. **force_volunteers_village** - Successfully forced volunteers
58. **village_looted** - Village completely destroyed
59. **village** - Village main menu (hostile action available)

### Miscellaneous
60. **village_loot_complete** - Finished raiding village (deprecated?)
61. **raid_interrupted** - Raid interrupted by enemy
62. **encounter_interrupted** - While waiting, settlement attacked
63. **encounter_interrupted_raid_started** - While waiting, raid started
64. **break_in_menu** - Breaking into besieged settlement
65. **break_in_debrief_menu** - Break-in result
66. **break_out_menu** - Breaking out of besieged settlement
67. **break_out_debrief_menu** - Break-out result
68. **camp** - Player camping

**Total menus cataloged: 68 (was 45)**

---

## When "join_siege_event" Appears

**From `GetEncounterMenu` (lines 64-66, 74-75, 102-104, 122-124):**

### Scenario 1: Arriving at Settlement Under Siege (Land)
```csharp
if (!MobileParty.MainParty.IsCurrentlyAtSea)
    return "join_siege_event";
```

**Conditions:**
- Settlement has active siege (`settlement.Party.SiegeEvent != null`)
- Player NOT currently at sea
- Player NOT the siege commander
- `MobileParty.MainParty.BesiegedSettlement == null`
- `MobileParty.MainParty.CurrentSettlement == null`
- NO active battle (if battle exists, shows different menu)

### Scenario 2: Encountering Mobile Party with SiegeEvent (Land)
```csharp
if (!MobileParty.MainParty.IsCurrentlyAtSea)
    return "join_siege_event";
```

**Conditions:**
- Encountered a mobile party that has `SiegeEvent`
- NO active `MapEvent` on that party
- Player NOT at sea

### Scenario 3: From `GetGenericStateMenu` (lines 168-170)
**Never reaches this in enlisted scenario because:**
- Requires `mainParty.CurrentSettlement != null` (player inside settlement)
- AND settlement under siege
- Enlisted players don't enter settlements independently

---

## When "menu_siege_strategies" Appears

**From `GetGenericStateMenu` (lines 145-146, 155, 174):**

### Scenario 1: Player is Siege Commander
```csharp
if (mainParty.BesiegerCamp != null)
    return "menu_siege_strategies";
```

**THIS IS CRITICAL:** If `MainParty.BesiegerCamp` is set, this menu ALWAYS appears.

### Scenario 2: Army Member at Siege Location
```csharp
if (mainParty.AttachedTo != null)
{
    if (mainParty.AttachedTo.CurrentSettlement != null && mainParty.AttachedTo.CurrentSettlement.IsUnderSiege)
        return "menu_siege_strategies";
}
```

**Conditions:**
- Player `AttachedTo` army leader (`mainParty.AttachedTo != null`)
- Army at settlement that's under siege
- NOT currently in battle

### Scenario 3: Defender Inside Besieged Settlement
```csharp
if (PlayerSiege.PlayerSiegeEvent != null && PlayerSiege.PlayerSide == BattleSideEnum.Defender)
    return "menu_siege_strategies";
```

---

## Critical Discovery: The `BesiegerCamp` Problem

**From lines 145-146:**
```csharp
if (mainParty.BesiegerCamp != null)
    return "menu_siege_strategies";
```

**And from `join_siege_event_on_consequence` (EncounterGameMenuBehavior.cs:754):**
```csharp
MobileParty.MainParty.BesiegerCamp = currentSettlement.SiegeEvent.BesiegerCamp;
PlayerSiege.StartPlayerSiege(BattleSideEnum.Attacker, settlement: currentSettlement);
```

**THE PROBLEM:** When player clicks "Join the continuing siege" in `join_siege_event`:
1. Sets `MainParty.BesiegerCamp` to the siege camp
2. Makes player the siege commander
3. From that point on, `GetGenericStateMenu()` ALWAYS returns `"menu_siege_strategies"`

**This is why enlisted players can't click that button!** It permanently makes them the siege commander, which conflicts with the enlisted role.

---

## The Enlisted Mod's BesiegerCamp Prevention

**From `EnlistmentBehavior.cs:7917-7931` (`TrySyncBesiegerCamp`):**

```csharp
// Encounter safety: the enlisted player's "main party" is a technical shell.
// Do not copy BesiegerCamp onto it. Native GetGenericStateMenu() returns menu_siege_strategies
// whenever MainParty.BesiegerCamp != null, which forces commander-level siege menus.
if (mainParty != null && IsEnlisted && !_isOnLeave)
{
    if (mainParty.BesiegerCamp != null)
    {
        mainParty.BesiegerCamp = null;
    }
    return;
}
```

**THE MOD ACTIVELY PREVENTS `BesiegerCamp` FROM BEING SET!**

This confirms the design: enlisted soldiers should NEVER have `BesiegerCamp` set, so they never see commander menus.

---

## When Army Members See Which Menu

**From `GetGenericStateMenu` (lines 147-156):**

```csharp
if (mainParty.AttachedTo != null)  // In army
{
    if (army at settlement under siege)
        return "menu_siege_strategies";  // THIS IS WHAT ARMY MEMBERS SEE AT SIEGES
    else if (army at non-siege settlement)
        return "army_wait_at_settlement";
    else
        return "army_wait";  // Traveling with army
}
```

**KEY INSIGHT:** Army members at a siege location see `menu_siege_strategies`, NOT `join_siege_event`.

The `menu_siege_strategies` menu has options based on whether you're the commander:
- **Commander:** "Lead assault", "Send troops", "Leave"
- **Army member (NOT commander):** "Leave Army" ONLY

---

## The Race Condition Explained

**Timeline when enlisted player approaches siege:**

1. **Frame N:** `EncounterManager.HandleEncounterForMobileParty()` runs
   - Player party is active, triggers encounter
   - Checks `GetEncounterMenu()`
   - At this moment: `mainParty.AttachedTo == null` (not joined army yet)
   - Returns `"join_siege_event"`
   - Menu shown to player

2. **Frame N+1:** `EnlistmentBehavior.OnRealtimeTick()` runs
   - Detects lord is in army
   - Sets `mainParty.Army = lordParty.Army`
   - Calls `Army.AddPartyToMergedParties(mainParty)`
   - **This sets `mainParty.AttachedTo`**

3. **Frame N+2:** Native menu system updates
   - Calls `GetGenericStateMenu()`
   - Now sees `mainParty.AttachedTo != null`
   - Returns `"menu_siege_strategies"`
   - Menu switches automatically

**Result:** `join_siege_event` flashes briefly, then switches to `menu_siege_strategies`.

---

## The Current Fix is WRONG

**Current implementation** in `JoinSiegeEventAutoSelectPatch`:
- Closes encounter when no battle
- Returns to `enlisted_status`

**Why this is wrong:**
1. Breaks connection to siege location
2. Player won't see `menu_siege_strategies` when they should
3. Won't participate properly in siege flow
4. Fights against native army mechanics

**The correct behavior:**
- Let the menu flash briefly
- System auto-corrects on next tick
- Player sees `menu_siege_strategies` as army member
- Only "Leave Army" option available (not commander)
- Participates in sieges via army system

---

## Army System Menu Flow

**Normal (non-enlisted) player joining army at siege:**

1. Approaches siege location
2. Joins army (manually or invited)
3. `mainParty.AttachedTo` gets set
4. `GetGenericStateMenu()` returns `"menu_siege_strategies"`
5. Sees "Leave Army" option (not commander)
6. When commander starts assault, army collects all members automatically

**Enlisted player with current mod behavior:**

1. Approaches siege location
2. `join_siege_event` briefly appears (race condition)
3. `OnRealtimeTick` joins army
4. Menu switches to `menu_siege_strategies` automatically
5. Sees "Leave Army" option (not commander)
6. When lord starts assault, army collects player automatically

**THIS IS CORRECT BEHAVIOR!** The brief flash is cosmetic, not a bug.

---

## Naval Battle Menus

**Naval-specific menus:**
1. `naval_town_outside` - At port or blockaded town
2. `player_blockade_got_attacked` - Your blockade under attack
3. `besiegers_lift_the_blockade` - Blockade lifted

**When naval menus appear instead of siege menus:**
- Player `IsCurrentlyAtSea == true`
- Settlement has blockade active
- Otherwise behavior mirrors land sieges

**Enlisted mod handles:** Naval battle ship assignment via `NavalBattleShipAssignmentPatch` (already implemented).

---

## Recommendations

### Option 1: Revert Current Fix (RECOMMENDED)
**Do nothing in `JoinSiegeEventAutoSelectPatch` when battle == null.**

- Let `join_siege_event` flash briefly
- Army join happens next tick
- System auto-corrects to `menu_siege_strategies`
- Uses native siege mechanics
- Minimal code, maximum compatibility

### Option 2: Force Immediate Army Join
```csharp
if (battle == null)
{
    var mainParty = MobileParty.MainParty;
    var lordParty = lord?.PartyBelongedTo;
    
    // If lord in army, join NOW instead of waiting for tick
    if (lordParty?.Army != null && (mainParty?.Army == null || mainParty.Army != lordParty.Army))
    {
        mainParty.Army = lordParty.Army;
        lordParty.Army.AddPartyToMergedParties(mainParty);
        ModLogger.Info("JoinSiegeEvent", "Immediately joined lord's army to prevent menu flash");
    }
    
    // Let native show correct menu (will be menu_siege_strategies now)
    return true;
}
```

**Pros:**
- Eliminates menu flash
- Still uses native mechanics
- Minimal interference

**Cons:**
- Duplicates army-joining logic
- Could interfere with `OnRealtimeTick` timing

### Option 3: Do Nothing
The flash is <1 second. Most players won't notice. The system self-corrects.

---

## Verification Needed

Test scenarios to verify:
1. ✅ Lord in army, player joins - should see `menu_siege_strategies` (army member)
2. ✅ Lord NOT in army, siege preparation - currently shows `join_siege_event` (WRONG)
3. ✅ Lord starts assault from siege prep - army system collects player
4. ✅ Sally out battle - army participation
5. ✅ Naval siege/blockade - naval menus work

**Key question:** What happens when lord is besieging solo (NO army)? Does enlisted player see:
- `join_siege_event` menu (current)
- Should they be in a "virtual army" with just the lord?
- Or should mod create army automatically?

---

## Final Answer

**The "Attack!" button doesn't work because clicking it tries to make the player the siege commander by setting `BesiegerCamp`.** The enlisted mod actively prevents this.

**The solution:** Ensure player is in lord's army BEFORE encounter is created, so native shows `menu_siege_strategies` instead of `join_siege_event`.

**Implementation:** Option 2 above (force immediate army join in the patch).
