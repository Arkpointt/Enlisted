# Feature Spec: Enlistment System

## Table of Contents

- [Overview](#overview)
- [Purpose](#purpose)
- [Inputs/Outputs](#inputsoutputs)
- [Behavior](#behavior)
- [Discharge & Final Muster (Pending Discharge Flow)](#discharge--final-muster-pending-discharge-flow)
- [Re-entry & Reservist System](#re-entry--reservist-system)
- [Technical Implementation](#technical-implementation)
- [Edge Cases](#edge-cases)
- [Acceptance Criteria](#acceptance-criteria)
- [Debugging](#debugging)

---

## Overview
Core military service functionality that lets players enlist with any lord, follow their armies, participate in military life, earn XP and wages, and eventually retire as a veteran with benefits. Minor faction enlistment is supported with mirrored war stances and mercenary-themed dialogs.

## Purpose
Provide the foundation for military service: enlist with a lord, follow their movements, participate in battles, progress through tiers, handle edge cases safely (lord death/defeat/capture), earn wages via the muster ledger + pay muster, and leave service either by **managed discharge** (Final Muster) or **desertion** (immediate penalties).

## Inputs/Outputs

**Inputs:**
- Player dialog choice to enlist with a lord (kingdom or minor faction)
- Lord availability and relationship status
- Current campaign state (peace/war, lord location, etc.)
- Real-time monitoring of lord and army status

**Outputs:**
- Player joins lord's kingdom as mercenary (native mercenary income remains native; Enlisted wages use the muster ledger)
- Player party hidden from map (`IsVisible = false`, Nameplate hidden)
- Player follows enlisted lord's movements (including naval travel)
- Daily wage accrual into muster ledger; periodic pay muster incident (inquiry fallback) handles payouts with multiple options (Standard, Corruption Challenge, Side Deal, Final Muster/Smuggle)
- XP progression: +25 daily, +25 per battle, +1 per kill
- Participation in lord's battles and army activities
- Kill tracking per faction (persists across re-enlistments)
- Safe handling of service interruption (lord death, army defeat, capture)
- Camp-based pending discharge + Final Muster flow (managed discharge) resolves at pay muster with service-length-based rewards (Washout/Honorable/Veteran/Heroic bands), pensions, and gear handling
- No loot access (spoils go to the lord)
- No food consumption (lord provides food - party skips food calculations entirely)
- Minor faction enlistment mirrors the lord faction’s active wars to the player clan and restores them to neutral on discharge
- Minor faction desertion applies -50 relation to the lord and their clan members and blocks re-enlistment with that faction for 90 days (no crime rating)

Related systems (shipping):
- Discipline can temporarily block promotion until the player recovers (clean service / decay). (Config: `escalation.enabled`, enabled by default.)
- The enlisted status UI can show “YOUR STANDING” and “YOUR CONDITION” sections. (Configs: `escalation.enabled`, `player_conditions.enabled`, enabled by default.)

## Behavior

**Enlistment Process:**
1. Talk to lord → Express interest in military service (kingdom flow or minor-faction mercenary flow)
2. Lord evaluates player (relationship, faction status)
3. **Army Leader Restriction**: If player is leading their own army, lord will refuse with roleplay dialog explaining they must disband their army first
4. Player confirms → Immediate enlistment with safety measures
5. Player party becomes invisible (`IsVisible = false`) and Nameplate removed via Patch
6. Begin following lord and receiving military benefits
7. Bag check is deferred ~12 in-game hours after enlistment and fires as a native map incident (uses `MapState.NextIncident`, not a regular menu). It only triggers when safe (no battle/encounter/captivity) and falls back to the inquiry prompt if the incident system is unavailable; enlistment never blocks waiting for it.
   - **Stow it all (50g)**: stashes all inventory + equipped items into the baggage train and charges a **50 denar wagon fee** (clamped to what you can afford).
   - **Sell it all (60%)**: liquidates inventory + equipped items at **60%** and gives you the resulting denars.
   - **I'm keeping one thing (Roguery 30+)**: attempts to keep a single item (currently selects the highest-value item). If Roguery < 30, it is confiscated.
7. **Minor factions only:** Mirror the lord faction’s current wars to the player clan so ally/enemy colors and battle joins work; relations are reverted to neutral on discharge

**Daily Service:**
- Follow enlisted lord's army movements
- Participate in battles when lord fights (Direct join, bypassing "Help or Leave")
- Naval battles: before `PlayerEncounter.Start/Init`, the player party copies the lord's `IsCurrentlyAtSea` flag and position so naval encounters meet the engine requirement (`encounteredBattle.IsNavalMapEvent == MainParty.IsCurrentlyAtSea`) and avoid crashes.
- Accrue daily wages into the muster ledger (`_pendingMusterPay`); periodic pay muster incident (inquiry fallback) handles payouts with options: Standard Pay, Corruption Challenge (skill check), Side Deal (gear bribe), Final Muster/Smuggle (when discharge pending)
- Earn XP: +25 daily, +25 per battle, +1 per enemy killed
- Kills tracked per faction and term (persists on re-enlistment)

**Wage Breakdown (muster ledger):**
- Soldier's Pay: Base wage from config (default 10)
- Combat Exp: +1 per player level
- Rank Pay: +5 per tier
- Service Seniority: +1 per 200 XP accumulated
- Army Campaign Bonus: +20% when lord is in army
- Duty Assignment: Varies by active duty (0.8x to 1.6x)
- Probation Multiplier: Applied if on probation (reduces wage)
- Wages accrue daily into `_pendingMusterPay`; paid out at pay muster (configurable interval ~12 days with jitter)
- Daily wage is clamped after multipliers (minimum **24/day**, maximum **150/day**)

**Tier Progression:**
| Tier | XP Required | Key Features |
|------|-------------|-------------|
| 1 | 0 | Basic levy gear, Runner duty |
| 2 | 800 | Formation choice (proving event), starter duty assigned |
| 3 | 3,000 | Military professions unlock, re-entry starting tier (Honorable) |
| 4 | 6,000 | Command 5 soldiers (retinue), re-entry starting tier (Veteran/Heroic) |
| 5 | 11,000 | Command 10 soldiers |
| 6 | 19,000 | Command 20 soldiers (max) |

**Culture-Specific Ranks:**
Rank names are determined by the enlisted lord's culture:
- Empire: Tiro → Miles → Immunes → Principalis → Evocatus → Centurion
- Vlandia: Peasant → Levy → Footman → Man-at-Arms → Sergeant → Knight Bachelor
- Sturgia: Thrall → Ceorl → Fyrdman → Drengr → Huskarl → Varangian
- (See `RankHelper.cs` for all cultures)

**Proving Events & Promotion:**

Promotions are no longer automatic upon reaching XP thresholds. Players must meet multiple requirements and complete a **proving event**.

**Promotion Requirements (T2+):**
- XP threshold (varies by tier)
- Days in rank (minimum service time)
- Events completed (Lance Life events)
- Battles survived
- Lance reputation (minimum threshold)
- Discipline (not too high)
- Leader relation (for higher tiers)

**Proving Events by Tier:**
| Tier | Event | Theme |
|------|-------|-------|
| T1→T2 | "Finding Your Place" | Formation choice (Infantry/Archer/Cavalry/Horse Archer) |
| T2→T3 | "The Sergeant's Test" | Judgment and discipline |
| T3→T4 | "Crisis of Command" | Leadership under fire |
| T4→T5 | "The Lance Vote" | Earning the trust of your peers |
| T5→T6 | "Audience with the Lord" | Loyalty declaration |

**After Promotion:**
- Formation assigned (T1→T2)
- Starter duty auto-assigned based on formation
- Message prompts player to visit Quartermaster for new kit
- New equipment unlocked (shown with [NEW] indicator)

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
  - Additional penalty for starving parties
- `ArmyCohesionExclusionPatch` adds compensating bonus to offset these penalties
- Compensation also offsets starvation penalty for the enlisted party (covers timing gaps and cases where the lord temporarily has no food)
- Compensation shown in cohesion tooltip as "Enlisted soldier (embedded)"
- Thematically correct: enlisted soldiers are embedded with their lord, not a separate party

## Discharge & Final Muster (Pending Discharge Flow)

- Managed discharge is requested from **Camp ("My Camp")**. The main enlisted menu still has a **Desert the Army** option (immediate abandonment with penalties).
- Selecting "Request Discharge" sets `IsPendingDischarge = true`; discharge resolves at the next pay muster incident (Final Muster branch).
- Discharge can be cancelled via "Cancel Pending Discharge" in Camp menu before pay muster fires.
- Eligibility & scaling:
  - Track total enlisted service days; discharge rewards scale by service length (Washout <100, Honorable 100-199, Veteran/Heroic 200+).
  - Minimum check: must currently be enlisted and in good standing (no active desertion/crime penalties with the faction).
- Discharge bands (resolved at Final Muster):
  - **Washout** (<100 days): -10 lord / -10 faction leader; no pension; gear stripped (moved to inventory or deleted); probation on re-entry.
  - **Honorable** (100-199 days, relation ≥ 0): +10 lord / +5 faction leader; severance gold (config); pension 50/day; gear: keep armor (slots 6-9), remove weapons (0-3) and mount/harness (10-11) to inventory.
  - **Veteran/Heroic** (200+ days, relation ≥ 0): +30 lord / +15 faction leader; severance gold (config); pension 100/day; same gear handling as Honorable.
  - **Smuggle** (deserter path): keep all gear; crime +30; -50 relation lord/leader; no pension; probation on re-entry.
- Pension system:
  - Pauses on re-enlistment (no double-dipping); updates on next retirement to new band; no top-up option.
  - Stops if relation below threshold, at war with pension faction, or crime rating > 0.
  - Paid via daily tick hook outside clan finances (custom ledger).
- Reservist snapshot:
  - On discharge, stores service metrics (days served, tier, XP, relations, faction/lord, discharge band) to `ReservistRecord` for re-entry boosts/probation.
  - Snapshot is consumed on first re-entry with matching faction; provides tier/XP/relation bonuses or probation based on discharge band.
- UX/location:
  - Discharge request/cancel appears in Camp ("My Camp") actions; removed from main town/army menus.
  - Final Muster resolves at pay muster incident (inquiry fallback) when `IsPendingDischarge` is true.

## Re-entry & Reservist System

When re-enlisting with a faction you've previously served, your reservist record (stored on discharge) provides automatic benefits or penalties:

**Re-entry Bonuses (by Discharge Band):**
- **Washout** (<100 days) or **Deserter** (smuggle path): Start at Tier 1 (raw recruit), probation status activated (reduced wage multiplier, fatigue cap), no XP/relation bonuses.
- **Honorable** (100-199 days): Start at Tier 3 (NCO path), +500 XP bonus, +5 relation bonus with enlisted lord, no probation.
- **Veteran/Heroic** (200+ days): Start at Tier 4 (officer path), +1,000 XP bonus, +10 relation bonus with enlisted lord, no probation.

**Probation System:**
- Activated automatically on washout/deserter re-entry.
- Reduces wage multiplier (configurable, default <1.0).
- Caps maximum fatigue (configurable).
- Clears automatically on pay muster resolution or after configurable duration (`probation_days`).

**Reservist Record Consumption:**
- Snapshot is consumed (marked `Consumed = true`) on first re-entry with matching faction.
- Subsequent re-entries with the same faction do not provide bonuses (fresh start).
- Record persists across save/load; can be manually cleared if needed.

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

- **Files:**
- `EnlistmentBehavior.cs` - Core enlistment logic, state management, battle handling, retire incident entry point, service transfer, naval position sync, minor faction war mirroring, minor faction desertion cooldowns
- `EncounterGuard.cs` - Utility for safe encounter state transitions
- `HidePartyNamePlatePatch.cs` - Harmony patch for UI visibility control
- `EnlistedDialogManager.cs` - Retire dialog routing, service transfer dialogs, minor faction dialog variants
- `EnlistedIncidentsBehavior.cs` - Registers the enlistment bag-check incident and pay muster incident; schedules deferred native map incidents via `MapState.NextIncident` with inquiry fallback if the incident system is unavailable; pay muster presents options (Standard, Corruption, Side Deal, Final Muster/Smuggle)
- `EnlistedKillTrackerBehavior.cs` - Mission behavior for tracking player kills
- `EnlistedFormationAssignmentBehavior.cs` - Mission behavior that assigns player and squad to formation, teleports to correct position (detects reinforcement vs initial spawn, skipped in naval battles)
- Finance patches removed: vanilla clan finance and workshops are untouched; wages accrue to muster ledger only
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

// Wage accrual (in OnDailyTick)
// Accrues daily wages into _pendingMusterPay ledger; pay muster incident handles payouts
// Probation reduces wage multiplier; fatigue cap applies during probation
```

**Safety Systems:**
- Lord status validation before any operations
- Graceful service termination on lord death/capture → 14-day grace period
- Army disbandment detection and handling
- Settlement/battle state awareness
- **Grace Period Shield**: After defeat, one-day ignore window prevents re-engagement
- **Visual Map Tracking**: Lord's party automatically tracked on map when enlisted, removed on discharge/leave
- **Minor Faction Support**: Veteran system now supports both Kingdoms and Clans (minor factions)

**Critical Code - StopEnlist Prisoner/Battle State Check:**
```csharp
// In StopEnlist() - determines whether to activate party or defer
bool playerInMapEvent = main.Party.MapEvent != null;
bool playerInEncounter = PlayerEncounter.Current != null;
bool playerIsPrisoner = Hero.MainHero?.IsPrisoner == true;
playerInBattleState = playerInMapEvent || playerInEncounter || playerIsPrisoner;

if (playerInBattleState)
{
    // Don't fight native state management - defer activation
    main.IsActive = false;
    main.IsVisible = false;
    SchedulePostEncounterVisibilityRestore();
}
else
{
    // Safe to activate - no active encounter/battle/captivity
    main.IsVisible = true;
    main.IsActive = true;
}
```

**Critical Code - Visibility Restore Watchdog:**
```csharp
// In RestoreVisibilityAfterEncounter() - handles stuck encounters
// Wait for encounter to clear
if (PlayerEncounter.Current != null || mainParty.Party?.MapEvent != null)
{
    // Watchdog: if discharged and encounter lingers >5s with no MapEvent, force-finish it
    if ((!IsEnlisted || _isOnLeave) && !mainHero.IsPrisoner &&
        encounter != null && mainParty.Party?.MapEvent == null &&
        CampaignTime.Now - _pendingVisibilityRestoreStartTime > CampaignTime.Seconds(5L))
    {
        ForceFinishLingeringEncounter("VisibilityRestoreTimeout");
    }
    NextFrameDispatcher.RunNextFrame(RestoreVisibilityAfterEncounter, true);
    return;
}

// Don't restore while prisoner - native captivity owns party state
if (mainHero != null && mainHero.IsPrisoner)
{
    NextFrameDispatcher.RunNextFrame(RestoreVisibilityAfterEncounter, true);
    return;
}

// Safe to restore visibility
mainParty.IsActive = true;
mainParty.IsVisible = true;
```

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
- Available in field battles when player is wounded or chooses to sit out
- Player removed from MapEvent (`MapEventSide = null`) to avoid participation
- `GenericStateMenuPatch` prevents menu stutter by intercepting native menu calls
- Player can rejoin anytime - re-adds to MapEvent and activates encounter menu
- Battle handling skipped while in reserve (prevents menu loops and duplicate XP)

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
- Prisoner transfers between captors (when enemy lords meet/dialog) is expected base game behavior
- Grace period remains valid regardless of which enemy currently holds the player

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

**Voluntary Discharge:**
- Player can request discharge via "Request Discharge" in Camp ("My Camp") menu
- Sets `IsPendingDischarge = true`; discharge resolves at next pay muster (Final Muster branch)
- Can be cancelled via "Cancel Pending Discharge" before pay muster fires
- Discharge bands determined by service length (see Discharge & Final Muster section)
- Smuggle path (deserter): available at Final Muster when discharge pending; keep all gear, crime +30, -50 relation, no pension, probation on re-entry

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
- Stranded UI suppression: `RaftStateSuppressionPatch` blocks the Naval DLC stranded menu (`player_raft_state`) while enlisted if the lord/army still has ships; only shows when the lord truly has no naval capability

**Naval Battles (Naval War Expansion):**
- When lord enters a naval battle, enlisted player participates as crew member
- Sea-state sync: before `PlayerEncounter.Start/Init`, player's `IsCurrentlyAtSea` and position sync to lord
- Ship assignment via `NavalBattleShipAssignmentPatch` (5 patches):
  - **GetSuitablePlayerShip**: Assigns ship from lord's fleet when player has no ships
    - Tier 1-5: Always board lord's ship (soldiers don't command vessels)
    - Tier 6+ with retinue AND ships: Can command own vessel (rare edge case)
    - Capacity-aware: Prefers ships that fit player's party, falls back to largest
  - **GetOrderedCaptainsForPlayerTeamShips**: Assigns lord as captain for borrowed ships
  - **AllocateAndDeployInitialTroopsOfPlayerTeam**: Spawns player on friendly ship if MissionShip is null
  - **OnUnitAddedToFormationForTheFirstTime**: Prevents crash when adding AI behaviors to formations without ships (skips behavior creation, just calls `ForceCalculateCaches()`)
  - **OnShipRemoved**: Safe mission cleanup when battle ends (handles agents with null Team, prevents native crash during FadeOut)
- Player and their party spawn on assigned ship as crew
- Logs: ship name, hull health %, capacity, party size, fleet composition

**Critical Naval Fixes:**

*Formation Behavior Crash:*
The Naval DLC creates AI behaviors (like `BehaviorNavalEngageCorrespondingEnemy`) for ALL formations in a battle. When enlisted players have no ships, some formations have no ship assigned. The behavior constructor crashes when accessing null ship data. We fix this by patching `OnUnitAddedToFormationForTheFirstTime` - if no ship exists for a formation, we skip behavior creation entirely.

*Mission Cleanup Crash:*
When the battle ends, the Naval DLC's `OnShipRemoved` iterates through agents and calls `FadeOut()`. Agents in certain states (null Team, already inactive) cause a native crash. We patch `NavalTeamAgents.OnShipRemoved` to handle cleanup safely with null checks before each operation. Key detail: `DequeueReservedTroop` has multiple overloads - must specify exact signature `(NavalShipAgents)` to avoid "Ambiguous match" exception.

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
- ✅ Daily wage accrual into muster ledger; periodic pay muster incident with multiple payout options
- ✅ Daily XP progression (+25 per day)
- ✅ Battle XP (+25 per battle, +1 per kill)
- ✅ Kill tracking per faction (persists across terms)
- ✅ Vanilla clan finances untouched; muster ledger separate from clan finance UI
- ✅ Lord death/capture triggers 14-day grace period (not immediate discharge)
- ✅ Army disbandment detected and grace period started
- ✅ Service state persists through save/load cycles
- ✅ Pending discharge + Final Muster at pay muster; service-length-based rewards (Washout/Honorable/Veteran/Heroic), pensions, gear handling, reservist snapshot
- ✅ Re-enlistment with preserved tier and kill count; reservist re-entry system:
  - Washout/Deserter: Tier 1 start, probation status
  - Honorable: Tier 3 start, +500 XP, +5 relation
  - Veteran/Heroic: Tier 4 start, +1,000 XP, +10 relation
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
