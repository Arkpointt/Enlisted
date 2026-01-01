# Content Orchestrator: Remaining Implementation Prompts

**Summary:** Copy-paste prompts for unimplemented phases of the Content Orchestrator. Phases 1-6F, 9, and 10 are COMPLETE. This document contains prompts for remaining work only.

**Status:** 📋 Reference (Unimplemented Phases Only)  
**Last Updated:** 2025-12-31  
**Related Docs:** [Content Orchestrator Plan](content-orchestrator-plan.md), [Content System Architecture](../Features/Content/content-system-architecture.md), [BLUEPRINT](../BLUEPRINT.md)

---

## Completed Phases (Reference Only)

The following phases are **IMPLEMENTED** and documented in [Content System Architecture](../Features/Content/content-system-architecture.md):

| Phase | Description | Status |
|-------|-------------|--------|
| Phase 1 | Foundation (orchestrator infrastructure) | ✅ Complete |
| Phase 2 | Content Selection Integration | ✅ Complete |
| Phase 3 | Cutover & Migration | ✅ Complete |
| Phase 4 | Orders Integration (85 events) | ✅ Complete |
| Phase 4.5 | Native Effect Integration | ✅ Complete |
| Phase 5 | UI Integration (forecasts, main menu) | ✅ Complete |
| Phase 5.5 | Camp Background Simulation | ✅ Complete |
| Phase 6A-F | Camp Life Simulation (29 opportunities) | ✅ Complete |
| Phase 9 | Decision Scheduling System | ✅ Complete |
| Phase 10 | Order Forecasting & Warnings | ✅ Complete |

**If you need context on completed phases**, read:
- [Content System Architecture](../Features/Content/content-system-architecture.md) - Core orchestrator
- [Camp Life Simulation](camp-life-simulation.md) - Opportunity generation
- [Camp Background Simulation](camp-background-simulation.md) - Autonomous company

---

## Remaining Work

| Phase | Description | Model | Time | Status |
|-------|-------------|-------|------|--------|
| [Phase 6G](#phase-6g-create-missing-decisions) | Create 26 missing camp decisions | Sonnet 4 | 3-4h | ⛔ **BLOCKING** |
| [Phase 7](#phase-7-content-variants) | Content variants (JSON-only) | Sonnet 4 | 30-60m | ⏸️ Future |
| [Phase 8](#phase-8-progression-system) | Progression System framework | Opus 4 | 2-3h | ⏸️ Future |
| [Phase 9](#phase-9-decision-scheduling) | Decision scheduling system | Sonnet 4 | 2-3h | ✅ **COMPLETE** |
| [Phase 10](#phase-10-order-forecasting) | Order warnings & forecasting | Sonnet 4 | 2-3h | ✅ **COMPLETE** |

**Critical Path:** Phase 6G (create missing decisions) → Phase 7-8 (future enhancements)

---

## Phase 6G: Create Missing Decisions

**Goal:** Create 26 missing camp decisions that opportunities reference

**Status:** ⛔ **BLOCKING Phase 9** - Must complete before decision scheduling  
**Priority:** Critical  
**Model:** Claude Sonnet 4 (JSON content creation)  
**Estimated Time:** 3-4 hours

### Problem Statement

Phase 6 created 29 camp opportunities in `ModuleData/Enlisted/Opportunities/camp_opportunities.json`, but only 3 target decisions exist. The opportunities reference decisions via `targetDecision` field:

```json
{
  "id": "opp_weapon_drill",
  "targetDecision": "dec_training_drill"  // ← Decision doesn't exist
}
```

**Two-Layer Architecture:**
- **Opportunities** - Orchestrator-curated menu items (fitness scoring, order compatibility, detection logic)
- **Decisions** - The actual event with options, outcomes, result text

### Current State

- ✅ Opportunities exist: 29
- ✅ Decisions exist: 3 (dec_maintain_gear, dec_write_letter, dec_gamble_high)
- ❌ Decisions missing: 26
- ❌ Old static decisions: 35 (pre-orchestrator, need deletion)

### Required Fix

**Step 0: Delete Old System**
1. Open `ModuleData/Enlisted/Decisions/decisions.json`
2. Delete all 35 old static decisions
3. Keep only: dec_maintain_gear, dec_write_letter, dec_gamble_high

**Step 1: Create 26 New Decisions**

Each decision needs:
- 2-3 options with meaningful choices
- Clear tooltips explaining consequences
- Appropriate costs/rewards/effects
- Light RP flavor (not heavy narrative)
- Culture-aware placeholders ({SERGEANT}, {LORD_NAME}, etc.)

**Step 2: Validate**
```powershell
python tools/events/validate_events.py
cd C:\Dev\Enlisted\Enlisted
dotnet build -c "Enlisted RETAIL" /p:Platform=x64
```

### Missing Decisions List

**Training (5 decisions):**
- dec_training_drill - Weapon training session
- dec_training_spar - Practice bout with fellow soldier
- dec_training_formation - Formation drill practice
- dec_training_veteran - Learn from experienced soldier
- dec_training_archery - Archery practice

**Social (6 decisions):**
- dec_social_stories - Share stories around fire
- dec_tavern_drink - Drinks with fellow soldiers
- dec_social_storytelling - Tell a tale from your past
- dec_drinking_contest - Drinking competition
- dec_social_singing - Join the singing
- dec_arm_wrestling - Arm wrestling match

**Economic (4 decisions):**
- dec_gamble_cards - Card game
- dec_gamble_dice - Dice game
- dec_forage - Gather supplies from area
- dec_work_repairs - Help with camp repairs

**Recovery (5 decisions):**
- dec_rest_sleep - Get some sleep
- dec_help_wounded - Assist wounded soldiers
- dec_prayer - Quiet prayer or meditation
- dec_rest_short - Brief rest break
- dec_meditate - Meditate on recent events

**Special (5 decisions):**
- dec_officer_audience - Request meeting with officer
- dec_baggage_access - Visit baggage train
- dec_mentor_recruit - Help train new recruit
- dec_volunteer_extra - Volunteer for extra duty
- dec_night_patrol - Volunteer for night patrol

### Implementation Prompt

```
I need you to create 26 missing camp decisions for the Enlisted mod.

═══════════════════════════════════════════════════════════════════════════════
CONTEXT
═══════════════════════════════════════════════════════════════════════════════

Phase 6 created 29 camp opportunities, but only 3 target decisions exist. 
I need to create the 26 missing decisions.

CRITICAL CONSTRAINTS:
- Each decision: 2-3 options maximum
- Clear tooltips explaining what each option does
- Light RP moments (not heavy narrative)
- Culture-aware placeholders: {SERGEANT}, {NCO}, {LORD_NAME}, {OFFICER_RANK}
- Follow existing decision patterns from dec_maintain_gear

═══════════════════════════════════════════════════════════════════════════════
FILES TO READ FIRST
═══════════════════════════════════════════════════════════════════════════════

1. docs/BLUEPRINT.md - Coding standards, tooltip requirements
2. ModuleData/Enlisted/Decisions/decisions.json - Current decisions
3. ModuleData/Enlisted/Opportunities/camp_opportunities.json - See targetDecision references
4. docs/Features/Content/event-system-schemas.md - Decision schema
5. docs/AFEATURE/content-orchestrator-plan.md - Context on Phase 6G

═══════════════════════════════════════════════════════════════════════════════
STEP 1: DELETE OLD DECISIONS
═══════════════════════════════════════════════════════════════════════════════

Open ModuleData/Enlisted/Decisions/decisions.json and:
1. Delete ALL 35 old static decisions
2. KEEP ONLY these 3:
   - dec_maintain_gear
   - dec_write_letter
   - dec_gamble_high

═══════════════════════════════════════════════════════════════════════════════
STEP 2: CREATE 26 NEW DECISIONS
═══════════════════════════════════════════════════════════════════════════════

For each missing decision from the list below, create a full decision with:
- titleId, title (fallback)
- setupId, setup (fallback)
- 2-3 options with:
  - textId, text (fallback)
  - tooltip (REQUIRED - cannot be null)
  - costs/effects
  - resultTextId, resultText (fallback)
  - resultFailureTextId, resultFailureText if risky

MISSING DECISIONS:

**Training:**
- dec_training_drill
- dec_training_spar
- dec_training_formation
- dec_training_veteran
- dec_training_archery

**Social:**
- dec_social_stories
- dec_tavern_drink
- dec_social_storytelling
- dec_drinking_contest
- dec_social_singing
- dec_arm_wrestling

**Economic:**
- dec_gamble_cards
- dec_gamble_dice
- dec_forage
- dec_work_repairs

**Recovery:**
- dec_rest_sleep
- dec_help_wounded
- dec_prayer
- dec_rest_short
- dec_meditate

**Special:**
- dec_officer_audience
- dec_baggage_access
- dec_mentor_recruit
- dec_volunteer_extra
- dec_night_patrol

═══════════════════════════════════════════════════════════════════════════════
DESIGN PATTERNS
═══════════════════════════════════════════════════════════════════════════════

**Training decisions:**
- Cost: fatigue, time
- Reward: skillXp, traitXp, soldierRep
- Risk: injury chance for intense training

**Social decisions:**
- Cost: time, sometimes gold
- Reward: soldierRep, morale, sometimes gold (gambling)
- Risk: discipline, scrutiny (if caught)

**Economic decisions:**
- Cost: time, fatigue
- Reward: gold, supplies, food
- Risk: scrutiny (foraging), injury (repairs)

**Recovery decisions:**
- Cost: time
- Reward: fatigueRelief, stressRelief
- Some may cost gold (helping wounded, donations)

**Special decisions:**
- Varied costs/rewards based on context
- Higher stakes (officer meetings, patrols)

═══════════════════════════════════════════════════════════════════════════════
TOOLTIP REQUIREMENTS (CRITICAL)
═══════════════════════════════════════════════════════════════════════════════

EVERY option MUST have a tooltip. Format:
"Action + side effects + restrictions"

Examples:
- "Train with weapon. Fatigue cost. Chance of injury."
- "Bet on cards. 50% to win gold. Lose stake if caught."
- "Request audience. Officer rep required. May be denied."

═══════════════════════════════════════════════════════════════════════════════
STEP 3: VALIDATE
═══════════════════════════════════════════════════════════════════════════════

After creating all decisions:

```powershell
# Validate JSON structure
python tools/events/validate_events.py

# Build to check for errors
cd C:\Dev\Enlisted\Enlisted
dotnet build -c "Enlisted RETAIL" /p:Platform=x64
```

═══════════════════════════════════════════════════════════════════════════════
ACCEPTANCE CRITERIA
═══════════════════════════════════════════════════════════════════════════════

[ ] All 35 old static decisions deleted
[ ] All 26 new decisions created
[ ] Each decision has 2-3 options
[ ] EVERY option has a non-null tooltip
[ ] Culture-aware placeholders used throughout
[ ] Validation passes without errors
[ ] Build succeeds
[ ] decisions.json has 29 total decisions (3 kept + 26 new)

═══════════════════════════════════════════════════════════════════════════════
```

---

## Phase 7: Content Variants (Post-Launch)

**Goal:** Add context-aware event variants (JSON-only enhancement)

**Status:** ⏸️ Future Enhancement  
**Priority:** Low  
**Model:** Claude Sonnet 4  
**Estimated Time:** 30-60 minutes per batch

### Overview

No code changes needed. Add variant events to existing JSON files with more specific `requirements.context` filters. Orchestrator automatically selects best-fitting variant.

### Example Pattern

```json
// Base event (always available)
{
  "id": "dec_rest",
  "requirements": { "tier": { "min": 1 } }
}

// Garrison variant (better rest)
{
  "id": "dec_rest_garrison",
  "requirements": {
    "tier": { "min": 1 },
    "context": ["Camp"]
  }
}

// Crisis variant (worse rest)
{
  "id": "dec_rest_exhausted",
  "requirements": {
    "tier": { "min": 1 },
    "context": ["Siege"]
  }
}
```

### Variant Types to Add

- Garrison vs Campaign vs Siege variants
- Culture-specific variants (Empire ≠ Aserai ≠ Vlandia)
- Relationship-aware (friendly QM vs hostile QM)
- Season variants (summer vs winter)
- Time-of-day variants (dawn vs night)

### Process

1. Identify high-traffic decisions/events
2. Create 2-3 variants per event
3. Add to appropriate JSON file
4. Test that orchestrator selects correct variant
5. Iterate based on player feedback

No prompt needed - just add JSON variants incrementally.

---

## Phase 8: Progression System (Future)

**Goal:** Generic CK3-style probabilistic progression tracks

**Status:** ⏸️ Future Enhancement  
**Priority:** Low (framework for future systems)  
**Model:** Claude Opus 4 (complex probability system)  
**Estimated Time:** 2-3 hours

### Overview

A generic system for escalation tracks that progress probabilistically based on daily rolls, conditions, and player choices.

**Example: Medical Risk Track**
```
Player has Medical Risk = 3
  ↓
Daily roll: 1d100 vs (15% base + 10% fatigue + 15% combat + 5% winter) = 45%
  ↓
Success → Fire threshold event (illness onset)
Failure → Try again tomorrow
```

### Schema (Already in event-system-schemas.md)

```json
{
  "track_id": "medical_risk",
  "threshold": 3,
  "base_chance_per_day": 0.15,
  "modifiers": [
    { "condition": "fatigue_high", "modifier": 0.10 },
    { "condition": "recent_combat", "modifier": 0.15 },
    { "condition": "winter_season", "modifier": 0.05 }
  ],
  "threshold_event": "evt_illness_onset"
}
```

### Potential Tracks

- Medical risk → Illness onset
- Desertion risk → Desertion attempt
- Mutiny risk → Mutiny event
- Promotion readiness → Promotion opportunity
- Lord favor → Special assignment

### First Implementation

Medical Progression System (see [Medical Progression System](../Features/Content/medical-progression-system.md)) would be the reference implementation.

**Implementation prompt available on request** - this is future work.

---

## Phase 9: Decision Scheduling ✅ COMPLETE

**Goal:** Allow players to schedule camp activities at specific times

**Status:** ✅ **IMPLEMENTED**  
**Priority:** High - Required for immersive time-aware gameplay  
**Completed:** 2025-12-31

### Implementation Summary

Players can now schedule decisions for specific day phases (Dawn, Midday, Dusk, Night) instead of only doing them immediately. The system includes:

**Implemented Components:**
- `PlayerCommitments.cs` - Tracks scheduled commitments with phase, day, and target decision
- `CampScheduleManager.cs` - Manages daily schedule and applies player commitments to phases
- `ScheduledCommitment` class - Stores opportunity ID, target decision, scheduled phase/day, display text
- Commitment tracking in `CampOpportunityGenerator.GetNextCommitment()`, `GetHoursUntilCommitment()`
- UI integration in `ForecastGenerator.BuildNowText()` - Shows upcoming commitments with countdown
- Phase transition detection - Fires commitments when scheduled time arrives

**How It Works:**
1. Player sees an opportunity (e.g., "spar with fellow soldier")
2. Can choose "DO NOW" or "SCHEDULE FOR [phase]"
3. Commitment tracked in save/load system
4. NOW section shows: "You've committed to sparring match at midday (3h)"
5. When phase arrives, decision fires automatically
6. Can track multiple commitments queued by phase

### Original Implementation Prompt (For Reference Only)

```
I need you to implement the Decision Scheduling system for the Enlisted mod.

═══════════════════════════════════════════════════════════════════════════════
CONTEXT
═══════════════════════════════════════════════════════════════════════════════

The Content Orchestrator (Phases 1-6F) is COMPLETE. Now I need to add a
scheduling system so players can commit to camp activities at specific times.

Example: Player sees opp_training_spar
  - Option 1: "Do this NOW" → Fire decision immediately
  - Option 2: "Schedule for MIDDAY" → Create commitment, reminder when time comes

═══════════════════════════════════════════════════════════════════════════════
READ THESE FIRST
═══════════════════════════════════════════════════════════════════════════════

1. docs/BLUEPRINT.md - Coding standards
2. docs/Features/Content/content-system-architecture.md - Orchestrator architecture
3. docs/AFEATURE/camp-life-simulation.md - Opportunity system
4. docs/AFEATURE/content-orchestrator-plan.md - Phase 9 requirements

═══════════════════════════════════════════════════════════════════════════════
REQUIREMENTS
═══════════════════════════════════════════════════════════════════════════════

1. **Player Commitments**
   - Store: {decisionId, scheduledPhase, targetNPC, location}
   - Persist in save/load
   - Max 3 active commitments at once

2. **Time Phases** (Already exist in WorldStateAnalyzer)
   - Dawn (6am-11am)
   - Midday (12pm-5pm)
   - Dusk (6pm-9pm)
   - Night (10pm-5am)

3. **Commitment Tracking**
   - Check on phase transitions (Orchestrator daily tick)
   - Reminder when phase arrives: "It's time for your sparring match"
   - Options: HONOR (do activity) or BREAK (face consequences)
   - Breaking commitment: -5 soldierRep, scrutiny+3

4. **Forecast Integration**
   - AHEAD section shows scheduled commitments
   - "You're meeting Sergeant Oleg at dusk for weapon training"
   - "You promised to help with repairs at dawn"

5. **UI Changes**
   - When opening an opportunity: Show "DO NOW" and "SCHEDULE FOR [phase]" options
   - AHEAD section lists commitments with countdown
   - Notification when phase arrives

═══════════════════════════════════════════════════════════════════════════════
IMPLEMENTATION TASKS
═══════════════════════════════════════════════════════════════════════════════

1. CREATE PlayerCommitment data model
   - Fields: decisionId, scheduledPhase, targetNPC, createdAt
   - List stored in player state
   - Save/load integration

2. CREATE CommitmentManager.cs
   - TrackCommitment(decisionId, phase, npc)
   - CheckPhaseTransition(currentPhase) → List<Commitment> due
   - BreakCommitment(id) → Apply consequences
   - GetActiveCommitments() → For forecast display

3. MODIFY CampOpportunityGenerator
   - Add "schedule" option to opportunities
   - Present phase selection UI

4. MODIFY ContentOrchestrator
   - Check commitments on phase transitions
   - Fire reminder when commitment due
   - Update forecast generation to include commitments

5. MODIFY EnlistedMenuBehavior
   - AHEAD section shows commitments
   - "Committed to spar at midday (in 3 hours)"

6. ADD Localization
   - Reminder messages
   - Commitment display strings
   - Consequence messages

═══════════════════════════════════════════════════════════════════════════════
ACCEPTANCE CRITERIA
═══════════════════════════════════════════════════════════════════════════════

[ ] Can schedule decisions for future phases
[ ] Reminder fires when phase arrives
[ ] Can honor or break commitment
[ ] Breaking commitment has consequences
[ ] Commitments appear in AHEAD forecast
[ ] Max 3 active commitments enforced
[ ] Save/load persists commitments
[ ] Build succeeds without errors

═══════════════════════════════════════════════════════════════════════════════
```

---

## Phase 10: Order Forecasting & Warnings ✅ COMPLETE

**Goal:** Provide advance warning before orders issue (critical for >> speed playability)

**Status:** ✅ **IMPLEMENTED**  
**Priority:** CRITICAL - Required for fast-forward speeds  
**Completed:** 2025-12-31

### Implementation Summary

Orders now provide advance warnings before issuing, making fast-forward gameplay viable. The system creates warnings 4-8 hours before orders become pending.

**Implemented Components:**
- `OrderState.Imminent` enum - Advance warning state before Pending
- `Order.ImminentTime` and `Order.IssueTime` fields - Track warning period
- `OrderManager.CreateImminentOrder()` - Creates order 4-8 hours before issue
- `OrderManager.GetImminentWarningText()` - Generates warning text for UI
- `OrderManager.GetHoursUntilIssue()` - Countdown until order issues
- `OrderManager.IsOrderImminent()` - Check if warning is active
- `OrderManager.UpdateOrderState()` - Hourly tick transitions Imminent → Pending when IssueTime arrives
- UI integration in `EnlistedMenuBehavior` (lines 2343, 2838) - Shows forecasts in AHEAD and ORDERS sections

**Order Lifecycle:**
```
CreateImminentOrder (4-8h warning)
  ↓
State = Imminent
ImminentTime = now, IssueTime = now + 4-8h
  ↓
UpdateOrderState (hourly tick)
  ↓
now >= IssueTime?
  ↓
TransitionToPending
  ↓
State = Pending (player can accept/decline)
```

**UI Display:**
- AHEAD section: "Sergeant's organizing duty roster" (soft hint)
- ORDERS menu: "Order Assignment: Guard Duty (in 6 hours)" (explicit countdown)
- At high speed (>>), warnings give players time to react

### Original Implementation Prompt (For Reference Only)

```
I need you to implement the Order Forecasting & Warning system for the Enlisted mod.

═══════════════════════════════════════════════════════════════════════════════
CONTEXT
═══════════════════════════════════════════════════════════════════════════════

The Content Orchestrator (Phases 1-6F) is COMPLETE. Orders currently issue
instantly with no warning. At fast-forward speed (>>), this is unplayable.

I need a three-stage warning system:
  FORECAST (24h) → SCHEDULED (8h) → URGENT (2h) → ACTIVE

═══════════════════════════════════════════════════════════════════════════════
READ THESE FIRST
═══════════════════════════════════════════════════════════════════════════════

1. docs/BLUEPRINT.md - Coding standards
2. docs/Features/Content/content-system-architecture.md - Orchestrator architecture
3. docs/AFEATURE/order-progression-system.md - Order system
4. docs/AFEATURE/content-orchestrator-plan.md - Phase 10 requirements

═══════════════════════════════════════════════════════════════════════════════
REQUIREMENTS
═══════════════════════════════════════════════════════════════════════════════

1. **Warning States**
   ```
   OrderWarningState enum:
     - None: No order planned
     - Forecast: 24h warning ("Something's coming")
     - Scheduled: 8h warning ("You're assigned to X")
     - Urgent: 2h warning ("Report soon")
     - Pending: Player must accept/decline
     - Active: Order in progress
   ```

2. **Planning System**
   - ContentOrchestrator.PlanNext24Hours()
   - Runs at dawn (6am) daily tick
   - Analyzes world state: Should order issue in next 24 hours?
   - Creates FORECAST state if conditions met

3. **Warning Display**
   - Main Menu AHEAD: Soft hints ("Sergeant's been making lists")
   - ORDERS Menu: Explicit warnings with countdown
   - Forecast Section: "Order Assignment: Guard Duty (6 hours)"

4. **Fast Travel Handling**
   - Fast travel past SCHEDULED → Auto-accept order
   - Fast travel past PENDING → Auto-decline with -5 rep penalty
   - Warning at 18h if no response

5. **Configuration** (already in config)
   ```json
   {
     "order_scheduling": {
       "warning_hours_24": 24,
       "warning_hours_8": 8,
       "warning_hours_2": 2
     }
   }
   ```

═══════════════════════════════════════════════════════════════════════════════
IMPLEMENTATION TASKS
═══════════════════════════════════════════════════════════════════════════════

1. ADD OrderWarningState enum
   - Define all 6 states
   - Add to order data model

2. MODIFY OrderManager
   - Track warning state per order
   - Update state based on time elapsed
   - Provide WarningState getter

3. CREATE OrderPlanningSystem.cs
   - PlanNext24Hours(WorldSituation)
   - Analyze world state
   - Determine if order should issue soon
   - Create FORECAST state

4. MODIFY ContentOrchestrator
   - Call OrderPlanningSystem at dawn tick
   - Generate forecast text based on warning state
   - Provide warnings to UI

5. MODIFY EnlistedMenuBehavior
   - AHEAD section shows order warnings
   - ORDERS menu shows countdown
   - Different text for each warning state

6. HANDLE Fast Travel
   - Check warning state on travel completion
   - Auto-accept if past SCHEDULED
   - Auto-decline with penalty if past PENDING

7. ADD Localization
   - Warning messages for each state
   - Forecast text variants
   - Auto-accept/decline notifications

═══════════════════════════════════════════════════════════════════════════════
WARNING TEXT EXAMPLES
═══════════════════════════════════════════════════════════════════════════════

**FORECAST (24h):**
- "Sergeant's been organizing the duty roster."
- "Word is assignments are coming tomorrow."
- "Officers are planning something."

**SCHEDULED (8h):**
- "You're assigned to guard duty. Report by sunset."
- "Patrol duty scheduled for this evening."

**URGENT (2h):**
- "Guard duty starts soon. Prepare now."
- "Report for patrol in two hours."

**AUTO-ACCEPT:**
- "You were assigned to guard duty while traveling."

**AUTO-DECLINE:**
- "You missed an order assignment. Command is displeased. (-5 rep)"

═══════════════════════════════════════════════════════════════════════════════
TESTING SCENARIOS
═══════════════════════════════════════════════════════════════════════════════

1. Normal speed (×1): Warnings add anticipation, feel natural
2. Fast speed (××): Warnings provide reaction time
3. Very fast (>>): CRITICAL - warnings prevent missing orders entirely
4. Fast travel past warning: Auto-accept/decline works correctly

═══════════════════════════════════════════════════════════════════════════════
ACCEPTANCE CRITERIA
═══════════════════════════════════════════════════════════════════════════════

[ ] FORECAST state creates 24h in advance
[ ] SCHEDULED state at 8h, URGENT at 2h
[ ] Warnings appear in AHEAD section
[ ] ORDERS menu shows countdown
[ ] Fast travel past SCHEDULED auto-accepts
[ ] Fast travel past PENDING auto-declines with penalty
[ ] Works smoothly at >> speed
[ ] Build succeeds without errors

═══════════════════════════════════════════════════════════════════════════════
```

---

## Quick Reference: Build & Test

```powershell
# Add new files to Enlisted.csproj (old-style csproj requires manual entries)
# Example:
# <Compile Include="src\Features\Content\NewClass.cs"/>

# Build
cd C:\Dev\Enlisted\Enlisted
dotnet build -c "Enlisted RETAIL" /p:Platform=x64

# Validate events (JSON structure check)
python tools/events/validate_events.py

# Check logs
# Location: <BannerlordInstall>\Modules\Enlisted\Debugging\enlisted.log
# Look for: [Orchestrator], [Orders], [Content] categories
```

---

**Last Updated:** 2025-12-31  
**Maintained By:** Project AI Assistant
