# Party Attachment and Merging System - Native Implementation Analysis

## Overview

This document analyzes how Bannerlord's native code handles party attachment, escorting, and the Army system. It examines what would be required to physically merge the player's party into a lord's party instead of using the current attachment system.

## Key Concepts

### Attachment vs Army Membership

**Attachment (Physical Merging)**
- Parties physically merge positions via `MobileParty.AttachedTo`
- Attached parties move with the leader party, share the exact same position
- Attached parties participate in the same MapEvents automatically
- Properties synchronized: MapEventSide, BesiegerCamp, CurrentSettlement, IsCurrentlyAtSea
- Used when army parties get close enough to the leader

**Army Membership (Logical Grouping)**
- Parties belong to an `Army` object but remain spatially separate
- Each party moves independently toward the army leader
- Parties can be in Army without being physically attached
- Army has a `LeaderParty` and tracks all member parties in `_parties` list
- AttachedParties property on leader shows only physically merged parties

### The Three-State System

1. **In Army, Not Attached**: Party is army member, moving toward leader, spatially separate
2. **In Army, Attached**: Party is army member AND physically merged to leader (via AttachedTo)
3. **Not In Army**: Independent party

## Core Classes and Properties

### MobileParty

```csharp
// Key fields related to attachment
[CachedData] private MBList<MobileParty> _attachedParties;  // Parties attached TO this party
[SaveableField(1046)] private MobileParty _attachedTo;       // Party this is attached TO
[SaveableField(1015)] private Army _army;                    // Army membership

// Property that handles attachment logic
public MobileParty AttachedTo
{
    get => this._attachedTo;
    set
    {
        if (this._attachedTo == value) return;
        this.SetAttachedToInternal(value);
    }
}

// Read-only list of attached parties
public MBReadOnlyList<MobileParty> AttachedParties
{
    get => (MBReadOnlyList<MobileParty>) this._attachedParties;
}
```

### Army

```csharp
[SaveableField(1)] private readonly MBList<MobileParty> _parties;  // All army members
[SaveableProperty(14)] public MobileParty LeaderParty { get; private set; }

// Returns leader + all physically attached parties
public int LeaderPartyAndAttachedPartiesCount => this.LeaderParty.AttachedParties.Count + 1;

// Method to physically attach a party to the leader
public void AddPartyToMergedParties(MobileParty mobileParty)
{
    mobileParty.AttachedTo = this.LeaderParty;  // This does the physical merge
    
    if (mobileParty.IsMainParty)
    {
        // Special handling for player party
        if (GameStateManager.Current.ActiveState is MapState activeState)
            activeState.OnJoinArmy();
            
        Hero leaderHero = this.LeaderParty.LeaderHero;
        if (leaderHero != null && leaderHero != Hero.MainHero && !leaderHero.HasMet)
            leaderHero.SetHasMet();
    }
}
```

### PartyBase

```csharp
[SaveableProperty(3)] public TroopRoster MemberRoster { get; private set; }
[SaveableProperty(4)] public TroopRoster PrisonRoster { get; private set; }
[SaveableProperty(5)] public ItemRoster ItemRoster { get; private set; }
[SaveableField(200)] private MapEventSide _mapEventSide;

// MapEventSide setter synchronizes attached parties
public MapEventSide MapEventSide
{
    get => this._mapEventSide;
    set
    {
        if (this._mapEventSide == value) return;
        
        if (this._mapEventSide != null)
            this._mapEventSide.RemovePartyInternal(this);
            
        this._mapEventSide = value;
        
        if (this._mapEventSide != null)
            this._mapEventSide.AddPartyInternal(this);
            
        if (this.MobileParty != null)
        {
            // Propagate to all attached parties
            foreach (MobileParty attachedParty in this.MobileParty.AttachedParties)
                attachedParty.Party.MapEventSide = this._mapEventSide;
        }
    }
}
```

## Attachment Process (SetAttachedToInternal)

When `MobileParty.AttachedTo` is set, the following occurs:

### Detachment from Previous Party

```csharp
if (this._attachedTo != null)
{
    // Cancel any ongoing navigation transition
    if (this.IsTransitionInProgress)
        this.CancelNavigationTransitionParallel();
    
    // Remove from previous leader's attached list
    this._attachedTo.RemoveAttachedPartyInternal(this);
    
    // Clear MapEvent participation if active
    if (this.Party.MapEventSide != null && this.IsActive)
    {
        this.Party.MapEventSide.HandleMapEventEndForPartyInternal(this.Party);
        this.Party.MapEventSide = null;
    }
    
    // Clear siege participation
    if (this.BesiegerCamp != null)
        this.BesiegerCamp = null;
    
    // Reset position modifiers and movement
    this.OnAttachedToRemoved();  // Sets ArmyPositionAdder to Vec2.Zero, calls SetMoveModeHold()
}
```

### Attachment to New Party

```csharp
this._attachedTo = value;

if (this._attachedTo != null)
{
    // Add to new leader's attached list
    this._attachedTo.AddAttachedPartyInternal(this);
    
    // Synchronize MapEvent participation
    this.Party.MapEventSide = this._attachedTo.Party.MapEventSide;
    
    // Synchronize siege participation
    this.BesiegerCamp = this._attachedTo.BesiegerCamp;
    
    // Synchronize settlement status
    this.CurrentSettlement = this._attachedTo.CurrentSettlement;
    
    // Handle navigation transitions
    if (this._attachedTo.IsTransitionInProgress)
        this.NavigationTransitionStartTime = CampaignTime.Now;
    else if (this.IsTransitionInProgress)
        this.CancelNavigationTransitionParallel();
    
    // Synchronize sea/land state
    if (this.IsCurrentlyAtSea != this._attachedTo.IsCurrentlyAtSea)
        this.IsCurrentlyAtSea = this._attachedTo.IsCurrentlyAtSea;
}

this.Party.SetVisualAsDirty();  // Update visual representation
```

### Internal List Management

```csharp
private void AddAttachedPartyInternal(MobileParty mobileParty)
{
    if (this._attachedParties == null)
        this._attachedParties = new MBList<MobileParty>();
        
    this._attachedParties.Add(mobileParty);
    
    if (CampaignEventDispatcher.Instance != null)
        CampaignEventDispatcher.Instance.OnPartyAttachedAnotherParty(mobileParty);
}

private void RemoveAttachedPartyInternal(MobileParty mobileParty)
{
    this._attachedParties.Remove(mobileParty);
}
```

## Army Tick System - Auto-Attachment

The Army's `Tick` method (called every 0.1 hours) automatically attaches parties when they get close:

```csharp
private void Tick(MBCampaignEvent campaignEvent, object[] delegateParams)
{
    foreach (MobileParty party in this._parties)
    {
        // Check if party should be attached to leader
        if (party.AttachedTo == null &&                    // Not already attached
            party.Army != null &&                          // Is in this army
            party.ShortTermTargetParty == this.LeaderParty && // Moving toward leader
            party.MapEvent == null &&                      // Not in battle
            party.IsCurrentlyAtSea == this.LeaderParty.IsCurrentlyAtSea && // Same terrain type
            (party.Position - this.LeaderParty.Position).LengthSquared < 
                Campaign.Current.Models.EncounterModel.NeededMaximumDistanceForEncounteringMobileParty)
        {
            this.AddPartyToMergedParties(party);
            
            if (party.IsMainParty)
                Campaign.Current.CameraFollowParty = this.LeaderParty.Party;
                
            CampaignEventDispatcher.Instance.OnArmyOverlaySetDirty();
        }
    }
}
```

## MapEvent - Involved Parties Collection

MapEvents collect all "involved parties" which includes attached parties:

### BesiegerCamp.GetInvolvedPartiesForEventType

```csharp
public IEnumerable<PartyBase> GetInvolvedPartiesForEventType(
    MapEvent.BattleTypes mapEventType = MapEvent.BattleTypes.Siege)
{
    foreach (MobileParty besiegerParty in this._besiegerParties)
    {
        if (this.InvolveCondition(mapEventType, besiegerParty))
            yield return besiegerParty.Party;
    }
}

private bool InvolveCondition(MapEvent.BattleTypes mapEventType, MobileParty besiegerParty)
{
    // For blockades, only parties with ships can participate
    return mapEventType != MapEvent.BattleTypes.BlockadeBattle || 
           besiegerParty.HasNavalNavigationCapability;
}
```

### Settlement.GetInvolvedPartiesForEventType

```csharp
// Settlement checks its current mobile parties
public List<PartyBase> GetInvolvedPartiesForEventType(MapEvent.BattleTypes battleType)
{
    List<PartyBase> parties = new List<PartyBase>();
    
    // Iterate through all mobile parties currently at this settlement
    foreach (MobileParty party in this.Parties)
    {
        // Only include parties that meet involvement conditions
        if (ShouldPartyBeInvolved(party, battleType))
            parties.Add(party.Party);
    }
    
    return parties;
}
```

### MapEvent Initialization - Attached Parties Auto-Added

```csharp
// From MapEvent constructor logic
private void AddInsideSettlementParties(Settlement relatedSettlement)
{
    List<PartyBase> partyBaseList = new List<PartyBase>();
    
    foreach (PartyBase partyBase in relatedSettlement.GetInvolvedPartiesForEventType(this._mapEventType))
    {
        // Skip main party (handled separately) and parties attached to main party
        if (partyBase != PartyBase.MainParty && 
            partyBase.MobileParty?.AttachedTo != MobileParty.MainParty)
        {
            partyBaseList.Add(partyBase);
        }
    }
}
```

## Encounter System

### Default Encounter Model - Party Under Player Command

```csharp
public override bool IsPartyUnderPlayerCommand(PartyBase party)
{
    if (party == PartyBase.MainParty)
        return true;
        
    if (party.Side != PartyBase.MainParty.Side)
        return false;
    
    // Check various command relationships
    bool isOwnedByPlayer = party.Owner == Hero.MainHero;
    bool isPlayerKingdomLeader = party.MapFaction?.Leader == Hero.MainHero;
    bool isEscortingPlayer = party.MobileParty != null && 
                            party.MobileParty.DefaultBehavior == AiBehavior.EscortParty && 
                            party.MobileParty.TargetParty == MobileParty.MainParty;
    bool isInPlayerArmy = party.MobileParty != null && 
                         party.MobileParty.Army != null && 
                         party.MobileParty.Army.LeaderParty == MobileParty.MainParty;
    bool isPlayerSettlementOwner = party.MapEvent.MapEventSettlement != null && 
                                   party.MapEvent.MapEventSettlement.OwnerClan.Leader == Hero.MainHero;
    
    return isOwnedByPlayer || isPlayerKingdomLeader || isEscortingPlayer || 
           isInPlayerArmy || isPlayerSettlementOwner;
}
```

### Finding Parties to Join Encounter

```csharp
public override void FindNonAttachedNpcPartiesWhoWillJoinPlayerEncounter(
    List<MobileParty> partiesToJoinPlayerSide,
    List<MobileParty> partiesToJoinEnemySide)
{
    // Search radius around battle
    float radius = Campaign.Current.Models.EncounterModel.GetEncounterJoiningRadius;
    
    // Find nearby parties
    LocatableSearchData<MobileParty> data = 
        MobileParty.StartFindingLocatablesAroundPosition(position, radius);
    
    for (MobileParty nearbyParty = MobileParty.FindNextLocatable(ref data);
         nearbyParty != null;
         nearbyParty = MobileParty.FindNextLocatable(ref data))
    {
        // Only consider parties that are:
        // - Not the main party
        // - Not in a map event already
        // - Not at a settlement
        // - Not attached to another party (AttachedTo == null)
        // - Same terrain type (sea/land)
        if (nearbyParty != MobileParty.MainParty &&
            nearbyParty.MapEvent == null &&
            nearbyParty.CurrentSettlement == null &&
            nearbyParty.AttachedTo == null &&  // KEY CHECK
            nearbyParty.IsCurrentlyAtSea == requiredTerrainType)
        {
            // Check war status and add to appropriate list
            if (CanJoinPlayerSide(nearbyParty))
                partiesToJoinPlayerSide.Add(nearbyParty);
            else if (CanJoinEnemySide(nearbyParty))
                partiesToJoinEnemySide.Add(nearbyParty);
        }
    }
}
```

## AI Behavior and Attached Parties

### MobilePartyAi.TickInternal

```csharp
private void TickInternal()
{
    if (this._mobileParty.MapEvent != null || !this._mobileParty.IsActive)
        return;
    
    if (this.IsDisabled)
    {
        if (!this.EnableAgainAtHourIsPast())
            return;
        this.EnableAi();
    }
    else
    {
        // Attached parties don't run AI - they follow their leader
        if (this._mobileParty.Army != null && 
            this._mobileParty.Army.LeaderParty.AttachedParties.Contains(this._mobileParty))
            return;  // SKIP AI PROCESSING
        
        // Run normal AI for unattached parties
        AiBehavior bestAiBehavior;
        IInteractablePoint behaviorObject;
        CampaignVec2 bestTargetPoint;
        this.GetBehaviors(out bestAiBehavior, out behaviorObject, out bestTargetPoint);
        this.SetAiBehavior(bestAiBehavior, behaviorObject, bestTargetPoint);
    }
}
```

## Position and Visual Management

### Army.GetRelativePositionForParty

Calculates visual offset positions for attached parties in formation around leader:

```csharp
public Vec2 GetRelativePositionForParty(MobileParty mobileParty, Vec2 armyFacing)
{
    float spacing = 0.5f;
    
    // Calculate formation radius based on number of attached parties
    float formationRadius = 
        MathF.Ceiling(MathF.Sqrt((1.0 + 8.0 * (this.LeaderParty.AttachedParties.Count - 1))) - 1f) / 4.0 * spacing * 0.5 + spacing;
    
    // Find this party's index in attached list
    int partyIndex = -1;
    for (int i = 0; i < this.LeaderParty.AttachedParties.Count; ++i)
    {
        if (this.LeaderParty.AttachedParties[i] == mobileParty)
        {
            partyIndex = i;
            break;
        }
    }
    
    // Calculate row and column in formation grid
    int row = MathF.Ceiling((MathF.Sqrt((1.0 + 8.0 * (partyIndex + 2))) - 1.0) / 2.0) - 1;
    int column = partyIndex + 1 - row * (row + 1) / 2;
    
    // Calculate 2D offset from leader position
    bool alternateRow = (row & 1) != 0;
    int horizontalOffset = (((column & 1) != 0 ? -1 - column : column) >> 1) * (alternateRow ? -1 : 1);
    
    float verticalSpacing = 1.25f;
    
    // Apply scaling for naval parties
    if (this.LeaderParty.IsCurrentlyAtSea)
    {
        verticalSpacing *= 3f;
        spacing *= 3f;
    }
    
    // Calculate final offset with some randomization
    return new Vec2(
        (alternateRow ? -spacing * 0.5 : 0.0) + 
        horizontalOffset * spacing + 
        mobileParty.Party.RandomFloat(-0.25f, 0.25f) * 0.6 * spacing,
        (-row + mobileParty.Party.RandomFloatWithSeed(1U, -0.25f, 0.25f)) * verticalSpacing * 0.3
    );
}
```

### CurrentSettlement Property Synchronization

```csharp
public Settlement CurrentSettlement
{
    get => this._currentSettlement;
    set
    {
        if (value == this._currentSettlement)
            return;
        
        // Remove from old settlement
        if (this._currentSettlement != null)
        {
            this._currentSettlement.RemoveMobileParty(this);
            this.ArmyPositionAdder = Vec2.Zero;
        }
        
        this._currentSettlement = value;
        
        // Add to new settlement
        if (this._currentSettlement != null)
        {
            this._currentSettlement.AddMobileParty(this);
            this.Position = this.IsCurrentlyAtSea ? 
                this._currentSettlement.PortPosition : 
                this._currentSettlement.GatePosition;
            this.LastVisitedSettlement = value;
            this.EndPositionForNavigationTransition = this.Position;
        }
        
        // PROPAGATE TO ALL ATTACHED PARTIES
        foreach (MobileParty attachedParty in this._attachedParties)
            attachedParty.CurrentSettlement = value;
        
        // Visual updates
        if (this._currentSettlement != null && this._currentSettlement.IsFortification)
        {
            this.ArmyPositionAdder = Vec2.Zero;
            this.Bearing = Vec2.Zero;
            foreach (MobileParty party in this._currentSettlement.Parties)
                party.Party.SetVisualAsDirty();
        }
        
        this.Party.SetVisualAsDirty();
    }
}
```

## Roster Management (TroopRoster and ItemRoster)

### TroopRoster.Add

```csharp
// Add entire roster to this roster
public void Add(TroopRoster troopRoster)
{
    foreach (TroopRosterElement troopRosterElement in troopRoster.GetTroopRoster())
        this.Add(troopRosterElement);
}

// Add single element
public void Add(TroopRosterElement troopRosterElement)
{
    this.AddToCounts(
        troopRosterElement.Character,
        troopRosterElement.Number,
        woundedCount: troopRosterElement.WoundedNumber,
        xpChange: troopRosterElement.Xp
    );
}

// Core method to add troops
public int AddToCounts(
    CharacterObject character,
    int count,
    bool insertAtFront = false,
    int woundedCount = 0,
    int xpChange = 0,
    bool removeDepleted = true,
    int insertIndex = -1)
{
    // Find existing entry or create new
    int index = this.FindIndexOfTroop(character);
    
    if (index >= 0)
    {
        // Update existing entry
        return this.AddToCountsAtIndex(index, count, woundedCount, xpChange, removeDepleted);
    }
    else
    {
        // Create new entry
        index = this.AddNewElement(character, insertIndex);
        return this.AddToCountsAtIndex(index, count, woundedCount, xpChange, false);
    }
}
```

### TroopRoster Cache Management

```csharp
// Cached statistics updated on modification
private void InitializeCachedDataAux()
{
    this._isInitialized = true;
    int totalHeroes = 0;
    int totalWoundedHeroes = 0;
    int totalRegulars = 0;
    int totalWoundedRegulars = 0;
    
    for (int index = 0; index < this._count; ++index)
    {
        TroopRosterElement element = this.data[index];
        
        if (element.Character.IsHero)
        {
            ++totalHeroes;
            if (element.Character.HeroObject.IsWounded)
                ++totalWoundedHeroes;
        }
        else
        {
            totalRegulars += element.Number;
            totalWoundedRegulars += element.WoundedNumber;
        }
    }
    
    this._totalWoundedHeroes = totalWoundedHeroes;
    this._totalWoundedRegulars = totalWoundedRegulars;
    this._totalHeroes = totalHeroes;
    this._totalRegulars = totalRegulars;
}

// Callbacks when roster changes
public int AddToCountsAtIndex(
    int index,
    int countChange,
    int woundedCountChange = 0,
    int xpChange = 0,
    bool removeDepleted = true)
{
    // ... update counts ...
    
    // Notify owner party
    if (this.OwnerParty != null && isHero)
    {
        if (countChange > 0)
            this.OwnerParty.OnHeroAdded(character.HeroObject, this);
        else if (countChange < 0)
            this.OwnerParty.OnHeroRemoved(character.HeroObject, this);
    }
    
    if (countChange != 0)
        this.OwnerParty?.OnRosterSizeChanged(this);
    
    // Update version for cache invalidation
    if (countChange != 0 || woundedCountChange != 0)
        this.UpdateVersion();
    
    return index;
}
```

### ItemRoster.AddToCounts

```csharp
public int AddToCounts(EquipmentElement rosterElement, int number)
{
    if (number == 0)
        return -1;
    
    // Find existing item or create new entry
    int index = this.FindIndexOfElement(rosterElement);
    
    if (index < 0)
    {
        if (number < 0)
        {
            Debug.FailedAssert("Trying to delete an element from Item Roster that does not exist!");
            return -1;
        }
        
        index = this.AddNewElement(new ItemRosterElement(rosterElement, 0));
    }
    
    // Update amount
    this.OnRosterUpdated(ref this._data[index], number);
    this._data[index].Amount += number;
    
    // Remove if depleted
    if (this._data[index].Amount <= 0)
    {
        this._data[index] = this._data[this._count - 1];
        this._data[this._count - 1] = ItemRosterElement.Invalid;
        --this._count;
    }
    
    this.UpdateVersion();
    return index;
}
```

## What Would Need to Be Patched for True Party Merging

To physically merge the player party into a lord's party (moving all troops/items into the lord's party), these systems would need patching:

### 1. Party Structure and Identity

**Current**: Player party maintains separate identity, rosters, and properties even when attached
**Required**: Transfer all player party data to lord's party and make player party a "shell"

**Patches Needed**:
- `MobileParty` - New method to transfer all rosters and properties to another party
- `PartyBase.MemberRoster` - Transfer all troops
- `PartyBase.PrisonRoster` - Transfer all prisoners
- `PartyBase.ItemRoster` - Transfer all items
- `MobileParty.LeaderHero` - Handle player hero becoming a party member rather than leader
- Hero management - Player hero needs to be added to target party's MemberRoster

### 2. Army and Battle Participation

**Current**: Both parties participate in battles separately via AttachedParties system
**Required**: Only lord's party participates, with merged troops

**Patches Needed**:
- `Army.AddPartyToMergedParties` - Instead of setting AttachedTo, physically merge rosters
- `Army.OnRemovePartyInternal` - Separate player troops back out when leaving army
- `MapEvent` initialization - Don't collect player party separately
- `BesiegerCamp.GetInvolvedPartiesForEventType` - Don't include merged player party
- `Settlement.GetInvolvedPartiesForEventType` - Don't include merged player party

### 3. Speed and Movement Calculations

**Current**: Each party calculates its own speed, leader party speed affects army movement
**Required**: Lord's party speed recalculated with merged troops

**Patches Needed**:
- `DefaultPartySpeedCalculatingModel.CalculateFinalSpeed` - Account for merged troops
- `DefaultPartySpeedCalculatingModel.CalculateBaseSpeed` - Include merged party sizes
- Party size modifiers - Merge affects herd/wounded penalties
- Party speed cache invalidation when parties merge/separate

### 4. AI and Behavior

**Current**: Attached parties have AI disabled, follow leader automatically
**Required**: Player retains control but physically part of lord's party

**Patches Needed**:
- `MobilePartyAi.TickInternal` - Handle merged player party specially
- Player input processing - Interpret commands as orders to the lord's party
- `DefaultEncounterModel.IsPartyUnderPlayerCommand` - Recognize merged state
- Party targeting - Enemies target lord's party, not separate player party

### 5. Visual Representation

**Current**: Attached parties shown in formation around leader with visual offsets
**Required**: Only lord's party visible on map, player party not rendered

**Patches Needed**:
- `Army.GetRelativePositionForParty` - Don't calculate position for merged player party
- Party visibility checks - Hide merged player party from map
- Party rendering - Don't render merged player party icon
- Camera following - May need to follow lord's party instead

### 6. Encounter and Menu Systems

**Current**: Menus and encounters detect player party separately
**Required**: Recognize when player is part of lord's party

**Patches Needed**:
- `PlayerEncounter` - Check if player is merged into another party
- `EncounterGameMenuBehavior` - Special handling for merged player
- Menu condition checks - Detect merged state for enabling/disabling options
- Dialog system - Handle conversations when player is not party leader

### 7. Save/Load System

**Current**: Both parties saved independently with attachment relationship
**Required**: Save merged state, handle restoration on load

**Patches Needed**:
- New saveable field indicating merge state
- Save merged troop/item attribution (which belonged to player originally)
- Load-time restoration of merge relationship
- Validation that referenced parties exist on load
- Handle save corruption if target party no longer exists

### 8. Separation and Exit

**Current**: Setting AttachedTo to null cleanly separates parties
**Required**: Reverse roster transfers, restore player party to independent state

**Patches Needed**:
- `Army.OnRemovePartyInternal` - Split out player troops back to player party
- Roster separation logic - Track which troops belonged to player originally
- Equipment distribution - Decide what items player takes when leaving
- Hero restoration - Make player hero the leader of their party again
- Position assignment - Place separated player party at valid location

### 9. Party Economics and Management

**Current**: Each party tracks its own gold, food consumption, wages
**Required**: Either pool resources or track player share separately

**Patches Needed**:
- Gold management - Decide if gold is pooled or separate
- Food consumption - Merged party consumes more food
- Wages - Who pays for player's troops?
- Morale calculations - Include merged troops in calculations
- Party upgrade tracking - Which troops can player upgrade?

### 10. Combat and Battle Formation

**Current**: Each party has separate formation in battle
**Required**: Player troops deploy as part of lord's party formation

**Patches Needed**:
- Mission initialization - Spawn merged troops under lord's command
- Troop assignment to formations - Distribute player troops appropriately
- Player control - Either direct lord's troops or command only player troops
- Post-battle casualties - Attribute casualties back to correct party
- Loot distribution - Handle player share of loot

### 11. Kingdom and Clan Integration

**Current**: Player party belongs to player clan, lord's party to their clan
**Required**: Handle troops from different clans in same party

**Patches Needed**:
- Clan troop tracking - Which troops belong to which clan?
- Influence calculations - Who gets influence for merged party actions?
- Renown - How is renown distributed for merged party?
- Relation effects - Troop losses affect both player and lord relations?
- Kingdom decisions - Can merged party affect kingdom votes?

### 12. Performance and Optimization

**Current**: Party systems optimized for many separate parties
**Required**: Ensure no performance degradation with merging

**Patches Needed**:
- Roster operation optimization - Merging large rosters efficiently
- Cache invalidation - Merged state changes many cached values
- Event dispatching - Avoid duplicate events for merged entities
- Memory management - Properly dispose of shell party objects

## Critical Edge Cases to Handle

1. **Player party has more troops than lord's party size limit** - Need to reject merge or expand limits
2. **Lord's party gets destroyed** - What happens to merged player troops?
3. **Player is captured** - Does this affect lord's party or just player hero?
4. **Lord changes allegiance** - Does player get forced to change too?
5. **Save/Load mid-battle** - Merged state must serialize properly
6. **Multiplayer considerations** - If relevant, merging may conflict with MP systems
7. **Quest party requirements** - Quests checking for player party specifically
8. **Companion location** - Companions in merged party aren't technically in player party
9. **Settlement interactions** - Entering settlement with merged party
10. **Party disbanding** - What if lord disbands party while player merged?

## Key Native Methods Reference

### Core Attachment

- `MobileParty.SetAttachedToInternal(MobileParty value)` - Main attachment logic
- `MobileParty.AddAttachedPartyInternal(MobileParty mobileParty)` - Add to attached list
- `MobileParty.RemoveAttachedPartyInternal(MobileParty mobileParty)` - Remove from attached list
- `MobileParty.OnAttachedToRemoved()` - Cleanup when detached
- `Army.AddPartyToMergedParties(MobileParty mobileParty)` - Army's merge method

### Party Collection

- `Army.Tick(...)` - Auto-attach parties when close enough
- `BesiegerCamp.GetInvolvedPartiesForEventType(...)` - Get siege participants
- `Settlement.GetInvolvedPartiesForEventType(...)` - Get settlement defenders
- `MapEvent.InvolvedParties` - All parties in map event
- `DefaultEncounterModel.FindNonAttachedNpcPartiesWhoWillJoinPlayerEncounter(...)` - Find reinforcements

### Roster Management

- `TroopRoster.Add(TroopRoster troopRoster)` - Merge entire roster
- `TroopRoster.AddToCounts(...)` - Add specific troops
- `TroopRoster.RemoveIf(...)` - Remove troops matching condition
- `ItemRoster.AddToCounts(...)` - Add items
- `PartyBase.OnHeroAdded(...)` - Hero joined callback
- `PartyBase.OnRosterSizeChanged(...)` - Roster modified callback

### Position and Movement

- `Army.GetRelativePositionForParty(...)` - Calculate formation position
- `MobileParty.CurrentSettlement` setter - Propagates to attached parties
- `MobileParty.Position` property - Attached parties may override
- `NavigationHelper.FindReachablePointAroundPosition(...)` - Find valid spawn position

### AI and Behavior

- `MobilePartyAi.TickInternal()` - Main AI tick, skips attached parties
- `DefaultMobilePartyAIModel.GetBehaviors(...)` - Determine party behavior
- `DefaultEncounterModel.IsPartyUnderPlayerCommand(...)` - Check command relationship

### Events and Callbacks

- `CampaignEventDispatcher.OnPartyAttachedAnotherParty(...)` - Attachment notification
- `CampaignEventDispatcher.OnPartyRemovedFromArmy(...)` - Army removal notification
- `CampaignEventDispatcher.OnArmyOverlaySetDirty()` - UI update needed
- `CampaignEvents.OnSiegeEventStartedEvent` - Siege started
- `CampaignEvents.OnSettlementOwnerChangedEvent` - Settlement captured

## Decompile File Locations

All files relative to `C:\Dev\Enlisted\Decompile\`:

- **Core Party**: `TaleWorlds.CampaignSystem\TaleWorlds\CampaignSystem\Party\MobileParty.cs`
- **Party Base**: `TaleWorlds.CampaignSystem\TaleWorlds\CampaignSystem\Party\PartyBase.cs`
- **Party AI**: `TaleWorlds.CampaignSystem\TaleWorlds\CampaignSystem\Party\MobilePartyAi.cs`
- **Army System**: `TaleWorlds.CampaignSystem\TaleWorlds\CampaignSystem\Army.cs`
- **Siege Camp**: `TaleWorlds.CampaignSystem\TaleWorlds\CampaignSystem\Siege\BesiegerCamp.cs`
- **Siege Event**: `TaleWorlds.CampaignSystem\TaleWorlds\CampaignSystem\Siege\SiegeEvent.cs`
- **Map Event**: `TaleWorlds.CampaignSystem\TaleWorlds\CampaignSystem\MapEvents\MapEvent.cs`
- **Encounter Model**: `TaleWorlds.CampaignSystem\TaleWorlds\CampaignSystem\ComponentInterfaces\EncounterModel.cs`
- **Default Encounter**: `TaleWorlds.CampaignSystem\TaleWorlds\CampaignSystem\GameComponents\DefaultEncounterModel.cs`
- **Troop Roster**: `TaleWorlds.CampaignSystem\TaleWorlds\CampaignSystem\Roster\TroopRoster.cs`
- **Item Roster**: `TaleWorlds.CampaignSystem\TaleWorlds\CampaignSystem\Roster\ItemRoster.cs`
- **Player Encounter**: `TaleWorlds.CampaignSystem\TaleWorlds\CampaignSystem\Encounters\PlayerEncounter.cs`
- **Encounter Menus**: `TaleWorlds.CampaignSystem\TaleWorlds\CampaignSystem\CampaignBehaviors\EncounterGameMenuBehavior.cs`

## Conclusion

Physically merging the player party into a lord's party would require extensive patches across nearly every party-related system. The native attachment system is designed to keep parties logically separate while allowing them to move and fight together. True merging would involve:

1. Transferring all rosters (troops, prisoners, items) to the target party
2. Updating all party collection systems to recognize the merged state
3. Modifying AI, speed, and movement calculations to account for merged troops
4. Handling player control and command in a non-leader role
5. Managing separation cleanly when player leaves
6. Tracking attribution of troops/items to restore on separation

The complexity is substantial because the game assumes each `MobileParty` is an independent entity. Merging requires maintaining that illusion while treating them as one unit internally, which touches almost every system that interacts with parties.

For the Enlisted mod, consider whether this level of integration is necessary, or if a hybrid approach (keeping parties separate but appearing merged to the player) might achieve similar gameplay goals with less complexity.
