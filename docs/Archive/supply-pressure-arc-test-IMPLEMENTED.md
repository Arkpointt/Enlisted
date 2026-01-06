# Supply Pressure Arc - Implementation Test

**Summary:** Minimal test implementation of the supply pressure arc system. Creates 9 tier-variant events (3 stages × 3 tiers) to validate the implementation workflow before tackling the full 35+ event plan.

**Status:** ✅ Implemented

**Last Updated:** 2026-01-06

**Related Docs:** 
- [Full Plan: Reputation-Morale-Supply Integration](./reputation-morale-supply-integration-v2.md)
- [Game Design Principles](../../Tools/CrewAI/knowledge/game-design-principles.md)

---

## Scope

This is a **test subset** of the full reputation-morale-supply integration plan. It implements ONLY the supply pressure arc to validate the workflow.

**In Scope:**
- `CheckPressureArcEvents()` method in CompanySimulationBehavior.cs
- 9 supply pressure events (3 stages × 3 tiers)
- Basic event delivery integration

**Out of Scope (Full Plan):**
- Morale/rest pressure arcs
- Positive arcs
- Gradient fitness modifiers
- Reputation-needs feedback loops
- Synergy effects

---

## C# Implementation

### File: `src/Features/Camp/CompanySimulationBehavior.cs`

Add method after existing pressure tracking:

```csharp
/// <summary>
/// Checks pressure counters and fires narrative arc events at thresholds.
/// Called from OnDailyTick after UpdatePressureTracking.
/// </summary>
private void CheckPressureArcEvents()
{
    if (_companyPressure == null) return;
    
    // Supply pressure arc
    CheckSupplyPressureArc(_companyPressure.DaysLowSupplies);
}

private void CheckSupplyPressureArc(int daysLow)
{
    // Only fire at exact thresholds (avoid duplicates)
    string eventId = null;
    
    switch (daysLow)
    {
        case 3:
            eventId = GetTierVariantEventId("supply_pressure_stage_1");
            break;
        case 5:
            eventId = GetTierVariantEventId("supply_pressure_stage_2");
            break;
        case 7:
            eventId = GetTierVariantEventId("supply_crisis");
            break;
    }
    
    if (eventId != null)
    {
        EventDeliveryManager.Instance?.QueueEvent(eventId);
        ModLogger.Info("CompanySimulation", $"Fired supply pressure event: {eventId}");
    }
}

/// <summary>
/// Returns tier-appropriate event ID suffix based on player tier.
/// </summary>
private string GetTierVariantEventId(string baseId)
{
    var tier = EnlistmentBehavior.Instance?.CurrentTier ?? 1;
    
    if (tier <= 4)
        return $"{baseId}_grunt";
    else if (tier <= 6)
        return $"{baseId}_nco";
    else
        return $"{baseId}_cmd";
}
```

### Integration Point

In `OnDailyTick()`, add call after `UpdatePressureTracking()`:

```csharp
// Existing code
UpdatePressureTracking();

// NEW: Check for pressure arc events
CheckPressureArcEvents();
```

---

## JSON Content

### File: `ModuleData/Enlisted/Events/pressure_arc_events.json`

Create new file with 9 events:

```json
{
  "schemaVersion": 2,
  "category": "event",
  "events": [
    
    // ========================================
    // SUPPLY PRESSURE - STAGE 1 (Day 3)
    // ========================================
    
    {
      "id": "supply_pressure_stage_1_grunt",
      "category": "event",
      "severity": "normal",
      "titleId": "supply_pressure_stage_1_grunt_title",
      "title": "Thin Rations",
      "setupId": "supply_pressure_stage_1_grunt_setup",
      "setup": "The cook's portions have shrunk. You notice men eyeing each other's bowls, calculating. Nobody says it aloud, but everyone knows: supplies are running low.",
      "requirements": {
        "tier": { "max": 4 }
      },
      "options": [
        {
          "id": "supply_pressure_stage_1_grunt_opt1",
          "textId": "supply_pressure_stage_1_grunt_opt1_text",
          "text": "Eat quietly and conserve energy",
          "tooltipId": "supply_pressure_stage_1_grunt_opt1_tooltip",
          "tooltip": "Accept the situation. No effect.",
          "effects": {}
        },
        {
          "id": "supply_pressure_stage_1_grunt_opt2",
          "textId": "supply_pressure_stage_1_grunt_opt2_text",
          "text": "Share a portion with a weaker soldier",
          "tooltipId": "supply_pressure_stage_1_grunt_opt2_tooltip",
          "tooltip": "+3 Soldier Rep. -5 HP (hunger).",
          "effects": {
            "soldierRep": 3,
            "hpChange": -5
          }
        },
        {
          "id": "supply_pressure_stage_1_grunt_opt3",
          "textId": "supply_pressure_stage_1_grunt_opt3_text",
          "text": "Try to get extra from the cook",
          "tooltipId": "supply_pressure_stage_1_grunt_opt3_tooltip",
          "tooltip": "-2 Soldier Rep. Others notice greed.",
          "effects": {
            "soldierRep": -2
          }
        }
      ]
    },
    
    {
      "id": "supply_pressure_stage_1_nco",
      "category": "event",
      "severity": "normal",
      "titleId": "supply_pressure_stage_1_nco_title",
      "title": "Your Squad Grumbles",
      "setupId": "supply_pressure_stage_1_nco_setup",
      "setup": "Your squad clusters around the cook fire, muttering. The portions are visibly smaller than last week. They look to you—their NCO—for answers you don't have.",
      "requirements": {
        "tier": { "min": 5, "max": 6 }
      },
      "options": [
        {
          "id": "supply_pressure_stage_1_nco_opt1",
          "textId": "supply_pressure_stage_1_nco_opt1_text",
          "text": "\"It's temporary. We've been through worse.\"",
          "tooltipId": "supply_pressure_stage_1_nco_opt1_tooltip",
          "tooltip": "+2 Officer Rep. Steady leadership.",
          "effects": {
            "officerRep": 2
          }
        },
        {
          "id": "supply_pressure_stage_1_nco_opt2",
          "textId": "supply_pressure_stage_1_nco_opt2_text",
          "text": "Share your own ration with the squad",
          "tooltipId": "supply_pressure_stage_1_nco_opt2_tooltip",
          "tooltip": "+5 Soldier Rep. -10 HP (hunger).",
          "effects": {
            "soldierRep": 5,
            "hpChange": -10
          }
        },
        {
          "id": "supply_pressure_stage_1_nco_opt3",
          "textId": "supply_pressure_stage_1_nco_opt3_text",
          "text": "Assign extra foraging duty to the squad",
          "tooltipId": "supply_pressure_stage_1_nco_opt3_tooltip",
          "tooltip": "-1 Soldier Rep. +1 Fatigue. May help supplies.",
          "effects": {
            "soldierRep": -1,
            "fatigueChange": 1
          }
        }
      ]
    },
    
    {
      "id": "supply_pressure_stage_1_cmd",
      "category": "event",
      "severity": "normal",
      "titleId": "supply_pressure_stage_1_cmd_title",
      "title": "Supply Officer's Report",
      "setupId": "supply_pressure_stage_1_cmd_setup",
      "setup": "Your supply officer approaches with a grim expression. \"Three days at current consumption, Captain. Maybe four if we cut portions again. The men are already noticing.\"",
      "requirements": {
        "tier": { "min": 7 }
      },
      "options": [
        {
          "id": "supply_pressure_stage_1_cmd_opt1",
          "textId": "supply_pressure_stage_1_cmd_opt1_text",
          "text": "Implement rationing across the retinue",
          "tooltipId": "supply_pressure_stage_1_cmd_opt1_tooltip",
          "tooltip": "-3 Morale. Extends supply duration.",
          "effects": {
            "companyNeeds": { "Morale": -3 }
          }
        },
        {
          "id": "supply_pressure_stage_1_cmd_opt2",
          "textId": "supply_pressure_stage_1_cmd_opt2_text",
          "text": "Send foraging parties immediately",
          "tooltipId": "supply_pressure_stage_1_cmd_opt2_tooltip",
          "tooltip": "-2 Readiness. Chance of supply improvement.",
          "effects": {
            "companyNeeds": { "Readiness": -2 }
          }
        },
        {
          "id": "supply_pressure_stage_1_cmd_opt3",
          "textId": "supply_pressure_stage_1_cmd_opt3_text",
          "text": "Request resupply from the lord",
          "tooltipId": "supply_pressure_stage_1_cmd_opt3_tooltip",
          "tooltip": "+1 Scrutiny. Lord may provide supplies.",
          "effects": {
            "scrutiny": 1
          }
        }
      ]
    },
    
    // ========================================
    // SUPPLY PRESSURE - STAGE 2 (Day 5)
    // ========================================
    
    {
      "id": "supply_pressure_stage_2_grunt",
      "category": "event",
      "severity": "major",
      "titleId": "supply_pressure_stage_2_grunt_title",
      "title": "Fight at the Cook Fire",
      "setupId": "supply_pressure_stage_2_grunt_setup",
      "setup": "A scuffle erupts near the cook's station. Two soldiers wrestle over a bread crust, snarling like dogs. Others gather to watch, eyes hollow with hunger.",
      "requirements": {
        "tier": { "max": 4 }
      },
      "options": [
        {
          "id": "supply_pressure_stage_2_grunt_opt1",
          "textId": "supply_pressure_stage_2_grunt_opt1_text",
          "text": "Step in and break it up",
          "tooltipId": "supply_pressure_stage_2_grunt_opt1_tooltip",
          "tooltip": "+2 Soldier Rep. Risk of injury.",
          "effects": {
            "soldierRep": 2,
            "hpChange": -5
          }
        },
        {
          "id": "supply_pressure_stage_2_grunt_opt2",
          "textId": "supply_pressure_stage_2_grunt_opt2_text",
          "text": "Watch and stay out of it",
          "tooltipId": "supply_pressure_stage_2_grunt_opt2_tooltip",
          "tooltip": "No effect. Smart survival.",
          "effects": {}
        },
        {
          "id": "supply_pressure_stage_2_grunt_opt3",
          "textId": "supply_pressure_stage_2_grunt_opt3_text",
          "text": "Get the sergeant",
          "tooltipId": "supply_pressure_stage_2_grunt_opt3_tooltip",
          "tooltip": "+1 Officer Rep. Proper chain of command.",
          "effects": {
            "officerRep": 1
          }
        }
      ]
    },
    
    {
      "id": "supply_pressure_stage_2_nco",
      "category": "event",
      "severity": "major",
      "titleId": "supply_pressure_stage_2_nco_title",
      "title": "Your Men Are Fighting",
      "setupId": "supply_pressure_stage_2_nco_setup",
      "setup": "Two of YOUR soldiers are grappling in the dirt, cursing over scraps. The rest of your squad forms a ring, watching. Waiting to see how their NCO handles this.",
      "requirements": {
        "tier": { "min": 5, "max": 6 }
      },
      "options": [
        {
          "id": "supply_pressure_stage_2_nco_opt1",
          "textId": "supply_pressure_stage_2_nco_opt1_text",
          "text": "Punish both harshly",
          "tooltipId": "supply_pressure_stage_2_nco_opt1_tooltip",
          "tooltip": "+2 Discipline. -3 Soldier Rep. Order restored.",
          "effects": {
            "discipline": 2,
            "soldierRep": -3
          }
        },
        {
          "id": "supply_pressure_stage_2_nco_opt2",
          "textId": "supply_pressure_stage_2_nco_opt2_text",
          "text": "Mediate and split the ration fairly",
          "tooltipId": "supply_pressure_stage_2_nco_opt2_tooltip",
          "tooltip": "+3 Soldier Rep. +1 Officer Rep. Fair leader.",
          "effects": {
            "soldierRep": 3,
            "officerRep": 1
          }
        },
        {
          "id": "supply_pressure_stage_2_nco_opt3",
          "textId": "supply_pressure_stage_2_nco_opt3_text",
          "text": "Share your own ration to end the fight",
          "tooltipId": "supply_pressure_stage_2_nco_opt3_tooltip",
          "tooltip": "+5 Soldier Rep. -15 HP. Sacrifice noted.",
          "effects": {
            "soldierRep": 5,
            "hpChange": -15
          }
        }
      ]
    },
    
    {
      "id": "supply_pressure_stage_2_cmd",
      "category": "event",
      "severity": "major",
      "titleId": "supply_pressure_stage_2_cmd_title",
      "title": "Discipline Breaking Down",
      "setupId": "supply_pressure_stage_2_cmd_setup",
      "setup": "Multiple fights have broken out across your retinue. Your sergeants report men hoarding scraps, stealing from each other. The rot is spreading fast.",
      "requirements": {
        "tier": { "min": 7 }
      },
      "options": [
        {
          "id": "supply_pressure_stage_2_cmd_opt1",
          "textId": "supply_pressure_stage_2_cmd_opt1_text",
          "text": "Public punishment for the worst offenders",
          "tooltipId": "supply_pressure_stage_2_cmd_opt1_tooltip",
          "tooltip": "+3 Discipline. -5 Morale. Example made.",
          "effects": {
            "discipline": 3,
            "companyNeeds": { "Morale": -5 }
          }
        },
        {
          "id": "supply_pressure_stage_2_cmd_opt2",
          "textId": "supply_pressure_stage_2_cmd_opt2_text",
          "text": "Open your personal stores to the men",
          "tooltipId": "supply_pressure_stage_2_cmd_opt2_tooltip",
          "tooltip": "-50 Gold. +5 Morale. +3 Lord Rep.",
          "effects": {
            "gold": -50,
            "companyNeeds": { "Morale": 5 },
            "lordRep": 3
          }
        },
        {
          "id": "supply_pressure_stage_2_cmd_opt3",
          "textId": "supply_pressure_stage_2_cmd_opt3_text",
          "text": "Demand the lord provide resupply",
          "tooltipId": "supply_pressure_stage_2_cmd_opt3_tooltip",
          "tooltip": "+2 Scrutiny. -2 Lord Rep. Forceful request.",
          "effects": {
            "scrutiny": 2,
            "lordRep": -2
          }
        }
      ]
    },
    
    // ========================================
    // SUPPLY CRISIS (Day 7)
    // ========================================
    
    {
      "id": "supply_crisis_grunt",
      "category": "event",
      "severity": "critical",
      "titleId": "supply_crisis_grunt_title",
      "title": "Whispers of Desertion",
      "setupId": "supply_crisis_grunt_setup",
      "setup": "A week of hunger. In the dark before dawn, you overhear voices: \"Three more days of this and I'm gone.\" \"There's a village two leagues south...\" They haven't seen you listening.",
      "requirements": {
        "tier": { "max": 4 }
      },
      "options": [
        {
          "id": "supply_crisis_grunt_opt1",
          "textId": "supply_crisis_grunt_opt1_text",
          "text": "Report them to the sergeant",
          "tooltipId": "supply_crisis_grunt_opt1_tooltip",
          "tooltip": "+3 Officer Rep. +2 Discipline. They'll hate you.",
          "effects": {
            "officerRep": 3,
            "discipline": 2,
            "soldierRep": -5
          }
        },
        {
          "id": "supply_crisis_grunt_opt2",
          "textId": "supply_crisis_grunt_opt2_text",
          "text": "Stay silent about what you heard",
          "tooltipId": "supply_crisis_grunt_opt2_tooltip",
          "tooltip": "No effect. You said nothing.",
          "effects": {}
        },
        {
          "id": "supply_crisis_grunt_opt3",
          "textId": "supply_crisis_grunt_opt3_text",
          "text": "Talk them out of it quietly",
          "tooltipId": "supply_crisis_grunt_opt3_tooltip",
          "tooltip": "+5 Soldier Rep. Loyalty to comrades.",
          "effects": {
            "soldierRep": 5
          }
        }
      ]
    },
    
    {
      "id": "supply_crisis_nco",
      "category": "event",
      "severity": "critical",
      "titleId": "supply_crisis_nco_title",
      "title": "One of Yours Is Leaving",
      "setupId": "supply_crisis_nco_setup",
      "setup": "Private Aldric—one of your best—is packing his kit in the dark. He freezes when he sees you. \"I'm sorry, Sergeant. I have a family. I can't die here for nothing.\"",
      "requirements": {
        "tier": { "min": 5, "max": 6 }
      },
      "options": [
        {
          "id": "supply_crisis_nco_opt1",
          "textId": "supply_crisis_nco_opt1_text",
          "text": "Arrest him for desertion",
          "tooltipId": "supply_crisis_nco_opt1_tooltip",
          "tooltip": "+3 Discipline. +2 Officer Rep. -5 Soldier Rep.",
          "effects": {
            "discipline": 3,
            "officerRep": 2,
            "soldierRep": -5
          }
        },
        {
          "id": "supply_crisis_nco_opt2",
          "textId": "supply_crisis_nco_opt2_text",
          "text": "Let him go. Pretend you saw nothing.",
          "tooltipId": "supply_crisis_nco_opt2_tooltip",
          "tooltip": "+3 Soldier Rep. -3 Officer Rep. Mercy shown.",
          "effects": {
            "soldierRep": 3,
            "officerRep": -3
          }
        },
        {
          "id": "supply_crisis_nco_opt3",
          "textId": "supply_crisis_nco_opt3_text",
          "text": "Give him your wages to send home",
          "tooltipId": "supply_crisis_nco_opt3_tooltip",
          "tooltip": "-30 Gold. +5 Soldier Rep. He stays.",
          "effects": {
            "gold": -30,
            "soldierRep": 5
          }
        }
      ]
    },
    
    {
      "id": "supply_crisis_cmd",
      "category": "event",
      "severity": "critical",
      "titleId": "supply_crisis_cmd_title",
      "title": "Desertions Imminent",
      "setupId": "supply_crisis_cmd_setup",
      "setup": "Your sergeant-major reports in low tones: \"We've lost six already. Slipped away in the night. More will follow unless something changes. The men are watching you, Captain.\"",
      "requirements": {
        "tier": { "min": 7 }
      },
      "options": [
        {
          "id": "supply_crisis_cmd_opt1",
          "textId": "supply_crisis_cmd_opt1_text",
          "text": "Execute a captured deserter as example",
          "tooltipId": "supply_crisis_cmd_opt1_tooltip",
          "tooltip": "+5 Discipline. -10 Morale. -5 Soldier Rep.",
          "effects": {
            "discipline": 5,
            "companyNeeds": { "Morale": -10 },
            "soldierRep": -5
          }
        },
        {
          "id": "supply_crisis_cmd_opt2",
          "textId": "supply_crisis_cmd_opt2_text",
          "text": "Address the company. Promise better days.",
          "tooltipId": "supply_crisis_cmd_opt2_tooltip",
          "tooltip": "+3 Morale. Leadership skill check.",
          "effects": {
            "companyNeeds": { "Morale": 3 },
            "skillXp": { "Leadership": 50 }
          }
        },
        {
          "id": "supply_crisis_cmd_opt3",
          "textId": "supply_crisis_cmd_opt3_text",
          "text": "Petition the lord directly for emergency resupply",
          "tooltipId": "supply_crisis_cmd_opt3_tooltip",
          "tooltip": "+3 Scrutiny. -3 Lord Rep. May solve the crisis.",
          "effects": {
            "scrutiny": 3,
            "lordRep": -3
          }
        },
        {
          "id": "supply_crisis_cmd_opt4",
          "textId": "supply_crisis_cmd_opt4_text",
          "text": "Spend personal funds on emergency supplies",
          "tooltipId": "supply_crisis_cmd_opt4_tooltip",
          "tooltip": "-100 Gold. +10 Supplies. +5 Lord Rep.",
          "effects": {
            "gold": -100,
            "companyNeeds": { "Supplies": 10 },
            "lordRep": 5
          }
        }
      ]
    }
  ]
}
```

---

## Localization

### File: `ModuleData/Languages/enlisted_strings.xml`

Add string entries for all 9 events (titles, setups, option texts, tooltips).

**Note:** The sync_event_strings.py tool will generate these automatically from the JSON.

---

## Implementation Checklist

### C# Tasks
- [ ] Add `CheckPressureArcEvents()` method to CompanySimulationBehavior.cs
- [ ] Add `CheckSupplyPressureArc()` helper method
- [ ] Add `GetTierVariantEventId()` helper method
- [ ] Add call to `CheckPressureArcEvents()` in `OnDailyTick()`
- [ ] Verify EventDeliveryManager.QueueEvent() accepts these IDs
- [ ] Build succeeds with no errors

### JSON Tasks
- [ ] Create `ModuleData/Enlisted/Events/pressure_arc_events.json`
- [ ] Add all 9 events with correct tier requirements
- [ ] Run `validate_content.py` - no errors
- [ ] Run `sync_event_strings.py` - localization synced

### Testing
- [ ] Start game at T2 with low supplies → verify grunt events fire
- [ ] Start game at T5 with low supplies → verify NCO events fire
- [ ] Start game at T8 with low supplies → verify commander events fire
- [ ] Verify events fire at exactly Day 3, 5, 7 (no duplicates)
- [ ] Verify event effects apply correctly

---

## Success Criteria

- [ ] C# code compiles without errors
- [ ] JSON validates without errors
- [ ] Events fire at correct thresholds (Day 3, 5, 7)
- [ ] Correct tier variant selected based on player tier
- [ ] Effects apply correctly (reputation, HP, gold, etc.)
- [ ] Player can make meaningful choices at each tier level

---

## After This Test

If successful, proceed with the full plan:
1. Add morale and rest pressure arcs (18 more events)
2. Add positive arcs (9+ events)
3. Implement gradient fitness modifiers
4. Implement reputation-needs feedback loops

**Full Plan:** [reputation-morale-supply-integration-v2.md](./reputation-morale-supply-integration-v2.md)

---

**End of Test Plan**
