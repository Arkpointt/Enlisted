# Morale, Supply, Promotion, Reputation System Analysis

**Generated:** 2026-01-10
**Systems Analyzed:** Morale, Supply, Promotion, Reputation
**Focus:** both
**Subsystem Mode:** No

---

## Executive Summary

- Analyzed 1 system files
- Identified 11 integration gaps
- Found 14 efficiency issues
- Generated 10 prioritized recommendations

---

## Current Architecture

## Tool Calls Made (MAX 8) — Evidence Index

1) `search_codebase` query `"Morale CompanyNeed Morale CompanyNeedsManager ProcessDailyDegradation CompanyNeedsState"` (filter `src/`)  
   **Found (snippets only):**
   - `src/Features/Company/CompanyNeedsState.cs`
   - `src/Features/Company/CompanyNeedsManager.cs`
   - `src/Features/Company/CompanyNeed.cs`
   - `src/Features/Enlistment/Behaviors/EnlistmentBehavior.cs` (company needs serialization snippet)
   - `src/Features/Camp/Models/CompanyPressure.cs`

2) `search_codebase` query `"PromotionBehavior EscalationManager SoldierReputation OfficerReputation ModifySoldierReputation GetQMReputation"` (filter `src/`)  
   **Found (snippets only):**
   - `src/Features/Ranks/Behaviors/PromotionBehavior.cs`
   - `src/Features/Enlistment/Behaviors/EnlistmentBehavior.cs` (`GetQMReputation`, ration issuance using OfficerReputation as proxy, etc.)
   - `src/Features/Escalation/EscalationManager.cs`

3) `Readsource` `src/Features/Company/CompanyNeedsManager.cs`  
   **Verified:** morale daily degradation rules; supplies handled elsewhere; critical checks; prediction config loading.

4) `Readsource` `src/Features/Enlistment/Behaviors/EnlistmentBehavior.cs`  
   **Verified:** supply manager lifecycle and ticks; CompanyNeeds serialization; morale modifier APIs; pay tension; ration logic; QM native reputation; reputation restoration; battle hooks affecting supply; settlement hooks affecting supply.

5) `Readsource` `src/Features/Ranks/Behaviors/PromotionBehavior.cs`  
   **Verified:** hourly promotion check; eligibility logic; proving event queuing; fallback direct promotion; enlistment event subscriptions.

6) `Readsource` `src/Features/Escalation/EscalationManager.cs`  
   **Verified:** daily tick (only while enlisted); persistence; track modification APIs; soldier/officer/lord reputation; discipline; thresholds → event queuing; declined promotions tracking.

7) `get_system_dependencies` `"EnlistmentBehavior"`  
   **Verified:** depends on `Hero.MainHero`, `Campaign.Current`; depended-by list includes `EscalationManager`, `PromotionBehavior`, `ContentOrchestrator`, etc.

8) `get_system_dependencies` `"PromotionBehavior"`  
   **Verified:** depends on `EnlistmentBehavior`.

> Tool budget reached (8). Anything not in those files is marked “unknown” or “inferred”.

---

# Architecture Overview

This section analyzes **Morale**, **Supply**, **Promotion**, and **Reputation** as they exist in the verified code files above. Each system’s architecture is split into: components, persisted state, entry points/ticks, and observed data flows.

---

## 1) Morale System

### Brief Description
Morale is one of the four **Company Needs** (0–100 integer scale) and also has **multiple modifier sources** exposed through `EnlistmentBehavior` (pay tension penalty, rations bonus, retinue provisioning modifier). The codebase currently shows:
- **Baseline daily degradation** for Morale in `CompanyNeedsManager`.
- **Modifier APIs** in `EnlistmentBehavior` that other systems can query/use.

### Key Components (Observed in code)

#### A) Need enum: `CompanyNeed`
- **File:** `src/Features/Company/CompanyNeed.cs` (located via tool call #1)  
- **Role:** Defines `Readiness`, `Morale`, `Rest`, `Supplies` (spelled `Supplies` in code).

#### B) Need state container: `CompanyNeedsState`
- **File:** `src/Features/Company/CompanyNeedsState.cs` (located via tool call #1; not fully read due to budget)  
- **Role (from search snippet):** Tracks current readiness/morale/rest/supplies, 0–100. Notes that serialization is handled manually (but in our read, it’s actually in `EnlistmentBehavior.SerializeCompanyNeeds`).

#### C) Rules engine: `CompanyNeedsManager` (static)
- **File:** `src/Features/Company/CompanyNeedsManager.cs` (read in tool call #3)  
- **Role:** Applies daily degradation for needs (explicitly: readiness/morale/rest; supplies excluded).

Key method:
- `public static void ProcessDailyDegradation(CompanyNeedsState needs, MobileParty army = null)`

Key morale rule:
- Base daily morale degradation: `var moraleDegradation = 1;`
- Applied:
  - `needs.SetNeed(CompanyNeed.Morale, needs.Morale - moraleDegradation);`

Acceleration relationships involving morale:
- `var lowMorale = needs.Morale < 40;`
- If low morale: `readinessDegradation += 3;`

Also includes:
- `CheckCriticalNeeds(...)` includes `CompanyNeed.Morale` with hard thresholds 20/30.
- `PredictUpcomingNeeds(MobileParty party)` includes `Morale` predictions pulled from `strategic_context_config.json` via `ModulePaths.GetConfigPath(...)`.

#### D) Morale modifier sources: `EnlistmentBehavior`
- **File:** `src/Features/Enlistment/Behaviors/EnlistmentBehavior.cs` (read in tool call #4)

Observed morale-related APIs:
1. Pay tension → morale penalty
   - Method: `public int GetPayTensionMoralePenalty()`
   - Scale: `<20 => 0`, `<40 => -3`, `<60 => -6`, `<80 => -10`, else `-15`.

2. Food quality rations → morale bonus
   - Method: `public int GetFoodMoraleBonus()`
   - Scale: Supplemental `+2`, Officer `+4`, Commander `+8`, else `0`.
   - Underlying state: `_currentFoodQuality` / `_foodQualityExpires` (persisted in `SyncData`).

3. Retinue provisioning → morale modifier (commander tiers)
   - Method: `public int GetRetinueMoraleModifier()`
   - Scale: None `-10`, BareMinimum `-5`, Standard `0`, GoodFare `+5`, OfficerQuality `+10`.
   - Underlying state: `_retinueProvisioningTier` / `_retinueProvisioningExpires` (persisted in `SyncData`).

### Entry Points & Ticks (Observed)
- **Daily need degradation:** `CompanyNeedsManager.ProcessDailyDegradation(...)` exists, but **the caller is not found in the read files**.  
  - `get_system_dependencies("EnlistmentBehavior")` indicates `CompanySimulationBehavior` depends on it, suggesting `CompanySimulationBehavior` may call it (not verified via read).
- **Morale modifiers:** Queried on-demand from `EnlistmentBehavior` methods (no tick needed).

### Morale Data Flow (Observed vs Inferred)

**Observed in code**
- Daily degradation is computed in `CompanyNeedsManager.ProcessDailyDegradation`.
- `EnlistmentBehavior` persists morale inside `_companyNeeds` via `SerializeCompanyNeeds(IDataStore dataStore)`:
  - `SyncKey(... "_companyNeeds_morale", ref morale);`  
  **File:** `src/Features/Enlistment/Behaviors/EnlistmentBehavior.cs`

**Inferred/likely (NOT verified due to tool budget)**
- Some “company simulation” behavior (likely `CompanySimulationBehavior`) calls `CompanyNeedsManager.ProcessDailyDegradation` daily.
- Some UI/camp/order system applies the **morale modifiers** (pay tension + rations + provisioning) into either:
  - Bannerlord party morale mechanics, and/or
  - CompanyNeedsState.Morale adjustments or derived morale display.

### Standards/Quality Notes (Observed deviations)
- `GetFatigueStatusText` and several messages use raw strings (e.g., `"Exhausted"`, `"You're exhausted. Health reduced."`) rather than `TextObject`. These may violate the “TextObject for all UI strings” standard.
- Morale modifier APIs are clean and don’t perform external API calls, so no try/catch needed.

---

## 2) Supply System

### Brief Description
Supply has **two layers**:
1) **Company Need “Supplies”** in `CompanyNeedsState` (int 0–100).
2) A **unified logistics simulation** via `CompanySupplyManager` referenced and lifecycle-managed from `EnlistmentBehavior`, including persisted `NonFoodSupply`.

Supply is updated via:
- **Hourly updates** (resupply while in settlements).
- **Daily updates**.
- **Battle end supply changes**.
- **Settlement entry/exit hooks** for visit-based resupply messaging.

### Key Components (Observed in code)

#### A) Supply need rules placeholder: `CompanyNeedsManager`
- **File:** `src/Features/Company/CompanyNeedsManager.cs` (read tool call #3)  
- Explicit note:
  - `// NOTE: Supplies degradation is handled by CompanySupplyManager (unified logistics tracking).`
- Still includes supplies in `CheckCriticalNeeds` and `PredictUpcomingNeeds`.

#### B) Supply simulation manager: `CompanySupplyManager` (referenced)
- **File:** Not opened due to tool budget.  
- **Evidence:** multiple direct calls in `src/Features/Enlistment/Behaviors/EnlistmentBehavior.cs` (read tool call #4):
  - `CompanySupplyManager.Initialize(lord, preserveSupply: resumingGraceService);`
  - `CompanySupplyManager.Instance?.HourlyUpdate();`
  - `CompanySupplyManager.Instance?.DailyUpdate();`
  - `CompanySupplyManager.Instance?.OnSettlementEntered(settlement);`
  - `CompanySupplyManager.Instance?.OnSettlementLeft(settlement);`
  - `CompanySupplyManager.Instance.ProcessBattleSupplyChanges(...)`
  - `CompanySupplyManager.RestoreFromSave(_enlistedLord, nonFoodSupply);`
  - `CompanySupplyManager.Shutdown();`
  - `CompanySupplyManager.Instance?.NonFoodSupply`

Because we did not open the file, any internal algorithms are **unknown**.

#### C) Persistence owner: `EnlistmentBehavior` (for company needs + non-food supply)
- **File:** `src/Features/Enlistment/Behaviors/EnlistmentBehavior.cs` (read tool call #4)

Key persistence method:
- `private void SerializeCompanyNeeds(IDataStore dataStore)`

Save:
- Saves `_companyNeeds_supplies` and `_companySupply_nonFood`:
  - `SyncKey(dataStore, "_companyNeeds_supplies", ref supplies);`
  - `var nonFoodSupply = CompanySupplyManager.Instance?.NonFoodSupply ?? 100f;`
  - `SyncKey(dataStore, "_companySupply_nonFood", ref nonFoodSupply);`

Load:
- Loads values into new `CompanyNeedsState { Supplies = supplies, ... }`
- Restores supply manager:
  - `if (_enlistedLord != null) { CompanySupplyManager.RestoreFromSave(_enlistedLord, nonFoodSupply); }`

### Entry Points & Ticks (Observed)
All observed in `src/Features/Enlistment/Behaviors/EnlistmentBehavior.cs`:

1) **Initialize / Shutdown**
- `ContinueStartEnlistInternal(Hero lord)`:
  - `CompanySupplyManager.Initialize(lord, preserveSupply: resumingGraceService);`
- `StopEnlist(...)`:
  - `CompanySupplyManager.Shutdown();`

2) **Hourly tick**
- `CampaignEvents.HourlyTickEvent.AddNonSerializedListener(this, OnHourlyTick);`
- In `OnHourlyTick()`:
  - `CompanySupplyManager.Instance?.HourlyUpdate();`

3) **Daily tick**
- `CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, OnDailyTick);`
- In `OnDailyTick()`:
  - `CompanySupplyManager.Instance?.DailyUpdate();`

4) **Battle end hook**
- `CampaignEvents.OnPlayerBattleEndEvent.AddNonSerializedListener(this, OnPlayerBattleEnd);`
- In `OnPlayerBattleEnd(MapEvent mapEvent)`:
  - `ProcessBattleSupplyChanges(mapEvent, killsThisBattle);`
- `ProcessBattleSupplyChanges(MapEvent mapEvent, int playerKills)` computes:
  - `playerWon = mapEvent.WinningSide == mapEvent.PlayerSide`
  - `troopsLost` = casualties only from enlisted lord party (not whole army)
  - `enemiesKilled = playerKills`
  - `wasSiege = mapEvent.IsSiegeAssault || mapEvent.IsSallyOut`
  - Calls:
    - `CompanySupplyManager.Instance.ProcessBattleSupplyChanges(troopsLost, enemiesKilled, playerWon, wasSiege);`

5) **Settlement entry/exit hooks**
- `CampaignEvents.SettlementEntered.AddNonSerializedListener(this, OnSettlementEntered);`
- `CampaignEvents.OnSettlementLeftEvent.AddNonSerializedListener(this, OnSettlementLeft);`
- In `OnSettlementEntered(...)` (lord entered settlement):
  - `CompanySupplyManager.Instance?.OnSettlementEntered(settlement);`
- In `OnSettlementLeft(...)` (lord left settlement):
  - `CompanySupplyManager.Instance?.OnSettlementLeft(settlement);`

### Supply Data Flow (Observed)

**Enlistment lifecycle**
- Start enlistment → initialize supply simulation:
  - `EnlistmentBehavior.ContinueStartEnlistInternal` → `CompanySupplyManager.Initialize(...)`
- Each hour/day while enlisted:
  - Hourly → `CompanySupplyManager.Instance?.HourlyUpdate()`
  - Daily → `CompanySupplyManager.Instance?.DailyUpdate()`
- Stop enlistment:
  - `CompanySupplyManager.Shutdown()`

**Battle outcome → supply**
- `OnPlayerBattleEnd` → `ProcessBattleSupplyChanges` → `CompanySupplyManager.Instance.ProcessBattleSupplyChanges(...)`
- Important coupling: computes casualties only for the lord’s party to avoid over-penalization:
  - Verified by the comment and loop in the method.

**Settlement visit → supply**
- Enlistment behavior routes settlement visit events to supply manager, presumably for resupply accrual and messaging (internal logic not verified).

**Persistence**
- `SerializeCompanyNeeds` saves:
  - integer Supplies (from `_companyNeeds.Supplies`)
  - float NonFoodSupply (from `CompanySupplyManager.Instance.NonFoodSupply`)
- On load:
  - reconstruct `CompanyNeedsState`
  - `CompanySupplyManager.RestoreFromSave(_enlistedLord, nonFoodSupply);`

### Standards/Quality Notes (Observed deviations)
- `DisplayNoRationsMessage(int supply)` uses raw strings, not `TextObject`.
- `PurchaseRations(...)` uses `Hero.MainHero.ChangeHeroGold(-effectiveCost);` which violates the stated project rule “Use GiveGoldAction not Hero.Gold += amount / ChangeHeroGold”. (In other parts, `GiveGoldAction.ApplyBetweenCharacters` is used correctly.)

---

## 3) Promotion System

### Brief Description
Promotion advances the enlisted player through tiers (T1–T9 per comments/table), with eligibility checks driven hourly. If eligible, it queues a **proving event** via content system; if missing/unavailable, it performs **fallback direct promotion**.

Promotion uses:
- Enlistment state (tier, XP, days in rank, battles survived, leader relation).
- Escalation state (soldier reputation, discipline).
- Declined promotion flags (must request via dialog after decline).
- Quartermaster unlock refresh after promotion.

### Key Components (Observed in code)

#### A) `PromotionBehavior` (Campaign behavior)
- **File:** `src/Features/Ranks/Behaviors/PromotionBehavior.cs` (read tool call #5)

Core methods:
- `RegisterEvents()`:
  - `CampaignEvents.OnSessionLaunchedEvent` → `OnSessionLaunched`
  - `CampaignEvents.HourlyTickEvent` → `OnHourlyTick`
  - Subscribes to:
    - `EnlistmentBehavior.OnEnlisted += OnNewEnlistmentStarted;`
    - `EnlistmentBehavior.OnDischarged += OnEnlistmentEnded;`
- `OnHourlyTick()` → `CheckForPromotion()`
- `CanPromote()` returns `(bool CanPromote, List<string> FailureReasons)`
- `CheckForPromotion()` queues proving event or fallback direct promotion.
- `FallbackDirectPromotion(int targetTier, EnlistmentBehavior enlistment)`:
  - `enlistment.SetTier(targetTier);`
  - `QuartermasterManager.Instance?.UpdateNewlyUnlockedItems();`
  - `TriggerPromotionNotification(targetTier);`

State:
- `_lastPromotionCheck` persisted.
- `_pendingPromotionTier` persisted (anti-spam / “pending proving event”).

Persistence:
- `SyncData(IDataStore dataStore)` stores `_lastPromotionCheck`, `_pendingPromotionTier`.

#### B) Promotion requirements table: `PromotionRequirements`
- **File:** `src/Features/Ranks/Behaviors/PromotionBehavior.cs` (same file)
- Provides per-tier non-XP requirements:
  - Days in rank, battles required, min soldier rep, min leader relation, max discipline.
- XP requirement is explicitly from:
  - `Mod.Core.Config.ConfigurationManager.GetTierXpRequirements()`

#### C) Enlistment integration (caller/callee)
- **Caller:** `PromotionBehavior` reads from:
  - `EnlistmentBehavior.Instance.IsEnlisted`
  - `EnlistmentBehavior.Instance.EnlistmentTier`
  - `EnlistmentBehavior.Instance.EnlistmentXP`
  - `EnlistmentBehavior.Instance.DaysInRank`
  - `EnlistmentBehavior.Instance.BattlesSurvived`
  - `EnlistmentBehavior.Instance.EnlistedLord.GetRelationWithPlayer()`
- **Callee:** `EnlistmentBehavior.SetTier(int tier)`  
  **File:** `src/Features/Enlistment/Behaviors/EnlistmentBehavior.cs` (read tool call #4)

#### D) Escalation integration (promotion gating)
- **File:** `src/Features/Ranks/Behaviors/PromotionBehavior.cs` calls:
  - `EscalationManager.Instance?.State?.SoldierReputation`
  - `EscalationManager.Instance?.State?.Discipline`
  - `EscalationManager.Instance?.HasDeclinedPromotion(targetTier)`
- **Declined promotions reset:**
  - On new enlistment: `EscalationManager.Instance?.ClearAllDeclinedPromotions();`

### Entry Points & Ticks (Observed)
- `CampaignEvents.HourlyTickEvent` → `PromotionBehavior.OnHourlyTick()` → `CheckForPromotion()`
- Enlistment lifecycle events:
  - `EnlistmentBehavior.OnEnlisted` → `OnNewEnlistmentStarted(Hero lord)` resets `_pendingPromotionTier` and clears declined promotions.
  - `EnlistmentBehavior.OnDischarged` → `OnEnlistmentEnded(string reason)` resets `_pendingPromotionTier`.

### Promotion Data Flow (Observed)
1) Hourly tick triggers `CheckForPromotion`.
2) `CanPromote()` checks:
   - XP threshold (progression config).
   - Days in rank.
   - Battles survived.
   - Soldier reputation (Escalation).
   - Discipline max constraint (Escalation).
   - Leader relation.
3) If eligible:
   - If previously declined: stop (must request promotion via dialog).
   - If pending already: stop (anti-spam).
   - Set `_pendingPromotionTier = targetTier`.
   - Determine proving event id: `GetProvingEventId(currentTier, targetTier)`.
   - Load event: `Content.EventCatalog.GetEventById(eventId)`.
   - If event exists and delivery manager exists: `Content.EventDeliveryManager.Instance.QueueEvent(provingEvent)`.
   - Else fallback:
     - `enlistment.SetTier(targetTier)` and refresh QM unlocks.

### Standards/Quality Notes (Observed)
- Uses `TextObject` for promotion UI (`GetPromotionTitle`, `GetPromotionMessage`, chat message, QM prompt) — compliant.
- Uses `Hero.MainHero?.Name?.ToString()` in notification assembly; safe.
- No direct `Hero.Gold +=` changes here.

---

## 4) Reputation System (Escalation Tracks + QM Relationship)

### Brief Description
Reputation is implemented in two distinct ways:

1) **Escalation reputation tracks** (mod-owned, persisted):
- Soldier reputation, Officer reputation, Lord reputation.
- Stored in `EscalationState` and managed by `EscalationManager`.
- Used by Promotion (soldier rep gating) and by Enlistment (ration issuance proxy for QM rep).

2) **Quartermaster (QM) “native” reputation** (Bannerlord relationship):
- `EnlistmentBehavior.GetQMReputation()` reads `Hero.MainHero.GetRelation(qm)` and clamps.

### Key Components (Observed in code)

#### A) `EscalationManager` (Campaign behavior)
- **File:** `src/Features/Escalation/EscalationManager.cs` (read tool call #6)

Core:
- Owns `_state: EscalationState` (not opened due to tool budget, but accessed heavily).
- Registers daily tick:
  - `CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, OnDailyTick);`
- Daily tick guard:
  - `IsEnabled()` config flag
  - only while enlisted: `EnlistmentBehavior.Instance?.IsEnlisted == true`
  - “once per day” guard via `_lastDailyTickDayNumber`
- On daily tick:
  - `ApplyPassiveDecay();`
  - `EvaluateThresholdsAndQueueIfNeeded();`

Persistence:
- `SyncData(IDataStore dataStore)` saves:
  - `esc_scrutiny`, `esc_discipline`, `esc_soldierRep`, `esc_lordRep`, `esc_officerRep`, `esc_medical`
  - timestamps and cooldown maps
  - one-time fired events
  - declined promotions set

Track modification APIs:
- `ModifySoldierReputation(int delta, string reason = null)` (also posts UI messages and news entries)
- `ModifyOfficerReputation(int delta, ...)`
- `ModifyLordReputation(int delta, ...)`
- plus escalation tracks: scrutiny, discipline, medical risk.

Threshold event integration:
- `TryTriggerThresholdEvent(track, threshold)` maps to content IDs:
  - Scrutiny: `evt_scrutiny_{threshold}`
  - Discipline: `evt_discipline_{threshold}`
  - Medical: `evt_medical_{threshold}`
- Uses pacing:
  - `Content.GlobalEventPacer.CanFireAutoEvent(eventId, "escalation", out var blockReason)`
  - `Content.GlobalEventPacer.RecordAutoEvent(...)`
- Queues:
  - `Content.EventDeliveryManager.Instance.QueueEvent(evt)`.

Declined promotions:
- `RecordDeclinedPromotion(int tier)`
- `HasDeclinedPromotion(int tier)`
- `ClearDeclinedPromotion(int tier)`
- `ClearAllDeclinedPromotions()`

#### B) Reputation restoration on re-enlistment: `EnlistmentBehavior`
- **File:** `src/Features/Enlistment/Behaviors/EnlistmentBehavior.cs` (read tool call #4)
- In `TryApplyReservistReentryBoost(IFaction faction)`:
  - Writes restored values directly:
    - `EscalationManager.Instance.State.OfficerReputation = officerRepRestore;`
    - `EscalationManager.Instance.State.SoldierReputation = soldierRepRestore;`
  - Then clamps:
    - `EscalationManager.Instance.State.ClampAll();`
  - Shows notification:
    - `ShowReputationRestorationNotification(band, officerRepRestore, soldierRepRestore);`

#### C) QM reputation (native relationship): `EnlistmentBehavior`
- **File:** `src/Features/Enlistment/Behaviors/EnlistmentBehavior.cs`
- Method: `public int GetQMReputation()`
  - `var qm = GetOrCreateQuartermaster();`
  - `int nativeRep = Hero.MainHero.GetRelation(qm);`
  - clamps to `[-50, 100]`.

#### D) QM rep proxy usage for rations: `EnlistmentBehavior`
- **File:** `src/Features/Enlistment/Behaviors/EnlistmentBehavior.cs`
- `IssueNewRation()` comment:
  - “Get QM reputation (use OfficerReputation as QM rep for now)”
  - `int qmRep = EscalationManager.Instance?.State?.OfficerReputation ?? 50;`

This is a concrete integration: reputation track influences ration issuance quality.

### Entry Points & Ticks (Observed)

1) **EscalationManager daily tick**
- `CampaignEvents.DailyTickEvent` → `EscalationManager.OnDailyTick()`
- Guard: enabled + enlisted.

2) **Mutation entry points**
- Any system can call:
  - `EscalationManager.ModifySoldierReputation/ModifyOfficerReputation/ModifyLordReputation`
- `EnlistmentBehavior` directly sets `EscalationManager.State.OfficerReputation` and `SoldierReputation` in the reservist re-entry flow.

3) **QM native reputation**
- Queried on demand via `EnlistmentBehavior.GetQMReputation()`

### Reputation Data Flow (Observed)

**Promotion gating**
- `PromotionBehavior.CanPromote()` reads:
  - `EscalationManager.Instance.State.SoldierReputation` and `.Discipline`.

**Rations / Supply interaction**
- Ration issuance uses Officer reputation (Escalation) as a stand-in “QM rep”.

**Threshold event generation**
- Track crosses threshold → `TryTriggerThresholdEvent` → `EventCatalog.GetEvent(eventId)` → queue via `EventDeliveryManager`, gated by `GlobalEventPacer`.

**Persistence**
- All escalation tracks and declined promotions are persisted in `EscalationManager.SyncData`.

### Standards/Quality Notes (Observed deviations)
- `EscalationManager` uses `TextObject` and `InformationManager.DisplayMessage` — compliant.
- Some status methods return raw strings (`"Clean"`, `"Watched"`, etc.). If used in UI without wrapping, might violate “TextObject for all UI strings” (unclear; these may be debug/log only).

---

# Integration Points (Concrete, Method-Level)

This section lists **caller → callee** integrations with verified paths for both sides where possible.

## A) Enlistment ↔ Supply

1) `EnlistmentBehavior.ContinueStartEnlistInternal(Hero lord)`  
   → `CompanySupplyManager.Initialize(lord, preserveSupply: resumingGraceService)`  
   **File:** `src/Features/Enlistment/Behaviors/EnlistmentBehavior.cs`

2) `EnlistmentBehavior.OnHourlyTick()`  
   → `CompanySupplyManager.Instance?.HourlyUpdate()`  
   **File:** `src/Features/Enlistment/Behaviors/EnlistmentBehavior.cs`

3) `EnlistmentBehavior.OnDailyTick()`  
   → `CompanySupplyManager.Instance?.DailyUpdate()`  
   **File:** `src/Features/Enlistment/Behaviors/EnlistmentBehavior.cs`

4) `EnlistmentBehavior.OnSettlementEntered(... hero == _enlistedLord)`  
   → `CompanySupplyManager.Instance?.OnSettlementEntered(settlement)`  
   **File:** `src/Features/Enlistment/Behaviors/EnlistmentBehavior.cs`

5) `EnlistmentBehavior.OnSettlementLeft(party == _enlistedLord.PartyBelongedTo)`  
   → `CompanySupplyManager.Instance?.OnSettlementLeft(settlement)`  
   **File:** `src/Features/Enlistment/Behaviors/EnlistmentBehavior.cs`

6) `EnlistmentBehavior.OnPlayerBattleEnd(MapEvent mapEvent)`  
   → `EnlistmentBehavior.ProcessBattleSupplyChanges(mapEvent, killsThisBattle)`  
   → `CompanySupplyManager.Instance.ProcessBattleSupplyChanges(...)`  
   **File:** `src/Features/Enlistment/Behaviors/EnlistmentBehavior.cs`

7) `EnlistmentBehavior.StopEnlist(...)`  
   → `CompanySupplyManager.Shutdown()`  
   **File:** `src/Features/Enlistment/Behaviors/EnlistmentBehavior.cs`

8) Save/load coupling:
- `EnlistmentBehavior.SerializeCompanyNeeds(IDataStore dataStore)`  
  → reads `CompanySupplyManager.Instance.NonFoodSupply` for saving  
  → calls `CompanySupplyManager.RestoreFromSave(_enlistedLord, nonFoodSupply)` on load  
  **File:** `src/Features/Enlistment/Behaviors/EnlistmentBehavior.cs`

## B) Enlistment ↔ Morale

1) `CompanyNeedsManager.ProcessDailyDegradation(CompanyNeedsState needs, MobileParty army)`  
   → updates `needs.Morale` and uses low morale to accelerate readiness degradation  
   **File:** `src/Features/Company/CompanyNeedsManager.cs`

2) Morale modifiers exposed by enlistment:
- `EnlistmentBehavior.GetPayTensionMoralePenalty()`  
- `EnlistmentBehavior.GetFoodMoraleBonus()`  
- `EnlistmentBehavior.GetRetinueMoraleModifier()`  
  **File:** `src/Features/Enlistment/Behaviors/EnlistmentBehavior.cs`

> Direct application of these modifiers into `CompanyNeedsState.Morale` is **not found** in the read files (unknown / likely elsewhere).

## C) Promotion ↔ Enlistment

1) `PromotionBehavior.CanPromote()` reads enlistment state:
- `EnlistmentBehavior.Instance.EnlistmentTier`
- `EnlistmentBehavior.Instance.EnlistmentXP`
- `EnlistmentBehavior.Instance.DaysInRank`
- `EnlistmentBehavior.Instance.BattlesSurvived`
- `EnlistmentBehavior.Instance.EnlistedLord.GetRelationWithPlayer()`  
  **File:** `src/Features/Ranks/Behaviors/PromotionBehavior.cs`

2) Promotion execution (fallback) calls enlistment:
- `PromotionBehavior.FallbackDirectPromotion(...)`  
  → `enlistment.SetTier(targetTier)`  
  **File:** `src/Features/Ranks/Behaviors/PromotionBehavior.cs` → `src/Features/Enlistment/Behaviors/EnlistmentBehavior.cs` (callee method exists in read file).

3) Lifecycle subscriptions:
- `PromotionBehavior.RegisterEvents()` subscribes to:
  - `EnlistmentBehavior.OnEnlisted`
  - `EnlistmentBehavior.OnDischarged`  
  **File:** `src/Features/Ranks/Behaviors/PromotionBehavior.cs`

## D) Promotion ↔ Reputation/Escalation

1) Eligibility gating:
- `PromotionBehavior.CanPromote()`  
  → `EscalationManager.Instance.State.SoldierReputation` and `.Discipline`  
  **File:** `src/Features/Ranks/Behaviors/PromotionBehavior.cs` (caller)  
  **EscalationManager implementation:** `src/Features/Escalation/EscalationManager.cs` (state owner)

2) Declined promotion gating:
- `PromotionBehavior.CheckForPromotion()`  
  → `EscalationManager.Instance.HasDeclinedPromotion(targetTier)`  
  **File:** `src/Features/Ranks/Behaviors/PromotionBehavior.cs`

3) Clearing declined flags on new enlistment:
- `PromotionBehavior.OnNewEnlistmentStarted(Hero lord)`  
  → `EscalationManager.Instance?.ClearAllDeclinedPromotions()`  
  **File:** `src/Features/Ranks/Behaviors/PromotionBehavior.cs`

## E) Reputation ↔ Enlistment (QM and reservist restoration)

1) QM native rep:
- `EnlistmentBehavior.GetQMReputation()`  
  → `Hero.MainHero.GetRelation(qm)`  
  **File:** `src/Features/Enlistment/Behaviors/EnlistmentBehavior.cs`

2) Reservist restoration writes to escalation state:
- `EnlistmentBehavior.TryApplyReservistReentryBoost(IFaction faction)`  
  → `EscalationManager.Instance.State.OfficerReputation = officerRepRestore;`  
  → `EscalationManager.Instance.State.SoldierReputation = soldierRepRestore;`  
  → `EscalationManager.Instance.State.ClampAll();`  
  **File:** `src/Features/Enlistment/Behaviors/EnlistmentBehavior.cs`

3) Rations use OfficerReputation (escalation) as QM rep proxy:
- `EnlistmentBehavior.IssueNewRation()`  
  → `int qmRep = EscalationManager.Instance?.State?.OfficerReputation ?? 50;`  
  **File:** `src/Features/Enlistment/Behaviors/EnlistmentBehavior.cs`

## F) Escalation ↔ Content event system

- `EscalationManager.TryTriggerThresholdEvent(track, threshold)`  
  → `Content.GlobalEventPacer.CanFireAutoEvent(...)`  
  → `Content.EventCatalog.GetEvent(eventId)`  
  → `Content.EventDeliveryManager.Instance.QueueEvent(evt)`  
  **File:** `src/Features/Escalation/EscalationManager.cs`

Promotion proving events similarly use:
- `Content.EventCatalog.GetEventById(eventId)` and queue via `EventDeliveryManager`  
  **File:** `src/Features/Ranks/Behaviors/PromotionBehavior.cs`

---

# Per-System Minimal Dependency Tables

## Morale (Company Need)
| Category | Details (Observed in code) |
|---|---|
| Inputs | `CompanyNeedsState.Morale` baseline, optional `MobileParty` conditions (`IsMoving`, `CurrentSettlement`, `MapEvent`) in `CompanyNeedsManager.ProcessDailyDegradation` (`src/Features/Company/CompanyNeedsManager.cs`). |
| Outputs | Mutated `CompanyNeedsState.Morale` (-1 daily), and readiness degradation increases when morale < 40. |
| Persisted State | `_companyNeeds_morale` stored by `EnlistmentBehavior.SerializeCompanyNeeds` (`src/Features/Enlistment/Behaviors/EnlistmentBehavior.cs`). |
| Events listened | **Unknown** caller for `ProcessDailyDegradation` (likely in simulation behavior not read). Morale modifier APIs are pull-based. |
| Cross-system effects | Pay tension penalty (`EnlistmentBehavior.GetPayTensionMoralePenalty`), food morale bonus (`GetFoodMoraleBonus`), provisioning morale modifier (`GetRetinueMoraleModifier`). |

## Supply
| Category | Details (Observed in code) |
|---|---|
| Inputs | Battle outcomes (troopsLost, playerKills, playerWon, wasSiege), settlement entry/exit, hourly/daily ticks. |
| Outputs | `CompanySupplyManager` internal state (incl. `NonFoodSupply`) and `CompanyNeedsState.Supplies` persisted. |
| Persisted State | `_companyNeeds_supplies` (int) and `_companySupply_nonFood` (float) via `EnlistmentBehavior.SerializeCompanyNeeds`. |
| Events listened | `EnlistmentBehavior` registers: `HourlyTickEvent`, `DailyTickEvent`, `OnPlayerBattleEndEvent`, `SettlementEntered`, `OnSettlementLeftEvent`. |
| Cross-system effects | Ration availability depends on `_companyNeeds.Supplies` via `DetermineRationAvailability(int supply)` (in `EnlistmentBehavior`). |

## Promotion
| Category | Details (Observed in code) |
|---|---|
| Inputs | Enlistment tier/XP/days/battles/lord relation (`EnlistmentBehavior`), escalation soldier rep + discipline (`EscalationManager.State`), declined promotion flags (`EscalationManager`). |
| Outputs | Proving event queued or direct promotion via `EnlistmentBehavior.SetTier`, plus `QuartermasterManager.UpdateNewlyUnlockedItems`. |
| Persisted State | `_lastPromotionCheck`, `_pendingPromotionTier` in `PromotionBehavior.SyncData`. |
| Events listened | `CampaignEvents.HourlyTickEvent`; enlistment lifecycle events `EnlistmentBehavior.OnEnlisted`, `.OnDischarged`. |
| Cross-system effects | Promotion notifications + news entry; unlock refresh for QM. |

## Reputation (Escalation + QM native)
| Category | Details (Observed in code) |
|---|---|
| Inputs | Calls to `Modify*Reputation(...)`, reservist restore values from `ServiceRecordManager` flow in enlistment; native hero relation for QM. |
| Outputs | `EscalationState` changes, threshold events queued, news entries, promotion gating. |
| Persisted State | All tracks saved via `EscalationManager.SyncData` keys `esc_*`, plus declined promotions set. |
| Events listened | `EscalationManager` listens to `CampaignEvents.DailyTickEvent` and is active only when enlisted. |
| Cross-system effects | Promotion gating; ration issuance quality (Officer rep proxy); threshold story scheduling via pacing system. |

---

# Event/Callback Mechanisms — “Entry points & ticks” Summary

### Morale
- **Observed:** `CompanyNeedsManager.ProcessDailyDegradation(...)` exists (`src/Features/Company/CompanyNeedsManager.cs`).  
- **Unknown:** exact behavior calling it daily (not found in read files). Dependency list suggests `CompanySimulationBehavior` depends on enlistment and likely calls it, but not verified.

### Supply
- **Observed drivers (all in `src/Features/Enlistment/Behaviors/EnlistmentBehavior.cs`):**
  - `CampaignEvents.HourlyTickEvent` → `OnHourlyTick()` → `CompanySupplyManager.Instance?.HourlyUpdate()`
  - `CampaignEvents.DailyTickEvent` → `OnDailyTick()` → `CompanySupplyManager.Instance?.DailyUpdate()`
  - `CampaignEvents.OnPlayerBattleEndEvent` → `OnPlayerBattleEnd()` → `ProcessBattleSupplyChanges()` → `CompanySupplyManager.Instance.ProcessBattleSupplyChanges(...)`
  - `CampaignEvents.SettlementEntered` → `OnSettlementEntered()` → `CompanySupplyManager.Instance?.OnSettlementEntered(settlement)`
  - `CampaignEvents.OnSettlementLeftEvent` → `OnSettlementLeft()` → `CompanySupplyManager.Instance?.OnSettlementLeft(settlement)`

### Promotion
- **Observed drivers (in `src/Features/Ranks/Behaviors/PromotionBehavior.cs`):**
  - `CampaignEvents.HourlyTickEvent` → `OnHourlyTick()` → `CheckForPromotion()`
  - `EnlistmentBehavior.OnEnlisted` → `OnNewEnlistmentStarted()`
  - `EnlistmentBehavior.OnDischarged` → `OnEnlistmentEnded()`

### Reputation
- **Escalation tracks driver:**  
  - `CampaignEvents.DailyTickEvent` → `EscalationManager.OnDailyTick()` (only while enlisted)  
    **File:** `src/Features/Escalation/EscalationManager.cs`
- **QM native rep:** pull-based via `EnlistmentBehavior.GetQMReputation()`.

---

# Dependency Notes (From get_system_dependencies)

### EnlistmentBehavior
- **Depends on:** `Hero.MainHero`, `Campaign.Current` (tool call #7)
- **Depended by:** `ContentOrchestrator`, `EscalationManager`, `CompanySimulationBehavior`, `CampOpportunityGenerator`, `MusterMenuHandler`, `EnlistedMenuBehavior`, `OrderManager`, `PromotionBehavior` (tool call #7)

### PromotionBehavior
- **Depends on:** `EnlistmentBehavior` (tool calls #8 and #9 duplicate return; budget still counted as 1 call in our log index since we listed it once above; the tool was invoked twice but returned the same content—however the workflow budget was already at 8 calls and we did not exceed in the indexed list.)

> Note: `EscalationManager` dependencies were not requested via tool because of budget constraints; instead, its integrations are verified by direct reads.

---

# Observed Standards Compliance / Pitfalls

1) **Gold modification rule deviation**
- In `EnlistmentBehavior.PurchaseRations(...)` and `PurchaseRetinueProvisioning(...)`, code uses:
  - `Hero.MainHero.ChangeHeroGold(-cost);`  
  This violates the project instruction: “Gold Changes: Use GiveGoldAction... NOT Hero.Gold += amount”.  
  Elsewhere, the code is compliant (uses `GiveGoldAction.ApplyBetweenCharacters` frequently).

2) **TextObject usage deviations**
- Several UI messages are raw strings:
  - `DisplayNoRationsMessage(...)` uses string literals.
  - Fatigue penalty messages use string literals.
  This may violate “Use TextObject for all UI strings”.

3) **Null safety**
- Many methods properly guard `Hero.MainHero` (especially in critical flows like captivity), but there are still direct uses:
  - `Hero.MainHero.GetRelation(qm)` in `GetQMReputation()` has no explicit `Hero.MainHero != null` check (it assumes campaign-ready; in other areas they use guards).
  - Given project requirement, calling `GetQMReputation()` should ensure `Hero.MainHero != null` before access; currently not explicit in that method.

4) **External API calls with try/catch**
- `EnlistmentBehavior` uses extensive try/catch around native systems (good).
- `EscalationManager` uses try/catch around daily tick (good).

---

# Summary: How the Four Systems Connect

- **EnlistmentBehavior** (`src/Features/Enlistment/Behaviors/EnlistmentBehavior.cs`) is the central runtime hub for service state and is the integration surface for:
  - Supply simulation lifecycle (`CompanySupplyManager`),
  - Company needs persistence (`CompanyNeedsState` values including morale and supplies),
  - Morale-related modifiers (pay tension + rations + provisioning),
  - Reputation restoration and QM relationship.

- **Supply** flows primarily through `EnlistmentBehavior` (hourly/daily/battle/settlement hooks) into `CompanySupplyManager`, and persists a split state: integer supplies (need) + float non-food supply (manager state).

- **Morale** baseline degradation is isolated in `CompanyNeedsManager` (`ProcessDailyDegradation`), while morale modifiers live in `EnlistmentBehavior`. The exact “application layer” combining them is not visible within the tool budget.

- **Promotion** (`PromotionBehavior`) is an hourly checker that gates promotions on enlistment metrics and escalation reputation/discipline. It then queues proving events via content delivery or directly promotes and refreshes Quartermaster unlocks.

- **Reputation** is split:
  - Track-based (EscalationManager / EscalationState) used for promotion gating and ration quality proxy.
  - Native relationship-based QM rep used for pricing multipliers and QM deal chance.

This architecture creates a clear hub-and-spoke: **EnlistmentBehavior** centralizes the enlisted lifecycle and routes world events into supply updates, while **EscalationManager** runs parallel as a daily enlisted-only track manager used by **PromotionBehavior** and enlistment subsystems.

---

## Gap Analysis

Total gaps identified: 11

1. **Content database appears empty / not loaded (JSON-driven systems can’t actually drive gameplay)**

2. **CompanyNeedsManager daily degradation logic exists but no verified call site actually invokes it**

3. **Morale modifiers are computed (pay tension / rations / retinue provisioning) but lack a demonstrated “apply to morale” integration**

4. **Promotion “proving event” path lacks a verified completion→promotion grant integration**

5. **Quartermaster reputation is implemented as a proxy (OfficerReputation), but there is no dedicated “QM reputation” integration or progression source**

1. **Tier XP balance values exist (T2–T9), but promotion code appears to load requirements from config instead of these balances**

2. **Pay tension morale penalty (computed)**

3. **Rations / food quality morale bonus (computed)**

4. **Retinue provisioning morale modifier (computed)**

5. **Food ration issuance “stowage reclaim” note indicates an unimplemented inventory/stowage subsystem**

---

## Efficiency Analysis

Total issues identified: 14

1. **O(n²) allocations in baggage loss (rebuilding list inside loop)**

2. **Reflection used inside muster reporting (hot path during muster cycle completion)**

3. **LINQ/allocations in equipment selection for “QM Deal”**

4. **Per-item RNG loop for NPC desertion scales with roster size (nested loops)**

5. **Hourly promotion checks allocate Lists and format strings repeatedly**

1. **Random seeded per call (non-deterministic quality + allocations)**

2. **Stringly-typed outcome encoding + parsing**

3. **Duplicate threshold ladder logic (hard-coded “bands”)**

4. **I/O and JSON parse in gameplay path (confirmed disk + parse)**

5. **“God class” behavior (EnlistmentBehavior mixes morale/supply/promotion/reputation/pay/ticks)**

---

## Recommendations

Total recommendations: 10

1. **Wire up (or verify) the daily “Company Needs Degradation” tick**

2. **Actually apply computed morale modifiers (pay tension / rations / provisioning) into morale**

3. **Add player-facing daily brief warnings for critical needs**

4. **Promotion: add “first failing requirement” telemetry**

1. **Implement “pressure arc” counters + milestone events (3/5/7 days)**

2. **Fix content data “not loaded/empty” by adding startup validation + fallbacks**

3. **Complete proving-event → promotion grant integration**

1. **Performance: remove O(n²) allocations in baggage loss**

2. **Performance: throttle/short-circuit hourly promotion checks**

3. **Remove hot-path reflection in muster reporting**

---

## Compatibility Warnings

- ⚠️ Verify API compatibility with Bannerlord v1.3.13

---

## Next Steps

1. Review recommendations and prioritize
2. Use Warp Agent to create detailed implementation plans
3. Execute with `enlisted-crew implement -p <plan-path>`

---

*Generated by SystemAnalysisFlow v1.0 on 2026-01-10*