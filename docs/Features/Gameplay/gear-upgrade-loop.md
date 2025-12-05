## Overview
- Goal: keep the army-issue/accountability loop, make issued gear start “scuffed” (low-quality modifiers), and add a fun repair → upgrade path that ties into promotions and camp revelry.
- Scope: Quartermaster issuance, promotion turnover, repair/upgrade actions, optional pay-to-retain, and small morale touches via camp revelry.
- Out of scope: SAS modifiers (not used); any new durability system (none in Bannerlord).

## Index
- [Overview](#overview)
- [Purpose](#purpose)
- [Inputs / Outputs](#inputs--outputs)
- [Behavior](#behavior)
  - [Phasing: Apothecary expansion (alongside this loop)](#phasing-apothecary-expansion-alongside-this-loop)
- [Edge Cases](#edge-cases)
- [Acceptance Criteria](#acceptance-criteria)

## Purpose
- Preserve realism: army reclaims issued kit on promotion and tracks missing gear.
- Add meaningful progression: turn scuffed issue gear into a ladder of repairs and upgrades with time/cost gates.
- Keep baseline free: requisition stays free; players only pay for loss, repairs, and voluntary upgrades.
- Make promotion feel fair: reclaim gear, but carry value forward via credits/vouchers; optional “buy out” at a steep price.

## Inputs / Outputs
- Inputs:
  - Player tier (gates max quality: T4 Fine cap, T5 Masterwork, T6 Legendary chance).
  - Recent performance/duty tokens (discounts or cooldown skips).
  - Gold (repair/upgrade costs), time (cooldowns), and current item modifier per slot.
  - Promotion event (triggers reclaim/credit).
  - Camp revelry actions (optional morale bumps when repairs/upgrades finish).
- Outputs:
  - Updated equipment elements per slot with a chosen native ItemModifier (rusty/chipped/worn → standard → fine → masterwork → legendary).
  - Cooldown timers per slot and per “priority requisition” token.
  - Retention credit value applied to first repair/upgrade after promotion.
  - Notifications/tooltips (current modifier, next step, cost, cooldown, credit applied).

## Behavior
- Issuance (free):
  - Quartermaster issues the standard kit for the troop.
  - Levy/Tier 1: no negative modifier roll; gear stays standard.
  - Tier 2+: roll a low-quality modifier on most slots (e.g., 60–70% chance) using the item’s ItemModifierGroup (native rusty/chipped/worn equivalents). This keeps the loop alive without charging for the issue.
  - Track issued state for accountability (unchanged).
- Repair (camp smith via Quartermaster):
  - Action: “Repair” resets a slot to standard (no modifier).
  - Cost: modest; Cooldown: per-slot (e.g., 3–7 days).
  - Applies any promotion credit to reduce this cost first.
- Upgrade (camp smith via Quartermaster):
  - Ladder: Standard → Fine → Masterwork → (optional) Legendary.
  - Cost: rising per step; Cooldown: per-slot (e.g., 7–14 days; 30 for Legendary).
  - Gates: tier caps (T4 Fine, T5 Masterwork, T6 Legendary chance); optionally require a duty token or performance flag for Legendary attempts.
  - “Priority requisition” token: once per 30 days, pay 2× cost to skip a cooldown for one slot.
- Promotion handling:
  - Reclaim all issued gear; clear accountability debt.
  - Issue new kit for the new role, with the same scuffed-roll step.
  - Retention credit: 50–70% of the player’s previous upgrade spend is banked and auto-applied to the first repair/upgrade on the new kit (per slot or pooled—choose pooled for simplicity).
  - Optional “buy out” toggle: Player may pay full replacement cost + penalty to keep current gear as personal. If kept, it must be legal for the new troop type (or disabled in battle if not). Price it high to avoid trivializing the loop.
- Loss/forfeit:
  - If gear is lost/stolen/forfeited, accountability charges apply (unchanged). Reissue uses the scuffed-roll again; upgrades must be re-earned.
- Camp revelry tie-ins (optional but additive):
  - Completing a repair/upgrade grants a small temporary morale bump (RecentEventsMorale) to simulate company pride.
  - Feast could temporarily reduce repair costs or cooldown by a small percentage for its duration (optional).

## UI / Copy / Flavor (concise, readable, light RP)
- Menu labels:
  - “Request Repair” — tooltip: “Our smith will true your kit. Standard issue restored. Fee applies; cooldown {DAYS} days.”
  - “Request Upgrade” — tooltip: “Quartermaster pulls a finer piece. Cost: {COST}{GOLD_ICON}; cooldown: {DAYS} days.”
  - “Priority Requisition” — tooltip: “Pay double to skip one cooldown this month.”
  - “Retention Credit” — tooltip: “Your prior care reduces this action by {CREDIT}{GOLD_ICON}.”
  - “Buy Out Gear” — tooltip: “Purchase and keep this kit as personal property. Steep fee; must fit your current role.”
- Status strings:
  - Damaged: “Issue Quality: Scuffed”
  - Standard: “Issue Quality: Standard”
  - Fine/Masterwork/Legendary: “Issue Quality: Fine / Masterwork / Legendary”
- Promotion flavor (brief, readable):
  - “Well done, soldier. Turn in your kit; the quartermaster has your new harness waiting.”
  - If credit applies: “We remember your care for the gear. Your next refit will cost less.”
- Apothecary UI (Phase 2 opt-in):
  - “Send to Apothecary” — tooltip: “Treat selected companions. Partial heal; 7-day cooldown; cost scales with patients.”
  - “Eligible for care” toggle — tooltip: “Mark this companion for the next medical round.”
- Light camp snippets:
  - Repair success: “The smith wipes the soot. ‘As good as army issue gets. Don’t dent it tomorrow.’”
  - Upgrade success: “A crate slides across the table. ‘Don’t say the quartermaster never did you a kindness.’”
  - Buy-out warning: “You sure? That’s Crown property. Pay the fee and it’s yours—else it stays in the ledger.”

### Phasing: Apothecary expansion (alongside this loop)
- Phase 1 (core): Apothecary applies to player + retinue only; 7-day cooldown; cost scales with treated count; modest heal (not full top-off).
- Phase 2 (companions opt-in): Add a toggle in Companion Assignments (“Eligible for care”). Only flagged companions are included in the next apothecary batch.
- Safety: Companions set to “Stay Back” are excluded unless wounded via non-battle events.
- UI: In Command Tent → Companion Assignments, add “Send to Apothecary” that queues selected companions; show wounded count and cooldown timer.
- Balance: Keep batch heal partial; cap per batch; keep the same cooldown; log treated heroes and cost.

## Edge Cases
- Illegal gear for new troop type after “buy out”: disallow equipping in battle; allow cosmetic in town or refund with penalty.
- Party inventory full when reclaiming: overflow to inventory rules remain; ensure no duplication.
- Multiple promotions in short time: retention credit should overwrite with the most recent spend snapshot (avoid stacking credits).
- Cooldown persistence: store per-slot timers so menu hopping cannot reset them.
- Legendary attempts without eligibility: gray out with tooltip; never silently fail.

## Acceptance Criteria
- Issuance remains free; accountability on loss remains intact.
- Issued gear rolls a low-quality modifier on issuance for Tier 2+ only (Levy/ Tier 1 stays standard); no new durability system is introduced.
- Repair action resets a slot to standard, with per-slot cooldown and cost.
- Upgrade ladder works per slot, respects tier caps, and enforces cooldowns/costs.
- Promotion reclaims old kit, issues new kit, and applies a retention credit toward the first repair/upgrade; optional buy-out is expensive and gated.
- No SAS modifiers are used; only native ItemModifier IDs from ItemModifierGroups.
- UI surfaces current modifier, next step, cost, cooldown, and any credit applied.

