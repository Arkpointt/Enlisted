# CampaignSystem API Reference - From Decompiled Sources

**Source**: `C:\Dev\Enlisted\DECOMPILE\TaleWorlds.CampaignSystem\TaleWorlds.CampaignSystem\`

**Date**: 2025-11-25

## MapEvent APIs

### MapEventState Enum
```csharp
public enum MapEventState
{
    Begin,           // Battle just started
    Wait,            // Battle in waiting state (this is what we see in logs!)
    WaitingRemoval   // Battle finalized, waiting for removal
}
```

### MapEvent Properties

#### State
```csharp
public MapEventState State { get; private set; }
```
- **Usage**: Check current battle state
- **Values**: `Begin`, `Wait`, `WaitingRemoval`
- **Note**: Log shows "Player MapEvent State: Wait" before crash

#### IsFinalized
```csharp
public bool IsFinalized
{
    get { return this._state == MapEventState.WaitingRemoval; }
}
```
- **Usage**: Check if battle is finalized (no longer active)
- **Returns**: `true` when `State == WaitingRemoval`

#### IsSiegeAssault
```csharp
public bool IsSiegeAssault
{
    get { return this._mapEventType == MapEvent.BattleTypes.Siege; }
}
```
- **Usage**: Check if this is a siege assault battle
- **Returns**: `true` when `EventType == BattleTypes.Siege`

#### EventType
```csharp
public MapEvent.BattleTypes EventType { get; }
```
- **Values**: `None`, `FieldBattle`, `Siege`, `Raid`, `Hideout`, `SallyOut`, `SiegeOutside`, `IsForcingVolunteers`, `IsForcingSupplies`

#### HasWinner
```csharp
public bool HasWinner
{
    get { return this.BattleState == BattleState.AttackerVictory || 
                 this.BattleState == BattleState.DefenderVictory; }
}
```
- **Usage**: Check if battle has a winner

#### MapEventSettlement
```csharp
public Settlement MapEventSettlement { get; private set; }
```
- **Usage**: Get the settlement involved in the battle (if any)

### MapEvent Methods

#### BeginWait()
```csharp
public void BeginWait()
{
    this.State = MapEventState.Wait;
}
```
- **Usage**: Sets battle state to Wait
- **Note**: This is called during siege transitions!

#### FinalizeEvent()
```csharp
public void FinalizeEvent()
{
    if (this.IsFinalized) return;
    this.State = MapEventState.WaitingRemoval;
    CampaignEventDispatcher.Instance.OnMapEventEnded(this);
    // ... cleanup logic ...
}
```
- **Usage**: Finalizes the battle and fires `OnMapEventEnded` event

## PlayerEncounter APIs

### Static Properties

#### Current
```csharp
public static PlayerEncounter Current
{
    get { return Campaign.Current.PlayerEncounter; }
}
```
- **Usage**: Get current player encounter (null if no encounter)

#### LocationEncounter
```csharp
public static LocationEncounter LocationEncounter
{
    get { return Campaign.Current.LocationEncounter; }
    set { Campaign.Current.LocationEncounter = value; }
}
```
- **Usage**: Get/set location encounter (separate from PlayerEncounter.Current)

#### EncounteredParty
```csharp
public static PartyBase EncounteredParty
{
    get
    {
        if (PlayerEncounter.Current != null)
            return PlayerEncounter.Current._encounteredParty;
        return null;
    }
}
```
- **Usage**: Get the party being encountered

#### EncounteredMobileParty
```csharp
public static MobileParty EncounteredMobileParty
{
    get
    {
        PartyBase encounteredParty = PlayerEncounter.EncounteredParty;
        if (encounteredParty == null) return null;
        return encounteredParty.MobileParty;
    }
}
```
- **Usage**: Get the mobile party being encountered (if any)

#### Battle
```csharp
public static MapEvent Battle
{
    get
    {
        if (PlayerEncounter.Current == null) return null;
        return PlayerEncounter.Current._mapEvent;
    }
}
```
- **Usage**: Get the MapEvent associated with the encounter

### PlayerEncounter Methods

#### Finish()
```csharp
public static void Finish(bool forcePlayerOutFromSettlement = true)
{
    // Stops time control
    if (MobileParty.MainParty.Army == null || 
        MobileParty.MainParty.Army.LeaderParty == PlayerEncounter.EncounteredMobileParty)
    {
        Campaign.Current.TimeControlMode = CampaignTimeControlMode.Stop;
    }
    
    // Exits current menu
    if (Campaign.Current.CurrentMenuContext != null)
    {
        GameMenu.ExitToLast();
    }
    
    // ... encounter cleanup logic ...
}
```
- **Critical**: Calls `GameMenu.ExitToLast()` if menu context exists!
- **Usage**: Finish the current encounter
- **Note**: This can trigger menu transitions that cause RGL crashes

## MobileParty APIs

### Siege-Related Properties

#### SiegeEvent
```csharp
// Access via Party.SiegeEvent (PartyBase property)
public SiegeEvent SiegeEvent
{
    get
    {
        if (this.IsSettlement)
            return this.Settlement.SiegeEvent;
        return this.MobileParty.SiegeEvent;
    }
}
```
- **Usage**: Check if party is involved in a siege
- **Access**: `MobileParty.MainParty.Party.SiegeEvent` or `lordParty.Party.SiegeEvent`

#### BesiegerCamp
```csharp
// Property on MobileParty
public BesiegerCamp BesiegerCamp { get; set; }
```
- **Usage**: Check if party is besieging a settlement
- **Access**: `MobileParty.MainParty.BesiegerCamp` or `lordParty.BesiegerCamp`

#### BesiegedSettlement
```csharp
// Property on MobileParty
public Settlement BesiegedSettlement { get; set; }
```
- **Usage**: Check if party is being besieged
- **Access**: `MobileParty.MainParty.BesiegedSettlement` or `lordParty.BesiegedSettlement`

### Battle-Related Properties

#### MapEvent
```csharp
// Access via Party.MapEvent (PartyBase property)
public MapEvent MapEvent { get; }
```
- **Usage**: Get the MapEvent the party is involved in (null if not in battle)
- **Access**: `MobileParty.MainParty.Party.MapEvent` or `lordParty.Party.MapEvent`
- **Note**: This is the correct way to check if a party is in battle!

#### IsActive
```csharp
public bool IsActive { get; set; }
```
- **Usage**: Check/set if party is active on the map
- **Note**: Inactive parties don't participate in encounters

### Party Properties

#### Party (PartyBase)
```csharp
public PartyBase Party { get; }
```
- **Usage**: Access PartyBase properties (MapEvent, SiegeEvent, etc.)
- **Access**: `MobileParty.MainParty.Party.MapEvent`

## Key Insights for RGL Crash Fix

### 1. MapEvent State Transitions
- **Begin** → Battle starts
- **Wait** → Battle in waiting state (this is when crash occurs!)
- **WaitingRemoval** → Battle finalized

### 2. PlayerEncounter.Finish() Behavior
- **Calls `GameMenu.ExitToLast()`** if menu context exists
- This can trigger menu transitions during siege → encounter transition
- **Solution**: Don't call `Finish()` during siege transitions

### 3. Siege Detection
- **SiegeEvent**: `Party.SiegeEvent != null`
- **BesiegerCamp**: `MobileParty.BesiegerCamp != null`
- **BesiegedSettlement**: `MobileParty.BesiegedSettlement != null`
- **MapEvent.IsSiegeAssault**: `Party.MapEvent?.IsSiegeAssault == true`
- **MapEvent.EventType**: `Party.MapEvent?.EventType == MapEvent.BattleTypes.Siege`
- **MapEvent.State**: `Party.MapEvent?.State == MapEventState.Wait` (critical!)

### 4. Battle State Detection
- **In Battle**: `Party.MapEvent != null && !Party.MapEvent.IsFinalized`
- **Battle Finalized**: `Party.MapEvent?.IsFinalized == true`
- **Battle Has Winner**: `Party.MapEvent?.HasWinner == true`

### 5. Encounter State Detection
- **Has Encounter**: `PlayerEncounter.Current != null`
- **Has Location Encounter**: `PlayerEncounter.LocationEncounter != null`
- **Encounter During Siege**: `PlayerEncounter.Current != null && PlayerEncounter.EncounteredParty?.SiegeEvent != null`

## Recommended Siege Detection Pattern

```csharp
// Comprehensive siege detection (from decompile analysis)
var mainP = MobileParty.MainParty;
var lordP = _enlistedLord?.PartyBelongedTo;

// Check siege indicators
bool anySiegeEvent = mainP?.Party?.SiegeEvent != null || lordP?.Party?.SiegeEvent != null;
bool anyBesiegerCamp = mainP?.BesiegerCamp != null || lordP?.BesiegerCamp != null;
bool anyBesiegedSettlement = mainP?.BesiegedSettlement != null || lordP?.BesiegedSettlement != null;
bool anySiegeMapEvent = mainP?.Party?.MapEvent?.IsSiegeAssault == true || 
                        lordP?.Party?.MapEvent?.IsSiegeAssault == true ||
                        mainP?.Party?.MapEvent?.EventType == MapEvent.BattleTypes.Siege ||
                        lordP?.Party?.MapEvent?.EventType == MapEvent.BattleTypes.Siege;

// Check for PlayerEncounter during siege
bool hasPlayerEncounter = PlayerEncounter.Current != null;
bool playerEncounterSiege = hasPlayerEncounter && PlayerEncounter.EncounteredParty?.SiegeEvent != null;
bool encounterDuringSiege = hasPlayerEncounter && (anySiegeEvent || anyBesiegerCamp || anyBesiegedSettlement || anySiegeMapEvent);

// Check for MapEvent in Wait state (critical - this is when crash occurs!)
bool mapEventWaitState = (mainP?.Party?.MapEvent?.State == MapEventState.Wait || 
                          lordP?.Party?.MapEvent?.State == MapEventState.Wait) && 
                         (anySiegeEvent || anyBesiegerCamp || anyBesiegedSettlement || anySiegeMapEvent);

// Check menu state
string currentMenu = Campaign.Current?.CurrentMenuContext?.GameMenu?.StringId ?? "";
bool inSiegeMenu = currentMenu.Contains("siege") || currentMenu == "encounter";
bool inEncounterMenuWithMapEvent = currentMenu == "encounter" && 
                                    (mainP?.Party?.MapEvent != null || lordP?.Party?.MapEvent != null);

// Combined check
bool anySiege = anySiegeEvent || anyBesiegerCamp || anyBesiegedSettlement || anySiegeMapEvent || 
                playerEncounterSiege || encounterDuringSiege || mapEventWaitState || 
                inSiegeMenu || inEncounterMenuWithMapEvent;
```

## Critical Findings

1. **MapEventState.Wait is the problem state** - This is when the crash occurs
2. **PlayerEncounter.Finish() calls GameMenu.ExitToLast()** - This can trigger menu transitions
3. **MapEvent.IsFinalized** - Use this to check if battle is still active
4. **Party.MapEvent** - Correct way to access MapEvent (not directly on MobileParty)
5. **SiegeEvent is on PartyBase** - Access via `Party.SiegeEvent`

## References

- `TaleWorlds.CampaignSystem.MapEvents.MapEvent.cs`
- `TaleWorlds.CampaignSystem.MapEvents.MapEventState.cs`
- `TaleWorlds.CampaignSystem.Encounters.PlayerEncounter.cs`
- `TaleWorlds.CampaignSystem.Party.MobileParty.cs`
- `TaleWorlds.CampaignSystem.Party.PartyBase.cs`


