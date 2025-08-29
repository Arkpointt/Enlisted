# ADR-011: Map Tracker Redirection and Main Party Nameplate Suppression

**Date:** 2025-08-29  
**Status:** Accepted  
**Deciders:** Development Team

## Context

During enlistment, the player should appear visually merged with the commander's party. The default map UI shows the player's `MainParty` banner/nameplate and tracks it, which breaks immersion. The game occasionally recreates visuals/trackers even after manual removal.

We need a robust, low-risk approach to:
- Hide the player's `MainParty` nameplate/shield while enlisted
- Redirect map trackers to follow the commander instead of the player's party
- Periodically enforce the hidden state to counter engine/UI refreshes

Target game version: Bannerlord 1.2.12 (`https://apidoc.bannerlord.com/v/1.2.12/`).

## Decision

Implement a two-pronged strategy:

1) Harmony patches in `src/Mod.GameAdapters/Patches/`
   - `HidePartyNamePlatePatch.cs`
     - Target: `SandBox.ViewModelCollection.Nameplate.PartyNameplateVM.RefreshBinding()`
     - Postfix: When enlisted and `__instance.Party == MobileParty.MainParty`, set `IsMainParty=false` and `IsVisibleOnMap=false` (via reflection). This hides the main party nameplate/shield.
   - `MobilePartyTrackerPatches.cs`
     - Target: tracker VMs under `SandBox.ViewModelCollection.Map.*Tracker*VM` (methods: `RefreshTrackedObjects`, `RefreshList`, `Initialize`, etc.)
     - Postfix: Track the commander and untrack the `MainParty` while enlisted. Fallback removes `MainParty` entries from internal tracked collections via reflection.
     - Prefix on `TrackParty`: Block tracking when the argument is `MainParty` while enlisted.

2) Behavior-level enforcement in `EnlistmentBehavior`
   - Each tick while enlisted: set `MobileParty.MainParty.IsVisible=false` and periodically call a helper that unregisters tracking and despawns visuals, in case the engine/UI respawns them.
   - On settlement entry, untrack `MainParty` via `VisualTrackerManager` reflection helpers.

## Consequences

### Positive
- Visually consistent “serve as a soldier” experience; the player appears attached to the commander
- Resilient to UI/engine refreshes (periodic enforcement)
- Patches remain minimal and guard-railed; behavior contains integration safeties

### Negative
- Reflection-based property access (e.g., nameplate flags) requires maintenance if API names change
- Tracker VM discovery is heuristic across several possible type names

### Risks
- Game updates may rename VM types or properties; we mitigate with multi-name discovery and reflection guards
- Over-aggressive untracking could hide wanted indicators; scope is limited to enlisted state and `MainParty`

## Alternatives Considered

- Only behavior-level toggles without Harmony patches: Insufficient, as UI refreshes can re-expose nameplates/trackers
- Deeper UI replacement: Higher risk/complexity and not needed for the goal

## Compliance

- Blueprint: Patches isolated in `Mod.GameAdapters/Patches` with structured headers and logging
- APIs within scope of Bannerlord 1.2.12 per official documentation: `https://apidoc.bannerlord.com/v/1.2.12/`

## References

- `src/Mod.GameAdapters/Patches/HidePartyNamePlatePatch.cs`
- `src/Mod.GameAdapters/Patches/MobilePartyTrackerPatches.cs`
- `src/Features/Enlistment/Application/EnlistmentBehavior.cs`
- Bannerlord API docs: `https://apidoc.bannerlord.com/v/1.2.12/`

