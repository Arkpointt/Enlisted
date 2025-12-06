# Feature Spec: Enlistment System

## Table of Contents

- [Overview](#overview)
- [Purpose](#purpose)
- [Inputs/Outputs](#inputsoutputs)
- [Behavior](#behavior)
- [Veteran Retirement System](#veteran-retirement-system)
- [Technical Implementation](#technical-implementation)
- [Edge Cases](#edge-cases)
- [Acceptance Criteria](#acceptance-criteria)
- [Debugging](#debugging)

---

## Overview
Core military service functionality that lets players enlist with any lord, follow their armies, participate in military life, earn XP and wages, and eventually retire as a veteran with benefits. Minor faction enlistment is supported with mirrored war stances and mercenary-themed dialogs.

## Purpose
Provide the foundation for military service - join a lord's forces, follow them around, participate in their battles, progress through six tiers, and handle all the complex edge cases that can break this (lord death, army defeat, capture, etc.).

## Inputs/Outputs

**Inputs:**
- Player dialog choice to enlist with a lord (kingdom or minor faction)
- Lord availability and relationship status
- Current campaign state (peace/war, lord location, etc.)
- Real-time monitoring of lord and army status

**Outputs:**
- Player joins lord's kingdom as mercenary (native mercenary income suppressed)
- Player party hidden from map (`IsVisible = false`, Nameplate hidden)
- Player follows enlisted lord's movements (including naval travel)
- Daily wage payments with detailed tooltip breakdown (mod wages only)
- XP progression: +25 daily, +25 per battle, +1 per kill
- Participation in lord's battles and army activities
- Kill tracking per faction (persists across re-enlistments)
- Safe handling of service interruption (lord death, army defeat, capture)
- Veteran retirement system with per-faction tracking
- Complete financial isolation from lord's clan finances
- No loot access (spoils go to the lord)
- No food consumption (lord provides food - party skips food calculations entirely)
- Minor faction enlistment mirrors the lord faction’s active wars to the player clan and restores them to neutral on discharge
- Minor faction desertion applies -50 relation to the lord and their clan members and blocks re-enlistment with that faction for 90 days (no crime rating)

## Behavior

**Enlistment Process:**
1. Talk to lord → Express interest in military service (kingdom flow or minor-faction mercenary flow)
2. Lord evaluates player (relationship, faction status)
3. **Army Leader Restriction**: If player is leading their own army, lord will refuse with roleplay dialog explaining they must disband their army first
4. Player confirms → Immediate enlistment with safety measures
5. Player party becomes invisible (`IsVisible = false`) and Nameplate removed via Patch
6. Begin following lord and receiving military benefits
7. **Minor factions only:** Mirror the lord faction’s current wars to the player clan so ally/enemy colors and battle joins work; relations are reverted to neutral on discharge

**Daily Service:**
- Follow enlisted lord's army movements
- Participate in battles when lord fights (Direct join, bypassing "Help or Leave")
- Receive daily wages (detailed breakdown shown in clan finance tooltip)
- Earn XP: +25 daily, +25 per battle, +1 per enemy killed
- Kills tracked per faction and term (persists on re-enlistment)
- Check retirement eligibility and term completion

**Wage Breakdown (shown in tooltip):**
- Soldier's Pay: Base wage from config (default 10)
- Combat Exp: +1 per player level
- Rank Pay: +5 per tier
- Service Seniority: +1 per 200 XP accumulated
- Army Campaign Bonus: +20% when lord is in army
- Duty Assignment: Varies by active duty (0.8x to 1.6x)

**Tier Progression (XP thresholds from progression_config.json):**
| Tier | Rank Name | XP Required |
|------|-----------|-------------|
| 1 | Levy | 0 |
| 2 | Footman | 800 |
| 3 | Serjeant | 3,000 |
| 4 | Man-at-Arms | 6,000 |
| 5 | Banner Serjeant | 11,000 |
| 6 | Household Guard | 19,000 |

Rank names are configurable in `progression_config.json`.

**Immersive Promotion Notifications:**

Each tier has unique roleplay-focused promotion text with narrative describing the ceremony and new privileges:

| Tier | Title | Key Unlocks |
|------|-------|-------------|
| 2 | "Recognized as a Soldier" | Formation selection, better equipment |
| 3 | "Rise to Serjeant" | Military professions unlock |
| 4 | "Sworn as Man-at-Arms" | Command 5 soldiers (retinue) |
| 5 | "Entrusted with the Banner" | Command 10 soldiers |
| 6 | "Welcomed to the Household" | Command 20 soldiers (max) |

Promotion popup features:
- Tier-specific title showing the ceremony name
- Narrative text describing the promotion from the lord's or serjeant's perspective
- Player name included in the narrative for immersion
- Context-aware button text ("Understood" for Tier 2-3, "To the Command Tent" for Tier 4+)
- Short chat notification for quick feedback

All promotion strings are localized in `ModuleData/Languages/enlisted_strings.xml` with IDs like `promo_title_X`, `promo_msg_X`, and `promo_chat_X`.

**Service Monitoring:**
- Continuous checking of lord status (alive, army membership, etc.)
- Automatic handling of army disbandment or lord capture
- 14-day grace period if lord dies, is captured, or army defeated (kingdom lords only)
- The player clan stays inside the kingdom throughout the grace window
- While on leave or grace, enlistment requests from foreign lords are automatically declined
- Minor faction enlistment skips grace; desertion is handled immediately with relation penalties and re-enlistment cooldown

**Service Transfer (Leave/Grace):**
- While on leave or in grace period, player can talk to other lords in the same faction
- Dialog option: "I wish to transfer my service to your command"
- Transfer preserves all progression (tier, XP, kills, service date)
- Immediately resumes active service with the new lord

**Army Cohesion Compensation:**
- Enlisted player's party doesn't negatively affect army cohesion
- Native game counts each party in army, causing cohesion penalties:
  - -1 cohesion/day per party
  - Additional penalty for parties with ≤10 healthy members
  - Additional penalty for parties with morale ≤25
- `ArmyCohesionExclusionPatch` adds compensating bonus to offset these penalties
- Compensation shown in cohesion tooltip as "Enlisted soldier (embedded)"
- Thematically correct: enlisted soldiers are embedded with their lord, not a separate party

## Veteran Retirement System

**First Term (252 days / 3 game years):**
- Notification when eligible for retirement
- Must speak with current lord to discuss options
- **Retirement benefits**: 10,000 gold, +30 relation with lord, +30 faction reputation, +15 with other lords (if respected)
- **Re-enlistment option**: 20,000 gold bonus for 1 additional year (84 days)

**Renewal Terms (84 days / 1 game year):**
- **Discharge**: 5,000 gold + 6-month (42 day) faction cooldown
- **Continue**: 5,000 gold bonus + another 1-year term

**After Cooldown:**
- Can re-enlist with same faction
- Tier/rank preserved, must re-select troop type
- 1-year term with 5,000 gold discharge at end

**Per-Faction Tracking:**
- `FactionVeteranRecord` class stores: FirstTermCompleted, PreservedTier, TotalKills, CooldownEnds, CurrentTermEnd, IsInRenewalTerm
- Each faction tracked separately - starting fresh with new factions
- TotalKills accumulated across all service terms with that faction
- Kill count preserved on re-enlistment after cooldown

**Kill Tracking:**
- `EnlistedKillTrackerBehavior` tracks kills during missions
- Registered via `SubModule.OnMissionBehaviorInitialize` for combat missions
- Kills added to `_currentTermKills` after each battle
- On retirement, term kills transfer to faction's TotalKills

**Formation Assignment (Battle Spawn):**
- `EnlistedFormationAssignmentBehavior` assigns player to their designated formation based on duty (Infantry, Ranged, Cavalry, Horse Archer)
- At Tier 4+, player commands a unified squad - all retinue soldiers and companions assigned to player's formation
- Teleports player and squad to formation position using smart spawn detection:
  - **Initial spawn**: Teleports if > 5m from formation (troops deploy near formation)
  - **Reinforcement spawn**: Always teleports (troops spawn at map edge, run onto field)
- Uses `MissionAgentSpawnLogic.IsInitialSpawnOver` to detect reinforcement phase
- Skipped in naval battles - Naval DLC has its own ship-based spawn system
- Player role set to Sergeant (can command own squad, not other formations)

## Technical Implementation

**Files:**
- `EnlistmentBehavior.cs` - Core enlistment logic, state management, battle handling, veteran retirement, service transfer, naval position sync, minor faction war mirroring, minor faction desertion cooldowns
- `EncounterGuard.cs` - Utility for safe encounter state transitions
- `HidePartyNamePlatePatch.cs` - Harmony patch for UI visibility control
- `EnlistedDialogManager.cs` - Retirement, re-enlistment, service transfer dialogs, minor faction dialog variants
- `EnlistedKillTrackerBehavior.cs` - Mission behavior for tracking player kills
- `EnlistedFormationAssignmentBehavior.cs` - Mission behavior that assigns player and squad to formation, teleports to correct position (detects reinforcement vs initial spawn, skipped in naval battles)
- `ClanFinanceEnlistmentIncomePatch.cs` - Wage breakdown in clan finance tooltip
- `MercenaryIncomeSuppressionPatch.cs` - Suppresses native mercenary income (players receive mod wages only)
- `FoodSystemPatches.cs` - Consolidated food handling: suppresses food consumption and prevents starvation when enlisted (lord provides food)
- `LootBlockPatch.cs` - Blocks all loot assignment and loot screens for enlisted soldiers
- `TownLeaveButtonPatch.cs` - Hides native Leave button in town/castle menus when enlisted
- `InfluenceMessageSuppressionPatch.cs` - Suppresses "gained 0 influence" messages
- `EnlistedWaitingPatch.cs` - Prevents game pausing when lord enters battle
- `ArmyCohesionExclusionPatch.cs` - Compensates for enlisted player's cohesion impact on army

**Key Mechanisms:**
```csharp
// Core enlistment tracking
private Hero _enlistedLord;
private int _enlistmentTier;
private int _enlistmentXP;
private CampaignTime _enlistmentDate;
private int _currentTermKills;  // Kills this service term

// Public properties for external access
public bool IsEnlisted { get; }           // True if actively enlisted (not on leave)
public bool IsOnLeave { get; }            // True if on temporary leave
public Hero EnlistedLord { get; }         // The lord the player is serving under
public CampaignTime EnlistmentDate { get; }  // When current enlistment started
public CampaignTime LeaveStartDate { get; }  // When current leave started

// Minor faction war mirroring and desertion cooldown
private List<string> _minorFactionWarRelations;                 // wars mirrored from minor faction lord
private Dictionary<string, CampaignTime> _minorFactionDesertionCooldowns; // re-enlist block per minor faction

// Veteran retirement system (per-faction)
private Dictionary<string, FactionVeteranRecord> _veteranRecords;
private Hero _savedGraceLord;  // Tracks lord during grace period for map marker cleanup

// Real-time monitoring (runs every frame)
CampaignEvents.TickEvent.AddNonSerializedListener(this, OnTick);

// Daily progression (runs once per game day)
CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, OnDailyTick);

// Battle end - awards XP and tracks kills
CampaignEvents.OnPlayerBattleEndEvent.AddNonSerializedListener(this, OnPlayerBattleEnd);

// Battle XP (in OnPlayerBattleEnd)
AwardBattleXP(kills);  // +25 participation + kills*1 XP
_currentTermKills += kills;  // Track for faction record

// Wage breakdown (in ClanFinanceEnlistmentIncomePatch)
// Shows: Base Pay, Level Bonus, Tier Bonus, Service Seniority, Army Bonus, Duty Bonus
```

**Safety Systems:**
- Lord status validation before any operations
- Graceful service termination on lord death/capture → 14-day grace period
- Army disbandment detection and handling
- Settlement/battle state awareness
- **Grace Period Shield**: After defeat, one-day ignore window prevents re-engagement
- **Visual Map Tracking**: Lord's party automatically tracked on map when enlisted, removed on discharge/leave
- **Minor Faction Support**: Veteran system now supports both Kingdoms and Clans (minor factions)

## Edge Cases

**Lord Dies During Service:**
- 14-day grace period initiated (not immediate discharge)
- Player can re-enlist with another lord in same faction
- Progress (tier, XP) preserved during grace
- Failing to re-enlist triggers desertion penalties

**Minor Factions (Mercenary Clans):**
- When enlisting with minor factions, player clan mirrors the lord faction’s active wars; nameplate colors and battle joins work as allies/enemies.
- On discharge, mirrored wars are reset to neutral.
- Deserting a minor faction applies -50 relation to the lord and their clan members and blocks re-enlistment with that faction for 90 in-game days (no crime rating).
- Minor faction lords remain `Clan.IsMinorFaction == true` even while mercenaries for a kingdom; `MapFaction` is the kingdom they serve.

**Waiting in Reserve:**
- If the player chose “Wait in Reserve” after being wounded, enlistment battle handling is skipped until the current battle series ends (prevents encounter/menu loops and duplicate XP).

**Army Defeated/Disbanded:**
- 14-day grace period initiated
- Player can find new lord in same faction
- Progress preserved

**Lord Captured/Imprisoned:**
- Service suspended, 14-day grace period
- Resume with another lord or wait for release

**Player Captured (Defeat):**
- Native capture flow completes
- Grace period starts after captivity
- One-day protection shield after release
- When captor enters a settlement, mod skips all settlement handling (`IsPrisoner` check)
- Native `PlayerCaptivity` system controls all state during captivity

**Player Captured at Sea (Naval War Expansion):**
- Native `PlayerCaptivity` system handles sea captures correctly
- Player follows captor ship via camera until captor reaches land
- The mod's 3-day forced escape (during grace period) checks `IsCurrentlyAtSea`
- If captor is at sea, forced escape is delayed until captor reaches land
- This prevents the player from being stranded at sea after escape
- Once captor reaches port/land, the forced escape triggers normally

**Leave Expires:**
- If player exceeds 14-day leave limit
- Desertion penalties applied
- Service terminated

**Voluntary Desertion:**
- Player can choose to desert at any time via the "Desert the Army" menu option
- Opens confirmation menu with roleplay explanation of consequences
- If confirmed:
  - Player keeps their current enlisted equipment (not restored to original gear)
  - -50 relation with ALL lords in the kingdom
  - +50 crime rating with the kingdom
  - Removed from kingdom (becomes independent)
  - Free to enlist with other factions afterward

**Player Leading Own Army:**
- If player is leading their own army, enlistment is blocked to prevent crashes
- Special roleplay dialog appears when attempting to enlist:
  - Lord explains that a general cannot become a foot soldier while lords still march beneath their banner
  - Player must disband their army first before enlisting
- Prevents undefined state where army members would be left without a leader

**Save/Load During Service:**
- All enlistment state persists correctly
- Veteran records saved via indexed primitive fields
- Lord references restored properly on load

**Retirement During Service:**
- Player must speak with current lord
- Dialog explains benefits and options
- Can accept retirement, re-enlist, or decide later

**Naval Travel (Naval War Expansion):**
- When lord boards a ship, player position syncs automatically
- `TrySyncNavalPosition` handles sea state and position matching
- Escort AI doesn't work across land/sea boundaries, so direct position sync is used
- Player's `IsCurrentlyAtSea` state synced with lord's state

**Army Disbanded at Sea:**
- When army disbands while at sea, player position re-syncs with lord immediately
- `OnArmyDispersed` detects naval state and teleports player to lord's position
- Player remains aboard with lord's party (not stranded)
- If service ends while at sea (e.g., lord captured), player teleported to nearest port
- `TryTeleportToNearestPort` finds closest settlement with a port and moves player there

**Naval Battles (Naval War Expansion):**
- When lord enters a naval battle, enlisted player participates as crew member
- `IsNavalMapEvent` check prevents direct `MapEventSide` join (which would crash - enlisted players have no ships)
- Player joins via `PlayerEncounter` only, spawning on lord's ship as crew
- Native Naval DLC's `GetSuitablePlayerShip()` fallback assigns players without ships to allied party ships
- No crashes when entering sea battles while enlisted

**Settlement Access (Castle/Town Menus):**
- Native Leave buttons are hidden in settlement menus while enlisted (via `TownLeaveButtonPatch`)
- "Return to camp" option added to all settlement menus to ensure player always has an exit
- Covers: `town`, `town_outside`, `castle`, `castle_outside`, `castle_guard`, `castle_enter_bribe`, `town_guard`, `town_keep_bribe`
- Prevents players from getting stuck in bribe menus (e.g., "bribe to enter keep" with no money)
- Works correctly when enlisted with minor clan parties in castles/towns

## Acceptance Criteria

- ✅ Can enlist with any lord that accepts player
- ✅ Army leader restriction prevents crash and shows roleplay dialog
- ✅ Player party safely hidden from map during service (UI Nameplate hidden)
- ✅ Daily wage payments with detailed tooltip breakdown
- ✅ Daily XP progression (+25 per day)
- ✅ Battle XP (+25 per battle, +1 per kill)
- ✅ Kill tracking per faction (persists across terms)
- ✅ Clan finances completely isolated (no lord income/expenses shown)
- ✅ Lord death/capture triggers 14-day grace period (not immediate discharge)
- ✅ Army disbandment detected and grace period started
- ✅ Service state persists through save/load cycles
- ✅ Veteran retirement at 252 days with full benefits
- ✅ Re-enlistment with preserved tier and kill count
- ✅ Per-faction veteran tracking
- ✅ Service transfer to different lord while on leave/grace (preserves all progression)
- ✅ Voluntary desertion with confirmation menu and penalties
- ✅ No pathfinding crashes or encounter system conflicts

## Debugging

**Common Issues:**
- **Encounters still triggering**: Check `MobileParty.MainParty.IsVisible` is false
- **Not following lord**: Verify escort AI and army attachment
- **Crashes on battle entry**: Mission behaviors are disabled for stability
- **Save fails**: Veteran records use manual serialization (not dictionary sync)

**Log Categories:**
- "Enlistment" - Core service state and lord tracking
- "Battle" - Battle participation and XP awards
- "Retirement" - Veteran system and term tracking
- "SaveLoad" - Save/load operations

**Testing:**
- Enlist with lord, check `IsVisible` property
- Serve 252+ days, verify retirement notification
- Save during grace period, reload, verify state preserved
- Test re-enlistment after cooldown
