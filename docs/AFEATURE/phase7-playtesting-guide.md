# Phase 7 Playtesting & Balance Guide

**Date:** 2025-12-31  
**Status:** Ready for playtesting  
**Prerequisites:** Phases 1-6D-E complete

---

## Overview

Phase 7 completes the camp life simulation integration by wiring together:
- Camp Opportunity Generator → DECISIONS menu
- Player commitment tracking → YOU section display
- Detection logic for risky opportunities when on duty

This document provides guidance for playtesting and balance tuning.

---

## What Was Implemented

### 1. DECISIONS Menu Integration ✅

**What it does:**
- DECISIONS menu (Opportunities section) now shows actual output from CampOpportunityGenerator
- Opportunities are context-aware based on garrison/campaign/siege
- Opportunities vary by day phase (Dawn/Midday/Dusk/Night)
- Learning system adapts to player preferences (70/30 split: learned vs variety)

**How to test:**
1. Start a new game or load an existing save
2. Open the Status menu (Z key)
3. Expand the DECISIONS section
4. Check OPPORTUNITIES subsection for 0-3 context-aware activities
5. Play through several days and note which opportunities appear when

**Expected behavior:**
- **Garrison mornings (Dawn):** 2-3 training opportunities
- **Garrison evenings (Dusk):** 2-3 social opportunities
- **Campaign:** 1-2 opportunities, mostly in evening
- **Siege:** 0-1 opportunities, very limited
- **On duty:** Reduced opportunities (1 max), risky options available

**Balance parameters to tune:**
- Opportunity budget per situation (lines 301-325 in `CampOpportunityGenerator.cs`)
- Fitness score modifiers (lines 438-593)
- Cooldown hours per opportunity type (in `camp_opportunities.json`)

### 2. Commitment Tracking UI ✅

**What it does:**
- When player commits to a scheduled activity, the YOU section shows it
- Displays time until activity ("in 3 hours")
- Persists across save/load

**How to test:**
1. Select an opportunity that schedules an activity (e.g., "Agree to join the card game tonight")
2. Open Status menu and check YOU section
3. Should see "You've committed to an activity in X hours"
4. Save and reload
5. Commitment should still be visible

**Expected behavior:**
- Commitment displays in YOU section immediately after accepting
- Updates when time passes
- Clears when activity fires

**Known limitations:**
- Scheduled activities don't automatically fire yet (Phase 8 feature)
- Manual clearing of commitments not yet implemented

### 3. Detection Logic for Risky Opportunities ✅

**What it does:**
- When player is on duty and attempts a "risky" opportunity, detection check occurs
- Detection chance varies by: base chance, night modifier, officer reputation
- If caught: officer reputation penalty, discipline increase, notification shown
- If caught: order may be compromised (extra discipline penalty)

**How to test:**
1. Accept an order to go on duty
2. Open DECISIONS menu - should see 1 risky opportunity
3. Attempt the risky opportunity
4. Roll for detection:
   - ~25% base chance of being caught
   - ~10% at night (harder to detect)
   - ~15% if officer rep > 70 (trusted soldiers get away with more)
5. If caught, should see popup and escalation penalties applied

**Expected behavior:**
- Risky opportunities only available when on duty
- Detection tooltip shows risk when hovering
- Caught popup appears if detection succeeds
- Officer reputation and discipline penalties apply
- Player can retry different opportunities

**Balance parameters to tune:**
Detection settings in `camp_opportunities.json`:
```json
"detection": {
  "baseChance": 0.25,        // 25% base detection
  "nightModifier": -0.15,    // -15% at night
  "highRepModifier": -0.10   // -10% if rep > 70
}
```

Consequences:
```json
"caughtConsequences": {
  "officerRep": -15,          // Reputation penalty
  "discipline": 2,            // Discipline increase
  "orderFailureRisk": 0.20    // 20% chance order impacted
}
```

---

## Balance Tuning Checklist

### Opportunity Budget (How many opportunities appear)

| Situation | Phase | Current | Feels Right? | Notes |
|-----------|-------|---------|--------------|-------|
| Garrison | Dawn | 3 | ? | Morning drill time |
| Garrison | Midday | 2 | ? | Work period |
| Garrison | Dusk | 3 | ? | Evening leisure |
| Garrison | Night | 1 | ? | Quiet time |
| Campaign | Dawn | 1 | ? | Limited |
| Campaign | Midday | 0 | ? | Marching |
| Campaign | Dusk | 2 | ? | Camp setup |
| Campaign | Night | 0 | ? | Rest |
| Siege Attacking | Any | 1 | ? | Very limited |
| Siege Defending | Any | 0 | ? | No opportunities |
| On Duty | Any | 1 | ? | Risky only |

**Adjust in:** `CampOpportunityGenerator.cs` lines 301-349

### Opportunity Fitness Scoring

Check if opportunities feel appropriate for context:

- [ ] Training appears more in garrison than siege
- [ ] Social events less common during siege
- [ ] Recovery opportunities prioritized when injured/fatigued
- [ ] Economic opportunities appear when player is broke
- [ ] High-engagement opportunities appear more often (learning system)
- [ ] Variety maintained (not same 2-3 opportunities every time)

**Adjust in:** `CampOpportunityGenerator.cs` lines 438-593

### Learning System (Player preference adaptation)

- [ ] After engaging with training 5+ times, training appears more often
- [ ] After ignoring social 5+ times, social appears less often
- [ ] 30% variety maintained (ignored types still occasionally appear)
- [ ] System feels responsive but not aggressive

**Adjust in:**
- `PlayerBehaviorTracker.cs` lines 151-189 (GetLearningModifier)
- Learning weight: 0.7 (70% learned, 30% variety)
- Engagement thresholds: >60% = bonus, <30% = penalty

### Detection System

Test detection feel:

- [ ] Getting caught ~25% of time feels fair
- [ ] Night penalty makes sneaking worth it
- [ ] High rep players feel trusted
- [ ] Consequences feel meaningful but not devastating
- [ ] Players willing to take calculated risks

**Adjust in:** `camp_opportunities.json` detection/caughtConsequences per opportunity

---

## Known Issues & Limitations

### Not Yet Implemented

1. **Scheduled activity firing** - Commitments display but don't automatically fire events at scheduled time (Phase 8)
2. **Order progression integration** - IsPlayerOnDuty() always returns false (line 705-710 in CampOpportunityGenerator.cs)
3. **Baggage window detection** - IsBaggageWindowActive() always returns false (line 718-728)
4. **Player fatigue system** - Hardcoded to 12 (line 242)

### Workarounds

To manually test on-duty detection:
1. Temporarily modify `IsPlayerOnDuty()` to return true
2. Rebuild and test detection logic
3. Revert when done

### Future Enhancements (Phase 8+)

- Progression system for escalation tracks
- Scheduled activity auto-firing
- Multi-step camp events
- Camp schedule variations by culture
- Weather affecting activities

---

## Playtesting Session Template

### Session Info
- Date: ___________
- Playtime: ___________
- Situation: Garrison / Campaign / Siege
- Tier: ___

### Opportunity Budget
- How many opportunities appeared? ___
- Did it feel right for the situation? Y / N
- Notes:

### Opportunity Relevance
- Did opportunities make sense for context? Y / N
- Any out-of-place opportunities? Y / N
- Which ones:

### Learning System
- After 10+ decisions, did the system adapt? Y / N
- Did adaptation feel natural or forced?
- Notes:

### Detection (if tested)
- How many risky attempts? ___
- Times caught: ___
- Detection rate: ___%
- Did consequences feel fair? Y / N

### Overall Feel
- Camp feels alive? Y / N
- Variety maintained? Y / N
- Repetitive content? Y / N
- Suggestions:

---

## Quick Reference: Config Files

| System | Config File | Location |
|--------|-------------|----------|
| Opportunities | camp_opportunities.json | ModuleData/Enlisted/ |
| Decisions | decisions.json | ModuleData/Enlisted/Decisions/ |
| Localization | enlisted_strings.xml | ModuleData/Languages/ |
| Detection settings | (in opportunities) | camp_opportunities.json |

---

## Balance Recommendations (Initial)

Based on Phase 6 testing and design goals:

### Conservative Start
1. **Keep opportunity budgets low initially** - easier to increase than decrease
2. **Detection chances around 20-30%** - enough risk to matter, not punishing
3. **Light consequences** - encourage experimentation, not fear

### Gradual Tuning
1. Increase budgets if camp feels too sparse
2. Decrease if players feel overwhelmed with choices
3. Adjust detection if players always/never take risks
4. Tune learning weight if adaptation too aggressive/subtle

### Watch For
- **Repetitive content** - same 2-3 opportunities every time (cooldown/variety issue)
- **Irrelevant opportunities** - training during siege, gambling when broke (filtering issue)
- **Learning not working** - preferences not affecting selection (tracking issue)
- **Detection feel** - too harsh or too lenient (balance issue)

---

## Next Steps

1. **Playtest 5-10 hours** across different situations (garrison, campaign, siege)
2. **Fill out playtesting template** after each 1-2 hour session
3. **Adjust balance parameters** based on feedback
4. **Retest** to verify improvements
5. **Document final values** in this file for release

---

## Contact & Feedback

When providing feedback, please include:
- Situation (garrison/campaign/siege/on duty)
- Tier and role
- Specific opportunity IDs that felt wrong
- Suggested changes to budget/fitness/detection values

This helps make balance adjustments precise and effective.
