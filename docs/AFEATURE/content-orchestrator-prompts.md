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
| [Phase 6G](#phase-6g-decisions--medical-migration) | Decisions + Medical Migration | Sonnet 4 | 5-6h | ⛔ **BLOCKING** |
| [Phase 6H](#phase-6h-medical-orchestration) | Medical System Orchestration | Sonnet 4 | 2h | ⛔ **BLOCKING** |
| [Phase 7](#phase-7-content-variants) | Content variants (JSON-only) | Sonnet 4 | 30-60m | ⏸️ Future |
| [Phase 8](#phase-8-progression-system) | Progression System framework | Opus 4 | 2-3h | ⏸️ Future |
| [Phase 9](#phase-9-decision-scheduling) | Decision scheduling system | Sonnet 4 | 2-3h | ✅ **COMPLETE** |
| [Phase 10](#phase-10-order-forecasting) | Order warnings & forecasting | Sonnet 4 | 2-3h | ✅ **COMPLETE** |

**Critical Path:** Phase 6G → Phase 6H → Phase 7-8 (future enhancements)

---

## Phase 6G: Decisions + Medical Migration

**Goal:** Create 30 missing camp decisions (26 camp + 4 medical) AND migrate Medical Tent to decisions

**Status:** ⛔ **BLOCKING Phase 6H**  
**Priority:** Critical  
**Model:** Claude Sonnet 4 (JSON content creation + code changes)  
**Estimated Time:** 5-6 hours

**See Full Spec:** [Medical Care Migration](medical-care-migration.md)

### Problem Statement

Phase 6 created 29 camp opportunities, but only 3 target decisions exist. Additionally, medical care uses a separate menu system instead of decisions.

**Two-Layer Architecture:**
- **Opportunities** - Orchestrator-curated menu items (fitness scoring, order compatibility)
- **Decisions** - The actual event with options, outcomes, result text

### Current State

- ✅ Opportunities exist: 29
- ✅ Decisions exist: 3 (dec_maintain_gear, dec_write_letter, dec_gamble_high)
- ❌ Decisions missing: 26 camp + 4 medical = **30 total**
- ❌ Old static decisions: 35 (pre-orchestrator, need deletion)
- ❌ Medical Tent: 535 lines of separate menu code (needs deletion)

### Required Fix

**Step 0: Delete Old Systems**
1. Open `ModuleData/Enlisted/Decisions/decisions.json`
2. Delete all 35 old static decisions
3. Keep only: dec_maintain_gear, dec_write_letter, dec_gamble_high

**Step 1: Create 26 Camp Decisions** (see list below)

**Step 2: Create 4 Medical Decisions**
- dec_medical_surgeon - Seek Surgeon's Care
- dec_medical_rest - Rest and Recovery
- dec_medical_herbal - Purchase Herbal Remedy
- dec_medical_emergency - Emergency Treatment (severe only)

**Step 3: Delete Medical Menu**
1. Delete `src/Features/Conditions/EnlistedMedicalMenuBehavior.cs` (535 lines)
2. Remove from `SubModule.cs` and `Enlisted.csproj`
3. Remove menu option from `EnlistedMenuBehavior.cs`

**Step 4: Add Requirement Checks**
- Add `hasAnyCondition`, `hasSevereCondition`, `maxIllness` to EventRequirementChecker
- Add `has_any_condition`, `has_untreated_injury` trigger conditions

**Step 5: Add Illness Severity Thresholds**
Update existing training/labor decisions with `maxIllness` restrictions:
- Heavy training: maxIllness: "Mild"
- Physical labor: maxIllness: "Mild"
- Strenuous social: maxIllness: "Mild"
- Light social: maxIllness: "Severe"
- Rest/medical: Always allowed

**Step 6: Validate**
```powershell
python tools/events/validate_events.py
cd C:\Dev\Enlisted\Enlisted
dotnet build -c "Enlisted RETAIL" /p:Platform=x64
```

### Missing Decisions List

**Training (5):** dec_training_drill, dec_training_spar, dec_training_formation, dec_training_veteran, dec_training_archery

**Social (6):** dec_social_stories, dec_tavern_drink, dec_social_storytelling, dec_drinking_contest, dec_social_singing, dec_arm_wrestling

**Economic (4):** dec_gamble_cards, dec_gamble_dice, dec_forage, dec_work_repairs

**Recovery (5):** dec_rest_sleep, dec_help_wounded, dec_prayer, dec_rest_short, dec_meditate

**Special (5):** dec_officer_audience, dec_baggage_access, dec_mentor_recruit, dec_volunteer_extra, dec_night_patrol

**Medical (4):** dec_medical_surgeon, dec_medical_rest, dec_medical_herbal, dec_medical_emergency

### Implementation Prompt

```
I need you to implement Phase 6G: Create 30 missing decisions + migrate medical system.

═══════════════════════════════════════════════════════════════════════════════
CONTEXT
═══════════════════════════════════════════════════════════════════════════════

Phase 6 created 29 camp opportunities, but only 3 target decisions exist.
Additionally, medical care uses a separate menu that should be migrated to decisions.

I need to:
1. Create 26 missing camp decisions
2. Create 4 medical care decisions
3. Delete Medical Tent menu system (535 lines)
4. Add illness severity restrictions to decisions

CRITICAL CONSTRAINTS:
- Each decision: 2-3 options maximum
- Clear tooltips explaining what each option does
- Light RP moments (not heavy narrative)
- Culture-aware placeholders: {SERGEANT}, {NCO}, {LORD_NAME}
- Follow existing decision patterns from dec_maintain_gear
- Medical decisions require hasAnyCondition or hasSevereCondition

═══════════════════════════════════════════════════════════════════════════════
FILES TO READ FIRST
═══════════════════════════════════════════════════════════════════════════════

1. docs/BLUEPRINT.md - Coding standards, tooltip requirements
2. docs/AFEATURE/medical-care-migration.md - COMPLETE MEDICAL SYSTEM SPEC
3. docs/Features/Content/event-system-schemas.md - Decision schema (includes maxIllness)
4. docs/AFEATURE/content-orchestrator-plan.md - Phase 6G/6H context
5. ModuleData/Enlisted/Decisions/decisions.json - Current decisions
6. src/Features/Conditions/EnlistedMedicalMenuBehavior.cs - DELETE this file

═══════════════════════════════════════════════════════════════════════════════
STEP 1: DELETE OLD SYSTEMS
═══════════════════════════════════════════════════════════════════════════════

A. Open ModuleData/Enlisted/Decisions/decisions.json and:
   1. Delete ALL 35 old static decisions
   2. KEEP ONLY these 3: dec_maintain_gear, dec_write_letter, dec_gamble_high

B. Delete Medical Menu:
   1. Delete src/Features/Conditions/EnlistedMedicalMenuBehavior.cs (535 lines)
   2. Remove from src/Mod.Entry/SubModule.cs:
      campaignStarter.AddBehavior(new EnlistedMedicalMenuBehavior());
   3. Remove from Enlisted.csproj:
      <Compile Include="src\Features\Conditions\EnlistedMedicalMenuBehavior.cs"/>

═══════════════════════════════════════════════════════════════════════════════
STEP 2: ADD REQUIREMENT CHECKS
═══════════════════════════════════════════════════════════════════════════════

In src/Features/Content/EventRequirementChecker.cs (or equivalent), add:

```csharp
// Medical condition requirements
if (req.ContainsKey("hasAnyCondition"))
{
    var required = (bool)req["hasAnyCondition"];
    var cond = PlayerConditionBehavior.Instance;
    var has = cond?.State?.HasAnyCondition ?? false;
    if (required && !has) return false;
}

if (req.ContainsKey("hasSevereCondition"))
{
    var required = (bool)req["hasSevereCondition"];
    var cond = PlayerConditionBehavior.Instance;
    var severe = cond?.State?.CurrentInjury >= InjurySeverity.Severe ||
                 cond?.State?.CurrentIllness >= IllnessSeverity.Severe;
    if (required && !severe) return false;
}

if (req.ContainsKey("maxIllness"))
{
    var maxStr = req["maxIllness"].ToString();
    var max = ParseIllnessSeverity(maxStr);
    var current = PlayerConditionBehavior.Instance?.State?.CurrentIllness ?? IllnessSeverity.None;
    if (current > max) return false;
}
```

═══════════════════════════════════════════════════════════════════════════════
STEP 3: CREATE 26 CAMP DECISIONS
═══════════════════════════════════════════════════════════════════════════════

For each, create full decision with titleId, setupId, 2-3 options, tooltips.

**Training (add maxIllness: "Mild"):**
- dec_training_drill, dec_training_spar, dec_training_formation, 
  dec_training_veteran, dec_training_archery

**Social (strenuous add maxIllness: "Mild", light add maxIllness: "Severe"):**
- dec_social_stories, dec_tavern_drink, dec_social_storytelling, 
  dec_drinking_contest, dec_social_singing, dec_arm_wrestling

**Economic (maxIllness: "Mild"):**
- dec_gamble_cards, dec_gamble_dice, dec_forage, dec_work_repairs

**Recovery (no maxIllness - always allowed):**
- dec_rest_sleep, dec_help_wounded, dec_prayer, dec_rest_short, dec_meditate

**Special (maxIllness: "Severe"):**
- dec_officer_audience, dec_baggage_access, dec_mentor_recruit, 
  dec_volunteer_extra, dec_night_patrol

═══════════════════════════════════════════════════════════════════════════════
STEP 4: CREATE 4 MEDICAL DECISIONS
═══════════════════════════════════════════════════════════════════════════════

**dec_medical_surgeon** (requires hasAnyCondition: true)
- Title: "Seek Surgeon's Care"
- Setup: "You visit the surgeon's tent. The smell of poultices and bloodied linen."
- Options:
  1. "Request full treatment" - 2 fatigue, 2.0x recovery, reset Medical Risk
  2. "Just check the wounds" - 1 fatigue, 1.5x recovery
  3. "Not now" - No cost, leave

**dec_medical_rest** (requires hasAnyCondition: true)
- Title: "Rest and Recovery"
- Setup: "You're still recovering. Today feels like it should be a rest day."
- Options:
  1. "Take the day off" - Restore 8 fatigue, 1.5x recovery
  2. "Light duties only" - Restore 4 fatigue, 1.25x recovery
  3. "Push through it" - No effect, small worsen risk

**dec_medical_herbal** (requires hasAnyCondition: true, context: Camp/Town)
- Title: "Purchase Herbal Remedy"
- Setup: "Camp followers sell herbs—willow bark, yarrow, poppy milk."
- Options:
  1. "Buy quality herbs" - 50 denars, 1.75x recovery
  2. "Buy cheap herbs" - 25 denars, 1.5x recovery, 25% no effect
  3. "Save your coin" - Leave

**dec_medical_emergency** (requires hasSevereCondition: true)
- Title: "Emergency Treatment"
- Setup: "Your condition has worsened. The surgeon says you need immediate care."
- Options:
  1. "Aggressive treatment" - 3 fatigue, 100 denars, 3.0x recovery
  2. "Standard treatment" - 2 fatigue, 2.0x recovery
  3. "Refuse treatment" - 50% worsen, +2 Medical Risk

═══════════════════════════════════════════════════════════════════════════════
STEP 4A: ADD ILLNESS HP REDUCTION (WITH 30% FLOOR)
═══════════════════════════════════════════════════════════════════════════════

**CRITICAL SAFETY RULE:** Illnesses reduce HP, but NEVER below 30% max HP minimum.

In PlayerConditionBehavior.TryApplyIllness(), add HP reduction:

```csharp
public void TryApplyIllness(string illnessType, IllnessSeverity severity, int days, string reason)
{
    // ... existing illness application code ...
    
    // Apply HP reduction based on severity (with 30% floor)
    var hero = Hero.MainHero;
    if (hero != null && severity > IllnessSeverity.Mild)
    {
        var maxHp = hero.CharacterObject.MaxHitPoints();
        var hpPercent = severity switch
        {
            IllnessSeverity.Moderate => 0.05f,  // -5%
            IllnessSeverity.Severe => 0.15f,     // -15%
            IllnessSeverity.Critical => 0.30f,   // -30%
            _ => 0f
        };
        
        var hpLoss = (int)(maxHp * hpPercent);
        var currentHp = hero.HitPoints;
        var newHp = currentHp - hpLoss;
        
        // CRITICAL: Never drop below 30% max HP
        var minimumHp = (int)(maxHp * 0.30f);
        hero.HitPoints = Math.Max(minimumHp, newHp);
        
        ModLogger.Debug("PlayerConditions", 
            $"Illness HP reduction: {hpLoss} (current: {currentHp} → {hero.HitPoints}, floor: {minimumHp})");
    }
    
    // ... rest of illness application ...
}
```

**Restore HP when illness heals:**

In PlayerConditionBehavior.OnDailyTick(), restore HP when illness duration expires:

```csharp
private void ApplyDailyRecoveryAndRisk()
{
    var hadIllnessAtStart = _state.HasIllness;
    var illnessSeverityAtStart = _state.CurrentIllness;
    
    // ... existing daily tick code ...
    
    // Restore HP when illness heals (illness HP reduction is temporary)
    if (hadIllnessAtStart && !_state.HasIllness)
    {
        var hero = Hero.MainHero;
        if (hero != null)
        {
            var maxHp = hero.CharacterObject.MaxHitPoints();
            var hpRestore = illnessSeverityAtStart switch
            {
                IllnessSeverity.Moderate => (int)(maxHp * 0.05f),
                IllnessSeverity.Severe => (int)(maxHp * 0.15f),
                IllnessSeverity.Critical => (int)(maxHp * 0.30f),
                _ => 0
            };
            
            if (hpRestore > 0)
            {
                hero.HitPoints = Math.Min(maxHp, hero.HitPoints + hpRestore);
                ModLogger.Debug("PlayerConditions", $"Illness healed, HP restored: +{hpRestore}");
            }
        }
    }
}
```

**Critical Test Case:**
Player with gut wound (-50% HP) + critical fever (-30% HP) = Combined -80%, but stops at 30% minimum.

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
STEP 5: ADD ILLNESS STATUS MESSAGING
═══════════════════════════════════════════════════════════════════════════════

In PlayerConditionBehavior.OnDailyTick(), add combat log messages:

```csharp
if (_state.HasIllness)
{
    var msg = _state.CurrentIllness switch
    {
        IllnessSeverity.Mild => "The cold lingers. You sniffle through the morning.",
        IllnessSeverity.Moderate => "Another restless night. The fever hasn't broken.",
        IllnessSeverity.Severe => "You can barely lift yourself from your bedroll. Everything hurts.",
        IllnessSeverity.Critical => "You slip in and out of consciousness. Is this how it ends?",
        _ => null
    };
    if (msg != null) InformationManager.DisplayMessage(new InformationMessage(msg, Colors.Yellow));
}
```

Add localization strings for all illness messages - see medical-care-migration.md.

═══════════════════════════════════════════════════════════════════════════════
ACCEPTANCE CRITERIA
═══════════════════════════════════════════════════════════════════════════════

[ ] All 35 old static decisions deleted
[ ] EnlistedMedicalMenuBehavior.cs deleted (535 lines)
[ ] All 26 camp decisions created with maxIllness restrictions
[ ] All 4 medical decisions created with condition requirements
[ ] hasAnyCondition, hasSevereCondition, maxIllness requirement checks added
[ ] Illness HP reduction applies with 30% floor (CRITICAL - test injury + illness combo)
[ ] HP restores when illness heals
[ ] Daily illness messages display in combat log
[ ] EVERY option has a non-null tooltip
[ ] Validation passes without errors
[ ] Build succeeds
[ ] decisions.json has 33 total decisions (3 kept + 26 camp + 4 medical)

═══════════════════════════════════════════════════════════════════════════════
```

---

## Phase 6H: Medical Orchestration

**Goal:** Make the medical system fully world-state-driven through the Content Orchestrator

**Status:** ⛔ **BLOCKING** - Requires Phase 6G complete  
**Priority:** High  
**Model:** Claude Sonnet 4  
**Estimated Time:** 2 hours

**See Full Spec:** [Medical Care Migration](medical-care-migration.md) - Orchestrator Integration section

### Overview

After Phase 6G creates medical decisions, this phase integrates them with the orchestrator for intelligent illness onset, medical care suggestions, and forecasting.

### Implementation Prompt

```
I need you to implement Phase 6H: Medical System Orchestration for the Enlisted mod.

═══════════════════════════════════════════════════════════════════════════════
CONTEXT
═══════════════════════════════════════════════════════════════════════════════

Phase 6G created medical care decisions (dec_medical_surgeon, etc.) and removed
the Medical Tent menu. Now I need the orchestrator to intelligently:
1. Trigger illness onset based on Medical Risk
2. Create camp opportunities for medical care
3. Show medical warnings in forecast

═══════════════════════════════════════════════════════════════════════════════
FILES TO READ FIRST
═══════════════════════════════════════════════════════════════════════════════

1. docs/AFEATURE/medical-care-migration.md - COMPLETE SPEC (Orchestrator section)
2. docs/Features/Content/event-system-schemas.md - Trigger requirements
3. src/Features/Content/ContentOrchestrator.cs - Existing orchestrator
4. src/Features/Content/SimulationPressureCalculator.cs - Pressure tracking
5. src/Features/Content/WorldStateAnalyzer.cs - World state
6. src/Features/Content/ForecastGenerator.cs - Forecast text

═══════════════════════════════════════════════════════════════════════════════
STEP 1: ADD MEDICAL PRESSURE TRACKING
═══════════════════════════════════════════════════════════════════════════════

Add to SimulationPressureCalculator.cs:

```csharp
public class MedicalPressureAnalysis
{
    public int MedicalRisk { get; set; }           // 0-5 from EscalationManager
    public bool HasCondition { get; set; }         // Active injury/illness
    public bool HasSevereCondition { get; set; }   // Severe/Critical
    public bool IsUntreated { get; set; }          // No UnderMedicalCare
    public int DaysUntreated { get; set; }
    public float HealthPercent { get; set; }
    
    public MedicalPressureLevel GetPressureLevel()
    {
        if (HasSevereCondition || MedicalRisk >= 4) return MedicalPressureLevel.Critical;
        if (HasCondition && IsUntreated && DaysUntreated >= 3) return MedicalPressureLevel.High;
        if (MedicalRisk >= 3 || (HasCondition && IsUntreated)) return MedicalPressureLevel.Moderate;
        if (MedicalRisk >= 2) return MedicalPressureLevel.Low;
        return MedicalPressureLevel.None;
    }
}

public enum MedicalPressureLevel { None, Low, Moderate, High, Critical }

public MedicalPressureAnalysis GetMedicalPressure() { ... }
```

═══════════════════════════════════════════════════════════════════════════════
STEP 2: CREATE CAMP OPPORTUNITIES
═══════════════════════════════════════════════════════════════════════════════

Add to ModuleData/Enlisted/Opportunities/camp_opportunities.json:

```json
{
  "id": "opp_seek_treatment",
  "title": "Seek Medical Care",
  "description": "Your condition needs attention. The surgeon can help.",
  "targetDecision": "dec_medical_surgeon",
  "requirements": { "hasAnyCondition": true },
  "priority": 70,
  "tags": ["medical", "recovery"]
},
{
  "id": "opp_emergency_care",
  "title": "URGENT: Seek Treatment",
  "description": "Your condition has worsened. You need immediate care.",
  "targetDecision": "dec_medical_emergency",
  "requirements": { "hasSevereCondition": true },
  "urgency": "critical",
  "priority": 95,
  "tags": ["medical", "emergency"]
},
{
  "id": "opp_medical_rest",
  "title": "Take a Rest Day",
  "description": "You're recovering. Rest would help.",
  "targetDecision": "dec_medical_rest",
  "requirements": { "hasAnyCondition": true },
  "priority": 50,
  "tags": ["medical", "rest"]
}
```

═══════════════════════════════════════════════════════════════════════════════
STEP 3: CREATE ILLNESS ONSET EVENTS
═══════════════════════════════════════════════════════════════════════════════

Create ModuleData/Enlisted/Events/illness_onset.json:

```json
{
  "schemaVersion": 2,
  "category": "automatic",
  "events": [
    {
      "id": "evt_illness_onset_fever",
      "titleId": "evt_illness_fever_title",
      "title": "Camp Fever",
      "setupId": "evt_illness_fever_setup",
      "setup": "You wake feeling hot. Your head pounds. The familiar signs of fever.",
      "triggers": {
        "all": ["is_enlisted"],
        "escalation_requirements": { "medical_risk": 3 }
      },
      "requirements": { "tier": { "min": 1 } },
      "options": [
        {
          "id": "accept",
          "text": "I need to see the surgeon.",
          "tooltip": "Accept illness. Applies moderate fever (7 days).",
          "effects": { "applyIllness": { "type": "camp_fever", "severity": "moderate", "days": 7 } },
          "resultText": "The fever takes hold. You'll need treatment and rest."
        },
        {
          "id": "push_through",
          "text": "I can push through this.",
          "tooltip": "Risky. 50% chance illness worsens to severe.",
          "chance": 0.5,
          "effects": { "applyIllness": { "type": "camp_fever", "severity": "severe", "days": 10 }, "hpChange": -15 },
          "resultText": "You try to ignore it. The fever worsens.",
          "resultFailureText": "You push through. The fever passes."
        }
      ]
    }
  ]
}
```

═══════════════════════════════════════════════════════════════════════════════
STEP 4: UPDATE CONTENT ORCHESTRATOR
═══════════════════════════════════════════════════════════════════════════════

Add to ContentOrchestrator.cs in daily tick:

```csharp
private bool _emergencyOpportunityForced = false;
private int _lastIllnessOnsetDay = -10;

private void CheckMedicalPressure()
{
    var pressure = SimulationPressureCalculator.Instance.GetMedicalPressure();
    var level = pressure.GetPressureLevel();
    
    // Critical: Force emergency opportunity (once)
    if (level == MedicalPressureLevel.Critical && !_emergencyOpportunityForced)
    {
        CampOpportunityGenerator.Instance?.ForceOpportunity("opp_emergency_care");
        _emergencyOpportunityForced = true;
        return;
    }
    
    // Reset flag when no longer critical
    if (level < MedicalPressureLevel.Critical) _emergencyOpportunityForced = false;
    
    // Roll for illness onset if Medical Risk >= 3 and not already sick
    if (pressure.MedicalRisk >= 3 && !pressure.HasCondition)
    {
        TryTriggerIllnessOnset(pressure);
    }
}

private void TryTriggerIllnessOnset(MedicalPressureAnalysis pressure)
{
    // Cooldown check
    var today = (int)CampaignTime.Now.ToDays;
    if (today - _lastIllnessOnsetDay < 7) return;
    
    // Calculate chance
    var chance = pressure.MedicalRisk * 0.05f; // 5% per risk level
    if (EnlistmentBehavior.Instance.FatigueCurrent <= 8) chance += 0.10f;
    if (WorldStateAnalyzer.GetSeason() == Season.Winter) chance += 0.08f;
    if (WorldStateAnalyzer.GetLordSituation() == LordSituation.InSiege) chance += 0.12f;
    
    if (MBRandom.RandomFloat < chance)
    {
        _lastIllnessOnsetDay = today;
        EventDeliveryManager.Instance?.QueueEvent("evt_illness_onset_fever", "medical_risk");
    }
}
```

═══════════════════════════════════════════════════════════════════════════════
STEP 5: UPDATE FORECAST
═══════════════════════════════════════════════════════════════════════════════

Add to ForecastGenerator.cs:

```csharp
private void AddMedicalForecast(StringBuilder ahead, StringBuilder concerns)
{
    var pressure = SimulationPressureCalculator.Instance.GetMedicalPressure();
    var cond = PlayerConditionBehavior.Instance;
    
    // Critical warning in AHEAD
    if (pressure.GetPressureLevel() == MedicalPressureLevel.Critical)
    {
        if (pressure.HasSevereCondition)
            ahead.AppendLine("Your condition is severe. Seek the surgeon immediately.");
        else
            ahead.AppendLine("Medical Risk critical. Illness likely without rest.");
    }
    
    // Active condition status
    if (cond?.State?.HasAnyCondition == true)
    {
        var days = Math.Max(cond.State.InjuryDaysRemaining, cond.State.IllnessDaysRemaining);
        var treatment = cond.State.UnderMedicalCare ? "under treatment" : "untreated";
        ahead.AppendLine($"Recovering from condition ({days} days, {treatment}).");
        
        if (!cond.State.UnderMedicalCare && days > 3)
            concerns.AppendLine($"Medical: Untreated condition ({days} days). Seek care.");
    }
    
    // Medical Risk warning
    if (pressure.MedicalRisk >= 3 && pressure.GetPressureLevel() != MedicalPressureLevel.Critical)
        concerns.AppendLine($"Medical Risk: {pressure.MedicalRisk}/5. Rest recommended.");
}
```

═══════════════════════════════════════════════════════════════════════════════
STEP 6: UPDATE WORLD STATE
═══════════════════════════════════════════════════════════════════════════════

Add to WorldSituation model:

```csharp
public MedicalPressureLevel MedicalPressure { get; set; }
public bool RequiresMedicalCare { get; set; }
public bool HasCriticalCondition { get; set; }
```

Calculate in WorldStateAnalyzer.AnalyzeCurrentSituation().

═══════════════════════════════════════════════════════════════════════════════
STEP 7: SAVE/LOAD
═══════════════════════════════════════════════════════════════════════════════

Add to ContentOrchestrator.SyncData():

```csharp
dataStore.SyncData("orchestrator_emergencyForced", ref _emergencyOpportunityForced);
dataStore.SyncData("orchestrator_lastIllnessDay", ref _lastIllnessOnsetDay);
```

═══════════════════════════════════════════════════════════════════════════════
ACCEPTANCE CRITERIA
═══════════════════════════════════════════════════════════════════════════════

[ ] MedicalPressureAnalysis class tracks condition state
[ ] 3 medical camp opportunities created
[ ] Illness onset event fires when Medical Risk >= 3 (with modifiers)
[ ] Emergency opportunity forced once when condition critical
[ ] Illness blocked if player already sick (spam prevention)
[ ] 7-day cooldown between illness triggers
[ ] Forecast shows Medical Risk warnings
[ ] Forecast shows condition recovery status
[ ] World state includes medical pressure
[ ] Save/load persists orchestrator medical state
[ ] Build succeeds without errors

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

**Last Updated:** 2026-01-01 (Added Phase 6G/6H Medical System Integration)  
**Maintained By:** Project AI Assistant
