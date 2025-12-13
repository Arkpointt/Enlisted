# Phase 4.5 — Duty System Alignment

**Status:** ✅ **COMPLETED** (December 2024)

**Priority:** Must complete before Phase 5 (Content Conversion)
**Dependencies:** Existing `duties_system.json`, `EnlistedDutiesBehavior.cs`
**Blocks:** Phase 5 content conversion (50 duty events won't fire if duties don't match)

---

## Problem Statement

The Lance Life content library has **50 duty events** written for **10 duties**. The current `duties_system.json` implementation has **11 duties** with a different set. If we convert the content without aligning, events will reference duties that don't exist (and vice versa).

### Current State

**Content Library (lance_life_events_content_library.md):**
| Duty | Events | Status |
|------|--------|--------|
| Quartermaster | 5 | ✅ Written |
| Scout | 5 | ✅ Written |
| Field Medic | 5 | ✅ Written |
| Messenger | 5 | ✅ Written |
| Armorer | 5 | ✅ Written |
| Runner | 5 | ✅ Written |
| Lookout | 5 | ✅ Written |
| Engineer | 5 | ✅ Written |
| Boatswain | 5 | ✅ Written (Naval) |
| Navigator | 5 | ✅ Written (Naval) |

**Current duties_system.json:**
| Formation | Duties |
|-----------|--------|
| Infantry | Runner, Quartermaster, Field Medic, Armorer |
| Archer | Scout, Marksman, Lookout |
| Cavalry | Messenger, Pathfinder, Shock Trooper |
| Horse Archer | Scout, Messenger, Skirmisher |

**Mismatch:**
| Status | Duties |
|--------|--------|
| ✅ In both | Quartermaster, Scout, Field Medic, Messenger, Armorer, Runner, Lookout |
| ❌ Events exist, not in system | Engineer, Boatswain, Navigator |
| ❌ In system, no events | Marksman, Pathfinder, Shock Trooper, Skirmisher |

---

## Solution: Align to 10 Canonical Duties

### Canonical Duty List

| ID | Display Name | Formations | Expansion | Events |
|----|--------------|------------|-----------|--------|
| `quartermaster` | Quartermaster | Infantry | Base | 5 |
| `field_medic` | Field Medic | Infantry | Base | 5 |
| `armorer` | Armorer | Infantry | Base | 5 |
| `runner` | Runner | Infantry | Base | 5 |
| `engineer` | Engineer | Infantry | Base | 5 |
| `scout` | Scout | Archer, Cavalry, Horse Archer | Base | 5 |
| `lookout` | Lookout | Archer | Base | 5 |
| `messenger` | Messenger | Cavalry, Horse Archer | Base | 5 |
| `boatswain` | Boatswain | Naval | War Sails | 5 |
| `navigator` | Navigator | Naval | War Sails | 5 |

### Formation → Duty Mapping

| Formation | Available Duties |
|-----------|------------------|
| Infantry | Runner, Quartermaster, Field Medic, Armorer, Engineer |
| Archer | Scout, Lookout |
| Cavalry | Scout, Messenger |
| Horse Archer | Scout, Messenger |
| Naval | Boatswain, Navigator |

### Deferred Duties (No Events Yet)

These duties are removed from the current system. They can be added later with events:
- Marksman (Archer)
- Pathfinder (Cavalry)
- Shock Trooper (Cavalry)
- Skirmisher (Horse Archer)

---

## Implementation Tasks

### Task 1: Update duties_system.json

Replace the current duty configuration with the canonical list.

**File:** `ModuleData/Enlisted/duties_system.json`

```json
{
  "schema_version": "2.0",
  
  "duties": {
    "quartermaster": {
      "id": "quartermaster",
      "display_name": "Quartermaster",
      "description": "Manage supplies, inventory, and logistics for the unit.",
      "required_formations": ["infantry"],
      "tier_required": 1,
      "wage_multiplier": 1.4,
      "skill_xp_daily": {
        "Steward": 30,
        "Trade": 20
      },
      "event_prefix": "qm_",
      "enabled": true
    },
    
    "field_medic": {
      "id": "field_medic",
      "display_name": "Field Medic",
      "description": "Treat wounds and care for the sick and injured.",
      "required_formations": ["infantry"],
      "tier_required": 1,
      "wage_multiplier": 1.2,
      "skill_xp_daily": {
        "Medicine": 40,
        "Athletics": 10
      },
      "event_prefix": "med_",
      "enabled": true
    },
    
    "armorer": {
      "id": "armorer",
      "display_name": "Armorer",
      "description": "Maintain and repair weapons and armor.",
      "required_formations": ["infantry"],
      "tier_required": 1,
      "wage_multiplier": 1.3,
      "skill_xp_daily": {
        "Smithing": 35,
        "Engineering": 15
      },
      "event_prefix": "arm_",
      "enabled": true
    },
    
    "runner": {
      "id": "runner",
      "display_name": "Runner",
      "description": "Carry messages, supplies, and orders around camp and battlefield.",
      "required_formations": ["infantry"],
      "tier_required": 1,
      "wage_multiplier": 0.9,
      "skill_xp_daily": {
        "Athletics": 40,
        "Scouting": 10
      },
      "event_prefix": "run_",
      "enabled": true
    },
    
    "engineer": {
      "id": "engineer",
      "display_name": "Engineer",
      "description": "Build fortifications, siege works, and field repairs.",
      "required_formations": ["infantry"],
      "tier_required": 2,
      "wage_multiplier": 1.5,
      "skill_xp_daily": {
        "Engineering": 40,
        "Athletics": 10
      },
      "event_prefix": "eng_",
      "enabled": true
    },
    
    "scout": {
      "id": "scout",
      "display_name": "Scout",
      "description": "Reconnaissance, pathfinding, and enemy observation.",
      "required_formations": ["archer", "cavalry", "horsearcher"],
      "tier_required": 1,
      "wage_multiplier": 1.3,
      "skill_xp_daily": {
        "Scouting": 40,
        "Athletics": 10
      },
      "event_prefix": "sc_",
      "enabled": true
    },
    
    "lookout": {
      "id": "lookout",
      "display_name": "Lookout",
      "description": "Watch duty, signal fires, and camp security.",
      "required_formations": ["archer"],
      "tier_required": 1,
      "wage_multiplier": 1.0,
      "skill_xp_daily": {
        "Scouting": 25,
        "Bow": 15,
        "Athletics": 10
      },
      "event_prefix": "look_",
      "enabled": true
    },
    
    "messenger": {
      "id": "messenger",
      "display_name": "Messenger",
      "description": "Deliver urgent dispatches between units and commanders.",
      "required_formations": ["cavalry", "horsearcher"],
      "tier_required": 1,
      "wage_multiplier": 1.2,
      "skill_xp_daily": {
        "Riding": 30,
        "Athletics": 10,
        "Charm": 10
      },
      "event_prefix": "msg_",
      "enabled": true
    },
    
    "boatswain": {
      "id": "boatswain",
      "display_name": "Boatswain",
      "description": "Manage ship operations, crew, and deck work.",
      "required_formations": ["naval"],
      "tier_required": 1,
      "wage_multiplier": 1.3,
      "skill_xp_daily": {
        "Athletics": 30,
        "Leadership": 20
      },
      "event_prefix": "boat_",
      "enabled": true,
      "requires_expansion": "war_sails"
    },
    
    "navigator": {
      "id": "navigator",
      "display_name": "Navigator",
      "description": "Chart courses, read stars, and guide the ship.",
      "required_formations": ["naval"],
      "tier_required": 2,
      "wage_multiplier": 1.6,
      "skill_xp_daily": {
        "Scouting": 30,
        "Engineering": 20
      },
      "event_prefix": "nav_",
      "enabled": true,
      "requires_expansion": "war_sails"
    }
  },
  
  "formation_training": {
    "enabled": true,
    "formations": {
      "infantry": {
        "description": "Foot soldiers - ground combat and conditioning",
        "skills": {
          "Athletics": 50,
          "OneHanded": 50,
          "TwoHanded": 50,
          "Polearm": 50,
          "Throwing": 25
        }
      },
      "cavalry": {
        "description": "Mounted soldiers - horsemanship and mounted combat",
        "skills": {
          "Riding": 50,
          "OneHanded": 50,
          "Polearm": 50,
          "Athletics": 25,
          "TwoHanded": 25
        }
      },
      "archer": {
        "description": "Ranged specialists - marksmanship and positioning",
        "skills": {
          "Bow": 50,
          "Crossbow": 50,
          "Athletics": 50,
          "OneHanded": 25
        }
      },
      "horsearcher": {
        "description": "Mounted ranged - mobile harassment and skirmishing",
        "skills": {
          "Riding": 50,
          "Bow": 50,
          "Throwing": 50,
          "Athletics": 25,
          "OneHanded": 25
        }
      },
      "naval": {
        "description": "Ship-based combat - boarding and ship operations",
        "skills": {
          "Athletics": 50,
          "OneHanded": 50,
          "Throwing": 25,
          "Polearm": 25
        }
      }
    }
  },
  
  "duty_slots": {
    "tier_1": 1,
    "tier_2": 1,
    "tier_3": 2,
    "tier_4": 2,
    "tier_5": 2,
    "tier_6": 3
  }
}
```

---

### Task 2: Update EnlistedDutiesBehavior.cs

Modify the duty behavior to:

1. **Load new schema** with `requires_expansion` field
2. **Filter Naval duties** when War Sails is not detected
3. **Support `event_prefix`** field for event system integration
4. **Handle tier requirements** for duties like Engineer (T2+) and Navigator (T2+)

**Key Changes:**

```csharp
// Add expansion check
private bool IsDutyAvailable(DutyDefinition duty)
{
    // Check formation
    if (!duty.RequiredFormations.Contains(_playerFormation))
        return false;
    
    // Check tier
    if (_currentTier < duty.TierRequired)
        return false;
    
    // Check expansion requirement
    if (!string.IsNullOrEmpty(duty.RequiresExpansion))
    {
        if (duty.RequiresExpansion == "war_sails" && !IsWarSailsActive())
            return false;
    }
    
    return duty.Enabled;
}

// Expansion detection (example - adapt to actual detection method)
private bool IsWarSailsActive()
{
    // Check if Naval War DLC/expansion is loaded
    return ModuleHelper.GetModules().Any(m => 
        m.Name.Contains("NavalWar") || m.Name.Contains("WarSails"));
}
```

---

### Task 3: Add Duty Trigger Support to Event System

The trigger evaluator (Phase 2) needs to support `has_duty:{duty_id}` triggers.

**File:** Event trigger evaluator

```csharp
// In trigger evaluation
case string s when s.StartsWith("has_duty:"):
    var dutyId = s.Substring("has_duty:".Length);
    return EnlistedDutiesBehavior.Instance?.HasActiveDuty(dutyId) ?? false;
```

**Required API in EnlistedDutiesBehavior:**

```csharp
public bool HasActiveDuty(string dutyId)
{
    return _activeDuties.Any(d => d.Id == dutyId);
}

public IReadOnlyList<string> GetActiveDutyIds()
{
    return _activeDuties.Select(d => d.Id).ToList();
}
```

---

### Task 4: Verify Event Prefixes Match

Ensure the `event_prefix` in the duty config matches the event IDs in the content library.

| Duty | Config Prefix | Content Library IDs |
|------|---------------|---------------------|
| Quartermaster | `qm_` | `qm_supply_inventory`, `qm_merchant_negotiation`, etc. |
| Field Medic | `med_` | `med_treating_wounded`, `med_disease_outbreak`, etc. |
| Armorer | `arm_` | `arm_equipment_inspection`, `arm_battle_repairs`, etc. |
| Runner | `run_` | `run_supply_run`, `run_battle_runner`, etc. |
| Engineer | `eng_` | `eng_fortification_work`, `eng_bridge_assessment`, etc. |
| Scout | `sc_` | `sc_terrain_recon`, `sc_enemy_contact`, etc. |
| Lookout | `look_` | `look_night_watch`, `look_movement_spotted`, etc. |
| Messenger | `msg_` | `msg_urgent_dispatch`, `msg_enemy_lines`, etc. |
| Boatswain | `boat_` | `boat_deck_management`, `boat_storm_rigging`, etc. |
| Navigator | `nav_` | `nav_course_setting`, `nav_dead_reckoning`, etc. |

**Action:** During Phase 5 content conversion, ensure all duty event IDs use these prefixes.

---

### Task 5: Update Duty Selection Menu

The duty selection menu (`enlisted_duty_selection`) should:

1. Show only available duties for current formation
2. Gray out duties that require higher tier
3. Hide Naval duties if War Sails not active
4. Show "Events: X available" indicator per duty (optional, polish)

---

## Validation Checklist

Before proceeding to Phase 5, verify:

- [x] `duties_system.json` updated with 10 canonical duties
- [x] `EnlistedDutiesBehavior.cs` loads new schema correctly
- [x] Naval duties hidden when War Sails not detected
- [x] Tier requirements enforced (Engineer T2+, Navigator T2+)
- [x] `has_duty:{id}` trigger works in event system
- [x] Duty selection menu shows correct duties per formation
- [x] Formation training still works for all 5 formations
- [x] Save/load preserves duty assignments correctly
- [x] Deferred duties (Marksman, Pathfinder, etc.) are fully removed

---

## Testing Scenarios

### Scenario 1: Infantry Player
1. Enlist as Infantry
2. Verify available duties: Runner, Quartermaster, Field Medic, Armorer
3. Reach Tier 2
4. Verify Engineer now available
5. Assign Engineer duty
6. Verify `has_duty:engineer` trigger returns true

### Scenario 2: Cavalry Player
1. Enlist as Cavalry
2. Verify available duties: Scout, Messenger
3. Verify NO Infantry duties (Quartermaster, etc.)

### Scenario 3: Naval (War Sails)
1. Enable War Sails expansion
2. Enlist with Naval formation
3. Verify available duties: Boatswain, Navigator (T2+)

### Scenario 4: Naval (No War Sails)
1. Disable War Sails expansion
2. Verify Naval duties do not appear
3. Verify no errors in duty loading

---

## Schema Reference

### Duty Definition

```json
{
  "id": "string (unique identifier, used in triggers)",
  "display_name": "string (shown to player)",
  "description": "string (tooltip/description)",
  "required_formations": ["infantry", "cavalry", "archer", "horsearcher", "naval"],
  "tier_required": 1,
  "wage_multiplier": 1.0,
  "skill_xp_daily": {
    "SkillName": 0
  },
  "event_prefix": "string (prefix for related event IDs)",
  "enabled": true,
  "requires_expansion": "string | null"
}
```

### Trigger Conditions

| Trigger | Description |
|---------|-------------|
| `has_duty:quartermaster` | Player has Quartermaster duty active |
| `has_duty:scout` | Player has Scout duty active |
| `has_duty:field_medic` | Player has Field Medic duty active |
| `has_duty:messenger` | Player has Messenger duty active |
| `has_duty:armorer` | Player has Armorer duty active |
| `has_duty:runner` | Player has Runner duty active |
| `has_duty:lookout` | Player has Lookout duty active |
| `has_duty:engineer` | Player has Engineer duty active |
| `has_duty:boatswain` | Player has Boatswain duty active |
| `has_duty:navigator` | Player has Navigator duty active |

---

## Files Modified

| File | Changes |
|------|---------|
| `ModuleData/Enlisted/duties_system.json` | Replace with canonical 10 duties |
| `EnlistedDutiesBehavior.cs` | Add expansion check, tier check, event prefix support |
| Event trigger evaluator | Add `has_duty:{id}` support |
| Duty selection menu | Update filtering logic |

---

## Post-Completion

Once this phase is complete:

1. **Phase 5 can proceed** — All 50 duty events will have matching duties
2. **Event triggers will work** — `has_duty:X` triggers will fire correctly
3. **Naval content gated** — Boatswain/Navigator events only fire with War Sails

---

## Future Expansion (Not This Phase)

These can be added later with corresponding events:

| Duty | Formation | Priority |
|------|-----------|----------|
| Marksman | Archer | Medium |
| Pathfinder | Cavalry | Medium |
| Shock Trooper | Cavalry | Low |
| Skirmisher | Horse Archer | Low |

Each would need 5 events written to match the existing duty event pattern.

---

*Document Version: 1.0*
*Phase: 4.5 (Pre-Content Conversion)*
*Dependencies: Phase 0-4 complete*
*Blocks: Phase 5 (Content Conversion)*
