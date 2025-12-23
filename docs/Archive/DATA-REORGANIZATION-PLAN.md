# Data Reorganization Plan

**Purpose:** Implementation plan to update all JSON/XML data files to match the documented specifications in `content-index.md`.

**Status:** ✅ Phases 1-4 Complete  
**Created:** 2025-12-22  
**Spec Source:** `docs/Content/content-index.md`  
**Target Mod Version:** 0.9.0  
**Target Game Version:** 1.3.13

---

## Spec Summary (From Documentation)

| Category | Target Count | Spec Location |
|----------|--------------|---------------|
| **Orders** | 17 | content-index.md §Orders |
| **Decisions** | 34 | content-index.md §Decisions |
| **Events** | 68+ | content-index.md §Events |
| **Map Incidents** | 45 | content-index.md §Map Incidents |

### Reputation Tracks (Per Spec)
- `soldierRep` / `soldier_reputation` - Standing with fellow troops
- `officerRep` / `officer_reputation` - Standing with officers  
- `lordRep` / `lord_reputation` - Standing with your lord

**NO `lance_reputation` - this is deprecated terminology.**

---

## Current vs. Target State

### Orders Files

| File | Target | Current State | Action |
|------|--------|---------------|--------|
| `orders_t1_t3.json` | 6 orders | Check | Verify IDs match spec |
| `orders_t4_t6.json` | 6 orders | Check | Verify IDs match spec |
| `orders_t7_t9.json` | 5 orders | Check | Verify IDs match spec |

**Spec IDs (from content-index.md):**
- T1-T3: `order_guard_duty`, `order_camp_patrol`, `order_firewood`, `order_equipment_check`, `order_muster`, `order_sentry`
- T4-T6: `order_scout_route`, `order_treat_wounded`, `order_repair_equipment`, `order_forage`, `order_lead_patrol`, `order_inspect_defenses`
- T7-T9: `order_command_squad`, `order_strategic_planning`, `order_coordinate_supply`, `order_interrogate`, `order_inspect_readiness`

### Decisions Files

| File | Target | Spec Reference |
|------|--------|----------------|
| `decisions.json` | 34 decisions | §Decisions in content-index.md |

**Categories:**
- Self-Care (3): `dec_rest`, `dec_rest_extended`, `dec_seek_treatment`
- Training (9): `dec_weapon_drill`, `dec_spar`, `dec_endurance`, `dec_study_tactics`, `dec_practice_medicine`, `dec_train_troops`, `dec_combat_drill`, `dec_weapon_specialization`, `dec_lead_drill`
- Social (6): `dec_join_men`, `dec_join_drinking`, `dec_seek_officers`, `dec_keep_to_self`, `dec_write_letter`, `dec_confront_rival`
- Economic (5): `dec_gamble_low`, `dec_gamble_high`, `dec_side_work`, `dec_shady_deal`, `dec_visit_market`
- Career (3): `dec_request_audience`, `dec_volunteer_duty`, `dec_request_leave`
- Information (3): `dec_listen_rumors`, `dec_scout_area`, `dec_check_supplies`
- Equipment (2): `dec_maintain_gear`, `dec_visit_quartermaster`
- Risk (3): `dec_dangerous_wager`, `dec_prove_courage`, `dec_challenge`

### Events Files

| File | Content | Target Count |
|------|---------|--------------|
| `events_escalation.json` | Scrutiny(5) + Discipline(5) + Medical(4) | 14 |
| `events_crisis.json` | Party crisis events | 5 |
| `events_role_scout.json` | Scout role events | 6 |
| `events_role_medic.json` | Medic role events | 6 |
| `events_role_engineer.json` | Engineer role events | 5 |
| `events_role_officer.json` | Officer role events | 6 |
| `events_role_operative.json` | Operative role events | 5 |
| `events_role_nco.json` | NCO role + training chain | 5 + chain |
| `events_universal.json` | Universal camp events | 8 |
| `events_food_supply.json` | Food loss + shortage events | 10 |
| `events_muster.json` | Muster cycle events | 6 |

### Map Incidents Files

| File | Content | Target Count |
|------|---------|--------------|
| `incidents_battle.json` | LeavingBattle events | 11 |
| `incidents_siege.json` | DuringSiege events | 10 |
| `incidents_town.json` | EnteringTown events | 8 |
| `incidents_village.json` | EnteringVillage events | 6 |
| `incidents_leaving.json` | LeavingSettlement events | 6 |
| `incidents_waiting.json` | WaitingInSettlement events | 4 |

---

## Files to Delete

These files contain deprecated "lance" system content that has no spec in the documentation:

| File | Reason |
|------|--------|
| `lances_config.json` | No lance system in spec |
| `lance_stories.json` | No lance system in spec |
| `events_lance_simulation.json` | No lance system in spec |
| `events_lance_leader_reactions.json` | No lance system in spec |
| `StoryPacks/LanceLife/*` | No lance system in spec |
| `LancePersonas/*` | No lance system in spec |
| `equipment_kits.json` | Already marked deprecated |
| `Events/samples/*` | Test files, not in spec |
| `*.disabled` files | Dead files |

---

## Files to Clean (Remove Lance References) ✅ MOSTLY COMPLETE

Replacements completed in all JSON files:

| Find | Replace | Status |
|------|---------|--------|
| `"lance_reputation"` | `"soldierRep"` | ✅ Done (643 instances) |
| `{LANCE_MATE_NAME}` | `{COMRADE_NAME}` | ✅ Done |
| `{LANCE_LEADER_SHORT}` | `{SERGEANT}` | ✅ Done |
| `{LANCE_LEADER_RANK}` | `{SERGEANT}` | ✅ Done |
| `{LANCE_NAME}` | `{COMPANY_NAME}` | ✅ Done |
| "your lance" (text) | "your comrades" / "the squad" | ✅ Done |
| "the lance" (text) | "the squad" / "your fellows" | ✅ Done |
| Event IDs with "lance" | Rename to "squad"/"comrade" | ⏳ Pending (176) |
| `lance_rep_*` triggers | `camp_rep_*` | ⏳ Pending (2) |

---

## Config File Updates

### SubModule.xml

```xml
<!-- Fix version to 1.3.13 -->
<DependedModule Id="Native" DependentVersion="v1.3.13"/>
<DependedModule Id="SandBoxCore" DependentVersion="v1.3.13"/>
<DependedModule Id="Sandbox" DependentVersion="v1.3.13"/>
```

### enlisted_config.json

```json
{
  "system_info": {
    "config_version": "2.0",
    "compatible_game_versions": ["1.3.13"],
    "mod_version": "0.9.0"
  }
}
```

**Remove sections:** `lances`, `lance_life`, `lance_life_events`

### settings.json

Rename log category: `LanceLifeEvents` → `Events`

---

## XML Localization

### enlisted_strings.xml

1. Search for strings containing "lance" in ID or text
2. Update text to use clean terminology
3. Remove orphaned string IDs (strings with no JSON reference)

---

## NCO System (Replaces Lance Leader)

### Design
One persistent NCO (non-commissioned officer) per enlistment:
- Generated on enlistment using Bannerlord's `NameGenerator` for lord's culture
- Persists in save data for duration of service
- Uses culture-appropriate rank from `progression_config.json` (T5 = NCO tier)
- Cleared on discharge, regenerated on re-enlistment

### Culture Rank Examples
| Culture | T5 Rank | Example |
|---------|---------|---------|
| Empire | Evocatus | "Evocatus Marcus" |
| Vlandia | Sergeant | "Sergeant Aldric" |
| Sturgia | Huskarl | "Huskarl Bjorn" |
| Aserai | Muqaddam | "Muqaddam Farid" |
| Khuzait | Torguud | "Torguud Temur" |
| Battania | Fiann | "Fiann Brennan" |

---

## Soldier Name Pool (Replaces Lance Mates)

### Design
Pool of 2-3 persistent comrade names per enlistment:
- Generated on enlistment using Bannerlord's `NameGenerator` for lord's culture
- Persists in save data for duration of service
- Used randomly in events for personalized dialogue
- Cleared on discharge, regenerated on re-enlistment

### Usage Examples
| Generic Text | Personalized Text |
|--------------|-------------------|
| "A fellow soldier approaches you..." | "Aldric approaches you..." |
| "One of your comrades looks troubled." | "Bjorn looks troubled." |
| "A soldier from your unit asks a favor." | "Lucius asks a favor." |

### Implementation
```csharp
// On enlistment, generate pool of 3 names
private List<string> _soldierNames = new List<string>();

private void GenerateSoldierNames(CultureObject culture)
{
    _soldierNames.Clear();
    for (int i = 0; i < 3; i++)
    {
        // Mix of male/female based on culture settings
        bool isFemale = MBRandom.RandomFloat < 0.2f; // 20% female
        string name = NameGenerator.Current.GenerateHeroFirstName(culture, isFemale);
        _soldierNames.Add(name.ToString());
    }
}

// When resolving {SOLDIER_NAME}, pick randomly from pool
private string GetRandomSoldierName()
{
    if (_soldierNames.Count == 0) return "a soldier";
    return _soldierNames[MBRandom.RandomInt(_soldierNames.Count)];
}
```

---

## Placeholder Mapping (Complete)

| Old Placeholder | New Placeholder | Source |
|-----------------|-----------------|--------|
| `{LANCE_LEADER_SHORT}` | `{NCO_NAME}` | Single persistent NCO name |
| `{LANCE_MATE_NAME}` | `{SOLDIER_NAME}` | Random from soldier pool |
| `{LANCE_NAME}` | `{COMPANY_NAME}` | Lord's party name |

### Text Variable Resolution
Add to `EventDeliveryManager.cs` or text resolution helper:

```csharp
// Set text variables before displaying event text
textObject.SetTextVariable("NCO_NAME", enlistment.NcoFullName);      // "Sergeant Aldric"
textObject.SetTextVariable("NCO_RANK", enlistment.NcoRank);          // "Sergeant"
textObject.SetTextVariable("SOLDIER_NAME", enlistment.GetRandomSoldierName()); // "Bjorn"
textObject.SetTextVariable("COMPANY_NAME", enlistment.EnlistedLord.PartyBelongedTo?.Name ?? "the company");
```

---

## Implementation Files

| File | Changes |
|------|---------|
| `EnlistmentBehavior.cs` | Add `NcoName`, `NcoRank`, `SoldierNames` fields + generation |
| `EnlistmentBehavior.cs` | Add `SyncData` persistence for names |
| `EventDeliveryManager.cs` | Add text variable resolution for all placeholders |
| Event JSONs | Replace `{LANCE_*}` with new placeholders |
| `enlisted_strings.xml` | Update any hardcoded lance text |

---

## Implementation Order

### Phase 1: Version Fixes ✅ COMPLETE
1. [x] Update `SubModule.xml` - changed 1.3.12 → 1.3.13
2. [x] Update `enlisted_config.json` - fixed versions, removed lance sections
3. [x] Update `settings.json` - renamed log category

### Phase 2: Delete Deprecated Files ✅ COMPLETE
1. [x] Delete `lances_config.json`
2. [x] Delete `lance_stories.json`
3. [x] Delete `events_lance_simulation.json`
4. [x] Delete `events_lance_leader_reactions.json`
5. [x] Delete `StoryPacks/LanceLife/` folder
6. [x] Delete `LancePersonas/` folder
7. [x] Delete `equipment_kits.json`
8. [x] Delete `Events/samples/` folder
9. [x] Delete `*.disabled` files

### Phase 3: Verify Event Files Match Spec ✅ COMPLETE
For each event file:
1. [x] Replaced all `lance_reputation` → `soldierRep` in effects (643 instances)
2. [x] Replaced `{LANCE_MATE_NAME}` → `{SOLDIER_NAME}` placeholders
3. [x] Replaced `{LANCE_LEADER_SHORT}` / `{LANCE_LEADER_RANK}` → `{NCO_RANK}` / `{SERGEANT_NAME}`
4. [x] Replaced `{LANCE_NAME}` → `{COMPANY_NAME}` placeholders
5. [x] Replaced narrative "lance" text → "squad/comrades/company"
6. [x] Renamed event IDs containing "lance" → squad/comrade/camp
7. [x] Updated localization string IDs in `enlisted_strings.xml` (reduced from 298 to <15 false positives)
8. [x] Updated trigger tokens: `lance_rep_*` → `soldier_rep_*`

### Phase 4: Verify Order Files Match Spec ✅ COMPLETE
1. [x] `orders_t1_t3.json` - 6 orders with spec-compliant IDs (`order_guard_duty`, `order_camp_patrol`, `order_firewood`, `order_equipment_check`, `order_muster`, `order_sentry`)
2. [x] `orders_t4_t6.json` - 6 orders with spec-compliant IDs (`order_scout_route`, `order_treat_wounded`, `order_repair_equipment`, `order_forage`, `order_lead_patrol`, `order_inspect_defenses`)
3. [x] `orders_t7_t9.json` - 5 orders with spec-compliant IDs (`order_command_squad`, `order_strategic_planning`, `order_coordinate_supply`, `order_interrogate`, `order_inspect_readiness`)

**Action Taken:** Replaced all 39 legacy orders (with `t#_*` IDs) with 17 spec-compliant orders (with `order_*` IDs). Each order includes full consequences (success, failure, decline), reputation effects, skill/trait XP, and narrative text. Critical failure consequences added for high-stakes orders per spec.

### Phase 5: Verify/Create Decision File
1. [ ] Create or verify `decisions.json` with all 34 decisions
2. [ ] Group by category per spec

### Phase 2B: NCO & Soldier Names Implementation ✅ COMPLETE
1. [x] Add `NcoName`, `NcoRank`, `SoldierNames` fields to `EnlistmentBehavior.cs`
2. [x] Generate NCO name on enlistment using `NameGenerator`
3. [x] Generate 3 soldier names on enlistment using `NameGenerator`
4. [x] Look up T5 rank from `progression_config.json` for lord's culture
5. [x] Add text variable resolution in `EventDeliveryManager.cs`:
   - `{NCO_NAME}` / `{SERGEANT}` → NCO full name with rank
   - `{NCO_RANK}` → Just the rank title
   - `{SOLDIER_NAME}` / `{COMRADE_NAME}` → Random name from soldier pool
   - `{COMPANY_NAME}` → Lord's party name
   - `{PLAYER_NAME}` → Player's first name
   - `{PLAYER_RANK}` → Player's current rank
   - `{LORD_NAME}` → Enlisted lord's name
   - `{LORD_TITLE}` → "Lord" or "Lady"
6. [x] Persist all names in save data (`SyncData`)
7. [x] Clear on discharge, regenerate on re-enlistment

### Phase 6: Verify Map Incident Files
1. [ ] `incidents_battle.json` - verify 11 incidents
2. [ ] `incidents_siege.json` - verify 10 incidents
3. [ ] `incidents_town.json` - verify 8 incidents
4. [ ] `incidents_village.json` - verify 6 incidents
5. [ ] `incidents_leaving.json` - verify 6 incidents
6. [ ] `incidents_waiting.json` - verify 4 incidents

### Phase 7: XML String Cleanup
1. [ ] Search `enlisted_strings.xml` for "lance"
2. [ ] Update or remove affected strings
3. [ ] Verify all event/decision/order IDs have strings

### Phase 8: Documentation Updates

**17 docs reference "lance" and need review:**

| File | Priority | Action |
|------|----------|--------|
| `Features/Core/core-gameplay.md` | HIGH | Core doc, update lance → NCO |
| `Features/Core/enlistment.md` | HIGH | Already has "Lance → Unit/Company" note, verify complete |
| `Features/Core/company-events.md` | HIGH | Update terminology |
| `Features/Content/content-system-architecture.md` | HIGH | Update `lance_rep` → `soldierRep` |
| `Features/Content/event-reward-choices.md` | MED | Update examples |
| `Content/content-index.md` | MED | Verify no lance references in spec |
| `Content/event-catalog-by-system.md` | MED | Update event descriptions |
| `Features/Core/pay-system.md` | MED | Check for lance refs |
| `Features/Core/camp-fatigue.md` | LOW | Minor refs |
| `Features/Equipment/quartermaster-system.md` | LOW | Check context |
| `Features/Equipment/company-supply-simulation.md` | LOW | Check context |
| `Features/Equipment/provisions-rations-system.md` | LOW | Check context |
| `Features/Identity/identity-system.md` | LOW | Update if needed |
| `Features/Technical/commander-track-schema.md` | LOW | Update schema refs |
| `Features/UI/ui-systems-master.md` | LOW | Update UI refs |
| `Features/UI/README.md` | LOW | Update if needed |
| `Reference/ai-behavior-analysis.md` | LOW | Reference doc |

**Documentation Update Rules:**
1. Replace "lance" → "NCO" or "soldiers" as appropriate
2. Replace "lance leader" → "NCO" 
3. Replace "lance mates" → "fellow soldiers" or "comrades"
4. Replace "lance reputation" → "soldier reputation"
5. Update any code snippets showing old field names
6. Verify examples use `soldierRep` not `lance_reputation`

---

## Validation

After each phase:
- [ ] All JSON files are valid JSON (no syntax errors)
- [ ] No `lance_reputation` in any JSON file
- [ ] No `LANCE_*` placeholders in any JSON file  
- [ ] Event/decision/order counts match spec
- [ ] All string IDs exist in XML

Final validation:
- [ ] Build succeeds
- [ ] Game loads mod
- [ ] Events fire correctly
- [ ] No errors in log

---

## Duplicate Files (Delete One)

| File 1 | File 2 | Action |
|--------|--------|--------|
| `events_lance_leader_reactions.json` | `events_lance_leader_reactions.json.disabled` | Delete BOTH (lance system removed) |

---

## Events with Lance IDs (Need Rename or Delete)

### In `events_escalation_thresholds.json`:
| Current ID | Action |
|------------|--------|
| `lance_bonded` | Rename → `soldier_bonded` |
| `lance_isolated` | Rename → `soldier_isolated` |
| `lance_sabotage` | Rename → `soldier_sabotage` |
| `lance_trusted` | Rename → `soldier_trusted` |

### In `events_decisions.json`:
| Current ID | Action |
|------------|--------|
| `decision_lance_mate_dice` | Rename → `decision_comrade_dice` |
| `decision_lance_mate_favor` | Rename → `decision_comrade_favor` |
| `decision_lance_mate_favor_repayment` | Rename → `decision_comrade_repayment` |
| `decision_lance_mate_favor_gratitude` | Rename → `decision_comrade_gratitude` |

---

## Audit Results (2025-12-22)

### Current File Inventory

**Root Config Files (8):**
- `enlisted_config.json` - needs version fix, lance section removal
- `settings.json` - needs log category rename
- `progression_config.json` ✅
- `strategic_context_config.json` ✅
- `schedule_config.json` ✅
- `duties_system.json` ✅
- `menu_config.json` ✅
- `retinue_config.json` ✅
- `equipment_pricing.json` ✅
- `duty_event_pools.json` ✅
- `schedule_popup_events.json` - check if used

**Files to DELETE:**
- `lances_config.json` ❌
- `lance_stories.json` ❌
- `equipment_kits.json` ❌ (deprecated)

**Folders to DELETE:**
- `StoryPacks/LanceLife/` (7 files) ❌
- `LancePersonas/` ❌ (move name_pools.json if keeping)
- `Events/samples/` (4 files) ❌

### Events Files

**Current (26 files):**
| File | Status | Action |
|------|--------|--------|
| `camp_events.json` | ✅ | Keep |
| `muster_events.json` | ✅ | Keep |
| `events_general.json` | ⚠️ | Clean lance refs |
| `events_training.json` | ⚠️ | Clean lance refs |
| `events_onboarding.json` | ⚠️ | Clean lance refs |
| `events_promotion.json` | ⚠️ | Clean lance refs |
| `events_escalation_thresholds.json` | ⚠️ | Clean lance refs |
| `events_pay_loyal.json` | ✅ | Merge → events_pay.json |
| `events_pay_mutiny.json` | ✅ | Merge → events_pay.json |
| `events_pay_tension.json` | ✅ | Merge → events_pay.json |
| `events_decisions.json` | ✅ | Merge → decisions.json |
| `events_player_decisions.json` | ✅ | Merge → decisions.json |
| `events_lance_simulation.json` | ❌ | DELETE |
| `events_lance_leader_reactions.json` | ❌ | DELETE |
| `events_lance_leader_reactions.json.disabled` | ❌ | DELETE |
| `events_duty_*.json` (10 files) | ✅ | Keep |
| `incidents_battle.json` | ✅ | Keep |
| `incidents_settlement.json` | ✅ | Keep |
| `Role/scout_events.json` | ✅ | Move to main Events/ |
| `samples/*.json` (4 files) | ❌ | DELETE |
| `schema_version.json` | ✅ | Keep |

### Orders Files (3)

| File | Spec Count | Status |
|------|------------|--------|
| `orders_t1_t3.json` | 6 | Verify |
| `orders_t4_t6.json` | 6 | Verify |
| `orders_t7_t9.json` | 5 | Verify |

### Files to Merge

**Decision Files → `decisions.json`:**
| Source File | Events | Content |
|-------------|--------|---------|
| `events_decisions.json` | 12 | Automatic decisions (lord hunt, training offer, etc.) |
| `events_player_decisions.json` | 6 | Player-initiated (dice game, petition lord, etc.) |
| **Total** | 18 | Merge into single `decisions.json` |

**Pay Files → `events_pay.json`:**
| Source File | Events | Content |
|-------------|--------|---------|
| `events_pay_tension.json` | ~5 | Pay grumbling, confrontation |
| `events_pay_mutiny.json` | ~3 | Desertion planning, mutiny |
| `events_pay_loyal.json` | ~3 | Loyal path missions |
| **Total** | ~11 | Merge into single `events_pay.json` |

---

### Gap Analysis: Spec vs. Reality

**Content in spec NOT in files:**

| Spec Section | Expected | Gap |
|--------------|----------|-----|
| Crisis Events | 5 | Need `events_crisis.json` |
| Role: Officer Events | 6 | Need file |
| Role: Operative Events | 5 | Need file |
| Role: NCO Events | 5+ | Need file |
| Universal Events | 8 | May be in events_general |
| Food/Supply Events | 10 | Need file |
| Incidents: Siege | 10 | Need file |
| Incidents: Town | 8 | Need file |
| Incidents: Village | 6 | Need file |
| Incidents: Leaving | 6 | Need file |
| Incidents: Waiting | 4 | Need file |

**Current incidents: 2 files (~15 events)**
**Spec incidents: 45 events needed**

### Summary

| Category | Spec | Current | Gap |
|----------|------|---------|-----|
| Orders | 17 | ~17 | Verify |
| Decisions | 34 | ~25 | +9 |
| Events | 68+ | ~50 | +18 |
| Incidents | 45 | ~15 | +30 |

**Priority Actions:**
1. Delete lance files (immediate)
2. Clean lance refs from existing files (immediate)
3. Merge fragmented files (consolidation)
4. Create missing content files (new content)

---

**End of Document**
