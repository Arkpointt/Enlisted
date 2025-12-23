# Content System Architecture

**Summary:** The unified content system manages all narrative content (events, decisions, orders, map incidents) through a centralized delivery pipeline. The system uses JSON content definitions, XML localization, requirement checking, and effect application to create dynamic player experiences that integrate with enlistment, reputation, and progression systems.

**Status:** ✅ Current  
**Last Updated:** 2025-12-22  
**Related Docs:** [Event Catalog](../../Content/event-catalog-by-system.md), [Training System](../Combat/training-system.md)

---

## Index

1. [Overview](#overview)
2. [Content Types](#content-types)
3. [System Architecture](#system-architecture)
4. [Content Pipeline](#content-pipeline)
5. [Requirement System](#requirement-system)
6. [Effect System](#effect-system)
7. [Delivery Contexts](#delivery-contexts)
8. [JSON Structure](#json-structure)
9. [Localization](#localization)
10. [Implementation Files](#implementation-files)

---

## Overview

The content system provides a unified pipeline for delivering narrative content:

**Content Types:**
- **Orders** - Military directives from chain of command
- **Decisions** - Player-initiated camp menu actions
- **Events** - Context-triggered narrative moments
- **Map Incidents** - Native Bannerlord incident integration

**Core Responsibilities:**
- Load content from JSON files at startup
- Check requirements before delivery
- Apply effects after player choices
- Integrate with escalation, reputation, and progression
- Provide news feed and UI feedback

**Design Philosophy:**
- Data-driven (JSON + XML)
- Extensible (new content without code changes)
- Integrated (shares state with enlistment/QM systems)
- Validated (requirement checks prevent invalid delivery)

---

## Content Types

### Orders

Military directives from the chain of command that the player must complete:

**Characteristics:**
- Issued by superiors (lord, officer, sergeant)
- Have completion criteria and time limits
- Reward completion, penalize failure
- Track completion in service record

**Examples:**
- Scouting duty (reconnaissance mission)
- Supply run (acquire provisions)
- Patrol duty (area security)
- Guard duty (static defense)

### Decisions

Player-initiated actions from the camp menu:

**Characteristics:**
- Player chooses when to attempt
- Often have skill checks or costs
- Provide progression and customization
- Can be daily or limited-use

**Examples:**
- Training (skill improvement)
- Social interactions (reputation building)
- Commerce (buy/sell/gamble)
- Risky actions (calculated risks)

### Events

Context-triggered narrative moments:

**Characteristics:**
- Fire based on context (camp, march, battle, muster)
- Check requirements (tier, role, reputation, traits)
- Provide choices with consequences
- Drive narrative and character development

**Examples:**
- Escalation thresholds (discipline warnings)
- Role-specific events (scout encounters)
- Universal events (camp life, social)
- Crisis events (supply shortages, mutiny)

### Map Incidents

Integration with native Bannerlord map incidents:

**Characteristics:**
- Use native incident system
- Trigger when conditions met
- Leverage existing map mechanics
- Provide Enlisted-specific outcomes

**Examples:**
- Encounter while leaving battle
- Events during siege
- Town/village entry incidents
- Waiting/resting incidents

---

## System Architecture

### Component Hierarchy

```
Content System
├── EventCatalog         (JSON loader, content registry)
├── EventDeliveryManager (Pipeline, effects, feedback)
├── EventRequirementChecker (Validation, gating)
├── DecisionCatalog      (Decision-specific loading)
├── OrderCatalog         (Order-specific loading)
└── MapIncidentManager   (Native integration)
```

### Data Flow

```
1. Content Loading (Startup)
   ├── Load JSON files from ModuleData/Enlisted/Events/
   ├── Parse event/decision/order definitions
   ├── Validate structure and IDs
   └── Register in catalogs

2. Content Delivery (Runtime)
   ├── Context trigger (camp menu, daily tick, battle end, etc.)
   ├── Query available content for context
   ├── Check requirements for each candidate
   ├── Select content (weighted random or player choice)
   └── Present to player

3. Player Response
   ├── Player selects option
   ├── Apply immediate effects (gold, rep, XP, etc.)
   ├── Apply delayed effects (orders, flags, timers)
   └── Display feedback (news, tooltips, notifications)

4. Effect Resolution
   ├── Update escalation state (Scrutiny, Discipline, Medical)
   ├── Update reputation (Officer, Soldier, QM, Lord)
   ├── Award XP and trait progress
   ├── Modify resources (gold, supplies, food)
   └── Set flags for follow-up events
```

---

## Content Pipeline

### Event Delivery Flow

```csharp
// 1. Context determines what content can fire
string context = "camp"; // or "march", "muster", "battle", etc.

// 2. Get candidate events for context
var candidates = EventCatalog.GetEventsForContext(context);

// 3. Filter by requirements
var validEvents = candidates.Where(e => 
    EventRequirementChecker.MeetsRequirements(e.Requirements));

// 4. Select event (weighted random or priority)
var selectedEvent = SelectEvent(validEvents);

// 5. Present to player
EventDeliveryManager.DeliverEvent(selectedEvent);

// 6. Player chooses option
var chosenOption = await GetPlayerChoice(selectedEvent.Options);

// 7. Apply effects
EventDeliveryManager.ApplyEffects(chosenOption.Effects);

// 8. Display feedback
PersonalDispatchManager.PostDispatch("Event Result", resultText);
```

### Decision Delivery Flow

```csharp
// 1. Player opens camp menu
ShowCampMenu();

// 2. Get available decisions
var decisions = DecisionCatalog.GetAvailableDecisions();

// 3. Filter by requirements (tier, role, cooldowns, etc.)
var validDecisions = decisions.Where(d => 
    MeetsDecisionRequirements(d));

// 4. Display in menu with costs/tooltips
DisplayDecisionMenu(validDecisions);

// 5. Player selects decision
var chosen = await GetPlayerDecisionChoice();

// 6. Deduct costs (gold, fatigue, etc.)
ApplyCosts(chosen.Costs);

// 7. Execute decision (may have sub-choices)
ExecuteDecision(chosen);

// 8. Apply rewards
ApplyRewards(chosen.Rewards);
```

---

## Requirement System

### Requirement Types

**Player State:**
```json
{
  "tier_min": 3,
  "tier_max": 6,
  "role": "Scout",
  "is_enlisted": true
}
```

**Reputation:**
```json
{
  "officer_rep_min": 40,
  "soldier_rep_min": -10,
  "qm_rep_min": 30
}
```

**Skills:**
```json
{
  "skill_checks": {
    "Scouting": 50,
    "Athletics": 30
  }
}
```

**Context:**
```json
{
  "context": ["camp", "march"],
  "time_of_day": "night",
  "in_settlement": false
}
```

**Flags:**
```json
{
  "required_flags": ["completed_onboarding"],
  "forbidden_flags": ["on_leave"]
}
```

**Onboarding:**
```json
{
  "onboarding_stage": 2,
  "onboarding_track": "green"
}
```

### Requirement Checking

```csharp
public bool MeetsRequirements(EventRequirements req)
{
    // Tier check
    if (!CheckTierRange(req.TierMin, req.TierMax))
        return false;
    
    // Role check
    if (req.Role != null && !HasRole(req.Role))
        return false;
    
    // Reputation checks
    if (!CheckReputationRequirements(req))
        return false;
    
    // Skill checks
    if (!CheckSkillRequirements(req.SkillChecks))
        return false;
    
    // Context checks
    if (!CheckContextRequirements(req.Context, req.TimeOfDay, req.InSettlement))
        return false;
    
    // Flag checks
    if (!CheckFlagRequirements(req.RequiredFlags, req.ForbiddenFlags))
        return false;
    
    // Onboarding checks
    if (!CheckOnboardingRequirements(req.OnboardingStage, req.OnboardingTrack))
        return false;
    
    return true;
}
```

---

## Effect System

### Effect Categories

**Resources:**
```json
{
  "gold": 100,
  "supplies": -5,
  "food": 3,
  "fatigue": -2
}
```

**Reputation:**
```json
{
  "officer_rep": 10,
  "soldier_rep": -5,
  "qm_rep": 3,
  "soldierRep": 5
}
```

**Escalation:**
```json
{
  "scrutiny": 5,
  "discipline": 2,
  "medical": -3
}
```

**XP & Traits:**
```json
{
  "reward_choices": [
    {
      "Scouting": 25,
      "Leadership": 15,
      "trait_xp": {
        "ScoutSkills": 10
      }
    }
  ]
}
```

**Flags & State:**
```json
{
  "set_flags": ["completed_scout_training"],
  "clear_flags": ["awaiting_assignment"],
  "advances_onboarding": true
}
```

**Special Effects:**
```json
{
  "triggers_discharge": true,
  "starts_order": "order_patrol_duty",
  "unlocks_decision": "decision_advanced_training"
}
```

### Risky Options

Options with success/failure outcomes:

```json
{
  "option_id": "risky_action",
  "risk_chance": 0.60,
  "effects_success": {
    "gold": 100,
    "officer_rep": 10
  },
  "effects_failure": {
    "scrutiny": 10,
    "officer_rep": -5
  }
}
```

---

## Delivery Contexts

### Context Types

| Context | When | Example Events |
|---------|------|----------------|
| `"camp"` | In camp, daily | Gambling, storytelling, brawls |
| `"march"` | Party moving | Patrol encounters, terrain challenges |
| `"muster"` | Every 12 days | Pay events, inspections, promotions |
| `"battle"` | After combat | Aftermath, looting, medical |
| `"settlement"` | In town/village | Town events, market encounters |
| `"waiting"` | Party waiting | Rest events, time-passing events |

### Context-Specific Behavior

**Camp Events:**
- Fire during daily tick
- Check fatigue, time of day
- Integrate with camp menu decisions

**Muster Events:**
- Fire on 12-day cycle
- Integrate with pay, rations, baggage checks
- Trigger promotions, reviews

**Battle Events:**
- Fire after combat ends
- Check battle outcome, casualties
- Award combat XP and trait progress

---

## JSON Structure

### Event Definition

```json
{
  "event_id": "evt_example",
  "titleId": "evt_example_title",
  "title": "Example Event",
  "setupId": "evt_example_setup",
  "setup": "Event description goes here.",
  "requirements": {
    "tier_min": 2,
    "context": ["camp"],
    "role": "Scout"
  },
  "options": [
    {
      "option_id": "option_1",
      "textId": "evt_example_opt1",
      "text": "First choice",
      "tooltip": "Explains consequences",
      "effects": {
        "gold": 50,
        "officer_rep": 5
      },
      "resultTextId": "evt_example_opt1_result",
      "resultText": "Result of first choice."
    },
    {
      "option_id": "option_2",
      "textId": "evt_example_opt2",
      "text": "Second choice",
      "tooltip": "Explains consequences",
      "risk_chance": 0.50,
      "effects_success": {
        "gold": 100
      },
      "effects_failure": {
        "scrutiny": 5
      },
      "resultTextId": "evt_example_opt2_result",
      "resultText": "Result of second choice."
    }
  ]
}
```

### Decision Definition

```json
{
  "decision_id": "decision_training",
  "nameId": "decision_training_name",
  "name": "Training",
  "descriptionId": "decision_training_desc",
  "description": "Improve your skills through practice.",
  "category": "training",
  "cost": {
    "gold": 0,
    "fatigue": 1
  },
  "requirements": {
    "tier_min": 1
  },
  "sub_choices": [
    {
      "id": "weapon_training",
      "textId": "decision_training_weapon",
      "text": "Weapon practice",
      "tooltip": "Train with equipped weapon",
      "reward_choices": [
        {
          "equipped_weapon_skill": 20,
          "applies_training_modifier": true
        }
      ]
    }
  ]
}
```

---

## Localization

### XML String Structure

All player-facing text is defined in `ModuleData/Languages/enlisted_strings.xml`:

```xml
<strings>
  <!-- Event title -->
  <string id="evt_example_title" text="Example Event" />
  
  <!-- Event setup -->
  <string id="evt_example_setup" text="Event description goes here." />
  
  <!-- Option text -->
  <string id="evt_example_opt1" text="First choice" />
  
  <!-- Result text -->
  <string id="evt_example_opt1_result" text="Result of first choice." />
</strings>
```

### TextObject Usage

```csharp
// In code, use TextObject with string IDs
var title = new TextObject("{=evt_example_title}Example Event");

// Fallback text is used if localization missing
// (useful during development)
```

### Fallback Strategy

JSON files include fallback text for development:

```json
{
  "titleId": "evt_example_title",
  "title": "Example Event",  // ← Fallback if ID not found
}
```

**Production:** String IDs resolve from XML  
**Development:** Fallback text displays if XML missing

---

## Implementation Files

### Core System

| File | Purpose |
|------|---------|
| `src/Features/Content/EventCatalog.cs` | JSON loading, content registry |
| `src/Features/Content/EventDeliveryManager.cs` | Delivery pipeline, effects |
| `src/Features/Content/EventRequirementChecker.cs` | Requirement validation |
| `src/Features/Content/EventDefinition.cs` | Content data structures |

### Subsystems

| File | Purpose |
|------|---------|
| `src/Features/Content/DecisionCatalog.cs` | Decision-specific loading |
| `src/Features/Content/RewardChoices.cs` | XP and reward handling |
| `src/Features/Content/ExperienceTrackHelper.cs` | Training modifiers |

### Content Files

| File | Content Type |
|------|--------------|
| `ModuleData/Enlisted/Events/events_escalation.json` | Escalation threshold events |
| `ModuleData/Enlisted/Events/events_crisis.json` | Crisis events |
| `ModuleData/Enlisted/Events/events_role_*.json` | Role-specific events |
| `ModuleData/Enlisted/Events/events_camp_life.json` | Universal camp events |
| `ModuleData/Enlisted/Events/events_training.json` | Training decisions |
| `ModuleData/Enlisted/Events/decisions_catalog.json` | Camp menu decisions |

### Localization

| File | Purpose |
|------|---------|
| `ModuleData/Languages/enlisted_strings.xml` | All player-facing text |

---

**End of Document**

