# Opportunities System - Implementation Spec

**Summary:** Technical specification for implementing the automatic decision triggering system that populates the Camp Hub "OPPORTUNITIES" section with game-initiated narrative events based on context, time, and conditions.

**Status:** 📋 Specification (Not Yet Implemented)  
**Last Updated:** 2025-12-23  
**Related Docs:** [Content Index](../Content/content-index.md), [UI Systems](../Features/UI/ui-systems-master.md), [Decision System](../Features/Content/content-system-architecture.md)

---

## Index

1. [Overview](#overview)
2. [Purpose](#purpose)
3. [Current State](#current-state)
4. [Inputs & Outputs](#inputs--outputs)
5. [Behavior](#behavior)
6. [Trigger Evaluation Logic](#trigger-evaluation-logic)
7. [Edge Cases](#edge-cases)
8. [Acceptance Criteria](#acceptance-criteria)
9. [Implementation Checklist](#implementation-checklist)

---

## Overview

The Opportunities system delivers **automatic decisions** to the player via the Camp Hub menu's "OPPORTUNITIES" section. Unlike player-initiated decisions (which the player controls from TRAINING, SOCIAL, etc.), opportunities are **game-triggered events** that appear based on context, time of day, activity state, and story flags.

**Current Status:** Placeholder implementation. The section exists in the UI but always shows "(none available)" because trigger evaluation is not implemented.

**Content Ready:** 11 automatic decisions are already defined in `ModuleData/Enlisted/Events/events_decisions.json` with full localization, waiting for the trigger system.

---

## Purpose

### Design Intent

Opportunities create a sense that **the world responds to the player** rather than waiting passively. They provide:

1. **Proactive storytelling** - Events seek the player out
2. **Time pressure** - Limited-time offers that expire
3. **Contextual immersion** - Events match what's happening (sieges, camps, marches)
4. **Reward for engagement** - Players who check regularly get more content

### Gameplay Role

Opportunities are **opt-in interruptions** that appear in a dedicated menu section rather than blocking popups. This respects player agency while surfacing special events they might want to engage with.

---

## Current State

### What Exists

**UI Section:**
- "OPPORTUNITIES" header in Camp Hub accordion
- Collapse/expand functionality
- `[NEW]` tag support for new opportunities
- Auto-clear after 1 day or when section is expanded

**Content:**
- 11 automatic decisions in `events_decisions.json`
- All strings localized in `enlisted_strings.xml`
- Trigger conditions defined in JSON

**Code Infrastructure:**
- `DecisionManager.GetAvailableOpportunities()` method exists (returns empty)
- `DecisionCatalog.GetAutomaticDecisions()` loads automatic decisions
- Menu integration complete

### What's Missing

The **trigger evaluation logic** that determines when an automatic decision should appear as an opportunity. Currently hardcoded to return empty:

```csharp
public IReadOnlyList<DecisionAvailability> GetAvailableOpportunities()
{
    // Automatic decisions ("Opportunities") require trigger evaluation 
    // (activity, flags, weekly_tick, etc.).
    // That logic is intentionally deferred; returning none keeps the 
    // menu clean until triggers are implemented.
    return Array.Empty<DecisionAvailability>();
}
```

---

## Inputs & Outputs

### Inputs

**From Game State:**
- Current time of day (morning, afternoon, evening, night)
- Player enlistment tier
- Activity context (camp, march, settlement, siege)
- Active flags (story progress markers)
- Cooldown state (when each decision last appeared)
- Random chance rolls

**From Decision Definition (JSON):**
```json
{
  "id": "decision_lord_hunt_invitation",
  "category": "decision",
  "delivery": {
    "method": "automatic",  // ← Marks as opportunity
    "channel": "inquiry"
  },
  "triggers": {
    "all": ["is_enlisted", "not_in_settlement"],
    "time_of_day": ["morning", "afternoon"]
  },
  "requirements": {
    "tier": { "min": 5, "max": 999 }
  },
  "timing": {
    "cooldown_days": 14,
    "priority": "normal"
  }
}
```

### Outputs

**To UI:**
- List of `DecisionAvailability` objects for opportunities that passed all checks
- Each opportunity shows as selectable option in OPPORTUNITIES section
- Selecting an opportunity opens popup inquiry with options
- All text supports placeholder variables (e.g., `{PLAYER_NAME}`, `{LORD_NAME}`, `{SERGEANT}`, `{SETTLEMENT_NAME}`)

**To State:**
- Updates cooldown tracking when opportunity is selected
- Sets flags if opportunity outcomes modify story state
- Removes opportunity from available list after selection

**Text Variables Available:**
All opportunity title/setup/option text can use 40+ placeholder variables that are populated at runtime. Common examples:
- Player: `{PLAYER_NAME}`, `{PLAYER_RANK}`
- NPCs: `{LORD_NAME}`, `{SERGEANT}`, `{OFFICER_NAME}`, `{VETERAN_1_NAME}`
- Location: `{SETTLEMENT_NAME}`, `{COMPANY_NAME}`, `{FACTION_NAME}`

Full reference: [Event Catalog - Placeholder Variables](../Content/event-catalog-by-system.md#placeholder-variables)

---

## Behavior

### When Player Opens Camp Hub

1. **Trigger Evaluation** runs for all automatic decisions
2. For each automatic decision:
   - Check if triggers match current context
   - Check if requirements are met (tier, flags, etc.)
   - Check if cooldown has expired
   - Roll random chance if specified
3. Matching decisions appear in OPPORTUNITIES section
4. Player selects one → opens as popup inquiry → resolves like any event

### Opportunity Lifecycle

```
[Defined in JSON]
       ↓
[Trigger Check: Pass] → Appears in OPPORTUNITIES → [Player Selects]
       ↓                                                    ↓
[Trigger Check: Fail] → Hidden                    [Opens Popup Inquiry]
       ↓                                                    ↓
[On Cooldown] → Hidden                            [Player Chooses Option]
                                                            ↓
                                                    [Effects Applied]
                                                            ↓
                                                    [Cooldown Starts]
```

### Frequency & Pacing

Opportunities should feel **occasional but not overwhelming**:

- Average 1-2 opportunities visible at any time
- Most have 7-14 day cooldowns
- Some are tier-gated (only T5+, T7+)
- Some require specific contexts (siege, camp, etc.)
- Time-of-day restrictions limit when they can appear

**Note:** If opportunities are delivered via automatic events (rather than menu-only), they would be subject to global event pacing limits defined in `enlisted_config.json` → `decision_events.pacing`. See [Event System Schemas](../Features/Content/event-system-schemas.md#global-event-pacing-enlisted_configjson) for details.

---

## Trigger Evaluation Logic

### Trigger Types (from JSON `triggers` field)

#### 1. Context Triggers (`triggers.all` array)

Standard conditions that must all be true:

| Trigger String | Condition |
|----------------|-----------|
| `is_enlisted` | Player is currently enlisted |
| `not_in_settlement` | Party is on campaign map (not in town/village) |
| `in_settlement` | Party is in settlement |
| `at_camp` | Party is waiting/resting |
| `on_march` | Party is moving |
| `during_siege` | Party is besieging settlement |
| `after_battle` | Within 6 hours of battle end |

#### 2. Time of Day (`triggers.time_of_day` array)

Must match current time block:

| JSON Value | Game Time |
|------------|-----------|
| `morning` | 6:00 - 12:00 |
| `afternoon` | 12:00 - 18:00 |
| `evening` | 18:00 - 21:00 (Dusk) |
| `night` | 21:00 - 6:00 |

#### 3. Flag Triggers (`triggers.all` with `has_flag:` prefix)

Story progress flags:

```json
"triggers": {
  "all": ["has_flag:met_quartermaster", "has_flag:promotion_pending"]
}
```

Uses `has_flag:flagName` format. Checks `EscalationState.HasFlag(flagName)` via `EventRequirementChecker.CheckTriggerCondition()`.

#### 4. Activity Triggers (Future)

Weekly/daily activity patterns:

```json
"triggers": {
  "activity": ["weekly_tick", "daily_inspection", "pay_day"]
}
```

Not yet implemented - requires activity calendar system.

### Evaluation Algorithm

**Note:** Much of the trigger evaluation logic already exists in `EventRequirementChecker.cs`, which handles `triggers.all`, `triggers.any`, and `triggers.escalation_requirements` for events. This same logic can be reused for opportunities.

```csharp
public IReadOnlyList<DecisionAvailability> GetAvailableOpportunities()
{
    var opportunities = new List<DecisionAvailability>();
    var automaticDecisions = DecisionCatalog.GetAutomaticDecisions();
    
    foreach (var decision in automaticDecisions)
    {
        // 1. Check basic requirements (tier, enlistment, etc.)
        var availability = CheckAvailability(decision);
        if (!availability.IsAvailable || !availability.IsVisible)
            continue;
            
        // 2. Check trigger conditions (reuse EventRequirementChecker logic)
        if (!EvaluateTriggers(decision))
            continue;
            
        // 3. Roll random chance if specified
        if (decision.Timing.RandomChance.HasValue)
        {
            if (MBRandom.RandomFloat > decision.Timing.RandomChance.Value)
                continue;
        }
        
        // 4. Passed all checks - add to available opportunities
        opportunities.Add(availability);
    }
    
    return opportunities;
}

private bool EvaluateTriggers(DecisionDefinition decision)
{
    // Check context triggers (all must be true)
    if (!CheckContextTriggers(decision.TriggersAll))
        return false;
        
    // Check time of day (must match one)
    if (!CheckTimeOfDay(decision.TimeOfDay))
        return false;
        
    // Check flags (all required flags must be active)
    if (!CheckRequiredFlags(decision.RequiredFlags))
        return false;
        
    // Check blocking flags (none can be active)
    if (CheckBlockingFlags(decision.BlockingFlags))
        return false;
        
    return true;
}
```

---

## Edge Cases

### 1. Multiple Opportunities Available

**Issue:** 5+ opportunities all trigger at once, overwhelming player.

**Solution:** 
- Priority system: `timing.priority` = "high", "normal", "low"
- Limit display to top 3 by priority
- Rest stay hidden until higher-priority ones are completed

### 2. Time-Limited Opportunities

**Issue:** Opportunity appears but becomes invalid before selection (time of day changes).

**Solution:**
- Cache opportunity validity on menu open
- Don't re-evaluate until menu is closed and reopened
- If player selects and conditions changed, show graceful message: "This opportunity is no longer available."

### 3. Cooldown During Session

**Issue:** Player completes opportunity, immediately re-opens menu, sees same opportunity again.

**Solution:**
- Cooldown tracking persists immediately via `EscalationState.RecordEventFired()`
- Next menu open will filter it out

### 4. Save/Load Mid-Opportunity

**Issue:** Player saves with opportunity visible, loads, opportunity is gone.

**Solution:**
- Opportunities are **ephemeral** - they re-evaluate on menu open
- Save/load does not preserve "pending" opportunities
- Only cooldowns and completed flags persist

### 5. No Opportunities for Long Period

**Issue:** Player never sees opportunities due to bad RNG or wrong tier/context.

**Solution:**
- At least 2-3 opportunities should have very broad triggers (any tier, any context)
- Example: "Evening Dice Game" only requires evening time + enlisted
- Ensures something appears regularly

---

## Acceptance Criteria

### ✅ Feature Complete When:

1. **Trigger Evaluation Works:**
   - [ ] Context triggers (`is_enlisted`, `not_in_settlement`, etc.) correctly evaluate
   - [ ] Time of day matching works for all 4 time blocks
   - [ ] Flag triggers check `EscalationState` correctly
   - [ ] Combined AND logic (all triggers must pass) works

2. **Opportunities Appear Correctly:**
   - [ ] Opening Camp Hub evaluates all automatic decisions
   - [ ] Matching opportunities appear in OPPORTUNITIES section
   - [ ] No opportunities shows "(none available)" message
   - [ ] `[NEW]` tag appears when new opportunities arrive

3. **Selection & Execution:**
   - [ ] Selecting opportunity opens popup inquiry
   - [ ] Options present correctly with tooltips
   - [ ] Effects apply correctly (reputation, gold, flags, etc.)
   - [ ] Cooldown starts after selection

4. **State Persistence:**
   - [ ] Cooldowns persist across save/load
   - [ ] One-time opportunities don't reappear
   - [ ] Flag-based opportunities respect flag state

5. **UX Polish:**
   - [ ] No more than 3 opportunities show at once (priority filtering)
   - [ ] Expired opportunities don't error when selected
   - [ ] `[NEW]` tag clears after expanding section or 1 day
   - [ ] At least 1-2 opportunities available regularly (not weeks of drought)

### 🧪 Test Scenarios

1. **Basic Trigger:** "Evening Dice Game" appears only during evening/night
2. **Tier Gate:** "Lord's Hunt" only appears at T5+
3. **Context Gate:** "Guard Commander" only appears when `at_camp`
4. **Cooldown:** Selecting opportunity makes it unavailable for specified days
5. **Flag Requirement:** Opportunity requiring `has_flag:met_quartermaster` only appears after quartermaster introduction
6. **Priority:** When 5 opportunities trigger, only top 3 by priority show

---

## Implementation Checklist

### Phase 1: Core Trigger Evaluation

**Note:** Much of this logic already exists in `EventRequirementChecker.cs` and can be reused.

- [ ] Implement `EvaluateTriggers()` method in `DecisionManager` (or reuse `EventRequirementChecker.CheckTriggerConditions()`)
- [ ] Verify `CheckContextTriggers()` covers all needed conditions (most already in `EventRequirementChecker`)
  - [ ] `is_enlisted` - Check `EnlistmentBehavior.IsEnlisted` ✅ (already exists)
  - [ ] `not_in_settlement` - Check `MobileParty.MainParty.IsActive` ✅ (already exists)
  - [ ] `in_settlement` - Check `MobileParty.MainParty.CurrentSettlement != null` ✅ (already exists)
  - [ ] `at_camp` - Check `MobileParty.MainParty.IsActive && !IsMoving`
  - [ ] `on_march` - Check `MobileParty.MainParty.IsMoving`
- [ ] Verify `CheckTimeOfDay()` using `CampaignTriggerTrackerBehavior.GetTimeBlock()`
- [ ] Verify `CheckRequiredFlags()` using `EscalationState.HasFlag()` ✅ (already exists in `EventRequirementChecker`)

### Phase 2: Opportunity Selection

- [ ] Replace `return Array.Empty<DecisionAvailability>()` in `GetAvailableOpportunities()`
- [ ] Add call to `EvaluateTriggers()` for each automatic decision
- [ ] Filter by availability (tier, cooldown, one-time)
- [ ] Return list of passing opportunities

### Phase 3: Priority & Limits

- [ ] Add priority sorting (high > normal > low)
- [ ] Limit returned list to top 3 opportunities
- [ ] Add random chance roll if `timing.random_chance` specified in JSON

### Phase 4: Extended Triggers (Optional)

- [ ] Implement `during_siege` trigger (may already exist in `EventRequirementChecker`)
- [ ] Implement `after_battle` trigger (6 hour window, may already exist)
- [ ] Implement activity calendar triggers (weekly_tick, pay_day, etc.)
- [ ] Add escalation requirement support for opportunities (e.g., `scrutiny_min`, `discipline_min`, `pay_tension_min`) - already available for events

### Phase 5: Testing & Balance

- [ ] Verify each of 11 automatic decisions appears under correct conditions
- [ ] Test cooldown tracking across save/load
- [ ] Test priority filtering when multiple opportunities trigger
- [ ] Adjust cooldowns and random chances for good pacing
- [ ] Verify at least 1-2 opportunities available most of the time

---

## Content Reference

### 11 Automatic Decisions Ready

All defined in `ModuleData/Enlisted/Events/events_decisions.json`:

| ID | Name | Tier | Triggers | Cooldown |
|----|------|------|----------|----------|
| `decision_lord_hunt_invitation` | Invitation to Hunt | T5+ | morning/afternoon, not in settlement | 14 days |
| `decision_dice` | Evening Dice Game | Any | evening/night | 7 days |
| `decision_training_offer` | Training Opportunity | Any | Any time | 10 days |
| `decision_scout_assignment` | Special Scouting Assignment | T3+ | Any time | 12 days |
| `decision_medical_emergency` | Medical Emergency | Any | Any time | 15 days |
| `decision_equipment_shortage` | Equipment Shortage | Any | Any time | 10 days |
| `decision_promotion_ceremony` | Promotion Ceremony | Tier-up | One-time per tier | N/A |
| `decision_messenger_duty` | Messenger Duty | T2+ | Any time | 8 days |
| `decision_guard_commander` | Guard Commander | T4+ | at_camp | 10 days |
| `decision_strategy_council` | Strategy Council | T7+ | Any time | 14 days |
| `decision_diplomatic_escort` | Diplomatic Escort | T6+ | in_settlement | 20 days |

**Localization:** All strings exist in `enlisted_strings.xml` with `decision_*` prefix.

---

## Design Notes

### Why Menu Section Instead of Popup?

**Considered:** Auto-popup when opportunity triggers (like Bannerlord's native lord requests).

**Chosen:** Menu section approach because:
1. **Player control** - Can ignore opportunities without interruption
2. **Context awareness** - Player chooses when to engage based on their situation
3. **Multiple opportunities** - Can see all available at once and choose
4. **Immersion** - Fewer forced interruptions maintains campaign flow

### Why Cooldowns?

Prevents repetitive spam of the same opportunities. Player shouldn't see "Lord's Hunt" every day. 7-14 day cooldowns create variety and make each opportunity feel special.

### Why Priority System?

Without limits, 5+ opportunities could flood the section. Priority filtering ensures:
- High-priority story events always show (promotions, special missions)
- Low-priority flavor events (dice games) appear when nothing else is happening
- Player isn't overwhelmed with choices

---

## Future Enhancements

### Phase 2 Features (Not Required for Initial Implementation)

1. **Activity Calendar System**
   - Weekly patterns (payday every 7 days, inspections every 3 days)
   - Trigger opportunities based on calendar events
   - Adds predictability and ritual to campaign

2. **Chained Opportunities**
   - Completing one opportunity unlocks follow-up
   - Example: "Hunt with Lord" → "Lord's Private Conversation"
   - Uses flag system to track chains

3. **Expiring Opportunities**
   - Some opportunities disappear if not taken within timeframe
   - Example: "Urgent message delivery" expires in 2 hours
   - Adds time pressure and consequence to ignoring opportunities

4. **Context-Aware Descriptions**
   - Opportunity text changes based on current situation
   - Example: "Guard Commander" mentions current location in setup text
   - ✅ **Already Available:** Comprehensive placeholder variable support implemented in `EventDeliveryManager.SetEventTextVariables()`. See [Event Catalog](../Content/event-catalog-by-system.md#placeholder-variables) for full list of 40+ available placeholders including `{PLAYER_NAME}`, `{LORD_NAME}`, `{SETTLEMENT_NAME}`, `{SERGEANT}`, `{VETERAN_1_NAME}`, etc.

---

**Last Updated:** 2025-12-23 (Updated with event pacing system and placeholder variable references)  
**Implementation Status:** Not Started  
**Blockers:** None - all infrastructure ready, just needs trigger evaluation logic

**Related Systems:**
- Trigger evaluation reuses `EventRequirementChecker.cs` logic
- Text variables use `EventDeliveryManager.SetEventTextVariables()` (40+ placeholders available)
- If delivered as automatic events, subject to global pacing limits in `enlisted_config.json` → `decision_events.pacing`


