# SAS Menu Handling Analysis

Generated from ServeAsSoldier decompiled sources on 2025-09-02 01:35:00 UTC

## Core Menu Management Strategy

SAS uses a simple but effective menu management approach:

### **Settlement Entry/Exit Handling**
```csharp
// When lord enters settlement - force player to party_wait menu
private void OnSettlementEntered(MobileParty party, Settlement settlement, Hero hero)
{
    if (followingHero != null && hero == followingHero)
    {
        GameMenu.ActivateGameMenu("party_wait");
        if (settlement.IsTown)
        {
            Campaign.Current.TimeControlMode = CampaignTimeControlMode.Stop;
        }
    }
}

// When lord leaves settlement - force player to party_wait menu  
private void OnSettlementLeftEvent(MobileParty party, Settlement settlement)
{
    if (party.LeaderHero != null && party.LeaderHero == followingHero)
    {
        GameMenu.ActivateGameMenu("party_wait");
    }
}
```

### **Party Visibility Management**
```csharp
// Hide player party visually (complex visual manipulation)
public static void hidePlayerParty()
{
    var partyVisual = PartyVisualManager.Current.GetVisualOfParty(PartyBase.MainParty);
    if (partyVisual.HumanAgentVisuals != null)
        partyVisual.HumanAgentVisuals.GetEntity().SetVisibilityExcludeParents(false);
    if (partyVisual.MountAgentVisuals != null)
        partyVisual.MountAgentVisuals.GetEntity().SetVisibilityExcludeParents(false);
}

// Show player party visually
public static void showPlayerParty()
{
    var partyVisual = PartyVisualManager.Current.GetVisualOfParty(PartyBase.MainParty);
    if (partyVisual.HumanAgentVisuals != null)
        partyVisual.HumanAgentVisuals.GetEntity().SetVisibilityExcludeParents(true);
    if (partyVisual.MountAgentVisuals != null)
        partyVisual.MountAgentVisuals.GetEntity().SetVisibilityExcludeParents(true);
}
```

### **Diplomatic Inheritance System**
```csharp
// Sync player's faction relationships with lord's faction
public static void UpdateDiplomacy()
{
    foreach (IFaction faction in Campaign.Current.Factions)
    {
        // If lord's faction is at war but player isn't - declare war
        if (faction.IsAtWarWith(followingHero.MapFaction) && 
            !faction.IsAtWarWith(Clan.PlayerClan.MapFaction))
        {
            DeclareWarAction.ApplyByDefault(faction, Clan.PlayerClan.MapFaction);
        }
        // If lord's faction is at peace but player is at war - make peace
        else if (!faction.IsAtWarWith(followingHero.MapFaction) && 
                 faction.IsAtWarWith(Clan.PlayerClan.MapFaction))
        {
            MakePeaceAction.Apply(faction, Clan.PlayerClan.MapFaction, 0);
        }
    }
}

// Undo diplomatic changes when retiring
private static void UndoDiplomacy()
{
    // Leave settlement if currently in one
    if (MobileParty.MainParty.CurrentSettlement != null)
    {
        LeaveSettlementAction.ApplyForParty(MobileParty.MainParty);
    }
    
    // Make peace with all factions at war with lord's faction
    foreach (IFaction faction in Campaign.Current.Factions)
    {
        if (faction.IsAtWarWith(followingHero.MapFaction) && !faction.IsBanditFaction)
        {
            MakePeaceAction.Apply(Hero.MainHero.MapFaction, faction, 0);
        }
    }
}
```

## Menu Flow Control

### **Primary Menu Strategy**
- **"party_wait"** is the default menu for enlisted soldiers
- **Force menu transitions** when lord enters/leaves settlements
- **Time control management** (stop time in towns)
- **No player menu autonomy** while enlisted

### **Battle/Encounter Integration**
```csharp
// From WanderSoldierBehavior.Tick() - battle detection
private void Tick()
{
    bool inBattle = followingHero?.PartyBelongedTo?.MapEvent != null;
    bool smallBattle = false;
    
    if (inBattle)
    {
        var battleSide = ContainsParty(followingHero.PartyBelongedTo.MapEvent.PartiesOnSide(BattleSideEnum.Attacker), followingHero.PartyBelongedTo)
            ? followingHero.PartyBelongedTo.MapEvent.AttackerSide
            : followingHero.PartyBelongedTo.MapEvent.DefenderSide;
            
        smallBattle = battleSide.TroopCount < 100;
    }
    
    if (followingHero != null && followingHero.PartyBelongedTo != null && 
        (followingHero.PartyBelongedTo.MapEvent == null || smallBattle))
    {
        GameMenu.ActivateGameMenu("party_wait");
    }
}
```

## Missing APIs We Need

### **Party Visual Management**
```csharp
// Currently missing from our engine-signatures.md:
SandBox.View.Map.PartyVisualManager :: Current { get; }
SandBox.View.Map.PartyVisual :: HumanAgentVisuals { get; }
SandBox.View.Map.PartyVisual :: MountAgentVisuals { get; }
TaleWorlds.Engine.GameEntity :: SetVisibilityExcludeParents(bool visible)
```

### **Diplomatic Actions**
```csharp
// Currently missing:
TaleWorlds.CampaignSystem.Actions.DeclareWarAction :: ApplyByDefault(IFaction faction1, IFaction faction2)
TaleWorlds.CampaignSystem.Actions.LeaveSettlementAction :: ApplyForParty(MobileParty party)
```

### **Time Control**
```csharp
// Currently missing:
TaleWorlds.CampaignSystem.CampaignTimeControlMode :: Stop
TaleWorlds.CampaignSystem.Campaign :: TimeControlMode { get; set; }
```

### **Settlement Actions**
```csharp
// Currently missing:
TaleWorlds.CampaignSystem.Settlement :: IsTown { get; }
TaleWorlds.CampaignSystem.Settlement :: HeroesWithoutParty { get; }
TaleWorlds.CampaignSystem.Party.MobileParty :: CurrentSettlement { get; }
```

## Implementation Strategy for Our SAS

### **Simplified Menu Management (Recommended)**
Instead of complex visual manipulation, use:
1. **`MobileParty.IsVisible = false`** (already documented)
2. **`GameMenu.ActivateGameMenu("party_wait")`** for menu control
3. **Settlement event detection** via campaign events

### **Battle Detection Pattern**
```csharp
// Modern approach using our documented APIs
private void OnHourlyTick()
{
    if (IsEnlisted && followingHero?.PartyBelongedTo != null)
    {
        var lordParty = followingHero.PartyBelongedTo;
        var playerEvent = MapEvent.PlayerMapEvent;
        
        // If lord is not in battle, ensure we're following
        if (lordParty.MapEvent == null)
        {
            GameMenu.ActivateGameMenu("party_wait");
            MobileParty.MainParty.Ai.SetMoveEscortParty(lordParty);
        }
        
        // Settlement handling
        if (lordParty.CurrentSettlement != null)
        {
            // Lord entered settlement - follow to party_wait
            GameMenu.ActivateGameMenu("party_wait");
        }
    }
}
```

## Critical Findings

### **SAS Menu Philosophy**:
1. **"party_wait" is king** - Default menu for all enlisted scenarios
2. **No player menu autonomy** - Player follows lord's menu state
3. **Force menu transitions** on settlement entry/exit
4. **Simple visual hiding** via `MobileParty.IsVisible`

### **Battle Integration**:
1. **Automatic menu switching** based on lord's battle state
2. **Troop count awareness** (avoid battles with <100 troops)
3. **Battle state detection** via `MapEvent` properties

### **Settlement Behavior**:
1. **Forced settlement following** when lord enters/exits
2. **Time control management** in towns
3. **Diplomatic synchronization** with lord's faction
