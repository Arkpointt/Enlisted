# Onboarding System Update - Labor Lance Integration

> **Purpose**: Document the updated T1 onboarding flow that assigns new recruits to a cultural labor lance, forces Camp Laborer duty, and manages progression to T2 combat lances with equipment upgrades.

**Last Updated**: December 18, 2025  
**Status**: Ready for Implementation (after codebase research)

---

## Overview

When a player enlists at T1 (Levy/Peasant), they are assigned to a **culture-specific labor lance** and forced into the **Camp Laborer** duty. Upon reaching T2, they qualify for promotion to a combat lance and can choose a specialized duty.

### **Key Finding: System Already 90% Complete!**

After researching the codebase, the existing systems already handle:
- ✅ Lance assignment on enlistment (`AssignProvisionalLance()`)
- ✅ Duty assignment on enlistment (`GetStarterDutyForFormation()`)
- ✅ Tier-based promotion with requirements (`PromotionBehavior`)
- ✅ Tier-based equipment/troop selection (`TroopSelectionManager`)

**What we need to add:**
1. Labor lance definitions (6 culture-specific)
2. "camp_laborer" duty definition
3. Labor-specific schedule activities and events
4. Modify `GetStarterDutyForFormation()` to return "camp_laborer" for T1
5. Modify `AssignProvisionalLance()` to assign labor lance at T1
6. Add duty selection event at T2 promotion

---

## Cultural Labor Lance Names

### Default T1 Lance Assignment (By Culture):

| Culture   | Lance Name       | Individual Title | Description |
|-----------|------------------|------------------|-------------|
| Vlandia   | The Valets       | Gros Valet       | Pack handlers and camp servants |
| Empire    | Lixa Cohort      | Lixa             | Camp followers and sutlers |
| Sturgia   | Thrall Gang      | Thrall           | Unfree laborers for menial tasks |
| Battania  | Gillie Band      | Gillie           | Servants who carry equipment |
| Khuzait   | Albatu Cadre     | Albatu           | Subject caste performing logistics |
| Aserai    | Sa'is            | Sa'is            | Horse grooms and attendants |

---

## T1 Onboarding Flow

### **Step 1: Enlistment**

```
Player accepts enlistment offer from lord
  ↓
EnlistmentBehavior.EnlistWithLord() called
  ↓
Tier = 1
Culture = lord.Culture
LanceId = GetLaborLanceId(culture)
SelectedDuty = "camp_laborer" (forced)
  ↓
Fire onboarding event: "You've been assigned to [The Valets]"
```

**New Field Required:**
- `_laborLanceId` (string) - Tracks if player is in a labor lance

### **Step 2: Labor Lance Assignment**

**Function:** `AssignToLaborLance(Hero lord)`

```csharp
private string AssignToLaborLance(Hero lord)
{
    string culture = lord.Culture.StringId;
    
    string lanceName = culture switch
    {
        "vlandia" => "The Valets",
        "empire" => "Lixa Cohort",
        "sturgia" => "Thrall Gang",
        "battania" => "Gillie Band",
        "khuzait" => "Albatu Cadre",
        "aserai" => "Sa'is",
        _ => "Labor Company" // Fallback
    };
    
    _currentLanceId = $"labor_{culture}";
    _isInLaborLance = true;
    _selectedDuty = "camp_laborer"; // Forced duty
    
    return lanceName;
}
```

### **Step 3: Display Onboarding Event**

**Event ID:** `onboarding_labor_lance_assignment`

```json
{
  "id": "onboarding_labor_lance_assignment",
  "title": "Assignment to {LANCE_NAME}",
  "body": "The sergeant eyes you up and down.\n\n'Fresh meat, eh? You're assigned to {LANCE_NAME}. Do your work, keep your head down, and maybe you'll earn a real posting.'\n\nYou've been assigned the duty of Camp Laborer. Prove yourself through hard work and loyalty.",
  "options": [
    {
      "text": "[Continue] Understood.",
      "outcome": "Player begins T1 service in labor lance"
    }
  ]
}
```

**Tokens:**
- `{LANCE_NAME}` → Culture-specific labor lance name
- Player cannot choose duty at T1
- Schedule auto-generated with `camp_laborer` activities

---

## T2 Promotion System

### **Promotion Criteria**

Player qualifies for T2 promotion when:
1. **Rank Tier = 2** (earned through XP)
2. **Days Enlisted ≥ 30** (minimum service time)
3. **Lance Reputation ≥ 20** (proven loyalty)
4. **Not blacklisted** (Heat < 80)

**BUT:** Player does NOT auto-promote. They receive an event offering promotion.

### **Promotion Event Flow**

**Trigger:** Daily check when player meets T2 criteria while in labor lance

**Event ID:** `promotion_t2_combat_lance_offer`

```json
{
  "id": "promotion_t2_combat_lance_offer",
  "title": "Promotion Opportunity",
  "body": "Your lance sergeant approaches.\n\n'You've done well. I'm recommending you for a combat posting. The captain has a spot open in [COMBAT_LANCE_NAME]. You'll get proper gear and a duty assignment.\n\nWhat do you say?'",
  "options": [
    {
      "text": "[Accept] I'm ready for combat duty.",
      "next_event_id": "promotion_t2_duty_selection",
      "outcome": "Promote to T2, move to combat lance"
    },
    {
      "text": "[Decline] I'll stay with the laborers for now.",
      "outcome": "Remain in labor lance (can accept later)"
    }
  ]
}
```

### **Duty Selection Event**

**Event ID:** `promotion_t2_duty_selection`

```json
{
  "id": "promotion_t2_duty_selection",
  "title": "Choose Your Specialty",
  "body": "The captain looks at your record.\n\n'Alright, soldier. We need bodies in several positions. Pick one that suits you.'",
  "options": [
    {
      "text": "Scout - Reconnaissance and pathfinding",
      "requires": { "skill": "Scouting", "value": 30 },
      "duty": "scout"
    },
    {
      "text": "Lookout - Watch duty and signals",
      "requires": { "skill": "Bow", "value": 20 },
      "duty": "lookout"
    },
    {
      "text": "Field Medic - Tend the wounded",
      "requires": { "skill": "Medicine", "value": 30 },
      "duty": "field_medic"
    },
    {
      "text": "Armorer - Repair weapons and armor",
      "requires": { "skill": "Smithing", "value": 30 },
      "duty": "armorer"
    },
    {
      "text": "Quartermaster - Manage supplies",
      "requires": { "skill": "Steward", "value": 30 },
      "duty": "quartermaster"
    },
    {
      "text": "[Default] General infantry duty",
      "duty": "none",
      "outcome": "No specialty, standard soldier"
    }
  ]
}
```

**Logic:**
- Options with `"requires"` only show if player meets skill threshold
- Player MUST pick one (cannot stay in labor lance after accepting promotion)
- Combat lance assignment is randomized from lord's active combat lances

---

## Equipment Progression System

### **Current System (Verify)**

Player equipment is gated by tier:
- **T1:** Basic/Peasant gear (pitchfork, club, rags)
- **T2:** Standard soldier gear (sword, shield, gambeson)
- **T3:** Veteran gear (quality weapons, mail)

**Implementation Check Required:**
- Does `EnlistmentBehavior` already gate equipment by `EnlistmentTier`?
- Is there an `EquipmentManager` or similar?

**TODO:** Verify existing equipment progression system in codebase.

### **Promotion Equipment Grant**

When player promotes to T2:
```
Player accepts T2 promotion
  ↓
Grant T2 equipment:
  - Remove T1 labor gear
  - Add T2 soldier gear (culture-specific)
  - Apply formation-appropriate loadout
```

**Function:** `GrantTierEquipment(int tier, string formation, string culture)`

---

## ACTUAL System Research Findings

### **Current System (What Already Exists):**

1. **Duty Assignment** (`EnlistmentBehavior.cs` line 2439-2471):
   - Defaults to `"runner"` initially
   - Then `EnlistedDutiesBehavior.GetStarterDutyForFormation()` assigns formation-specific duty:
     - Infantry → `"runner"`
     - Archer → `"lookout"`
     - Cavalry → `"messenger"`
     - HorseArcher → `"scout"`
     - Naval → `"boatswain"`

2. **Lance Assignment** (`EnlistmentBehavior.cs` line 2461):
   - `AssignProvisionalLance()` already exists
   - Uses `LanceRegistry.GenerateProvisionalLance()` to pick from culture-specific lance pools
   - Player gets provisional lance, then can finalize choice later

3. **Promotion System** (`PromotionBehavior.cs`):
   - Already has tier-based requirements (XP, Days, Events, Battles, Rep, Relation, Discipline)
   - T1→T2 requires: 700 XP, 14 days, 5 events, 2 battles, 0 rep, 0 relation, <8 discipline
   - Promotion events already fire at tier milestones

4. **Equipment System** (`TroopSelectionManager.cs`):
   - `GetUnlockedTroopsForCurrentTier()` already gates troops/equipment by tier
   - Player selects troop from culture-specific troop tree at their tier level
   - Equipment is applied from selected troop template

### **What Needs to Change:**

**INSTEAD of creating a complex new system, we need MINIMAL changes:**

## Implementation Checklist

### **Phase 1: Data Configuration**

- [ ] Add `"camp_laborer"` duty to `duties_system.json`
  - `min_tier: 1`, `max_tier: 2`
  - `event_prefix: "labor_"`
  - `wage_multiplier: 0.8`
  - `multi_skill_xp: { "Athletics": 2, "Engineering": 1 }`

- [ ] Add labor lance activities to `schedule_config.json`:
  - `labor_dig_latrines` (Morning)
  - `labor_haul_water` (Afternoon)
  - `labor_chop_firewood` (Morning/Afternoon)
  - `labor_carry_supplies` (Afternoon)
  - `labor_stable_work` (Morning)
  - `labor_kitchen_duty` (Afternoon/Dusk)

- [ ] Create `events_duty_camp_laborer.json`
  - 8-10 multi-choice duty events for labor tasks
  - Include skill checks, gold gains/losses, chaining

- [ ] Create labor lance definitions in `ModuleData/Enlisted/Lances/lances_labor.json`:
  ```json
  {
    "style_id": "labor",
    "lances": [
      { "id": "labor_vlandia", "name": "The Valets", "culture": "vlandia" },
      { "id": "labor_empire", "name": "Lixa Cohort", "culture": "empire" },
      { "id": "labor_sturgia", "name": "Thrall Gang", "culture": "sturgia" },
      { "id": "labor_battania", "name": "Gillie Band", "culture": "battania" },
      { "id": "labor_khuzait", "name": "Albatu Cadre", "culture": "khuzait" },
      { "id": "labor_aserai", "name": "Sa'is", "culture": "aserai" }
    ]
  }
  ```

### **Phase 2: Code Changes**

**File:** `src/Features/Assignments/Behaviors/EnlistedDutiesBehavior.cs`

- [ ] Modify `GetStarterDutyForFormation()` (line 179):
  ```csharp
  public static string GetStarterDutyForFormation(string formation)
  {
      // All T1 recruits start as camp laborers regardless of formation
      return "camp_laborer";
  }
  ```

**File:** `src/Features/Assignments/Core/LanceRegistry.cs`

- [ ] Add method: `GenerateLaborLance(Hero lord)`
  - Check lord's culture
  - Return labor lance for that culture (e.g., "labor_vlandia" → "The Valets")
  - This is simpler than `GenerateProvisionalLance` - no randomization, direct culture mapping

**File:** `src/Features/Enlistment/Behaviors/EnlistmentBehavior.cs`

- [ ] Modify `AssignProvisionalLance()` (line 2777):
  - Check if player tier == 1
  - If T1: Call `LanceRegistry.GenerateLaborLance(lord)` instead of `GenerateProvisionalLance()`
  - If T2+: Use existing `GenerateProvisionalLance()` (combat lances)

- [ ] Add field: `private bool _isInLaborLance = false;`

- [ ] Add method: `PromoteToT2CombatLance(string dutyId)`
  - Set `_isInLaborLance = false`
  - Call existing `AssignProvisionalLance(false)` to get combat lance
  - Set `_selectedDuty = dutyId`
  - Trigger troop selection (equipment upgrade) via `TroopSelectionManager`

**File:** `src/Features/Ranks/Behaviors/PromotionBehavior.cs`

- [ ] Modify promotion event at T1→T2 to offer duty selection
  - Check if player was in labor lance
  - If yes: Fire `promotion_t2_duty_selection` event before standard promotion
  - Player picks duty, then equipment selection happens as normal

### **Phase 3: Story Content**

- [ ] Update `events_onboarding.json`:
  - Add `onboarding_labor_lance_assignment` (fires at T1 enlistment)
  - Add `promotion_t2_duty_selection` (fires at T2 promotion if from labor lance)

### **Phase 4: Testing**

- [ ] Test T1 enlistment → labor lance assignment (culture-specific)
- [ ] Test T1 gets "camp_laborer" duty (not formation-specific)
- [ ] Test camp laborer activities appear in schedule
- [ ] Test labor duty events fire correctly
- [ ] Test T2 promotion → duty selection event
- [ ] Test T2 promotion → combat lance assignment (existing system)
- [ ] Test T2 promotion → troop/equipment selection (existing system)
- [ ] Test all 6 cultural labor lance names display correctly

---

## Story Content Requirements

### **Camp Laborer Duty Events** (8-10 events)

**Event Categories:**
1. **Hard Labor** - Dig latrines, haul logs, shovel dirt
   - Skill checks: Athletics, Engineering
   - Outcomes: Find coins, get injured, impress sergeant

2. **Fetch & Carry** - Haul water, carry supplies, move tents
   - Skill checks: Athletics, Steward
   - Outcomes: Spill supplies, meet merchants, eavesdrop on officers

3. **Kitchen Duty** - Peel potatoes, serve meals, scrub pots
   - Skill checks: Steward, Charm
   - Outcomes: Extra rations, make friends, food poisoning

4. **Stable Work** - Muck stalls, groom horses, repair tack
   - Skill checks: Riding, Smithing
   - Outcomes: Bond with horse, get kicked, find hidden items

5. **Firewood & Foraging** - Chop wood, gather kindling, hunt small game
   - Skill checks: Athletics, Scouting
   - Outcomes: Find treasure, encounter bandits, get lost

**Event Design Pattern:**
```
Notification → [Do Duty] or [Skip] 
  ↓
[Do Duty] → Event fires (20% chance)
  ↓
Multi-choice event with skill checks
  ↓
Outcomes: Gold, XP, Rep, Items, Heat
  ↓
20% chance: Chain to follow-up event (2-4 hours later)
```

---

## Future Enhancements (Post-Launch)

- [ ] Add "labor lance reputation" separate from combat lance rep
- [ ] Add unique labor lance personalities (gruff sergeant archetypes)
- [ ] Add T1→T2 "graduation ceremony" cinematic event
- [ ] Add culture-specific labor lance backstories
- [ ] Add option to voluntarily return to labor lance (disgrace/demotion)

---

## Notes

- Labor lances are **infinite capacity** (not 5-10 soldiers like combat lances)
- Labor lance has **generic sergeant NPC** (not procedurally generated lance leader)
- Player **cannot switch duties at T1** (forced camp laborer)
- Player **can decline T2 promotion** but will receive offer again in 7 days
- Equipment progression must align with faction troop tree (T1 = Recruit, T2 = Soldier, T3 = Veteran)

---

## Summary: Simplified Integration Approach

**OLD PLAN (Complex):** Build entire new system with labor lance tracking, promotion eligibility checks, separate equipment manager

**NEW PLAN (Simple):** Leverage existing systems with minimal changes:

| System | Current Behavior | New Behavior (T1 Only) | Code Change |
|--------|------------------|------------------------|-------------|
| Duty Assignment | Formation-specific | Always "camp_laborer" | 1 line change in `GetStarterDutyForFormation()` |
| Lance Assignment | Random combat lance | Culture-specific labor lance | Add `GenerateLaborLance()`, modify `AssignProvisionalLance()` |
| T2 Promotion | Auto troop selection | Duty selection → troop selection | Add event at promotion |
| Equipment | Tier-gated (exists) | No change needed | None |

**Estimated Implementation:** 
- Config/Data: 2-3 hours (JSON files)
- Code Changes: 3-4 hours (3 files modified)
- Story Content: 6-8 hours (8-10 labor events)
- Testing: 2-3 hours

**Total:** ~15 hours (vs. ~40 hours for building from scratch)

---

**Ready for implementation.** Review and approve before proceeding with code changes.

