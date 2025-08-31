# ADR-013: Army Handling via Escort/Merge and Auto-Join Behavior

Status: Accepted
Date: 2025-08-31
Deciders: Enlisted Team

## Context

As an enlisted soldier, the player should naturally move with and participate in their commander's engagements. When the commander forms or joins an army, the player should be treated as part of that merged blob. During battles initiated by the commander, the player must be reliably included without forcing custom events.

Target: Bannerlord 1.2.12 (`https://apidoc.bannerlord.com/v/1.2.12/`).

## Decision

- Follow the commander using public AI escort:
  - `MobileParty.MainParty.Ai.SetMoveEscortParty(_trackedCommander.Army?.LeaderParty ?? _trackedCommander)`
- When the commander is in an army, ensure the player's party is merged/attached to that army for blob treatment.
- Auto-join commander engagements:
  - If the commander enters a `MapEvent` and the player is not already in one, briefly set `MainParty.IsActive = true` and gently nudge near the commander to allow engine-driven inclusion as reinforcement.
  - Do not nudge toward other friendly parties if the commander is not involved.
- Inside settlements/siege: skip auto-join attempts and defer until outside to avoid menu/encounter contention.

## Consequences

Positive:
- Leverages stable engine flows; minimal custom encounter routing.
- Works with both solo commander and army contexts.

Negative/Risks:
- Over-nudging could cause visible jumps; mitigated by gentle offset and only when commander is involved.
- Army merge relies on current engine behavior for blobs; monitor across updates.

## Compliance

- Blueprint Sections: 4.6 (Army & Encounter Handling), 4.8 (Camera/Tracker), 4.9 (Ignore AI Safety).
- Code: `src/Features/Enlistment/Application/EnlistmentBehavior.cs` tick and map-event hooks.

## References

- Bannerlord API 1.2.12
- Existing ADRs: ADR-011 (Tracker/Nameplate), ADR-012 (Menu/Time Control)


