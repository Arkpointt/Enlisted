# ADR-017: Conditional IgnoreByOtherPartiesTill Safety While Enlisted

Status: Accepted
Date: 2025-08-31
Deciders: Enlisted Team

## Context

While enlisted, the player's `MainParty` is hidden and generally inactive. Nearby world AI may still attempt to target the player party under some conditions. We need a low-risk mitigation without changing global AI.

## Decision

- Periodically extend `MobileParty.MainParty.IgnoreByOtherPartiesTill(CampaignTime.Now + CampaignTime.Hours(0.5f))` while enlisted to discourage world AI from selecting the hidden main party as a target.
- Conditional use:
  - Enabled when the commander is not in an army (solo commander contexts).
  - Disabled when merged into an army to avoid unintended effects on army targeting dynamics.

## Consequences

Positive:
- Reduces spurious engagements against an invisible/inactive player party.
- Simple, bounded, and reversible safety.

Negative/Risks:
- Overuse could mask edge cases; we bound the window and disable it while in army contexts.

## Compliance

- Blueprint Sections: 4.9 (Conditional Ignore AI Safety).
- Code: `EnlistmentBehavior` tick loops under `ShouldUseIgnore()`.

## References

- Bannerlord API 1.2.12 `MobileParty.IgnoreByOtherPartiesTill`


