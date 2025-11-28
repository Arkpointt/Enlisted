# SandBox.View Reference

## Relevance to Enlisted Mod: HIGH

This assembly handles the visual/rendering layer for the campaign map and settlements. Most importantly, it contains `PartyVisualManager` which we use to hide/show the player party visual without triggering RGL skeleton crashes. This is critical for our party invisibility system.

---

## Key Classes and Systems

### Party Visuals

| Class | Purpose | Enlisted Usage |
|-------|---------|----------------|
| `PartyVisualManager` | Manages party visuals on map | Hide/show player party visual safely |
| `PartyVisual` | Individual party's visual representation | Access HumanAgentVisuals, MountAgentVisuals |
| `MapScreen` | Campaign map screen | Map interaction context |

### Map Rendering

| Class | Purpose | Enlisted Usage |
|-------|---------|----------------|
| `MapView` | Main map view class | Understanding map rendering |
| `MapScene` | Map scene management | Scene context |
| `MapCameraView` | Camera control on map | Camera following logic |

### Menu Handling

| Class | Purpose | Enlisted Usage |
|-------|---------|----------------|
| `MenuView` | Game menu visual handling | Menu rendering |
| `MenuViewContext` | Menu visual context | Menu state |

### Conversation

| Class | Purpose | Enlisted Usage |
|-------|---------|----------------|
| `ConversationVisualsHandler` | Dialog scene visuals | Conversation rendering |

---

## Critical: PartyVisualManager

This is the key class for hiding the player party on the campaign map without causing crashes.

### Location
`SandBox.View/Map/PartyVisualManager.cs`

### Why It Matters
Setting `MobileParty.IsVisible = false` directly can cause RGL skeleton timing assertion crashes during menu transitions. Using `PartyVisualManager` to hide visuals is safer because it only affects rendering, not the party's logical state.

### Our Usage Pattern
```csharp
public static void HidePlayerPartyVisual()
{
    var visualManager = SandBox.View.Map.PartyVisualManager.Current;
    if (visualManager == null) return;

    var partyVisual = visualManager.GetVisualOfParty(PartyBase.MainParty);
    if (partyVisual == null) return;

    // Hide using official SetVisible API
    partyVisual.HumanAgentVisuals?.SetVisible(false);
    partyVisual.MountAgentVisuals?.SetVisible(false);
    partyVisual.CaravanMountAgentVisuals?.SetVisible(false);
}

public static void ShowPlayerPartyVisual()
{
    var visualManager = SandBox.View.Map.PartyVisualManager.Current;
    if (visualManager == null) return;

    var partyVisual = visualManager.GetVisualOfParty(PartyBase.MainParty);
    if (partyVisual == null) return;

    partyVisual.HumanAgentVisuals?.SetVisible(true);
    partyVisual.MountAgentVisuals?.SetVisible(true);
    partyVisual.CaravanMountAgentVisuals?.SetVisible(true);
}
```

---

## Important Directories

### `/Map/`
All campaign map visual handling:
- `PartyVisualManager.cs` - CRITICAL for our mod
- `MapView.cs` - Map screen view
- `MapCameraView.cs` - Camera control
- `MapScreen.cs` - Main map screen

### `/Menu/`
Menu visual handling:
- `MenuView.cs` - Menu rendering
- Context classes for different menu types

### `/Conversation/`
Dialog/conversation scene visuals.

### `/Missions/`
Mission-specific visual handling for settlements, battles.

### `/CharacterCreation/`
Character creation visuals - not relevant.

---

## Files to Study

| File | Why |
|------|-----|
| `PartyVisualManager.cs` | Critical for party visibility |
| `MapView.cs` | Understanding map screen |
| `SandBoxViewSubModule.cs` | Module initialization |
| `MapCameraView.cs` | Camera following behavior |

---

## PartyVisual Properties

The `PartyVisual` class has these key visual components:

| Property | Purpose |
|----------|---------|
| `HumanAgentVisuals` | The character model on the map |
| `MountAgentVisuals` | The horse/mount model |
| `CaravanMountAgentVisuals` | Caravan animal model |
| `StrategicEntity` | The map entity reference |

Each of these has a `SetVisible(bool)` method that controls rendering.

---

## Camera Following

The map camera system is in this assembly. When the player is enlisted, we want the camera to follow the lord's party, not the player's hidden party.

```csharp
// Set camera to follow lord's party
lordParty.Party.SetAsCameraFollowParty();
```

This call affects the camera system in this assembly.

---

## Save/Load Screen

`SaveLoadScreen.cs` handles the save/load UI visuals. Not directly relevant but shows screen management patterns.

---

## Known Issues / Gotchas

1. **PartyVisualManager.Current**: Can be null if not on the campaign map
2. **Visual vs Logical State**: Hiding visuals doesn't affect party's logical IsVisible property
3. **Skeleton Updates**: Visual changes are safer than logical state changes during transitions
4. **Camera Persistence**: Camera follow state can be reset by game events

---

## When to Use Visual vs Logical Hiding

| Approach | When to Use |
|----------|-------------|
| `PartyVisualManager.SetVisible(false)` | During enlisted service, safe during menu transitions |
| `MobileParty.IsVisible = false` | When you need to hide from AI detection, not just visually |

For Enlisted, we primarily use the visual approach because:
1. It's safer during siege/battle menu transitions
2. It doesn't affect the party's skeleton update state
3. It achieves the visual goal without side effects

