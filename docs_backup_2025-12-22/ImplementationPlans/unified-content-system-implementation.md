# Unified Content & Quartermaster System Implementation

> **Purpose**: Single implementation plan for the narrative content system (events, orders, decisions, map incidents) AND the quartermaster system (equipment, food, supply, baggage checks). These systems share dependencies and must be built together.

**Last Updated**: December 22, 2025  
**Status**: Phase 5 Complete - Content Loading, Localization & Decisions System (with fixes)  
**Target Game Version**: Bannerlord v1.3.11

---

## Engineering Standards

**Follow these while implementing all phases:**

### Code Quality
- **Follow ReSharper linter/recommendations.** Fix warnings; don't suppress them with pragmas.
- **Comments should be factual descriptions of current behavior.** Write them as a human developer would—professional and natural. Don't use "Phase" references, changelog-style framing ("Added X", "Changed from Y"), or mention legacy/migration in doc comments. Just describe what the code does.
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
- **CRITICAL:** In JSON, fallback fields (`title`, `setup`, `text`, `resultText`) must immediately follow their ID fields (`titleId`, `setupId`, `textId`, `resultTextId`) for proper parser association. See [Decisions System](#decisions-system--new---december-2025) for details.

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
- Food: T1-T6 issued rations, T7+ commander retinue provisioning
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
  2. Ration exchange (T1-T6) OR refresh provisions shop (T7+)
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

### Recently Implemented ✅

| Component | Location | Notes |
|-----------|----------|-------|
| **Event Catalog** | `src/Features/Content/EventCatalog.cs` | Loads JSON from Events/, supports old/new schema |
| **Event Models** | `src/Features/Content/EventDefinition.cs` | Full data model for events, options, effects |
| **Skill Check Helper** | `src/Features/Content/SkillCheckHelper.cs` | Universal formula: Base% + (Skill/3) |
| **Event Delivery Manager** | `src/Features/Content/EventDeliveryManager.cs` | Queue system, MultiSelectionInquiryData popups, effect application |
| **Role Requirements** | `EventDeliveryManager.cs` | Checks player specialization, shows "Requires X" hints |
| **CurrentSpecialization** | `EnlistmentBehavior.cs` | Property exposing player's role from EnlistedStatusManager |
| **Threshold Event Triggers** | `EscalationManager.cs` (updated) | Auto-queues events when Scrutiny/Discipline/Medical cross thresholds |
| **QM Reputation Pricing** | `EnlistmentBehavior.cs` methods | GetEquipmentPriceMultiplier, GetBuybackMultiplier, GetOfficerFoodPriceMultiplier |
| **QM Pricing Integration** | `QuartermasterManager.cs` | Uses rep + camp mood multipliers, debug logging |
| **Supply Gate** | `EnlistedMenuBehavior.cs` | Blocks QM menu when supplies < 15% |
| **Per-Item Stock System** | `QuartermasterManager.cs` | Stock rolls at muster based on supply level |
| **Camp News Section** | `EnlistedMenuBehavior.cs` | Shows supply/stock/muster news when notable |
| **Content Migration** | Built into EventCatalog | Auto-maps lance_reputation→soldierRep, formation→role, etc. |
| **Sample Events** | `ModuleData/Enlisted/Events/samples/` | 6 events total (scout, escalation, camp, threshold_test) |

### Decisions System ✅ (NEW - December 2025)

| Component | Location | Notes |
|-----------|----------|-------|
| **DecisionCatalog** | `src/Features/Content/DecisionCatalog.cs` | Filters events with `category: "decision"`, organizes by section |
| **DecisionManager** | `src/Features/Content/DecisionManager.cs` | Cooldowns, gates (tier, time, flags), availability checking |
| **Decision Menu** | `EnlistedMenuBehavior.cs` | Accordion-style menu with OPPORTUNITIES, TRAINING, SOCIAL, CAMP LIFE, LOGISTICS (no icons on individual entries) |
| **Decision JSON** | `ModuleData/Enlisted/Events/events_player_decisions.json` | 6 player-initiated decisions |
| **Decision JSON** | `ModuleData/Enlisted/Events/events_decisions.json` | 11 automatic decisions (opportunities) |
| **Decision XML** | `ModuleData/Languages/enlisted_strings.xml` | 95+ localized strings for all decisions |
| **UI Documentation** | `docs/Features/UI/ui-systems-master.md` | Comprehensive UI systems documentation with localization troubleshooting |

**CRITICAL LOCALIZATION PATTERN (Fixed Dec 22, 2025):**

JSON fallback fields MUST immediately follow their ID fields for the parser to associate them correctly:

```json
{
  "titleId": "decision_training_title",
  "title": "Request Extra Training",        ← Must be IMMEDIATELY after titleId
  "setupId": "decision_training_setup",
  "setup": "The drillmaster approaches...",  ← Must be IMMEDIATELY after setupId
  "options": [...]
}
```

**Common Mistake:** Placing `title` and `setup` at the end of the JSON object causes raw string IDs to display in popups instead of localized text. This affected 17 decision events and was fixed on Dec 22, 2025.

**Terminology Update (December 2025):**

| Old Term | New Term | Notes |
|----------|----------|-------|
| `lance_reputation` | `camp_reputation` | Effect key in JSON |
| `{LANCE_MATE}` | `{COMRADE_NAME}` | Placeholder for culture-generated soldier name |
| `{LANCE_LEADER_SHORT}` | `{SERGEANT_NAME}` | Placeholder for culture-generated NCO name |
| "lance mates" | "comrades" | Player-facing text |
| "the lance" | "the company" | Player-facing text |

### Phase 8 Implementations ✅

| Component | Location | Notes | Status |
|-----------|----------|-------|--------|
| **Flag System** | `EscalationState.cs`, `EventDeliveryManager.cs` | ActiveFlags dictionary, SetFlag/ClearFlag/HasFlag methods | ✅ Phase 8A |
| **Chain Events with Delay** | `EscalationState.cs`, `EventDeliveryManager.cs`, `EventPacingManager.cs` | PendingChainEvents, ScheduleChainEvent, PopReadyChainEvents, daily tick | ✅ Phase 8B (Dec 22, 2025) |
| **Reward Choices (Sub-Choices)** | `EventDefinition.cs`, `EventCatalog.cs`, `EventDeliveryManager.cs` | RewardChoices/RewardChoiceOption classes, ParseRewardChoices, ShowSubChoicePopup, ApplyRewards/ApplyCosts | ✅ Phase 8C (Dec 22, 2025) |

### NOT Yet Implemented ❌

| Component | Why Needed | Notes | Implementation Plan |
|-----------|------------|-------|---------------------|
| **Map Incidents** | Event delivery during travel | Hooks in EnlistedIncidentsBehavior | Future |
| **Ration Exchange** | T1-T4 food at muster | Needs item tracking system | Future |
| **Baggage Check** | Contraband search at muster | Complex system | Future |
| **Supply Simulation** | Hybrid 40/60 model | Currently just degrades | Future |

**Phase 8 Implementation Plan:** See [`phase8-advanced-content-features.md`](phase8-advanced-content-features.md) for detailed implementation steps for Reward Choices, Flag System, and Chain Events.

### Reusable Patterns

| Existing Code | How to Extend | Status |
|---------------|---------------|--------|
| `OrderCatalog.cs` | Pattern copied for `EventCatalog` | ✅ Done |
| `Order.cs` models | Pattern copied for `EventDefinition` | ✅ Done |
| `OrderManager.cs` | Copy for `EventDeliveryManager` | ✅ Done |
| `EnlistedMenuBehavior.cs` | Decision options in camp hub | ✅ Done |
| `EnlistedIncidentsBehavior.cs` | Add map incident hooks | Pending |
| `EscalationManager.cs` | Add threshold event triggering | ✅ Done |
| `QuartermasterManager.cs` | Pricing uses multipliers, stock system | ✅ Done |
| `EnlistmentBehavior.cs` | Muster triggers stock roll | ✅ Done |

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

*Reference Code:*
- [x] `OrderCatalog.cs` exists - used as pattern for EventCatalog
- [x] `Order.cs` models exist - used as pattern for EventDefinition
- [x] JSON loading works for orders - extended pattern for events

*Completed:*
- [x] Create `EventDefinition.cs` with all data models (`src/Features/Content/EventDefinition.cs`)
- [x] Create `EventCatalog.cs` with JSON loading (`src/Features/Content/EventCatalog.cs`)
- [x] Add fallback parsing for old schema (lance_reputation → soldierRep, formation → role, trait_xp.Leadership → traitXp.Commander)
- [x] Add `SkillCheckHelper.cs` with universal formulas (`src/Features/Content/SkillCheckHelper.cs`)
- [x] Create 3 sample events in simplified schema (`ModuleData/Enlisted/Events/samples/`)
- [x] Add XML strings for sample events (`ModuleData/Languages/enlisted_strings.xml`)
- [x] EventCatalog loads both old and new format files
- [x] Migration warnings logged when old fields are encountered

*Not Started:*
- [ ] Audit existing JSON files, document which are usable (can be done incrementally)

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

*Phase 2 Complete:*
- [x] Add threshold crossing detection to `EscalationManager` (logs when thresholds crossed)
- [x] Add `GetQMReputation()` method to EnlistmentBehavior (uses Hero.GetRelation)
- [x] Add `GetEquipmentPriceMultiplier()` method to EnlistmentBehavior (0.70 to 1.40)
- [x] Add `GetBuybackMultiplier()` method to EnlistmentBehavior (0.30 to 0.65)
- [x] Add `GetOfficerFoodPriceMultiplier()` method to EnlistmentBehavior (1.50 to 2.00)
- [x] Wire pricing methods to `QuartermasterManager` purchase/buyback calculations

*Phase 3 Complete:*
- [x] Wire threshold detection to EventDeliveryManager (event delivery pipeline)
- [x] Create EventDeliveryManager.cs with queue and MultiSelectionInquiryData UI
- [x] Test: Scrutiny 4 triggers `evt_scrutiny_4` event popup (ready for in-game testing)

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

*Phase 3 Complete:*
- [x] Create `EventDeliveryManager.cs` (copy OrderManager pattern)
- [x] Wire event delivery to use `MultiSelectionInquiryData`
- [x] Wire threshold detection in EscalationManager to queue events
- [x] Add skill-gated option hints ("Requires Scouting 40+")
- [x] Register EventDeliveryManager in SubModule.cs
- [x] Create sample threshold test events (evt_scrutiny_4, evt_discipline_6, evt_medical_3)

*Decisions System Complete (December 2025):*
- [x] Add decision options to camp hub menu (DecisionManager + EnlistedMenuBehavior)
- [x] DecisionCatalog loads decisions from JSON
- [x] Accordion-style menu with collapsible sections
- [x] Cooldown tracking and display
- [x] Tier/time/flag gates with tooltips
- [x] 95+ XML strings for all decision text
- [ ] Test: Manual event delivery works

---

## Phase 4: Quartermaster Integration

> ✅ **COMPLETED** - December 21, 2025

**Goal**: Wire the quartermaster system to use new pricing, supply gates, and stock availability.

### Phase 3 Deferred Items - RESOLVED

**1. Role-Based Requirements** ✅
- Added `CurrentSpecialization` property to `EnlistmentBehavior.cs`
- Updated `MeetsRequirements()` in `EventDeliveryManager.cs` to check player role against option requirements
- Updated `GetOptionHint()` to show "Requires [Role] specialization" hints
- Event options are now properly disabled for players without the required role

**2. Text Localization** 
- Still deferred to Phase 5 (intentional). Event text shows IDs as placeholders until XML localization is implemented.

---

### 4.1 Equipment Pricing Update ✅

Pricing now uses QM reputation multipliers. Already implemented in Phase 2, verified working:

- `EnlistmentBehavior.GetEquipmentPriceMultiplier()` - 0.70 (30% discount) to 1.40 (40% markup)
- `EnlistmentBehavior.GetBuybackMultiplier()` - 0.30 (hostile) to 0.65 (trusted)
- `QuartermasterManager.CalculateQuartermasterPrice()` uses both rep and camp mood multipliers
- Debug logging added to show price calculations

### 4.2 Supply Gate ✅

Quartermaster menu blocked when supplies critically low:

- **Threshold**: Supplies < 15% blocks entire menu access
- **Location**: `EnlistedMenuBehavior.cs` camp hub quartermaster option
- **Message**: "Quartermaster unavailable. The company's supplies are critically low."

### 4.3 Per-Item Stock Availability ✅ (NEW)

Individual items can be out of stock based on supply levels. Stock is rolled once per muster:

| Supply Level | Out-of-Stock Chance |
|--------------|---------------------|
| ≥ 60% | 0% (all items in stock) |
| 40-59% | 20% per item |
| 15-39% | 50% per item |
| < 15% | Menu blocked entirely |

**Implementation:**
- `EquipmentVariantOption.IsInStock` property added
- `QuartermasterManager.RollStockAvailability()` called at muster completion
- `_outOfStockItems` HashSet tracks unavailable items until next muster
- UI shows "(Out of Stock)" and blocks purchase for unavailable items

### 4.4 Camp News Section ✅ (NEW)

Camp hub displays company simulation updates at the top:

**Company Health:**
- Wounded soldiers currently recovering (from party roster)
- Soldiers lost since last muster (real battle data, resets at muster)
- Sickness spreading through camp (when applicable)

**Morale & Supplies:**
- Morale status (breaking, low, or high spirits)
- Food supply warnings (critical, low, thin)
- Supply shortages and quartermaster stock availability

**Administrative:**
- Upcoming muster reminders (within 2 days)
- Owed backpay warnings
- Muster awaiting attention

Shows "All quiet in camp. No urgent matters." when nothing notable to report.

### 4.5 Deferred to Future Phases

- **Ration Exchange (T1-T6)**: Needs design work for issued ration tracking
- **Baggage Check**: Complex system with contraband scanning
- **Decision Menu Integration**: Optional camp menu decisions

**Checklist:**

*Completed:*
- [x] Role-based requirements working in EventDeliveryManager
- [x] CurrentSpecialization property in EnlistmentBehavior
- [x] Equipment pricing uses QM reputation multipliers
- [x] Supply gate blocks QM menu at < 15%
- [x] Per-item stock availability system
- [x] Stock rolls at muster based on supply level
- [x] "Out of Stock" display in QM variant menu
- [x] Camp News section in camp hub with company simulation data
- [x] Wounded/sick/lost tracking from real battle data
- [x] "Since last muster" counters reset at muster completion
- [x] Casualty reports with RP flavor in Company Report
- [x] Debug logging for price calculations

*Deferred:*
- [ ] Ration exchange in muster (T1-T4)
- [ ] Ration tracking (`_issuedFoodRations` list)
- [ ] Baggage check roll (30% at muster)
- [ ] Contraband scanning logic
- [ ] Decision menu integration

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

*Phase 5 Complete (Dec 2024-2025):*
- [x] Created folder structure: `ModuleData/Enlisted/Events/Role/`
- [x] Created 9 sample events in NEW schema format (3 files: scout_events.json, muster_events.json, camp_events.json)
- [x] Added 72 XML strings for all sample events to enlisted_strings.xml
- [x] EventCatalog already loads both old and new format with migration support
- [x] Content count logging already implemented at startup (EventCatalog.cs line 76)
- [x] Implemented ResolveText() in EventDeliveryManager using TextObject for XML lookup
- [x] **Fixed JSON field ordering** (Dec 22): Moved fallback fields to immediately follow ID fields in 17 decision events
- [x] **Created UI documentation** (Dec 22): `docs/Features/UI/ui-systems-master.md` with localization system reference
- [x] **Fixed decision menu icons** (Dec 22): Individual entries no longer show icons, only section headers do

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

*Implemented:*
- [x] `EventSelector.SelectEvent()` - weighted selection algorithm
- [x] `EventRequirementChecker.cs` - tier, role, context, skills, traits, escalation
- [x] Cooldown tracking in `EscalationState` (EventLastFired, OneTimeEventsFired)
- [x] Weighting system (role match 2×, context match 1.5×, priority modifiers)
- [x] `EventPacingManager.cs` - daily tick, 3-5 day pacing window
- [x] Save/load persistence for all pacing and cooldown data

*Testing:*
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
- [x] Add comprehensive logging for event selection and effects (implemented in Phase 6)
- [ ] Add skill threshold hints to UI
- [ ] Add consequence previews
- [x] Test save/load with cooldowns (EscalationManager.SyncData extended)
- [ ] Test save/load with issued rations
- [ ] Playtest for 30+ in-game days
- [ ] Balance XP amounts
- [ ] Balance event frequency

---

## Acceptance Criteria

### Content System

- [x] Events fire every 3-5 days with appropriate content (Phase 6 - EventPacingManager)
- [x] Role-specific events appear for matching roles (Phase 6 - 2× weight)
- [x] Skill-gated options are properly locked/unlocked (Phase 3 - EventDeliveryManager)
- [x] Effects apply correctly (XP, rep, escalation, gold, HP) (Phase 3 - EventDeliveryManager.ApplyEffects)
- [ ] Orders appear and can be accepted/declined
- [x] Decisions appear in camp menu with costs/cooldowns (December 2025 - DecisionManager)
- [ ] Map incidents trigger at appropriate moments

### Quartermaster System

- [ ] QM Rep 65+ gives 30% equipment discount
- [ ] QM Rep -25 gives 40% equipment markup
- [ ] Supply <30% blocks equipment menu
- [ ] T1-T6 receive rations at muster (quality by QM Rep)
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
| `docs/ImplementationPlans/phase8-advanced-content-features.md` | **Phase 8: Flag system, chain events, reward choices** |
| `docs/ImplementationPlans/phase9-logistics-simulation.md` | **Phase 9: Supply simulation, map incidents, rations, news context** |
| `docs/ImplementationPlans/phase10-combat-xp-training.md` | **Phase 10: Combat XP, weapon-aware training, troop training** |
| `docs/ImplementationPlans/onboarding-retirement-system.md` | **Onboarding: Experience tracks, discharge bands** |
| `docs/Features/UI/ui-systems-master.md` | UI systems and localization reference |
| `docs/StoryBlocks/content-index.md` | Master content catalog (176+ pieces) |
| `docs/StoryBlocks/event-catalog-by-system.md` | Schema, design principles, systems |
| `docs/Features/Equipment/Quartermaster_Master_Implementation.md` | QM-specific details |
| `docs/Features/Equipment/player-food-ration-system.md` | Food/ration system |
| `docs/Features/Equipment/company-supply-simulation.md` | Supply hybrid simulation |
| `docs/Features/Equipment/quartermaster-dialogue-implementation.md` | QM dialogue trees |

---

## Implementation Order Summary

| Phase | Focus | Key Deliverables | Status |
|-------|-------|------------------|--------|
| 1 | Infrastructure | JSON loader, skill checks, data models | ✅ Complete |
| 2 | Escalation | Threshold triggers, QM Rep pricing | ✅ Complete |
| 3 | Delivery | Event popups, order popups, decision menu | ✅ Complete |
| 4 | Quartermaster | Pricing, rations, baggage checks, supply gate | ✅ Complete |
| 5 | Content | JSON files, XML strings, loading | ✅ Complete |
| 6 | Intelligence | Selection algorithm, pacing, weighting | ✅ Complete |
| 7 | Polish | Logging, UI feedback, save/load, testing | Pending |
| **8A** | **Flag System** | **set_flags, clear_flags, flag_duration_days** | **✅ Complete** |
| **8B** | **Chain Events** | **chains_to, chain_delay_hours, scheduling** | **✅ Complete (Dec 22, 2025)** |
| **8C** | **Reward Choices** | **Sub-choice popups, reward_choices** | **✅ Complete (Dec 22, 2025)** |

**Phase 8 Details:** See [`phase8-advanced-content-features.md`](phase8-advanced-content-features.md) for implementation plans.

| Sub-Phase | Feature | Time | Status |
|-----------|---------|------|--------|
| 8A | Flag System | 2-3 hours | ✅ Complete (Dec 2025) |
| 8B | Chain Events with Delay | ~1 hour | ✅ Complete (Dec 22, 2025) |
| 8C | Reward Choices (Sub-choices) | ~1 hour | ✅ Complete (Dec 22, 2025) |

---

**Phase 9: Logistics, Map Simulation & News Context**

See [`phase9-logistics-simulation.md`](phase9-logistics-simulation.md) for full implementation plans.

| Sub-Phase | Feature | Time | Status |
|-----------|---------|------|--------|
| 9A | Supply Simulation (40/60 hybrid) | 3-4 days | ✅ Complete |
| 9B | Map Incidents (45 incidents) | 2-3 days | ✅ Complete |
| 9C | Ration Exchange (muster food) | 2-3 days | ✅ Complete |
| 9D | Baggage Check (contraband) | 3-4 days | ✅ Complete (Dec 22, 2025) |
| 9E | Event Context in News | 2-3 days | ✅ Complete |

---

**Phase 10: Combat XP & Training Enhancements**

See [`phase10-combat-xp-training.md`](phase10-combat-xp-training.md) for full implementation plans.

| Sub-Phase | Feature | Time | Status |
|-----------|---------|------|--------|
| 10A | Weapon-Aware Training | 2-3 hours | ✅ Complete (Dec 22, 2025) |
| 10B | Robust Troop Training | 1-2 hours | ✅ Complete (Dec 22, 2025) |
| 10C | XP Feedback in News | 1-2 hours | ✅ Complete (Dec 22, 2025) |
| 10D | Experience Track Modifiers | 1 hour | ✅ Complete (Dec 22, 2025) |

**Key Dependencies:**
- Phase 10A uses Phase 8C's `reward_choices` for weapon focus selection
- Phase 10B reports training to Phase 9E's Personal Feed
- Phase 10D shares experience track code with Onboarding System

**Estimated Timeline**: Phases build on each other. Complete Phase 1-3 before significant content authoring. Phase 4 can parallel Phase 3. Phase 5-7 depend on earlier phases. Phase 9A/9E can run in parallel. Phase 10 can begin after Phase 8-9.

