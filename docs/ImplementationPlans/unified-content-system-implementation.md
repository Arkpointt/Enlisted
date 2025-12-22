# Unified Content & Quartermaster System Implementation

> **Purpose**: Single implementation plan for the narrative content system (events, orders, decisions, map incidents) AND the quartermaster system (equipment, food, supply, baggage checks). These systems share dependencies and must be built together.

**Last Updated**: December 21, 2025  
**Status**: Master Implementation Plan  
**Target Game Version**: Bannerlord v1.3.11

---

## Engineering Standards

**Follow these while implementing all phases:**

### Code Quality
- **Follow ReSharper linter/recommendations.** Fix warnings; don't suppress them with pragmas.
- Comments should be professional, factual, human-sounding (not robotic).
- Reuse existing patterns (copy OrderCatalog → EventCatalog, etc.)

### API Verification
- **Use the local native decompile** to verify Bannerlord APIs before using them.
- Decompile location: `C:\Dev\Enlisted\Decompile\`
- Key namespaces: `TaleWorlds.CampaignSystem`, `TaleWorlds.Core`, `TaleWorlds.Library`
- Don't rely on external docs or AI assumptions - verify in decompile first.

### Data Files
- **XML** for player-facing text (localization via `ModuleData/Languages/enlisted_strings.xml`)
- **JSON** for content data (events, decisions, orders in `ModuleData/Enlisted/`)
- In code, use `TextObject("{=stringId}Fallback")` for localized strings.

### Logging
- All logs go to: `<BannerlordInstall>/Modules/Enlisted/Debugging/`
- Use: `ModLogger.Info("Category", "message")`
- Log content loading counts, migration warnings, errors.

### Build
```bash
dotnet build -c "Enlisted RETAIL" /p:Platform=x64
```
Output: `Modules/Enlisted/bin/Win64_Shipping_Client/`

---

## Index

1. [Engineering Standards](#engineering-standards) ← **Read first**
2. [Overview](#overview)
3. [Shared Dependencies](#shared-dependencies)
4. [Current State](#current-state) ← **Check what's already done**
5. [Phase 1: Core Infrastructure](#phase-1-core-infrastructure)
6. [Phase 2: Escalation & Reputation](#phase-2-escalation--reputation)
7. [Phase 3: Event Delivery Pipeline](#phase-3-event-delivery-pipeline)
8. [Phase 4: Quartermaster Integration](#phase-4-quartermaster-integration)
9. [Phase 5: Content Loading](#phase-5-content-loading)
10. [Phase 6: Intelligent Selection](#phase-6-intelligent-selection)
11. [Phase 7: Polish & Testing](#phase-7-polish--testing)
12. [Acceptance Criteria](#acceptance-criteria)

---

## Overview

This plan unifies two interrelated systems:

**Content System** (StoryBlocks)
- Orders: Military directives from chain of command
- Decisions: Player-initiated actions from camp menu
- Events: Context-triggered narrative moments
- Map Incidents: Native Bannerlord incident integration

**Quartermaster System**
- Equipment: Master-at-Arms loadout selection with pricing
- Food: T1-T4 issued rations, T5+ officer provisions shop
- Supply: Hybrid simulation (40% food observed, 60% non-food simulated)
- Baggage Checks: Muster inspections with contraband detection

**Why Unified?**
These systems share:
- Muster cycle (12-day tick triggers pay, rations, baggage checks, some events)
- QM Reputation (affects equipment pricing, food quality, baggage outcomes)
- Scrutiny escalation (affects baggage check severity)
- Supply level (gates equipment access, triggers crisis events)
- Skill checks (apply to both event options and QM interactions)

---

## Shared Dependencies

### Muster Cycle (12 Days)

The muster is a central event that triggers multiple systems:

```
OnMusterDay():
  1. Pay wages (existing)
  2. Ration exchange (T1-T4) OR refresh provisions shop (T5+)
  3. Baggage check (30% chance)
  4. Reset schedule cycle
  5. Fire muster-triggered events
```

### Reputation Tracks

| Track | Range | Used By |
|-------|-------|---------|
| Lord Rep | 0-100 | Events, Orders |
| Officer Rep | 0-100 | Events, Orders |
| Soldier Rep | -50 to +50 | Events, Decisions |
| **QM Rep** | -50 to +100 | Equipment, Food, Baggage (separate NPC relationship) |

### Escalation Tracks

| Track | Range | Used By |
|-------|-------|---------|
| Scrutiny | 0-10 | Threshold events, Baggage checks |
| Discipline | 0-10 | Threshold events, Orders |
| Medical Risk | 0-5 | Threshold events, Injury outcomes |

### Company Supply (0-100%)

| Threshold | Effects |
|-----------|---------|
| 70%+ | Normal operations |
| 50-69% | Warnings, ration chance reduced to 80% |
| 30-49% | Equipment blocked, ration chance 50% |
| <30% | Crisis events, no rations, potential desertion |

---

## Current State

> **Cross-Reference**: `enlisted-interface-master-plan.md` Phases 1-6 completed.
> Before implementing anything, verify it doesn't already exist in code.

### Already Implemented ✓ (From Interface Master Plan)

| Component | Location | Status | Verified |
|-----------|----------|--------|----------|
| **Menu System** | `EnlistedMenuBehavior.cs` | ✅ Complete | Has MultiSelectionInquiryData |
| **Order Catalog** | `OrderCatalog.cs` | ✅ Complete | Loads JSON, 39 orders |
| **Order Manager** | `OrderManager.cs` | ✅ Complete | Issues, tracks, expires |
| **Order Models** | `Features/Orders/Models/` | ✅ Complete | Order, Consequence, Outcome |
| **Identity System** | `EnlistedStatusManager.cs` | ✅ Complete | Role detection from traits |
| **Trait Helper** | `TraitHelper.cs` | ✅ Complete | XP awards, level checks |
| **Escalation Tracks** | `EscalationManager.cs` | ✅ Complete | Scrutiny, Discipline, Medical |
| **Reputation State** | `EscalationState.cs` | ✅ Complete | Lord/Officer/Soldier Rep |
| **Company Needs** | `CompanyNeedsManager.cs` | ✅ Complete | Readiness, Morale, etc. |
| **Strategic Context** | `ArmyContextAnalyzer.cs` | ✅ Complete | War/Peace/Siege detection |
| **QM Hero** | `EnlistmentBehavior.cs` | ✅ Complete | GetOrCreateQuartermaster() |
| **QM Dialogue** | `EnlistedDialogManager.cs` | ✅ Complete | Conversation hub |
| **Equipment Store** | `QuartermasterManager.cs` | ✅ Complete | Needs pricing update |
| **Incident Delivery** | `EnlistedIncidentsBehavior.cs` | ✅ Complete | Native MapState.NextIncident |
| **Muster Cycle** | `EnlistmentBehavior.cs` | ✅ Complete | 12-day pay cycle |
| **News/Reports** | `EnlistedNewsBehavior.cs` | ✅ Complete | Daily report generation |

### NOT Yet Implemented ❌ (Verified Missing)

| Component | Why Needed | Check Command |
|-----------|------------|---------------|
| **Event Catalog** | Load events from JSON | `grep "EventCatalog" src/` → No matches |
| **Decision Catalog** | Load decisions from JSON | `grep "DecisionCatalog" src/` → No matches |
| **Event Delivery** | Fire events with MultiSelectionInquiryData | Only orders use inquiry popups |
| **RoleBasedEventTrigger** | Fire events based on role/context | `grep "RoleBasedEventTrigger" src/` → No matches |
| **QM Rep Pricing** | 30% discount at high rep | `grep "GetEquipmentPriceMultiplier" src/` → No matches |
| **Ration Exchange** | T1-T4 food at muster | `grep "RationExchange\|IssuedRation" src/` → No matches |
| **Baggage Check** | Contraband search at muster | `grep "BaggageCheck" src/` → No matches |
| **Supply Simulation** | Hybrid 40/60 model | Currently just degrades, no observation |
| **Pacing System** | 3-5 days between events | No cooldown/spacing logic |
| **Content Migration** | Old schema → new fields | Loader doesn't map old field names |

### Reusable (Extend Existing)

| Existing Code | How to Extend |
|---------------|---------------|
| `OrderCatalog.cs` | Copy pattern for `EventCatalog`, `DecisionCatalog` |
| `OrderManager.cs` | Copy pattern for `EventManager`, `DecisionManager` |
| `EnlistedMenuBehavior.cs` | Add decision options to camp hub |
| `EnlistedIncidentsBehavior.cs` | Add map incident hooks |
| `EscalationManager.cs` | Add threshold event triggering |
| `QuartermasterManager.cs` | Add pricing multiplier methods |
| `EnlistmentBehavior.cs` | Add ration exchange to muster |

---

## Phase 1: Core Infrastructure

**Goal**: Build the foundation for loading and selecting content.

### 1.0 Existing Content Assessment

We have **50 existing JSON files** with usable content. Strategy: **migrate, don't delete**.

**Existing Content (keep and migrate):**

| Folder | Files | Format Status |
|--------|-------|---------------|
| `Orders/` | 3 files (T1-T3, T4-T6, T7-T9) | ✅ Good - minor updates |
| `Events/` | 15+ files (duties, general, training) | ⚠️ Verbose - simplify fields |
| `StoryPacks/LanceLife/` | 7 files (escalation, discipline, etc.) | ⚠️ Uses old "lance" naming |
| `Languages/enlisted_strings.xml` | 1 file | ✅ Keep, add new strings |

**Migration Mapping (old → new):**

| Old Field | New Field | Notes |
|-----------|-----------|-------|
| `lance_reputation` | `soldierRep` | Renamed |
| `formation` | `role` | Concept changed |
| `duty` | — | Remove (duties deprecated) |
| `trait_xp.Leadership` | `traitXp.Commander` | Map to native traits |
| `trait_xp.MartialSkills` | `skillXp.OneHanded` etc. | Split to skills |
| `rewards.relation` | `effects.lordRep` etc. | Flatten structure |

**Migration Approach:**
1. Build loader that handles BOTH old and new schema (fallback parsing)
2. Create 3-5 new sample files in simplified format
3. Test loader with both old and new files
4. Gradually update old files as we touch them
5. Don't break existing content while migrating

### 1.1 Event Schema & Loader

Create a JSON loader that reads event definitions from `ModuleData/Enlisted/Events/`.

```csharp
public class EventDefinition
{
    public string Id { get; set; }
    public string TitleId { get; set; }      // XML string key
    public string SetupId { get; set; }       // XML string key
    public EventRequirements Requirements { get; set; }
    public List<EventOption> Options { get; set; }
}

public class EventRequirements
{
    public int? MinTier { get; set; }
    public int? MaxTier { get; set; }
    public string Context { get; set; }       // "War", "Peace", "Siege", "Any"
    public string Role { get; set; }          // "Scout", "Medic", "Officer", "Any"
    public Dictionary<string, int> MinSkills { get; set; }
    public Dictionary<string, int> MinTraits { get; set; }
}

public class EventOption
{
    public string TextId { get; set; }        // XML string key
    public string ResultTextId { get; set; } // XML string key
    public EventRequirements Requirements { get; set; }  // Skill gates
    public EventEffects Effects { get; set; }
}

public class EventEffects
{
    public Dictionary<string, int> SkillXp { get; set; }
    public Dictionary<string, int> TraitXp { get; set; }
    public int? LordRep { get; set; }
    public int? OfficerRep { get; set; }
    public int? SoldierRep { get; set; }
    public int? Scrutiny { get; set; }
    public int? Discipline { get; set; }
    public int? MedicalRisk { get; set; }
    public int? Gold { get; set; }
    public int? HpChange { get; set; }
    public int? TroopLoss { get; set; }
    public int? TroopWounded { get; set; }
    public int? FoodLoss { get; set; }
    public int? TroopXp { get; set; }
    public string ApplyWound { get; set; }    // "Minor", "Serious", "Permanent"
    public string ChainEventId { get; set; }  // Follow-up event
}
```

**Files to create:**
- `src/Features/Content/EventDefinition.cs` - Data models
- `src/Features/Content/ContentLoader.cs` - JSON loading
- `src/Features/Content/ContentRegistry.cs` - Runtime storage

### 1.2 Localization Integration

Events reference XML string IDs, not hardcoded text.

```xml
<!-- ModuleData/Languages/enlisted_strings.xml -->
<string id="evt_scout_tracks_title" text="Strange Tracks" />
<string id="evt_scout_tracks_setup" text="You find tracks that don't match our patrol routes. Fresh, heading east." />
<string id="evt_scout_tracks_opt1" text="Report to the sergeant immediately." />
<string id="evt_scout_tracks_opt1_result" text="The sergeant nods. 'Good eye. We'll send a patrol.'" />
```

```csharp
public static TextObject GetEventText(string stringId)
{
    return new TextObject($"{{={stringId}}}");
}
```

### 1.3 Skill Check Framework

Centralized skill check logic used by events, orders, decisions.

```csharp
public static class SkillCheckHelper
{
    // Universal formula: Base% + (Skill / 3)
    public static float CalculateSuccessChance(Hero hero, SkillObject skill, float baseChance)
    {
        int skillLevel = hero.GetSkillValue(skill);
        float bonus = skillLevel / 3f / 100f;  // Skill 60 = +20%
        return Math.Min(1f, baseChance + bonus);
    }
    
    public static bool CheckSkill(Hero hero, SkillObject skill, float baseChance)
    {
        float chance = CalculateSuccessChance(hero, skill, baseChance);
        return MBRandom.RandomFloat < chance;
    }
    
    public static bool MeetsSkillThreshold(Hero hero, SkillObject skill, int threshold)
    {
        return hero.GetSkillValue(skill) >= threshold;
    }
}
```

**Checklist:**

*Already Done (verify in code):*
- [x] `OrderCatalog.cs` exists - use as pattern for EventCatalog
- [x] `Order.cs` models exist - use as pattern for EventDefinition
- [x] JSON loading works for orders - extend pattern

*Still Needed:*
- [ ] Audit existing JSON files, document which are usable
- [ ] Create `EventDefinition.cs` with all data models (copy Order pattern)
- [ ] Create `EventCatalog.cs` (copy OrderCatalog pattern)
- [ ] Add fallback parsing for old schema (lance_reputation → soldierRep, etc.)
- [ ] Add `SkillCheckHelper.cs` with universal formulas
- [ ] Create 3-5 new sample events in simplified schema
- [ ] Test loading BOTH old and new format files
- [ ] Log migration warnings ("Old field 'lance_reputation' found, mapping to soldierRep")

---

## Phase 2: Escalation & Reputation

**Goal**: Wire existing escalation system to trigger events and affect outcomes.

### 2.1 Threshold Event Triggering

When escalation crosses a threshold, fire the appropriate event.

```csharp
// In EscalationManager.cs
public void ModifyScrutiny(int amount, string reason)
{
    int oldValue = _state.Scrutiny;
    _state.Scrutiny = Math.Clamp(_state.Scrutiny + amount, 0, 10);
    
    // Check for threshold crossing
    if (oldValue < 2 && _state.Scrutiny >= 2)
        TriggerEvent("evt_scrutiny_2");
    else if (oldValue < 4 && _state.Scrutiny >= 4)
        TriggerEvent("evt_scrutiny_4");
    // ... etc
    
    ModLogger.Info("Escalation", $"Scrutiny {oldValue} → {_state.Scrutiny}: {reason}");
}

private void TriggerEvent(string eventId)
{
    var eventDef = ContentRegistry.GetEvent(eventId);
    if (eventDef != null)
    {
        EventDeliveryManager.Instance.QueueEvent(eventDef);
    }
}
```

### 2.2 QM Reputation Integration

QM Rep uses `Hero.GetRelation(quartermaster)` but needs multiplier methods.

```csharp
// In EnlistmentBehavior.cs or new QuartermasterReputationHelper.cs
public float GetEquipmentPriceMultiplier()
{
    int rep = GetQMReputation();
    if (rep >= 65) return 0.70f;  // 30% discount
    if (rep >= 35) return 0.80f;  // 20% discount
    if (rep >= 10) return 0.90f;  // 10% discount
    if (rep >= -10) return 1.00f; // Standard
    if (rep >= -25) return 1.20f; // 20% markup
    return 1.40f;                  // 40% markup
}

public float GetBuybackMultiplier()
{
    int rep = GetQMReputation();
    if (rep >= 65) return 0.65f;
    if (rep >= 35) return 0.60f;
    if (rep >= 10) return 0.55f;
    if (rep >= -10) return 0.50f;
    if (rep >= -25) return 0.40f;
    return 0.30f;
}
```

**Checklist:**

*Already Done (verify in code):*
- [x] `EscalationManager.cs` exists with ModifyScrutiny/ModifyDiscipline
- [x] `EscalationState.cs` has all rep tracks (Lord, Officer, Soldier)
- [x] QM Hero exists via `GetOrCreateQuartermaster()`

*Still Needed:*
- [ ] Add threshold event triggering to `EscalationManager` (call EventDeliveryManager)
- [ ] Add `GetEquipmentPriceMultiplier()` method to EnlistmentBehavior
- [ ] Add `GetBuybackMultiplier()` method to EnlistmentBehavior
- [ ] Add `GetOfficerFoodPriceMultiplier()` method to EnlistmentBehavior
- [ ] Wire pricing methods to `QuartermasterManager.CalculatePrice()`
- [ ] Test: Scrutiny 4 triggers `evt_scrutiny_4`

---

## Phase 3: Event Delivery Pipeline

**Goal**: Build the UI layer for presenting events to the player.

### 3.1 Event Popup (MultiSelectionInquiryData)

Standard events use `MultiSelectionInquiryData` for multiple choice.

```csharp
public class EventDeliveryManager : CampaignBehaviorBase
{
    private Queue<EventDefinition> _pendingEvents = new();
    
    public void QueueEvent(EventDefinition evt)
    {
        _pendingEvents.Enqueue(evt);
        TryDeliverNextEvent();
    }
    
    private void TryDeliverNextEvent()
    {
        if (_pendingEvents.Count == 0) return;
        
        var evt = _pendingEvents.Dequeue();
        var options = BuildOptions(evt);
        
        var inquiry = new MultiSelectionInquiryData(
            GetEventText(evt.TitleId).ToString(),
            GetEventText(evt.SetupId).ToString(),
            options,
            isExitShown: false,
            maxSelectableOptionCount: 1,
            affirmativeText: "Confirm",
            negativeText: null,
            OnOptionSelected,
            OnEventClosed
        );
        
        InformationManager.ShowMultiSelectionInquiry(inquiry);
    }
    
    private List<InquiryElement> BuildOptions(EventDefinition evt)
    {
        var options = new List<InquiryElement>();
        foreach (var opt in evt.Options)
        {
            bool enabled = MeetsRequirements(opt.Requirements);
            string hint = GetOptionHint(opt);
            
            options.Add(new InquiryElement(
                opt,                                    // Identifier
                GetEventText(opt.TextId).ToString(),   // Text
                null,                                   // Image
                enabled,                                // Enabled
                hint                                    // Hint
            ));
        }
        return options;
    }
}
```

### 3.2 Order Popup (InquiryData)

Orders use simple Accept/Decline `InquiryData`.

```csharp
public void DeliverOrder(Order order)
{
    string issuer = OrderCatalog.DetermineOrderIssuer(GetPlayerTier(), order);
    string title = $"Order from {issuer}";
    
    var inquiry = new InquiryData(
        title,
        GetOrderText(order),
        true,  // Affirmative
        true,  // Negative
        "Accept",
        "Decline",
        () => AcceptOrder(order),
        () => DeclineOrder(order)
    );
    
    InformationManager.ShowInquiry(inquiry);
}
```

### 3.3 Decision Menu (Camp Hub)

Decisions appear as options in the `enlisted_camp_hub` game menu.

```csharp
// In EnlistedMenuBehavior.cs
private void AddDecisionOptions(GameMenu menu)
{
    var availableDecisions = DecisionCatalog.GetAvailable(Hero.MainHero);
    
    foreach (var decision in availableDecisions)
    {
        menu.AddGameMenuOption(
            "enlisted_camp_hub",
            $"decision_{decision.Id}",
            GetDecisionText(decision),
            args => CanSelectDecision(decision, args),
            args => ExecuteDecision(decision),
            false,
            decision.Index
        );
    }
}

private bool CanSelectDecision(Decision decision, MenuCallbackArgs args)
{
    // Check cooldown
    if (IsOnCooldown(decision.Id))
    {
        args.Tooltip = new TextObject("On cooldown ({DAYS} days remaining)");
        args.IsEnabled = false;
        return true;
    }
    
    // Check costs
    if (!CanAffordCosts(decision.Costs))
    {
        args.Tooltip = new TextObject("Cannot afford");
        args.IsEnabled = false;
        return true;
    }
    
    args.IsEnabled = true;
    return true;
}
```

**Checklist:**

*Already Done (verify in code):*
- [x] `EnlistedMenuBehavior.cs` uses `MultiSelectionInquiryData` (for some UI)
- [x] `OrderManager.cs` delivers orders via inquiry popups
- [x] Camp hub menu exists with multiple options
- [x] `EnlistedIncidentsBehavior.cs` delivers native incidents

*Still Needed:*
- [ ] Create `EventDeliveryManager.cs` (copy OrderManager pattern)
- [ ] Wire event delivery to use `MultiSelectionInquiryData`
- [ ] Add decision options to camp hub menu (extend existing)
- [ ] Add skill-gated option hints ("Requires Scouting 40+")
- [ ] Test: Manual event delivery works

---

## Phase 4: Quartermaster Integration

**Goal**: Wire the quartermaster system to use new pricing, food, and baggage check systems.

### 4.1 Equipment Pricing Update

Replace hardcoded pricing with QM Rep multipliers.

```csharp
// In QuartermasterManager.cs
public int CalculateEquipmentPrice(ItemObject item)
{
    float baseValue = item.Value;
    float repMult = _enlistment.GetEquipmentPriceMultiplier();
    float moodMult = _campLife.GetPurchaseMoodMultiplier();  // 0.98-1.15
    float dutyMult = HasProvisionerDuty() ? 0.85f : 1.0f;
    
    return (int)(baseValue * repMult * moodMult * dutyMult);
}

public int CalculateBuybackPrice(ItemObject item)
{
    float baseValue = item.Value;
    float repMult = _enlistment.GetBuybackMultiplier();
    float moodMult = _campLife.GetBuybackMoodMultiplier();
    
    return (int)(baseValue * repMult * moodMult);
}
```

### 4.2 Supply Gate

Block equipment when supply < 30%.

```csharp
public bool CanAccessEquipmentMenu()
{
    int supply = GetCompanySupply();
    if (supply < 30)
    {
        ModLogger.Info("QM", "Equipment blocked: supply critical");
        return false;
    }
    return true;
}
```

### 4.3 Ration Exchange (T1-T4)

Add to muster processing.

```csharp
private void ProcessMusterRation()
{
    if (GetPlayerTier() >= 5)
    {
        RefreshOfficerProvisionsShop();
        return;
    }
    
    // Reclaim old issued ration (only if issuing new one)
    int supply = GetCompanySupply();
    bool rationAvailable = DetermineRationAvailability(supply);
    
    if (rationAvailable)
    {
        ReclaimIssuedRations();
        IssueNewRation();
    }
    else
    {
        ShowNoRationMessage();
    }
}
```

### 4.4 Baggage Check

30% chance at muster, outcomes based on QM Rep.

```csharp
private void TryBaggageCheck()
{
    if (MBRandom.RandomFloat > 0.30f) return;
    
    bool hasContraband = ScanForContraband();
    if (!hasContraband)
    {
        TriggerEvent("evt_baggage_clear");
        return;
    }
    
    int qmRep = GetQMReputation();
    if (qmRep >= 65)
        TriggerEvent("evt_baggage_lookaway");
    else if (qmRep >= 35)
        TriggerEvent("evt_baggage_bribe");
    else
        TriggerEvent("evt_baggage_confiscate");
}
```

**Checklist:**

*Already Done (verify in code):*
- [x] `QuartermasterManager.cs` exists with equipment purchasing
- [x] `EnlistmentBehavior.cs` has muster cycle (12-day pay)
- [x] `CompanyNeedsState.Supplies` exists (0-100)

*Still Needed:*
- [ ] Add pricing multiplier methods (Phase 2)
- [ ] Update `QuartermasterManager.CalculatePrice()` to use multipliers
- [ ] Add supply gate check (`Supplies < 30` blocks menu)
- [ ] Implement ration exchange in muster (T1-T4)
- [ ] Implement ration tracking (`_issuedFoodRations` list)
- [ ] Implement baggage check roll (30% at muster)
- [ ] Add contraband scanning logic
- [ ] Test: High QM Rep gives 30% discount
- [ ] Test: Supply <30% blocks equipment

---

## Phase 5: Content Loading

**Goal**: Create JSON content files and load them at startup.

### 5.1 JSON File Structure

```
ModuleData/Enlisted/
├── Events/
│   ├── escalation_scrutiny.json
│   ├── escalation_discipline.json
│   ├── escalation_medical.json
│   ├── muster.json
│   ├── food_supply.json
│   ├── crisis.json
│   └── Role/
│       ├── scout.json
│       ├── medic.json
│       ├── engineer.json
│       ├── officer.json
│       ├── operative.json
│       ├── nco.json
│       └── universal.json
├── Orders/
│   └── catalog.json
├── Decisions/
│   ├── social.json
│   ├── training.json
│   ├── commerce.json
│   └── risk.json
└── Incidents/
    ├── leaving_battle.json
    ├── during_siege.json
    ├── entering_town.json
    ├── entering_village.json
    ├── leaving_settlement.json
    └── waiting.json
```

### 5.2 Example Event JSON

```json
{
  "events": [
    {
      "id": "evt_scout_tracks",
      "titleId": "evt_scout_tracks_title",
      "setupId": "evt_scout_tracks_setup",
      "requirements": {
        "role": "Scout",
        "context": "Any"
      },
      "options": [
        {
          "textId": "evt_scout_tracks_opt1",
          "resultTextId": "evt_scout_tracks_opt1_result",
          "effects": {
            "skillXp": { "Scouting": 15 },
            "officerRep": 5
          }
        },
        {
          "textId": "evt_scout_tracks_opt2",
          "resultTextId": "evt_scout_tracks_opt2_result",
          "requirements": { "minSkills": { "Scouting": 60 } },
          "effects": {
            "skillXp": { "Scouting": 25, "Tactics": 10 },
            "lordRep": 8
          }
        }
      ]
    }
  ]
}
```

### 5.3 Loading at Startup

```csharp
public class ContentLoader
{
    public static void LoadAllContent()
    {
        string basePath = ModuleHelper.GetModuleFullPath("Enlisted") + "ModuleData/Enlisted/";
        
        LoadEvents(basePath + "Events/");
        LoadOrders(basePath + "Orders/");
        LoadDecisions(basePath + "Decisions/");
        LoadIncidents(basePath + "Incidents/");
        
        ModLogger.Info("Content", $"Loaded {ContentRegistry.EventCount} events, " +
            $"{ContentRegistry.OrderCount} orders, {ContentRegistry.DecisionCount} decisions, " +
            $"{ContentRegistry.IncidentCount} incidents");
    }
}
```

**Checklist:**

*Already Done (verify in code):*
- [x] `ModuleData/Enlisted/Orders/` has 3 JSON files (39 orders)
- [x] `ModuleData/Enlisted/Events/` has 15+ JSON files (old schema)
- [x] `ModuleData/Languages/enlisted_strings.xml` exists

*Still Needed:*
- [ ] Reorganize folder structure per new spec
- [ ] Create 3-5 sample events in NEW schema format
- [ ] Add XML strings for sample events
- [ ] Test EventCatalog loads both old and new format
- [ ] Add content count logging at startup

---

## Phase 6: Intelligent Selection

**Goal**: Select appropriate events based on context, role, skills, cooldowns.

### 6.1 Event Selection Algorithm

```csharp
public EventDefinition SelectEvent()
{
    var candidates = ContentRegistry.GetAllEvents()
        .Where(e => MeetsRequirements(e.Requirements))
        .Where(e => !IsOnCooldown(e.Id))
        .Where(e => !IsRecentlyFired(e.Id))
        .ToList();
    
    if (candidates.Count == 0) return null;
    
    // Weight by relevance
    var weighted = candidates.Select(e => new {
        Event = e,
        Weight = CalculateWeight(e)
    }).ToList();
    
    // Random selection with weighting
    float totalWeight = weighted.Sum(w => w.Weight);
    float roll = MBRandom.RandomFloat * totalWeight;
    
    float cumulative = 0;
    foreach (var w in weighted)
    {
        cumulative += w.Weight;
        if (roll <= cumulative)
            return w.Event;
    }
    
    return candidates.First();
}

private float CalculateWeight(EventDefinition evt)
{
    float weight = 1.0f;
    
    // Role match bonus
    if (evt.Requirements.Role == GetPlayerRole())
        weight *= 2.0f;
    
    // Context match bonus
    if (evt.Requirements.Context == GetCurrentContext())
        weight *= 1.5f;
    
    // Skill opportunity bonus (if player could benefit from XP)
    if (PlayerHasWeakSkillIn(evt))
        weight *= 1.3f;
    
    return weight;
}
```

### 6.2 Pacing System

```csharp
public class EventPacingManager
{
    private const int MIN_DAYS_BETWEEN_EVENTS = 3;
    private const int MAX_DAYS_BETWEEN_EVENTS = 5;
    
    private CampaignTime _lastEventTime;
    private CampaignTime _nextEventWindow;
    
    public void OnDailyTick()
    {
        if (CampaignTime.Now < _nextEventWindow) return;
        
        // Try to fire an event
        var evt = EventSelector.SelectEvent();
        if (evt != null)
        {
            EventDeliveryManager.Instance.QueueEvent(evt);
            _lastEventTime = CampaignTime.Now;
            SetNextEventWindow();
        }
    }
    
    private void SetNextEventWindow()
    {
        int days = MBRandom.RandomInt(MIN_DAYS_BETWEEN_EVENTS, MAX_DAYS_BETWEEN_EVENTS + 1);
        _nextEventWindow = CampaignTime.DaysFromNow(days);
    }
}
```

**Checklist:**

*Already Done (verify in code):*
- [x] `OrderCatalog.cs` has filtering by tier - use as pattern
- [x] `EnlistedStatusManager.GetPrimaryRole()` exists
- [x] `ArmyContextAnalyzer.cs` detects war/peace/siege

*Still Needed:*
- [ ] Implement `EventSelector.SelectEvent()` (copy OrderCatalog pattern)
- [ ] Add requirement checking (tier, role, context, skills)
- [ ] Add cooldown tracking (store in EscalationState or new ContentState)
- [ ] Add weighting system (role match bonus, context bonus)
- [ ] Implement pacing (3-5 days between events)
- [ ] Test: Scout gets scout events more often
- [ ] Test: No event spam

---

## Phase 7: Polish & Testing

### 7.1 Logging & Diagnostics

```csharp
ModLogger.Info("Event", $"Selected: {evt.Id} (weight: {weight}, role: {role}, context: {context})");
ModLogger.Info("Event", $"Player chose option {optionIndex}: {option.TextId}");
ModLogger.Info("Event", $"Applied effects: {EffectsToString(option.Effects)}");
```

### 7.2 UI Feedback

- Skill-gated options show "(Requires Scouting 40+)" as hint
- Show consequence preview: "This will affect: Soldier Rep, Scrutiny"
- Disabled options explain why

### 7.3 Save/Load

- Store cooldowns in `EscalationState` or new `ContentState`
- Store issued ration tracking
- Store QM shop inventory (T5+)

**Checklist:**
- [ ] Add comprehensive logging for event selection and effects
- [ ] Add skill threshold hints to UI
- [ ] Add consequence previews
- [ ] Test save/load with cooldowns
- [ ] Test save/load with issued rations
- [ ] Playtest for 30+ in-game days
- [ ] Balance XP amounts
- [ ] Balance event frequency

---

## Acceptance Criteria

### Content System

- [ ] Events fire every 3-5 days with appropriate content
- [ ] Role-specific events appear for matching roles
- [ ] Skill-gated options are properly locked/unlocked
- [ ] Effects apply correctly (XP, rep, escalation, gold, HP)
- [ ] Orders appear and can be accepted/declined
- [ ] Decisions appear in camp menu with costs/cooldowns
- [ ] Map incidents trigger at appropriate moments

### Quartermaster System

- [ ] QM Rep 65+ gives 30% equipment discount
- [ ] QM Rep -25 gives 40% equipment markup
- [ ] Supply <30% blocks equipment menu
- [ ] T1-T4 receive rations at muster (quality by QM Rep)
- [ ] Rations are reclaimed when issuing new ones
- [ ] T5+ can buy from provisions shop (150-200% of town)
- [ ] Baggage checks fire 30% at muster
- [ ] High QM Rep looks away at contraband
- [ ] Low QM Rep confiscates + adds Scrutiny

### Integration

- [ ] Muster cycle triggers pay, rations, and baggage checks
- [ ] Scrutiny threshold triggers appropriate events
- [ ] Events can modify QM Rep
- [ ] Baggage check events affect Scrutiny
- [ ] Supply crisis triggers crisis events

---

## Reference Documents

| Document | Purpose |
|----------|---------|
| `docs/StoryBlocks/content-index.md` | Master content catalog (176 pieces) |
| `docs/StoryBlocks/event-catalog-by-system.md` | Schema, design principles, systems |
| `docs/Features/Equipment/Quartermaster_Master_Implementation.md` | QM-specific details |
| `docs/Features/Equipment/player-food-ration-system.md` | Food/ration system |
| `docs/Features/Equipment/company-supply-simulation.md` | Supply hybrid simulation |
| `docs/Features/Equipment/quartermaster-dialogue-implementation.md` | QM dialogue trees |

---

## Implementation Order Summary

| Phase | Focus | Key Deliverables |
|-------|-------|------------------|
| 1 | Infrastructure | JSON loader, skill checks, data models |
| 2 | Escalation | Threshold triggers, QM Rep pricing |
| 3 | Delivery | Event popups, order popups, decision menu |
| 4 | Quartermaster | Pricing, rations, baggage checks, supply gate |
| 5 | Content | JSON files, XML strings, loading |
| 6 | Intelligence | Selection algorithm, pacing, weighting |
| 7 | Polish | Logging, UI feedback, save/load, testing |

**Estimated Timeline**: Phases build on each other. Complete Phase 1-3 before significant content authoring. Phase 4 can parallel Phase 3. Phase 5-7 depend on earlier phases.

