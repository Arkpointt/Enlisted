# Master Implementation Plan: Order Prompt Model

**Summary:** Eliminate random event interrupts. Orders auto-assign, 85% of phases are routine (nothing happens), 15% fire a mini-prompt asking player if they want to engage. Player choice leads to CK3-style event chains with consequences.

**Status:** 📋 Specification  
**Created:** 2026-01-14  
**Last Updated:** 2026-01-14  
**Target Completion:** 2-3 weeks  
**Related Docs:** [CK3 Feast Chain Analysis](ANEWFEATURE/ck3-feast-chain-analysis.md), [Order Progression System](Features/Core/order-progression-system.md), [Event System Schemas](Features/Content/event-system-schemas.md)  
**Target Version:** Bannerlord v1.3.13

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
    
    // Get contextual prompt text based on order type AND travel context (land/sea)
    var isAtSea = IsPartyAtSea();
    var prompt = GetPromptForOrder(order, isAtSea);
    
    InquiryData inquiry = new InquiryData(
        prompt.Title,           // "Something Stirs"
        prompt.Description,     // "You hear rustling in the bushes..." (land) or "Strange shadow in the water..." (sea)
        true, true,
        prompt.InvestigateText, // "Investigate" / "Check It Out" / etc.
        prompt.IgnoreText,      // "Stay Focused" / "Ignore It" / etc.
        () => FireEventChain(order, outcome),  // Player engages
        () => { /* Nothing - order continues */ }  // Player ignores
    );
    
    InformationManager.ShowInquiry(inquiry);
}

private OrderPrompt GetPromptForOrder(Order order, bool isAtSea)
{
    // Get prompts for this order type
    var prompts = PromptCatalog.GetPromptsForOrderType(order.Id);
    
    // Filter by travel context - only show contextually appropriate prompts
    var contextualPrompts = prompts.Where(p => 
        p.Contexts.Contains("any") || 
        (isAtSea && p.Contexts.Contains("sea")) ||
        (!isAtSea && p.Contexts.Contains("land"))
    ).ToList();
    
    // Pick random contextual prompt
    return contextualPrompts[MBRandom.RandomInt(contextualPrompts.Count)];
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

**IMPORTANT: Context Filtering**
Prompts must be contextually appropriate. "Rustling in bushes" makes no sense at sea. Use `contexts` field to filter:
- `["land"]` = Only fires on land (bushes, treeline, campsite)
- `["sea"]` = Only fires at sea (hull, rigging, waves, hold)
- `["any"]` = Fires in both contexts (rare, for universal prompts like "distant figure")

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
          "ignore_text": "Stay at Post",
          "contexts": ["land"]
        },
        {
          "title": "Distant Noise",
          "description": "A sound carries from the treeline. Too faint to identify.",
          "investigate_text": "Check It Out",
          "ignore_text": "Ignore It",
          "contexts": ["land"]
        },
        {
          "title": "Strange Light",
          "description": "A brief flicker of light in the darkness. Probably a firefly. Probably.",
          "investigate_text": "Move Closer",
          "ignore_text": "Keep Watch",
          "contexts": ["land"]
        },
        {
          "title": "Shadow Below",
          "description": "Something large moves beneath the hull. Too big to be a fish.",
          "investigate_text": "Look Closer",
          "ignore_text": "Keep Watch",
          "contexts": ["sea"]
        },
        {
          "title": "Loose Rope",
          "description": "A rope swings free in the rigging. Cut loose, or just poorly tied?",
          "investigate_text": "Investigate",
          "ignore_text": "Report It Later",
          "contexts": ["sea"]
        },
        {
          "title": "Strange Sound",
          "description": "Unusual creaking from the hold below. Could be cargo shifting. Could be something else.",
          "investigate_text": "Check Below",
          "ignore_text": "Stay at Post",
          "contexts": ["sea"]
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
          "ignore_text": "Stay on Task",
          "contexts": ["land"]
        },
        {
          "title": "Bodies Ahead",
          "description": "You come across bodies in the field. Recent, from the look of them.",
          "investigate_text": "Check Them",
          "ignore_text": "Move On",
          "contexts": ["land"]
        },
        {
          "title": "Floating Debris",
          "description": "Wreckage floats nearby. Could be from a merchant ship. Could be bait.",
          "investigate_text": "Investigate",
          "ignore_text": "Sail On",
          "contexts": ["sea"]
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
          "ignore_text": "Continue Mission",
          "contexts": ["land", "sea"]
        },
        {
          "title": "Fresh Tracks",
          "description": "Horse tracks, headed away from the main road. Could be nothing.",
          "investigate_text": "Follow Them",
          "ignore_text": "Mark and Report",
          "contexts": ["land"]
        },
        {
          "title": "Distant Sail",
          "description": "A sail appears on the horizon. Flying colors you don't recognize.",
          "investigate_text": "Close Distance",
          "ignore_text": "Note and Continue",
          "contexts": ["sea"]
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
3. **Run validation:** `python Tools/Validation/validate_content.py`
4. **Build and test:** `dotnet build -c "Enlisted RETAIL" /p:Platform=x64`
5. Test that random events no longer fire
6. Verify threshold events still work (supply crisis, illness arcs)

### Phase 2: Add Prompt System (3-5 days)
1. Create data structures (see Data Structures below)
2. **Add new .cs files to `Enlisted.csproj`** (validator will check)
3. **Register new types in `EnlistedSaveDefiner`** if they need persistence
4. Add prompt check to `OrderProgressionBehavior.ProcessSlotPhase()`
5. Create `ModuleData/Enlisted/Prompts/order_prompts.json` with templates
6. Create `ModuleData/Enlisted/Prompts/prompt_outcomes.json` with outcomes
7. Create `PromptCatalog` to load and manage prompts
8. Implement pre-roll outcome logic with ModLogger
9. **Run validation:** `python Tools/Validation/validate_content.py`
10. **Build and test:** `dotnet build -c "Enlisted RETAIL" /p:Platform=x64`

### Phase 3: Event Chains (5-7 days)
1. Create `EventChainManager` class (see Data Structures below)
2. **Add new .cs files to `Enlisted.csproj`**
3. **Register chain state in `EnlistedSaveDefiner`**
4. Create `ModuleData/Enlisted/Chains/event_chains.json` schema
5. Convert 20 story hook events to chains
6. Implement flag system for chain branching (use existing save system)
7. Add delayed follow-up scheduling (integrate with CampaignTime)
8. **Run validation:** `python Tools/Validation/validate_content.py`
9. Test chain branching with different choices

### Phase 4: Campaign Context Tracking (3-4 days)
1. Create `CampaignContextTracker` class in `src/Features/Content/`
2. **Add to `Enlisted.csproj`**
3. Hook into battle end events to record battles
4. Integrate with `WorldStateAnalyzer.DetermineActivityLevel()`
5. Add `valid_contexts` field to prompts and decisions
6. Filter content by campaign context
7. **Run validation:** `python Tools/Validation/validate_content.py`

### Phase 5: Map Incident Weighting (2-3 days)
1. Add CK3-style weighting to `MapIncidentManager`
2. Integrate with `ContentOrchestrator.GetCurrentWorldSituation()`
3. Make incident chances respect global activity levels
4. Test that incidents feel appropriately rare
5. **Run validation:** `python Tools/Validation/validate_content.py`

### Phase 6: Content Conversion (Ongoing)
1. Convert context events to appropriate categories
2. Write new prompt templates per order type (follow [Writing Style Guide](Features/Content/writing-style-guide.md))
3. Create outcome pools with variety
4. Write 5-10 event chains with branches
5. **Run validation after each content addition:** `python Tools/Validation/validate_content.py`
6. **Sync localization strings:** `python Tools/Validation/sync_event_strings.py`
7. Test in-game with different order types and contexts

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

## Map Incident Weighting (Phase 4)

### Problem

MapIncidentManager currently fires incidents at **100% rate** when eligible:
- Every battle end → Event fires
- Every settlement entry → Event fires
- Every settlement exit → Event fires

Only siege/waiting have chance rolls (10%/15% per hour).

At 2x speed, this creates **interrupt fatigue** even with cooldowns.

### Solution: CK3-Style Weighting + Orchestrator Integration

Make map incidents respect `ContentOrchestrator` activity levels:

```csharp
// src/Features/Content/MapIncidentManager.cs

private bool ShouldFireIncident(string context)
{
    // Get world situation from orchestrator
    var worldSituation = ContentOrchestrator.Instance?.GetCurrentWorldSituation();
    var activityLevel = worldSituation?.ExpectedActivity ?? ActivityLevel.Routine;
    
    // Base chance by activity level (CK3-style: most contexts are quiet)
    float baseChance = activityLevel switch
    {
        ActivityLevel.Quiet => 0.10f,    // 10% - garrison/recovery, less dramatic
        ActivityLevel.Routine => 0.20f,  // 20% - peacetime, normal pacing
        ActivityLevel.Active => 0.30f,   // 30% - campaign, elevated activity
        ActivityLevel.Intense => 0.40f,  // 40% - siege/battle, very eventful
        _ => 0.20f
    };
    
    // Context modifier (battles more dramatic than leaving settlements)
    float contextModifier = context switch
    {
        "leaving_battle" => 1.5f,      // Battles are dramatic
        "entering_town" => 1.0f,       // Towns normal
        "entering_village" => 0.8f,    // Villages quieter
        "leaving_settlement" => 0.75f, // Leaving less interesting
        "during_siege" => 1.2f,        // Siege events important
        "waiting_in_settlement" => 1.0f,
        _ => 1.0f
    };
    
    float finalChance = baseChance * contextModifier;
    
    ModLogger.Debug(LogCategory, 
        $"Incident chance for {context}: {finalChance * 100:F1}% (activity={activityLevel})");
    
    return MBRandom.RandomFloat < finalChance;
}
```

### Updated Incident Flow

**Before (Current):**
```
Battle ends → Check cooldown → TryDeliverIncident → Event fires (if eligible)
```

**After (CK3-Style):**
```
Battle ends → Check cooldown → ShouldFireIncident? (30% at Active)
  ├─ Yes → TryDeliverIncident → Event fires
  └─ No → Nothing (routine)
```

### Integration Points

**OnBattleEnd:**
```csharp
private void OnBattleEnd(MapEvent mapEvent)
{
    // ... existing checks ...
    
    if (!ShouldFireIncident("leaving_battle"))
    {
        ModLogger.Debug(LogCategory, "Battle incident roll failed (routine)");
        return;
    }
    
    if (TryDeliverIncident("leaving_battle"))
    {
        _lastBattleIncidentTime = CampaignTime.Now;
    }
}
```

**OnSettlementEntered:**
```csharp
private void OnSettlementEntered(MobileParty party, Settlement settlement, Hero hero)
{
    // ... existing checks ...
    
    var context = settlement.IsTown || settlement.IsCastle 
        ? "entering_town" 
        : "entering_village";
    
    if (!ShouldFireIncident(context))
    {
        ModLogger.Debug(LogCategory, $"{context} incident roll failed (routine)");
        return;
    }
    
    if (TryDeliverIncident(context))
    {
        _lastSettlementIncidentTime = CampaignTime.Now;
    }
}
```

**OnSettlementLeft:**
```csharp
private void OnSettlementLeft(MobileParty party, Settlement settlement)
{
    // ... existing checks ...
    
    if (!ShouldFireIncident("leaving_settlement"))
    {
        ModLogger.Debug(LogCategory, "Settlement exit incident roll failed (routine)");
        return;
    }
    
    if (TryDeliverIncident("leaving_settlement"))
    {
        _lastSettlementIncidentTime = CampaignTime.Now;
    }
}
```

### Expected Results

**Garrison (Quiet - 10% base):**
- Entering town: 10% chance
- Leaving settlement: 7.5% chance
- Battle end: 15% chance

**Peacetime Campaign (Routine - 20% base):**
- Entering town: 20% chance
- Leaving settlement: 15% chance
- Battle end: 30% chance

**Active Campaign (Active - 30% base):**
- Entering town: 30% chance
- Leaving settlement: 22.5% chance
- Battle end: 45% chance

**Siege (Intense - 40% base):**
- During siege (hourly): 48% chance (10% base * 1.2 * 4 checks/day)
- Battle end: 60% chance

### Benefits

1. **Respects global pacing** - Orchestrator controls overall activity
2. **Context-aware** - Battles more eventful than garrison
3. **Dynamic** - Changes with campaign situation
4. **CK3-aligned** - Most triggers are routine, events feel special
5. **Player-friendly** - No surprise spam at 2x speed

### Testing

- [ ] Garrison: Very few incidents (feels quiet)
- [ ] Campaign: Moderate incidents (feels active)
- [ ] Siege: Frequent incidents (feels intense)
- [ ] No interrupt fatigue at 2x speed
- [ ] Events feel special, not routine

---

## Campaign Context Tracking (Phase 4)

### Problem

Current `WorldStateAnalyzer` only checks "where is lord now" - but lords are ALWAYS moving.
Result: `ActivityLevel.Routine` 90% of the time, defeating dynamic pacing.

Also: No way to filter events by recent history. Card games shouldn't fire right after battle.

### Solution: Track Campaign History

Track **what has happened recently** to determine:
1. **Activity Level** - Based on recent battles, not current location
2. **Campaign Context** - Filter events to contextually appropriate ones

### Campaign Contexts

```csharp
public enum CampaignContext
{
    PostBattle,         // 0-24 hours since battle (rare events)
    RecentEngagement,   // 1-3 days since battle
    NormalCampaign,     // 3+ days since battle
    Garrison,           // In settlement, no recent battles (7+ days)
    PreBattle           // Enemy army nearby (future enhancement)
}
```

### Time Windows

| Context | Time Since Battle | Activity Level | Event Types |
|---------|-------------------|----------------|-------------|
| PostBattle | 0-24 hours | Intense | Loot dead, help wounded, report casualties |
| RecentEngagement | 1-3 days | Active | War stories, equipment repair, rest |
| NormalCampaign | 3-7 days | Routine | Patrols, training, gambling, social |
| Garrison | 7+ days | Quiet | Relaxation, town activities, downtime |

### CampaignContextTracker Class

```csharp
// src/Features/Content/CampaignContextTracker.cs
public class CampaignContextTracker : CampaignBehaviorBase
{
    private const string LogCategory = "CampaignContext";
    
    public static CampaignContextTracker Instance { get; private set; }
    
    // Battle tracking
    private CampaignTime _lastBattleTime = CampaignTime.Never;
    private int _lastBattleCasualties;
    private bool _wasVictorious;
    private int _battlesLastSevenDays;
    
    public CampaignContextTracker()
    {
        Instance = this;
    }
    
    public override void RegisterEvents()
    {
        CampaignEvents.OnPlayerBattleEndEvent.AddNonSerializedListener(this, OnBattleEnd);
    }
    
    public override void SyncData(IDataStore dataStore)
    {
        dataStore.SyncData("ctx_lastBattleTime", ref _lastBattleTime);
        dataStore.SyncData("ctx_lastBattleCasualties", ref _lastBattleCasualties);
        dataStore.SyncData("ctx_wasVictorious", ref _wasVictorious);
        dataStore.SyncData("ctx_battlesLastSevenDays", ref _battlesLastSevenDays);
    }
    
    private void OnBattleEnd(MapEvent mapEvent)
    {
        if (mapEvent == null || !mapEvent.IsPlayerMapEvent)
        {
            return;
        }
        
        _lastBattleTime = CampaignTime.Now;
        _wasVictorious = mapEvent.BattleState == BattleState.AttackerVictory; // Adjust based on player side
        _battlesLastSevenDays++;
        
        ModLogger.Info(LogCategory, 
            $"Battle recorded: Victory={_wasVictorious}, Context now PostBattle");
    }
    
    /// <summary>
    /// Gets current campaign context based on battle history.
    /// </summary>
    public CampaignContext GetCurrentContext()
    {
        if (_lastBattleTime == CampaignTime.Never)
        {
            return CampaignContext.Garrison;
        }
        
        var hoursSinceBattle = (float)(CampaignTime.Now - _lastBattleTime).ToHours;
        
        // PostBattle: 0-24 hours (rare events window)
        if (hoursSinceBattle < 24)
        {
            return CampaignContext.PostBattle;
        }
        
        // RecentEngagement: 1-3 days
        if (hoursSinceBattle < 72)
        {
            return CampaignContext.RecentEngagement;
        }
        
        // Garrison: 7+ days without battle
        if (hoursSinceBattle >= 168)
        {
            return CampaignContext.Garrison;
        }
        
        // NormalCampaign: 3-7 days
        return CampaignContext.NormalCampaign;
    }
    
    /// <summary>
    /// Gets activity level based on recent battle history.
    /// More accurate than location-based detection.
    /// </summary>
    public ActivityLevel GetActivityLevelFromHistory()
    {
        var context = GetCurrentContext();
        
        return context switch
        {
            CampaignContext.PostBattle => ActivityLevel.Intense,
            CampaignContext.RecentEngagement => ActivityLevel.Active,
            CampaignContext.NormalCampaign => ActivityLevel.Routine,
            CampaignContext.Garrison => ActivityLevel.Quiet,
            _ => ActivityLevel.Routine
        };
    }
    
    /// <summary>
    /// Hours since last battle. Used for decision/event filtering.
    /// </summary>
    public float GetHoursSinceLastBattle()
    {
        if (_lastBattleTime == CampaignTime.Never)
        {
            return float.MaxValue;
        }
        return (float)(CampaignTime.Now - _lastBattleTime).ToHours;
    }
    
    /// <summary>
    /// Was last battle a victory? Affects post-battle event tone.
    /// </summary>
    public bool WasLastBattleVictory() => _wasVictorious;
}
```

### Integration with WorldStateAnalyzer

```csharp
// Update WorldStateAnalyzer.DetermineActivityLevel()
private static ActivityLevel DetermineActivityLevel(LifePhase lifePhase, LordSituation lordSituation)
{
    // Crisis/Siege override history-based detection
    if (lifePhase == LifePhase.Crisis) return ActivityLevel.Intense;
    if (lifePhase == LifePhase.Siege) return ActivityLevel.Active;
    if (lifePhase == LifePhase.Recovery) return ActivityLevel.Quiet;
    
    // Use history-based detection for Campaign/Peacetime
    var contextTracker = CampaignContextTracker.Instance;
    if (contextTracker != null)
    {
        return contextTracker.GetActivityLevelFromHistory();
    }
    
    // Fallback to existing logic
    return lifePhase switch
    {
        LifePhase.Campaign => ActivityLevel.Routine,
        LifePhase.Peacetime => ActivityLevel.Quiet,
        _ => ActivityLevel.Routine
    };
}
```

### Event/Prompt Context Filtering

Add `valid_contexts` field to prompts:

```json
{
  "prompts": [
    {
      "title": "Loot the Fallen",
      "description": "Bodies lie scattered. Some may have coin.",
      "valid_contexts": ["PostBattle"],
      "contexts": ["land"]
    },
    {
      "title": "Help the Wounded",
      "description": "A soldier groans nearby, clutching his side.",
      "valid_contexts": ["PostBattle", "RecentEngagement"],
      "contexts": ["land", "sea"]
    },
    {
      "title": "War Stories",
      "description": "Veterans gather to share tales of past battles.",
      "valid_contexts": ["RecentEngagement", "NormalCampaign"],
      "contexts": ["land", "sea"]
    },
    {
      "title": "Card Game",
      "description": "Some soldiers are dealing cards by the fire.",
      "valid_contexts": ["NormalCampaign", "Garrison"],
      "contexts": ["land", "sea"]
    },
    {
      "title": "Something Stirs",
      "description": "You hear rustling in the bushes nearby.",
      "valid_contexts": ["NormalCampaign", "Garrison"],
      "contexts": ["land"]
    }
  ]
}
```

### Decision Availability Windows

Post-battle decisions appear in Camp Hub only during valid window:

```json
{
  "decisions": [
    {
      "id": "dec_loot_fallen",
      "title": "Loot the Fallen",
      "valid_contexts": ["PostBattle"],
      "max_hours_since_battle": 24,
      "effects": { "gold": "30-100", "scrutiny": 2 }
    },
    {
      "id": "dec_help_wounded",
      "title": "Help the Wounded", 
      "valid_contexts": ["PostBattle", "RecentEngagement"],
      "max_hours_since_battle": 48,
      "effects": { "medicine_xp": 20, "soldier_reputation": 5 }
    },
    {
      "id": "dec_report_casualties",
      "title": "Report to Sergeant",
      "valid_contexts": ["PostBattle"],
      "max_hours_since_battle": 12,
      "effects": { "officer_reputation": 3 }
    }
  ]
}
```

### PostBattle Event Rarity

**IMPORTANT:** PostBattle context (0-24 hours) should have **rare** events.
Player needs time for something to actually happen.

```csharp
// In prompt selection logic
float promptChance = context switch
{
    CampaignContext.PostBattle => 0.08f,      // 8% per phase - rare!
    CampaignContext.RecentEngagement => 0.12f, // 12% per phase
    CampaignContext.NormalCampaign => 0.15f,   // 15% per phase (standard)
    CampaignContext.Garrison => 0.10f,         // 10% per phase
    _ => 0.15f
};
```

With 4 phases per day at 8%, that's ~32% daily chance for a PostBattle event.
Gives player ~3 chances for something meaningful to happen in the 24-hour window.

### Testing

- [ ] Battle ends → Context switches to PostBattle
- [ ] PostBattle decisions appear in Camp Hub
- [ ] PostBattle decisions disappear after 24 hours
- [ ] Events filter to context-appropriate content
- [ ] PostBattle events are rare (player has time)
- [ ] Activity level reflects recent battle history
- [ ] Context persists across save/load
- [ ] Card games don't fire during PostBattle

---

## Data Structures

### OrderPrompt Model

```csharp
// src/Features/Orders/Models/OrderPrompt.cs
public class OrderPrompt
{
    public string Title { get; set; }
    public string Description { get; set; }
    public string InvestigateText { get; set; }
    public string IgnoreText { get; set; }
    public List<string> Contexts { get; set; } = new(); // "land", "sea", "any"
}
```

### PromptOutcome Enum

```csharp
// src/Features/Orders/Models/PromptOutcome.cs
public enum PromptOutcome
{
    Nothing,       // 50% - false alarm, waste of time
    SmallReward,   // 30% - gold, supplies, reputation
    EventChain,    // 15% - trigger multi-phase event
    Danger         // 5%  - ambush, injury, combat
}
```

### PromptCatalog Class

```csharp
// src/Features/Orders/PromptCatalog.cs
public static class PromptCatalog
{
    private const string LogCategory = "PromptCatalog";
    private static Dictionary<string, List<OrderPrompt>> _promptsByOrderType;
    private static Dictionary<PromptOutcome, List<PromptOutcomeData>> _outcomes;
    
    public static void LoadPrompts()
    {
        try
        {
            // Load from ModuleData/Enlisted/Prompts/order_prompts.json
            // Parse JSON and populate _promptsByOrderType
            ModLogger.Info(LogCategory, "Loaded prompt templates");
        }
        catch (Exception ex)
        {
            ModLogger.Error(LogCategory, "Failed to load prompt templates", ex);
        }
    }
    
    public static List<OrderPrompt> GetPromptsForOrderType(string orderType)
    {
        if (_promptsByOrderType == null)
        {
            ModLogger.Warn(LogCategory, "Prompts not loaded yet");
            return new List<OrderPrompt>();
        }
        
        return _promptsByOrderType.TryGetValue(orderType, out var prompts) 
            ? prompts 
            : new List<OrderPrompt>();
    }
    
    public static PromptOutcomeData GetOutcomeData(PromptOutcome outcome)
    {
        // Select random outcome from pool for this outcome type
        var pool = _outcomes[outcome];
        return pool[MBRandom.RandomInt(pool.Count)];
    }
}
```

### PromptOutcomeData Model

```csharp
// src/Features/Orders/Models/PromptOutcomeData.cs
public class PromptOutcomeData
{
    public string Text { get; set; }
    public Dictionary<string, object> Effects { get; set; } = new();
    public string ChainId { get; set; } // For EventChain outcomes
    public string TriggerEvent { get; set; } // Event ID to fire
}
```

### Integration with Existing Systems

**IsPartyAtSea() - Use Existing Code:**
```csharp
private bool IsPartyAtSea()
{
    // Reuse logic from OrderManager.cs lines 118-121
    var enlistment = EnlistmentBehavior.Instance;
    var party = enlistment?.CurrentLord?.PartyBelongedTo;
    return party != null && 
           party.CurrentSettlement == null && 
           party.BesiegedSettlement == null && 
           party.IsCurrentlyAtSea;
}
```

**FireEventChain() - Connect to Event System:**
```csharp
private void FireEventChain(Order order, PromptOutcome outcome)
{
    const string LogCategory = "OrderPrompts";
    
    try
    {
        var outcomeData = PromptCatalog.GetOutcomeData(outcome);
        
        if (outcomeData == null)
        {
            ModLogger.Warn(LogCategory, $"No outcome data for {outcome}");
            return;
        }
        
        ModLogger.Info(LogCategory, $"Prompt outcome: {outcome} during order {order.Id}");
        
        // Show outcome text
        if (!string.IsNullOrEmpty(outcomeData.Text))
        {
            InformationManager.DisplayMessage(new InformationMessage(outcomeData.Text, Colors.Yellow));
        }
        
        // Apply effects
        ApplyPromptEffects(outcomeData.Effects);
        
        // Trigger event chain if specified
        if (!string.IsNullOrEmpty(outcomeData.TriggerEvent))
        {
            var evt = EventCatalog.GetEvent(outcomeData.TriggerEvent);
            if (evt != null)
            {
                EventDeliveryManager.Instance?.QueueEvent(evt);
                ModLogger.Debug(LogCategory, $"Queued event chain: {outcomeData.TriggerEvent}");
            }
            else
            {
                ModLogger.Warn(LogCategory, $"Event not found: {outcomeData.TriggerEvent}");
            }
        }
    }
    catch (Exception ex)
    {
        ModLogger.Error(LogCategory, "Failed to fire event chain", ex);
    }
}

private void ApplyPromptEffects(Dictionary<string, object> effects)
{
    foreach (var effect in effects)
    {
        switch (effect.Key)
        {
            case "gold":
                // Parse range "25-75" or single value
                var gold = ParseGoldRange(effect.Value.ToString());
                Hero.MainHero.ChangeHeroGold(gold);
                break;
                
            case "officer_reputation":
            case "soldier_reputation":
            case "lord_reputation":
                var rep = int.Parse(effect.Value.ToString());
                EscalationManager.Instance.ModifyReputation(effect.Key, rep);
                break;
                
            case "company_supplies":
                var supplies = int.Parse(effect.Value.ToString());
                EnlistmentBehavior.Instance?.CompanyNeeds?.SetNeed(CompanyNeed.Supplies, supplies);
                break;
                
            case "injury":
                InjurySystem.ApplyInjury(effect.Value.ToString(), "prompt_outcome");
                break;
                
            case "trigger_combat":
                // Queue combat encounter via MapIncidentManager
                var encounterType = effect.Value.ToString();
                MapIncidentManager.Instance?.TriggerCombatEncounter(encounterType);
                break;
        }
    }
}
```

### File Structure

```
ModuleData/Enlisted/
├── Prompts/
│   ├── order_prompts.json          (NEW - prompt templates)
│   └── prompt_outcomes.json        (NEW - outcome pools)
├── Chains/
│   └── event_chains.json           (NEW - event chain definitions)
├── Orders/
│   ├── orders_t1_t3.json          (EXISTING - keep)
│   └── orders_t4_t6.json          (EXISTING - keep)
└── Events/
    ├── narrative-events.json       (EXISTING - convert to chains)
    └── escalation-events.json      (EXISTING - keep)
```

### Classes to Create

| Class | File Path | Purpose | Add to .csproj |
|-------|-----------|---------|----------------|
| `OrderPrompt` | `src/Features/Orders/Models/OrderPrompt.cs` | Prompt data model | ✅ |
| `PromptOutcome` | `src/Features/Orders/Models/PromptOutcome.cs` | Outcome enum | ✅ |
| `PromptOutcomeData` | `src/Features/Orders/Models/PromptOutcomeData.cs` | Outcome effects | ✅ |
| `PromptCatalog` | `src/Features/Orders/PromptCatalog.cs` | Load/manage prompts | ✅ |
| `EventChainManager` | `src/Features/Content/EventChainManager.cs` | Manage chain state | ✅ |

**IMPORTANT:** After creating files, manually add to `Enlisted.csproj`:
```xml
<Compile Include="src\Features\Orders\Models\OrderPrompt.cs"/>
<Compile Include="src\Features\Orders\Models\PromptOutcome.cs"/>
<Compile Include="src\Features\Orders\Models\PromptOutcomeData.cs"/>
<Compile Include="src\Features\Orders\PromptCatalog.cs"/>
<Compile Include="src\Features\Content\EventChainManager.cs"/>
```

### Save System Registration

**If any new classes need persistence, register in `EnlistedSaveDefiner`:**
```csharp
// Example:
AddClassDefinition(typeof(EventChainState), 1234);
AddEnumDefinition(typeof(PromptOutcome), 5678);
```

---

**End of Order Prompt Model Specification**
