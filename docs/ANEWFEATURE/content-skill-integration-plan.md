# Content Skill Integration Plan

**Summary:** Strategic plan for integrating Bannerlord's attribute/skill system into all Enlisted content. Maps current content coverage, proposes thematic aliases, and provides an action plan for improving immersion and balance across events, decisions, opportunities, routines, and map incidents.

**Status:** 📋 Planning  
**Last Updated:** 2026-01-03  
**Related Docs:** [Native Skill XP](native-skill-xp.md), [Content Effects Reference](content-effects-reference.md), [Content Index](../Features/Content/content-index.md)

---

## Vision

**Goal:** Make content authoring more immersive by using thematic skill terms while ensuring balanced attribute coverage across all content systems.

**Benefits:**
1. **Immersive authoring** - Write `"Perception"` instead of `"Scouting"` in narrative content
2. **Attribute awareness** - Track which attributes content benefits for balance
3. **Consistent mapping** - Single source of truth for skill aliases
4. **Analytics** - Analyze content coverage by attribute to find gaps

---

## Bannerlord Attribute/Skill Structure

```
┌─────────────────────────────────────────────────────────────────────┐
│                         6 ATTRIBUTES                                │
├───────────────┬───────────────┬───────────────┬────────────────────┤
│    VIGOR      │   CONTROL     │  ENDURANCE    │     CUNNING        │
│  (melee)      │   (ranged)    │  (physical)   │    (mental)        │
├───────────────┼───────────────┼───────────────┼────────────────────┤
│ • OneHanded   │ • Bow         │ • Riding      │ • Scouting         │
│ • TwoHanded   │ • Crossbow    │ • Athletics   │ • Tactics          │
│ • Polearm     │ • Throwing    │ • Crafting    │ • Roguery          │
├───────────────┼───────────────┼───────────────┼────────────────────┤
│    SOCIAL     │ INTELLIGENCE  │               │                    │
│  (people)     │  (knowledge)  │               │                    │
├───────────────┼───────────────┤               │                    │
│ • Charm       │ • Steward     │               │                    │
│ • Leadership  │ • Medicine    │               │                    │
│ • Trade       │ • Engineering │               │                    │
└───────────────┴───────────────┴───────────────┴────────────────────┘
```

---

## Proposed Thematic Aliases

### Current Aliases (Implemented)

| Alias | → Skill | Attribute | Used In |
|-------|---------|-----------|---------|
| `Perception` | Scouting | Cunning | Skill checks |
| `Smithing` | Crafting | Endurance | Skill XP, checks |

### Proposed New Aliases

| Alias | → Skill | Attribute | Thematic Use |
|-------|---------|-----------|--------------|
| **Vigor Aliases** |
| `Swordplay` | OneHanded | Vigor | Dueling, sword drills |
| `MeleeCombat` | OneHanded | Vigor | Generic melee |
| `HeavyWeapons` | TwoHanded | Vigor | Axe/mace training |
| `PikeWork` | Polearm | Vigor | Formation, pike drills |
| **Control Aliases** |
| `Archery` | Bow | Control | Bow practice, hunting |
| `Marksmanship` | Crossbow | Control | Precision shooting |
| `JavelinWork` | Throwing | Control | Javelin practice |
| **Endurance Aliases** |
| `Horsemanship` | Riding | Endurance | Cavalry, mounted patrol |
| `Endurance` | Athletics | Endurance | Marching, labor |
| `PhysicalLabor` | Athletics | Endurance | Work details |
| `Repair` | Crafting | Endurance | Equipment maintenance |
| **Cunning Aliases** |
| `Awareness` | Scouting | Cunning | Ambush detection |
| `Vigilance` | Scouting | Cunning | Guard duty, sentry |
| `BattlePlanning` | Tactics | Cunning | Strategy, formations |
| `Stealth` | Roguery | Cunning | Night ops, sneaking |
| `Cunning` | Roguery | Cunning | Deception, tricks |
| **Social Aliases** |
| `Persuasion` | Charm | Social | Negotiation, convincing |
| `Diplomacy` | Charm | Social | Noble interactions |
| `Command` | Leadership | Social | Leading men |
| `Inspiration` | Leadership | Social | Morale boosting |
| `Bargaining` | Trade | Social | Market dealings |
| **Intelligence Aliases** |
| `Logistics` | Steward | Intelligence | Supply management |
| `Administration` | Steward | Intelligence | Camp management |
| `Healing` | Medicine | Intelligence | Treating wounds |
| `Surgery` | Medicine | Intelligence | Complex medical |
| `Fortification` | Engineering | Intelligence | Building, siege |
| `Siegecraft` | Engineering | Intelligence | Siege equipment |

---

## Current Content Coverage Analysis

### By Content System

| System | Files | Skill XP Used | Primary Attributes |
|--------|-------|---------------|-------------------|
| **Order Events** | 16 files, 84 events | Yes | Cunning, Endurance, Social |
| **Decisions** | 4 files, 37 decisions | Yes | Mixed |
| **Context Events** | 12 files, 56 events | Partial | Social, Cunning |
| **Map Incidents** | 7 files, 51 incidents | Partial | Mixed |
| **Routines** | 1 config, 10 activities | Yes | Endurance, Cunning, Social |

### Attribute Coverage Gap Analysis

| Attribute | Current Coverage | Gap | Recommendation |
|-----------|------------------|-----|----------------|
| **Vigor** | Low | Missing melee training content | Add sparring events, combat drills |
| **Control** | Low | Archery practice sparse | Add ranged training, hunting events |
| **Endurance** | Good | Athletics, Crafting covered | Expand Riding content |
| **Cunning** | Excellent | Scouting, Tactics well covered | Expand Roguery options |
| **Social** | Good | Charm, Leadership covered | Expand Trade content |
| **Intelligence** | Medium | Medicine covered, others sparse | Add Engineering, Steward content |

---

## Content Improvement Index

### Phase 1: Implement Thematic Aliases (Code)

Update `SkillCheckHelper.GetSkillByName()` with new aliases:

```csharp
var mappedSkill = normalizedName switch
{
    // Current
    "perception" => DefaultSkills.Scouting,
    "smithing" => DefaultSkills.Crafting,
    
    // Vigor
    "swordplay" or "meleecombat" => DefaultSkills.OneHanded,
    "heavyweapons" => DefaultSkills.TwoHanded,
    "pikework" => DefaultSkills.Polearm,
    
    // Control
    "archery" => DefaultSkills.Bow,
    "marksmanship" => DefaultSkills.Crossbow,
    "javelinwork" => DefaultSkills.Throwing,
    
    // Endurance
    "horsemanship" => DefaultSkills.Riding,
    "endurance" or "physicallabor" => DefaultSkills.Athletics,
    "repair" => DefaultSkills.Crafting,
    
    // Cunning
    "awareness" or "vigilance" => DefaultSkills.Scouting,
    "battleplanning" => DefaultSkills.Tactics,
    "stealth" or "cunning" => DefaultSkills.Roguery,
    
    // Social
    "persuasion" or "diplomacy" => DefaultSkills.Charm,
    "command" or "inspiration" => DefaultSkills.Leadership,
    "bargaining" => DefaultSkills.Trade,
    
    // Intelligence
    "logistics" or "administration" => DefaultSkills.Steward,
    "healing" or "surgery" => DefaultSkills.Medicine,
    "fortification" or "siegecraft" => DefaultSkills.Engineering,
    
    _ => null
};
```

**Files to Update:**
- [ ] `src/Features/Content/SkillCheckHelper.cs`
- [ ] `src/Features/Camp/CampRoutineProcessor.cs`

### Phase 2: Routine Outcomes Config

Update `routine_outcomes.json` to use thematic aliases:

| Current | Change To | Attribute |
|---------|-----------|-----------|
| `"skill": "Athletics"` | `"skill": "Endurance"` or `"PhysicalLabor"` | Endurance |
| `"skill": "OneHanded"` | `"skill": "MeleeCombat"` or `"Swordplay"` | Vigor |
| `"skill": "Scouting"` | `"skill": "Vigilance"` or `"Awareness"` | Cunning |
| `"skill": "Charm"` | `"skill": "Persuasion"` | Social |
| `"skill": "Medicine"` | `"skill": "Healing"` | Intelligence |
| `"skill": "Trade"` | `"skill": "Bargaining"` | Social |
| `"skill": "Tactics"` | `"skill": "BattlePlanning"` | Cunning |
| `"skill": "Steward"` | `"skill": "Logistics"` | Intelligence |

### Phase 3: Order Events Audit

Each order event file should be reviewed for:

1. **Skill check uses thematic alias** (already done for `Perception`)
2. **SkillXp uses thematic aliases** for consistency
3. **Attribute balance** - ensure XP rewards benefit appropriate attributes

| File | Current Skills | Proposed Thematic |
|------|----------------|-------------------|
| `sentry_duty_events.json` | Scouting, Tactics, Athletics | Vigilance, BattlePlanning, Endurance |
| `guard_post_events.json` | Scouting, Tactics, Athletics | Perception, BattlePlanning, Endurance |
| `scout_route_events.json` | Scouting, Athletics, Riding | Awareness, Endurance, Horsemanship |
| `treat_wounded_events.json` | Medicine | Healing, Surgery |
| `repair_equipment_events.json` | Crafting | Repair, Smithing |
| `firewood_detail_events.json` | Athletics | PhysicalLabor |
| `forage_supplies_events.json` | Scouting, Athletics | Awareness, Endurance |
| `lead_patrol_events.json` | Leadership, Tactics, Scouting | Command, BattlePlanning, Awareness |
| `train_recruits_events.json` | Leadership, varied combat | Command, Inspiration, MeleeCombat |
| `inspect_defenses_events.json` | Engineering | Fortification, Siegecraft |

### Phase 4: Decision Content Audit

Update `camp_decisions.json` for thematic consistency:

| Decision Category | Current Skills | Proposed Thematic |
|-------------------|----------------|-------------------|
| Training | OneHanded, Bow, Polearm, etc. | Swordplay, Archery, PikeWork |
| Social | Charm | Persuasion, Diplomacy |
| Economic | Trade, Scouting | Bargaining, Awareness |
| Recovery | Medicine | Healing |

### Phase 5: New Content to Fill Gaps

#### Vigor Gap (Melee Combat)

Add to `camp_decisions.json`:
```json
{
  "id": "dec_sparring_ring",
  "title": "Enter the Sparring Ring",
  "effects": { "fatigue": 4, "skillXp": { "Swordplay": 15 } }
},
{
  "id": "dec_heavy_weapons_drill", 
  "title": "Heavy Weapons Drill",
  "effects": { "fatigue": 5, "skillXp": { "HeavyWeapons": 12 } }
}
```

#### Control Gap (Ranged Combat)

Add to `camp_decisions.json`:
```json
{
  "id": "dec_target_practice",
  "title": "Target Practice",
  "effects": { "fatigue": 3, "skillXp": { "Archery": 12 } }
},
{
  "id": "dec_hunting_party",
  "title": "Join Hunting Party",
  "effects": { "fatigue": 4, "skillXp": { "Archery": 10, "Awareness": 8 } }
}
```

#### Intelligence Gap (Engineering/Steward)

Add to order events:
- `order_construct_defenses` → Engineering XP as "Fortification"
- `order_manage_supplies` → Steward XP as "Logistics"

---

## Attribute Balance Matrix

Target: Each attribute should have roughly equal XP opportunity across all content.

| Attribute | Orders | Decisions | Events | Incidents | Routines | Total Opportunities |
|-----------|--------|-----------|--------|-----------|----------|---------------------|
| Vigor | 🔴 2 | 🔴 3 | 🟡 5 | 🔴 2 | 🔴 1 | **13** (LOW) |
| Control | 🔴 1 | 🔴 2 | 🔴 2 | 🔴 1 | 🔴 0 | **6** (VERY LOW) |
| Endurance | 🟢 8 | 🟢 6 | 🟡 4 | 🟡 3 | 🟢 5 | **26** (GOOD) |
| Cunning | 🟢 10 | 🟡 4 | 🟢 8 | 🟢 6 | 🟢 4 | **32** (EXCELLENT) |
| Social | 🟢 6 | 🟢 8 | 🟢 7 | 🟡 4 | 🟢 3 | **28** (GOOD) |
| Intelligence | 🟡 4 | 🔴 2 | 🟡 5 | 🔴 2 | 🔴 2 | **15** (LOW) |

**Priority:** Add Vigor, Control, and Intelligence content.

---

## Implementation Checklist

### Code Changes
- [ ] Update `SkillCheckHelper.GetSkillByName()` with all aliases
- [ ] Update `CampRoutineProcessor.GetSkillFromName()` to match
- [ ] Add validation warning for unknown skill names in content validator

### Config Changes
- [ ] Update `routine_outcomes.json` with thematic skill names
- [ ] Review `simulation_config.json` for skill references

### Content Updates (By Priority)

**Priority 1: Already Using Skill Checks**
- [ ] `sentry_duty_events.json` - already uses Perception ✅
- [ ] `guard_post_events.json` - uses Perception, add consistent skillXp
- [ ] `scout_route_events.json` - update skillXp to thematic

**Priority 2: Combat Training**
- [ ] Add Vigor training decisions (sparring, weapons drills)
- [ ] Add Control training decisions (archery, throwing)
- [ ] Update training event skillXp to thematic aliases

**Priority 3: Specialist Orders**
- [ ] Update all T4-T6 order events to thematic aliases
- [ ] Add Engineering content for defense/siege orders

**Priority 4: Full Audit**
- [ ] All 84 order events reviewed for thematic consistency
- [ ] All 37 decisions reviewed for thematic consistency
- [ ] All map incidents reviewed

### Documentation Updates
- [ ] Update [native-skill-xp.md](native-skill-xp.md) with complete alias table
- [ ] Update [event-system-schemas.md](../Features/Content/event-system-schemas.md) with alias guidance
- [ ] Update [writing-style-guide.md](../Features/Content/writing-style-guide.md) with thematic terms

---

## Example: Before and After

### Before (Current)
```json
{
  "id": "sentry_movement_spotted",
  "options": [
    {
      "id": "challenge",
      "skillCheck": { "skill": "Perception", "difficulty": 40 },
      "effects": {
        "skillXp": { "Scouting": 22 }  // ← Inconsistent with skill check
      }
    }
  ]
}
```

### After (Thematic)
```json
{
  "id": "sentry_movement_spotted", 
  "options": [
    {
      "id": "challenge",
      "skillCheck": { "skill": "Perception", "difficulty": 40 },
      "effects": {
        "skillXp": { "Perception": 22 }  // ← Consistent thematic term
      }
    }
  ]
}
```

Both map to `Scouting` skill → `Cunning` attribute, but content is more readable.

---

## Success Metrics

1. **Code:** All thematic aliases implemented and tested
2. **Content:** 100% of skill references use thematic terms (or raw Bannerlord names where thematic doesn't apply)
3. **Balance:** Each attribute has at least 15 XP opportunities across all content
4. **Documentation:** All aliases documented with attribute mapping
5. **Validation:** Content validator warns on unknown skill names
