# Feature Spec: Troop Selection (Master at Arms)

## Overview
Troop Selection lets enlisted players choose which **real Bannerlord troop template** they represent (culture + tier). This choice drives:
- Your **troop identity** (kit source) for the Quartermaster
- Your **formation identity** (Infantry/Archer/Cavalry/Horse Archer)
- Tier 1 only: a one-time **auto-issue** equipment replacement

## Purpose
- Keep progression grounded in native troop trees (culture-appropriate roles)
- Provide a single place (Master at Arms) to update your role expression without changing your tier
- Prevent “free gear escalation” after Tier 1 by requiring **Quartermaster purchases** for Tier 2+

## Inputs / Outputs

**Inputs**
- Current enlisted lord culture
- Current tier (Tier 1–6)
- Promotion flow (optional) and Master at Arms access

**Outputs**
- Selected troop template stored (last selected troop ID)
- Tier 1: immediate equipment assignment (auto-issue / full replacement)
- Tier 2+: no auto-issue; chosen troop’s branch becomes eligible for Quartermaster purchase pools

## Behavior

### Master at Arms popup
- Available while enlisted from the main enlisted status menu.
- Builds a list of unlocked troops for the lord’s culture where troop tier ≤ your current tier.
- On selection:
  - If selected troop tier ≤ 1 → **auto-issue** the chosen kit (replaces equipment)
  - If selected troop tier > 1 → **no auto-issue**; selection updates your “current kit identity” for Quartermaster availability

### Promotions
On tier promotion, the same selection flow can be used to pick a troop identity appropriate to the new tier.

## Equipment access
- Tier 1: auto-issued kit replacement.
- Tier 2+: equipment is **purchased** at the Quartermaster (no accountability/issued-gear tracking).

## Technical implementation
- `src/Features/Equipment/Behaviors/TroopSelectionManager.cs`
  - Builds culture troop trees and unlock lists
  - Shows the inquiry popup
  - Applies Tier 1 auto-issue and records last selected troop
- `src/Features/Interface/Behaviors/EnlistedMenuBehavior.cs`
  - Exposes Master at Arms entry point

## Related docs
- [Quartermaster](../UI/quartermaster.md)
- [Enlistment](../Core/enlistment.md)
