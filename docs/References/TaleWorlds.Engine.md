# TaleWorlds.Engine Reference

## Relevance to Enlisted Mod: LOW-MEDIUM

TaleWorlds.Engine is the low-level game engine layer. It handles rendering, scene management, input, audio, and core engine functions. Most modding doesn't touch this directly, but understanding it helps debug crashes and understand performance.

---

## Key Classes and Systems

### Scene and Entities

| Class | Purpose | Enlisted Usage |
|-------|---------|----------------|
| `Scene` | 3D scene container | Scene context during missions |
| `GameEntity` | Entity in scene | Game objects |
| `MetaMesh` | Mesh rendering | Visual elements |

### Input

| Class | Purpose | Enlisted Usage |
|-------|---------|----------------|
| `Input` | Input handling | Potential keybind handling |
| `InputKey` | Key definitions | Key constants |

### Audio

| Class | Purpose | Enlisted Usage |
|-------|---------|----------------|
| `SoundEvent` | Sound playback | Could add sound effects |
| `MusicManager` | Music control | Not directly relevant |

### Rendering

| Class | Purpose | Enlisted Usage |
|-------|---------|----------------|
| `Texture` | Texture management | Visual assets |
| `Material` | Material definitions | Visual appearance |

---

## Why It Matters

### RGL Crashes
The "RGL" in our crash logs refers to the Render Game Layer - the engine's rendering system. Understanding that:
- Skeleton updates happen in the engine
- Visual state changes trigger engine updates
- Menu transitions involve scene/entity changes

### The Skeleton Assertion Crash
The crash we're debugging:
```
ASSERTION FAILED: time_since_last_update > 0
```

This happens in the engine layer when:
1. A party's visual/skeleton hasn't been updating (IsActive = false)
2. A menu transition triggers a skeleton update
3. The engine asserts that time has passed since the last update
4. But since the skeleton wasn't updating, time_since_last_update is 0

---

## Engine Tick System

The engine processes ticks in this order:
1. Input processing
2. Game logic ticks
3. Physics updates
4. Skeleton/animation updates
5. Rendering

Our `NextFrameDispatcher` defers actions to run after the current tick completes, avoiding mid-tick state changes.

---

## Files That Might Help Debug

| File | Why |
|------|-----|
| `Skeleton.cs` | Skeleton animation system |
| `Scene.cs` | Scene management |
| `GameEntity.cs` | Entity lifecycle |

---

## Potential Future Use

If we ever need:
- Custom visual effects
- Sound effects for enlistment events
- Custom input handling
- Scene manipulation

We would interact with this assembly.

---

## Not Directly Relevant

Most of TaleWorlds.Engine is internal engine code that mods don't typically touch:
- Rendering pipeline
- Physics system
- Asset loading
- Memory management
- Threading

---

## Key Insight

The engine expects consistent state. When we hide/show parties or change their active state, we must do so at safe times to avoid engine assertions. This is why:

1. We use `NextFrameDispatcher` to defer state changes
2. We check for siege/battle states before operations
3. We use visual hiding instead of logical state changes when possible

