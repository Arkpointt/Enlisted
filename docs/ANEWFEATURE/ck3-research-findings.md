# CK3 Roads to Power: Research Findings

## Research Date
2026-01-14

## System Architecture Overview

### 1. Decision System (Player-Initiated Actions)

**File Structure:** `game/common/decisions/`

**Decision Anatomy:**
```
decision_id = {
    picture = { reference = "gfx/path/to/image.dds" }
    desc = decision_desc_localization_key
    selection_tooltip = tooltip_key
    
    decision_group_type = adventurer_minor  // Category
    
    cooldown = { years = 5 }  // Time before can be taken again
    
    ai_check_interval_by_tier = {  // How often AI considers
        duchy = 32
    }
    
    is_shown = {  // Conditions for decision to APPEAR
        has_ep3_dlc_trigger = yes
        government_has_flag = government_is_landless_adventurer
    }
    
    is_valid = {  // Conditions for decision to be CLICKABLE
        is_alive = yes
        is_available_adult = yes
        gold >= 100
    }
    
    is_valid_showing_failures_only = {  // Quick validation
        is_at_war = no
    }
    
    effect = {  // What happens when you take it
        trigger_event = { id = ep3_laamps.8010 }
        add_gold = 50
    }
    
    ai_potential = {  // AI conditions
        ai_honor > 0
    }
    
    ai_will_do = {  // AI weighting
        base = 100
        modifier = {
            add = 50
            has_trait = greedy
        }
    }
}
```

**Key Insights:**
- Decisions have 3 validation levels: `is_shown`, `is_valid`, `is_valid_showing_failures_only`
- Cooldowns prevent spamming (5-10 years typical)
- Decisions grouped by type (`adventurer_minor`, `adventurer_major`, etc.)
- AI has separate evaluation logic

---

### 2. Task Contract System (Job Board)

**File Structure:** `game/common/task_contracts/`

**Contract Types:**
- `laamp_base_contracts.txt` - Core contracts (262kb)
- `laamp_extra_contracts.txt` - Additional variety (157kb)
- Organized by skill: Diplomacy, Martial, Stewardship, Intrigue, Learning, Prowess

**Contract Anatomy:**
```
laamp_base_0001 = {  // "Regale court with stories"
    group = laamp_contracts_diplomacy_group
    icon = "gfx/interface/icons/scheme_types/diplomacy.dds"
    
    travel = yes  // Requires travel to employer
    use_diplomatic_range = no
    
    weight = {  // Likelihood to appear
        value = task_contract_weight_default_value
        add = laamp_contracts_weight_up_diplomacy_value
        
        scope:employer = {
            // Weight up for gregarious, trusting employers
            if = {
                limit = { has_trait = gregarious }
                add = task_contract_weight_bonus_value
            }
            // Weight down for arrogant employers
            if = {
                limit = { has_trait = arrogant }
                add = task_contract_weight_malus_value
            }
        }
        multiply = task_contract_weight_by_tier_value
    }
    
    valid_to_create = {  // Can this contract be generated?
        valid_laamp_basic_trigger = yes
        employer_has_treasury_to_offer_job_trigger = yes
        rule_out_dramatic_laamp_employers_trigger = yes
    }
    
    valid_to_accept = {  // Can player accept it?
        valid_laamp_basic_accept_only_trigger = yes
        scope:employer = { is_landed = yes }
    }
    
    valid_to_continue = {  // Can contract continue?
        task_contract_employer = { is_landed = yes }
    }
    
    valid_to_keep = {  // Should contract stay available?
        task_contract_employer = { is_alive = yes }
    }
    
    on_accepted = {  // What happens when accepted
        task_contract_taker = { 
            play_sound_effect = "event:/...contracts_accept_contract"
        }
        save_scope_as = task_contract
        task_contract_taker ?= {
            start_scheme = {  // Starts multi-step event chain
                type = laamp_base_0001_contract_scheme
                contract = root
                target_character = root.task_contract_employer
            }
            trigger_event = laamp_base_contract_schemes.0001
        }
    }
    
    on_create = {  // Set up rewards
        scope:contract = {
            set_variable = {
                name = gold_success_critical
                value = task_contract_taker.task_contract_success_gold_gain_half_value
            }
        }
    }
}
```

**Key Insights:**
- Contracts are **generated dynamically** based on employer traits, location, player skills
- Weight system determines likelihood (affected by player skills, employer personality, strategic context)
- 4 validation stages: `create`, `accept`, `continue`, `keep`
- Contracts linked to **schemes** (CK3's multi-step event chains)
- Gold rewards set at creation, stored in variables
- Category system: ~70+ base contracts across 6-7 skill categories

**Contract Categories Found:**
1. **Diplomacy** - Storytelling, reputation improvement, mediator
2. **Martial** - Hunt criminals, hired muscle, garrison service, train troops
3. **Stewardship** - Tax collection, census, construction help
4. **Intrigue** - Intelligence gathering, assassination, abduction, heists
5. **Learning** - Transcription, theological debates, tutoring
6. **Prowess** - Hunting, guard duty, rustling, poaching
7. **Justicar** - Protect innocents, rescue missions, chivalry events
8. **Criminal** - Confidence tricks, fake taxes, bogus relics, theft

---

### 3. Domicile System (Camp Progression)

**File Structure:** `game/common/domiciles/buildings/`

**Building Anatomy:**
```
camp_main_01 = {  // Pavilion (Starting tent)
    slot_type = main
    construction_time = 1  // Always built
    allowed_domicile_types = { camp }
    
    character_modifier = {
        health = 0.5
        domicile_external_slots_capacity_add = 2
    }
    
    parameters = {
        camp_unlocks_second_officer = yes
    }
    
    ai_value = { value = 10000 }  // AI priority
    
    asset = {  // Visual appearance
        trigger = { owner.culture = { has_graphical_western_culture_group_trigger = yes } }
        icon = "gfx/interface/icons/domicile_building/domicile_building_pavillion.dds"
        texture = "gfx/interface/window_domiciles/laamp_building_pavillion_level_01.dds"
        soundeffect = "event:/DLC/EP3/SFX/UI/camp_buildings/ep3_ui_domicile_buildings_main_building"
    }
}

camp_main_02 = {  // Upgraded Pavilion
    slot_type = main
    construction_time = 300  // Days
    allowed_domicile_types = { camp }
    previous_building = camp_main_01  // Upgrade chain
    
    cost = { gold = camp_main_02_domicile_building_gold_cost_value }
    refund = {
        gold = {
            value = camp_main_02_domicile_building_gold_cost_value
            multiply = camp_refund_mult_value
            floor = yes
        }
    }
    
    character_modifier = {
        domicile_external_slots_capacity_add = 1
        men_at_arms_cap = 1
        enemy_hostile_scheme_phase_duration_add = miniscule_scheme_phase_duration_malus_value
        character_travel_safety_mult = 0.02
        provisions_capacity_add = 100
    }
    
    parameters = {
        camp_unlocks_second_officer = yes
    }
    
    ai_value = {
        value = camp_main_main_path_value
        if = {
            limit = { num_domicile_buildings >= 4 }
            multiply = 10
        }
    }
}
```

**Camp Building Types Found:**
- **Main Building** (Pavilion) - 6 upgrade levels
- **External Slots** (2-5 available):
  - Armory (combat bonuses)
  - Marketplace (gold income)
  - Kennels (dogs, hunting)
  - Forge (crafting)
  - Barracks (troop capacity)
- **Parameters** unlock features (e.g., `camp_unlocks_adopt_a_kennel_dog_decision`)

**Key Insights:**
- Buildings provide **passive modifiers** (health, gold, troops, scheme resistance)
- **Slot system** - main building + 2-5 external slots
- **Upgrade chains** - each building has 2-4 tiers
- **Gold cost + time cost** for construction (180-420 days)
- **Refund system** when downgrading
- **Parameters** unlock new decisions/features
- **Cultural variants** - different visuals per culture group

---

### 4. Integration: How It All Works Together

**Player Flow:**
```
1. BROWSE DECISIONS
   └─> Open decision menu
   └─> See available decisions (filtered by is_shown)
   └─> Click one (if is_valid)
   └─> Execute effect

2. BROWSE CONTRACTS
   └─> Open contract board
   └─> See contracts generated for current area (weighted by skills/context)
   └─> Accept contract
   └─> Start scheme (multi-step event chain)
   └─> Complete for rewards

3. UPGRADE CAMP
   └─> Open domicile menu
   └─> See available buildings (filtered by prerequisites)
   └─> Pay gold + wait construction time
   └─> Get passive modifiers + unlock new features

4. TRIGGER EVENTS
   └─> Some decisions trigger immediate events
   └─> Contracts trigger scheme-based event chains
   └─> Camp upgrades may unlock new decisions (via parameters)
```

---

## Comparison to Enlisted Implementation

| System | CK3 | Enlisted (Planned) | Notes |
|--------|-----|-------------------|-------|
| **Player-Initiated Content** | Decisions + Contracts | Opportunity Pool | Similar concept, different structure |
| **Browsing Interface** | Decision menu, Contract board | Camp Hub menu | Text-based vs UI |
| **Generation Logic** | Weight-based (traits, skills, location) | ContentOrchestrator + categories | Both use dynamic generation |
| **Cooldowns** | Per-decision (5-10 years) | Per-category (5-10 days) | CK3 timescale is years, Enlisted is days |
| **Validation Levels** | is_shown, is_valid, is_valid_showing_failures_only | MeetsRequirements() | Similar gating |
| **Rewards** | Gold, prestige, artifacts, traits | Gold, XP, reputation, fatigue effects | Enlisted more granular |
| **Multi-Step Content** | Schemes (built-in system) | Event chains (existing system) | Both support branching |
| **Camp Progression** | Domicile buildings (passive modifiers) | Personal Kit (passive multipliers) | Enlisted more personal-focused |
| **Camp Upgrades** | Gold + time cost | Gold + reputation gates | Enlisted adds rep requirement |
| **Unlock System** | Parameters unlock decisions | Career path tags, rep gates | Enlisted more progression-focused |

---

## Key Takeaways for Enlisted

### What CK3 Does Well (Apply to Enlisted)

1. **Validation Layers**
   - `is_shown` - filters what appears
   - `is_valid` - determines if clickable
   - This prevents clutter while showing locked options

2. **Weight-Based Generation**
   - Contracts more likely based on player skills
   - Employer personality affects offerings
   - **Apply:** Opportunity generation bias toward active career paths

3. **Category Cooldowns**
   - Prevents spam of similar content
   - **Apply:** Already in plan (training every 7 days, social every 5 days)

4. **Clear Validation Messages**
   - `custom_description` blocks explain why invalid
   - **Apply:** Show requirements in opportunity descriptions

5. **Parameter-Based Unlocks**
   - Camp upgrades unlock new decisions
   - **Apply:** Personal Kit upgrades unlock new opportunities

6. **Passive Modifier Stacking**
   - Buildings provide small bonuses that stack
   - **Apply:** Personal Kit items stack multipliers

### What CK3 Doesn't Do (Enlisted Improvements)

1. **No Career Path System**
   - CK3 has legacy tracks (prestige-based) but not explicit career paths
   - **Enlisted advantage:** 6 clear career paths with milestones

2. **No Reputation Gates**
   - CK3 uses prestige/fame as soft gates, not hard requirements
   - **Enlisted advantage:** Clear rep thresholds unlock content

3. **Timescale Issues**
   - CK3 decisions have 5-10 year cooldowns (makes sense for grand strategy)
   - **Enlisted advantage:** 5-10 day cooldowns suit faster-paced soldier life

4. **Limited Personal Identity**
   - Camp upgrades are functional, not personal
   - **Enlisted advantage:** Personal Kit is YOUR gear, not just camp infrastructure

---

## Decision Group Types (CK3 Reference)

Found in decision files:
- `adventurer_minor` - Small decisions, frequent
- `adventurer_major` - Big decisions, rare
- `activity_decisions` - Activity-related
- `court_decisions` - Court management

**Enlisted Equivalent:**
- Category tags: `training`, `social`, `economic`, `companion`, `contract`

---

## Files Examined

1. `common/decisions/dlc_decisions/ep_3/06_ep3_laamp_decisions.txt` (77kb)
2. `common/task_contracts/laamp_base_contracts.txt` (262kb)
3. `common/domiciles/buildings/00_camp_buildings.txt` (201kb)

Total: ~540kb of data examined across 3 core systems

---

## Recommendations

### Apply These CK3 Patterns

1. **Three-tier validation** for opportunities:
   ```csharp
   public bool IsShown { get; }     // Should it appear in pool at all?
   public bool IsAvailable { get; }  // Can player select it right now?
   public bool IsValid { get; }      // Final check before execution
   ```

2. **Explicit cooldown tracking** per opportunity ID:
   ```csharp
   private Dictionary<string, int> _opportunityCooldowns = new();
   ```

3. **Parameter-based unlocks**:
   ```csharp
   public class PersonalKitUpgrade
   {
       public List<string> UnlockedOpportunities { get; set; }
       // When purchased, makes new opportunities available
   }
   ```

4. **Clear requirement display**:
   ```
   Available Opportunities:
     🎯 Advanced Weapon Drill (expires 3d)
        Requires: Melee 100, Officer Rep 50
   ```

### Skip These CK3 Patterns

1. **Employer-based weighting** - Not relevant (Enlisted has fixed company/lord)
2. **Geographical region filtering** - Enlisted uses Strategic Context instead
3. **Multi-year cooldowns** - Too slow for Enlisted's pace
4. **Scheme system complexity** - Enlisted's event chains are simpler and sufficient

---

## Conclusion

CK3's Roads to Power uses a sophisticated **decision + contract** system where:
- **Decisions** are one-time or cooldown-gated player choices
- **Contracts** are dynamically generated jobs with weight-based filtering
- **Domiciles** provide passive progression and unlock new decisions

Enlisted's planned **Opportunity Pool** system is conceptually similar but adapted for:
- Faster timescale (days vs years)
- Text-based UI (GameMenu vs Gauntlet)
- Career path integration (not present in CK3)
- Personal progression focus (vs camp infrastructure)

The core insight remains: **Player browses and chooses** instead of **content interrupts player**. CK3 proves this design works for landless adventurer gameplay.
