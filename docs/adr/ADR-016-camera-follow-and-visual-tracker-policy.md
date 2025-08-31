# ADR-016: Camera Follow and Visual Tracker Policy While Enlisted

Status: Accepted
Date: 2025-08-31
Deciders: Enlisted Team

## Context

During enlistment the player's perspective should remain on the commander (or the army leader) and the world HUD should focus on that party. The engine can sometimes reset camera follow or respawn trackers/nameplates.

## Decision

- Camera follow cadence: periodically reassert camera follow to `(commander.Army?.LeaderParty ?? commander).Party` while enlisted.
- Visual tracker routing: register the commander in `VisualTrackerManager` and unregister `MainParty` while enlisted; periodically enforce due to engine/UI refreshes.
- Keep `MobileParty.MainParty.IsVisible = false` while enlisted; optionally perform visual despawn via adapters if needed.
- Backstop UI presentation with small Harmony patches (see ADR-011) to suppress `MainParty` nameplate and prevent tracker VM from re-adding it.

## Consequences

Positive:
- Stable camera/overlay focus on the commander experience.
- Resilient to UI refreshes and state churn.

Negative:
- Requires periodic enforcement (bounded cost) and reflection-based calls for tracker management.

## Compliance

- Blueprint Sections: 4.8 (Camera/Tracker/Visibility).
- Code: `EnlistmentBehavior` tick, `TryUntrackAndDespawn` helper; `Mod.GameAdapters` patches.

## References

- ADR-011 (Nameplate/Tracker suppression)
- Bannerlord API 1.2.12 `PartyBase.SetAsCameraFollowParty`, `VisualTrackerManager`


