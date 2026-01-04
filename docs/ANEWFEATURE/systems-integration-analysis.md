# Systems Integration Analysis

**Summary:** Comprehensive analysis of Enlisted's core systems (Supply, Morale, Reputation tracks, Escalation) and how they integrate with the ContentOrchestrator. Includes recommendations for improving orchestrator awareness, content effect utilization, and skill integration for more dynamic, state-aware content delivery.

**Status:** 📋 Planning (Phase-aware scheduling & commitment model ✅ IMPLEMENTED 2026-01-04)
**Last Updated:** 2026-01-04
**Related Docs:** [Content Effects Reference](content-effects-reference.md), [Content Skill Integration Plan](content-skill-integration-plan.md), [Camp Simulation System](../Features/Campaign/camp-simulation-system.md), [Identity System](../Features/Identity/identity-system.md), [ORCHESTRATOR-OPPORTUNITY-UNIFICATION.md](../ORCHESTRATOR-OPPORTUNITY-UNIFICATION.md)

---

## Executive Summary

Enlisted has **7 core tracking systems** that should drive content delivery:

| System | Range | Current Integration | Orchestrator Awareness |
|--------|-------|--------------------|-----------------------|
| **Supply** | 0-100 | ⚠️ Partial | Gates QM access, affects messaging |
| **Morale** | 0-100 | ⚠️ Partial | Background events only |
| **Rest/Fatigue** | 0-100 | ⚠️ Partial | Activity gating |
| **Readiness** | 0-100 | ⚠️ Partial | Combat effectiveness |
| **Soldier Rep** | -50 to +50 | ✅ Good | Event filtering, option gating |
| **Officer Rep** | -50 to +50 | ✅ Good | Promotion, order quality |
| **Lord Rep** | -50 to +50 | ✅ Good | Trust, assignments, discharge |
| **Scrutiny** | 0-10 | ✅ Good | Inspection events, discipline |
| **Discipline** | 0-10 | ✅ Good | Promotion blocking |
| **Medical Risk** | 0-5 | ✅ Good | Illness triggers, treatment |

**Key Finding:** Company Needs (Supply, Morale, Rest, Readiness) are underutilized for content steering. The orchestrator reads these values but doesn't use them to dynamically weight content selection or create tension-based narrative arcs.

---

## Index

1. [Core Systems Deep Dive](#core-systems-deep-dive)
2. [Current Orchestrator Integration](#current-orchestrator-integration)
3. [Gap Analysis](#gap-analysis)
4. [Improvement Recommendations](#improvement-recommendations)
5. [Implementation Roadmap](#implementation-roadmap)

---

## Core Systems Deep Dive

### 1. Company Needs (0-100 Scale)

**Location:** `src/Features/Company/CompanyNeedsManager.cs`, `CompanyNeedsState.cs`

Four transparent metrics that simulate the company's operational state:

| Need | Description | Affected By | Effects |
|------|-------------|-------------|---------|
| **Readiness** | Combat preparation | Training, drills, rest | Battle effectiveness, order success |
| **Morale** | Unit cohesion, spirits | Victories, pay, rest, leadership | Desertion risk, event options |
| **Supplies** | Food, water, equipment | Consumption, foraging, resupply | Starvation, QM access, attrition |
| **Rest** | Fatigue level | Marching, fighting, recovery | Performance, injury risk |

**Thresholds (from `simulation_config.json`):**

```
CRITICAL: <20    → Crisis events, severe penalties
LOW:      20-40  → Warning state, pressure building
NORMAL:   40-70  → Standard operations
GOOD:     70-90  → Positive modifiers
EXCELLENT: >90   → Bonus opportunities
```

**Current Content Integration:**
- Supply < 30% → Equipment purchases blocked at QM
- Supply < 20% → "Supply Crisis" pulse event fires
- Morale < 25% → "Morale Crisis" event, mutiny risk
- Rest < 15% → "Exhaustion Crisis" event

**Gap:** These are binary triggers. The orchestrator doesn't use them for:
- Weighted opportunity selection (low morale → more social opportunities)
- Narrative arc building (sustained pressure → climactic events)
- Proactive content (hints about approaching crisis)

---

### 2. Reputation Tracks (-50 to +50)

**Location:** `src/Features/Escalation/EscalationManager.cs`, `EscalationState.cs`

Three social standing tracks with the military hierarchy:

| Track | Description | Positive Effects | Negative Effects |
|-------|-------------|------------------|------------------|
| **Soldier Rep** | Peer respect | Better help in events, social opportunities | Isolation, hostility, theft |
| **Officer Rep** | NCO perception | Better assignments, promotion speed | Scrutiny, bad details, harassment |
| **Lord Rep** | Lord's trust | Special missions, better pay, protection | Discharge risk, dangerous assignments |

**Current Content Integration:**
- Requirements filtering (event needs `soldierRep >= 20`)
- Effect application (choice grants `+10 officerRep`)
- Promotion gating (T3 needs Soldier Rep ≥10, Leader Rel ≥10)
- Discharge band calculation (honorable needs neutral+ rep)

**Gap:** Reputation doesn't currently affect:
- Opportunity weighting (high rep → more opportunities appear)
- Narrative tone (high lord rep → different event text)
- Crisis prevention (high rep → buffer against discipline problems)

---

### 3. Escalation Tracks (0-10 Scale)

**Location:** `src/Features/Escalation/EscalationManager.cs`

Three pressure gauges that can spiral into serious consequences:

| Track | Description | Causes | Consequences |
|-------|-------------|--------|--------------|
| **Scrutiny** | How closely watched | Rule-breaking, suspicion, failed inspections | More inspections, reduced privacy |
| **Discipline** | Accumulated infractions | Insubordination, theft, fighting | Promotion blocked, punishment events |
| **Medical Risk** | Health vulnerability | Injuries, illness, poor conditions | Illness onset, worsening conditions |

**Threshold Events:**
- Discipline = 10 → Dishonorable discharge (kicked out)
- Medical Risk ≥ 3 → Illness onset probability increases significantly
- Scrutiny ≥ 7 → Inspection events highly likely

**Current Content Integration:**
- Promotion requirements (T3→T4 needs Discipline < 6)
- Event triggers (high scrutiny → inspection events)
- Medical system (risk level affects illness probability)

**Gap:** Escalation doesn't affect:
- Opportunity availability (high scrutiny → fewer covert options)
- NPC behavior (high discipline → officers warn player)
- Decay narratives (how scrutiny reduces over time)

---

### 4. Native Traits & Skills

**Location:** Native Bannerlord + `src/Features/Traits/TraitIntegrationHelper.cs`

**Personality Traits (-2 to +2):**
- Mercy, Valor, Honor, Generosity, Calculating

**Hidden Skill Traits (0-20):**
- Commander, Surgeon, Sergeant, Engineer, Rogue, Scout, Trader, Smuggler, Thug

**Current Content Integration:**
- Event requirements (`requires: { traits: { honor: { min: 1 } } }`)
- Trait XP effects (`effects: { traits: { valor: 50 } }`)
- Option gating (different choices visible based on traits)

**Gap:** Skills/traits don't currently affect:
- Opportunity fitness scoring (high Rogue → gambling opportunities weight higher)
- Automatic event selection (commander trait → leadership content)
- Skill check difficulty adjustments based on trait levels

---

## Current Orchestrator Integration

### What the Orchestrator Currently Does

**From `ContentOrchestrator.cs` and `WorldStateAnalyzer.cs`:**

```csharp
// World State Analysis
LordSituation     // Garrison, Campaign, Siege, etc.
WarStance         // Peace, Active, Desperate
ActivityLevel     // Quiet, Routine, Active, Intense
DayPhase          // Dawn, Midday, Dusk, Night
StrategicContext  // 8 contexts (Grand Campaign, Winter Camp, etc.)

// Scheduling Logic
ScheduleOpportunities()  // Once daily at 6am
PredictContextForPhase() // Guesses context for each phase
DetermineOpportunityBudget() // How many opportunities per phase
GenerateCandidatesForPhase() // Gets candidates from generator
BuildNarrativeHints() // Creates Daily Brief hints
```

### How Company Needs Currently Flow

```
CompanyNeedsState
    ↓
WorldStateAnalyzer.AnalyzeSituation()
    ↓
worldState["company_stressed"] = supplyPressure || moralePressure
    ↓
Used for BINARY decisions only:
  - Budget reduction (stressed → fewer opportunities)
  - Crisis event queueing
```

### How Reputation Currently Flows

```
EscalationState (SoldierRep, OfficerRep, LordRep)
    ↓
EventRequirementChecker.MeetsReputationRequirements()
    ↓
Filters events that don't meet requirements
    ↓
NOT used for:
  - Weighting (high rep events more likely)
  - Ordering (prioritize content for player's rep level)
  - Tone adjustment
```

---

## Gap Analysis

### Gap 1: Company Needs Are Binary, Not Gradient

**Current:** Supplies < 30 → Block equipment. That's it.

**Problem:** Player has no content difference between 31% and 99% supplies.

**Opportunity:** Use needs as continuous modifiers:

```csharp
// Proposed: Gradient need influence on opportunity fitness
float needModifier = CalculateNeedModifier(opportunity, companyNeeds);

// Examples:
// Supplies at 40% → Foraging opportunities +50% fitness
// Morale at 35% → Social opportunities +30% fitness
// Rest at 25% → Recovery opportunities +70% fitness
// Readiness at 80% → Training opportunities +20% fitness
```

### Gap 2: No "Pressure Narrative Arcs"

**Current:** Low supplies → crisis event → resolved (or not).

**Problem:** No narrative building. Crisis appears suddenly, resolves suddenly.

**Opportunity:** Track pressure duration and create escalating content:

```csharp
// Proposed: Pressure-aware content selection
public class PressureArc
{
    public string NeedType;         // "Supplies", "Morale", etc.
    public int DaysInPressure;      // Consecutive days below threshold
    public float IntensityTrend;    // Getting better or worse?
    
    // Day 1 low supplies: "Rations are getting thin."
    // Day 3 low supplies: "Men are grumbling about food."
    // Day 5 low supplies: "Private Jonas was caught stealing bread."
    // Day 7 low supplies: "The company is on the verge of mutiny."
}
```

### Gap 3: Reputation Doesn't Weight Content

**Current:** Soldier Rep 45 unlocks same content as Soldier Rep 100.

**Problem:** High reputation players don't feel the difference.

**Opportunity:** Use reputation for fitness scoring:

```csharp
// Proposed: Rep-weighted opportunity selection
if (opportunity.Category == "social" && soldierRep > 30)
{
    fitness *= 1.0f + (soldierRep / 100f); // Up to +50% for max rep
}

// High soldier rep → more social invites
// High officer rep → more leadership opportunities
// High lord rep → more special missions
```

### Gap 4: Traits Don't Influence Opportunity Generation

**Current:** Traits only gate options within events.

**Problem:** A high-Rogue player sees the same opportunities as a high-Honor player.

**Opportunity:** Trait-weighted fitness scoring:

```csharp
// Proposed: Trait influence on opportunity selection
var rogueLevel = Hero.MainHero.GetTraitLevel(DefaultTraits.RogueSkills);
if (opportunity.Id == "opp_dice_game" && rogueLevel > 5)
{
    fitness *= 1.3f; // Rogues see gambling opportunities more often
}

var surgeonLevel = Hero.MainHero.GetTraitLevel(DefaultTraits.Surgeon);
if (opportunity.Category == "medical" && surgeonLevel > 8)
{
    fitness *= 1.5f; // Surgeons see medical opportunities more often
}
```

### Gap 5: Skills Underutilized in Content Effects

**Current:** Most content grants Scouting, Athletics, Leadership XP.

**Problem:** Vigor (melee) and Control (ranged) attributes get minimal XP.

**From Content Skill Integration Plan:**

| Attribute | Current Coverage | Gap |
|-----------|-----------------|-----|
| Vigor (OneHanded, TwoHanded, Polearm) | 🔴 Low | Need sparring, combat drill content |
| Control (Bow, Crossbow, Throwing) | 🔴 Very Low | Need archery, hunting content |
| Endurance (Riding, Athletics, Crafting) | 🟢 Good | Well covered |
| Cunning (Scouting, Tactics, Roguery) | 🟢 Excellent | Well covered |
| Social (Charm, Leadership, Trade) | 🟢 Good | Well covered |
| Intelligence (Steward, Medicine, Engineering) | 🟡 Medium | Need logistics, engineering content |

---

## Improvement Recommendations

### Recommendation 1: Gradient Need Influence

**Goal:** Make Company Needs affect content selection continuously, not just at thresholds.

**Implementation:**

```csharp
// In CampOpportunityGenerator.CalculateFitness()

private float ApplyNeedModifiers(CampOpportunity opp, CompanyNeedsState needs)
{
    float modifier = 1.0f;
    
    // Supply pressure → boost foraging/economic opportunities
    if (needs.Supplies < 50)
    {
        float supplyPressure = (50 - needs.Supplies) / 50f; // 0.0 to 1.0
        if (opp.Category == "economic" || opp.Tags.Contains("foraging"))
        {
            modifier += supplyPressure * 0.5f; // Up to +50% at 0 supplies
        }
    }
    
    // Morale pressure → boost social/recovery opportunities
    if (needs.Morale < 50)
    {
        float moralePressure = (50 - needs.Morale) / 50f;
        if (opp.Category == "social" || opp.Category == "recovery")
        {
            modifier += moralePressure * 0.4f; // Up to +40%
        }
    }
    
    // Rest pressure → boost recovery, reduce training
    if (needs.Rest < 40)
    {
        float restPressure = (40 - needs.Rest) / 40f;
        if (opp.Category == "recovery")
        {
            modifier += restPressure * 0.6f; // Up to +60%
        }
        if (opp.Category == "training")
        {
            modifier -= restPressure * 0.3f; // Up to -30%
        }
    }
    
    // High readiness → training less needed
    if (needs.Readiness > 80 && opp.Category == "training")
    {
        modifier *= 0.7f; // Already ready, less training pressure
    }
    
    return modifier;
}
```

**Content Changes:**
- Tag existing opportunities with need-relevance (`"affects": ["supplies", "morale"]`)
- Create opportunities that explicitly address specific needs

---

### Recommendation 2: Pressure Arc Tracking

**Goal:** Build narrative tension over time as needs stay critical.

**Implementation:**

```csharp
// New: CompanyPressureTracker.cs

public class PressureArc
{
    public string NeedType { get; set; }
    public int DaysBelowWarning { get; set; }      // Days below 40
    public int DaysBelowCritical { get; set; }     // Days below 20
    public float PreviousValue { get; set; }
    public PressureTrend Trend { get; set; }       // Improving, Stable, Worsening
}

public enum PressureStage
{
    Normal,         // 0 days
    Tension,        // 1-2 days warning
    Stress,         // 3-4 days warning
    Crisis,         // 5+ days or critical
    Breaking        // 7+ days critical
}

// Orchestrator uses this for content selection
public PressureStage GetOverallPressure()
{
    var worstArc = _arcs.Values
        .OrderByDescending(a => a.DaysBelowWarning + a.DaysBelowCritical * 2)
        .FirstOrDefault();
    
    if (worstArc == null) return PressureStage.Normal;
    
    return (worstArc.DaysBelowCritical, worstArc.DaysBelowWarning) switch
    {
        (>= 7, _) => PressureStage.Breaking,
        (>= 3, _) => PressureStage.Crisis,
        (>= 1, _) => PressureStage.Crisis,
        (_, >= 5) => PressureStage.Stress,
        (_, >= 3) => PressureStage.Tension,
        _ => PressureStage.Normal
    };
}
```

**Content Changes:**
- Create tiered narrative events for each pressure stage
- Use `hintId` fields to foreshadow approaching problems

| Pressure Stage | Content Examples |
|----------------|------------------|
| Tension | "Rations look thin" hint in Daily Brief |
| Stress | Event: "Soldiers grumble about food" |
| Crisis | Event: "Private caught stealing" + decision |
| Breaking | Event: "Mutiny brewing" - major choice |

---

### Recommendation 3: Reputation-Weighted Fitness

**Goal:** High reputation = more/better opportunities in that domain.

**Implementation:**

```csharp
// In CampOpportunityGenerator.CalculateFitness()

private float ApplyReputationModifiers(CampOpportunity opp, EscalationState rep)
{
    float modifier = 1.0f;
    
    // Soldier Rep affects social opportunities
    if (opp.Category == "social")
    {
        // -50 rep = 0.5x, 0 rep = 1.0x, +50 rep = 1.5x
        modifier *= 1.0f + (rep.SoldierReputation / 100f);
    }
    
    // Officer Rep affects training/leadership opportunities
    if (opp.Category == "training" || opp.Tags.Contains("leadership"))
    {
        modifier *= 1.0f + (rep.OfficerReputation / 100f);
    }
    
    // Lord Rep affects special mission opportunities
    if (opp.Tags.Contains("special") || opp.Tags.Contains("trusted"))
    {
        modifier *= 1.0f + (rep.LordReputation / 100f);
    }
    
    // Negative rep can BLOCK opportunities
    if (rep.SoldierReputation < -20 && opp.Category == "social")
    {
        modifier *= 0.3f; // Soldiers avoid you
    }
    
    return modifier;
}
```

**Content Changes:**
- Add `"tags": ["trusted", "leadership", "social"]` to opportunity definitions
- Create exclusive high-rep content (Lord Rep 40+ → special mission offers)
- Create negative-rep content (Soldier Rep -30 → confrontation events)

---

### Recommendation 4: Trait-Influenced Content Selection

**Goal:** Player's traits shape what opportunities appear, not just what options are available.

**Implementation:**

```csharp
// In CampOpportunityGenerator.CalculateFitness()

private float ApplyTraitModifiers(CampOpportunity opp)
{
    float modifier = 1.0f;
    var hero = Hero.MainHero;
    
    // Hidden traits influence related opportunities
    var traitInfluences = new Dictionary<string, (TraitObject trait, string[] categories, float weight)>
    {
        { "rogue", (DefaultTraits.RogueSkills, new[] { "gambling", "covert" }, 0.03f) },
        { "surgeon", (DefaultTraits.Surgeon, new[] { "medical", "recovery" }, 0.04f) },
        { "scout", (DefaultTraits.ScoutSkills, new[] { "scouting", "patrol" }, 0.03f) },
        { "commander", (DefaultTraits.Commander, new[] { "leadership", "training" }, 0.03f) },
        { "trader", (DefaultTraits.Trader, new[] { "economic", "trade" }, 0.03f) },
    };
    
    foreach (var (key, influence) in traitInfluences)
    {
        int level = hero.GetTraitLevel(influence.trait);
        if (level > 0 && influence.categories.Any(c => opp.Category == c || opp.Tags.Contains(c)))
        {
            modifier += level * influence.weight; // +3% per level for relevant content
        }
    }
    
    // Personality traits influence tone-matched opportunities
    int mercyLevel = hero.GetTraitLevel(DefaultTraits.Mercy);
    if (mercyLevel > 0 && opp.Tags.Contains("compassionate"))
    {
        modifier += mercyLevel * 0.1f; // Merciful players see helpful opportunities
    }
    
    int calculatingLevel = hero.GetTraitLevel(DefaultTraits.Calculating);
    if (calculatingLevel > 0 && opp.Tags.Contains("strategic"))
    {
        modifier += calculatingLevel * 0.1f;
    }
    
    return modifier;
}
```

**Content Changes:**
- Add trait-relevant tags to opportunities
- Create trait-specific opportunities that feel personalized

---

### Recommendation 5: Skill XP Balance Improvements

**Goal:** Fill coverage gaps for Vigor, Control, and Intelligence attributes.

**From Content Skill Integration Plan - Priority Actions:**

| Gap | New Content Needed |
|-----|-------------------|
| **Vigor** | `opp_sparring_ring`, `opp_heavy_weapons_drill`, `dec_sword_practice` |
| **Control** | `opp_archery_range`, `opp_hunting_party`, `dec_target_practice` |
| **Intelligence** | `opp_supply_inventory`, `opp_fortification_help`, `dec_logistics_assist` |

**Implementation - Thematic Skill Aliases:**

Update `SkillCheckHelper.GetSkillByName()`:

```csharp
public static SkillObject GetSkillByName(string name)
{
    return name.ToLower() switch
    {
        // Existing
        "perception" => DefaultSkills.Scouting,
        "smithing" => DefaultSkills.Crafting,
        
        // NEW: Vigor
        "swordplay" or "meleecombat" => DefaultSkills.OneHanded,
        "heavyweapons" => DefaultSkills.TwoHanded,
        "pikework" or "spearwork" => DefaultSkills.Polearm,
        
        // NEW: Control
        "archery" or "bowmanship" => DefaultSkills.Bow,
        "marksmanship" => DefaultSkills.Crossbow,
        "javelinwork" => DefaultSkills.Throwing,
        
        // NEW: Endurance
        "horsemanship" => DefaultSkills.Riding,
        "endurance" or "physicallabor" => DefaultSkills.Athletics,
        "repair" => DefaultSkills.Crafting,
        
        // NEW: Cunning
        "awareness" or "vigilance" => DefaultSkills.Scouting,
        "battleplanning" => DefaultSkills.Tactics,
        "stealth" or "cunning" => DefaultSkills.Roguery,
        
        // NEW: Social
        "persuasion" or "diplomacy" => DefaultSkills.Charm,
        "command" or "inspiration" => DefaultSkills.Leadership,
        "bargaining" => DefaultSkills.Trade,
        
        // NEW: Intelligence
        "logistics" or "administration" => DefaultSkills.Steward,
        "healing" or "surgery" => DefaultSkills.Medicine,
        "fortification" or "siegecraft" => DefaultSkills.Engineering,
        
        // Direct skill names (fallback)
        _ => DefaultSkills.All.FirstOrDefault(s => 
            s.StringId.Equals(name, StringComparison.OrdinalIgnoreCase))
    };
}
```

---

### Recommendation 6: Orchestrator Schedule Override Expansion

**Goal:** Make orchestrator actively reshape the day based on company state.

**Current:** Schedule overrides only fire at extreme thresholds.

**Proposed - Proactive Scheduling:**

```csharp
// In ContentOrchestrator.ScheduleOpportunities()

private void ApplyProactiveScheduling(WorldSituation world, CompanyNeedsState needs)
{
    // Morale is trending down → schedule social opportunities
    if (_pressureTracker.GetTrend("Morale") == PressureTrend.Worsening)
    {
        InjectGuaranteedOpportunity(DayPhase.Dusk, "opp_campfire_stories");
        ModLogger.Info(LogCategory, "Proactive: Scheduled social event due to morale decline");
    }
    
    // Rest is critical → reduce training, boost recovery
    if (needs.Rest < 30)
    {
        RemoveOpportunitiesOfCategory(DayPhase.Dawn, "training");
        InjectGuaranteedOpportunity(DayPhase.Midday, "opp_short_rest");
    }
    
    // Supplies trending critical → inject foraging
    if (_pressureTracker.WillCrossThreshold("Supplies", 30, daysAhead: 2))
    {
        InjectGuaranteedOpportunity(DayPhase.Midday, "opp_forage");
        AddDailyBriefHint("Camp supplies won't last much longer.");
    }
    
    // High reputation → unlock special opportunities
    if (rep.LordReputation >= 40 && MBRandom.RandomFloat < 0.15f)
    {
        InjectGuaranteedOpportunity(DayPhase.Dawn, "opp_special_assignment");
    }
}
```

---

## Implementation Roadmap

### Phase 1: Foundation (1-2 days)

**Goal:** Add gradient need influence to opportunity scoring.

| Task | File | Effort |
|------|------|--------|
| Add `ApplyNeedModifiers()` to fitness calculation | `CampOpportunityGenerator.cs` | 2 hrs |
| Add need-relevant tags to 15 key opportunities | `camp_opportunities.json` | 2 hrs |
| Test gradient behavior at various need levels | Manual testing | 2 hrs |

### Phase 2: Pressure Tracking (2-3 days)

**Goal:** Track sustained pressure and create narrative arcs.

| Task | File | Effort |
|------|------|--------|
| Create `CompanyPressureTracker.cs` | New file | 4 hrs |
| Integrate with daily tick | `ContentOrchestrator.cs` | 2 hrs |
| Create tiered pressure events (4 per need × 4 needs) | JSON content | 6 hrs |
| Add pressure stage hints to Daily Brief | `EnlistedNewsBehavior.cs` | 2 hrs |

### Phase 3: Reputation Weighting (1-2 days)

**Goal:** High reputation = better content selection.

| Task | File | Effort |
|------|------|--------|
| Add `ApplyReputationModifiers()` to fitness | `CampOpportunityGenerator.cs` | 2 hrs |
| Add reputation tags to opportunities | `camp_opportunities.json` | 2 hrs |
| Create high-rep exclusive content (5 opportunities) | JSON content | 4 hrs |
| Create negative-rep confrontation events (3 events) | JSON content | 3 hrs |

### Phase 4: Trait Influence (2-3 days)

**Goal:** Player traits shape content selection.

| Task | File | Effort |
|------|------|--------|
| Add `ApplyTraitModifiers()` to fitness | `CampOpportunityGenerator.cs` | 3 hrs |
| Add trait-relevant tags to opportunities | `camp_opportunities.json` | 2 hrs |
| Create trait-specific opportunities (10 total) | JSON content | 6 hrs |
| Test trait influence on content mix | Manual testing | 2 hrs |

### Phase 5: Skill XP Balance (2-3 days)

**Goal:** Fill attribute coverage gaps.

| Task | File | Effort |
|------|------|--------|
| Implement all thematic skill aliases | `SkillCheckHelper.cs` | 2 hrs |
| Create Vigor training content (3 decisions) | `camp_decisions.json` | 3 hrs |
| Create Control training content (3 decisions) | `camp_decisions.json` | 3 hrs |
| Create Intelligence content (3 decisions) | `camp_decisions.json` | 3 hrs |
| Audit existing content for thematic consistency | All content files | 4 hrs |

### Phase 6: Proactive Scheduling (2-3 days)

**Goal:** Orchestrator actively shapes the day.

| Task | File | Effort |
|------|------|--------|
| Add `ApplyProactiveScheduling()` | `ContentOrchestrator.cs` | 4 hrs |
| Add trend detection to pressure tracker | `CompanyPressureTracker.cs` | 2 hrs |
| Create guaranteed-slot opportunities (5-8) | `camp_opportunities.json` | 4 hrs |
| Add proactive hints to Daily Brief | `EnlistedNewsBehavior.cs` | 2 hrs |

---

## Success Metrics

### Measurable Goals

| Metric | Current | Target | How to Measure |
|--------|---------|--------|----------------|
| Need influence on content | Binary | Gradient | Log fitness modifiers |
| Pressure narrative events | 4 total | 16+ (4 stages × 4 needs) | Content count |
| Rep-weighted opportunities | 0% | 50%+ have rep influence | Audit tags |
| Trait-influenced fitness | 0 traits | 10+ traits considered | Code review |
| Vigor skill XP sources | 13 | 20+ | Content audit |
| Control skill XP sources | 6 | 15+ | Content audit |
| Intelligence skill XP sources | 15 | 20+ | Content audit |

### Player Experience Goals

1. **"The game responds to my situation"** - Low supplies → foraging opportunities appear naturally
2. **"My reputation matters"** - High soldier rep → more social invites, friendlier tone
3. **"My character feels unique"** - High rogue trait → gambling/covert content appears more
4. **"Crises build tension"** - Low morale builds over days, not sudden crisis
5. **"I can train all my skills"** - Melee and ranged training options readily available

---

## Quick Reference: Effect Types for Content

When writing content, use these effect types to touch all systems:

### Company Needs Effects

```json
"effects": {
  "companyNeeds": {
    "Readiness": 10,
    "Morale": -5,
    "Rest": 15,
    "Supplies": -10
  }
}
```

### Reputation Effects

```json
"effects": {
  "soldierRep": 10,
  "officerRep": -5,
  "lordRep": 3
}
```

### Escalation Effects

```json
"effects": {
  "scrutiny": 2,
  "discipline": -1,
  "medicalRisk": 1
}
```

### Trait Effects

```json
"effects": {
  "traitXp": {
    "Valor": 50,
    "Honor": -30,
    "Commander": 100
  }
}
```

### Skill XP Effects (with thematic aliases)

```json
"effects": {
  "skillXp": {
    "Swordplay": 15,      // → OneHanded
    "Archery": 12,        // → Bow
    "Vigilance": 20,      // → Scouting
    "Healing": 18,        // → Medicine
    "Logistics": 10       // → Steward
  }
}
```

---

## Appendix: System Interaction Matrix

Shows how systems affect each other:

```
                    ┌─────────────────────────────────────────┐
                    │           CONTENT ORCHESTRATOR          │
                    │  (Reads all, schedules opportunities)   │
                    └────────────────┬────────────────────────┘
                                     │
           ┌─────────────────────────┼─────────────────────────┐
           ▼                         ▼                         ▼
┌──────────────────┐    ┌──────────────────┐    ┌──────────────────┐
│  COMPANY NEEDS   │    │    REPUTATION    │    │   ESCALATION     │
│ Supply, Morale   │    │ Soldier, Officer │    │ Scrutiny, Disc.  │
│ Rest, Readiness  │    │     Lord         │    │  Medical Risk    │
└────────┬─────────┘    └────────┬─────────┘    └────────┬─────────┘
         │                       │                       │
         │    ┌──────────────────┼──────────────────┐    │
         │    │                  │                  │    │
         ▼    ▼                  ▼                  ▼    ▼
┌──────────────────────────────────────────────────────────────────┐
│                       CONTENT EFFECTS                             │
│  • Skill XP (18 skills via aliases)                              │
│  • Gold, HP, Renown                                               │
│  • Trait XP (5 personality + 10 hidden)                          │
│  • Company Needs deltas                                           │
│  • Reputation deltas                                              │
│  • Escalation deltas                                              │
│  • Party effects (troop loss, retinue)                           │
│  • Narrative (chain events, discharge)                           │
└──────────────────────────────────────────────────────────────────┘
         │                       │                       │
         ▼                       ▼                       ▼
┌──────────────────┐    ┌──────────────────┐    ┌──────────────────┐
│ NATIVE BANNERLORD│    │  EVENT DELIVERY  │    │ PLAYER FEEDBACK  │
│  Hero stats      │    │  News feed       │    │  Combat log      │
│  Skills, Traits  │    │  Daily Brief     │    │  Menu updates    │
│  Gold, HP        │    │  Story outcomes  │    │  Tooltips        │
└──────────────────┘    └──────────────────┘    └──────────────────┘
```

---

**Document End**
