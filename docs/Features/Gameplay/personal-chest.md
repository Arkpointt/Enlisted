# Feature Spec: Personal Chest

## Overview
A sealed Personal Chest kept by the quartermaster that lets enlisted soldiers store personal belongings, buy/upgrade capacity, and safely manage weight when toggling between duty and leave.

## Purpose
- Give players a lore-appropriate place to keep personal gear while enlisted.
- Prevent soft-locks from overweight inventories when switching to leave/retirement.
- Preserve existing accountability: issued kit remains mandatory for duty and battle.

## Inputs/Outputs
**Inputs**
- Player enlistment state (enlisted, leave, discharge, capture, grace).
- Chest tier (purchased upgrades) and capacity limits.
- Player carry state: `MobileParty.TotalWeightCarried` vs `InventoryCapacity` (1.3.4).
- Access context (camp/town via synthetic encounter; on leave).

**Outputs**
- Chest inventory state (single container we serialize).
- Automatic auto-restore of issued loadout before battle/promotion.
- Overweight handling decisions (store excess, block, or cart with fee).
- Logs in `Modules/Enlisted/Debugging/enlisted.log` under category `Chest`.

## Behavior
- Access points:
  - Enlisted camp/town menus (synthetic town access flow), and while on leave.
  - No access while marching, in siege/battle, in reserve, or inside active MapEvents/PlayerEncounters.
- Single container:
  - One Personal Chest roster, not a world prop; “lodged” with the quartermaster wherever you access it.
  - Capacity is slot/weight themed and scales with purchased tier upgrades.
- Issued vs personal:
  - Issued loadout remains tracked by Troop Selection/Quartermaster accountability.
  - Before any battle/promotion, auto-restore issued kit; if items are missing, run existing fines.
  - Personal Chest never masks debt or replaces issued tracking.
- Overweight handling:
  - On leave start / discharge / capture release: if `TotalWeightCarried > InventoryCapacity`, prompt to auto-store excess into the chest (default) or pay for a temporary cart bonus (short-lived) if we add that perk; otherwise block the transition.
  - When withdrawing from the chest, warn if the result would exceed capacity and allow “store excess back” instead of letting the player crawl.
  - When not in town and overweight, allow “seal with the army” (contents stay in the same chest) and reclaim next time you reach camp/town.
- Upgrades/economy:
  - Buy-in: initial fee to obtain the Personal Chest.
  - Upgrades: Standard → Reinforced → Officer’s Chest; each adds capacity. Quartermaster duty may grant a small slot bonus or fee reduction.
  - Optional small access fee while enlisted; free on leave. Shipping/cart fees are configurable.
- Logging:
  - Log open/close, swaps, auto-restores, overweight interventions, and fines under `Chest` in the single session log.
- Flavor/RP:
  - “Your Personal Chest is sealed under the quartermaster’s eye. Off-duty you may don your own harness; on the march, you wear the lord’s colors.”

## Phased Plan
- Phase 1 (baseline):
  - Single Personal Chest container; purchase + tiered upgrades.
  - Access from camp/town/leave; no world props.
  - Auto-restore issued gear before battle/promotion; overweight guard on leave/discharge.
  - Chest logging category added.
- Phase 2 (logistics polish):
  - Optional paid “cart” grace (temporary capacity bump until next settlement).
  - Configurable fees/tiers and UI tooltips for weight warnings.
  - Quartermaster duty perk hooks (capacity or fee waiver).
  - Capture/army-destroyed flows: chest remains sealed and reclaimable at next camp/town.

## Edge Cases
- Leave start while overweight: must store excess or pay cart; otherwise blocked. If cart option exists, it is temporary and expires on next settlement entry.
- Discharge/retirement while overweight: same guard; avoid reactivating an immobile party. Offer to seal chest locally or cart it for a fee.
- Capture release: before restoring control, run overweight guard; chest is intact with the army. If player refuses to store excess and cannot pay, block until resolved.
- Army destroyed / lord slain: chest auto-sealed; reclaim at next camp/town access point. If player deserts before reclaim, contents can be lost by design.
- Desertion: chest forfeited unless ransom fee is paid; losing the chest is an explicit risk/reward lever.
- Naval battles: issued auto-restore still runs pre-battle to avoid ship assignment edge cases; chest access disabled while at sea.
- Insufficient gold for fees: fallback to “store excess” (no move) and block if the player refuses; no negative gold states.
- Town lodging vs. on-road: In town, player can “lodge” the chest (flavor only; same container). On road, sealing “with the army” keeps contents accessible at next camp/town access.
- Town Stash expectation: Bannerlord has no native town stash; the Personal Chest is our only container. “Town lodging” is just a safe access point to the same chest, not a second inventory.

## Acceptance Criteria
- Personal Chest purchased once and upgraded through tiers; capacity scales accordingly.
- Accessible only in camp/town menus or while on leave; never during march/battle/siege/reserve/encounters.
- Issued loadout auto-restores before battle/promotion; missing issued gear still fined via existing path.
- Overweight guards on leave/discharge/capture release: player cannot become active while overweight without storing excess or paying a cart fee.
- Single serialized chest container (no world prop, no reliance on vanilla town stash).
- Logs written under `Chest` for access, swaps, auto-restores, overweight handling, and fines in `Modules/Enlisted/Debugging/enlisted.log`.

