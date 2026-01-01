# Medical Care Migration: From Menu to Decisions

**Summary:** Migration plan to replace the separate Medical Tent menu system with integrated medical care decisions. This consolidates UI, reduces complexity, and makes medical care consistent with other camp activities in the Decisions menu.

**Status:** ⏸️ Planning  
**Priority:** High - Part of Phase 6G (menu migration) and Phase 6H (orchestrator integration)  
**Estimated Time:** 4-5 hours (2-3 hours migration + 2 hours orchestrator)  
**Related Docs:** [Content Orchestrator Plan](content-orchestrator-plan.md), [Camp Life Simulation](../Features/Campaign/camp-life-simulation.md), [Medical Progression System](../Features/Content/medical-progression-system.md)

---

## Index

1. [Current System Problems](#current-system-problems)
2. [Migration Overview](#migration-overview)
3. [Decision Specifications](#decision-specifications)
4. [Orchestrator Integration](#orchestrator-integration)
5. [Implementation Steps](#implementation-steps)
6. [Files to Delete](#files-to-delete)
7. [Files to Modify](#files-to-modify)
8. [Testing Checklist](#testing-checklist)

---

## Current System Problems

### Issues with Medical Tent Menu

**Separate Menu System:**
- `EnlistedMedicalMenuBehavior.cs` creates a dedicated game menu
- Adds complexity with separate menu registration and navigation
- Inconsistent with how other camp activities work (Decisions menu)
- 535 lines of menu management code

**Inconsistent UX:**
- Medical care is the ONLY camp activity with its own separate menu
- Training, rest, socializing all use Decisions menu
- Creates cognitive load (where do I find what?)
- Extra navigation step to reach treatment options

**Maintenance Burden:**
- Separate behavior to register/maintain
- Duplicate text variable management
- Menu state management complexity
- More surface area for bugs

### Current Medical Tent Features

The existing menu provides 5 options:

1. **Request Treatment from Surgeon**
   - Cost: 2 fatigue
   - Effect: Recovery rate +100%
   - Resets Medical Risk

2. **Treat Yourself (Field Medic)** [Disabled]
   - Cost: None
   - Effect: Recovery rate +50%, Medicine XP
   - Requires Field Medic profession (not implemented)

3. **Purchase Herbal Remedy**
   - Cost: 50 denars
   - Effect: Recovery rate +75%
   - Resets Medical Risk

4. **Rest in Camp**
   - Cost: 1 day
   - Effect: Recovery rate +50%
   - Skip activities for the day

5. **View Detailed Status** (Popup)
   - Shows condition details
   - Days remaining
   - Effects and restrictions

---

## Migration Overview

### New Design: Medical Decisions

Replace the separate menu with **3-4 medical care decisions** that appear in the standard Decisions menu when player has a condition.

**Advantages:**
- Consistent with all other camp activities
- Uses existing decision system infrastructure
- Reduces code complexity (535 lines → ~150 lines of JSON)
- Better integration with camp opportunities
- Can be filtered by world state like other decisions

**Decision Types:**

1. **Emergency Care** (Severe/Critical conditions)
   - Only when condition is serious
   - Immediate treatment required
   - Higher costs, faster recovery

2. **Routine Treatment** (All conditions)
   - Standard surgeon care
   - Balanced cost/benefit

3. **Self Care** (Minor/Moderate conditions)
   - Lower cost options
   - Herbal remedies, rest
   - Slower recovery

4. **View Status** (Optional, or fold into UI)
   - Could be a decision
   - Or just show status in decision tooltips

---

## Decision Specifications

### Decision 1: Seek Surgeon's Care

**ID:** `dec_medical_surgeon`  
**Availability:** `HasAnyCondition` requirement  
**Narrative:** "You visit the surgeon's tent. The smell of poultices and bloodied linen. The surgeon looks you over."

**Options:**

1. **Request full treatment** (2 fatigue, resets Medical Risk)
   - Costs: `fatigue: 2`
   - Effects: Apply treatment (2.0x recovery multiplier)
   - Tooltip: "Thorough treatment. Recovery rate doubled. Costs 2 fatigue."
   - Feedback: "The surgeon binds your wounds and prescribes rest."

2. **Just check the wounds** (1 fatigue, minor treatment)
   - Costs: `fatigue: 1`
   - Effects: Apply treatment (1.5x recovery multiplier)
   - Tooltip: "Basic care. Recovery rate +50%. Costs 1 fatigue."
   - Feedback: "The surgeon cleans and rebandages your wounds."

3. **Not now** (Leave)
   - No cost
   - Tooltip: "Leave without treatment. Condition may worsen."

---

### Decision 2: Rest and Recovery

**ID:** `dec_medical_rest`  
**Availability:** `HasAnyCondition` requirement  
**Narrative:** "You're still recovering from your condition. Today feels like it should be a rest day."

**Options:**

1. **Take the day off** (Full day rest)
   - Costs: None
   - Duration: 8 hours (full day)
   - Effects: Apply treatment (1.5x recovery multiplier), restore 8 fatigue
   - Tooltip: "Skip activities for the day. Recovery rate +50%. Restore 8 fatigue."
   - Feedback: "You spend the day resting in camp, letting your body heal."

2. **Light duties only** (Partial rest)
   - Costs: None
   - Duration: 4 hours (half day)
   - Effects: Apply treatment (1.25x recovery multiplier), restore 4 fatigue
   - Tooltip: "Easy pace today. Recovery rate +25%. Restore 4 fatigue."
   - Feedback: "You keep to light activities, giving yourself time to recover."

3. **Push through it** (No rest)
   - No effect
   - Tooltip: "Continue normal activities. No recovery bonus."
   - Risk: Small chance condition worsens

---

### Decision 3: Purchase Herbal Remedy

**ID:** `dec_medical_herbal`  
**Availability:** `HasAnyCondition` requirement, in settlement or garrison  
**Narrative:** "Camp followers sell herbal remedies—willow bark, yarrow, poppy milk. Medicine of uncertain quality."

**Options:**

1. **Buy quality herbs** (50 denars, good effect)
   - Costs: `denars: 50`
   - Effects: Apply treatment (1.75x recovery multiplier)
   - Tooltip: "Good quality herbs. Recovery rate +75%. Costs 50 denars."
   - Feedback: "You purchase herbs from a reputable trader."

2. **Buy cheap herbs** (25 denars, risky)
   - Costs: `denars: 25`
   - Effects: Apply treatment (1.5x recovery multiplier), 25% chance no effect
   - Tooltip: "Cheap herbs. Recovery rate +50%. Quality uncertain. Costs 25 denars."
   - Feedback: "You buy herbs from a questionable source..."
   - Success: "The herbs seem effective."
   - Failure: "The herbs do nothing. Wasted coin."

3. **Don't buy** (Leave)
   - No cost
   - Tooltip: "Save your coin."

---

### Decision 4: Emergency Treatment (Severe/Critical only)

**ID:** `dec_medical_emergency`  
**Availability:** `HasSevereCondition` requirement (Severe or Critical injury/illness)  
**Narrative:** "Your condition has worsened. The pain is constant. The surgeon says you need immediate care."

**Options:**

1. **Aggressive treatment** (3 fatigue, 100 denars, fast recovery)
   - Costs: `fatigue: 3, denars: 100`
   - Effects: Apply treatment (3.0x recovery multiplier), cut recovery time by 1/3
   - Tooltip: "Intensive care. Recovery rate tripled. Costs 3 fatigue and 100 denars."
   - Feedback: "The surgeon devotes hours to your treatment. The pain eases."

2. **Standard treatment** (2 fatigue)
   - Costs: `fatigue: 2`
   - Effects: Apply treatment (2.0x recovery multiplier)
   - Tooltip: "Standard care for severe conditions. Recovery rate doubled. Costs 2 fatigue."
   - Feedback: "The surgeon treats your condition. It will take time to heal."

3. **Refuse treatment** (Very risky)
   - No cost
   - Effects: 50% chance condition worsens, +2 Medical Risk
   - Tooltip: "DANGER: Severe conditions can worsen without treatment. Not recommended."
   - Feedback: "The surgeon warns you this is unwise. You leave anyway."

---

## Orchestrator Integration

### Phase 6H: Medical System Orchestration

Make the medical system fully world-state-driven through the Content Orchestrator. Instead of illness being random or manual, the orchestrator analyzes player condition and world state to intelligently trigger illness onset and suggest medical care.

---

### 1. Medical Pressure Tracking

Add medical condition tracking to `SimulationPressureCalculator.cs`:

```csharp
/// <summary>
/// Analyzes medical risk and current conditions for pressure calculation.
/// Used by orchestrator to determine when to trigger illness or suggest care.
/// </summary>
public class MedicalPressureAnalysis
{
    public int MedicalRisk { get; set; }           // 0-5 scale from EscalationManager
    public bool HasCondition { get; set; }         // Any active injury/illness
    public bool HasSevereCondition { get; set; }   // Severe/Critical condition
    public bool IsUntreated { get; set; }          // Condition exists but no treatment
    public int DaysUntreated { get; set; }         // Days since condition started without treatment
    public float HealthPercent { get; set; }       // Current HP / Max HP
    public int LowFatigueDays { get; set; }        // Consecutive days with fatigue < 8
    
    public MedicalPressureLevel GetPressureLevel()
    {
        if (HasSevereCondition || MedicalRisk >= 4)
            return MedicalPressureLevel.Critical;
        
        if (HasCondition && IsUntreated && DaysUntreated >= 3)
            return MedicalPressureLevel.High;
        
        if (MedicalRisk >= 3 || (HasCondition && IsUntreated))
            return MedicalPressureLevel.Moderate;
        
        if (MedicalRisk >= 2 || LowFatigueDays >= 3)
            return MedicalPressureLevel.Low;
        
        return MedicalPressureLevel.None;
    }
}

public enum MedicalPressureLevel
{
    None,       // Healthy, no concerns
    Low,        // Minor risk building
    Moderate,   // Should seek care soon
    High,       // Needs treatment
    Critical    // Emergency - must treat now
}
```

Add to `SimulationPressureCalculator`:

```csharp
public MedicalPressureAnalysis GetMedicalPressure()
{
    var cond = PlayerConditionBehavior.Instance;
    var esc = EscalationManager.Instance;
    var enlistment = EnlistmentBehavior.Instance;
    
    var analysis = new MedicalPressureAnalysis
    {
        MedicalRisk = esc?.MedicalRisk ?? 0,
        HasCondition = cond?.State?.HasAnyCondition ?? false,
        HasSevereCondition = cond?.State?.CurrentInjury >= InjurySeverity.Severe ||
                           cond?.State?.CurrentIllness >= IllnessSeverity.Severe,
        IsUntreated = (cond?.State?.HasAnyCondition ?? false) && 
                     !(cond?.State?.UnderMedicalCare ?? false),
        DaysUntreated = CalculateDaysUntreated(cond),
        HealthPercent = Hero.MainHero.HitPoints / (float)Hero.MainHero.CharacterObject.MaxHitPoints(),
        LowFatigueDays = CalculateLowFatigueDays(enlistment)
    };
    
    return analysis;
}
```

---

### 2. Camp Opportunities for Medical Care

Add medical care opportunities to `camp_opportunities.json`:

```json
{
  "id": "opp_seek_treatment",
  "title": "Seek Medical Care",
  "description": "Your condition needs attention. The surgeon can help.",
  "icon": "medical",
  "targetDecision": "dec_medical_surgeon",
  "requirements": {
    "condition": {
      "hasAnyCondition": true
    }
  },
  "urgency": "medium",
  "priority": 70,
  "tags": ["medical", "recovery"]
},
{
  "id": "opp_emergency_care",
  "title": "URGENT: Seek Treatment",
  "description": "Your condition has worsened. You need immediate medical attention.",
  "icon": "medical_urgent",
  "targetDecision": "dec_medical_emergency",
  "requirements": {
    "condition": {
      "hasSevereCondition": true
    }
  },
  "urgency": "critical",
  "priority": 95,
  "tags": ["medical", "emergency"]
},
{
  "id": "opp_medical_rest",
  "title": "Take a Rest Day",
  "description": "You're recovering from injury. Rest would help.",
  "icon": "rest",
  "targetDecision": "dec_medical_rest",
  "requirements": {
    "condition": {
      "hasAnyCondition": true
    }
  },
  "urgency": "low",
  "priority": 50,
  "tags": ["medical", "rest", "recovery"]
}
```

---

### 3. Illness Onset Events

Create illness onset events that the orchestrator can trigger based on Medical Risk:

**File:** `ModuleData/Enlisted/Events/illness_onset.json`

```json
{
  "schemaVersion": 2,
  "events": [
    {
      "id": "evt_illness_onset_fever",
      "title": "Camp Fever",
      "narrative": "You wake feeling hot. Your head pounds. The familiar signs of fever.",
      "trigger": "medical_risk_threshold",
      "requirements": {
        "tier": { "min": 1 },
        "escalation": {
          "medicalRisk": { "min": 3 }
        }
      },
      "baseChance": 0.15,
      "modifiers": [
        {
          "condition": "fatigue_low",
          "modifier": 0.10,
          "reason": "Exhaustion weakens immune system"
        },
        {
          "condition": "winter",
          "modifier": 0.08,
          "reason": "Cold weather increases illness risk"
        },
        {
          "condition": "campaign_active",
          "modifier": 0.05,
          "reason": "Campaign conditions spread disease"
        },
        {
          "condition": "siege_active",
          "modifier": 0.12,
          "reason": "Siege sanitation breeds sickness"
        }
      ],
      "outcomes": [
        {
          "id": "accept_illness",
          "text": "I need to see the surgeon.",
          "tooltip": "Accept you're sick. Applies illness condition.",
          "effects": {
            "applyIllness": {
              "type": "camp_fever",
              "severity": "moderate",
              "days": 7
            },
            "resetMedicalRisk": true
          },
          "feedback": "The fever takes hold. You'll need treatment and rest."
        },
        {
          "id": "ignore_symptoms",
          "text": "I can push through this.",
          "tooltip": "Risky. Illness may worsen.",
          "chance": 0.5,
          "effects": {
            "applyIllness": {
              "type": "camp_fever",
              "severity": "severe",
              "days": 10
            },
            "hp_loss": 15
          },
          "feedback": "You try to ignore it. The fever worsens."
        }
      ]
    },
    {
      "id": "evt_illness_onset_injury_infection",
      "title": "Infection",
      "narrative": "The wound has turned angry. Red lines spreading. The smell of rot.",
      "requirements": {
        "tier": { "min": 1 },
        "condition": {
          "hasInjury": true,
          "isUntreated": true,
          "daysUntreated": { "min": 3 }
        }
      },
      "baseChance": 0.25,
      "outcomes": [
        {
          "id": "seek_surgeon",
          "text": "Get to the surgeon. Now.",
          "tooltip": "Emergency treatment required.",
          "effects": {
            "applyIllness": {
              "type": "infection",
              "severity": "severe",
              "days": 10
            },
            "hp_loss": 20,
            "resetMedicalRisk": true
          },
          "feedback": "The surgeon cleans the infection. It was close."
        }
      ]
    }
  ]
}
```

---

### 4. Orchestrator Daily Check

Add medical checks to `ContentOrchestrator.OnDailyTick()`:

```csharp
private void CheckMedicalPressure()
{
    var enlistment = EnlistmentBehavior.Instance;
    if (enlistment?.IsEnlisted != true) return;
    
    var medicalPressure = SimulationPressureCalculator.Instance.GetMedicalPressure();
    var pressureLevel = medicalPressure.GetPressureLevel();
    
    // Critical - force emergency treatment opportunity
    if (pressureLevel == MedicalPressureLevel.Critical)
    {
        CampOpportunityGenerator.Instance?.ForceOpportunity("opp_emergency_care");
        ModLogger.Info("Orchestrator", "Critical medical pressure - emergency care forced");
        return;
    }
    
    // High - boost medical care opportunity priority
    if (pressureLevel == MedicalPressureLevel.High)
    {
        CampOpportunityGenerator.Instance?.BoostOpportunityPriority("opp_seek_treatment", 20);
        ModLogger.Debug("Orchestrator", "High medical pressure - treatment priority boosted");
    }
    
    // Roll for illness onset if Medical Risk >= 3
    if (medicalPressure.MedicalRisk >= 3)
    {
        TryTriggerIllnessOnset(medicalPressure);
    }
}

private void TryTriggerIllnessOnset(MedicalPressureAnalysis pressure)
{
    // Base chance increases with Medical Risk
    var baseChance = pressure.MedicalRisk * 0.05f; // 5% per risk level
    
    // Modifiers
    var chance = baseChance;
    
    if (EnlistmentBehavior.Instance.FatigueCurrent <= 8)
        chance += 0.10f; // +10% if fatigued
    
    if (WorldStateAnalyzer.GetSeason() == Season.Winter)
        chance += 0.08f; // +8% in winter
    
    var lordSituation = WorldStateAnalyzer.GetLordSituation();
    if (lordSituation == LordSituation.InSiege)
        chance += 0.12f; // +12% during siege
    else if (lordSituation == LordSituation.InCampaign)
        chance += 0.05f; // +5% during campaign
    
    // Roll
    if (MBRandom.RandomFloat < chance)
    {
        // Select appropriate illness event based on context
        var eventId = SelectIllnessEvent(pressure);
        EventDeliveryManager.Instance?.QueueEvent(eventId, "medical_risk_onset");
        
        ModLogger.Info("Orchestrator", 
            $"Illness onset triggered (risk={pressure.MedicalRisk}, chance={chance:P1})");
    }
}

private string SelectIllnessEvent(MedicalPressureAnalysis pressure)
{
    // If untreated injury, risk infection
    if (pressure.HasCondition && pressure.IsUntreated && pressure.DaysUntreated >= 3)
    {
        return "evt_illness_onset_injury_infection";
    }
    
    // Otherwise standard illness
    return "evt_illness_onset_fever";
}
```

---

### 5. Forecast Integration

Update `ForecastGenerator.cs` to show medical warnings:

```csharp
private void AddMedicalForecast(WorldSituation situation, StringBuilder ahead, StringBuilder concerns)
{
    var medicalPressure = SimulationPressureCalculator.Instance.GetMedicalPressure();
    var pressureLevel = medicalPressure.GetPressureLevel();
    var cond = PlayerConditionBehavior.Instance;
    
    // Critical conditions - show in AHEAD
    if (pressureLevel == MedicalPressureLevel.Critical)
    {
        if (medicalPressure.HasSevereCondition)
        {
            ahead.AppendLine("Your condition is severe. Seek the surgeon immediately.");
        }
        else
        {
            ahead.AppendLine("Medical Risk critical. Illness likely without rest.");
        }
    }
    
    // Active conditions - show recovery status
    if (cond?.State?.HasAnyCondition == true)
    {
        var days = Math.Max(cond.State.InjuryDaysRemaining, cond.State.IllnessDaysRemaining);
        var treatment = cond.State.UnderMedicalCare ? "under treatment" : "untreated";
        
        ahead.AppendLine($"Recovering from condition ({days} days, {treatment}).");
        
        if (!cond.State.UnderMedicalCare && days > 3)
        {
            concerns.AppendLine($"Medical: Untreated condition ({days} days). Seek care.");
        }
    }
    
    // Medical Risk warnings
    if (medicalPressure.MedicalRisk >= 3 && pressureLevel != MedicalPressureLevel.Critical)
    {
        concerns.AppendLine($"Medical Risk: {medicalPressure.MedicalRisk}/5. Rest recommended.");
    }
    
    // Fatigue warnings (contributes to illness risk)
    if (medicalPressure.LowFatigueDays >= 3)
    {
        concerns.AppendLine($"Exhausted for {medicalPressure.LowFatigueDays} days. Illness risk rising.");
    }
}
```

---

### 6. World State Context

Add medical pressure to `WorldSituation` model:

```csharp
public class WorldSituation
{
    // ... existing fields ...
    
    public MedicalPressureLevel MedicalPressure { get; set; }
    public bool RequiresMedicalCare { get; set; }
    public bool HasCriticalCondition { get; set; }
}
```

Update `WorldStateAnalyzer.AnalyzeCurrentSituation()`:

```csharp
var medicalPressure = SimulationPressureCalculator.Instance.GetMedicalPressure();

situation.MedicalPressure = medicalPressure.GetPressureLevel();
situation.RequiresMedicalCare = medicalPressure.HasCondition && medicalPressure.IsUntreated;
situation.HasCriticalCondition = medicalPressure.HasSevereCondition;
```

---

### Benefits of Orchestrator Integration

**Intelligent Illness Onset:**
- Medical Risk builds from fatigue, untreated conditions, campaign stress
- Orchestrator rolls daily when risk is high (3+)
- Context modifiers: winter +8%, siege +12%, exhaustion +10%
- Natural escalation from neglecting health

**Proactive Medical Care:**
- Camp opportunities appear when treatment needed
- Emergency opportunities forced when condition is critical
- Priority boosted based on severity
- Integrates with existing camp opportunity system

**Player Awareness:**
- Forecast warns about Medical Risk building
- Shows untreated conditions in CONCERNS section
- Tracks recovery progress in AHEAD section
- Clear feedback about health trajectory

**World State Integration:**
- Medical pressure affects activity level calculation
- Influences content selection and pacing
- Treated as simulation pressure like other systems
- Fully integrated with orchestrator architecture

---

## Implementation Steps

### Step 1: Create Decision JSON Files

Create 4 new decision files in `ModuleData/Enlisted/Decisions/`:

1. `medical_surgeon.json` - Surgeon care decision
2. `medical_rest.json` - Rest and recovery decision
3. `medical_herbal.json` - Herbal remedy purchase decision
4. `medical_emergency.json` - Emergency treatment decision

Each decision should:
- Use `HasAnyCondition` or `HasSevereCondition` requirements
- Include proper tooltips (factual, concise)
- Apply treatment effects via `PlayerConditionBehavior.ApplyTreatment()`
- Reset Medical Risk when appropriate
- Award Medicine XP for successful treatments

### Step 2: Add Localization Strings

Add to `ModuleData/Languages/enlisted_strings.xml`:

```xml
<!-- Medical Decision Narratives -->
<string id="dec_medical_surgeon_text" text="You visit the surgeon's tent. The smell of poultices and bloodied linen. The surgeon looks you over." />
<string id="dec_medical_surgeon_opt1" text="Request full treatment" />
<string id="dec_medical_surgeon_opt1_tt" text="Thorough treatment. Recovery rate doubled. Costs 2 fatigue." />
<!-- ... etc for all options ... -->
```

### Step 3: Update Requirement System

Add new condition requirements to `EventRequirementChecker.cs`:

```csharp
public bool HasAnyCondition()
{
    var cond = PlayerConditionBehavior.Instance;
    return cond?.State?.HasAnyCondition == true;
}

public bool HasSevereCondition()
{
    var cond = PlayerConditionBehavior.Instance;
    if (cond?.State == null) return false;
    
    return cond.State.CurrentInjury >= InjurySeverity.Severe ||
           cond.State.CurrentIllness >= IllnessSeverity.Severe;
}
```

Add to requirement JSON schema:
```json
{
  "condition": {
    "hasAnyCondition": true
  }
}
```

### Step 4: Remove Medical Menu Option

In `EnlistedMenuBehavior.cs`, remove the "Seek Medical Attention" menu option:

- Delete call to `EnlistedMedicalMenuBehavior.Instance?.AddMedicalOptionToEnlistedMenu(starter)`
- Remove any menu visibility logic related to medical conditions

### Step 5: Delete Medical Menu Behavior

Delete entire file:
- `src/Features/Conditions/EnlistedMedicalMenuBehavior.cs` (535 lines)

Remove from `SubModule.cs`:
```csharp
// DELETE THIS LINE
campaignStarter.AddBehavior(new EnlistedMedicalMenuBehavior());
```

Remove from `Enlisted.csproj`:
```xml
<!-- DELETE THIS LINE -->
<Compile Include="src\Features\Conditions\EnlistedMedicalMenuBehavior.cs" />
```

### Step 6: Update UI Status Display

Ensure condition status shows in:

1. **Enlisted Status Menu** - Brief condition summary in status text
2. **Decision Tooltips** - Show when conditions prevent activities
3. **Daily Brief** - Mention conditions in status section

Condition summary format:
```
Health: Recovering from injury (3 days remaining)
```

### Step 7: Orchestrator Integration (Phase 6H)

**Add medical pressure tracking:**
1. Update `SimulationPressureCalculator.cs` with `GetMedicalPressure()` method
2. Add `MedicalPressureAnalysis` class and `MedicalPressureLevel` enum
3. Track untreated days, low fatigue days, health percent

**Add camp opportunities:**
1. Add `opp_seek_treatment`, `opp_emergency_care`, `opp_medical_rest` to `camp_opportunities.json`
2. Set urgency and priority based on condition severity
3. Link to medical care decisions

**Create illness onset events:**
1. Create `ModuleData/Enlisted/Events/illness_onset.json`
2. Define `evt_illness_onset_fever` and `evt_illness_onset_injury_infection`
3. Set base chance and context modifiers (winter, siege, fatigue)

**Update ContentOrchestrator:**
1. Add `CheckMedicalPressure()` method to daily tick
2. Implement `TryTriggerIllnessOnset()` with probabilistic rolling
3. Force emergency opportunities when critical
4. Boost treatment priority when high pressure

**Update forecast system:**
1. Add `AddMedicalForecast()` method to `ForecastGenerator.cs`
2. Show condition recovery status in AHEAD
3. Show Medical Risk warnings in CONCERNS
4. Show exhaustion warnings

**Update world state:**
1. Add `MedicalPressure`, `RequiresMedicalCare`, `HasCriticalCondition` to `WorldSituation`
2. Calculate in `WorldStateAnalyzer.AnalyzeCurrentSituation()`

### Step 8: Update Documentation

Update references in:
- `docs/Features/Core/core-gameplay.md` - Update medical care section
- `docs/Features/UI/ui-systems-master.md` - Remove Medical Tent section
- `docs/BLUEPRINT.md` - Update enlisted_medical reference
- `docs/INDEX.md` - Update medical care references
- `docs/Features/Content/content-system-architecture.md` - Add medical orchestration section

---

## Files to Delete

### Source Files
- `src/Features/Conditions/EnlistedMedicalMenuBehavior.cs` (535 lines)

### Total Deletion
- **1 file**
- **535 lines of code**

---

## Files to Modify

### Add New Files
- `ModuleData/Enlisted/Decisions/medical_surgeon.json` (new)
- `ModuleData/Enlisted/Decisions/medical_rest.json` (new)
- `ModuleData/Enlisted/Decisions/medical_herbal.json` (new)
- `ModuleData/Enlisted/Decisions/medical_emergency.json` (new)

### Modify Existing Files
- `src/Mod.Entry/SubModule.cs` - Remove behavior registration
- `src/Features/Interface/Behaviors/EnlistedMenuBehavior.cs` - Remove menu option
- `src/Features/Content/EventRequirementChecker.cs` - Add condition requirements
- `ModuleData/Languages/enlisted_strings.xml` - Add decision strings
- `Enlisted.csproj` - Remove compile entry

### Update Documentation
- `docs/Features/Core/core-gameplay.md`
- `docs/Features/UI/ui-systems-master.md`
- `docs/BLUEPRINT.md`
- `docs/INDEX.md`

---

## Testing Checklist

### Functionality Tests

**Decision Availability:**
- [ ] Medical decisions appear ONLY when player has a condition
- [ ] Emergency decision appears ONLY for Severe/Critical conditions
- [ ] Herbal remedy available ONLY in settlements/garrison
- [ ] All decisions disappear when fully recovered

**Treatment Effects:**
- [ ] Surgeon treatment doubles recovery rate (2.0x multiplier)
- [ ] Rest treatment applies 1.5x recovery multiplier
- [ ] Herbal remedy applies 1.75x recovery multiplier (quality) or 1.5x (cheap)
- [ ] Emergency treatment applies 3.0x recovery multiplier

**Cost Deduction:**
- [ ] Fatigue costs properly consumed
- [ ] Denar costs properly deducted
- [ ] Insufficient resources show disabled option with tooltip

**Medical Risk Integration:**
- [ ] Treatment resets Medical Risk
- [ ] Refusing treatment increases Medical Risk
- [ ] Untreated conditions still increase Medical Risk daily

**UI Integration:**
- [ ] Condition status shows in Enlisted Status menu
- [ ] Decision tooltips show condition effects
- [ ] Daily Brief mentions active conditions
- [ ] Recovery feedback messages display correctly

### Regression Tests

**Condition System:**
- [ ] Injuries still apply from events/orders
- [ ] Illnesses still apply from triggers
- [ ] Daily recovery still ticks down days remaining
- [ ] Conditions still block training when severe
- [ ] Save/load preserves condition state

**Menu Navigation:**
- [ ] Enlisted Status menu loads without errors
- [ ] Decisions menu shows medical decisions when appropriate
- [ ] No broken menu references to Medical Tent
- [ ] Time control works properly in Decisions menu

### Edge Cases

- [ ] What happens if player gains condition while in Decisions menu?
- [ ] What happens if condition heals while viewing medical decision?
- [ ] Can player spam treatment options? (Should cost prevent this)
- [ ] Does refusing emergency treatment properly warn player?

---

## Benefits Summary

### Code Reduction
- **Delete:** 535 lines of C# menu code
- **Add:** ~150 lines of JSON decision definitions
- **Net:** -385 lines, -27% maintenance surface area

### UX Improvements
- **Consistency:** All camp activities in one place (Decisions menu)
- **Discoverability:** Medical care is now an option like any other activity
- **Less Navigation:** One less menu level to traverse
- **Better Tooltips:** Decision system provides better cost/benefit clarity

### System Integration
- **World State:** Medical decisions can be filtered by context (garrison/campaign/siege)
- **Opportunities:** Can create camp opportunities that link to medical decisions
- **Forecasting:** Medical needs can appear in forecast system
- **Content Orchestrator:** Medical care becomes part of unified content system

---

## Migration Timeline

**Estimated Total Time:** 4-5 hours (Phase 6G: 2-3 hours, Phase 6H: 2 hours)

### Phase 6G: Menu to Decisions Migration

| Task | Time | Dependencies |
|------|------|--------------|
| Create 4 decision JSON files | 45 min | None |
| Add localization strings | 15 min | Decision files |
| Update requirement system | 20 min | None |
| Remove menu option & behavior | 15 min | None |
| Update .csproj and SubModule | 10 min | None |
| Update UI status display | 30 min | None |
| Testing (decisions) | 20 min | All above |

**Subtotal:** ~2.5 hours

### Phase 6H: Orchestrator Integration

| Task | Time | Dependencies |
|------|------|--------------|
| Add medical pressure tracking | 30 min | Phase 6G |
| Create camp opportunities | 15 min | Phase 6G |
| Create illness onset events | 30 min | Phase 6G |
| Update ContentOrchestrator | 25 min | Pressure tracking |
| Update forecast system | 20 min | Pressure tracking |
| Update world state model | 15 min | Pressure tracking |
| Testing (orchestration) | 25 min | All above |

**Subtotal:** ~2.5 hours

### Total Implementation

**Combined Total:** ~5 hours

**Recommended Approach:**
1. Phase 6G first - get medical decisions working
2. Test decision system thoroughly
3. Phase 6H second - add orchestrator intelligence
4. Test orchestrator behavior (illness onset, opportunities, forecasts)
5. Bundle with other Phase 6G decision creation

---

**Last Updated:** 2026-01-01 (Added Phase 6H orchestrator integration)  
**Status:** Ready for Implementation
