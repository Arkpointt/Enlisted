# Quartermaster Overhaul — Design Notes

## Index
- [Goals](#goals)
- [What to remove/retire](#what-to-removeretire)
- [Core loops](#core-loops)
  - [Standard Issue (free, strict)](#standard-issue-free-strict)
  - [Under-the-Table Store](#under-the-table-store)
  - [Fatigue Actions](#fatigue-actions)
- [Data & Config](#data--config)
- [UI/Menu](#uimenu)
- [Behavior changes](#behavior-changes)
- [Logging & Safety](#logging--safety)
- [Open Items](#open-items)
- [Baggage Train & Inventory Handling](#baggage-train--inventory-handling)
- [Native API Implementation Plan (1.3.10)](#native-api-implementation-plan-1310)
- [Enlistment Day Script & Risk](#enlistment-day-script--risk)
- [Loot Deposit, Horses, Discharge, Theft](#loot-deposit-horses-discharge-theft)

## Goals
- Keep Tier 1 enlistment auto-issue.
- For Tier 2+, no auto-equip on promotion or Master at Arms; gear becomes purchasable.
- Quartermaster becomes the sole gateway: standard issue (strict) + under-the-table commerce with fatigue-driven favors.

## What to remove/retire
- Equipment accountability debt/charges before re-issue.
- Auto-equip on promotions beyond Tier 1 (already disabled).
- Any direct auto-upgrade flows that bypass Quartermaster trade.

## Core loops

### Standard Issue (free, strict)
- Always available; issues rank-appropriate kit, takes old gear (no resale).
- If an item is missing (e.g., shield lost), apply gold/debt penalty before issuing replacement.
- Delivers exact template; no variants/upsell.

### Under-the-Table Store
- Menu: “Show me the ‘Extra’ Stock.”
- Pricing: “Soldier Tax” markup: town price +20% to buy; buyback to player at 50% of town price (hard floor).
- Stock:
  - Off-books/illegal weapons or armor not normally allowed by rank/role.
  - Food/comfort upgrades (meat/cheese, better rations).
  - Buys player loot at poor rates if rank-restricted (e.g., Tier 5 helm when Tier 2).
- Officer Stock (locked behind charm action) for high-tier pieces.
- Rank locks still apply to equipping; purchase allowed.

### Fatigue Actions
- Work (Tier 1–3): Cost 8 Fatigue → random small freebie or discount.
- Charm/Share a drink (Tier 4+): Cost 4 Fatigue + 1 Wine → Trade XP + unlock Officer Stock for this visit.
- Roguery/Cook the Books: Cost 6 Fatigue → success +200 gold; failure: relation loss with QM (and optionally lord/army).

## Data & Config
- Pricing multipliers: `soldier_tax` (default 1.2 buy), `buyback_rate` (default 0.5 sell), `officer_stock_tax` (e.g., >1.2).
- Toggles for fatigue actions and officer stock.
- Stock pools:
  - `standard_issue` by tier/formation.
  - `extra_stock` for off-books items, food, black-market goods.
  - `officer_stock` gated by charm action.

## UI/Menu
- Quartermaster menu options:
  - Request Standard Kit (0 Fatigue; applies strict issue, confiscates old gear, penalizes missing items).
  - Show “Extra” Stock (trade with markup).
  - Work for Gear (Tier 1–3; 8 Fatigue; freebie/discount).
  - Share a Drink (Tier 4+; 4 Fatigue + 1 Wine; unlock Officer Stock, grant Trade XP).
  - Cook the Books (6 Fatigue; gold on success, relation loss on fail).
- Hints: show fatigue costs, markups, and penalties in text responses.

## Behavior changes
- Tier 1: keep enlistment auto-issue.
- Tier 2+: promotions and Master at Arms do not auto-equip; they only register troop selection and unlock gear for purchase.
- Standard Issue flow:
  - Identify rank template; replace kit; confiscate old gear; if missing items, charge gold/debt then issue replacements.
- Under-the-Table trade:
  - Build stock from `extra_stock`/`officer_stock`; apply Soldier Tax (1.2x buy), Officer Tax as needed; buybacks at 0.5x town price.
- Fatigue gating:
  - Use `TryConsumeFatigue`; fail-fast with messaging if insufficient fatigue or missing wine (for charm).

## Logging & Safety
- Log issued templates, penalties, fatigue spends, and under-the-table transactions.
- Guard against null lord/culture; don’t crash if stock pools empty—fallback to standard issue message.
- Keep enlistment flow intact; don’t alter party/time control.

## Open Items
- Tune tax multipliers and rewards (freebie tables, discount ranges).
- Decide relation hit targets for failed roguery (QM vs. lord).
- Balance buyback penalties and officer-stock pricing.

## Baggage Train & Inventory Handling
Goal: prevent recruits from wearing 50k crowns on day one, without deleting player gear. Treat enlistment as a logistics event with a “baggage train” stash.

### Bag Check (deferred map incident after enlistment)
- Service starts immediately; the bag check is scheduled for 12 in-game hours later and fires as a native map incident (via `MapState.NextIncident`), not a regular game menu. It waits for a safe state (no battle/encounter/captivity).
- If the incident system is unavailable, it falls back to the same inquiry prompt so enlistment is never blocked.
- Choices when the incident fires:
  - Stow in Baggage Train (Stash): move all inventory + equipped items into a virtual stash stored in mod data. Cost: free first enlistment or 50g “wagon fee”. Result: player inventory cleared; Tier 1 kit issued.
  - Liquidate everything (Sell-Off): auto-sell all items at 50% value. Result: inventory cleared; player gets gold + Tier 1 kit.
  - Smuggle one item: keep exactly one chosen item; everything else stashed. Risk: future camp/inspection event may reprimand for non-standard gear.

### Accessing the stash (“Visit Baggage Train”)
- Camp action; Cost: 4 Fatigue.
- Text: digging through crates to retrieve your chest.
- Opens inventory transfer between player inventory and virtual stash.
- Enables smuggling old high-tier gear back if the player dares.

### Risk on defeat (army routed/captured)
- If the main army is defeated while you are enlisted/captured: 50% chance the stash is lost or severely reduced (“enemy burned the wagons”).
- Notify the player on loss; adjust stash contents accordingly.

### Notes
- Stash is per-enlistment data; survives save/load but can be wiped by defeat.
- Integrate with Quartermaster menus (or a dedicated camp menu) to reach the baggage train action.

## Native API Implementation Plan (1.3.10)
- Inventory/stash surfaces:
  - Use `InventoryScreenHelper.OpenScreenAsStash(ItemRoster stash)` for baggage train access (fatigue-gated action).
  - For under-the-table trade, use `InventoryScreenHelper.OpenScreenAsTrade(ItemRoster, SettlementComponent, ...)` with a custom InventoryListener (markup, poor buyback) or clone `MerchantInventoryListener` logic; alternatively a custom `InventoryLogic` instance with adjusted price factors.
  - Stash storage: maintain an `ItemRoster` (virtual stash) on an Enlisted behavior; persist via `SyncData`.
- Confiscation/issue:
  - To confiscate old gear: clone `Hero.BattleEquipment`/`CivilianEquipment`, move items into stash or sell-off list, then `EquipmentHelper.AssignHeroEquipmentFromEquipment` for Tier 1 issue.
  - To sell-off: iterate `ItemRoster` (inventory + equipment-converted items) and apply 50% value to gold, then clear.
- Bag check hook:
  - Integrate in enlistment flow (before fade): present dialog with three choices; on accept, run stash/sell/smuggle logic, then call Tier 1 issue.
- Fatigue gating:
  - Reuse `EnlistmentBehavior.TryConsumeFatigue(amount, reason)` for Work/Charm/Roguery/Baggage Train actions; fail-fast with UI message if insufficient.
- Risk on defeat:
  - Subscribe to `CampaignEvents.MapEventEnded` (player map event, defeated side == player) or captivity events; if routed/captured, roll 50% to clear/reduce stash.
- Menu/UI:
  - Quartermaster game menu options call into behavior methods that:
    - Standard Issue: pick rank template, confiscate old gear, penalize missing items, assign template.
    - Extra Stock: open trade with markup listener and custom stock roster.
    - Officer Stock unlock: set a session flag after Charm action; same trade with different roster/tax.
    - Baggage Train: open stash via `OpenScreenAsStash` (fatigue cost).
- Data plumbing:
  - Maintain last selected troop (already) for stock building.
  - Config keys for taxes, buyback penalties, stock pools, wagon fee, defeat-loss chance.

## Enlistment Day Script & Risk
Goal: Treat enlistment as a scripted “check-in” event that defines carry vs. stash, smuggling risk, and future vulnerability.

### On-Person vs. Baggage
- On Person (restricted): worn kit + 1–2 small items (food/dagger); enforced after enlistment.
- Baggage (stash): everything else; only reachable via camp/baggage actions (fatigue-gated by rank).

### Enlistment event (deferred bag check)
- Triggered ~12 in-game hours after enlistment as a native map incident; enlistment does not pause waiting for it. If the incident system is missing, the prompt falls back to an inquiry.
- Options:
  - Stow it all (Standard): move all inventory + equipped into stash; cost 50g admin fee (first enlistment can be free toggle). Issue Tier 1 kit.
  - Sell it all (Liquidate): auto-sell at 60% market value; give gold; issue Tier 1 kit.
  - Smuggle one item (Roguery check > 30): keep one selected item; rest stashed. On failure: item confiscated (lost), -5 relation.
- Implementation: schedule bag check on enlistment; when safe, set `MapState.NextIncident` to the bag-check incident; on failure, fall back to inquiry and still complete the action; then run Tier 1 issue using `EquipmentHelper.AssignHeroEquipmentFromEquipment`.
- Storage: virtual stash `ItemRoster` persisted via `SyncData`.

### Accessing stash (Baggage Train action)
- Camp action “Visit Baggage Train.”
- Fatigue cost by rank: Tier1–2: 4 fatigue; Tier3–4: 2; Tier5+: 0.
- Opens stash via `InventoryScreenHelper.OpenScreenAsStash(stashRoster)`.
- Text hints about wagon/locker access speed.

### Catastrophe (stash loss hook)
- Trigger: main army routed/defeated (player side loses MapEvent) or capture flow.
- Event text about wagons burning.
- Branch:
  - Let it burn: stash deleted, player HP safe.
  - Try to save chest: costs HP (e.g., -50), Athletics check:
    - Success: save ~50% of stash.
    - Failure: lose stash, take HP hit, risk capture flag.
- Implementation: on defeat, roll; present menu; adjust stash roster accordingly; notify.

### UI feedback
- Menu header example: `Rank: Recruit (Tier 1) | Fatigue: 18/24 | Stash: Secure (Wagon 4)`
- Danger state: `Stash: AT RISK (Rear Guard Failing)` when army starving/retreating or after a rout trigger.

## Loot Deposit, Horses, Discharge, Theft

### Loot Deposit (post-battle / camp)
- Problem: Rank 1 can’t wear new loot; needs a way to stash it.
- Action: “Deposit Loot” (post-battle menu or camp).
- Cost: 2 Fatigue (deposit-only, faster than full stash access).
- Text: sprint to wagons, shove loot into crate before formation.
- Constraint: If 0 Fatigue, cannot deposit; must sell cheap to Quartermaster (50% rate) or drop.
- Implementation: call `OpenScreenAsStash` with stash roster, but limit to moving from loot to stash; enforce fatigue pre-check.

### Horses (remount officer) on enlistment
- Problem: personal horses can’t go in crates.
- Enlistment dialog branch:
  - Sell horse → gold.
  - Donate to regiment → +relation with lord, lose horse.
  - Let it go → lose horse.
- Rule: Only Tier 5+ (knights/noble recruits) may keep a personal horse stabled (no fee).
- Implementation: detect horse items in inventory/roster; route through choice; remove or keep accordingly.

### Discharge / Muster Out
- Trigger: retire/contract end/promotion to lord.
- Flow:
  - Strip army-issue gear (unless high relation perk lets you keep it).
  - Move stash -> main inventory; if over capacity/weight, open loot exchange UI for player to drop/choose.
  - Text: QM hands back crate; warns you.

### Internal Theft Risk (camp incident)
- Trigger: low morale or low relation with troops (camp loop).
- Event: stash crate lock smashed; random item missing.
- Options:
  - Report: QM shrugs; likely nothing recovered.
  - Search recruits: Fatigue 6 + Roguery/Tactics check.
    - Success: recover item; duel/beat option; gain XP.
    - Fail: wrong accusation; lose relation.
- Implementation: camp incident; adjust stash roster; skill checks gate outcome.

