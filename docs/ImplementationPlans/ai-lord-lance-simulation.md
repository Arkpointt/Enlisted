# AI Lord Lance Simulation System

**Status:** Design Phase  
**Category:** Core Feature  
**Dependencies:** Lance Life Simulation, AI Camp Schedule, Bannerlord Party System  
**Related Docs:** `lance-life-simulation.md`, `ai-camp-schedule.md`

---

## Table of Contents

1. [Overview](#overview)
2. [Core Concept](#core-concept)
3. [System Architecture](#system-architecture)
4. [Lance Generation & Management](#lance-generation--management)
5. [Company Status Simulation](#company-status-simulation)
6. [Lance Member Simulation](#lance-member-simulation)
7. [Menu Interface](#menu-interface)
8. [Technical Implementation](#technical-implementation)
9. [Integration Points](#integration-points)
10. [Event System Integration](#event-system-integration)
11. [News & Dispatch Integration](#news--dispatch-integration)
12. [Configuration](#configuration)
13. [File Structure](#file-structure)
14. [Implementation Roadmap](#implementation-roadmap)
15. [Open Questions & Design Decisions](#open-questions--design-decisions)
16. [Future Enhancements](#future-enhancements)
17. [Content Authoring Guide](#content-authoring-guide)
18. [Summary](#summary)

---

## Overview

The AI Lord Lance Simulation creates a living, breathing military structure for every lord's army in the game. Instead of viewing armies as abstract troop numbers, players can see the actual organizational structure - the lances that make up the army, the men in each lance, and the overall health of the company.

This system provides:
- **Realistic Military Structure:** Lords' armies are organized into lances just like the player's
- **Dynamic Lance Management:** Lances form and dissolve based on recruitment and casualties
- **Visible Personnel:** Named lance members with ranks and roles
- **Company Status:** Aggregate view of morale, supplies, and readiness
- **Intelligence Opportunity:** Players can scout enemy army composition and status

**Key Philosophy:** We simulate based on real-world data (actual troop counts) but use procedural generation for the details. This keeps memory footprint low while providing rich detail when the player looks.

---

## Core Concept

### The Military Reality

Every lord's army operates like the player's:
- **Lance Structure:** 8-12 men per lance (configurable)
- **Hierarchical Organization:** Each lance has a leader, veterans, and soldiers
- **Dynamic Composition:** Lances form as troops are recruited, dissolve as casualties mount
- **Cultural Identity:** Lance names and member names match the lord's culture

### Simulation Scope

**What We Simulate:**
- Number of lances (derived from total troop count)
- Lance composition (officers, veterans, soldiers)
- Lance names (culture-appropriate)
- Member names (generated on-demand)
- Company-level statistics (morale, food status, readiness)
- Casualty distribution (rank-weighted)

**What We Don't Simulate:**
- Individual member personalities (only player's lance has this)
- Daily schedules for AI lance members
- Inter-member relationships
- Individual equipment (use troop tier instead)

---

## System Architecture

### High-Level Design

```
MobileParty (Lord's Army)
    ↓
AI Lance Simulation Behavior (tracks simulation state)
    ↓
    ├─→ Lance Registry (generated lances)
    │   └─→ Individual Lances (8-12 members each)
    │       └─→ Lance Members (name, rank, tier)
    │
    └─→ Company Status (aggregate statistics)
        ├─→ Morale (average)
        ├─→ Food Status (days remaining)
        ├─→ Cohesion (formation integrity)
        └─→ Readiness (combat effectiveness)
```

### Data Flow

```
Battle/Recruitment Event
    ↓
Troop Count Change Detected
    ↓
Recalculate Lance Count
    ↓
┌──────────────┴──────────────┐
│                              │
↓ (Count Increased)            ↓ (Count Decreased)
Generate New Lance(s)          Apply Casualties
├─ Generate lance name         ├─ Select casualties (rank-weighted)
├─ Assign members              ├─ Remove from lances
└─ Set initial status          └─ Dissolve empty lances
    ↓                              ↓
Update Company Status ←────────────┘
```

---

## Lance Generation & Management

### Lance Count Calculation

**Formula:**
```
lance_count = floor(total_troop_count / troops_per_lance)

Default: troops_per_lance = 10
Configurable range: 8-12
```

**Example:**
- 45 troops → 4 lances (4 lances × 10 = 40 assigned, 5 in reserve pool)
- 103 troops → 10 lances (10 lances × 10 = 100 assigned, 3 in reserve pool)

### Lance Creation

When a new lance is needed:

```
1. Generate Lance Name
   └─→ Use Bannerlord's name generator with lord's culture
       Format: "{Culture Adjective} {Military Term}" or "{Heroic Name}'s {Unit Type}"
       Examples: "Bold Hawks", "Stormborn Riders", "Caladog's Vanguard"

2. Determine Lance Composition
   └─→ Based on party's troop tier distribution
       Officer (1): Tier 4-6 troop
       Veterans (2-3): Tier 3-4 troops
       Soldiers (5-7): Tier 1-3 troops
       Total: 8-12 members

3. Generate Member Names
   └─→ Use Bannerlord's FirstName generator
       Culture: Lord's culture
       Gender: Based on troop type (if applicable)
       Cached: Names generated once, stored with member

4. Assign Ranks
   └─→ Based on tier:
       Tier 5-6: Lance Leader
       Tier 4: Sergeant
       Tier 3: Corporal
       Tier 2: Lance Corporal
       Tier 1: Private

5. Initialize Member State
   └─→ All start healthy, on duty
       Survival weight assigned based on rank
```

### Lance Dissolution

When troop count decreases:

```
1. Calculate Casualties
   total_losses = previous_troop_count - current_troop_count

2. Distribute Casualties (Rank-Weighted)
   Survival Weights:
   ├─ Lance Leader (Tier 5-6): 90% survival
   ├─ Sergeant (Tier 4): 80% survival  
   ├─ Corporal (Tier 3): 70% survival
   ├─ Lance Corporal (Tier 2): 60% survival
   └─ Private (Tier 1): 50% survival

3. Remove Casualties
   ├─ Select from lance members using weighted random
   ├─ Remove from lance rosters
   └─ Update member counts

4. Consolidate Lances
   ├─ If lance drops below 5 members: dissolve
   ├─ Reassign survivors to other lances
   └─ Recalculate total lance count

5. Handle Lance Loss
   If entire lance wiped out:
   ├─ Remove from registry
   ├─ Generate "Lance Lost" status flag
   └─ Affects company morale (-5 for full lance loss)
```

### Lance Name Generation

**Name Components:**

```json
{
  "formats": [
    "{adjective} {unit_type}",
    "{heroic_term}'s {unit_type}",
    "{culture_term} {military_term}",
    "The {adjective} {count}"
  ],
  "adjectives": ["Bold", "Stalwart", "Iron", "Swift", "Thunder", "Storm"],
  "unit_types": ["Lances", "Blades", "Hawks", "Wolves", "Guards"],
  "military_terms": ["Vanguard", "Rearguard", "Raiders", "Wardens"],
  "culture_terms": "generated_from_bannerlord_culture_data"
}
```

**Implementation:**
```csharp
// Use Bannerlord's native name generator
string adjective = MBTextManager.GetCultureSpecificText(culture, "adjectives");
string unitType = MBTextManager.GetCultureSpecificText(culture, "unit_types");
string lanceName = $"{adjective} {unitType}";

// Fallback to simple numbered system if generation fails
if (string.IsNullOrEmpty(lanceName))
{
    lanceName = $"{lordName}'s {lanceNumber} Lance";
}
```

---

## Company Status Simulation

The company status represents the aggregate health of the lord's army. This is what players see at the top of the Lance Inspector menu.

### Status Components

#### 1. Morale
**Range:** 0-100  
**Calculation:**
```
base_morale = party.morale (from Bannerlord)
modifiers:
  - Recent victories: +10
  - Recent defeats: -15
  - Low food: -10
  - Full lance wiped: -5 per lance
  - High casualties (>30% in battle): -10
```

**Display:**
- 80-100: "High Spirits" (green)
- 60-79: "Steady" (yellow)
- 40-59: "Shaken" (orange)
- 0-39: "Broken" (red)

#### 2. Food Status
**Range:** Days of food remaining (0-30+)  
**Calculation:**
```
days_remaining = party.food / party.daily_food_consumption
```

**Display:**
- 10+ days: "Well Supplied" (green)
- 5-9 days: "Adequate" (yellow)
- 2-4 days: "Low Supplies" (orange)
- 0-1 days: "Starving" (red)

#### 3. Cohesion
**Range:** 0-100  
**Calculation:**
```
base_cohesion = party.total_strength / party.party_size_limit * 100
modifiers:
  - All lances at full strength: +10
  - Multiple depleted lances: -5 per depleted lance
  - Mixed cultures in party: -10
  - Recent battles (unit familiarity): +5
```

**Display:**
- 80-100: "Elite Formation" (green)
- 60-79: "Disciplined" (yellow)
- 40-59: "Disorganized" (orange)
- 0-39: "Scattered" (red)

#### 4. Combat Readiness
**Range:** 0-100  
**Calculation:**
```
readiness = (morale * 0.4) + (cohesion * 0.3) + (food_security * 0.2) + (equipment_quality * 0.1)

where:
  food_security = min(days_remaining / 10, 1.0) * 100
  equipment_quality = average_troop_tier / 6 * 100
```

**Display:**
- 80-100: "Battle Ready" (green)
- 60-79: "Combat Capable" (yellow)
- 40-59: "Diminished" (orange)
- 0-39: "Combat Ineffective" (red)

---

## Lance Member Simulation

### Member Data Structure

Each lance member is represented by:

```csharp
public class SimulatedLanceMember
{
    public string Name { get; set; }                    // Generated once
    public string Rank { get; set; }                    // Based on tier
    public int Tier { get; set; }                       // 1-6
    public FormationClass Formation { get; set; }       // Infantry, Archer, Cavalry, etc.
    public MemberState State { get; set; }              // Healthy, Wounded, Dead
    public float SurvivalWeight { get; set; }           // Rank-based survival chance
    public string LanceId { get; set; }                 // Which lance they belong to
}

public enum MemberState
{
    Healthy,        // Full combat capability
    Wounded,        // Survived battle but injured
    Dead            // Killed in action
}
```

### Casualty Application

When a battle occurs:

```csharp
public void ApplyCasualties(MobileParty party, int casualtyCount)
{
    // 1. Get all healthy members
    var availableMembers = GetAllMembers()
        .Where(m => m.State == MemberState.Healthy)
        .ToList();
    
    // 2. Apply weighted selection
    for (int i = 0; i < casualtyCount; i++)
    {
        var casualty = SelectCasualtyWeighted(availableMembers);
        
        // 70% killed, 30% wounded (can return later)
        casualty.State = MBRandom.RandomFloat < 0.7f 
            ? MemberState.Dead 
            : MemberState.Wounded;
        
        availableMembers.Remove(casualty);
    }
    
    // 3. Reorganize lances
    ReorganizeLances();
}

private SimulatedLanceMember SelectCasualtyWeighted(List<SimulatedLanceMember> members)
{
    // Lower survival weight = higher chance of becoming casualty
    float totalInverseWeight = members.Sum(m => 1f / m.SurvivalWeight);
    float roll = MBRandom.RandomFloat * totalInverseWeight;
    
    float cumulative = 0f;
    foreach (var member in members)
    {
        cumulative += 1f / member.SurvivalWeight;
        if (roll <= cumulative)
            return member;
    }
    
    return members.Last(); // Fallback
}
```

### Survival Weights by Rank

```
Lance Leader:    0.90  (90% survival per battle)
Sergeant:        0.80  (80% survival)
Corporal:        0.70  (70% survival)
Lance Corporal:  0.60  (60% survival)
Private:         0.50  (50% survival)
```

**Rationale:** 
- Officers and veterans are more experienced, better equipped
- They're often positioned more strategically in battle
- Creates realistic attrition where junior troops bear brunt of casualties
- Lances maintain leadership continuity longer

### Recruitment Simulation

When party recruits troops:

```csharp
public void AddRecruits(int recruitCount)
{
    // 1. Calculate how many new lances can form
    int currentTotal = GetTotalTroopCount();
    int newTotal = currentTotal + recruitCount;
    int currentLanceCount = currentTotal / _troopsPerLance;
    int newLanceCount = newTotal / _troopsPerLance;
    
    // 2. If new lances needed, generate them
    if (newLanceCount > currentLanceCount)
    {
        int lancesToCreate = newLanceCount - currentLanceCount;
        for (int i = 0; i < lancesToCreate; i++)
        {
            GenerateNewLance();
        }
    }
    
    // 3. Distribute new recruits to existing lances (fill to capacity)
    DistributeRecruitsToLances(recruitCount);
}
```

---

## Menu Interface

### Lance Inspector Menu

Players can access this menu when:
- In an encounter with the lord's party
- After selecting "Inspect Army" option
- Through diplomacy/conversation options
- Via intelligence gathering (scout/spy actions)

### Menu Structure

```
╔════════════════════════════════════════════════════════════════╗
║         {LORD_NAME}'s COMPANY - {FACTION_NAME}                 ║
╠════════════════════════════════════════════════════════════════╣
║                                                                ║
║  Company Status:                                               ║
║  ┌──────────────────────────────────────────────────────────┐ ║
║  │ Morale: [████████░░] High Spirits (85/100)               │ ║
║  │ Food:   [███████░░░] 8 Days Remaining                    │ ║
║  │ Cohesion: [██████░░░░] Disciplined (68/100)              │ ║
║  │ Readiness: [███████░░░] Battle Ready (78/100)            │ ║
║  └──────────────────────────────────────────────────────────┘ ║
║                                                                ║
║  Total Troops: 87    Lances: 8    Avg. Lance Size: 10.9      ║
║                                                                ║
╠════════════════════════════════════════════════════════════════╣
║  LANCES:                                                       ║
║                                                                ║
║  [1] Iron Hawks                    [11 men] [All Ranks]       ║
║      > Galter (Lance Leader)       Infantry                   ║
║      > Rolf (Sergeant)             Infantry                   ║
║      > Aedrin, Cadoc, Elwin... (8 more)                       ║
║                                                                ║
║  [2] Stormborn Riders              [10 men] [All Ranks]       ║
║      > Carac (Lance Leader)        Cavalry                    ║
║      > Drest (Sergeant)            Cavalry                    ║
║      > Morcar, Taliesin... (7 more)                           ║
║                                                                ║
║  [3] The Bold Seven                [7 men]  [3 Wounded]       ║
║      > Caradog (Lance Leader)      Infantry                   ║
║      > [No Sergeant - KIA]                                    ║
║      > Elstan, Gwriad... (5 more)                             ║
║                                                                ║
║  [4] Thunder Blades                [12 men] [All Ranks]       ║
║  [5] Swift Vanguard                [11 men] [All Ranks]       ║
║  [6] Cunedda's Guards              [9 men]  [1 Wounded]       ║
║  [7] Gwallog's Wardens             [10 men] [All Ranks]       ║
║  [8] The Stalwart                  [8 men]  [All Ranks]       ║
║                                                                ║
╠════════════════════════════════════════════════════════════════╣
║  [Inspect Lance] [Compare to Yours] [Leave]                   ║
╚════════════════════════════════════════════════════════════════╝
```

### Detailed Lance View

When player selects "Inspect Lance":

```
╔════════════════════════════════════════════════════════════════╗
║         Iron Hawks - Infantry Lance                            ║
╠════════════════════════════════════════════════════════════════╣
║                                                                ║
║  Lance Leader:  Galter (Tier 5 - Veteran Infantry)            ║
║  Sergeant:      Rolf (Tier 4 - Trained Infantry)              ║
║                                                                ║
║  Corporals (2):                                                ║
║    • Aedrin (Tier 3)                                           ║
║    • Cadoc (Tier 3)                                            ║
║                                                                ║
║  Lance Corporals (2):                                          ║
║    • Elwin (Tier 2)                                            ║
║    • Gwallog (Tier 2)                                          ║
║                                                                ║
║  Privates (5):                                                 ║
║    • Caradoc, Idris, Mabon, Owain, Tudur (Tier 1)            ║
║                                                                ║
║  Lance Status: Full Strength, High Morale                      ║
║  Battle Record: 3 engagements, 0 casualties                   ║
║                                                                ║
╠════════════════════════════════════════════════════════════════╣
║  [Back to Company View]                                        ║
╚════════════════════════════════════════════════════════════════╝
```

### Menu Integration Points

**Entry Points:**
1. **Enemy Encounter:** "Scout their forces" option
2. **Allied Lord:** "How fares your company?" conversation
3. **Camp Menu (Player):** "Intelligence Reports" → View known army compositions
4. **After Battle:** "Review enemy losses" shows their lance casualties

**Information Availability:**
- **Full Detail:** Allied lords, after victory
- **Partial Detail:** Neutral parties (lance count, general strength)
- **Limited Detail:** Enemies (estimated lance count only, unless scouted)

---

## Technical Implementation

### Core Behavior Class

```csharp
// src/Features/Lances/Behaviors/AILordLanceSimulationBehavior.cs

using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.SaveSystem;
using Enlisted.Mod.Core.Logging;
using Enlisted.Features.Lances.Models;
using JetBrains.Annotations;

namespace Enlisted.Features.Lances.Behaviors
{
    /// <summary>
    /// Simulates lance structure and composition for AI lord armies.
    /// Provides intelligence about enemy/allied army organization.
    /// </summary>
    /// <remarks>
    /// This behavior tracks lance composition based on actual troop counts,
    /// generating procedural lance names and members on-demand for performance.
    /// </remarks>
    [UsedImplicitly]
    public sealed class AILordLanceSimulationBehavior : CampaignBehaviorBase
    {
        #region Static Instance

        private static AILordLanceSimulationBehavior _instance;

        /// <summary>
        /// Gets the singleton instance of the AI Lord Lance Simulation behavior.
        /// </summary>
        [CanBeNull]
        public static AILordLanceSimulationBehavior Instance => _instance;

        #endregion

        #region Events

        /// <summary>
        /// Fired when a new lance is formed in an AI army.
        /// </summary>
        public static event Action<MobileParty, AILance> OnLanceFormed;

        /// <summary>
        /// Fired when a lance is dissolved (eliminated or consolidated).
        /// </summary>
        public static event Action<MobileParty, AILance> OnLanceDissolved;

        /// <summary>
        /// Fired when a lance takes casualties in battle.
        /// </summary>
        public static event Action<MobileParty, AILance, int> OnLanceCasualties;

        /// <summary>
        /// Fired when company status changes significantly.
        /// </summary>
        public static event Action<MobileParty, CompanyStatus> OnCompanyStatusChanged;

        /// <summary>
        /// Fired when a simulated lance member is killed.
        /// </summary>
        public static event Action<MobileParty, SimulatedLanceMember> OnMemberKilled;

        /// <summary>
        /// Fired when a simulated lance member is wounded.
        /// </summary>
        public static event Action<MobileParty, SimulatedLanceMember> OnMemberWounded;

        #endregion

        #region Fields

        /// <summary>
        /// Registry of AI companies, keyed by mobile party.
        /// </summary>
        [SaveableField(1)]
        private Dictionary<MobileParty, AILanceCompany> _companyRegistry;

        private readonly ILanceNameGenerator _nameGenerator;
        private readonly IAILanceConfig _config;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="AILordLanceSimulationBehavior"/> class.
        /// </summary>
        public AILordLanceSimulationBehavior()
            : this(null, null)
        {
        }

        /// <summary>
        /// Initializes a new instance with optional dependencies (for testing/extensibility).
        /// </summary>
        /// <param name="nameGenerator">Custom name generator (null uses default).</param>
        /// <param name="config">Custom configuration (null uses global config).</param>
        public AILordLanceSimulationBehavior(
            [CanBeNull] ILanceNameGenerator nameGenerator,
            [CanBeNull] IAILanceConfig config)
        {
            _instance = this;
            _companyRegistry = new Dictionary<MobileParty, AILanceCompany>();
            _nameGenerator = nameGenerator ?? new DefaultLanceNameGenerator();
            _config = config ?? AILanceConfigLoader.Load();
        }

        #endregion

        #region CampaignBehaviorBase Implementation

        /// <inheritdoc />
        public override void RegisterEvents()
        {
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
            CampaignEvents.MapEventEnded.AddNonSerializedListener(this, OnMapEventEnded);
            CampaignEvents.OnPartyDisbandedEvent.AddNonSerializedListener(this, OnPartyDisbanded);
            CampaignEvents.DailyTickPartyEvent.AddNonSerializedListener(this, OnDailyTick);
            CampaignEvents.OnTroopsRecruited.AddNonSerializedListener(this, OnTroopsRecruited);
        }

        /// <inheritdoc />
        public override void SyncData(IDataStore dataStore)
        {
            dataStore.SyncData("_aiLanceCompanyRegistry", ref _companyRegistry);
        }

        #endregion

        #region Event Handlers

        private void OnSessionLaunched(CampaignGameStarter starter)
        {
            try
            {
                LogManager.Log("[AI Lance] Initializing simulation for all lord parties...");

                var lordParties = MobileParty.All
                    .Where(p => p?.IsLordParty == true && p.LeaderHero != null)
                    .ToList();

                foreach (var party in lordParties)
                {
                    InitializeCompany(party);
                }

                LogManager.Log($"[AI Lance] Initialized {_companyRegistry.Count} lord armies.");
            }
            catch (Exception ex)
            {
                LogManager.LogError($"[AI Lance] Error during session launch: {ex.Message}", ex);
            }
        }

        private void OnMapEventEnded(MapEvent mapEvent)
        {
            if (mapEvent == null) return;

            try
            {
                foreach (var partyBase in mapEvent.InvolvedParties)
                {
                    var party = partyBase.MobileParty;
                    if (party == null || !_companyRegistry.ContainsKey(party))
                        continue;

                    var casualties = CalculateCasualties(party, mapEvent);
                    if (casualties > 0)
                    {
                        ApplyCasualties(party, casualties);
                    }
                }
            }
            catch (Exception ex)
            {
                LogManager.LogError($"[AI Lance] Error processing battle casualties: {ex.Message}", ex);
            }
        }

        private void OnPartyDisbanded(MobileParty party, Settlement settlement)
        {
            if (party == null) return;

            if (_companyRegistry.Remove(party))
            {
                LogManager.Log($"[AI Lance] Removed disbanded party: {party.Name}");
            }
        }

        private void OnDailyTick(MobileParty party)
        {
            if (party == null || !_companyRegistry.TryGetValue(party, out var company))
                return;

            try
            {
                // Update company status
                var previousStatus = company.Status;
                company.UpdateCompanyStatus();

                // Check if status changed significantly
                if (HasSignificantStatusChange(previousStatus, company.Status))
                {
                    OnCompanyStatusChanged?.Invoke(party, company.Status);
                }

                // Check for lance reorganization
                company.RecalculateLances();

                // Process wounded recovery
                company.ProcessWoundedRecovery();
            }
            catch (Exception ex)
            {
                LogManager.LogError($"[AI Lance] Error in daily tick for {party.Name}: {ex.Message}", ex);
            }
        }

        private void OnTroopsRecruited(Hero hero, Settlement settlement, Hero recruiterHero, 
            CharacterObject troop, int count)
        {
            if (hero?.PartyBelongedTo == null || !_companyRegistry.TryGetValue(hero.PartyBelongedTo, out var company))
                return;

            company.AddRecruits(count);
        }

        #endregion

        #region Public API

        /// <summary>
        /// Gets the simulated company for a given party.
        /// </summary>
        /// <param name="party">The mobile party to query.</param>
        /// <returns>The company, or null if not simulated.</returns>
        [CanBeNull]
        public AILanceCompany GetCompany([CanBeNull] MobileParty party)
        {
            if (party == null) return null;
            return _companyRegistry.TryGetValue(party, out var company) ? company : null;
        }

        /// <summary>
        /// Forces initialization of a company for a party (if not already tracked).
        /// </summary>
        /// <param name="party">The party to initialize.</param>
        /// <returns>True if initialized, false if already tracked or invalid.</returns>
        public bool InitializeCompany([NotNull] MobileParty party)
        {
            if (party == null) throw new ArgumentNullException(nameof(party));
            if (!party.IsLordParty || party.LeaderHero == null) return false;
            if (_companyRegistry.ContainsKey(party)) return false;

            try
            {
                var company = new AILanceCompany(
                    party, 
                    _config.TroopsPerLance,
                    _nameGenerator
                );

                _companyRegistry[party] = company;
                LogManager.Log($"[AI Lance] Initialized company for {party.Name}: {company.Lances.Count} lances");
                return true;
            }
            catch (Exception ex)
            {
                LogManager.LogError($"[AI Lance] Failed to initialize company for {party.Name}: {ex.Message}", ex);
                return false;
            }
        }

        #endregion

        #region Private Methods

        private void ApplyCasualties(MobileParty party, int casualties)
        {
            if (!_companyRegistry.TryGetValue(party, out var company))
                return;

            company.ApplyCasualties(casualties);

            // Fire events for lance casualties
            foreach (var lance in company.Lances)
            {
                var lanceCasualties = lance.Members.Count(m => m.State == MemberState.Dead);
                if (lanceCasualties > 0)
                {
                    OnLanceCasualties?.Invoke(party, lance, lanceCasualties);
                }
            }
        }

        private int CalculateCasualties(MobileParty party, MapEvent mapEvent)
        {
            // Calculate casualties based on party's involvement in map event
            var partyInvolvement = mapEvent.GetPartyInvolvement(party.Party);
            if (partyInvolvement == null) return 0;

            return Math.Max(0, partyInvolvement.NumRemovedFromBattleSim + partyInvolvement.KilledCount);
        }

        private bool HasSignificantStatusChange(CompanyStatus old, CompanyStatus current)
        {
            return Math.Abs(old.Morale - current.Morale) >= 10
                || Math.Abs(old.Cohesion - current.Cohesion) >= 10
                || Math.Abs(old.Readiness - current.Readiness) >= 10
                || (old.FoodDays >= 5 && current.FoodDays < 5);
        }

        #endregion
    }
}
```

### AILanceCompany Class

```csharp
// src/Features/Lances/Models/AILanceCompany.cs

using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.SaveSystem;
using JetBrains.Annotations;

namespace Enlisted.Features.Lances.Models
{
    /// <summary>
    /// Represents the simulated company structure for an AI lord's army.
    /// Manages lance composition, casualties, and overall company status.
    /// </summary>
    [SaveableClass(100)]
    public sealed class AILanceCompany
    {
        #region Properties

        /// <summary>
        /// Gets the mobile party this company represents.
        /// </summary>
        [SaveableProperty(1)]
        [NotNull]
        public MobileParty Party { get; private set; }

        /// <summary>
        /// Gets the list of lances in this company.
        /// </summary>
        [SaveableProperty(2)]
        [NotNull]
        public List<AILance> Lances { get; private set; }

        /// <summary>
        /// Gets the current company status (morale, food, cohesion, readiness).
        /// </summary>
        [SaveableProperty(3)]
        [NotNull]
        public CompanyStatus Status { get; private set; }

        #endregion

        #region Fields

        [SaveableField(4)]
        private readonly int _troopsPerLance;

        [SaveableField(5)]
        private readonly CultureObject _culture;

        [SaveableField(6)]
        private int _lanceIdCounter;

        private readonly ILanceNameGenerator _nameGenerator;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="AILanceCompany"/> class.
        /// </summary>
        /// <param name="party">The mobile party this company represents.</param>
        /// <param name="troopsPerLance">Number of troops per lance (default 10).</param>
        /// <param name="nameGenerator">Lance name generator (optional).</param>
        /// <exception cref="ArgumentNullException">Thrown if party is null.</exception>
        /// <exception cref="ArgumentException">Thrown if party has no leader or culture.</exception>
        public AILanceCompany(
            [NotNull] MobileParty party, 
            int troopsPerLance = 10,
            [CanBeNull] ILanceNameGenerator nameGenerator = null)
        {
            Party = party ?? throw new ArgumentNullException(nameof(party));
            
            if (party.LeaderHero == null)
                throw new ArgumentException("Party must have a leader hero.", nameof(party));
            
            if (party.LeaderHero.Culture == null)
                throw new ArgumentException("Party leader must have a culture.", nameof(party));

            _troopsPerLance = Math.Clamp(troopsPerLance, 8, 12);
            _culture = party.LeaderHero.Culture;
            _nameGenerator = nameGenerator ?? new DefaultLanceNameGenerator();
            _lanceIdCounter = 0;

            Lances = new List<AILance>();
            Status = new CompanyStatus();

            GenerateInitialLances();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Applies casualties to the company, distributing them across lances.
        /// Uses rank-weighted selection (officers more likely to survive).
        /// </summary>
        /// <param name="totalCasualties">Number of casualties to apply.</param>
        public void ApplyCasualties(int totalCasualties)
        {
            if (totalCasualties <= 0) return;

            var allMembers = Lances
                .SelectMany(l => l.Members)
                .Where(m => m.State == MemberState.Healthy)
                .ToList();

            if (allMembers.Count == 0) return;

            var casualties = Math.Min(totalCasualties, allMembers.Count);

            for (var i = 0; i < casualties; i++)
            {
                var casualty = SelectWeightedCasualty(allMembers);
                if (casualty == null) break;

                // 70% killed, 30% wounded
                casualty.State = MBRandom.RandomFloat < 0.7f
                    ? MemberState.Dead
                    : MemberState.Wounded;

                allMembers.Remove(casualty);
            }

            RemoveDeadMembers();
            ReorganizeLances();
            UpdateCompanyStatus();
        }

        /// <summary>
        /// Recalculates lance count based on current troop numbers.
        /// Creates new lances or consolidates existing ones as needed.
        /// </summary>
        public void RecalculateLances()
        {
            var currentTroops = Party.MemberRoster.TotalManCount;
            var targetLanceCount = currentTroops / _troopsPerLance;
            var currentLanceCount = Lances.Count;

            if (targetLanceCount > currentLanceCount)
            {
                // Generate new lances
                var toAdd = targetLanceCount - currentLanceCount;
                for (var i = 0; i < toAdd; i++)
                {
                    CreateNewLance();
                }
            }
            else if (targetLanceCount < currentLanceCount)
            {
                // Consolidate lances
                ConsolidateLances(targetLanceCount);
            }
        }

        /// <summary>
        /// Adds new recruits to the company, distributing them across lances.
        /// </summary>
        /// <param name="recruitCount">Number of recruits added.</param>
        public void AddRecruits(int recruitCount)
        {
            if (recruitCount <= 0) return;

            RecalculateLances();
            DistributeRecruitsToLances(recruitCount);
            UpdateCompanyStatus();
        }

        /// <summary>
        /// Updates the company status (morale, food, cohesion, readiness).
        /// Should be called after any significant changes.
        /// </summary>
        public void UpdateCompanyStatus()
        {
            Status.Morale = CalculateMorale();
            Status.FoodDays = CalculateFoodDays();
            Status.Cohesion = CalculateCohesion();
            Status.Readiness = CalculateReadiness();
        }

        /// <summary>
        /// Processes daily recovery for wounded members.
        /// </summary>
        public void ProcessWoundedRecovery()
        {
            foreach (var lance in Lances)
            {
                foreach (var member in lance.Members.Where(m => m.State == MemberState.Wounded))
                {
                    // 10% chance per day to recover
                    if (MBRandom.RandomFloat < 0.10f)
                    {
                        member.State = MemberState.Healthy;
                    }
                }
            }
        }

        /// <summary>
        /// Gets total deaths in the last battle.
        /// </summary>
        public int GetTotalDeaths()
        {
            return Lances.SelectMany(l => l.Members)
                .Count(m => m.State == MemberState.Dead);
        }

        /// <summary>
        /// Gets total wounded in the last battle.
        /// </summary>
        public int GetTotalWounded()
        {
            return Lances.SelectMany(l => l.Members)
                .Count(m => m.State == MemberState.Wounded);
        }

        /// <summary>
        /// Checks if a full lance was lost in the last battle.
        /// </summary>
        public bool WasLanceLostInLastBattle()
        {
            return Lances.Any(l => l.Members.All(m => m.State == MemberState.Dead));
        }

        #endregion

        #region Private Methods

        private void GenerateInitialLances()
        {
            var lanceCount = Party.MemberRoster.TotalManCount / _troopsPerLance;

            for (var i = 0; i < lanceCount; i++)
            {
                CreateNewLance();
            }
        }

        private void CreateNewLance()
        {
            var lanceId = $"{Party.Id}_{_lanceIdCounter++}";
            var lanceName = _nameGenerator.GenerateLanceName(_culture, Party.LeaderHero);

            var lance = new AILance(
                lanceId,
                lanceName,
                _culture,
                _troopsPerLance,
                Party
            );

            Lances.Add(lance);
        }

        [CanBeNull]
        private SimulatedLanceMember SelectWeightedCasualty([NotNull] List<SimulatedLanceMember> members)
        {
            if (members == null || members.Count == 0) return null;

            // Lower survival weight = higher chance of becoming casualty
            var totalInverseWeight = members.Sum(m => 1f / m.SurvivalWeight);
            var roll = MBRandom.RandomFloat * totalInverseWeight;

            var cumulative = 0f;
            foreach (var member in members)
            {
                cumulative += 1f / member.SurvivalWeight;
                if (roll <= cumulative)
                    return member;
            }

            return members.LastOrDefault();
        }

        private void RemoveDeadMembers()
        {
            foreach (var lance in Lances)
            {
                lance.Members.RemoveAll(m => m.State == MemberState.Dead);
            }
        }

        private void ReorganizeLances()
        {
            // Remove empty lances
            Lances.RemoveAll(l => l.Members.Count == 0);

            // Consolidate lances below minimum size
            var depletedLances = Lances.Where(l => l.Members.Count < 5).ToList();
            
            if (depletedLances.Count > 1)
            {
                ConsolidateDepletedLances(depletedLances);
            }
        }

        private void ConsolidateLances(int targetCount)
        {
            while (Lances.Count > targetCount)
            {
                // Find smallest lance
                var smallest = Lances.OrderBy(l => l.Members.Count).First();
                
                // Redistribute members to other lances
                if (Lances.Count > 1)
                {
                    var targetLance = Lances.First(l => l != smallest);
                    targetLance.Members.AddRange(smallest.Members);
                }

                Lances.Remove(smallest);
            }
        }

        private void ConsolidateDepletedLances(List<AILance> depletedLances)
        {
            // Merge depleted lances together
            if (depletedLances.Count < 2) return;

            var primary = depletedLances[0];
            for (var i = 1; i < depletedLances.Count; i++)
            {
                primary.Members.AddRange(depletedLances[i].Members);
                Lances.Remove(depletedLances[i]);
            }
        }

        private void DistributeRecruitsToLances(int recruitCount)
        {
            // Distribute recruits evenly across lances
            var remaining = recruitCount;
            foreach (var lance in Lances.Where(l => l.Members.Count < _troopsPerLance))
            {
                var needed = _troopsPerLance - lance.Members.Count;
                var toAdd = Math.Min(needed, remaining);
                
                for (var i = 0; i < toAdd; i++)
                {
                    lance.AddMember(CreateRecruitMember(lance));
                }

                remaining -= toAdd;
                if (remaining <= 0) break;
            }
        }

        private SimulatedLanceMember CreateRecruitMember(AILance lance)
        {
            return new SimulatedLanceMember
            {
                Name = _nameGenerator.GenerateMemberName(_culture),
                Rank = "Private",
                Tier = 1,
                Formation = FormationClass.Infantry,
                State = MemberState.Healthy,
                SurvivalWeight = 0.50f,
                LanceId = lance.Id
            };
        }

        private int CalculateMorale()
        {
            var baseMorale = (int)Party.Morale;
            
            // Penalties
            if (Party.Food < Party.FoodChange)
                baseMorale -= 10;
            
            if (WasLanceLostInLastBattle())
                baseMorale -= 5;

            return Math.Clamp(baseMorale, 0, 100);
        }

        private float CalculateFoodDays()
        {
            return Party.Food / Math.Max(1f, -Party.FoodChange);
        }

        private int CalculateCohesion()
        {
            var baseCohesion = (int)((Party.Party.TotalStrength / (float)Party.Party.PartySizeLimit) * 100);
            
            // Penalties
            var depletedLances = Lances.Count(l => l.Members.Count < _troopsPerLance * 0.6f);
            baseCohesion -= depletedLances * 5;

            return Math.Clamp(baseCohesion, 0, 100);
        }

        private int CalculateReadiness()
        {
            var morale = Status.Morale;
            var cohesion = CalculateCohesion();
            var foodSecurity = Math.Min(CalculateFoodDays() / 10f, 1f) * 100;
            var equipmentQuality = (Party.Party.AverageWoundedLevel / 6f) * 100;

            var readiness = (morale * 0.4f) + (cohesion * 0.3f) + (foodSecurity * 0.2f) + (equipmentQuality * 0.1f);
            return (int)Math.Clamp(readiness, 0, 100);
        }

        #endregion
    }
}
```

### AILance Class

```csharp
// src/Features/Lances/Models/AILance.cs

using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.SaveSystem;
using JetBrains.Annotations;

namespace Enlisted.Features.Lances.Models
{
    /// <summary>
    /// Represents a single lance within an AI lord's company.
    /// Contains simulated members with ranks, tiers, and states.
    /// </summary>
    [SaveableClass(101)]
    public sealed class AILance
    {
        #region Properties

        /// <summary>
        /// Gets the unique identifier for this lance.
        /// </summary>
        [SaveableProperty(1)]
        [NotNull]
        public string Id { get; private set; }

        /// <summary>
        /// Gets the name of this lance (e.g., "Iron Hawks", "Storm Blades").
        /// </summary>
        [SaveableProperty(2)]
        [NotNull]
        public string Name { get; private set; }

        /// <summary>
        /// Gets the culture of this lance (determines names and aesthetics).
        /// </summary>
        [SaveableProperty(3)]
        [NotNull]
        public CultureObject Culture { get; private set; }

        /// <summary>
        /// Gets the list of simulated lance members.
        /// </summary>
        [SaveableProperty(4)]
        [NotNull]
        public List<SimulatedLanceMember> Members { get; private set; }

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="AILance"/> class.
        /// </summary>
        /// <param name="id">Unique lance identifier.</param>
        /// <param name="name">Lance name.</param>
        /// <param name="culture">Lance culture.</param>
        /// <param name="targetSize">Target number of members.</param>
        /// <param name="party">Parent mobile party (for tier distribution).</param>
        /// <exception cref="ArgumentNullException">Thrown if any required parameter is null.</exception>
        public AILance(
            [NotNull] string id,
            [NotNull] string name,
            [NotNull] CultureObject culture,
            int targetSize,
            [NotNull] MobileParty party)
        {
            Id = id ?? throw new ArgumentNullException(nameof(id));
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Culture = culture ?? throw new ArgumentNullException(nameof(culture));
            
            if (party == null)
                throw new ArgumentNullException(nameof(party));

            Members = new List<SimulatedLanceMember>();
            GenerateMembers(targetSize, party);
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Adds a new member to this lance.
        /// </summary>
        /// <param name="member">The member to add.</param>
        public void AddMember([NotNull] SimulatedLanceMember member)
        {
            if (member == null) throw new ArgumentNullException(nameof(member));
            
            member.LanceId = Id;
            Members.Add(member);
        }

        /// <summary>
        /// Gets the lance leader (highest ranking member).
        /// </summary>
        [CanBeNull]
        public SimulatedLanceMember GetLanceLeader()
        {
            return Members
                .OrderByDescending(m => m.Tier)
                .ThenBy(m => m.State)
                .FirstOrDefault();
        }

        /// <summary>
        /// Gets all healthy members (available for duty).
        /// </summary>
        [NotNull]
        public IEnumerable<SimulatedLanceMember> GetHealthyMembers()
        {
            return Members.Where(m => m.State == MemberState.Healthy);
        }

        /// <summary>
        /// Checks if this lance is at full strength.
        /// </summary>
        public bool IsFullStrength(int targetSize)
        {
            return Members.Count >= targetSize && Members.All(m => m.State == MemberState.Healthy);
        }

        #endregion

        #region Private Methods

        private void GenerateMembers(int count, MobileParty party)
        {
            var tierDistribution = GetTierDistribution(party);

            // Generate 1 leader (highest tier)
            Members.Add(CreateMember(
                tierDistribution.LeaderTier,
                GetRankForTier(tierDistribution.LeaderTier),
                GetFormationForParty(party)
            ));

            // Generate 1 sergeant
            if (count > 3)
            {
                Members.Add(CreateMember(4, "Sergeant", GetFormationForParty(party)));
            }

            // Generate corporals
            var corporalCount = Math.Max(1, count / 4);
            for (var i = 0; i < corporalCount && Members.Count < count; i++)
            {
                Members.Add(CreateMember(3, "Corporal", GetFormationForParty(party)));
            }

            // Fill remainder with lower ranks
            while (Members.Count < count)
            {
                var tier = tierDistribution.GetRandomTier();
                var rank = GetRankForTier(tier);
                Members.Add(CreateMember(tier, rank, GetFormationForParty(party)));
            }
        }

        private SimulatedLanceMember CreateMember(int tier, string rank, FormationClass formation)
        {
            return new SimulatedLanceMember
            {
                Name = GenerateMemberName(),
                Rank = rank,
                Tier = Math.Clamp(tier, 1, 6),
                Formation = formation,
                State = MemberState.Healthy,
                SurvivalWeight = GetSurvivalWeightForRank(rank),
                LanceId = Id
            };
        }

        private string GenerateMemberName()
        {
            // Fallback to generic names (can be extended with Bannerlord's name generator)
            var genericNames = new[]
            {
                "Aldric", "Bors", "Cadoc", "Drest", "Elwin", "Finn", "Gareth",
                "Hector", "Idris", "Jorah", "Kael", "Leoric", "Mabon", "Nero"
            };

            return genericNames[MBRandom.RandomInt(genericNames.Length)];
        }

        private static string GetRankForTier(int tier)
        {
            return tier switch
            {
                >= 5 => "Lance Leader",
                4 => "Sergeant",
                3 => "Corporal",
                2 => "Lance Corporal",
                _ => "Private"
            };
        }

        private static float GetSurvivalWeightForRank(string rank)
        {
            return rank switch
            {
                "Lance Leader" => 0.90f,
                "Sergeant" => 0.80f,
                "Corporal" => 0.70f,
                "Lance Corporal" => 0.60f,
                _ => 0.50f
            };
        }

        private static TierDistribution GetTierDistribution(MobileParty party)
        {
            var roster = party.MemberRoster;
            var tierCounts = new Dictionary<int, int>();

            for (var i = 1; i <= 6; i++)
            {
                tierCounts[i] = roster.Sum(element => element.Character?.Tier == i ? element.Number : 0);
            }

            var leaderTier = tierCounts
                .OrderByDescending(kvp => kvp.Value)
                .ThenByDescending(kvp => kvp.Key)
                .First()
                .Key;

            return new TierDistribution
            {
                LeaderTier = leaderTier,
                TierWeights = tierCounts
            };
        }

        private static FormationClass GetFormationForParty(MobileParty party)
        {
            // Determine primary formation based on party composition
            var roster = party.MemberRoster;
            var infantryCount = roster.Sum(e => e.Character?.IsInfantry == true ? e.Number : 0);
            var cavalryCount = roster.Sum(e => e.Character?.IsMounted == true ? e.Number : 0);
            var archerCount = roster.Sum(e => e.Character?.IsArcher == true ? e.Number : 0);

            if (cavalryCount > infantryCount && cavalryCount > archerCount)
                return FormationClass.Cavalry;
            
            if (archerCount > infantryCount)
                return FormationClass.Ranged;

            return FormationClass.Infantry;
        }

        #endregion

        #region Nested Types

        private struct TierDistribution
        {
            public int LeaderTier { get; set; }
            public Dictionary<int, int> TierWeights { get; set; }

            public int GetRandomTier()
            {
                var totalWeight = TierWeights.Values.Sum();
                if (totalWeight == 0) return 1;

                var roll = MBRandom.RandomInt(totalWeight);
                var cumulative = 0;

                foreach (var kvp in TierWeights.OrderBy(k => k.Key))
                {
                    cumulative += kvp.Value;
                    if (roll < cumulative)
                        return kvp.Key;
                }

                return 1;
            }
        }

        #endregion
    }
}
```

### Extensibility Interfaces (Mod-Friendly)

```csharp
// src/Features/Lances/Interfaces/ILanceNameGenerator.cs

using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using JetBrains.Annotations;

namespace Enlisted.Features.Lances.Interfaces
{
    /// <summary>
    /// Interface for generating lance and member names.
    /// Implement this interface to provide custom name generation for specific cultures or factions.
    /// </summary>
    public interface ILanceNameGenerator
    {
        /// <summary>
        /// Generates a lance name based on culture and lord.
        /// </summary>
        /// <param name="culture">The culture of the lance.</param>
        /// <param name="lord">The lord commanding this army (optional).</param>
        /// <returns>A generated lance name.</returns>
        [NotNull]
        string GenerateLanceName([NotNull] CultureObject culture, [CanBeNull] Hero lord);

        /// <summary>
        /// Generates a member name based on culture.
        /// </summary>
        /// <param name="culture">The culture of the member.</param>
        /// <returns>A generated member name.</returns>
        [NotNull]
        string GenerateMemberName([NotNull] CultureObject culture);
    }
}
```

```csharp
// src/Features/Lances/Interfaces/IAILanceConfig.cs

using JetBrains.Annotations;

namespace Enlisted.Features.Lances.Interfaces
{
    /// <summary>
    /// Configuration interface for AI Lance Simulation.
    /// Implement this interface to provide custom configuration.
    /// </summary>
    public interface IAILanceConfig
    {
        /// <summary>
        /// Gets the number of troops per lance.
        /// </summary>
        int TroopsPerLance { get; }

        /// <summary>
        /// Gets whether AI lance simulation is enabled.
        /// </summary>
        bool IsEnabled { get; }

        /// <summary>
        /// Gets the officer survival rate (0.0 - 1.0).
        /// </summary>
        float OfficerSurvivalRate { get; }

        /// <summary>
        /// Gets the private survival rate (0.0 - 1.0).
        /// </summary>
        float PrivateSurvivalRate { get; }

        /// <summary>
        /// Gets whether enemy lance inspector is enabled.
        /// </summary>
        bool ShowEnemyLanceInspector { get; }
    }
}
```

```csharp
// src/Features/Lances/Services/DefaultLanceNameGenerator.cs

using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;
using Enlisted.Features.Lances.Interfaces;
using JetBrains.Annotations;

namespace Enlisted.Features.Lances.Services
{
    /// <summary>
    /// Default implementation of <see cref="ILanceNameGenerator"/>.
    /// Uses culture-specific name components from JSON config.
    /// </summary>
    public sealed class DefaultLanceNameGenerator : ILanceNameGenerator
    {
        private readonly LanceNameConfig _config;

        public DefaultLanceNameGenerator()
        {
            _config = LanceNameConfigLoader.Load();
        }

        /// <inheritdoc />
        public string GenerateLanceName(CultureObject culture, Hero lord)
        {
            if (culture == null)
                return "Unknown Lance";

            var format = _config.Formats[MBRandom.RandomInt(_config.Formats.Length)];
            var adjective = GetCultureAdjective(culture);
            var unitType = GetCultureUnitType(culture);

            var name = format
                .Replace("{adjective}", adjective)
                .Replace("{unit_type}", unitType)
                .Replace("{lord_name}", lord?.Name.ToString() ?? "Commander");

            return name;
        }

        /// <inheritdoc />
        public string GenerateMemberName(CultureObject culture)
        {
            if (culture == null)
                return "Soldier";

            // Use Bannerlord's built-in name generation if available
            // Fallback to generic names from config
            return _config.GenericNames[MBRandom.RandomInt(_config.GenericNames.Length)];
        }

        private string GetCultureAdjective(CultureObject culture)
        {
            // Culture-specific adjectives can be loaded from JSON
            return _config.Adjectives[MBRandom.RandomInt(_config.Adjectives.Length)];
        }

        private string GetCultureUnitType(CultureObject culture)
        {
            // Culture-specific unit types can be loaded from JSON
            return _config.UnitTypes[MBRandom.RandomInt(_config.UnitTypes.Length)];
        }
    }
}
```

```csharp
// src/Features/Lances/Services/AILanceConfigLoader.cs

using System;
using System.IO;
using Newtonsoft.Json;
using TaleWorlds.Engine;
using Enlisted.Features.Lances.Interfaces;
using Enlisted.Mod.Core.Logging;
using JetBrains.Annotations;

namespace Enlisted.Features.Lances.Services
{
    /// <summary>
    /// Loads AI Lance configuration from JSON files.
    /// </summary>
    public static class AILanceConfigLoader
    {
        private const string ConfigFileName = "lance_simulation_config.json";

        /// <summary>
        /// Loads AI lance configuration from ModuleData.
        /// </summary>
        /// <returns>The loaded configuration, or default if load fails.</returns>
        [NotNull]
        public static IAILanceConfig Load()
        {
            try
            {
                var configPath = Path.Combine(
                    Utilities.GetBasePath(),
                    "Modules",
                    "Enlisted",
                    "ModuleData",
                    "Enlisted",
                    ConfigFileName
                );

                if (!File.Exists(configPath))
                {
                    LogManager.LogWarning($"[AI Lance] Config not found at {configPath}, using defaults.");
                    return new DefaultAILanceConfig();
                }

                var json = File.ReadAllText(configPath);
                var config = JsonConvert.DeserializeObject<AILanceConfigData>(json);

                return new LoadedAILanceConfig(config);
            }
            catch (Exception ex)
            {
                LogManager.LogError($"[AI Lance] Failed to load config: {ex.Message}", ex);
                return new DefaultAILanceConfig();
            }
        }
    }

    /// <summary>
    /// Default configuration values.
    /// </summary>
    internal sealed class DefaultAILanceConfig : IAILanceConfig
    {
        public int TroopsPerLance => 10;
        public bool IsEnabled => true;
        public float OfficerSurvivalRate => 0.85f;
        public float PrivateSurvivalRate => 0.50f;
        public bool ShowEnemyLanceInspector => true;
    }

    /// <summary>
    /// Configuration loaded from JSON.
    /// </summary>
    internal sealed class LoadedAILanceConfig : IAILanceConfig
    {
        private readonly AILanceConfigData _data;

        public LoadedAILanceConfig([NotNull] AILanceConfigData data)
        {
            _data = data ?? throw new ArgumentNullException(nameof(data));
        }

        public int TroopsPerLance => _data.LanceComposition?.DefaultSize ?? 10;
        public bool IsEnabled => _data.Enabled;
        public float OfficerSurvivalRate => _data.RankSurvivalWeights?.LanceLeader ?? 0.85f;
        public float PrivateSurvivalRate => _data.RankSurvivalWeights?.Private ?? 0.50f;
        public bool ShowEnemyLanceInspector => _data.ShowEnemyInspector;
    }
}
```

### Menu Implementation

```csharp
// src/Features/Lances/Menus/AILanceInspectorMenu.cs

using System;
using System.Text;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using Enlisted.Features.Lances.Behaviors;
using Enlisted.Mod.Core.Logging;
using JetBrains.Annotations;

namespace Enlisted.Features.Lances.Menus
{
    /// <summary>
    /// Provides menus for inspecting AI lord army composition.
    /// </summary>
    [UsedImplicitly]
    public static class AILanceInspectorMenu
    {
        /// <summary>
        /// Registers AI lance inspector menus.
        /// </summary>
        /// <param name="starter">Campaign game starter.</param>
        public static void AddMenus([NotNull] CampaignGameStarter starter)
    {
        // Entry point: encounter with lord party
        starter.AddGameMenuOption(
            "army_encounter",
            "inspect_enemy_army",
            "Scout their forces",
            args => CanInspectArmy(args),
            args => OpenArmyInspector(args),
            false,
            3
        );
        
        // Main army inspector menu
        starter.AddGameMenu(
            "ai_army_inspector",
            "{AI_ARMY_INSPECTOR_TEXT}",
            OnArmyInspectorInit,
            GameOverlays.MenuOverlayType.None
        );
        
        // Options: view lances, compare, leave
        starter.AddGameMenuOption(
            "ai_army_inspector",
            "view_lance_list",
            "View Lances",
            args => true,
            args => OpenLanceList(args),
            false,
            1
        );
        
        starter.AddGameMenuOption(
            "ai_army_inspector",
            "compare_armies",
            "Compare to Your Company",
            args => HasPlayerLance(),
            args => OpenArmyComparison(args),
            false,
            2
        );
        
        starter.AddGameMenuOption(
            "ai_army_inspector",
            "leave_inspector",
            "Leave",
            args => true,
            args => GameMenu.SwitchToMenu("army_encounter"),
            true,
            3
        );
    }
    
    private static void OnArmyInspectorInit(MenuCallbackArgs args)
    {
        var targetParty = GetTargetParty();
        var company = GetLanceSimulation().GetCompany(targetParty);
        
        if (company == null)
        {
            args.MenuContext.SetBackgroundMeshName("wait_urban");
            MBTextManager.SetTextVariable("AI_ARMY_INSPECTOR_TEXT", "No intelligence available.");
            return;
        }
        
        // Build status display text
        var status = company.Status;
        var text = new StringBuilder();
        
        text.AppendLine($"{targetParty.LeaderHero.Name}'s Company - {targetParty.LeaderHero.Clan.Kingdom.Name}");
        text.AppendLine();
        text.AppendLine("Company Status:");
        text.AppendLine($"  Morale: {GetMoraleBar(status.Morale)} {GetMoraleText(status.Morale)} ({status.Morale}/100)");
        text.AppendLine($"  Food: {GetFoodBar(status.FoodDays)} {status.FoodDays} Days Remaining");
        text.AppendLine($"  Cohesion: {GetCohesionBar(status.Cohesion)} {GetCohesionText(status.Cohesion)} ({status.Cohesion}/100)");
        text.AppendLine($"  Readiness: {GetReadinessBar(status.Readiness)} {GetReadinessText(status.Readiness)} ({status.Readiness}/100)");
        text.AppendLine();
        text.AppendLine($"Total Troops: {targetParty.MemberRoster.TotalManCount}    Lances: {company.Lances.Count}    Avg. Lance Size: {company.Lances.Average(l => l.Members.Count):F1}");
        
        MBTextManager.SetTextVariable("AI_ARMY_INSPECTOR_TEXT", text.ToString());
        args.MenuContext.SetBackgroundMeshName("wait_army");
    }
    
    private static void OpenLanceList(MenuCallbackArgs args)
    {
        // Open detailed lance list menu
        GameMenu.SwitchToMenu("ai_lance_list");
    }
}
```

---

## Integration Points

### With Lance Life Simulation

The AI Lord Lance Simulation is a **simplified parallel** to the player's Lance Life Simulation:

| Feature | Player's Lance | AI Lord Lances |
|---------|---------------|----------------|
| **Individual Personalities** | ✓ Full system | ✗ Not simulated |
| **Daily Schedules** | ✓ AI Camp Schedule | ✗ Not simulated |
| **Member Names** | ✓ Hand-crafted | ✓ Procedural |
| **Casualty Tracking** | ✓ Full detail | ✓ Rank-weighted |
| **Lance Structure** | ✓ 8-12 members | ✓ 8-12 members |
| **Rank Hierarchy** | ✓ Identical | ✓ Identical |

**Shared Code:**
- Lance size calculations
- Rank determination logic
- Survival weight formulas
- Menu display components (can be reused)

### With Bannerlord Party System

```csharp
// Hook into Bannerlord events
public override void RegisterEvents()
{
    // Detect recruitment
    CampaignEvents.OnTroopRecruitedEvent.AddNonSerializedListener(
        this, 
        OnTroopRecruited
    );
    
    // Detect casualties
    CampaignEvents.MapEventEnded.AddNonSerializedListener(
        this,
        OnBattleEnded
    );
    
    // Detect party disbanding
    CampaignEvents.OnPartyDisbandedEvent.AddNonSerializedListener(
        this,
        OnPartyDisbanded
    );
}
```

### With Intelligence/Scouting System

**Future Enhancement:** Tie information detail to scouting:

```csharp
public enum IntelligenceLevel
{
    None,           // Just troop count
    Basic,          // Lance count estimate
    Detailed,       // Lance names, rough composition
    Complete        // Full roster, exact status
}

public IntelligenceLevel GetIntelligenceLevel(MobileParty party)
{
    // Factors:
    // - Relationship with party (allied = complete)
    // - Recent scouting actions
    // - After defeating them in battle
    // - Spy network level
    
    if (FactionManager.IsAlliedWithFaction(Hero.MainHero.MapFaction, party.MapFaction))
        return IntelligenceLevel.Complete;
    
    if (HasRecentScoutReport(party))
        return IntelligenceLevel.Detailed;
    
    if (HasDefeatedInBattle(party))
        return IntelligenceLevel.Basic;
    
    return IntelligenceLevel.None;
}
```

---

## Event System Integration

The AI Lord Lance Simulation is fully integrated with the Enlisted event system, allowing content creators to build events that react to AI lance activities, and allowing the system to generate events based on AI lance state changes.

### Observable State & Trigger Tokens

The simulation exposes state that can be used in event triggers:

#### Campaign-Wide Triggers

```json
{
  "triggers": {
    "all": ["ai_lance_battle_occurred"],
    "any": []
  }
}
```

**Available Trigger Tokens:**

| Token | Description | When Active |
|-------|-------------|-------------|
| `ai_lance_battle_occurred` | Any AI lord army just finished a battle | After `MapEventEnded` |
| `ai_lance_heavy_casualties` | AI army lost 30%+ troops in battle | After major defeat |
| `ai_lance_wiped_out` | Entire AI lance eliminated | When lance dissolved |
| `ai_lance_new_formed` | New AI lance created | After recruitment surge |
| `watching_allied_army` | Player in allied lord's party | When player is attached |
| `enemy_army_scouted` | Player successfully scouted enemy | After scout action |

#### Party-Specific Conditions

These can be checked via conditional logic in event requirements:

```csharp
// Example: Event only fires if player's lord has low morale lances
public bool CheckCondition_LordLanceMoralelow()
{
    var lordParty = Hero.MainHero.PartyBelongedTo?.Army?.LeaderParty;
    if (lordParty == null) return false;
    
    var company = AILordLanceSimulationBehavior.Instance?.GetCompany(lordParty);
    if (company == null) return false;
    
    return company.Status.Morale < 40;
}
```

**Available State Queries:**

```csharp
// Get company for any lord party
AILanceCompany company = AILordLanceSimulationBehavior.Instance.GetCompany(party);

// Query company status
bool isDemoralized = company.Status.Morale < 40;
bool isStarving = company.Status.FoodDays < 3;
bool isFragmented = company.Status.Cohesion < 50;
bool isCombatIneffective = company.Status.Readiness < 40;

// Query lance composition
int totalLances = company.Lances.Count;
int depletedLances = company.Lances.Count(l => l.Members.Count < 6);
int fullStrengthLances = company.Lances.Count(l => l.Members.Count >= 10);

// Query casualties
int totalDeaths = company.GetTotalDeaths(); // Since last battle
int totalWounded = company.GetTotalWounded();
bool lanceLost = company.WasLanceLostInLastBattle();
```

### Event Generation from AI Lance Activities

The simulation automatically generates events that integrate with the existing event delivery system.

#### Event Categories

```json
{
  "category": "ai_lance_intel"
}
```

**Event Categories:**

| Category | Purpose | Delivery Channel |
|----------|---------|------------------|
| `ai_lance_intel` | Intelligence reports about enemy lances | `inquiry`, `menu` |
| `ai_lance_allied` | Updates about allied army status | `incident`, `menu` |
| `ai_lance_battle` | Battle aftermath for AI armies | `incident` |
| `ai_lance_morale` | Morale events in allied armies | `inquiry` |

#### Event Schema for AI Lance Events

**File Location:** `ModuleData/Enlisted/Events/events_ai_lance.json`

**Example Event - Enemy Lance Eliminated:**

```json
{
  "schemaVersion": 1,
  "packId": "ai_lance_events",
  "category": "ai_lance_intel",
  "events": [
    {
      "id": "enemy_lance_eliminated",
      "category": "ai_lance_intel",
      "metadata": {
        "tier_range": { "min": 3, "max": 9 },
        "content_doc": "docs/Features/Core/ai-lord-lance-simulation.md"
      },
      "delivery": {
        "method": "automatic",
        "channel": "incident",
        "incident_trigger": "MapEventEnded"
      },
      "triggers": {
        "all": [
          "is_enlisted",
          "ai_safe",
          "enemy_army_scouted"
        ],
        "any": ["ai_lance_wiped_out"],
        "time_of_day": ["any"]
      },
      "requirements": {
        "duty": "any",
        "formation": "any",
        "tier": { "min": 3, "max": 9 }
      },
      "timing": {
        "cooldown_days": 1,
        "priority": "medium",
        "one_time": false
      },
      "content": {
        "titleId": "ll_evt_enemy_lance_eliminated_title",
        "setupId": "ll_evt_enemy_lance_eliminated_setup",
        "title": "Enemy Lance Destroyed",
        "setup": "Your scouts return with news: {ENEMY_LORD}'s {LANCE_NAME} was annihilated in the recent battle. All hands lost. That's one less lance to worry about.",
        "options": [
          {
            "id": "acknowledge",
            "text": "Good. Keep tracking their strength.",
            "tooltip": "Note the intelligence for future engagements",
            "risk": "safe",
            "rewards": {
              "xp": { "Tactics": 20 }
            },
            "outcome": "You file away the intelligence. {ENEMY_LORD} just lost a full lance - that's a significant blow to their combat effectiveness."
          }
        ]
      }
    }
  ]
}
```

**Example Event - Allied Army Low Morale:**

```json
{
  "id": "allied_army_morale_crisis",
  "category": "ai_lance_allied",
  "delivery": {
    "method": "automatic",
    "channel": "inquiry",
    "incident_trigger": null
  },
  "triggers": {
    "all": [
      "is_enlisted",
      "ai_safe",
      "watching_allied_army"
    ],
    "any": [],
    "custom_conditions": ["allied_lord_morale_low"]
  },
  "timing": {
    "cooldown_days": 7,
    "priority": "high",
    "one_time": false
  },
  "content": {
    "titleId": "ll_evt_allied_morale_crisis_title",
    "title": "Trouble in the Ranks",
    "setupId": "ll_evt_allied_morale_crisis_setup",
    "setup": "{LORD_NAME}'s army is showing signs of strain. You've seen the hollow looks, heard the grumbling. Word is several lances are at breaking point - low on food, high on fear.\n\nTheir morale could crack at the worst possible moment.",
    "options": [
      {
        "id": "offer_help",
        "text": "Offer to help shore up morale",
        "tooltip": "Leadership check - try to boost their spirits",
        "condition": "player_leadership >= 75",
        "risk": "risky",
        "risk_chance": 30,
        "costs": { "fatigue": 15, "time_hours": 4 },
        "rewards": { "xp": { "Leadership": 40 } },
        "effects_success": {
          "lance_reputation": 5,
          "relation": { "{LORD_NAME}": 2 }
        },
        "outcome": "You spend time with the struggling lances, sharing encouragement and practical advice. It helps. {LORD_NAME} notices your effort.",
        "outcome_failure": "Your words ring hollow to soldiers who've seen too much. They appreciate the gesture, but morale stays low."
      },
      {
        "id": "warn_lord",
        "text": "Warn {LORD_NAME} privately",
        "tooltip": "Alert the lord to the morale crisis",
        "risk": "safe",
        "rewards": { "xp": { "Charm": 20 } },
        "effects": {
          "relation": { "{LORD_NAME}": 1 }
        },
        "outcome": "{LORD_NAME} listens gravely. \"I'll address it. Thank you for bringing this to my attention.\" Whether they actually do something remains to be seen."
      },
      {
        "id": "stay_out",
        "text": "Not my problem. Focus on your own lance.",
        "tooltip": "Don't get involved in other lances' issues",
        "risk": "safe",
        "outcome": "You keep your head down. Whatever happens with those lances, it won't be your fault."
      }
    ]
  }
}
```

### Event Hooks in Code

The behavior exposes events that other systems can subscribe to:

```csharp
// src/Features/Lances/Behaviors/AILordLanceSimulationBehavior.cs

public class AILordLanceSimulationBehavior : CampaignBehaviorBase
{
    // Custom events for other systems to hook into
    public static event Action<MobileParty, AILance> OnLanceFormed;
    public static event Action<MobileParty, AILance> OnLanceDissolved;
    public static event Action<MobileParty, AILance, int> OnLanceCasualties;
    public static event Action<MobileParty, CompanyStatus> OnCompanyStatusChanged;
    public static event Action<MobileParty, SimulatedLanceMember> OnMemberKilled;
    public static event Action<MobileParty, SimulatedLanceMember> OnMemberWounded;
    
    // Fire events when things happen
    private void GenerateNewLance(MobileParty party)
    {
        var lance = new AILance(/* params */);
        _companyRegistry[party].Lances.Add(lance);
        
        // Notify subscribers
        OnLanceFormed?.Invoke(party, lance);
        
        // Log for debugging
        LogManager.Log($"[AI Lance] {party.Name} formed new lance: {lance.Name}");
    }
    
    private void DissolveLance(MobileParty party, AILance lance)
    {
        _companyRegistry[party].Lances.Remove(lance);
        
        // Notify subscribers
        OnLanceDissolved?.Invoke(party, lance);
        
        LogManager.Log($"[AI Lance] {party.Name} lost lance: {lance.Name}");
    }
    
    private void ApplyCasualty(MobileParty party, SimulatedLanceMember member)
    {
        member.State = MemberState.Dead;
        
        // Notify subscribers
        OnMemberKilled?.Invoke(party, member);
        
        // Check if this was the last member of a lance
        var lance = GetLanceForMember(party, member);
        if (lance != null && lance.Members.All(m => m.State == MemberState.Dead))
        {
            DissolveLance(party, lance);
        }
    }
}
```

### Subscribing to AI Lance Events

Other behaviors can subscribe to these events:

```csharp
// Example: News/Dispatch system subscribing to lance events
public class EnlistedNewsBehavior : CampaignBehaviorBase
{
    public override void RegisterEvents()
    {
        base.RegisterEvents();
        
        // Subscribe to AI lance events
        AILordLanceSimulationBehavior.OnLanceDissolved += OnAILanceDissolved;
        AILordLanceSimulationBehavior.OnLanceCasualties += OnAILanceCasualties;
        AILordLanceSimulationBehavior.OnCompanyStatusChanged += OnCompanyStatusChanged;
    }
    
    private void OnAILanceDissolved(MobileParty party, AILance lance)
    {
        // Generate news dispatch
        if (ShouldGenerateNewsFor(party))
        {
            var dispatch = new DispatchItem
            {
                Category = "battle",
                HeadlineKey = "News_LanceDestroyed",
                PlaceholderValues = new Dictionary<string, string>
                {
                    { "LORD", party.LeaderHero.Name.ToString() },
                    { "LANCE", lance.Name },
                    { "FACTION", party.MapFaction.Name.ToString() }
                },
                Type = DispatchType.Report,
                DayCreated = CampaignTime.Now.ToDays
            };
            
            AddToKingdomFeed(dispatch);
        }
    }
    
    private void OnAILanceCasualties(MobileParty party, AILance lance, int casualtyCount)
    {
        // Generate dispatch if casualties are heavy
        if (casualtyCount >= lance.Members.Count / 2)
        {
            var dispatch = new DispatchItem
            {
                Category = "battle",
                HeadlineKey = "News_LanceHeavyCasualties",
                PlaceholderValues = new Dictionary<string, string>
                {
                    { "LORD", party.LeaderHero.Name.ToString() },
                    { "LANCE", lance.Name },
                    { "CASUALTIES", casualtyCount.ToString() }
                },
                Type = DispatchType.Report,
                Confidence = 85
            };
            
            AddToKingdomFeed(dispatch);
        }
    }
}
```

### Custom Conditions for Events

Events can check AI lance state in their condition evaluation:

```csharp
// src/Features/Lances/Events/AILanceEventConditions.cs

public static class AILanceEventConditions
{
    /// <summary>
    /// Checks if player's enlisted lord has low morale lances
    /// </summary>
    public static bool allied_lord_morale_low()
    {
        var enlistment = EnlistmentBehavior.Instance;
        if (enlistment == null || !enlistment.IsEnlisted) return false;
        
        var lordParty = enlistment.EnlistedArmy?.LeaderParty;
        if (lordParty == null) return false;
        
        var company = AILordLanceSimulationBehavior.Instance?.GetCompany(lordParty);
        return company != null && company.Status.Morale < 40;
    }
    
    /// <summary>
    /// Checks if any enemy army the player knows about has been weakened
    /// </summary>
    public static bool enemy_army_weakened()
    {
        var knownEnemies = GetScoutedEnemyArmies();
        foreach (var party in knownEnemies)
        {
            var company = AILordLanceSimulationBehavior.Instance?.GetCompany(party);
            if (company != null && company.Status.Readiness < 50)
                return true;
        }
        return false;
    }
    
    /// <summary>
    /// Checks if player witnessed an allied lance being wiped out
    /// </summary>
    public static bool witnessed_allied_lance_loss()
    {
        return CampaignEventTracker.Instance.HasFlag("witnessed_lance_loss_last_battle");
    }
}
```

### Event-Driven Lance Simulation Features

#### Intelligence Report Events

When player scouts enemy armies:

```json
{
  "id": "scout_report_enemy_lances",
  "category": "ai_lance_intel",
  "delivery": {
    "method": "player_initiated",
    "channel": "menu",
    "menu": "army_encounter",
    "menu_section": "scout"
  },
  "content": {
    "title": "Enemy Army Composition",
    "setup": "Your scouts report on {ENEMY_LORD}'s army:\n\n{LANCE_COUNT} lances detected\n{MORALE_STATUS}\n{FOOD_STATUS}\n{READINESS_STATUS}\n\nEstimated combat effectiveness: {READINESS_PERCENT}%",
    "options": [
      {
        "id": "engage",
        "text": "They look vulnerable. Press the attack.",
        "condition": "enemy_readiness < 60",
        "tooltip": "Engage while they're weakened"
      },
      {
        "id": "avoid",
        "text": "Too strong. Fall back.",
        "tooltip": "Disengage and look for better targets"
      }
    ]
  }
}
```

#### Allied Army Support Events

When traveling with allied lords:

```json
{
  "id": "help_allied_lance",
  "category": "ai_lance_allied",
  "delivery": {
    "method": "automatic",
    "channel": "inquiry"
  },
  "triggers": {
    "all": ["watching_allied_army", "allied_lord_morale_low"]
  },
  "content": {
    "title": "A Request from {LORD_NAME}",
    "setup": "{LORD_NAME} approaches you quietly.\n\n\"My {LANCE_NAME} is struggling. Low supplies, lower morale. You have experience with keeping troops together. Would you... talk to them? As a peer?\"",
    "options": [
      {
        "id": "help",
        "text": "I'll do what I can.",
        "costs": { "fatigue": 10, "time_hours": 2 },
        "rewards": { "xp": { "Leadership": 30 } },
        "effects": {
          "relation": { "{LORD_NAME}": 3 },
          "lance_reputation": 5
        },
        "custom_effects": {
          "boost_allied_lance_morale": "{LANCE_NAME}"
        }
      }
    ]
  }
}
```

---

## News & Dispatch Integration

The AI Lord Lance Simulation integrates seamlessly with the News & Dispatches system to generate kingdom-wide and personal news based on AI lance activities.

### Dispatch Generation

The simulation automatically generates dispatches for significant events:

#### Kingdom Feed Dispatches

Dispatches that appear in the kingdom news feed for all players:

**Battle Aftermath:**
```csharp
private void GenerateBattleDispatch(MapEvent mapEvent, MobileParty party)
{
    var company = GetCompany(party);
    var casualties = CalculateCasualties(party, mapEvent);
    var lancesLost = company.GetLancesLostInBattle();
    
    if (lancesLost > 0)
    {
        var dispatch = new DispatchItem
        {
            Category = "battle",
            HeadlineKey = "News_LancesLost",
            PlaceholderValues = new Dictionary<string, string>
            {
                { "LORD", party.LeaderHero.Name.ToString() },
                { "LANCE_COUNT", lancesLost.ToString() },
                { "BATTLE_LOCATION", mapEvent.MapEventSettlement?.Name.ToString() ?? "the field" }
            },
            Type = DispatchType.Report,
            DayCreated = (int)CampaignTime.Now.ToDays,
            Confidence = 100
        };
        
        EnlistedNewsBehavior.Instance?.AddKingdomDispatch(dispatch);
    }
}
```

**Lance Formation:**
```csharp
private void GenerateLanceFormationDispatch(MobileParty party, AILance newLance)
{
    // Only for player's faction or major powers
    if (!ShouldGenerateNewsFor(party)) return;
    
    var dispatch = new DispatchItem
    {
        Category = "army",
        HeadlineKey = "News_NewLanceFormed",
        PlaceholderValues = new Dictionary<string, string>
        {
            { "LORD", party.LeaderHero.Name.ToString() },
            { "LANCE_NAME", newLance.Name },
            { "FACTION", party.MapFaction.Name.ToString() }
        },
        Type = DispatchType.Report,
        DayCreated = (int)CampaignTime.Now.ToDays
    };
    
    EnlistedNewsBehavior.Instance?.AddKingdomDispatch(dispatch);
}
```

#### Personal Feed Dispatches

Dispatches specific to the player's experience:

**Allied Army Status:**
```csharp
private void GenerateAlliedArmyStatusDispatch(MobileParty party)
{
    var enlistment = EnlistmentBehavior.Instance;
    if (enlistment?.EnlistedArmy?.LeaderParty != party) return;
    
    var company = GetCompany(party);
    if (company.Status.Morale < 40)
    {
        var dispatch = new DispatchItem
        {
            Category = "personal",
            HeadlineKey = "News_YourLordArmyMoraleLow",
            PlaceholderValues = new Dictionary<string, string>
            {
                { "LORD", party.LeaderHero.Name.ToString() },
                { "MORALE", company.Status.Morale.ToString() }
            },
            Type = DispatchType.Report,
            DayCreated = (int)CampaignTime.Now.ToDays
        };
        
        EnlistedNewsBehavior.Instance?.AddPersonalDispatch(dispatch);
    }
}
```

**Enemy Intelligence:**
```csharp
private void GenerateEnemyIntelDispatch(MobileParty enemyParty)
{
    var company = GetCompany(enemyParty);
    var dispatch = new DispatchItem
    {
        Category = "intel",
        HeadlineKey = "News_EnemyArmyIntel",
        PlaceholderValues = new Dictionary<string, string>
        {
            { "LORD", enemyParty.LeaderHero.Name.ToString() },
            { "LANCE_COUNT", company.Lances.Count.ToString() },
            { "READINESS", GetReadinessText(company.Status.Readiness) },
            { "THREAT_LEVEL", CalculateThreatLevel(company).ToString() }
        },
        Type = DispatchType.Report,
        Confidence = GetIntelConfidence(enemyParty),
        DayCreated = (int)CampaignTime.Now.ToDays
    };
    
    EnlistedNewsBehavior.Instance?.AddPersonalDispatch(dispatch);
}
```

### Localization Strings for Dispatches

**File:** `ModuleData/Languages/enlisted_strings.xml`

```xml
<!-- AI Lance Simulation - News Headlines -->
<string id="News_LancesLost" text="{LORD} loses {LANCE_COUNT} lances at {BATTLE_LOCATION}" />
<string id="News_NewLanceFormed" text="{LORD} forms new lance: {LANCE_NAME}" />
<string id="News_LanceDestroyed" text="{LANCE} eliminated in battle" />
<string id="News_LanceHeavyCasualties" text="{LANCE} suffers heavy casualties: {CASUALTIES} lost" />
<string id="News_YourLordArmyMoraleLow" text="Morale crisis in {LORD}'s army" />
<string id="News_EnemyArmyIntel" text="Intel: {LORD} commands {LANCE_COUNT} lances, {READINESS} readiness" />
<string id="News_AlliedLanceAssist" text="You helped stabilize {LORD}'s {LANCE_NAME}" />
```

### Dispatch Categories and Priority

```csharp
public enum DispatchPriority
{
    Low = 0,        // Formation of new lances, routine updates
    Medium = 1,     // Heavy casualties, morale changes
    High = 2,       // Lance wiped out, commander killed
    Critical = 3    // Army collapse, major defeat
}

private DispatchPriority DetermineDispatchPriority(string category, MobileParty party)
{
    var company = GetCompany(party);
    
    // Allied armies get higher priority
    bool isAllied = FactionManager.IsAlliedWithFaction(
        Hero.MainHero.MapFaction, 
        party.MapFaction
    );
    
    // Major events get boosted
    if (company.WasLanceLostInLastBattle())
        return isAllied ? DispatchPriority.Critical : DispatchPriority.High;
    
    if (company.Status.Morale < 30)
        return isAllied ? DispatchPriority.High : DispatchPriority.Medium;
    
    return DispatchPriority.Low;
}
```

### Integration with Menu Display

The news system displays AI lance dispatches in camp menus:

```csharp
// In EnlistedMenuBehavior.cs - Status display section

private string BuildNewsSection()
{
    var sb = new StringBuilder();
    sb.AppendLine("\n--- Kingdom News ---");
    
    var recentDispatches = EnlistedNewsBehavior.Instance?.GetRecentKingdomNews(3);
    foreach (var dispatch in recentDispatches)
    {
        // Format dispatch based on category
        string icon = GetDispatchIcon(dispatch.Category);
        string text = FormatDispatch(dispatch);
        
        sb.AppendLine($"{icon} {text}");
    }
    
    // Personal news
    sb.AppendLine("\n--- Personal Dispatches ---");
    var personalDispatches = EnlistedNewsBehavior.Instance?.GetRecentPersonalNews(2);
    foreach (var dispatch in personalDispatches)
    {
        string text = FormatDispatch(dispatch);
        sb.AppendLine($"• {text}");
    }
    
    return sb.ToString();
}
```

### Event-Driven Dispatch Flow

```
AI Lance Event Occurs (e.g., lance wiped out)
    ↓
AILordLanceSimulationBehavior fires OnLanceDissolved event
    ↓
EnlistedNewsBehavior.OnAILanceDissolved() receives event
    ↓
Generate DispatchItem with localized headline
    ↓
Add to appropriate feed (kingdom or personal)
    ↓
Next time player opens camp menu → dispatch appears in news section
```

---

## Configuration

```csharp
// src/Mod.Core/Config/ModSettings.cs

public class LanceSimulationSettings
{
    [SettingPropertyInteger(
        "Troops Per Lance (AI)",
        8, 12, 
        Order = 0,
        RequireRestart = false,
        HintText = "How many troops form one lance in AI lord armies."
    )]
    [SettingPropertyGroup("Lance Simulation")]
    public int TroopsPerLance { get; set; } = 10;
    
    [SettingPropertyBool(
        "Enable AI Lance Simulation",
        Order = 1,
        RequireRestart = false,
        HintText = "Simulate lance structure for AI lord armies."
    )]
    [SettingPropertyGroup("Lance Simulation")]
    public bool EnableAILanceSimulation { get; set; } = true;
    
    [SettingPropertyBool(
        "Show Enemy Lance Inspector",
        Order = 2,
        RequireRestart = false,
        HintText = "Allow inspecting enemy army lance composition after scouting."
    )]
    [SettingPropertyGroup("Lance Simulation")]
    public bool ShowEnemyLanceInspector { get; set; } = true;
    
    [SettingPropertyFloatingInteger(
        "Officer Survival Rate",
        0.5f, 1.0f,
        Order = 3,
        RequireRestart = false,
        HintText = "Chance for officers (Tier 4+) to survive battle casualties."
    )]
    [SettingPropertyGroup("Lance Simulation")]
    public float OfficerSurvivalRate { get; set; } = 0.85f;
    
    [SettingPropertyFloatingInteger(
        "Private Survival Rate",
        0.3f, 0.8f,
        Order = 4,
        RequireRestart = false,
        HintText = "Chance for privates (Tier 1) to survive battle casualties."
    )]
    [SettingPropertyGroup("Lance Simulation")]
    public float PrivateSurvivalRate { get; set; } = 0.50f;
}
```

### JSON Configuration

```json
// ModuleData/Enlisted/lance_simulation_config.json
// Schema Version: 1.0
// Description: Configuration for AI Lord Lance Simulation system.
// This file can be modified by other mods to customize lance behavior.

{
  "$schema": "./schemas/lance_simulation_config.schema.json",
  "schemaVersion": 1,
  "enabled": true,
  "showEnemyInspector": true,
  
  "lance_names": {
    "_comment": "Name components for generating lance names. Add custom entries here.",
    "formats": [
      "{adjective} {unit_type}",
      "{lord_name}'s {unit_type}",
      "The {adjective} {military_term}"
    ],
    "adjectives": [
      "Bold", "Iron", "Swift", "Thunder", "Storm", "Grim", 
      "Stalwart", "Valiant", "Daring", "Fierce", "Crimson", "Shadow"
    ],
    "unit_types": [
      "Lances", "Blades", "Hawks", "Wolves", "Riders", 
      "Guards", "Wardens", "Raiders", "Shields", "Spears"
    ],
    "military_terms": [
      "Vanguard", "Rearguard", "Scouts", "Sentinels", "Warband", "Company"
    ],
    "generic_member_names": [
      "Aldric", "Bors", "Cadoc", "Drest", "Elwin", "Finn", "Gareth",
      "Hector", "Idris", "Jorah", "Kael", "Leoric", "Mabon", "Nero"
    ]
  },
  
  "rank_survival_weights": {
    "_comment": "Survival chance per rank (0.0 = always dies, 1.0 = never dies).",
    "lance_leader": 0.90,
    "sergeant": 0.80,
    "corporal": 0.70,
    "lance_corporal": 0.60,
    "private": 0.50
  },
  
  "company_status": {
    "_comment": "Thresholds for status displays in menus.",
    "morale_thresholds": {
      "high_spirits": 80,
      "steady": 60,
      "shaken": 40,
      "broken": 0
    },
    "food_thresholds": {
      "well_supplied": 10,
      "adequate": 5,
      "low": 2,
      "starving": 0
    },
    "cohesion_thresholds": {
      "elite": 80,
      "disciplined": 60,
      "disorganized": 40,
      "scattered": 0
    },
    "readiness_thresholds": {
      "battle_ready": 80,
      "combat_capable": 60,
      "diminished": 40,
      "ineffective": 0
    }
  },
  
  "lance_composition": {
    "_comment": "Lance size and organization rules.",
    "min_size": 8,
    "max_size": 12,
    "default_size": 10,
    "leader_count": 1,
    "sergeant_count": 1,
    "corporal_ratio": 0.25,
    "minimum_for_reorganization": 5,
    "_note": "minimum_for_reorganization: lances below this size get consolidated"
  },
  
  "casualty_rules": {
    "_comment": "Rules for applying battle casualties to simulated lances.",
    "death_chance": 0.70,
    "wounded_recovery_chance_per_day": 0.10,
    "apply_rank_weight": true,
    "_note": "death_chance: 70% of casualties die, 30% wounded"
  },
  
  "events": {
    "_comment": "Event generation settings for AI lance events.",
    "generate_battle_events": true,
    "generate_morale_events": true,
    "generate_intel_events": true,
    "min_days_between_events": 1
  },
  
  "news_dispatches": {
    "_comment": "News dispatch generation settings.",
    "generate_kingdom_feed": true,
    "generate_personal_feed": true,
    "only_player_faction": false,
    "only_major_powers": true,
    "_note": "only_major_powers: only generate news for kingdoms, not minor clans"
  },
  
  "performance": {
    "_comment": "Performance optimization settings.",
    "lazy_load_companies": true,
    "update_interval_hours": 24,
    "max_cached_companies": 100,
    "_note": "lazy_load_companies: only create companies when player views them"
  },
  
  "mod_compatibility": {
    "_comment": "Settings for mod compatibility and extensibility.",
    "allow_custom_name_generators": true,
    "allow_custom_event_handlers": true,
    "preserve_custom_data": true
  }
}
```

**JSON Schema Definition** (optional but recommended for validation):

```json
// ModuleData/Enlisted/schemas/lance_simulation_config.schema.json

{
  "$schema": "http://json-schema.org/draft-07/schema#",
  "$id": "https://enlisted-mod.com/schemas/lance_simulation_config.json",
  "title": "AI Lance Simulation Configuration",
  "description": "Configuration schema for AI Lord Lance Simulation system",
  "type": "object",
  "properties": {
    "schemaVersion": {
      "type": "integer",
      "description": "Schema version number",
      "minimum": 1
    },
    "enabled": {
      "type": "boolean",
      "description": "Whether AI lance simulation is enabled"
    },
    "lance_composition": {
      "type": "object",
      "properties": {
        "min_size": { "type": "integer", "minimum": 5, "maximum": 15 },
        "max_size": { "type": "integer", "minimum": 5, "maximum": 20 },
        "default_size": { "type": "integer", "minimum": 5, "maximum": 20 }
      },
      "required": ["min_size", "max_size", "default_size"]
    },
    "rank_survival_weights": {
      "type": "object",
      "properties": {
        "lance_leader": { "type": "number", "minimum": 0.0, "maximum": 1.0 },
        "sergeant": { "type": "number", "minimum": 0.0, "maximum": 1.0 },
        "corporal": { "type": "number", "minimum": 0.0, "maximum": 1.0 },
        "lance_corporal": { "type": "number", "minimum": 0.0, "maximum": 1.0 },
        "private": { "type": "number", "minimum": 0.0, "maximum": 1.0 }
      }
    }
  },
  "required": ["schemaVersion", "enabled"]
}
```

---

## Implementation Roadmap

### Phase 1: Core System (Week 1)
- [ ] Create `AILordLanceSimulationBehavior`
- [ ] Implement `AILanceCompany` and `AILance` classes
- [ ] Implement `SimulatedLanceMember` data structure
- [ ] Add basic lance generation (count calculation, member creation)
- [ ] Hook into Bannerlord party events (recruitment, casualties)
- [ ] **Add C# events:** `OnLanceFormed`, `OnLanceDissolved`, `OnMemberKilled`
- [ ] Register behavior in `SubModule.cs`

### Phase 2: Casualty System (Week 2)
- [ ] Implement rank-weighted casualty selection
- [ ] Add lance reorganization logic (consolidation, dissolution)
- [ ] Implement wounded member tracking and recovery
- [ ] Add lance loss detection and morale effects
- [ ] Test with various battle scenarios
- [ ] **Fire events:** Invoke C# events when casualties occur

### Phase 3: Company Status (Week 3)
- [ ] Implement morale calculation
- [ ] Implement food status tracking
- [ ] Implement cohesion calculation
- [ ] Implement combat readiness calculation
- [ ] Add status update hooks (daily tick, post-battle)
- [ ] **Add event:** `OnCompanyStatusChanged`

### Phase 4: Name Generation (Week 4)
- [ ] Research Bannerlord name generation API
- [ ] Implement culture-specific lance name generation
- [ ] Implement culture-specific member name generation
- [ ] Add fallback generic name system
- [ ] Create name component JSON config

### Phase 5: Menu Interface (Week 5-6)
- [ ] Create army inspector menu entry points
- [ ] Implement company status display
- [ ] Create lance list view
- [ ] Create detailed lance inspector
- [ ] Add army comparison feature
- [ ] Polish UI text and formatting

### Phase 6: Event System Integration (Week 7)
- [ ] **Create trigger tokens:** `ai_lance_battle_occurred`, `ai_lance_heavy_casualties`, etc.
- [ ] **Implement `AILanceEventConditions`:** Custom condition checks
- [ ] **Create event pack:** `ModuleData/Enlisted/Events/events_ai_lance.json`
- [ ] **Write example events:** Enemy lance eliminated, allied morale crisis
- [ ] **Add localization strings:** Event titles, setup text, outcomes
- [ ] **Test event delivery:** Verify events fire correctly with existing event system
- [ ] **Document event schema:** Add to technical docs

### Phase 7: News & Dispatch Integration (Week 8)
- [ ] **Subscribe to AI lance events** in `EnlistedNewsBehavior`
- [ ] **Implement dispatch generation:** Lance wiped out, heavy casualties, new lance formed
- [ ] **Add dispatch categories:** `ai_lance_intel`, `ai_lance_allied`, `ai_lance_battle`
- [ ] **Create localization strings:** Dispatch headlines and content
- [ ] **Integrate with menu display:** Show AI lance news in camp status
- [ ] **Test dispatch priority:** Verify allied armies get higher priority
- [ ] **Add intelligence filtering:** Restrict enemy dispatches based on intel level

### Phase 8: Integration & Testing (Week 9)
- [ ] Add ModSettings configuration
- [ ] Integrate with intelligence/scouting system
- [ ] Test with all factions and cultures
- [ ] Performance testing (100+ parties)
- [ ] Balance survival weights and thresholds
- [ ] **Test event-driven features:** Verify events and dispatches work together
- [ ] **Performance test event system:** Ensure no lag from event generation

### Phase 9: Polish & Documentation (Week 10)
- [ ] Add tooltips and help text
- [ ] Write user-facing documentation
- [ ] Create example screenshots
- [ ] Final bug fixes
- [ ] Prepare for release
- [ ] **Document event authoring:** Guide for creating new AI lance events
- [ ] **Example event pack:** Ship sample events for modders to extend

---

## Data Models & Saveable Structures

### SimulatedLanceMember

```csharp
// src/Features/Lances/Models/SimulatedLanceMember.cs

using TaleWorlds.Core;
using TaleWorlds.SaveSystem;
using JetBrains.Annotations;

namespace Enlisted.Features.Lances.Models
{
    /// <summary>
    /// Represents a simulated lance member in an AI lord's army.
    /// Lightweight structure for procedurally generated soldiers.
    /// </summary>
    [SaveableStruct(200)]
    public struct SimulatedLanceMember
    {
        /// <summary>
        /// Gets or sets the member's name.
        /// </summary>
        [SaveableField(1)]
        [NotNull]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the member's rank (e.g., "Private", "Sergeant").
        /// </summary>
        [SaveableField(2)]
        [NotNull]
        public string Rank { get; set; }

        /// <summary>
        /// Gets or sets the member's troop tier (1-6).
        /// </summary>
        [SaveableField(3)]
        public int Tier { get; set; }

        /// <summary>
        /// Gets or sets the member's formation class.
        /// </summary>
        [SaveableField(4)]
        public FormationClass Formation { get; set; }

        /// <summary>
        /// Gets or sets the member's current state (Healthy, Wounded, Dead).
        /// </summary>
        [SaveableField(5)]
        public MemberState State { get; set; }

        /// <summary>
        /// Gets or sets the survival weight (higher = more likely to survive).
        /// </summary>
        [SaveableField(6)]
        public float SurvivalWeight { get; set; }

        /// <summary>
        /// Gets or sets the lance ID this member belongs to.
        /// </summary>
        [SaveableField(7)]
        [NotNull]
        public string LanceId { get; set; }
    }

    /// <summary>
    /// Enumeration of possible member states.
    /// </summary>
    public enum MemberState : byte
    {
        /// <summary>
        /// Member is healthy and available for duty.
        /// </summary>
        Healthy = 0,

        /// <summary>
        /// Member is wounded and recovering.
        /// </summary>
        Wounded = 1,

        /// <summary>
        /// Member has been killed in action.
        /// </summary>
        Dead = 2
    }
}
```

### CompanyStatus

```csharp
// src/Features/Lances/Models/CompanyStatus.cs

using TaleWorlds.SaveSystem;

namespace Enlisted.Features.Lances.Models
{
    /// <summary>
    /// Represents the aggregate status of an AI lord's company.
    /// Used for intelligence reports and menu displays.
    /// </summary>
    [SaveableStruct(201)]
    public struct CompanyStatus
    {
        /// <summary>
        /// Gets or sets the morale level (0-100).
        /// </summary>
        [SaveableField(1)]
        public int Morale { get; set; }

        /// <summary>
        /// Gets or sets the number of days of food remaining.
        /// </summary>
        [SaveableField(2)]
        public float FoodDays { get; set; }

        /// <summary>
        /// Gets or sets the cohesion level (0-100).
        /// Represents unit organization and coordination.
        /// </summary>
        [SaveableField(3)]
        public int Cohesion { get; set; }

        /// <summary>
        /// Gets or sets the combat readiness (0-100).
        /// Aggregate of morale, cohesion, supplies, and equipment.
        /// </summary>
        [SaveableField(4)]
        public int Readiness { get; set; }

        /// <summary>
        /// Creates a default status with neutral values.
        /// </summary>
        public static CompanyStatus Default => new CompanyStatus
        {
            Morale = 50,
            FoodDays = 7,
            Cohesion = 50,
            Readiness = 50
        };
    }
}
```

---

## Mod Extensibility Guide

This section explains how other mods can extend or customize the AI Lance Simulation.

### 1. Custom Name Generators

Create a custom name generator for specific cultures or factions:

```csharp
using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;
using Enlisted.Features.Lances.Interfaces;

namespace MyMod.CustomLances
{
    /// <summary>
    /// Custom name generator for Vlandian-style lance names.
    /// </summary>
    public class VlandianLanceNameGenerator : ILanceNameGenerator
    {
        private static readonly string[] KnightTitles = 
            { "Sir", "Lord", "Baron", "Count" };
        
        private static readonly string[] LanceTypes = 
            { "Company", "Knights", "Regiment", "Guards" };

        public string GenerateLanceName(CultureObject culture, Hero lord)
        {
            if (culture?.StringId == "vlandia" && lord != null)
            {
                var title = KnightTitles[MBRandom.RandomInt(KnightTitles.Length)];
                var type = LanceTypes[MBRandom.RandomInt(LanceTypes.Length)];
                return $"{title} {lord.FirstName}'s {type}";
            }

            // Fall back to default for other cultures
            return new DefaultLanceNameGenerator()
                .GenerateLanceName(culture, lord);
        }

        public string GenerateMemberName(CultureObject culture)
        {
            // Use Bannerlord's built-in Vlandian names
            // Your implementation here
            return "Vlandian Soldier";
        }
    }

    /// <summary>
    /// Register your custom generator in SubModule.
    /// </summary>
    public class MyModSubModule : MBSubModuleBase
    {
        protected override void OnGameStart(Game game, IGameStarter gameStarter)
        {
            if (game.GameType is Campaign)
            {
                var campaignStarter = (CampaignGameStarter)gameStarter;
                
                // Register custom name generator
                campaignStarter.AddBehavior(
                    new AILordLanceSimulationBehavior(
                        new VlandianLanceNameGenerator(),
                        null // Use default config
                    )
                );
            }
        }
    }
}
```

### 2. Custom Event Subscribers

Subscribe to AI lance events to trigger your own mod features:

```csharp
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using Enlisted.Features.Lances.Behaviors;
using Enlisted.Features.Lances.Models;

namespace MyMod.EventHandlers
{
    /// <summary>
    /// Subscribes to AI lance events for custom mod features.
    /// </summary>
    public class MyCustomLanceEventHandler : CampaignBehaviorBase
    {
        public override void RegisterEvents()
        {
            // Subscribe to lance events
            AILordLanceSimulationBehavior.OnLanceDissolved += OnLanceLost;
            AILordLanceSimulationBehavior.OnCompanyStatusChanged += OnStatusChanged;
        }

        public override void SyncData(IDataStore dataStore)
        {
            // Your persistence logic
        }

        private void OnLanceLost(MobileParty party, AILance lance)
        {
            // Your custom logic when a lance is eliminated
            // Example: Grant player renown for destroying enemy lances
            if (FactionManager.IsAtWarAgainstFaction(
                Hero.MainHero.MapFaction, 
                party.MapFaction))
            {
                GainRenownAction.Apply(Hero.MainHero, 5f);
                InformationManager.DisplayMessage(
                    new InformationMessage(
                        $"Enemy {lance.Name} destroyed! (+5 Renown)"
                    )
                );
            }
        }

        private void OnStatusChanged(MobileParty party, CompanyStatus status)
        {
            // Your custom logic when company status changes
            // Example: Trigger special events when allied army is low morale
            if (status.Morale < 30 && IsAlliedArmy(party))
            {
                TriggerMoraleBoostQuest(party);
            }
        }

        private bool IsAlliedArmy(MobileParty party)
        {
            return FactionManager.IsAlliedWithFaction(
                Hero.MainHero.MapFaction,
                party.MapFaction
            );
        }

        private void TriggerMoraleBoostQuest(MobileParty party)
        {
            // Your quest logic here
        }
    }
}
```

### 3. Custom Configuration Providers

Provide custom configuration based on your mod's settings:

```csharp
using Enlisted.Features.Lances.Interfaces;

namespace MyMod.Configuration
{
    /// <summary>
    /// Custom configuration that integrates with your mod's settings.
    /// </summary>
    public class MyModAILanceConfig : IAILanceConfig
    {
        private readonly MyModSettings _settings;

        public MyModAILanceConfig(MyModSettings settings)
        {
            _settings = settings;
        }

        public int TroopsPerLance => _settings.CustomLanceSize;
        public bool IsEnabled => _settings.EnableAILances;
        public float OfficerSurvivalRate => _settings.OfficerProtection;
        public float PrivateSurvivalRate => _settings.SoldierSurvivalRate;
        public bool ShowEnemyLanceInspector => _settings.AllowEnemyIntel;
    }

    /// <summary>
    /// Register in SubModule.
    /// </summary>
    public class MyModSubModule : MBSubModuleBase
    {
        protected override void OnGameStart(Game game, IGameStarter gameStarter)
        {
            if (game.GameType is Campaign)
            {
                var campaignStarter = (CampaignGameStarter)gameStarter;
                var mySettings = MyModSettings.Instance;
                
                campaignStarter.AddBehavior(
                    new AILordLanceSimulationBehavior(
                        null, // Use default name generator
                        new MyModAILanceConfig(mySettings)
                    )
                );
            }
        }
    }
}
```

### 4. Extending JSON Configuration

Add custom entries to `lance_simulation_config.json`:

```json
{
  "schemaVersion": 1,
  "enabled": true,
  
  // Add your mod's custom section
  "my_mod_extensions": {
    "enable_elite_lances": true,
    "elite_lance_threshold": 5,
    "custom_lance_abilities": [
      {
        "name": "Battle Hardened",
        "battles_required": 10,
        "bonus_morale": 10
      }
    ]
  }
}
```

Then load it in your mod:

```csharp
public class MyModConfig
{
    public static MyModExtensions LoadExtensions()
    {
        var configPath = GetConfigPath();
        var json = File.ReadAllText(configPath);
        var config = JsonConvert.DeserializeObject<dynamic>(json);
        
        return JsonConvert.DeserializeObject<MyModExtensions>(
            config.my_mod_extensions.ToString()
        );
    }
}
```

### 5. Custom Menu Extensions

Add your own menu options to the lance inspector:

```csharp
using TaleWorlds.CampaignSystem.GameMenus;

namespace MyMod.Menus
{
    public static class CustomLanceMenuExtensions
    {
        public static void AddCustomMenuOptions(CampaignGameStarter starter)
        {
            // Add option to "ai_army_inspector" menu
            starter.AddGameMenuOption(
                "ai_army_inspector",
                "my_mod_special_action",
                "Perform Special Action",
                args => MyModCanPerformAction(),
                args => MyModPerformAction(),
                false,
                4 // Priority after standard options
            );
        }

        private static bool MyModCanPerformAction()
        {
            // Your condition logic
            return true;
        }

        private static void MyModPerformAction()
        {
            // Your action logic
            InformationManager.DisplayMessage(
                new InformationMessage("Custom action performed!")
            );
        }
    }
}
```

### Mod Compatibility Best Practices

1. **Check for Instance Before Use:**
   ```csharp
   if (AILordLanceSimulationBehavior.Instance != null)
   {
       var company = AILordLanceSimulationBehavior.Instance.GetCompany(party);
   }
   ```

2. **Handle Null Returns:**
   ```csharp
   var company = AILordLanceSimulationBehavior.Instance?.GetCompany(party);
   if (company != null)
   {
       // Safe to use company
   }
   ```

3. **Use Interfaces, Not Concrete Classes:**
   ```csharp
   // Good - flexible
   public void MyMethod(ILanceNameGenerator generator) { }
   
   // Bad - coupled to implementation
   public void MyMethod(DefaultLanceNameGenerator generator) { }
   ```

4. **Don't Modify Core Behavior Directly:**
   ```csharp
   // Bad - direct modification
   AILordLanceSimulationBehavior.Instance.SomeField = newValue;
   
   // Good - use events and interfaces
   AILordLanceSimulationBehavior.OnLanceFormed += MyCustomHandler;
   ```

5. **Preserve Backward Compatibility:**
   ```json
   {
     "schemaVersion": 1,
     "_my_mod_version": "1.2.0",
     "_backward_compatible": true
   }
   ```

---

## File Structure

Complete reference for where all files related to AI Lord Lance Simulation should be located:

### Source Code

```
src/Features/Lances/
├── Behaviors/
│   ├── AILordLanceSimulationBehavior.cs    # Main behavior, simulation logic
│   └── LanceLifeSimulationBehavior.cs      # Player's lance (existing)
│
├── Models/
│   ├── AILanceCompany.cs                   # Company-level state and logic
│   ├── AILance.cs                          # Individual lance state
│   ├── SimulatedLanceMember.cs             # Lance member data structure
│   └── CompanyStatus.cs                    # Morale, food, cohesion stats
│
├── Menus/
│   ├── AILanceInspectorMenu.cs             # Army inspector menu logic
│   └── LanceMenuHelpers.cs                 # Shared menu utilities
│
└── Events/
    ├── AILanceEventConditions.cs           # Custom event conditions
    └── AILanceEventTriggers.cs             # Trigger token definitions
```

### Module Data

```
ModuleData/Enlisted/
├── Events/
│   ├── events_ai_lance.json                # AI lance event definitions
│   └── schema_version.json                 # (existing)
│
├── lance_simulation_config.json            # AI lance config (new)
│
└── enlisted_config.json                    # (existing, add ai_lance section)
```

### Localization

```
ModuleData/Languages/
└── enlisted_strings.xml                    # Add AI lance strings
    ├── Event titles/setup text (ll_evt_*)
    ├── Menu text (ll_menu_*)
    └── News headlines (News_*)
```

### Documentation

```
docs/Features/Core/
├── ai-lord-lance-simulation.md            # This document
├── lance-life-simulation.md               # Player's lance (existing)
└── ai-camp-schedule.md                    # AI scheduling (existing)
```

### Example Localization Entries

```xml
<!-- AI Lance Events -->
<string id="ll_evt_enemy_lance_eliminated_title" text="Enemy Lance Destroyed" />
<string id="ll_evt_enemy_lance_eliminated_setup" text="Your scouts return with news: {ENEMY_LORD}'s {LANCE_NAME} was annihilated in the recent battle. All hands lost. That's one less lance to worry about." />
<string id="ll_evt_allied_morale_crisis_title" text="Trouble in the Ranks" />
<string id="ll_evt_allied_morale_crisis_setup" text="{LORD_NAME}'s army is showing signs of strain. You've seen the hollow looks, heard the grumbling. Word is several lances are at breaking point - low on food, high on fear.\n\nTheir morale could crack at the worst possible moment." />

<!-- AI Lance Menu Text -->
<string id="ll_menu_ai_army_inspector" text="Army Inspector - {LORD_NAME}'s Company" />
<string id="ll_menu_company_status" text="Company Status" />
<string id="ll_menu_morale" text="Morale" />
<string id="ll_menu_food_status" text="Food" />
<string id="ll_menu_cohesion" text="Cohesion" />
<string id="ll_menu_readiness" text="Combat Readiness" />
<string id="ll_menu_lance_list" text="Lances" />
<string id="ll_menu_lance_details" text="Lance Details - {LANCE_NAME}" />

<!-- AI Lance News Headlines -->
<string id="News_LancesLost" text="{LORD} loses {LANCE_COUNT} lances at {BATTLE_LOCATION}" />
<string id="News_NewLanceFormed" text="{LORD} forms new lance: {LANCE_NAME}" />
<string id="News_LanceDestroyed" text="{LANCE} eliminated in battle" />
<string id="News_LanceHeavyCasualties" text="{LANCE} suffers heavy casualties: {CASUALTIES} lost" />
<string id="News_YourLordArmyMoraleLow" text="Morale crisis in {LORD}'s army" />
<string id="News_EnemyArmyIntel" text="Intel: {LORD} commands {LANCE_COUNT} lances, {READINESS} readiness" />

<!-- Status Text -->
<string id="Status_HighSpirits" text="High Spirits" />
<string id="Status_Steady" text="Steady" />
<string id="Status_Shaken" text="Shaken" />
<string id="Status_Broken" text="Broken" />
<string id="Status_WellSupplied" text="Well Supplied" />
<string id="Status_Adequate" text="Adequate" />
<string id="Status_LowSupplies" text="Low Supplies" />
<string id="Status_Starving" text="Starving" />
<string id="Status_EliteFormation" text="Elite Formation" />
<string id="Status_Disciplined" text="Disciplined" />
<string id="Status_Disorganized" text="Disorganized" />
<string id="Status_Scattered" text="Scattered" />
<string id="Status_BattleReady" text="Battle Ready" />
<string id="Status_CombatCapable" text="Combat Capable" />
<string id="Status_Diminished" text="Diminished" />
<string id="Status_CombatIneffective" text="Combat Ineffective" />
```

---

## Open Questions & Design Decisions

### Q1: Should we persist AI lance data or regenerate on-demand?

**Option A: Full Persistence**
- Save all lance member names, states, etc.
- Pro: Consistent across game sessions
- Con: Large save file size, complexity

**Option B: Regenerate on View**
- Only persist lance count and casualty distribution
- Regenerate names/details when player views
- Pro: Minimal save data
- Con: Names change between views

**Recommendation:** **Option B** - Regenerate with seeded randomness (use party ID as seed). This keeps save files small while maintaining consistency within a session.

### Q2: How much detail for wounded members?

**Option A: Simple Binary** (Current Design)
- Healthy or Dead only
- Pro: Simple, low overhead
- Con: No recovery gameplay

**Option B: Wounded State**
- Track wounded, recover over time
- Pro: More realistic, recovery events
- Con: More complexity

**Recommendation:** **Option A initially**, add Option B in future update if desired.

### Q3: Should player be able to interact with enemy lances?

**Potential Interactions:**
- Bribe enemy lance to defect
- Assassinate enemy lance leader
- Sabotage enemy lance equipment
- Recruit from defeated enemy lances

**Recommendation:** Not in initial implementation. Add as "Lance Warfare" expansion feature later.

### Q4: Performance considerations for large campaigns

With 50-100 lord parties:
- **Memory:** ~50KB per company (names, states) = ~5MB total
- **CPU:** Daily tick for each party = ~50ms total
- **On-Demand Generation:** Negligible

**Mitigation:**
- Only initialize simulation for parties player has encountered
- Lazy-load detailed lance info on menu open
- Use object pooling for lance member structs

---

## Future Enhancements

### Advanced Intelligence System
- **Spy Networks:** Plant spies in enemy armies to get real-time lance updates
- **Interrogation:** Question prisoners about their lance composition
- **Scouts:** Better scouting reveals more detail

### Dynamic Lance Personality
- **Elite Lances:** Some lances gain bonuses after multiple victories
- **Demoralized Lances:** Lances with high casualties fight worse
- **Rival Lances:** Create rivalries between player's and enemy lances

### Lance Transfer System
- **Promotions:** AI lance members get promoted between lances
- **Detachments:** Lords split off lances for special missions
- **Garrison Integration:** Lances rotate to/from garrison duty

### Historical Battle Records
- **Lance Battle History:** Track which lances fought which battles
- **Casualties Over Time:** View casualty graphs for specific lances
- **Veteran Status:** Lances gain titles based on performance

---

## Content Authoring Guide

This section is for modders and content creators who want to add new events that integrate with the AI Lance Simulation.

### Creating AI Lance Events

#### Step 1: Choose Event Category

Determine which category fits your event:

| Category | Purpose | Examples |
|----------|---------|----------|
| `ai_lance_intel` | Intelligence/reconnaissance | Scout reports, enemy lance status |
| `ai_lance_allied` | Allied army interactions | Help allied lances, morale support |
| `ai_lance_battle` | Battle aftermath | Casualty reports, lance losses |
| `ai_lance_morale` | Morale events | Army morale crisis, celebration |

#### Step 2: Define Triggers

Use available trigger tokens:

```json
{
  "triggers": {
    "all": ["is_enlisted", "ai_safe", "enemy_army_scouted"],
    "any": ["ai_lance_heavy_casualties", "ai_lance_wiped_out"],
    "custom_conditions": ["enemy_army_weakened"]
  }
}
```

**Available Tokens:**
- `ai_lance_battle_occurred` - Any AI battle just ended
- `ai_lance_heavy_casualties` - Army lost 30%+ troops
- `ai_lance_wiped_out` - Full lance eliminated
- `ai_lance_new_formed` - New lance created
- `watching_allied_army` - Player in allied army
- `enemy_army_scouted` - Player scouted enemy

#### Step 3: Write Event Content

Use placeholder tokens for dynamic content:

**Standard Placeholders:**
- `{PLAYER_NAME}` - Player's name
- `{LORD_NAME}` - Lord's name
- `{ENEMY_LORD}` - Enemy lord's name
- `{LANCE_NAME}` - Specific lance name
- `{LANCE_COUNT}` - Number of lances
- `{LANCE_LEADER_SHORT}` - Lance leader's name

**AI Lance-Specific Placeholders:**
- `{MORALE_STATUS}` - "High Spirits", "Shaken", etc.
- `{FOOD_STATUS}` - "Well Supplied", "Starving", etc.
- `{READINESS_STATUS}` - "Battle Ready", "Diminished", etc.
- `{READINESS_PERCENT}` - Numeric readiness (0-100)
- `{CASUALTIES}` - Number of casualties
- `{BATTLE_LOCATION}` - Where battle occurred

#### Step 4: Define Effects

```json
{
  "effects": {
    "heat": 0,
    "discipline": 0,
    "lance_reputation": 5,
    "relation": { "{LORD_NAME}": 2 }
  },
  "custom_effects": {
    "boost_allied_lance_morale": "{LANCE_NAME}"
  }
}
```

**Custom Effects Available:**
- `boost_allied_lance_morale` - Increase morale of specific lance
- `reveal_enemy_intel` - Grant intelligence on enemy army
- `improve_allied_cohesion` - Boost allied army cohesion
- `demoralize_enemy_lance` - Reduce enemy lance effectiveness

#### Step 5: Add Localization

```xml
<string id="ll_evt_your_event_id_title" text="Your Event Title" />
<string id="ll_evt_your_event_id_setup" text="Your event description with {PLACEHOLDERS}." />
<string id="ll_evt_your_event_id_opt_choice1_text" text="First choice text" />
<string id="ll_evt_your_event_id_opt_choice1_outcome" text="Result of first choice." />
```

### Example: Creating a New Intel Event

**Goal:** Event fires when player's scouting reveals enemy army has critically low morale.

**File:** `ModuleData/Enlisted/Events/events_ai_lance.json`

```json
{
  "id": "enemy_morale_collapse_intel",
  "category": "ai_lance_intel",
  "delivery": {
    "method": "automatic",
    "channel": "inquiry",
    "incident_trigger": null
  },
  "triggers": {
    "all": ["is_enlisted", "ai_safe", "enemy_army_scouted"],
    "any": [],
    "custom_conditions": ["enemy_morale_critical"]
  },
  "requirements": {
    "duty": "any",
    "formation": "any",
    "tier": { "min": 3, "max": 9 }
  },
  "timing": {
    "cooldown_days": 3,
    "priority": "high",
    "one_time": false
  },
  "content": {
    "titleId": "ll_evt_enemy_morale_collapse_title",
    "setupId": "ll_evt_enemy_morale_collapse_setup",
    "title": "Enemy Army on the Brink",
    "setup": "Your scouts report that {ENEMY_LORD}'s army is in dire straits. Morale is at {MORALE_STATUS} - barely holding together. Food is {FOOD_STATUS}. Several lances look ready to desert.\n\nThis could be the perfect time to strike.",
    "options": [
      {
        "id": "recommend_attack",
        "text": "Report to command: enemy is vulnerable",
        "tooltip": "Recommend immediate attack while they're weak",
        "risk": "safe",
        "rewards": {
          "xp": { "Tactics": 30, "Leadership": 20 }
        },
        "effects": {
          "relation": { "{LORD_NAME}": 3 }
        },
        "outcome": "Your lord listens intently. \"Excellent intelligence. We attack at dawn.\" This could be a decisive victory."
      },
      {
        "id": "exploit_desertion",
        "text": "Spread word to encourage desertion",
        "tooltip": "Psychological warfare - encourage enemy to abandon",
        "risk": "risky",
        "risk_chance": 40,
        "costs": { "gold": 200 },
        "rewards": {
          "xp": { "Roguery": 40 }
        },
        "effects_success": {
          "heat": 1
        },
        "custom_effects": {
          "trigger_enemy_desertion": "{ENEMY_LORD}"
        },
        "outcome": "Your agents spread rumors through enemy camps. By morning, two lances have deserted. The enemy army is in chaos.",
        "outcome_failure": "Your agents are caught. The enemy executes them as spies, and their army rallies in anger."
      },
      {
        "id": "just_observe",
        "text": "Continue observing, report later",
        "tooltip": "Gather more intelligence before acting",
        "risk": "safe",
        "rewards": {
          "xp": { "Scouting": 20 }
        },
        "outcome": "You file the intelligence away. Timing is everything in war - perhaps an opportunity will present itself."
      }
    ]
  }
}
```

**Localization:**

```xml
<string id="ll_evt_enemy_morale_collapse_title" text="Enemy Army on the Brink" />
<string id="ll_evt_enemy_morale_collapse_setup" text="Your scouts report that {ENEMY_LORD}'s army is in dire straits. Morale is at {MORALE_STATUS} - barely holding together. Food is {FOOD_STATUS}. Several lances look ready to desert.\n\nThis could be the perfect time to strike." />
```

### Adding Custom Conditions

To create new trigger conditions, add them to the event conditions class:

```csharp
// src/Features/Lances/Events/AILanceEventConditions.cs

public static class AILanceEventConditions
{
    /// <summary>
    /// Checks if any scouted enemy has critically low morale
    /// </summary>
    public static bool enemy_morale_critical()
    {
        var knownEnemies = GetScoutedEnemyArmies();
        foreach (var party in knownEnemies)
        {
            var company = AILordLanceSimulationBehavior.Instance?.GetCompany(party);
            if (company != null && company.Status.Morale < 30)
                return true;
        }
        return false;
    }
    
    // Add your custom condition here...
}
```

### Testing Your Event

1. **Enable debug logging:**
   ```csharp
   LogManager.EnableDebug("AILanceEvents");
   ```

2. **Force trigger conditions:**
   - Use console commands to modify AI army state
   - Manually trigger scouts
   - Set morale/food values for testing

3. **Verify event fires:**
   - Check logs for event evaluation
   - Confirm event appears in-game
   - Test all option outcomes

4. **Balance iteration:**
   - Adjust rewards/costs based on gameplay
   - Tune cooldowns and priorities
   - Refine risk chances

### Best Practices

**DO:**
- ✓ Use existing trigger tokens when possible
- ✓ Provide multiple meaningful choices
- ✓ Include safe and risky options
- ✓ Use localization strings (not hardcoded text)
- ✓ Test with different army sizes and states
- ✓ Consider cooldowns to prevent spam

**DON'T:**
- ✗ Create events that fire too frequently (use cooldowns)
- ✗ Make all options "optimal" (include hard choices)
- ✗ Hardcode lord/faction names (use placeholders)
- ✗ Skip localization (always use string IDs)
- ✗ Create events without "safe" option (player needs outs)
- ✗ Forget to test event chains (follow-ups should work)

---

## Code Quality & ReSharper Compliance

### JetBrains Annotations

The codebase uses JetBrains.Annotations for enhanced static analysis:

```csharp
// Install via NuGet: JetBrains.Annotations

using JetBrains.Annotations;

namespace Enlisted.Features.Lances.Services
{
    public class ExampleService
    {
        /// <summary>
        /// Processes a party and returns the result.
        /// </summary>
        /// <param name="party">The party to process (cannot be null).</param>
        /// <returns>The result string, or null if processing fails.</returns>
        [NotNull]
        public string ProcessParty([NotNull] MobileParty party)
        {
            // ReSharper knows party cannot be null
            // No null check warning needed
            return party.Name.ToString();
        }

        /// <summary>
        /// Tries to get a company for a party.
        /// </summary>
        /// <param name="party">The party to query (can be null).</param>
        /// <returns>The company, or null if not found.</returns>
        [CanBeNull]
        public AILanceCompany TryGetCompany([CanBeNull] MobileParty party)
        {
            // ReSharper knows return can be null
            // Callers will get warnings if they don't null-check
            return party == null ? null : GetCompany(party);
        }

        /// <summary>
        /// Method used only by serialization.
        /// </summary>
        [UsedImplicitly]
        private void OnDeserialized()
        {
            // ReSharper won't mark this as unused
            // Even though no explicit calls exist
        }

        /// <summary>
        /// Pure function with no side effects.
        /// </summary>
        [Pure]
        public int CalculateValue(int input)
        {
            // ReSharper can optimize calls to pure functions
            return input * 2;
        }
    }
}
```

**Common Annotations:**

| Annotation | Purpose | Usage |
|------------|---------|-------|
| `[NotNull]` | Parameter/return value cannot be null | Methods, parameters, properties |
| `[CanBeNull]` | Parameter/return value can be null | Methods, parameters, properties |
| `[UsedImplicitly]` | Member is used via reflection/serialization | Methods, fields, properties |
| `[Pure]` | Function has no side effects | Methods |
| `[PublicAPI]` | Member is part of public API | Classes, methods, properties |
| `[MustUseReturnValue]` | Caller must use return value | Methods |

### XML Documentation Standards

All public APIs must have XML documentation:

```csharp
/// <summary>
/// Calculates casualties for a party in a battle.
/// </summary>
/// <param name="party">The party that took casualties.</param>
/// <param name="mapEvent">The battle that occurred.</param>
/// <returns>The number of casualties, or 0 if none.</returns>
/// <exception cref="ArgumentNullException">
/// Thrown if <paramref name="party"/> or <paramref name="mapEvent"/> is null.
/// </exception>
/// <remarks>
/// This method uses the party's battle involvement data to calculate
/// accurate casualty counts, including both killed and wounded.
/// </remarks>
/// <example>
/// <code>
/// var casualties = CalculateCasualties(party, mapEvent);
/// if (casualties > 0)
/// {
///     ApplyCasualties(party, casualties);
/// }
/// </code>
/// </example>
private int CalculateCasualties(
    [NotNull] MobileParty party, 
    [NotNull] MapEvent mapEvent)
{
    if (party == null) throw new ArgumentNullException(nameof(party));
    if (mapEvent == null) throw new ArgumentNullException(nameof(mapEvent));
    
    // Implementation...
}
```

### ReSharper Inspection Compliance

**Code should pass all ReSharper inspections:**

1. **Naming Conventions:**
   ```csharp
   // Private fields: camelCase with underscore
   private readonly int _troopsPerLance;
   
   // Public properties: PascalCase
   public AILanceCompany Company { get; private set; }
   
   // Constants: PascalCase
   private const int DefaultLanceSize = 10;
   
   // Local variables: camelCase
   var lanceCount = 5;
   ```

2. **Null Checking:**
   ```csharp
   // Good - with annotation
   public void Process([NotNull] MobileParty party)
   {
       // No null check needed - annotation enforces non-null
       var name = party.Name;
   }
   
   // Good - without annotation
   public void Process(MobileParty party)
   {
       if (party == null) throw new ArgumentNullException(nameof(party));
       var name = party.Name;
   }
   
   // Good - can be null
   public void Process([CanBeNull] MobileParty party)
   {
       var name = party?.Name ?? "Unknown";
   }
   ```

3. **LINQ Best Practices:**
   ```csharp
   // Good - efficient
   var count = lances.Count(l => l.Members.Count > 5);
   
   // Bad - double enumeration
   var count = lances.Where(l => l.Members.Count > 5).Count();
   
   // Good - ToList only when needed
   var filtered = lances.Where(l => l.IsActive);
   foreach (var lance in filtered)
   {
       Process(lance);
   }
   ```

4. **String Formatting:**
   ```csharp
   // Good - interpolation
   var message = $"Lance {name} has {count} members";
   
   // Good - composite for localization
   var localized = string.Format(LocalizedText, name, count);
   
   // Bad - concatenation
   var message = "Lance " + name + " has " + count + " members";
   ```

5. **Exception Handling:**
   ```csharp
   // Good - specific exceptions
   try
   {
       ProcessLance(lance);
   }
   catch (InvalidOperationException ex)
   {
       LogManager.LogError($"Failed to process lance: {ex.Message}", ex);
   }
   
   // Bad - catching Exception
   try
   {
       ProcessLance(lance);
   }
   catch (Exception ex) // ReSharper warning
   {
       // Too broad
   }
   ```

### .editorconfig Settings

```ini
# .editorconfig

[*.cs]
# ReSharper inspection severities
resharper_check_namespace_highlighting = warning
resharper_unused_member_global_highlighting = suggestion
resharper_member_can_be_private_global_highlighting = suggestion

# Code style
csharp_style_var_for_built_in_types = true:suggestion
csharp_style_var_when_type_is_apparent = true:suggestion
csharp_style_var_elsewhere = true:suggestion

# Naming conventions
dotnet_naming_rule.private_fields_rule.severity = warning
dotnet_naming_rule.private_fields_rule.symbols = private_fields
dotnet_naming_rule.private_fields_rule.style = underscore_camelcase

dotnet_naming_symbols.private_fields.applicable_kinds = field
dotnet_naming_symbols.private_fields.applicable_accessibilities = private

dotnet_naming_style.underscore_camelcase.capitalization = camel_case
dotnet_naming_style.underscore_camelcase.required_prefix = _

# XML documentation
dotnet_diagnostic.CS1591.severity = warning # Missing XML comment
```

### Performance Considerations

**ReSharper Performance Analyzers:**

1. **Avoid Boxing:**
   ```csharp
   // Good
   var list = new List<int>();
   
   // Bad - boxing
   var list = new ArrayList(); // ReSharper warning
   ```

2. **Use Struct for Small Data:**
   ```csharp
   // Good - small immutable data
   [SaveableStruct(200)]
   public struct SimulatedLanceMember { }
   
   // Bad - would cause allocations
   public class SimulatedLanceMember { }
   ```

3. **Avoid Unnecessary Allocations:**
   ```csharp
   // Good - reuse StringBuilder
   private readonly StringBuilder _builder = new StringBuilder();
   
   public string BuildText()
   {
       _builder.Clear();
       _builder.AppendLine("Text");
       return _builder.ToString();
   }
   
   // Bad - new allocation every call
   public string BuildText()
   {
       var builder = new StringBuilder(); // ReSharper suggestion
       builder.AppendLine("Text");
       return builder.ToString();
   }
   ```

---

## Summary

The AI Lord Lance Simulation brings the same depth and structure to AI armies that the player experiences with their own lance. By deriving lance composition from real troop counts and using procedural generation for details, we create a rich, believable military world without excessive memory or CPU cost.

**Key Benefits:**

### Gameplay & Immersion
- ✅ **Realistic Military Structure:** Every army has organization and history
- ✅ **Intelligence Gameplay:** Scouting reveals valuable tactical information
- ✅ **Narrative Depth:** Battles have consequences for named lances and their members
- ✅ **Dynamic Events:** AI lance activities generate automatic events and dispatches
- ✅ **Meaningful Choices:** Player can interact with allied/enemy lance status

### Technical Excellence
- ✅ **Scalability:** Lightweight design works with dozens of simultaneous armies
- ✅ **Performance:** Lazy loading, efficient caching, minimal CPU overhead
- ✅ **Persistence:** Full save/load support with `SaveableClass`/`SaveableStruct`
- ✅ **ReSharper Compliant:** Full XML documentation, JetBrains annotations, code quality
- ✅ **Null Safety:** Comprehensive null checking with `[NotNull]`/`[CanBeNull]`

### Mod-Friendly Design
- ✅ **Extensible Interfaces:** `ILanceNameGenerator`, `IAILanceConfig` for customization
- ✅ **Event Hooks:** C# events (`OnLanceFormed`, `OnLanceDissolved`, etc.)
- ✅ **JSON Configuration:** External config files, schema validation
- ✅ **Dependency Injection:** Constructor injection for testing and extensibility
- ✅ **No Harmony Patches:** Uses Bannerlord's native event system only
- ✅ **Backward Compatible:** Preserves custom data, graceful degradation

### Integration Points
- ✅ **Event System:** Full integration with Lance Life Events schema
- ✅ **News Dispatches:** Automatic dispatch generation for significant events
- ✅ **Menu System:** Inspector menus for viewing AI army composition
- ✅ **Localization:** XML string tables, placeholder support
- ✅ **AI Camp Schedule:** Compatible with existing AI scheduling system

### Content Creator Support
- ✅ **Complete Event Schema:** Standard JSON event format
- ✅ **Trigger Tokens:** `ai_lance_battle_occurred`, `ai_lance_heavy_casualties`, etc.
- ✅ **Custom Conditions:** Extensible condition system for events
- ✅ **Authoring Guide:** Step-by-step guide with examples
- ✅ **Testing Tools:** Debug logging, validation, error handling

### Future Enhancements
This system lays the groundwork for:
- 🔮 **Lance Rivalries:** Track historical conflicts between specific lances
- 🔮 **Intelligence Networks:** Spy systems for detailed enemy intel
- 🔮 **Dynamic Army Management:** AI lords respond to lance status
- 🔮 **Elite Lance Progression:** Veteran lances gain bonuses over time
- 🔮 **Lance Transfer System:** Members promoted between lances
- 🔮 **Historical Battle Records:** Lance battle histories and statistics

All while maintaining:
- **Performance:** No frame drops, efficient memory use
- **Mod Compatibility:** No conflicts with other mods
- **Save Compatibility:** No save breaks, forward/backward compatible
- **Code Quality:** Maintainable, documented, tested

---

## References

### Related Documentation
- **Lance Life Simulation:** `docs/Features/Core/lance-life-simulation.md`
- **AI Camp Schedule:** `docs/Features/Core/ai-camp-schedule.md`
- **Lance Life Events:** `docs/Features/Core/lance-life-events.md`
- **News & Dispatches:** `docs/Features/UI/news-dispatches-implementation-plan.md`
- **Story Pack Contract:** `docs/Features/Core/story-pack-contract.md`
- **Systems Reference:** `docs/SYSTEMS_AND_STORY_BLOCKS_REFERENCE.md`

### External Resources
- **Bannerlord API Documentation:** [Mount & Blade II: Bannerlord API](https://apidoc.bannerlord.com/)
- **JetBrains Annotations:** [NuGet Package](https://www.nuget.org/packages/JetBrains.Annotations/)
- **JSON Schema Validation:** [json-schema.org](https://json-schema.org/)
- **ReSharper Code Quality:** [JetBrains ReSharper](https://www.jetbrains.com/resharper/)

### Development Tools
- **Visual Studio 2022** (with ReSharper or Rider)
- **JetBrains Rider** (recommended for Unity/game dev)
- **Newtonsoft.Json** (JSON serialization)
- **TaleWorlds.SaveSystem** (save/load support)

---

**Document Version:** 2.0  
**Last Updated:** December 13, 2025  
**Status:** Design Complete - Ready for Implementation  
**Compatibility:** Mount & Blade II: Bannerlord v1.2.0+
