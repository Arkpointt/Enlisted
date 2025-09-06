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
- Player party hidden from map (`IsActive = false`)
- Player follows enlisted lord's movements  
- Daily wage payments and XP progression
- Participation in lord's battles and army activities
- Safe handling of service interruption (lord death, army defeat)

## Behavior

**Enlistment Process:**
1. Talk to lord → Express interest in military service
2. Lord evaluates player (relationship, faction status)
3. Player confirms → Immediate enlistment with safety measures
4. Player party becomes invisible to encounter system
5. Begin following lord and receiving military benefits

**Daily Service:**
- Follow enlisted lord's army movements
- Participate in battles when lord fights
- Receive daily wages based on tier and performance
- Earn XP through military activities and time in service

**Service Monitoring:**
- Continuous checking of lord status (alive, army membership, etc.)
- Automatic handling of army disbandment or lord capture
- Emergency retirement if lord dies or becomes unavailable

## Technical Implementation

**Files:**
- `EnlistmentBehavior.cs` - Core enlistment logic and state management
- `EncounterGuard.cs` - Utility for safe encounter state transitions

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
- Temporary service suspension while lord imprisoned
- Resume service if lord escapes/released
- Retirement option if imprisonment extends too long

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
- ✅ Player party safely hidden from encounter system during service
- ✅ Daily wage payments and XP progression work correctly
- ✅ Lord death/capture handled gracefully without crashes
- ✅ Army disbandment detected and handled appropriately  
- ✅ Service state persists through save/load cycles
- ✅ Emergency retirement works when lord becomes unavailable
- ✅ No pathfinding crashes or encounter system conflicts

## Debugging

**Common Issues:**
- **Encounters still triggering**: Check `MobileParty.MainParty.IsActive` is false
- **Not following lord**: Verify army attachment and escort logic  
- **Crashes on lord death**: Check emergency retirement handlers are registered
- **Service not resuming after load**: Verify lord references restore correctly

**Log Categories:**
- "Enlistment" - Core service state and lord tracking
- "EncounterGuard" - Encounter state management
- Look for daily/tick processing in main enlisted log

**Testing:**
- Enlist with lord, check `IsActive` property in debugger
- Kill enlisted lord in console, verify graceful retirement  
- Save during service, reload, verify service resumes
- Try encounters while enlisted (should not trigger)
