## Quartermaster Master Implementation Plan (Equipment + Supply + Food)

> **Purpose**: One master plan for implementing and integrating the Quartermaster features described in:
> - `docs/Features/Equipment/quartermaster-equipment-quality.md`
> - `docs/Features/Equipment/company-supply-simulation.md`
> - `docs/Features/Equipment/player-food-ration-system.md`
> - `docs/Features/Equipment/quartermaster-dialogue-implementation.md`
>
> This document also reconciles those designs with the **current in-code Quartermaster/store system**, and explicitly calls out what we will **keep**, **replace**, or **refactor**.

**Last Updated**: 2025-12-18  
**Status**: Master Implementation Plan (ready to execute in phases)

---

## Read This First (Authority + Thresholds)

- This document is the **canonical** implementation plan for Quartermaster equipment + supply + food.
- If any of the linked design docs conflict with this plan, treat that as a documentation bug and update the design doc to match this plan.
- Thresholds used throughout:
  - **Supply gate**: equipment purchases are blocked when `CompanySupply < 30` and allowed when `CompanySupply >= 30`.

## Overview

The Quartermaster is our “logistics hub” and the natural place to unify:

- **Equipment access** (buy / change kit, with supply gates and reputation-based pricing)
- **Buyback** (selling quartermaster-issued gear back at a loss)
- **Contraband + baggage checks** (muster inspections with consequences)
- **Food & provisions** (T1–T4 ration exchange; T5+ officer provisioning shop)
- **Company Supply** (a single 0–100% meter that gates access and drives scarcity)

We already have a functional Quartermaster conversation + menu pipeline. The work is mainly:

1. **Aligning pricing/rep/supply rules** to the new specs.
2. **Replacing the old food model** (virtual food link + morale “rations”) with the new ration/provisions model.
3. **Adding missing enforcement** (QM-purchased tracking, discharge reclaim, muster baggage checks).

---

## Hard Constraints (Non-Negotiable)

### 1) No native AI interference

- We must **not** modify the world’s native AI food behavior or AI party purchase logic.
- We may observe native values (e.g., `lordParty.GetNumDaysForFoodToLast()`), but we should not change:
  - `MobileParty.Food`
  - `MobileParty.FoodChange`
  - `FoodConsumptionBehavior` for AI parties
  - `PartiesBuyFoodCampaignBehavior`

### 2) No profit exploits

- Quartermaster **buyback must never be profitable**, even at maximum rep.
- Quartermaster **food for officers is always a premium** (150–200% of town price; never cheaper than 150%).

### 3) Supply gate behavior

- Company Supply < 30% means **no equipment changes** through the Quartermaster (buy buttons disabled).
- The UI must clearly communicate why something is blocked (tooltip + message).

---

## Current Code Reality (What Already Exists)

This is what we are building on and/or replacing.

### A) Quartermaster access pipeline (existing)

- **Entry**: `src/Features/Interface/Behaviors/EnlistedMenuBehavior.cs`
  - “Visit Quartermaster” opens a conversation with the Quartermaster Hero, and falls back to a direct menu if needed.
- **Quartermaster Hero**: `src/Features/Enlistment/Behaviors/EnlistmentBehavior.cs`
  - `GetOrCreateQuartermaster()` creates and persists a culture-appropriate Quartermaster hero.
  - Archetype string exists (`veteran`, `merchant`, `bookkeeper`, `scoundrel`, `believer`, `eccentric`).
  - Relationship exists, but is currently clamped to **0..100** and only provides **0–15% discounts**.
- **Conversation hub**: `src/Features/Conversations/Behaviors/EnlistedDialogManager.cs`
  - Options exist:
    - “I need equipment.” → opens `quartermaster_equipment`
    - “I want to sell equipment.” → opens `quartermaster_returns`
    - “I need better provisions.” → opens `quartermaster_rations`
    - “Chat” (relationship gain)
    - PayTension/archetype lines (black market, moral guidance, survival advice, etc.)

### B) Quartermaster “store” (existing)

- **Equipment variants store**: `src/Features/Equipment/Behaviors/QuartermasterManager.cs`
  - Sells individual items based on discovered troop variants.
  - Purchase pricing is currently:
    - `item.Value` × **SoldierTax** (default 1.2) × duty discount (0.85 if provisioner/QM officer) × CampLife mood multiplier
    - then applies `EnlistmentBehavior.ApplyQuartermasterDiscount()` (0–15%).
  - **Buyback** currently:
    - `item.Value` × **BuybackRate** (default 0.5) × CampLife buyback multiplier.
  - **Important mismatch** with the new specs:
    - Buyback is not currently tied to QM rep.
    - Return menu currently sells “any returnable item”, not “QM-purchased only”.
    - No QM-issued tracking exists for items.

### C) Schedule + Supplies meter (existing)

- `ScheduleBehavior` tracks and saves `LanceNeedsState.Supplies` (0–100) and a 12‑day cycle.
- The “Supplies” value currently degrades via `LanceNeedsManager.ProcessDailyDegradation()`.
- This is already a good place to “host” Company Supply, but the calculation must be replaced to match the hybrid simulation spec.

### D) Food system (existing — must be replaced)

- There is an active patch-based **Virtual Food Link** system:
  - `src/Mod.GameAdapters/Patches/FoodSystemPatches.cs`
  - It links the player’s food to the lord/army food and prevents starvation by patching consumption.
- This conflicts directly with the new design where:
  - T1–T4 receive **1 issued ration item** at muster and must manage scarcity.
  - T5+ must **buy** food at the Quartermaster shop.

### E) “Provisions” menu today (existing — partially replaced)

- `quartermaster_rations` currently sells morale/fatigue “food quality” bonuses (not real food items).
- Retinue provisioning exists for T7+ commanders and uses the same “rations” menu.
- The new plan introduces a **real food shop for officers** and a **ration exchange** for T1–T4, so this menu must be redesigned.

---

## Target Player Experience (Final Behavior)

### 1) Equipment (Quartermaster “store”)

Player talks to Quartermaster → sees:

- **Buy Armor**
- **Buy Weapons**
- **Buy Accessories**
- **(Optional) Buy Mounts**
- **Sell Equipment Back** (only QM‑purchased gear)
- **Provisions**
  - T1–T4: “Ask about rations” (status + explanation; rations are issued at muster, not bought)
  - T5+: “Buy provisions” (officer food shop)
- **Supply Situation** (short report)
- **Officers Armory** (T5+, high rep + lance rep)

### 2) Supply gate

- If Company Supply < 30%:
  - Equipment purchase options are disabled:
    - “We can’t issue kit right now. Supplies are critically low.”
  - Selling back equipment is still allowed (optional; design choice).
  - Food options depend on tier (see Food below).

### 3) Baggage checks (muster)

Every pay muster (12 days):

- 30% chance of “Baggage Check”
- Quartermaster scans for contraband:
  - illegal goods
  - stolen gear (per our definitions)
  - missing tracked QM-issued items (optional escalation)
- Outcome depends on QM rep:
  - Trusted: looks away
  - Friendly/Close: bribe option (50–75% value)
  - Neutral/Hostile: confiscation + fines + Heat

### 4) Food & scarcity

**T1–T4 (enlisted/NCO)**:
- At muster, Quartermaster performs **ration exchange**:
  - if issuing a new ration: reclaim previous issued ration (if present) and replace it
  - if no ration is issued due to low supply: reclaim nothing (the player keeps what they have)
  - ration quality based on QM rep
- Personal food can be bought/foraged and is subject to loss events (rats, spoilage, theft, etc.).
- Issued rations are immune to those loss events (they’re tracked/reclaimed anyway).

**T5+ (officer)**:
- No issued rations.
- Quartermaster offers a **food shop**:
  - prices are **150–200%** of town price, based on QM rep (never below 150%).
  - inventory refreshes each muster and depends on Company Supply (high supply → more/better items).

---

## System Ownership & State (Where Data Lives)

### 1) Company Supply (0–100)

**Authoritative storage**:
- `ScheduleBehavior.Instance.LanceNeeds.Supplies` (already saved)

**Authoritative computation**:
- Add a new behavior (recommended): `CompanySupplyBehavior` that updates once per day.
- This behavior writes the computed value to `ScheduleBehavior.LanceNeeds.Supplies`.

**Why not reuse current degradation directly?**
- Current `LanceNeedsManager.ProcessDailyDegradation()` is a generic degradation loop.
- The supply simulation spec is a **hybrid observation+simulation** model and needs to be computed from:
  - lord food situation (observed)
  - movement/war activity/terrain (simulated)

### 2) Quartermaster reputation / relationship

**Authoritative storage**:
- `EnlistmentBehavior` (already saved)

**Required change**:
- Expand from **0..100** to **-50..100** (per spec).
- Replace current “0–15% discount only” logic with the new multiplier table (markup + discount).

### 3) QM-purchased equipment tracking

We need tracking to support:
- sell-back restrictions (“sell back only what the QM issued”)
- discharge reclaim (“all QM-issued gear is confiscated when leaving service”)
- contraband checks (“missing issued gear” or “stolen issue”)

**Recommended storage**:
- `EnlistmentBehavior` (persistent, player-state owned).

**Recommended representation**:
- Store a list of “tracked issue records”:
  - `ItemStringId`
  - `ModifierId` (if present)
  - `Count`
  - `IssuedAtTime` / `IssuedAtTier` (optional; useful for audits)

**Important detail**:
- Inventory can hold multiple `EquipmentElement`s for the same item with different modifiers.
- When removing or reclaiming, remove the exact roster element (not a fresh `EquipmentElement(item)`).
- (QuartermasterManager already contains logic for this in `TryRemoveFromInventory`.)

### 4) Issued ration tracking

**Recommended storage**:
- `EnlistmentBehavior` (persistent).

**Representation**:
- Same approach as QM-issued gear, but separated into a “ration issue” list.
- Must support scanning/removing from:
  - party inventory
  - baggage train stash (see `EnlistmentBehavior._baggageStash`)

### 5) Officer provisions shop inventory

**Recommended storage**:
- `EnlistmentBehavior` (persistent, refreshed at muster).

**Representation**:
- List of shop entries:
  - `ItemId`
  - `Quantity`
  - `BasePrice` (town value baseline, stored for consistency)
  - optional: `RefreshMusterId` to debug refresh logic

---

## Core Tables (Rep, Supply, Pricing)

### A) Supply Gate (equipment)

| Company Supply | State | Equipment purchases |
|---:|---|---|
| 0–29 | Critical | **Blocked** |
| 30–49 | Low | Allowed, warn |
| 50–100 | Normal | Allowed |

### B) QM Reputation → Equipment purchase multiplier

Rep range: **-50..100**

| QM Rep band | Multiplier | Meaning |
|---|---:|---|
| -50 to -25 | 1.4x | 40% markup |
| -25 to -10 | 1.2x | 20% markup |
| -10 to 10 | 1.0x | standard |
| 10 to 35 | 0.9x | 10% discount |
| 35 to 65 | 0.8x | 20% discount |
| 65 to 100 | 0.7x | 30% discount |

### C) QM Reputation → Equipment buyback multiplier

| QM Rep band | Multiplier |
|---|---:|
| -50 to -25 | 0.30x |
| -25 to -10 | 0.40x |
| -10 to 10 | 0.50x |
| 10 to 35 | 0.55x |
| 35 to 65 | 0.60x |
| 65 to 100 | 0.65x |

**Anti-exploit invariant**:
- Best buy multiplier: **0.70x**
- Best sell multiplier: **0.65x**
- Therefore buyback is always less than purchase at equal rep (even before other multipliers).

### D) Officer food shop pricing (T5+ only)

| QM Rep band | Food price multiplier |
|---|---:|
| -50 to 0 | 2.0x |
| 1 to 30 | 1.9x |
| 31 to 60 | 1.75x |
| 61 to 100 | **1.5x (floor)** |

**Rule**: officer food is never cheaper than 150% of town market values.

---

## Feature-by-Feature Implementation Plan

## 1) Company Supply (Hybrid Simulation)

### Goal

Make `ScheduleBehavior.LanceNeeds.Supplies` represent “Company Supply” as defined in `company-supply-simulation.md`:

- 40% observed from lord’s food days
- 60% simulated from non-food logistics

### Implementation approach

- Create `CompanySupplyBehavior`:
  - listens to DailyTick
  - observes:
    - `enlistment.CurrentLord.PartyBelongedTo` (**always the enlisted lord’s party / the Company**, even if that party is attached to an army)
    - `GetNumDaysForFoodToLast()`
  - simulates:
    - daily non-food consumption
    - activity multiplier (moving/raiding/sieging)
    - terrain multiplier
  - writes final computed percent to `ScheduleBehavior.Instance.LanceNeeds.Supplies`.

### UI wiring

- Quartermaster menus already display pricing multipliers; add a line:
  - `Company Supply: {Supplies}% (status)`
- Add tooltip logic for supply gates.

---

## 2) Quartermaster Relationship / Rep (Rework)

### Goal

Rep becomes the primary driver for:
- equipment price markups/discounts (30% discount, 40% markup max)
- buyback rates (30–65%)
- baggage check outcomes
- ration quality (T1–T4)
- officer food pricing tier (T5+)

### Required changes

- Expand rep bounds to **-50..100** in `EnlistmentBehavior`.
- Replace the existing 0–15% discount function with multiplier functions:
  - `GetQuartermasterEquipmentPriceMultiplier()`
  - `GetQuartermasterEquipmentBuybackMultiplier()`
  - `GetQuartermasterOfficerFoodPriceMultiplier()`

### Compatibility

- Existing saves using 0..100 map directly into the new range (no migration needed).
- New negative values only appear once we start penalizing for contraband or misconduct.

---

## 3) Equipment Purchasing (Quartermaster store)

### Keep

- The existing Quartermaster Hero + conversation hub flow.
- The existing discovered variant lists and Gauntlet selector UI.
- The “hands full → stow in inventory” logic.

### Replace / Refactor

#### A) Supply gate

Add `CompanySupply >= 30` checks to:
- weapon/armor/accessory/mount purchase options
- Master-at-Arms loadout changes (if applicable)

**Blocked behavior**:
- button disabled
- tooltip: “Low Supply — cannot issue equipment”

#### B) Pricing formula alignment

Current formula includes SoldierTax + CampLife mood + small discount.
New spec wants rep-driven markups/discounts.

**Recommended final formula (equipment purchase)**:
- `finalPrice = round(item.Value × repEquipmentMult × campMoodPurchaseMult × dutyDiscountMult)`
- where:
  - `repEquipmentMult` is 0.7–1.4
  - `campMoodPurchaseMult` stays 0.98–1.15 (small, flavorful)
  - `dutyDiscountMult` stays 0.85 for provisioner/QM officer (optional, but cap total discount if needed)

**Config changes** (if we keep config multipliers):
- Consider setting `QuartermasterConfig.SoldierTax` to **1.0** (rep now handles markup/discount).
- Consider deprecating `QuartermasterConfig.BuybackRate` (rep buyback handles it).

---

## 4) Buyback & “QM-Purchased” Tracking

### Goal

- Quartermaster buyback menu should show **only QM-issued items**.
- Buyback price uses the rep buyback multipliers (30–65%), not a flat 0.5.
- Discharge must reclaim all QM-issued items.

### Required changes

#### A) Track purchases

Whenever Quartermaster issues an item:
- record it into a persistent “QM issue registry”.

#### B) Restrict sell-back list

`quartermaster_returns` should be rebuilt to only include items that:
- are present in player inventory/equipment, AND
- exist in the QM issue registry.

#### C) Buyback price

`buybackPrice = round(item.Value × repBuybackMult × campMoodBuybackMult)`

#### D) Discharge reclaim

On discharge/retirement (non-deserter):
- scan inventory + equipment
- remove all QM-issued items (quest items excluded)
- if some issued items are missing:
  - compute replacement fine and deduct from final pay / pension
  - optionally increase Heat and reduce QM rep

---

## 5) Officers Armory (Premium Gear)

### Goal

Provide a high-tier reward loop:
- gated by rank and rep
- offers quality modifiers (Fine/Masterwork) where appropriate

### Requirements

- Rank: **T5+**
- QM rep: **60+**
- Lance rep: **50+** (wherever lance rep is tracked; if not yet implemented, gate only by tier+QM rep for now)

### Inventory source

Two practical options:

1) **Troop template expansion**:
   - Allow selection from 1–2 tiers above player tier for loadouts (culture restricted).
2) **Item pool expansion**:
   - Allow higher-tier items in variant store and apply modifiers.

### Item modifiers

- Use Bannerlord’s native `ItemModifier` system on `EquipmentElement`.
- Apply only in Officers Armory context.
- Keep a conservative modifier set at first:
  - Fine / Masterwork

---

## 6) Muster: Ration Exchange (T1–T4)

### Goal

Replace the old “virtual food link” model with a tangible ration item loop:

- 12-day muster cycle
- if issuing a new ration: reclaim old issued ration and replace it
- if no ration is issued due to low supply: reclaim nothing
- ration quality based on QM rep

### Hook points

The pay muster resolves in:
- `EnlistmentBehavior.ResolvePayMusterStandard()`
- and other muster variants (e.g., corruption muster)

Add “Muster Logistics” processing there:

- **Step A**: resolve pay (existing)
- **Step B**: run ration exchange (if tier < 5)
- **Step C**: run baggage check roll (if enabled)
- **Step D**: refresh officer provisions shop (if tier >= 5)
- **Step E**: notify schedule reset (already done)

### Ration availability by supply

| Company Supply | Chance |
|---:|---:|
| 70–100 | 100% |
| 50–69 | 80% |
| 30–49 | 50% |
| < 30 | 0% |

### Ration quality by QM rep

| QM Rep | Item |
|---|---|
| -50..19 | `grain` |
| 20..49 | `butter` |
| 50..79 | `cheese` |
| 80..100 | `meat` |

---

## 7) Officer Provisions Shop (T5+)

### Goal

At T5+:
- stop issuing rations entirely
- offer a Quartermaster food shop
- inventory refreshes every muster
- supply controls inventory quality/quantity
- rep controls premium pricing (150–200% of town values)

### Implementation outline

- Store a “QM food inventory” list in `EnlistmentBehavior`.
- Refresh on muster resolution.
- Create a Quartermaster submenu (either a new menu `quartermaster_provisions` or reuse `quartermaster_rations` but repurpose it).

**Important**: This replaces the current “morale/fatigue food quality buff” rations menu for T5+.

---

## 8) Muster: Baggage Checks (Contraband)

### Goal

Make muster feel like a real military checkpoint:
- periodic inspection
- consequences based on rep
- ties into Heat/contraband systems

### Trigger

- Every muster: 30% roll (tunable).

### Contraband detection

Start with a conservative definition:
- stolen high-value civilian goods (custom list)
- flagged contraband items (if we add a list)
- missing QM-issued items (optional escalation)

### Outcomes (by rep)

Use the table from `quartermaster-equipment-quality.md`:
- Trusted: look away
- Close/Friendly: bribe option
- Neutral/Hostile: confiscation + fines + Heat

---

## 9) Retiring the old food implementation (Virtual Food Link)

### Goal

Remove or disable the “virtual food link” patches once the ration system is live.

### Plan

- Introduce a config feature flag:
  - `UseVirtualFoodLinkWhileEnlisted` (default: false once ration system is enabled)
- When the ration system is enabled:
  - do not patch `MobileParty.TotalFoodAtInventory`
  - do not patch `FoodConsumptionBehavior.PartyConsumeFood`

This is the most important “integration replacement” step to match the new scarcity design.

---

## Menus & Dialogue Changes (Required)

### A) Quartermaster conversation hub

Add new *direct* options, but keep compatibility with existing menus:

- Buy Armor → opens `quartermaster_equipment` with armor filter
- Buy Weapons → opens `quartermaster_equipment` with weapon filter
- Buy Accessories → opens `quartermaster_equipment` with accessory filter
- Sell Equipment Back → opens `quartermaster_returns`
- Provisions:
  - T1–T4: “Ask about rations” (status text)
  - T5+: “Buy provisions” (opens provisions shop)
- Supply Situation → text response (reads Company Supply)
- Officers Armory → opens armory menu when eligible

### B) Quartermaster equipment menu

Rename and reorganize existing options to match the new experience (purely UX; same backend).

---

## Phased Rollout Plan (Recommended Order)

### Phase 1 — Wire Company Supply (read-only UI)
- Implement supply computation (even if simplified)
- Show Company Supply in Quartermaster UI

### Phase 2 — Rep system rework
- Expand to -50..100
- Implement multiplier tables for equipment + buyback

### Phase 3 — QM-purchased tracking + buyback restriction + discharge reclaim
- Track items issued by QM
- Restrict sell-back to QM-issued items only
- Reclaim QM-issued items on discharge

### Phase 4 — Replace food system
- Implement T1–T4 ration exchange at muster
- Implement personal food loss events
- Disable virtual food link patches (behind config)

### Phase 5 — Officer provisions shop (T5+)
- Add shop inventory refresh at muster
- Add purchase UI/menu
- Implement premium pricing (150–200%)

### Phase 6 — Muster baggage checks
- Add inspection roll at muster
- Implement contraband detection + consequences
- Integrate with Heat/contraband meters

### Phase 7 — Officers Armory
- Add eligibility gate
- Build inventory source + apply modifiers

### Phase 8 — Polish and balancing
- tooltips, messaging, localization strings
- tune multipliers and probabilities

---

## Acceptance Criteria (Definition of Done)

### Company Supply
- Supply is stable and updates daily while enlisted.
- Supply < 30 blocks equipment purchases and communicates why.

### Equipment pricing
- At max rep, equipment costs are ~70% of item value (before small mood/duty factors).
- At hostile rep, equipment costs are ~140% of item value (before small mood/duty factors).

### Buyback
- Only QM-issued items appear in the buyback menu.
- Buyback can never exceed purchase price under any circumstances.

### Food (T1–T4)
- Muster every ~12 days performs ration exchange.
- Supply controls ration availability; rep controls ration quality.
- Personal food can be lost; issued ration is not targeted.

### Food (T5+)
- No issued rations.
- QM shop sells food at 150–200% of value; never lower than 150%.
- Stock refreshes each muster and depends on supply.

### Baggage checks
- Trigger at muster with correct probability and outcomes.
- Rep tiers determine look-away / bribe / confiscation behavior.

---

## Open Design Decisions (Need a quick yes/no)

1) **Do we allow selling issued rations?**
   - The spec tolerates it (consumed or sold = no penalty), but it can become “free money” at high rep.
   - Options:
     - Allow it (small income, low impact)
     - Prevent it (mark issued ration as unsellable via custom item or sale restrictions)

2) **Does “Sell Equipment Back” accept only QM-issued gear (spec), or any gear (current)?**
   - Spec: QM buys back only tracked gear.
   - Current: sells any weapon/armor/mount.
   - This plan assumes **spec behavior** (QM-issued only).

3) **Retinue provisioning (current feature): keep, or fold into officer provisions later?**
   - This plan: keep for now, refactor later.


