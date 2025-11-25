# Feature Spec: Enlistment System

## Overview
Core military service functionality that lets players enlist with any lord, follow their armies, and participate in military life while safely handling edge cases.

## Purpose
Provide the foundation for military service - join a lord's forces, follow them around, participate in their battles, and handle all the complex edge cases that can break this (lord death, army defeat, etc.).

## Inputs/Outputs

**Inputs:**
- Player dialog choice to enlist with a lord
- Lord availability and relationship status
- Current campaign state (peace/war, lord location, etc.)
- Real-time monitoring of lord and army status

**Outputs:**
- Player party hidden from map (`IsActive = false`, Nameplate hidden)
- Player follows enlisted lord's movements  
- Daily wage payments and XP progression
- Participation in lord's battles and army activities
- Safe handling of service interruption (lord death, army defeat, capture)

## Behavior

**Enlistment Process:**
1. Talk to lord → Express interest in military service
2. Lord evaluates player (relationship, faction status)
3. Player confirms → Immediate enlistment with safety measures
4. Player party becomes invisible (Nameplate removed via Patch) and inactive (`IsActive = false`)
5. Begin following lord and receiving military benefits

**Daily Service:**
- Follow enlisted lord's army movements
- Participate in battles when lord fights (Direct join, bypassing "Help or Leave")
- Receive daily wages based on tier and performance
- Earn XP through military activities and time in service

**Service Monitoring:**
- Continuous checking of lord status (alive, army membership, etc.)
- Automatic handling of army disbandment or lord capture
- Emergency retirement if lord dies or becomes unavailable
- During the 14-day grace period we remember the player’s rank XP and their last troop kit so service resumes seamlessly with another lord from the same kingdom.
- The player clan stays inside the kingdom throughout the grace window; we only force independence if the 14 days expire without re-enlisting.
- While on leave or grace, enlistment requests from foreign lords are automatically declined so the player can only report back to the rightful commander or kingdom.

## Technical Implementation

**Files:**
- `EnlistmentBehavior.cs` - Core enlistment logic, state management, and battle handling
- `EncounterGuard.cs` - Utility for safe encounter state transitions
- `HidePartyNamePlatePatch.cs` - Harmony patch for UI visibility control

**Key Mechanisms:**
```csharp
// Core enlistment tracking
private Hero _enlistedLord;
private bool _isEnlisted;

// Real-time monitoring (runs every frame)
CampaignEvents.TickEvent.AddNonSerializedListener(this, OnTick);

// Daily progression (runs once per game day)  
CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, OnDailyTick);

// Emergency handlers for lord death/capture
CampaignEvents.HeroKilledEvent.AddNonSerializedListener(this, OnHeroKilled);
```

**Safety Systems:**
- Lord status validation before any operations
- Graceful service termination on lord death/capture  
- Army disbandment detection and handling
- Settlement/battle state awareness
- **Grace Period Shield**: After defeat we finish any lingering encounter state, grant a one-day ignore window, and the encounter suppression patch honors that shield so enemies cannot re-engage immediately.

## Edge Cases

**Lord Dies During Service:**
- Immediate service termination with emergency retirement
- Player party re-enabled for normal map interaction
- Equipment retention choice offered to player
- Graceful transition back to independent status

**Army Defeated/Disbanded:**
- Detect army loss through event system
- Handle service interruption appropriately  
- Maintain player safety during chaotic situations

**Lord Captured/Imprisoned:**
- Service suspended, player enters "Desertion Grace Period" (14 days)
- Resume service if lord escapes/released or join another lord in same faction
- Retirement option if imprisonment extends too long

**Player Captured (Defeat):**
- Loss is resolved through the vanilla Attack/Surrender menu; once captured we defer teardown until the encounter closes.
- Enlistment enters a 14-day grace period instead of immediate discharge.
- If captivity lasts more than 3 in-game days, the mod forces `EndCaptivityAction.ApplyByEscape` so the player can use the grace period.
- Rejoining another lord from the same kingdom during this window restores the saved tier, XP, and troop equipment.
- Once captivity resolves we explicitly finish any leftover encounter before enabling the player, then apply the one-day grace shield so the enemy army cannot re-open the surrender menu.
- The clan remains a vassal of the grace-period kingdom; only missing the deadline applies the deserter penalties and removes the clan from the realm.
- During grace, conversations with lords from the pending kingdom expose a dedicated transfer line so the dialog acknowledges that the player is resuming service rather than enlisting fresh.

**Save/Load During Service:**
- Enlistment state persists correctly
- Lord references restored properly on load
- Service resumption if save occurred during active service

**Player in Settlement When Lord Leaves:**
- Detect separation and provide catch-up options
- Rejoin army automatically or through player choice
- Prevent getting permanently separated from enlisted lord

## Acceptance Criteria

- ✅ Can enlist with any lord that accepts player
- ✅ Player party safely hidden from encounter system during service (UI Nameplate hidden)
- ✅ Daily wage payments and XP progression work correctly
- ✅ Lord death/capture handled gracefully without crashes
- ✅ Army disbandment detected and handled appropriately  
- ✅ Service state persists through save/load cycles
- ✅ Emergency retirement works when lord becomes unavailable
- ✅ No pathfinding crashes or encounter system conflicts
- ✅ **Battle Defeat**: Native capture completes without duplicating roster entries; grace period starts afterward.
- ✅ **Battle Joining**: Correct "Encounter" menu appears, not "Help or Leave".

## Debugging

**Common Issues:**
- **Encounters still triggering**: Check `MobileParty.MainParty.IsActive` is false
- **Not following lord**: Verify army attachment and escort logic  
- **Crashes on lord death**: Check emergency retirement handlers are registered
- **Service not resuming after load**: Verify lord references restore correctly

**Log Categories:**
- "Enlistment" - Core service state and lord tracking
- "EncounterGuard" - Encounter state management
- "Battle" - Battle participation and capture logic
- Look for daily/tick processing in main enlisted log

**Testing:**
- Enlist with lord, check `IsActive` property in debugger
- Kill enlisted lord in console, verify graceful retirement  
- Save during service, reload, verify service resumes
- Try encounters while enlisted (should not trigger)
- **Defeat Test**: Join battle, lose intentionally, verify capture and grace period start.
