# TaleWorlds.Library Reference

## Relevance to Enlisted Mod: LOW-MEDIUM

TaleWorlds.Library contains foundational utility classes, data structures, and helper functions used throughout the game. It's the base library that all other TaleWorlds assemblies depend on.

---

## Key Classes and Systems

### Collections

| Class | Purpose | Enlisted Usage |
|-------|---------|----------------|
| `MBList<T>` | Custom list implementation | Game uses these extensively |
| `MBReadOnlyList<T>` | Read-only list wrapper | Safe iteration |
| `MBBindingList<T>` | Observable list for UI | ViewModel bindings |

### Math and Geometry

| Class | Purpose | Enlisted Usage |
|-------|---------|----------------|
| `Vec2` | 2D vector | Map positions |
| `Vec3` | 3D vector | World positions |
| `Mat3` | 3x3 matrix | Rotations |
| `MBMath` | Math utilities | Calculations |

### Text and Localization

| Class | Purpose | Enlisted Usage |
|-------|---------|----------------|
| `MBStringBuilder` | Efficient string building | Text construction |

### Debugging

| Class | Purpose | Enlisted Usage |
|-------|---------|----------------|
| `Debug` | Debug utilities | Logging, assertions |
| `TaleWorlds.Library.Debug` | Game debug class | Development logging |

### Serialization

| Class | Purpose | Enlisted Usage |
|-------|---------|----------------|
| `ISerializableObject` | Serialization interface | Save/load support |

---

## Vec2 - Map Positions

Used for campaign map positions:

```csharp
// Get party position
Vec2 position = mobileParty.Position2D;

// Calculate distance
float distance = position.Distance(otherPosition);

// Move to position
mobileParty.Position2D = targetPosition;
```

---

## MBBindingList - UI Data Binding

Used for ViewModels that update UI:

```csharp
public MBBindingList<SomeItemVM> Items { get; } = new MBBindingList<SomeItemVM>();

// Adding items
Items.Add(new SomeItemVM());

// UI automatically updates
```

---

## MBMath Utilities

Common math operations:

```csharp
// Clamp value
float clamped = MBMath.ClampFloat(value, min, max);

// Linear interpolation
float lerped = MBMath.Lerp(start, end, t);

// Random
int randomInt = MBMath.RandomInt(0, 100);
```

---

## Debug Utilities

```csharp
// Debug print (only in debug builds)
Debug.Print("Debug message");

// Assert (debug builds)
Debug.Assert(condition, "Assertion message");

// Warning
Debug.FailedAssert("Warning message");
```

---

## Color

For UI and text coloring:

```csharp
var color = new Color(1f, 0f, 0f, 1f); // RGBA
var color = Color.FromUint(0xFF0000FF); // From hex
```

---

## Files to Study

| File | Why |
|------|-----|
| `Vec2.cs` | 2D vector operations |
| `MBBindingList.cs` | UI binding patterns |
| `MBMath.cs` | Math utilities |
| `Debug.cs` | Debug utilities |

---

## Not Directly Relevant

Most of TaleWorlds.Library is infrastructure code:
- Threading utilities
- Memory management
- Platform abstraction
- Native interop

We use these indirectly through higher-level APIs.

---

## Key Pattern: Position Handling

When snapping player position to lord:

```csharp
// Get positions as Vec2
Vec2 playerPos = MobileParty.MainParty.Position2D;
Vec2 lordPos = lordParty.Position2D;

// Check distance
float distance = playerPos.Distance(lordPos);

// Snap if needed
if (distance > threshold)
{
    MobileParty.MainParty.Position2D = lordPos;
}
```

