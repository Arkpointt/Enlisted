# story systems master reference

**purpose:** single source of truth for all story content systems in enlisted. combines content indexes, runtime flow, and escalation system references.

**last updated:** december 17, 2025  
**version:** 2.0 (consolidated)

---

## index

| section | what it covers |
|---------|----------------|
| [story content systems](#story-content-systems) | events, decision events, activities |
| [runtime flow](#runtime-flow) | schedule generation, simulation, event firing |
| [escalation systems](#escalation-systems) | heat, discipline, pay tension, reputation, medical, fatigue |
| [content index](#content-index) | all events by category |
| [decision events](#decision-events-ck3-style) | ck3-style pushed decisions |

---

## story content systems

## Story Content Systems

Enlisted has **four** story content systems:

| System | Location | Status | Use Case |
|--------|----------|--------|----------|
| **Events** | `ModuleData/Enlisted/Events/*.json` | CANONICAL | All story content |
| **Decision Events** | `Events/*.json` (category: "decision") | ACTIVE | CK3-style pushed choices |
| **Activities** | `ModuleData/Enlisted/Activities/activities.json` | ACTIVE | Camp location actions |
| **Story Packs** | `ModuleData/Enlisted/StoryPacks/LanceLife/*.json` | DEPRECATED | Legacy system, migrate to Events |

### Source of Truth

```
ModuleData/Enlisted/
├── Events/                    ← CANONICAL for all story content
│   ├── events_decisions.json          (CK3-style decisions)
│   ├── events_player_decisions.json   (Player-initiated)
│   ├── events_escalation_thresholds.json
│   ├── events_duty_*.json             (Duty-specific)
│   ├── events_general.json
│   ├── events_training.json
│   └── ...
├── Activities/
│   └── activities.json        ← Camp activities
└── StoryPacks/LanceLife/      ← DEPRECATED (migrate to Events)
```

---

## Runtime Flow

### Overview

When enlisted, the mod simulates lance life through three interconnected systems:

```
┌─────────────────────────────────────────────────────────────────┐
│                    DAILY TICK (Midnight)                         │
│  CampLifeBehavior.OnDailyTick()                                  │
│  - Updates: LogisticsStrain, MoraleShock, PayTension             │
│  - Decays: Heat (-1/7 days), Discipline (-1/14 days)             │
└─────────────────────────────────────────────────────────────────┘
                              ↓
┌─────────────────────────────────────────────────────────────────┐
│                    HOURLY TICK                                   │
│  ScheduleBehavior.OnHourlyTick()                                 │
│  - Hour 0: Degrade lance needs (context-aware)                   │
│  - Hour 6: Generate new schedule (lord objectives)               │
│  - Block transitions: Auto-start activities                      │
└─────────────────────────────────────────────────────────────────┘
                              ↓
┌─────────────────────────────────────────────────────────────────┐
│                    EVENT EVALUATION                              │
│  DecisionEventBehavior.OnHourlyTick()                            │
│  - Evaluates at: 8, 14, 20 hours                                 │
│  - 8 protection layers (cooldowns, limits, flags)                │
│  - Fires decision events via ModernEventPresenter                │
└─────────────────────────────────────────────────────────────────┘
```

### Key Code Files

| System | File |
|--------|------|
| Schedule Generation | `src/Features/Schedule/Behaviors/ScheduleBehavior.cs` |
| Lord Objective Analysis | `src/Features/Schedule/Core/ArmyStateAnalyzer.cs` |
| Camp Simulation | `src/Features/Camp/CampLifeBehavior.cs` |
| Event Evaluation | `src/Features/Lances/Events/LanceLifeEventsAutomaticBehavior.cs` |
| Decision Events | `src/Features/Lances/Events/Decisions/DecisionEventBehavior.cs` |
| Event Presentation | `src/Features/Lances/UI/ModernEventPresenter.cs` |

### Lord Objectives -> Schedule

The lord's current activity determines daily schedules:

| Lord Objective | Schedule Focus | Example Blocks |
|----------------|---------------|----------------|
| `Besieging` | Combat prep | Training, Weapons Check, Guard Duty |
| `Defending` | Fortification | Guard, Patrol, Rest |
| `Traveling` | March support | March, Scout, Rest |
| `Resting` | Recovery | Rest, Training, Social |
| `Fleeing` | Survival | March, Guard, Rest |

---

## Escalation Systems

### Quick Reference

| System | Range | Decay | Key Thresholds |
|--------|-------|-------|----------------|
| **Heat** | 0-10 | -1 per 7 days | 3: Warning, 5: Shakedown, 7: Audit, 10: Exposed |
| **Discipline** | 0-10 | -1 per 14 days | 3: Extra Duty, 5: Hearing, 7: Blocked, 10: Discharge |
| **Pay Tension** | 0-100 | Event-driven | 40: Desperate Actions, 60: Free Desertion, 80: Mutiny |
| **Lance Reputation** | -50 to +50 | ±1 per 14 days to 0 | ±20: Trusted/Isolated, ±40: Bonded/Sabotage |
| **Medical Risk** | 0-5 | -1 per day (rest) | 3: Worsening, 4: Complication, 5: Emergency |
| **Fatigue** | 0-30+ | Natural recovery | Blocks activities when high |

### Heat (Corruption Attention)

**What it tracks:** Attention from corrupt activities (skimming, bribes, theft)

| Level | Value | State | Effect |
|-------|-------|-------|--------|
| Clean | 0 | No attention | None |
| Watched | 1-2 | Mild suspicion | Warning text |
| Noticed | 3-4 | Active suspicion | `heat_warning` event |
| Hot | 5-6 | Investigation likely | `heat_shakedown` event |
| Burning | 7-9 | Active investigation | `heat_audit` event |
| Exposed | 10 | Caught | `heat_exposed` event |

**Sources:** Corruption choices (+1 to +4), witnessed corruption (+1-2 bonus)  
**Reduction:** Clean behavior options (-1 to -2), time decay

### Discipline

**What it tracks:** Military infractions and rule-breaking

| Level | Value | State | Effect |
|-------|-------|-------|--------|
| Exemplary | 0 | Model soldier | Promotion bonus |
| Good | 1-2 | Minor issues | None |
| Noticed | 3-4 | Pattern emerging | `discipline_extra_duty` |
| Problem | 5-6 | Formal record | `discipline_hearing` |
| Serious | 7-9 | Major concerns | `discipline_blocked` |
| Critical | 10 | Court-martial risk | `discipline_discharge` |

**Sources:** Insubordination, missed duty, fighting, gambling problems  
**Reduction:** Extra duty, good behavior, time decay

### Pay Tension

**What it tracks:** Financial stress when pay is late

| Level | Value | State | Available Actions |
|-------|-------|-------|-------------------|
| Normal | 0-19 | All good | Standard options |
| Concerned | 20-39 | Worried | Grumbling starts |
| Desperate | 40-59 | Financial crisis | Corruption/Loyalty paths unlock |
| Critical | 60-79 | Breaking point | Free desertion available |
| Mutiny | 80-100 | Open revolt | Mutiny events fire |

### Lance Reputation

**What it tracks:** Standing with fellow lance members

| Level | Value | State | Effect |
|-------|-------|-------|--------|
| Bonded | +40 to +50 | Brothers in arms | Event: `lance_bonded`, cover fire |
| Trusted | +20 to +39 | Reliable | Event: `lance_trusted`, support |
| Neutral | -19 to +19 | Just another soldier | Standard interactions |
| Distant | -20 to -39 | Outsider | Event: `lance_isolated`, ignored |
| Hostile | -40 to -50 | Enemy within | Event: `lance_sabotage`, sabotaged |

---

## Decision Events (CK3-Style)

decision events push narrative choices to the player during gameplay. see `docs/StoryBlocks/decision-events-spec.md` for full specification.

### Quick Summary

| Feature | Description |
|---------|-------------|
| **Delivery** | Automatic push at 8am, 2pm, 8pm |
| **Pacing** | Max 2/day, 8/week, 6hr minimum gap |
| **Protections** | 9 layers: cooldowns, flags, tier gates, exclusions |
| **Tier Gates** | Phase 6: T1 peasants don't hunt with lords |
| **Chains** | One decision can trigger follow-up events |
| **Narrative Sources** | lord, lance_leader, lance_mate, situation |

### Implementation Status

| Phase | Status |
|-------|--------|
| Phase 1: Core Infrastructure | [x] Complete |
| Phase 1.5: Activity-Aware | [x] Complete |
| Phase 2: Pacing System | [x] Complete |
| Phase 3: Event Chains | [x] Complete |
| Phase 4: Player Menu | [x] Complete |
| Phase 5: Loot System | IN PROGRESS Optional |
| Phase 6: Tier-Based Narrative | [x] Complete |
| Phase 7: Content Creation | IN PROGRESS Ongoing |
| Phase 8: News Integration | IN PROGRESS Documented |
| Phase 9: Testing & Debug | IN PROGRESS Pending |

---

## Content Index

### Events by Category

| Category | Count | Files |
|----------|-------|-------|
| **Decision** | 14 | `events_decisions.json`, `events_player_decisions.json` |
| **Duty** | 50 | `events_duty_*.json` (10 duty types × 5 events) |
| **Threshold** | 16 | `events_escalation_thresholds.json` |
| **General** | 18 | `events_general.json` |
| **Onboarding** | 9 | `events_onboarding.json` |
| **Training** | 16 | `events_training.json` |
| **Pay** | 14 | `events_pay_loyal.json`, `events_pay_mutiny.json`, `events_pay_tension.json` |
| **Promotion** | 5 | `events_promotion.json` |
| **TOTAL** | ~142 | |

### Decision Events (Current)

| Event ID | Source | Tier | Description |
|----------|--------|------|-------------|
| `decision_lord_hunt_invitation` | lord | T5+ | Lord invites you hunting |
| `decision_lance_mate_dice` | lance_mate | T1+ | Dice game invitation |
| `decision_training_offer` | lance_leader | T1+ | Extra training offer |
| `decision_scout_assignment` | lance_leader | T2+ | Special scouting mission |
| `decision_medic_emergency` | situation | T2+ | Medical crisis |
| `decision_quartermaster_deal` | situation | T3+ | Merchant proposition |
| `decision_lance_mate_favor` | lance_mate | T1+ | Loan request (chain start) |
| `decision_lance_mate_favor_repayment` | lance_mate | T1+ | Chain: Debt repaid |
| `decision_lance_mate_favor_gratitude` | lance_mate | T1+ | Chain: Small thanks |
| `player_organize_dice_game` | situation | T1+ | Player-initiated: Dice |
| `player_request_training` | situation | T2+ | Player-initiated: Training |
| `player_visit_wounded` | situation | T1+ | Player-initiated: Visit wounded |
| `player_petition_lord` | lord_direct | T3+ | Player-initiated: Petition |
| `player_write_letter` | situation | T1+ | Player-initiated: Letter home |

### Activities (Camp Locations)

| Location | Activity Count | Example |
|----------|---------------|---------|
| `training_grounds` | 7 | Formation drill, Sparring, Weapons practice |
| `mess_tent` | 3 | Eat, Socialize, Rest |
| `quartermaster` | 2 | Check supplies, Trade |
| `medical_tent` | 2 | Rest, Visit wounded |

---

## Adding New Content

### New Decision Event

1. Add to `ModuleData/Enlisted/Events/events_decisions.json` or `events_player_decisions.json`
2. Include required fields:
   ```json
   {
     "id": "decision_your_event",
     "category": "decision",
     "narrative_source": "lance_leader|lord|lance_mate|situation",
     "delivery": { "method": "automatic|player_initiated", "channel": "inquiry" },
     "triggers": { "all": ["is_enlisted"], "time_of_day": ["morning"] },
     "requirements": { "tier": { "min": 1, "max": 999 } },
     "timing": { "cooldown_days": 7, "priority": "normal" },
     "titleFallback": "Event Title",
     "bodyFallback": "Event description...",
     "options": [...]
   }
   ```
3. For tier gating, set `narrative_source` and `requirements.tier.min` appropriately

### New Escalation Threshold Event

1. Add to `events_escalation_thresholds.json`
2. Set trigger token: `heat_3`, `discipline_5`, etc.
3. Include recovery options that reduce the escalation value

### New Activity

1. Add to `ModuleData/Enlisted/Activities/activities.json`
2. Reference in appropriate location category
3. Link to events via `current_activity:X` trigger tokens

---

## File Consolidation History

This document is the consolidated source of truth for StoryBlocks-related systems. Older draft/reference files were removed after consolidation to avoid drift.

for active decision events development, see `docs/StoryBlocks/decision-events-spec.md`.

