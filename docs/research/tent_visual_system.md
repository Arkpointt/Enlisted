# Tent Visual System for Player Party

## Summary
Yes! The native code already has a tent entity that's used on the campaign map for besieging parties. We can use this same tent to display the player's Lord's party as a tent when resting at night.

## Key Findings from Native Decompile

### 1. Tent Entity Already Exists
**Prefab Name**: `"map_icon_siege_camp_tent"`

This is the same tent entity used by siege camps on the campaign map. Found in:
- **File**: `SandBox.View\Map\Visuals\MobilePartyVisual.cs`
- **Line**: 795

### 2. Native Implementation: AddTentEntityForParty Method

The native game already has a complete method for adding tents to parties during sieges:

```csharp
public void AddTentEntityForParty(
    GameEntity strategicEntity,
    PartyBase party,
    ref bool clearBannerComponentCache)
{
    // Create the tent entity
    GameEntity empty = GameEntity.CreateEmpty(strategicEntity.Scene, true, true, true);
    empty.AddMultiMesh(MetaMesh.GetCopy("map_icon_siege_camp_tent", true, false), true);
    
    // Scale the tent
    MatrixFrame identity1 = MatrixFrame.Identity;
    ((Mat3) ref identity1.rotation).ApplyScaleLocal(1.2f);
    empty.SetFrame(ref identity1, true);
    
    // Get banner code from party leader
    string str = (string) null;
    if (party.LeaderHero?.ClanBanner != null)
        str = party.LeaderHero.ClanBanner.BannerCode;
    
    // Check if this is an army leader
    bool flag = party.MobileParty.Army != null && party.MobileParty.Army.LeaderParty == party.MobileParty;
    
    // Position the banner on the tent
    MatrixFrame identity2 = MatrixFrame.Identity;
    identity2.origin.z += flag ? 0.2f : 0.15f;  // Higher for army leaders
    ((Mat3) ref identity2.rotation).RotateAboutUp(1.57079637f);
    
    // Scale banner based on party strength
    float num = MBMath.Map(
        (float)((double)party.CalculateCurrentStrength() / 500.0 * 
        (party.MobileParty.Army != null & flag ? 1.0 : 0.800000011920929)), 
        0.0f, 1f, 0.15f, 0.5f
    );
    ((Mat3) ref identity2.rotation).ApplyScaleLocal(num);
    
    // Add banner to tent if available
    if (!string.IsNullOrEmpty(str))
    {
        clearBannerComponentCache = false;
        string bannerMeshName = "campaign_flag";
        // ... banner caching and creation code ...
    }
    
    // Attach tent to strategic entity
    strategicEntity.AddChild(empty, false);
    empty.SetVisibilityExcludeParents(true);
}
```

### 3. Current Usage in Native Code

The tent is currently shown when a party is part of a besieging camp:

```csharp
// From AddMobileIconComponents method (line 553-556)
if (this.IsPartOfBesiegerCamp(party))
{
    this.AddTentEntityForParty(this.StrategicEntity, party, ref clearBannerComponentCache);
}
else
{
    // Normal character/mount visuals
    this.AddCharacterToPartyIcon(...);
}

// IsPartOfBesiegerCamp method (lines 1089-1092)
private bool IsPartOfBesiegerCamp(PartyBase party)
{
    return party.MobileParty.BesiegedSettlement?.SiegeEvent != null && 
           party.MobileParty.BesiegedSettlement.SiegeEvent.BesiegerCamp
               .HasInvolvedPartyForEventType(party, (MapEvent.BattleTypes) 5);
}
```

### 4. Other Relevant Tent/Camp Prefabs

From the decompile search, here are other potential prefabs you could use:
- `"map_icon_siege_camp_tent"` - The main siege camp tent (RECOMMENDED)
- `"raft"` - Used for parties in raft state (naval system)
- Various hideout and camp-related scene identifiers

## Implementation Plan for Enlisted Mod

### Option 1: Simple Night Tent Display
Replace the player party's visual with a tent at night:

```csharp
public class PlayerPartyTentVisual : CampaignBehaviorBase
{
    public override void RegisterEvents()
    {
        // Hook into party visual refresh
        CampaignEvents.HourlyTickEvent.AddNonSerializedListener(this, OnHourlyTick);
        CampaignEvents.OnPartyVisualChangedEvent.AddNonSerializedListener(this, OnPartyVisualChanged);
    }
    
    private void OnHourlyTick()
    {
        if (MobileParty.MainParty != null)
        {
            // Force visual refresh if transitioning to/from night
            bool isNight = Campaign.Current.IsNight;
            if (ShouldShowTent())
            {
                MobileParty.MainParty.Party.SetVisualAsDirty();
            }
        }
    }
    
    private bool ShouldShowTent()
    {
        var mainParty = MobileParty.MainParty;
        
        // Show tent if:
        // 1. Party is waiting/resting
        // 2. It's nighttime (between 20:00 and 6:00)
        // 3. Not in a settlement
        // 4. Not in battle
        
        return mainParty != null &&
               Campaign.Current.IsMainPartyWaiting &&
               Campaign.Current.IsNight &&
               mainParty.CurrentSettlement == null &&
               mainParty.MapEvent == null;
    }
}
```

### Option 2: Harmony Patch Approach
Patch the `MobilePartyVisual.AddMobileIconComponents` method:

```csharp
[HarmonyPatch(typeof(MobilePartyVisual), "AddMobileIconComponents")]
public class MobilePartyVisualPatch
{
    static bool Prefix(MobilePartyVisual __instance, 
                       PartyBase party, 
                       ref bool clearBannerComponentCache,
                       ref bool clearBannerEntityCache)
    {
        // Check if this is main party and should show tent
        if (party == PartyBase.MainParty && ShouldShowTentForMainParty())
        {
            // Get the StrategicEntity from the MobilePartyVisual instance
            var strategicEntity = __instance.StrategicEntity;
            
            // Call the native AddTentEntityForParty method
            __instance.AddTentEntityForParty(strategicEntity, party, ref clearBannerComponentCache);
            
            // Return false to prevent normal character visual
            return false;
        }
        
        // Continue with normal processing
        return true;
    }
    
    private static bool ShouldShowTentForMainParty()
    {
        var mainParty = MobileParty.MainParty;
        return mainParty != null &&
               Campaign.Current.IsMainPartyWaiting &&
               Campaign.Current.IsNight &&
               mainParty.CurrentSettlement == null &&
               mainParty.MapEvent == null;
    }
}
```

### Option 3: Custom Visual Manager (Advanced)
Create a custom visual component that hooks into the visual manager:

```csharp
public class CampTentVisualBehavior : CampaignBehaviorBase
{
    private GameEntity _tentEntity;
    private bool _tentCurrentlyVisible = false;
    
    public override void RegisterEvents()
    {
        CampaignEvents.HourlyTickEvent.AddNonSerializedListener(this, OnHourlyTick);
        CampaignEvents.OnGameLoadedEvent.AddNonSerializedListener(this, OnGameLoaded);
    }
    
    private void OnHourlyTick()
    {
        UpdateTentVisibility();
    }
    
    private void UpdateTentVisibility()
    {
        bool shouldShow = ShouldShowTent();
        
        if (shouldShow && !_tentCurrentlyVisible)
        {
            ShowTent();
        }
        else if (!shouldShow && _tentCurrentlyVisible)
        {
            HideTent();
        }
    }
    
    private void ShowTent()
    {
        var mainParty = MobileParty.MainParty;
        if (mainParty == null) return;
        
        // Get the party's visual
        var visual = MobilePartyVisualManager.Current?.GetVisualOfEntity(mainParty.Party);
        if (visual == null) return;
        
        // Create tent entity (you'll need reflection or Harmony to access AddTentEntityForParty)
        var strategicEntity = visual.StrategicEntity;
        bool clearCache = true;
        
        // Call via reflection or Harmony
        typeof(MobilePartyVisual)
            .GetMethod("AddTentEntityForParty", BindingFlags.NonPublic | BindingFlags.Instance)
            ?.Invoke(visual, new object[] { strategicEntity, mainParty.Party, clearCache });
        
        _tentCurrentlyVisible = true;
    }
    
    private void HideTent()
    {
        // Force visual refresh to restore normal appearance
        MobileParty.MainParty?.Party.SetVisualAsDirty();
        _tentCurrentlyVisible = false;
    }
    
    private bool ShouldShowTent()
    {
        var mainParty = MobileParty.MainParty;
        return mainParty != null &&
               Campaign.Current.IsMainPartyWaiting &&
               Campaign.Current.IsNight &&
               mainParty.CurrentSettlement == null &&
               mainParty.MapEvent == null;
    }
}
```

## Technical Details

### Scene and Entity Management
- **Scene**: Accessible via `((SandBox.MapScene)Campaign.Current.MapSceneWrapper).Scene`
- **Entity Creation**: Use `GameEntity.CreateEmpty(scene, true, true, true)`
- **Mesh Loading**: Use `MetaMesh.GetCopy("map_icon_siege_camp_tent", true, false)`

### Timing Considerations
- **Night Detection**: Use `Campaign.Current.IsNight`
- **Waiting Detection**: Use `Campaign.Current.IsMainPartyWaiting`
- **Hour Range**: Night is typically 20:00 to 6:00 (check `CampaignTime.Current.GetHourOfDay`)

### Visual Refresh
- Call `party.SetVisualAsDirty()` to force visual update
- The visual manager will call `RefreshPartyIcon()` on next tick
- This triggers `AddMobileIconComponents()` which is where you inject tent logic

## Recommendations

1. **Start with Option 2 (Harmony Patch)**: 
   - Least invasive
   - Uses native code directly
   - Easy to maintain

2. **Add Configuration**:
   - Toggle tent display on/off
   - Configure time range for tent display
   - Option to show tent when waiting anytime (not just at night)

3. **Test Cases**:
   - Resting at night in open terrain [x]
   - Moving during night (should hide tent)
   - Entering settlement (should hide tent)
   - Starting battle (should hide tent)
   - Loading saved game (should restore correct state)

## Files to Modify in Enlisted Mod

1. Create new file: `src/Features/Visual/Behaviors/CampTentVisualBehavior.cs`
2. Or patch: Add Harmony patch to existing visual behavior
3. Register in: `src/SubModule.cs` via `SubModule.OnSubModuleLoad()`

## Visual Result
When implemented, the player's party will appear as:
- **During Day / Moving**: Normal character on horse
- **At Night While Resting**: Tent with clan banner flying

This matches the visual style of siege camps and provides clear visual feedback that the party is camping for the night!
