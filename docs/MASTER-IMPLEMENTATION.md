# Master Implementation Plan: Order Prompt Model

**Summary:** Eliminate random event interrupts. Orders auto-assign, 85% of phases are routine (nothing happens), 15% fire a mini-prompt asking player if they want to engage. Player choice leads to CK3-style event chains with consequences.

**Status:** 📋 Planning  
**Created:** 2026-01-14  
**Target Completion:** 2-3 weeks  
**Related Research:** [CK3 Feast Chain Analysis](ANEWFEATURE/ck3-feast-chain-analysis.md)

---

## Index

1. [Core Vision](#core-vision)
2. [Current Problems](#current-problems)
3. [New Architecture](#new-architecture)
4. [Implementation Plan](#implementation-plan)
5. [Systems to Remove](#systems-to-remove)
6. [Systems to Keep](#systems-to-keep)
7. [Event Chain Design](#event-chain-design)
8. [Testing Checklist](#testing-checklist)

---

## Core Vision

### The Problem We're Solving
Random event interrupts feel jarring at 2x speed. Events "happen to" the player instead of the player choosing to engage.

### The Solution: Order Prompt Model

**How it works:**
```
Order auto-assigned: "Guard Duty - 2 days"
  ↓
Phase progresses (Dawn/Midday/Dusk/Night)
  ↓
85% of phases: Routine - nothing happens
  "The watch continues uneventfully."
  ↓
15% of phases: PROMPT fires
  "You hear rustling in the bushes."
  [Investigate] [Stay Focused]
  ↓
Player clicks [Investigate]: Event chain fires
Player clicks [Stay Focused]: Nothing, move on
  ↓
Event chain: 50% nothing / 30% gold / 20% ambush
```

**Key principles:**
1. **Orders are auto-assigned** - realistic for a soldier
2. **Most duty is routine** - 85% nothing happens (like real military life)
3. **Player chooses to engage** - prompts ask permission before events fire
4. **Consequences follow choices** - CK3-style chains with outcomes
5. **No random interrupts** - events only fire when player engages

### CK3 Research Insight

CK3 fires very few random events during normal play:
- **Yearly events:** 25% chance (`chance_to_happen = 25`)
- **Event pools:** 500-1000 weight for "nothing" vs 50-100 for events
- **Result:** 3-5 random events per YEAR for players

The drama comes from **activities** (feasts, hunts) where events ARE frequent because the player initiated them.

**Your orders = CK3 activities.** Make them event-rich via prompts, but keep idle time quiet.

---

## Current Problems

### Five Overlapping Content Systems

The mod currently has multiple content delivery systems that overlap and cause confusion:

| System | File | Behavior | Problem |
|--------|------|----------|--------|
| **Decisions** | DecisionManager.cs | Player browses Camp Hub, chooses | ✅ Working correctly |
| **Order Events** | OrderProgressionBehavior.cs | Fire randomly at slot phases | Should be player-initiated |
| **Narrative Events** | EventPacingManager.cs | Fire randomly every 3-5 days | **THE PROBLEM** - random spam |
| **Map Incidents** | MapIncidentManager.cs | Fire on battle/settlement/siege | ✅ Contextual, keep |
| **Camp Opportunities** | ContentOrchestrator.cs | Pre-scheduled 24h ahead | Overcomplicated |

### The Core Problem: EventPacingManager.TryFireEvent()

```csharp
// EventPacingManager.cs line 133-176
private void TryFireEvent(EscalationState escalationState)
{
    // This fires random narrative events every 3-5 days
    // Player has NO CHOICE - event just interrupts them
    var selectedEvent = EventSelector.SelectEvent(worldSituation);
    deliveryManager.QueueEvent(selectedEvent); // POPUP!
}
```

This is the random event spam. It needs to be **deleted**.

### What Players Experience Now

1. Playing game at 2x speed (1 day = ~1 minute)
2. Random popup: "A soldier approaches you..." (didn't ask for this)
3. Another random popup: "You overhear rumors..." (interruption)
4. Order event fires: "Strange noise during guard duty" (OK, contextual)
5. More random popups...

**Result:** Player feels bombarded by content they didn't choose.

---

## New Architecture: The Order Prompt Model

### Design Philosophy
Inspired by CK3's feast event chains:
- **Player chooses to engage** (via prompts during orders)
- **Outcomes are pre-rolled** (hidden setup before player sees choices)
- **Chains create narrative arcs** (not isolated random events)
- **85% routine, 15% interesting** (CK3's weighted "nothing" approach)

### The Flow

```
Order auto-assigned → Player works through phases → 85% nothing / 15% prompt fires

[Prompt Example]
┌─────────────────────────────────────┐
│ SOMETHING STIRS                     │
│                                     │
│ You hear rustling in the bushes     │
│ nearby. Could be nothing. Could be  │
│ trouble.                            │
│                                     │
│ [Investigate]  [Stay Focused]       │
└─────────────────────────────────────┘

Player clicks [Investigate] → Event chain fires (outcome was pre-rolled)
Player clicks [Stay Focused] → Nothing happens, order continues
```

### What Gets Removed

| System | File | Action |
|--------|------|--------|
| Random narrative events | EventPacingManager.cs | **DELETE** TryFireEvent() |
| GlobalEventPacer | enlisted_config.json | **DELETE** pacing section |
| Context Events (65) | narrative-events.json | **CONVERT** to chain outcomes |

### What Gets Kept

| System | File | Notes |
|--------|------|-------|
| Orders (17) | orders.json | Auto-assigned, phases progress |
| Decisions (37) | decisions.json | Player browses Camp Hub menu |
| Map Incidents (51) | map-incidents.json | Battle/settlement/siege triggers |
| Threshold events | escalation-events.json | Supply pressure, illness arcs |

---

## Implementation: Order Prompt System

### Core Logic (OrderProgressionBehavior.cs)

```csharp
// During each slot phase transition
private void ProcessSlotPhase(Order currentOrder)
{
    // 85% of the time: routine, nothing happens
    var roll = MBRandom.RandomFloat;
    if (roll >= 0.15f) 
    {
        // Silent phase - order progresses normally
        return;
    }
    
    // 15% of the time: show a prompt
    ShowOrderPrompt(currentOrder);
}

private void ShowOrderPrompt(Order order)
{
    // PRE-ROLL the outcome (CK3 pattern)
    var outcome = RollPromptOutcome(order);
    
    // Get contextual prompt text based on order type
    var prompt = GetPromptForOrder(order);
    
    InquiryData inquiry = new InquiryData(
        prompt.Title,           // "Something Stirs"
        prompt.Description,     // "You hear rustling in the bushes..."
        true, true,
        "Investigate",          // Positive button
        "Stay Focused",         // Negative button  
        () => FireEventChain(order, outcome),  // Player engages
        () => { /* Nothing - order continues */ }  // Player ignores
    );
    
    InformationManager.ShowInquiry(inquiry);
}

private PromptOutcome RollPromptOutcome(Order order)
{
    // CK3-style weighted random list
    // Outcome is determined BEFORE player sees prompt
    var roll = MBRandom.RandomFloat;
    
    // Example weights (varies by order type):
    // 50% = nothing interesting
    // 30% = small reward (gold, item, reputation)
    // 15% = interesting event chain
    // 5%  = danger (ambush, injury)
    
    if (roll < 0.50f) return PromptOutcome.Nothing;
    if (roll < 0.80f) return PromptOutcome.SmallReward;
    if (roll < 0.95f) return PromptOutcome.EventChain;
    return PromptOutcome.Danger;
}
```

### Prompt Templates by Order Type

```json
// order_prompts.json (NEW FILE)
{
  "schemaVersion": 1,
  "prompts": [
    {
      "order_types": ["guard_duty", "camp_patrol"],
      "prompts": [
        {
          "title": "Something Stirs",
          "description": "You hear rustling in the bushes nearby. Could be nothing. Could be trouble.",
          "investigate_text": "Investigate",
          "ignore_text": "Stay at Post"
        },
        {
          "title": "Distant Noise",
          "description": "A sound carries from the treeline. Too faint to identify.",
          "investigate_text": "Check It Out",
          "ignore_text": "Ignore It"
        },
        {
          "title": "Strange Light",
          "description": "A brief flicker of light in the darkness. Probably a firefly. Probably.",
          "investigate_text": "Move Closer",
          "ignore_text": "Keep Watch"
        }
      ]
    },
    {
      "order_types": ["foraging", "supply_run"],
      "prompts": [
        {
          "title": "Off the Path",
          "description": "You spot what might be an old campsite through the brush. Worth investigating?",
          "investigate_text": "Search the Area",
          "ignore_text": "Stay on Task"
        },
        {
          "title": "Bodies Ahead",
          "description": "You come across bodies in the field. Recent, from the look of them.",
          "investigate_text": "Check Them",
          "ignore_text": "Move On"
        }
      ]
    },
    {
      "order_types": ["scout_duty", "reconnaissance"],
      "prompts": [
        {
          "title": "Smoke on the Horizon",
          "description": "A thin column of smoke rises in the distance. Not on your planned route.",
          "investigate_text": "Investigate",
          "ignore_text": "Continue Mission"
        },
        {
          "title": "Fresh Tracks",
          "description": "Horse tracks, headed away from the main road. Could be nothing.",
          "investigate_text": "Follow Them",
          "ignore_text": "Mark and Report"
        }
      ]
    }
  ]
}
```

### Event Chain Outcomes

```json
// prompt_outcomes.json (NEW FILE)
{
  "schemaVersion": 1,
  "outcomes": {
    "nothing": [
      {
        "text": "Just the wind. Nothing there.",
        "effects": {}
      },
      {
        "text": "A rabbit bolts from the underbrush. False alarm.",
        "effects": {}
      },
      {
        "text": "You find nothing of interest. Time wasted, but at least you were thorough.",
        "effects": { "fatigue": 2 }
      }
    ],
    "small_reward": [
      {
        "text": "You find a coin purse dropped by a careless traveler.",
        "effects": { "gold": "25-75" }
      },
      {
        "text": "An abandoned pack contains useful supplies.",
        "effects": { "company_supplies": 3 }
      },
      {
        "text": "A fellow soldier saw you checking the perimeter. Word gets around.",
        "effects": { "officer_reputation": 2 }
      }
    ],
    "event_chain": [
      {
        "chain_id": "deserter_encounter",
        "text": "You find a deserter from another company, hiding in the brush...",
        "trigger_event": "evt_deserter_chain_start"
      },
      {
        "chain_id": "wounded_soldier",
        "text": "A wounded soldier lies hidden in the grass, barely alive...",
        "trigger_event": "evt_wounded_soldier_start"
      }
    ],
    "danger": [
      {
        "text": "Ambush! Bandits were waiting in the bushes!",
        "effects": { "trigger_combat": "bandit_ambush_small" }
      },
      {
        "text": "You step into a concealed pit. Your ankle twists painfully.",
        "effects": { "injury": "sprained_ankle", "fatigue": 15 }
      }
    ]
  }
}
```

---

## Event Chain Design (CK3 Pattern)

### Chain Structure

```json
// event_chains.json (NEW FILE)
{
  "chains": [
    {
      "id": "deserter_encounter",
      "name": "The Deserter",
      "phases": [
        {
          "phase": 1,
          "event_id": "evt_deserter_chain_start",
          "title": "A Desperate Man",
          "description": "The man begs you not to turn him in. He says the officers beat him, that he couldn't take another day. He's thin, exhausted, terrified.",
          "choices": [
            {
              "text": "Let him go",
              "next_phase": 2,
              "flag": "showed_mercy"
            },
            {
              "text": "Take him to the officers",
              "next_phase": 3,
              "flag": "turned_in"
            },
            {
              "text": "Give him food and directions away from camp",
              "next_phase": 2,
              "flag": "helped_escape",
              "requirements": { "soldier_reputation": 20 }
            }
          ]
        },
        {
          "phase": 2,
          "event_id": "evt_deserter_mercy",
          "condition": "showed_mercy OR helped_escape",
          "title": "Gone",
          "description": "He vanishes into the night. You wonder if you'll ever see him again.",
          "delay_days": "7-14",
          "follow_up": {
            "chance": 0.3,
            "event_id": "evt_deserter_returns",
            "condition": "helped_escape"
          },
          "immediate_effects": {
            "soldier_reputation": 3,
            "scrutiny": 1
          }
        },
        {
          "phase": 3,
          "event_id": "evt_deserter_turned_in",
          "condition": "turned_in",
          "title": "Justice",
          "description": "The officers thank you for your diligence. The deserter is hauled away. You try not to hear him screaming.",
          "immediate_effects": {
            "officer_reputation": 5,
            "soldier_reputation": -8,
            "gold": 15
          }
        }
      ]
    }
  ]
}
```

### CK3 Probability Model Applied

From CK3's feast events analysis:
- `random_list { 500 = { nothing } 50 = { actual_event } }` = 90% nothing
- Hidden setup events pre-roll outcomes
- Delayed follow-ups create anticipation

**Applied to Enlisted:**

```
Phase Transition Check:
├── 85% → Routine (silent, order continues)
└── 15% → Prompt fires
        ├── Player clicks [Ignore] → Nothing
        └── Player clicks [Investigate] → Pre-rolled outcome
                ├── 50% → Nothing interesting
                ├── 30% → Small reward
                ├── 15% → Event chain starts
                └── 5%  → Danger
```

**Net result:** ~2-3% of phases lead to actual interesting content.
With 4 phases/day, that's roughly 1 interesting event every 8-12 days.
Much closer to CK3's pacing than current spam.

---

## Systems to Modify

### OrderProgressionBehavior.cs Changes

**Current (lines ~89-120):**
```csharp
private void ProcessSlotPhase(Order order)
{
    // Fires order events randomly
    if (ShouldFireOrderEvent(order))
    {
        var evt = SelectOrderEvent(order);
        deliveryManager.QueueEvent(evt);
    }
}
```

**New:**
```csharp
private void ProcessSlotPhase(Order order)
{
    // 85% routine - no prompt
    if (MBRandom.RandomFloat >= 0.15f)
        return;
    
    // 15% - show player a prompt
    ShowOrderPrompt(order);
}
```

### EventPacingManager.cs Changes

**DELETE entire TryFireEvent() method (lines 133-176)**

This is the random event spam. Remove it entirely.

### enlisted_config.json Changes

**Remove:**
```json
"decision_events": {
  "pacing": {
    "event_window_min_days": 3,
    "event_window_max_days": 5
  }
}
```

**Add:**
```json
"order_prompts": {
  "prompt_chance": 0.15,
  "outcome_weights": {
    "nothing": 50,
    "small_reward": 30,
    "event_chain": 15,
    "danger": 5
  }
}
```

---

## Content Conversion Plan

### Converting 65 Context Events → Chain Outcomes

The current 65 "Context Events" that fire randomly should be converted:

| Current Category | Count | Conversion |
|-----------------|-------|------------|
| Social encounters | 18 | → Camp Decisions (player-initiated) |
| Discovery events | 12 | → Prompt outcomes (small_reward) |
| Danger events | 15 | → Prompt outcomes (danger) |
| Story hooks | 20 | → Event chains (event_chain) |

### Order Events (84) → Prompt System

The 84 existing order events become:
- Prompt templates (contextual text per order type)
- Outcome pools (what happens when player investigates)
- Event chains (multi-phase narratives)

---

## Testing Checklist

### Prompt System
- [ ] 85% of phases pass silently
- [ ] 15% of phases show prompt
- [ ] Prompts are contextual to order type
- [ ] [Ignore] button does nothing
- [ ] [Investigate] triggers pre-rolled outcome

### Outcome Distribution
- [ ] 50% of investigations yield nothing
- [ ] 30% yield small rewards (gold, supplies, rep)
- [ ] 15% trigger event chains
- [ ] 5% trigger danger

### Event Chains
- [ ] Chains progress through phases
- [ ] Player choices set flags
- [ ] Flags affect later phases
- [ ] Delayed follow-ups fire correctly
- [ ] Chain state persists across saves

### Pacing
- [ ] Average 1 interesting event per 8-12 days
- [ ] No more random popup spam
- [ ] Threshold events (supply crisis, etc.) still fire
- [ ] Decisions still available in Camp Hub
- [ ] Map incidents still fire on context

---

## Implementation Priority

### Phase 1: Remove Random Events (1-2 days)
1. Delete `EventPacingManager.TryFireEvent()` method
2. Remove pacing config from `enlisted_config.json`
3. Test that random events no longer fire
4. Verify threshold events still work

### Phase 2: Add Prompt System (3-5 days)
1. Create `OrderPromptManager` class
2. Add prompt check to `OrderProgressionBehavior.ProcessSlotPhase()`
3. Create `order_prompts.json` with templates
4. Create `prompt_outcomes.json` with outcomes
5. Implement pre-roll outcome logic

### Phase 3: Event Chains (5-7 days)
1. Create `EventChainManager` class
2. Create `event_chains.json` schema
3. Convert 20 story hook events to chains
4. Implement flag system for chain branching
5. Add delayed follow-up scheduling

### Phase 4: Content Conversion (Ongoing)
1. Convert context events to appropriate categories
2. Write new prompt templates per order type
3. Create outcome pools with variety
4. Write 5-10 event chains with branches

---

## Appendix: CK3 Research Summary

### Key Findings from CK3 Feast Events

Location: `C:\Program Files (x86)\Steam\steamapps\common\Crusader Kings III\game\events\activities\feast_activity\`

1. **Heavy "nothing" weighting:**
   - `random_list { 500 = { nothing } 50 = { actual_event } }` common pattern
   - Result: Events feel rare and special, not spammy

2. **Hidden setup events:**
   - `feast_event_setup_0001` fires invisibly, pre-rolls outcome
   - Player sees result event, not the dice roll

3. **Flag-based branching:**
   - `set_variable { name = feast_outcome_generous }` stores choice
   - Later phases check: `has_variable = feast_outcome_generous`

4. **Delayed follow-ups:**
   - `trigger_event = { id = feast_follow_up days = { 7 14 } }`
   - Creates anticipation between chain phases

5. **Yearly event frequency:**
   - `yearly_on_actions.txt` shows `chance_to_happen = 25`
   - Most random events: 500-1000 weight "nothing" vs 50-100 "event"
   - Net result: ~3-5 random events per year

### Research Date
January 14, 2026

See also: `docs/ANEWFEATURE/ck3-feast-chain-analysis.md` for detailed analysis

---

**End of Order Prompt Model Specification**
