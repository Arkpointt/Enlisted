# Lance Life Documentation Index

This is the central hub for all Lance Life event system documentation. Use this to find the right document for what you're working on.

**Key Principle:** All skill XP is ACTION-BASED. No passive gains. Players earn XP through:
- **Combat** — Using weapons in battle
- **Duty Events** — Performing assigned duties
- **Training Events** — Opting into drills and practice
- **General Events** — Camp situations and choices

---

## Document Map

```
lance-life/
├── INDEX.md                              ◄── You are here
├── lance_life_events_master_doc_v2.md    ◄── Core reference (triggers, skills, duties, events)
├── lance_life_ui_integration.md          ◄── UI systems (Map Incident vs Menu vs Inquiry)
├── menu_system_update.md                 ◄── Full menu changes for all new systems
├── menu_enhancements_spec.md             ◄── Menu changes for action-based XP (older)
├── player_condition_system.md            ◄── Injuries, illness, recovery, medical care
├── time_and_ai_state_system.md           ◄── Time-of-day events, AI safety checks
├── placeholder_system.md                 ◄── Dynamic text, personalization
├── lance_career_system.md                ◄── 10-person lance, culture ranks, NPCs, progression
├── lance-life.md                         ◄── Feature spec (phased plan, acceptance criteria)
├── camp-life-simulation.md               ◄── Camp conditions system (meters, integration)
├── military-duties-system.md             ◄── Duty system spec
└── map-incidents-integration.md          ◄── Native incident system notes
```

---

## Quick Reference: Which Doc Do I Need?

| I want to... | Read this |
|--------------|-----------|
| **Understand the XP system** | `lance_life_events_master_doc_v2.md` → Skill Growth Model |
| **Brainstorm new events** | `lance_life_events_master_doc_v2.md` → External AI Brainstorm Prompt |
| **Create duty events** | `lance_life_events_master_doc_v2.md` → Duty Events section |
| **Create training events** | `lance_life_events_master_doc_v2.md` → Training Events section |
| **Understand triggers** | `lance_life_events_master_doc_v2.md` → Trigger System |
| **Decide Map Incident vs Game Menu vs Inquiry** | `lance_life_ui_integration.md` → Decision Matrix |
| **Implement a Map Incident** | `lance_life_ui_integration.md` → Map Incidents |
| **Add a Game Menu option** | `lance_life_ui_integration.md` → Game Menus |
| **Understand menu enhancements needed** | `menu_enhancements_spec.md` |
| **Add injury/illness to events** | `player_condition_system.md` |
| **Understand medical/recovery loop** | `player_condition_system.md` → Recovery and Medical Care |
| **Understand time-of-day events** | `time_and_ai_state_system.md` → Time-of-Day System |
| **Check AI safety before events** | `time_and_ai_state_system.md` → AI State Detection |
| **Create dawn/night/siege events** | `time_and_ai_state_system.md` → Time-Based Event Categories |
| **Use placeholders for personalization** | `placeholder_system.md` |
| **Understand camp conditions** | `camp-life-simulation.md` → Core Model |
| **See duty definitions** | `military-duties-system.md` |

---

## The Three Event Types

| Type | Who Gets It | How It Fires | XP For |
|------|-------------|--------------|--------|
| **Duty Events** | Players with that duty | Automatic on triggers | Duty-related skills |
| **Training Events** | Players in that formation | Opt-in via menu | Combat skills |
| **General Events** | All enlisted players | Automatic on triggers | Various skills |

### Duty Events
- **Require** player to have specific duty (Quartermaster, Scout, Medic, etc.)
- Fire automatically when trigger conditions met
- Primary source of non-combat skill XP
- Target: 2–3 per duty per week

### Training Events
- Based on **formation** (Infantry, Cavalry, Archer, Naval)
- Player must **opt in** via wait menu
- Replace old passive formation XP
- Player controls fatigue/XP tradeoff

### General Events
- Available to **all** enlisted players
- Fire based on campaign state (morale, corruption, logistics)
- Lower frequency, higher impact

---

## Event Creation Workflow

### Step 1: Determine Event Type

Ask yourself:
- Is this tied to a **specific duty**? → Duty Event
- Is this **combat skill practice**? → Training Event  
- Is this a **camp situation** anyone might face? → General Event

### Step 2: Brainstorm
Use the external AI prompt from `lance_life_events_master_doc_v2.md`.

**Key inputs:**
- What trigger makes sense?
- What skills should this train? (Must match duty if duty event)
- What tier should this require?
- Safe/risky/corrupt options?

### Step 3: Choose UI System

| Event Type | Default UI | Exception |
|------------|------------|-----------|
| Duty Event | Map Incident | Inquiry for simple/urgent |
| Training Event | Game Menu (always) | — |
| General Event | Map Incident | Inquiry for quick decisions |

See `lance_life_ui_integration.md` for details.

### Step 4: Define Event Data
Add to `lance_stories.json`:

```json
{
  "id": "scout_terrain_recon",
  "title": "Terrain Reconnaissance",
  "type": "duty_event",
  "duty_required": "scout",
  "category": "duty_scout",
  "ui_system": "map_incident",
  "ui_trigger": "BeforeBattle",
  "tier_min": 3,
  "require_final_lance": true,
  "triggers": {
    "all": ["before_battle", "has_duty_scout"]
  },
  "cooldown_days": 3,
  "setup": "Battle's coming. The captain needs terrain intel...",
  "options": [
    {
      "id": "thorough",
      "text": "Thorough survey, take your time",
      "risk": "safe",
      "costs": { "fatigue": 3 },
      "rewards": { 
        "skill_xp": { "Scouting": 40, "Tactics": 20 }
      }
    },
    {
      "id": "quick",
      "text": "Quick sweep",
      "risk": "safe", 
      "costs": { "fatigue": 1 },
      "rewards": {
        "skill_xp": { "Scouting": 20 }
      }
    },
    {
      "id": "risky_deep",
      "text": "Push deep into risky ground",
      "risk": "risky",
      "costs": { "fatigue": 2 },
      "rewards": {
        "skill_xp": { "Scouting": 50, "Tactics": 25 }
      },
      "risk_chance": 0.2,
      "risk_consequence": "ambush"
    }
  ]
}
```

### Step 5: Implement
- Duty Events: Register in `DutyEventBehavior` with duty check
- Training Events: Add via `CampaignGameStarter.AddGameMenuOption` to wait menus
- General Events: Register in `LanceStoryBehavior`

### Step 6: Test
- Verify duty requirement enforced
- Check XP applies correctly (no passive!)
- Confirm fatigue costs apply
- Test escalation accumulation

---

## Trigger Quick Reference

### Campaign State Triggers

| Trigger | Detection | Best UI |
|---------|-----------|---------|
| `siege_started` | `OnSiegeEventStartedEvent` | Map Incident |
| `siege_ongoing` | `BesiegedSettlement != null` | Map Incident / Game Menu |
| `siege_ended_victory` | `OnSiegeEventEndedEvent` + outcome | Map Incident |
| `battle_won` | `MapEventEnded` + victory | Map Incident |
| `battle_heavy_casualties` | `MapEventEnded` + casualty % | Map Incident |
| `entered_town` | `SettlementEntered` (town) | Map Incident / Game Menu |
| `waiting_in_settlement` | In wait menu | Game Menu (best) |
| `raid_completed` | `VillageLooted` | Map Incident |
| `at_sea` | Naval travel state | Map Incident / Game Menu |
| `long_voyage` | Days at sea > X | Map Incident |

### Camp Condition Triggers

| Trigger | Source | Typical Events |
|---------|--------|----------------|
| `logistics_high` | `LogisticsStrain > 60` | Foraging, rationing |
| `morale_low` | `MoraleShock > 60` | Rally, drinking, grief |
| `pay_tension_high` | `PayTension > 50` | Bribery, complaints |
| `heat_high` | `ContrabandHeat > 50` | Shakedowns, consequences |

---

## Duty → Skill → Event Coverage

### Duty Event Coverage Checklist

Ensure each duty has sufficient events:

| Duty | Target Events/Week | Skills | Coverage |
|------|-------------------|--------|----------|
| **Quartermaster** | 2–3 | Steward, Trade, Leadership | [ ] |
| **Scout** | 2–3 | Scouting, Athletics, Bow, Tactics | [ ] |
| **Field Medic** | 2–3 | Medicine, Steward | [ ] |
| **Messenger** | 2–3 | Riding, Athletics, Charm | [ ] |
| **Armorer** | 2–3 | Smithing, Engineering | [ ] |
| **Runner** | 2–3 | Athletics, Scouting | [ ] |
| **Lookout** (naval) | 2–3 | Scouting, Shipmaster | [ ] |
| **Boatswain** (naval) | 2–3 | Boatswain, Leadership | [ ] |
| **Navigator** (naval) | 2–3 | Shipmaster, Scouting | [ ] |

### Training Event Coverage Checklist

Ensure each formation has training options:

| Formation | Training Events | Skills Covered | Coverage |
|-----------|-----------------|----------------|----------|
| **Infantry** | Shield wall, sparring, wrestling, march | Polearm, OneHanded, TwoHanded, Athletics | [ ] |
| **Cavalry** | Horse rotation, mounted drill, charge, care | Riding, Polearm, OneHanded | [ ] |
| **Archer** | Target practice, volley drill, hunting, maintenance | Bow, Crossbow, Scouting | [ ] |
| **Horse Archer** | Mounted archery, skirmish, horse care | Riding, Bow, Throwing | [ ] |
| **Naval** | Boarding drill, rigging, navigation, deck combat | Mariner, Boatswain, Shipmaster, Athletics | [ ] |

### Skill Coverage Checklist

Every skill should be earnable through actions:

### Naval Skills (War Sails)
- [ ] Mariner — Boarding drills, deck combat
- [ ] Boatswain — Deck duties, rigging, crew management
- [ ] Shipmaster — Navigation, helm, fleet command

### Vigor (Melee)
- [ ] OneHanded — Sparring, guard duty
- [ ] TwoHanded — Heavy weapon challenges
- [ ] Polearm — Formation drills, pike practice

### Control (Ranged)
- [ ] Bow — Hunting, target practice
- [ ] Crossbow — Maintenance, wall defense
- [ ] Throwing — Hunting, skirmish drills

### Endurance (Physical)
- [ ] Riding — Horse rotation, cavalry drills
- [ ] Athletics — Labor, climbing, marching
- [ ] Smithing — Forge work, repairs

### Cunning (Tactical)
- [ ] Scouting — Night watch, foraging, recon
- [ ] Tactics — Battle planning, raid tactics
- [ ] Roguery — Theft, contraband, looting

### Social (Influence)
- [ ] Charm — Defusing conflicts, socializing
- [ ] Leadership — Rally men, discipline, responsibility
- [ ] Trade — Merchant dealings, haggling

### Intelligence (Knowledge)
- [ ] Steward — Ration management, logistics
- [ ] Medicine — Tend wounded, surgeon help
- [ ] Engineering — Siege construction, repairs

---

## Event Categories Checklist

Ensure balanced coverage:

- [ ] **Training & Drills** — Combat skill development
- [ ] **Logistics & Scrounging** — Supply problems
- [ ] **Morale & Revelry** — Camp spirits, grief, celebration
- [ ] **Corruption & Theft** — Temptation events
- [ ] **Discipline & Duty** — Following orders, hard jobs
- [ ] **Rivalries & Conflict** — Inter-personal friction
- [ ] **Medical & Wounded** — Injury aftermath
- [ ] **Siege Operations** — Siege-specific events
- [ ] **Naval Operations** — Ship-specific events

---

## Escalation Tracks

### Heat (Contraband/Corruption)
Builds from: smuggling, theft, bribes, corrupt deals

| Level | Threshold | Consequence |
|-------|-----------|-------------|
| Low | 1–3 | Quartermaster watches closely |
| Medium | 4–6 | Random shakedown events |
| High | 7+ | Confiscation, discipline, discharge risk |

### Discipline Risk
Builds from: shirking, sleeping on watch, insubordination

| Level | Threshold | Consequence |
|-------|-----------|-------------|
| Low | 1–3 | Worse duty assignments |
| Medium | 4–6 | Extra duty fatigue penalties |
| High | 7+ | Formal punishment, promotion blocked |

---

## Config Flags

In `enlisted_config.json`:

```json
{
  "lance_life": {
    "enabled": true,
    "max_events_per_week": 3,
    "global_cooldown_days_min": 1,
    "global_cooldown_days_max": 3,
    "debug_logging": false,
    "disabled_events": [],
    "disabled_categories": []
  }
}
```

---

## Version History

| Version | Date | Changes |
|---------|------|---------|
| 1.0 | [Current] | Initial documentation set |

---

*This index is the starting point for all Lance Life development.*
