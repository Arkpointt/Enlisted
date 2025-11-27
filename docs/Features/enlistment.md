# Feature Spec: Enlistment System

## Overview
Core military service functionality that lets players enlist with any lord, follow their armies, participate in military life, earn XP and wages, and eventually retire as a veteran with benefits.

## Purpose
Provide the foundation for military service - join a lord's forces, follow them around, participate in their battles, progress through tiers, and handle all the complex edge cases that can break this (lord death, army defeat, capture, etc.).

## Inputs/Outputs

**Inputs:**
- Player dialog choice to enlist with a lord
- Lord availability and relationship status
- Current campaign state (peace/war, lord location, etc.)
- Real-time monitoring of lord and army status

**Outputs:**
- Player party hidden from map (`IsVisible = false`, Nameplate hidden)
- Player follows enlisted lord's movements  
- Daily wage payments and XP progression (+25 XP daily, +25 XP per battle)
- Participation in lord's battles and army activities
- Safe handling of service interruption (lord death, army defeat, capture)
- Veteran retirement system with per-faction tracking

## Behavior

**Enlistment Process:**
1. Talk to lord → Express interest in military service
2. Lord evaluates player (relationship, faction status)
3. Player confirms → Immediate enlistment with safety measures
4. Player party becomes invisible (`IsVisible = false`) and Nameplate removed via Patch
5. Begin following lord and receiving military benefits

**Daily Service:**
- Follow enlisted lord's army movements
- Participate in battles when lord fights (Direct join, bypassing "Help or Leave")
- Receive daily wages based on tier and performance
- Earn +25 XP daily for service, +25 XP per battle participation
- Check retirement eligibility and term completion

**Service Monitoring:**
- Continuous checking of lord status (alive, army membership, etc.)
- Automatic handling of army disbandment or lord capture
- 14-day grace period if lord dies, is captured, or army defeated
- The player clan stays inside the kingdom throughout the grace window
- While on leave or grace, enlistment requests from foreign lords are automatically declined

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

## Technical Implementation

**Files:**
- `EnlistmentBehavior.cs` - Core enlistment logic, state management, battle handling, veteran retirement
- `EncounterGuard.cs` - Utility for safe encounter state transitions
- `HidePartyNamePlatePatch.cs` - Harmony patch for UI visibility control
- `EnlistedDialogManager.cs` - Retirement and re-enlistment dialogs

**Key Mechanisms:**
```csharp
// Core enlistment tracking
private Hero _enlistedLord;
private int _enlistmentTier;
private int _enlistmentXP;
private CampaignTime _enlistmentDate;

// Veteran retirement system
private Dictionary<string, FactionVeteranRecord> _veteranRecords;
private CampaignTime _currentTermEndDate;

// Real-time monitoring (runs every frame)
CampaignEvents.TickEvent.AddNonSerializedListener(this, OnTick);

// Daily progression (runs once per game day)  
CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, OnDailyTick);

// Battle XP
AwardBattleXP(participated); // +25 XP per battle
```

**Safety Systems:**
- Lord status validation before any operations
- Graceful service termination on lord death/capture → 14-day grace period
- Army disbandment detection and handling
- Settlement/battle state awareness
- **Grace Period Shield**: After defeat, one-day ignore window prevents re-engagement

## Edge Cases

**Lord Dies During Service:**
- 14-day grace period initiated (not immediate discharge)
- Player can re-enlist with another lord in same faction
- Progress (tier, XP) preserved during grace
- Failing to re-enlist triggers desertion penalties

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

**Leave Expires:**
- If player exceeds 14-day leave limit
- Desertion penalties applied
- Service terminated

**Save/Load During Service:**
- All enlistment state persists correctly
- Veteran records saved via indexed primitive fields
- Lord references restored properly on load

**Retirement During Service:**
- Player must speak with current lord
- Dialog explains benefits and options
- Can accept retirement, re-enlist, or decide later

## Acceptance Criteria

- ✅ Can enlist with any lord that accepts player
- ✅ Player party safely hidden from map during service (UI Nameplate hidden)
- ✅ Daily wage payments (+10 base + tier bonuses)
- ✅ Daily XP progression (+25 per day)
- ✅ Battle XP (+25 per battle)
- ✅ Lord death/capture triggers 14-day grace period (not immediate discharge)
- ✅ Army disbandment detected and grace period started
- ✅ Service state persists through save/load cycles
- ✅ Veteran retirement at 252 days with full benefits
- ✅ Re-enlistment with preserved tier
- ✅ Per-faction veteran tracking
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
