# Content Orchestrator: Implementation Status & Future Work

**Summary:** The Content Orchestrator world-state-driven system is FULLY IMPLEMENTED (Phases 1-6F complete). The Order Progression System is IMPLEMENTED. This document tracks remaining future enhancements that would improve the system but are not required for core functionality.

**Status:** ✅ **Core Systems Complete** - All critical functionality implemented  
**Priority:** Phase 6G (Recommended), then Phases 9-10 (Enhancement), Phase 7-8 (Future)  
**Last Updated:** 2025-12-31
**Related Docs:** [Content System Architecture](../Features/Content/content-system-architecture.md), [Camp Life Simulation](camp-life-simulation.md), [Camp Background Simulation](camp-background-simulation.md), [Order Progression System](order-progression-system.md)

---

## Implementation Status

### ✅ COMPLETED (2025-12-30)

**Phases 1-6F** are fully implemented and documented in [Content System Architecture](../Features/Content/content-system-architecture.md):

- **Phase 1:** Foundation (ContentOrchestrator, WorldStateAnalyzer, SimulationPressureCalculator, PlayerBehaviorTracker)
- **Phase 2:** Content Selection Integration (fitness scoring, context mapping)
- **Phase 3:** Cutover & Migration (removed schedule-driven pacing, activity level system)
- **Phase 4:** Orders Integration (85/85 order events, world-state weighting)
- **Phase 4.5:** Native Effect Integration (IncidentEffectTranslator, trait mapping)
- **Phase 5:** UI Integration (forecast generation, main menu restructure, camp opportunities)
- **Phase 5.5:** Camp Background Simulation (autonomous company life, news feed integration)
- **Phase 6A-F:** Camp Life Simulation (29 opportunities, learning system, time-of-day awareness, info caching, integration testing)

**Key Files Created:**
- `src/Features/Content/ContentOrchestrator.cs`
- `src/Features/Content/WorldStateAnalyzer.cs`
- `src/Features/Content/SimulationPressureCalculator.cs`
- `src/Features/Content/PlayerBehaviorTracker.cs`
- `src/Features/Content/IncidentEffectTranslator.cs`
- `src/Features/Content/TraitMilestoneTracker.cs`
- `src/Features/Camp/CompanySimulationBehavior.cs`
- `src/Features/Camp/CampOpportunityGenerator.cs`
- `src/Features/Camp/Models/` (WorldSituation, SimulationPressure, PlayerPreferences, etc.)

**Config Added:**
- `orchestrator` section in `enlisted_config.json`
- `native_trait_mapping` section
- Activity modifiers (Quiet/Routine/Active/Intense)

### ⛔ BLOCKING: Phase 6G - Create Missing Decisions & Medical Migration

**Problem:** Phase 6 created 29 camp opportunities, but only 3 target decisions exist. 26 decisions are missing. Additionally, medical care uses a separate menu system instead of the unified decision system.

**Two-Layer Architecture:**
```
OPPORTUNITIES (menu display)         DECISIONS (event content)
─────────────────────────────────    ────────────────────────────
camp_opportunities.json              decisions.json
  opp_weapon_drill                     dec_training_drill ← MISSING
    targetDecision: "dec_training_drill"
```

**Current State:**
- ✅ Opportunities: 29 defined
- ✅ Decisions exist: 3 (dec_maintain_gear, dec_write_letter, dec_gamble_high)
- ❌ Decisions missing: 26
- ❌ Old static decisions: 35 (pre-orchestrator, need deletion)
- ❌ Medical Tent: Separate menu system (should be decisions)

**Required Fix (Blocks Phase 9):**

1. **Delete Old System** - Remove 35 pre-orchestrator static decisions from `decisions.json`
2. **Create 26 New Decisions** - Match `targetDecision` IDs from opportunities
3. **Migrate Medical Care** - Replace Medical Tent menu with 4 medical decisions
4. **Delete Medical Menu** - Remove `EnlistedMedicalMenuBehavior.cs` (535 lines)
5. **Design Pattern** - 2-3 options, light RP moments, clear tooltips, consequences

**Missing Decisions List:**

| Category | Missing IDs |
|----------|-------------|
| Training (5) | dec_training_drill, dec_training_spar, dec_training_formation, dec_training_veteran, dec_training_archery |
| Social (6) | dec_social_stories, dec_tavern_drink, dec_social_storytelling, dec_drinking_contest, dec_social_singing, dec_arm_wrestling |
| Economic (4) | dec_gamble_cards, dec_gamble_dice, dec_forage, dec_work_repairs |
| Recovery (5) | dec_rest_sleep, dec_help_wounded, dec_prayer, dec_rest_short, dec_meditate |
| Special (5) | dec_officer_audience, dec_baggage_access, dec_mentor_recruit, dec_volunteer_extra, dec_night_patrol |
| **Medical (4)** | **dec_medical_surgeon, dec_medical_rest, dec_medical_herbal, dec_medical_emergency** |

**Medical Migration Benefits:**
- Consolidate all camp activities into Decisions menu (consistent UX)
- Delete 535 lines of menu management code
- Better integration with content orchestrator and world state
- Reduce maintenance burden (one system instead of two)

**Estimated Time:** 5-6 hours (3-4 for decisions + 2-3 for medical migration)

**See:** [Medical Care Migration Plan](medical-care-migration.md) for detailed specifications

---

### ⛔ BLOCKING: Phase 6H - Medical System Orchestration

**Goal:** Make the medical system fully world-state-driven. Replace random/manual illness with intelligent orchestrator control based on Medical Risk, fatigue, and campaign context.

**Status:** ❌ Not Implemented  
**Priority:** High - Required after Phase 6G  
**Blocks:** None (enhances existing systems)  
**Estimated Time:** 2 hours

**Current State:**
- ✅ Medical conditions tracked (`PlayerConditionBehavior`)
- ✅ Medical Risk escalation tracked (`EscalationManager`)
- ❌ Illness onset is random/manual (not orchestrated)
- ❌ Medical care not integrated with camp opportunities
- ❌ No forecast warnings for Medical Risk
- ❌ Medical pressure not part of world state

**Requirements:**

1. **Medical Pressure Tracking**
   - Add `GetMedicalPressure()` to `SimulationPressureCalculator`
   - Track: Medical Risk, untreated days, fatigue history, health percent
   - Return pressure level: None/Low/Moderate/High/Critical

2. **Camp Opportunities Integration**
   - Create `opp_seek_treatment` (appears when has condition)
   - Create `opp_emergency_care` (forced when severe condition)
   - Create `opp_medical_rest` (rest option for recovery)
   - Priority boosted based on severity

3. **Illness Onset Events**
   - Create `illness_onset.json` with fever and infection events
   - Base chance scales with Medical Risk (5% per level)
   - Context modifiers: winter +8%, siege +12%, fatigue +10%
   - Triggered by orchestrator daily check

4. **ContentOrchestrator Daily Check**
   - Add `CheckMedicalPressure()` to daily tick
   - Force emergency opportunity if critical
   - Boost treatment priority if high pressure
   - Roll for illness onset if Medical Risk >= 3

5. **Forecast Integration**
   - Show condition recovery status in AHEAD
   - Show Medical Risk warnings in CONCERNS
   - Show exhaustion warnings (contributes to illness)
   - Example: "Medical Risk: 4/5. Illness likely without rest."

6. **World State Integration**
   - Add `MedicalPressure`, `RequiresMedicalCare`, `HasCriticalCondition` to `WorldSituation`
   - Calculate in `WorldStateAnalyzer`
   - Use for content filtering and pacing

**Implementation Files:**
- `src/Features/Content/SimulationPressureCalculator.cs` (add medical tracking)
- `src/Features/Content/ContentOrchestrator.cs` (add daily medical check)
- `src/Features/Content/ForecastGenerator.cs` (add medical forecast)
- `src/Features/Content/WorldStateAnalyzer.cs` (add to situation)
- `ModuleData/Enlisted/Config/camp_opportunities.json` (add 3 medical opportunities)
- `ModuleData/Enlisted/Events/illness_onset.json` (new file)

**Benefits:**
- Illness becomes predictable and preventable (manage Medical Risk)
- Natural escalation from neglecting health
- Proactive suggestions for medical care
- Fully integrated with world-state-driven content
- Player has agency (rest to avoid illness vs push through)

**Example Flow:**
```
Day 1: Player exhausted (fatigue 4/24), Medical Risk +1
Day 2: Still exhausted, Medical Risk +1 (now 2/5)
Day 3: Still exhausted, Medical Risk +1 (now 3/5)
  → Orchestrator: Roll 15% base + 10% fatigue = 25% illness chance
  → Forecast: "Medical Risk: 3/5. Rest recommended."
  → Camp Opportunity: "Take a Rest Day" appears

Day 4: Player ignores, still exhausted, Medical Risk +1 (now 4/5)
  → Orchestrator: Roll 20% base + 10% fatigue + 12% siege = 42% chance
  → SUCCESS: Illness onset event fires
  → Player gets camp fever (7 days, moderate severity)
  
Day 5: Condition active, no treatment
  → Camp Opportunity: "Seek Medical Care" priority boosted
  → Forecast: "Recovering from camp fever (7 days, untreated)."
  → CONCERNS: "Medical: Untreated condition. Seek care."
```

---

## ❌ CRITICAL: Phases 9-10 (Must Implement)

### Phase 9: Decision Scheduling

**Goal:** Allow players to commit to camp activities at specific times (phases).

**Status:** ❌ Not Implemented  
**Priority:** High - Required for immersive time-aware gameplay  
**Blocks:** None (standalone)  
**Estimated Time:** 2-3 hours

**Requirements:**

1. **Player Commitments**
   - Player selects decision: "Do this NOW" or "Schedule for [time]"
   - Commitment stored: `{decisionId, scheduledPhase, targetNPC, location}`
   - Appears in forecast: "You've agreed to spar with Viktor at noon"

2. **Time Phases (Existing)**
   - Dawn (6am-11am), Midday (12pm-5pm), Dusk (6pm-9pm), Night (10pm-5am)
   - Already tracked by `WorldStateAnalyzer.GetDayPhase()`

3. **Commitment Tracking**
   - Store in player state
   - Check on phase transitions
   - Reminder when phase arrives: "It's time for your sparring match"
   - Option to honor or break commitment (consequences)

4. **Forecast Integration**
   - AHEAD section shows scheduled commitments
   - "You're meeting Sergeant Oleg at dusk"
   - "You promised to help with equipment at dawn"

**Benefits:**
- Immersive time awareness
- Builds anticipation
- Consequence for broken promises (reputation/discipline)
- Natural pacing (player chooses when things happen)

---

### Phase 10: Order Forecasting & Warnings

**Goal:** Provide advance warning before orders issue so players have time to react at fast-forward speeds.

**Status:** ❌ Not Implemented  
**Priority:** CRITICAL - Required for >> speed playability  
**Blocks:** None (standalone)  
**Estimated Time:** 2-3 hours

**Problem:**

At fast-forward speed (>>), orders appear instantly with no time to prepare. Players miss the chance to:
- Complete current activities
- Adjust equipment
- Use QM before duty starts
- Read what's coming

**Solution: Three Warning States**

```
FORECAST (24h warning)
  ↓
  Sergeant's been making lists. Duty assignment likely tomorrow.
  
SCHEDULED (8h warning)
  ↓
  You're assigned to guard duty. Report by sunset.
  
PENDING (2h warning)
  ↓
  Guard duty starts soon. Prepare now.
  
ACTIVE (issued)
  ↓
  Report for guard duty!
```

**Implementation:**

1. **Planning System** (`ContentOrchestrator.PlanNext24Hours()`)
   - Runs at dawn (6am) daily tick
   - Analyzes world state: Should order issue in next 24 hours?
   - Creates FORECAST state if conditions met
   - Appears in AHEAD text: "Sergeant's been making lists"

2. **Order State System**
```csharp
   public enum OrderWarningState
   {
       None,           // No order planned
       Forecast,       // 24h: "Something's coming"
       Scheduled,      // 8h: "You're assigned to X"
       Urgent,         // 2h: "Report soon"
       Pending,        // Player must accept/decline
       Active          // Order in progress
   }
   ```

3. **Warning Display Locations**
   - **Main Menu AHEAD:** Soft hints ("Sergeant's organizing patrols")
   - **ORDERS Menu:** Explicit warnings with countdown
   - **Forecast Section:** "Order Assignment: Guard Duty (6 hours)"

4. **Fast Travel Handling**
   - Fast travel past SCHEDULED → Auto-accept order
   - Fast travel past PENDING → Auto-decline with -5 rep penalty
   - Warning at 18h if player hasn't responded

5. **Configuration** (already added to config)
   ```json
   {
     "order_scheduling": {
       "warning_hours_24": 24,
       "warning_hours_8": 8,
       "warning_hours_2": 2
     }
   }
   ```

**Benefits:**
- Players can prepare at fast-forward speeds
- Natural tension builds ("Something's coming")
- Time to finish current activities
- Consequence for ignoring warnings
- Immersive command structure (orders aren't instant surprises)

**Testing Scenarios:**
1. Normal speed (×1): Should feel natural, warnings add anticipation
2. Fast speed (××): Warnings provide reaction time
3. Very fast (>>): Critical - without this, orders are missed entirely

---

## ⏸️ FUTURE: Phase 7 - Content Variants

**Goal:** Context-aware event variants selected automatically by orchestrator.

**Status:** ⏸️ Future Enhancement  
**Priority:** Low - Nice to have, not blocking  
**Estimated Time:** 30-60 minutes (JSON-only work)

**How It Works:**

Events can have multiple variants for different situations. Orchestrator selects best-fitting variant based on context.

**Example: Rest Decision**

```json
// Base event (always available)
{
  "id": "dec_rest",
  "requirements": { "tier": { "min": 1 } },
  "costs": { "fatigue": 2 },
  "rewards": { "fatigueRelief": 5 }
}

// Garrison variant (better rest)
{
  "id": "dec_rest_garrison",
  "requirements": {
    "tier": { "min": 1 },
    "context": ["Camp"]
  },
  "costs": { "fatigue": 1 },
  "rewards": { "fatigueRelief": 8 }
}

// Crisis variant (worse rest)
{
  "id": "dec_rest_exhausted",
  "requirements": {
    "tier": { "min": 1 },
    "context": ["Siege"]
  },
  "costs": { "fatigue": 2 },
  "rewards": { "fatigueRelief": 2 }
}
```

**Implementation:**
- No code changes needed (requirements system already filters by context)
- Add variants as JSON files incrementally
- Orchestrator selects most specific matching variant
- Content creators can enhance existing events over time

**Variant Types to Add:**
- Garrison vs Campaign vs Siege variants
- Culture-specific variants (Empire drills ≠ Aserai drills)
- Relationship-aware variants (friendly QM vs hostile QM)
- Season variants (summer training ≠ winter training)

---

## ⏸️ FUTURE: Phase 8 - Progression System

**Goal:** Generic CK3-style probabilistic progression tracks for escalation systems.

**Status:** ⏸️ Future Enhancement  
**Priority:** Low - Framework for future systems  
**Estimated Time:** 2-3 hours (Claude Opus 4 recommended)

**What It Is:**

A generic daily roll system for escalation tracks that progress probabilistically based on conditions and player choices.

**Example: Medical Risk Track**

```
Player has Medical Risk = 3 (threshold reached)
  ↓
Daily roll: 1d100 vs (base_chance + modifiers)
  ├─ Base: 15%
  ├─ Fatigue High: +10%
  ├─ Recent Combat: +15%
  ├─ Winter: +5%
  └─ Total: 45% chance
  ↓
Roll succeeds → Fire threshold event (player gets sick)
Roll fails → Risk remains, try again tomorrow
```

**Generic Schema** (already in event-system-schemas.md):

```json
{
  "track_id": "medical_risk",
  "threshold": 3,
  "base_chance_per_day": 0.15,
  "modifiers": [
    { "condition": "fatigue_high", "modifier": 0.10 },
    { "condition": "recent_combat", "modifier": 0.15 }
  ],
  "threshold_event": "evt_illness_onset"
}
```

**Potential Tracks:**
- Medical risk → Illness onset
- Desertion risk → Desertion attempt
- Mutiny risk → Mutiny event
- Promotion readiness → Promotion opportunity
- Lord favor → Special assignment

**Benefits:**
- CK3-style probabilistic drama
- Contextual modifiers make sense
- Predictable long-term but uncertain short-term
- Player can influence through choices

**First Implementation:**
- Medical progression system (see [Medical Progression System](../Features/Content/medical-progression-system.md))
- Generic framework usable for other tracks

---

## References

### Implemented Systems

For details on implemented features, see:
- **[Content System Architecture](../Features/Content/content-system-architecture.md)** - Complete orchestrator architecture
- **[Camp Life Simulation](camp-life-simulation.md)** - Living camp with opportunities (Phase 6)
- **[Camp Background Simulation](camp-background-simulation.md)** - Autonomous company life (Phase 5.5)
- **[Order Progression System](order-progression-system.md)** - Multi-day order execution
- **[UI Systems Master](../Features/UI/ui-systems-master.md)** - Menu integration and forecasts

### Related Documents

- **[Event System Schemas](../Features/Content/event-system-schemas.md)** - JSON structures for all content types
- **[BLUEPRINT](../BLUEPRINT.md)** - Project architecture and coding standards
- **[Order Events Master](order-events-master.md)** - 85 order events across 16 orders
- **[Orders Content](orders-content.md)** - 16 order definitions

---

## Timeline & Priorities

| Phase | Status | Priority | Estimated Time | Dependencies |
|-------|--------|----------|----------------|--------------|
| 6G - Missing Decisions + Medical Migration | ⛔ BLOCKING | Critical | 5-6 hours | None |
| 6H - Medical System Orchestration | ⛔ BLOCKING | High | 2 hours | Phase 6G |
| 9 - Decision Scheduling | ❌ MUST DO | High | 2-3 hours | Phase 6G |
| 10 - Order Forecasting | ❌ MUST DO | Critical | 2-3 hours | None |
| 7 - Content Variants | ⏸️ Future | Low | 30-60 min | None |
| 8 - Progression System | ⏸️ Future | Low | 2-3 hours | None |

**Total Critical Work:** ~12-14 hours (6G → 6H → 9 → 10)

**Recommended Order:**
1. Phase 6G (create decisions, migrate medical menu to decisions)
2. Phase 6H (integrate medical system with orchestrator)
3. Phase 10 (critical for playability at fast-forward speeds)
4. Phase 9 (enhances immersion with time-aware scheduling)
5. Phase 7-8 (future enhancements)

**Phase 6G Breakdown:**
- Create 26 missing camp decisions (3-4 hours)
- Create 4 medical care decisions (45 min)
- Remove medical menu system (15 min)
- Delete old systems (535 lines of menu code + 35 old decisions)
- Result: Unified decision system, reduced complexity

**Phase 6H Breakdown:**
- Add medical pressure tracking (30 min)
- Create medical camp opportunities (15 min)
- Create illness onset events (30 min)
- Update orchestrator daily check (25 min)
- Update forecast system (20 min)
- Update world state integration (15 min)
- Result: Intelligent, world-state-driven medical system

---

**Last Updated:** 2026-01-01 (Added Phase 6H: Medical System Orchestration)  
**Maintained By:** Project AI Assistant
