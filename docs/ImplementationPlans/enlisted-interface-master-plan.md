# Enlisted Mod - Master Implementation Plan v2.0 (Mod Version v0.9.0)

**Status**: Phases 1-4 Complete - Ready for Phase 5
**Last Updated**: December 21, 2025
**Mod Version**: v0.9.0
**Target Game Version**: v1.3.12
**Current Phase**: Phase 5.6 - Strategic Context Enhancement
**Related Documents**: 
- `traits-identity-system.md` - Identity & trait integration details
- `macro-schedule-simplification.md` - Orders & UI simplification details

---

## 🚀 Quick Start for Next AI Session

**Current Status:** Phases 1-4 complete. Core systems (Menu, Traits, Orders) are implemented and functional. Ready for strategic enhancement.

**Your Task:** Phase 5.6 - Strategic Context Enhancement (High Value)

**What to Build:**
1. Enhance `ArmyContextAnalyzer` to understand strategic intent (not just tactical state)
2. Make Orders reflect lord's actual strategic plans from game state
3. Make Reports explain strategic WHY behind tactical WHAT
4. Predict company needs based on upcoming operations
5. Create strategic coherence across features

**Key Files to Enhance:**
- `src/Features/Context/ArmyContextAnalyzer.cs` - Add strategic intent detection
- `src/Features/Orders/OrderCatalog.cs` - Add strategic context filtering
- `src/Features/News/EnlistedNewsBehavior.cs` - Add strategic narratives
- `src/Features/Company/CompanyNeedsManager.cs` - Add predictive needs

**Existing Systems (Already Done):**
- ✅ Native Game Menu interface (EnlistedMenuBehavior)
- ✅ Trait-based identity system (EnlistedStatusManager, TraitHelper)
- ✅ Orders system with 39 orders across all tiers (T1-T9)
- ✅ Reputation system (Lord, Officer, Soldier)
- ✅ Company Needs system

**Build Command:**
```bash
dotnet build -c "Enlisted RETAIL" /p:Platform=x64
```

**See Phase 5.6 section below for strategic research findings and implementation guidance.**

---

## Vision Statement

Transform the Enlisted mod into an **emergent, choice-driven military experience** where:

- **Identity emerges** from choices, traits, and reputation (not prescribed systems)
- **Orders from chain of command** provide structure and meaningful missions
- **Native Game Menu** provides clean, fast, keyboard-friendly interface
- **Skills + Traits** determine success and unlock specialist paths
- **Company-wide consequences** make your actions impact the entire unit
- **Rank progression** (T1-T9) gates authority and responsibility
- **Reputation matters** with Lord, Officers, and Soldiers

This creates an experience where being an enlisted soldier feels **authentic, dynamic, and consequential**.

---

## Implementation Status

### ✅ Phase 1 Complete (December 20, 2025)

**What Was Done:**
- Expanded reputation system in `EscalationState.cs`:
  - Added `LordReputation` (0-100)
  - Added `OfficerReputation` (0-100)
  - Renamed `LanceReputation` → `SoldierReputation` (-50 to +50)
- Deleted Schedule system (4 time blocks, auto-scheduling)
- Deleted Duties system (passive duty assignments)
- Deleted Lance system (lance identity, roster, members)
- Deleted Custom Camp Management UI (multi-tab UI, ViewModels, XML)
- Preserved and renamed Company Needs system:
  - `LanceNeedsManager` → `CompanyNeedsManager`
  - `LanceNeedsState` → `CompanyNeedsState`
  - `LanceNeed` → `CompanyNeed`
  - Moved to `Features/Company/`
- Fixed all build errors and warnings from deletions
- Updated all comments to be factual and professional

### ✅ Phase 2 Complete (December 21, 2025)

**What Was Done:**
- Implemented Native Game Menu interface in `EnlistedMenuBehavior.cs`:
  - Main "enlisted_status" wait menu with real-time updates
  - Camp hub submenu with Rest, Train, Morale, Quartermaster, Medical, Companions, Service Records, Retinue
  - Reports submenu integration with `EnlistedNewsBehavior`
  - Status detail submenu with rank, reputation, traits display
  - Orders submenu integration with `OrderManager`
  - Decisions submenu for pending choices
  - Full navigation with back buttons and menu transitions
- Text-based display (no custom UI/ViewModels/XML)
- Accordion-style navigation
- Integration with existing systems (Quartermaster, Medical, News, Company Needs)

### ✅ Phase 3 Complete (December 21, 2025)

**What Was Done:**
- Implemented trait-based identity system:
  - `EnlistedStatusManager.cs` - Role detection based on traits
    - Roles: Officer, Scout, Medic, Engineer, Operative, NCO, Soldier
    - Priority hierarchy: Commander > Specialists > NCO > Default
  - `TraitHelper.cs` - Trait utilities for XP awards and level checks
  - Role-based requirements and filtering
  - Trait XP rewards from events/orders

**Build Status:** ✅ 0 Errors, 0 Warnings

### ✅ Phase 4 Complete (December 21, 2025)

**What Was Done:**
- ✅ OrderManager code (order issuing, tracking, expiration)
- ✅ OrderCatalog code (JSON loading and filtering)
- ✅ Order models (Order, OrderRequirements, OrderConsequences, OrderOutcome)
- ✅ Menu integration (Orders submenu in EnlistedMenuBehavior)
- ✅ Created order JSON files:
  - `orders_t1_t3.json` - 12 group soldier orders (T1-T3: camp duties, patrols, basic tasks)
  - `orders_t4_t6.json` - 15 solo/NCO orders (T4-T6: scouting, leadership, specialized missions)
  - `orders_t7_t9.json` - 12 tactical/strategic orders (T7-T9: command, siege, campaign planning)
  - Total: 39 orders with tier-appropriate requirements, balanced rewards, and Bannerlord RP style

### Phase 5.6: Strategic Context Enhancement (High Value)

**What Needs to Happen:**
- Apply AI strategic behavior research to enhance existing features
- Make ArmyContextAnalyzer understand strategic intent (not just tactical state)
- Make Orders reflect lord's actual strategic plans
- Make Reports explain strategic WHY behind tactical WHAT
- Predict company needs based on upcoming operations
- **Impact**: Same features, but strategically coherent and immersive

---

## The Three Pillars

### Pillar 1: Emergent Identity
**Replace prescribed systems (Lance, Duties, Formation) with emergent identity:**

- **Native Traits** define specialization (Scout, Surgeon, Commander, etc.)
- **Skills** represent capability (Scouting, Medicine, Leadership, etc.)
- **Reputation** tracks relationships (Lord, Officers, Soldiers)
- **Rank** gates authority (T1 group tasks → T9 strategic command)
- **Choices** shape identity through event outcomes

**Result**: Identity emerges from how you play, not what system you pick.

### Pillar 2: Orders from Chain of Command
**Replace passive duties with explicit directives:**

- **Chain of Command** gives orders (Sergeant/Lieutenant/Captain/Lord)
- **Accept/Decline** choice with reputation consequences
- **Rank-compliant** scaling (T1-T9 progression)
- **Trait-gated** requirements (need Scout trait for scout orders)
- **Company impact** on Readiness, Morale, Supplies, Equipment, Rest
- **Success/Failure** branches with different consequences

**Result**: Clear missions with meaningful stakes.

### Pillar 3: Native Game Menu Interface
**Replace custom UI with proven native systems:**

- **Native Game Menu** (text-based, accordion-style)
- **Zero custom UI** (no ViewModels, no XML)
- **Fast navigation** (keyboard-friendly, instant transitions)
- **Simple structure** (Camp, Reports, Decisions, Status)
- **Easy maintenance** (pure code, no UI debugging)

**Result**: Simpler, faster, more maintainable.

---

## System Architecture

### New System Overview

```
┌─────────────────────────────────────────────────────┐
│              ENLISTED STATUS (Main Menu)            │
│  • Rank, Lord, Campaign Context                     │
│  • Active Orders (from chain of command)            │
│  • Brief Reports Summary                            │
│  • Quick Links: Camp, Reports, Decisions, Status    │
└─────────────────────────────────────────────────────┘
                         │
        ┌────────────────┼────────────────┐
        │                │                │
        ▼                ▼                ▼
┌──────────────┐  ┌──────────────┐  ┌──────────────┐
│   ORDERS     │  │    TRAITS    │  │  REPUTATION  │
│              │  │              │  │              │
│ • Selection  │  │ • Detection  │  │ • Lord       │
│ • Execution  │  │ • Progression│  │ • Officers   │
│ • Tracking   │  │ • Role       │  │ • Soldiers   │
└──────────────┘  └──────────────┘  └──────────────┘
        │                │                │
        └────────────────┼────────────────┘
                         ▼
        ┌─────────────────────────────────┐
        │      COMPANY NEEDS              │
        │  • Readiness  • Morale          │
        │  • Equipment  • Rest            │
        │  • Supplies                     │
        └─────────────────────────────────┘
                         │
                         ▼
        ┌─────────────────────────────────┐
        │         EVENTS                  │
        │  • Role + Context Based         │
        │  • Skill/Trait Checks           │
        │  • Chains & Follow-ups          │
        └─────────────────────────────────┘
```

### What's Being Deleted

❌ **Schedule System** (-1700 lines, but keep 3 files)
- 4 time blocks per day
- Auto-assigned activities
- Schedule generation and execution
- Schedule-based event triggers
- **KEEP:** CompanyNeedsManager, CompanyNeedsState, ArmyStateAnalyzer (move/rename to Features/Company & Features/Context)

❌ **Duties System** (-800 lines)
- Passive duty assignments
- Formation-based duties
- Duty progression
- Duty event mappings

❌ **Lance System** (-1200 lines)
- Lance identity and names
- Lance roster and members
- Lance leader interactions
- Lance reputation (replaced by SoldierReputation)

❌ **Custom Camp Management UI** (-1800 lines)
- Multi-tab custom UI
- ViewModels (CampManagementVM, etc.)
- XML UI files
- All custom UI components

**Total Deleted: -5500 lines (preserving CompanyNeedsManager/State for company need tracking)**

### What's Being Added

✨ **Orders System** (+300 lines)
- Order selection and tracking
- Accept/decline flow
- Rank/trait-compliant filtering
- Company need effects

✨ **Expanded Reputation** (+50 lines)
- LordReputation (0-100)
- OfficerReputation (0-100)
- SoldierReputation (-50 to +50, renamed from LanceReputation)

✨ **Native Game Menu Integration** (+300 lines)
- Menu definitions (Camp, Reports, Decisions, Status)
- Text generators
- Navigation logic

✨ **Trait Integration Utilities** (+150 lines)
- Role detection (Scout, Officer, Medic, etc.)
- Trait wrappers
- Event requirement/effect handlers

✨ **Role-Based Event Routing** (+200 lines)
- Event tagging (role, context)
- Event pool selection
- Trigger logic

✨ **Order Catalog** (+1500 lines JSON)
- 30-40 order definitions
- T1-T9 scaling
- Success/failure branches

✨ **News Integration** (+300 lines)
- Order outcome tracking
- Reputation change reporting
- Company need change reporting
- Enhanced daily brief generation

**Total Added: +2800 lines**

**Net Change: -2700 lines (60% reduction)**

---

## Implementation Roadmap

### Phase 0: Pre-Implementation Audit
**Duration**: 1 hour
**Goal**: Verify existing code structure and document current state before making changes

#### Tasks

**0.1: Audit Schedule System**
```bash
# List all files that will be deleted
ls -R src/Features/Schedule/

# Expected structure:
# src/Features/Schedule/
#   Core/
#     LanceNeedsManager.cs (MOVE to src/Features/Company/CompanyNeedsManager.cs)
#     ArmyStateAnalyzer.cs (MOVE to src/Features/Context/ArmyContextAnalyzer.cs)
#     ScheduleBehavior.cs (DELETE)
#     ActivityScheduler.cs (DELETE)
#   Models/
#     LanceNeedsState.cs (MOVE to src/Features/Company/CompanyNeedsState.cs)
#     LanceNeed.cs (MOVE to src/Features/Company/CompanyNeed.cs)
#     ScheduleActivity.cs (DELETE)
#   UI/
#     (all DELETE)
```

**0.2: Audit Duties System**
```bash
# List all files that will be deleted
ls -R src/Features/Assignments/

# Note: DutyConfiguration.cs, DutyManager.cs, etc. will all be deleted
```

**0.3: Audit Lance System**
```bash
# List all files that will be deleted
ls -R src/Features/Lances/

# Note: LanceManager.cs, LanceMember.cs, LanceRoster.cs, etc. will all be deleted
```

**0.4: Verify Escalation System**
```bash
# Read current EscalationState structure
cat src/Features/Escalation/EscalationState.cs

# Verify it has:
# - Heat (int)
# - Discipline (int)
# - LanceReputation (int) - will rename to SoldierReputation
# - MedicalRisk (int)

# Confirm EscalationManager location
cat src/Features/Escalation/EscalationManager.cs
```

**0.5: Verify Enlistment System**
```bash
# Verify EnlistmentBehavior API
grep -n "class EnlistmentBehavior" src/Features/Enlistment/

# Confirm it has:
# - CurrentLord (Hero)
# - EnlistmentTier (int)
# - CurrentRankTitle (string)
```

**0.6: Verify News System**
```bash
# Verify EnlistedNewsBehavior exists
cat src/Features/Interface/Behaviors/EnlistedNewsBehavior.cs

# Verify DailyReportGenerator exists
cat src/Features/Interface/News/Generation/DailyReportGenerator.cs
```

**0.7: Verify Company Needs Manager**
```bash
# Locate current LanceNeedsManager (will be renamed)
cat src/Features/Schedule/Core/LanceNeedsManager.cs

# Verify access pattern via ScheduleBehavior
grep -n "LanceNeeds" src/Features/Schedule/Core/ScheduleBehavior.cs
```

**0.8: Count Event Files**
```bash
# List all event JSON files that need migration
find src/Features/Events -name "*.json" | wc -l

# Expected: 80+ events across:
# - Schedule-based events
# - Duty-based events
# - Lance-based events
# - Generic events
```

**0.9: Verify Rank System**
```bash
# Confirm RankHelper exists and has GetRankTitle method
grep -n "GetRankTitle" src/Features/Ranks/RankHelper.cs

# Verify it supports culture-specific ranks
```

**0.10: Document Current State**
Create a snapshot document listing:
- Total line count of files to be deleted
- Current save/load structure for EscalationState
- Current dependencies between Schedule/Duties/Lance systems
- Any active warnings/errors in the codebase

#### Deliverables
- [ ] Complete file structure audit
- [ ] Verified all systems to be modified exist
- [ ] Documented any discrepancies from plan
- [ ] Created baseline metrics (line counts, file counts)
- [ ] Identified any missing dependencies
- [ ] Reviewed schema documentation (`event-system-schemas.md`)

#### Acceptance Criteria
- All referenced files/classes exist
- No major architectural surprises discovered
- Ready to proceed with Phase 1 deletions

---

### Phase 1: Foundation - Delete & Expand
**Duration**: Week 1-2 (~20 hours)
**Goal**: Remove old systems, expand reputation tracking

#### Tasks

**1.1: Expand Reputation System**
```csharp
// File: src/Features/Escalation/EscalationState.cs

public sealed class EscalationState
{
    // Existing (keep)
    public int Heat { get; set; }             // 0-10
    public int Discipline { get; set; }       // 0-10
    public int MedicalRisk { get; set; }      // 0-5
    
    // RENAMED
    public int SoldierReputation { get; set; } // -50 to +50 (was LanceReputation)
    
    // NEW
    public int LordReputation { get; set; }    // 0 to 100
    public int OfficerReputation { get; set; } // 0 to 100
    
    // Constants
    public const int LordReputationMin = 0;
    public const int LordReputationMax = 100;
    public const int OfficerReputationMin = 0;
    public const int OfficerReputationMax = 100;
    public const int SoldierReputationMin = -50;
    public const int SoldierReputationMax = 50;
}
```

**1.2: Delete Schedule System & Rename LanceNeeds**
```bash
# FIRST: Move files we want to keep (before deleting Schedule folder)
mkdir -p src/Features/Context
mkdir -p src/Features/Company

mv src/Features/Schedule/Core/ArmyStateAnalyzer.cs src/Features/Context/ArmyContextAnalyzer.cs
mv src/Features/Schedule/Core/LanceNeedsManager.cs src/Features/Company/CompanyNeedsManager.cs
mv src/Features/Schedule/Models/LanceNeedsState.cs src/Features/Company/CompanyNeedsState.cs
mv src/Features/Schedule/Models/LanceNeed.cs src/Features/Company/CompanyNeed.cs

# Update namespaces in moved files
sed -i 's/Features.Schedule.Core/Features.Context/g' src/Features/Context/ArmyContextAnalyzer.cs
sed -i 's/Features.Schedule.Core/Features.Company/g' src/Features/Company/CompanyNeedsManager.cs
sed -i 's/Features.Schedule.Models/Features.Company/g' src/Features/Company/CompanyNeedsState.cs
sed -i 's/Features.Schedule.Models/Features.Company/g' src/Features/Company/CompanyNeed.cs

# Rename classes and enums (find/replace in each file)
# CompanyNeedsManager.cs: LanceNeedsManager → CompanyNeedsManager
# CompanyNeedsState.cs: LanceNeedsState → CompanyNeedsState  
# CompanyNeed.cs: LanceNeed → CompanyNeed

# Update references in ScheduleBehavior.cs
sed -i 's/LanceNeedsState/CompanyNeedsState/g' src/Features/Schedule/Behaviors/ScheduleBehavior.cs
sed -i 's/LanceNeedsManager/CompanyNeedsManager/g' src/Features/Schedule/Behaviors/ScheduleBehavior.cs
sed -i 's/LanceNeed\./CompanyNeed./g' src/Features/Schedule/Behaviors/ScheduleBehavior.cs

# Update references in ScheduleConfigLoader.cs
sed -i 's/LanceNeed/CompanyNeed/g' src/Features/Schedule/Config/ScheduleConfigLoader.cs

# THEN: Delete entire Schedule system
rm -rf src/Features/Schedule/
```

**Semantic Clarity:** Lance system is deleted, so "LanceNeeds" would be confusing. Renamed to `CompanyNeed` to reflect that these are the enlisted lord's company/party needs (Readiness, Equipment, Morale, Rest, Supplies).

**1.3: Delete Duties System**
```bash
# Delete entire Duties system
rm -rf src/Features/Assignments/Behaviors/EnlistedDutiesBehavior.cs
rm -rf src/Features/Assignments/Models/DutyDefinition.cs
rm -rf src/Features/Assignments/Core/DutyAssignmentLogic.cs
rm -rf ModuleData/Enlisted/Duties/
```

**1.4: Delete Lance System**
```bash
# Delete entire Lance system
rm -rf src/Features/Lances/Models/LanceState.cs
rm -rf src/Features/Lances/Models/LanceMemberState.cs
rm -rf src/Features/Lances/Behaviors/LanceAssignmentBehavior.cs
rm -rf src/Features/Lances/Simulation/LanceLifeSimulationBehavior.cs
rm -rf ModuleData/Enlisted/Lances/
```

**1.5: Delete Custom Camp Management UI**
```bash
# Delete all custom UI
rm -rf src/Features/Camp/UI/Management/
rm -rf GUI/Prefabs/Camp/
```

**1.6: Save Migration**
```csharp
// File: src/Features/Escalation/EscalationManager.cs

public override void SyncData(IDataStore dataStore)
{
    if (dataStore.IsLoading)
    {
        // Load existing fields
        dataStore.SyncData("heat", ref _state.Heat);
        dataStore.SyncData("discipline", ref _state.Discipline);
        dataStore.SyncData("medicalRisk", ref _state.MedicalRisk);
        
        // Migrate LanceReputation → SoldierReputation
        int oldLanceRep = 0;
        dataStore.SyncData("lanceReputation", ref oldLanceRep);
        _state.SoldierReputation = oldLanceRep;
        
        // Initialize new reputation fields
        _state.LordReputation = 50;     // Start neutral
        _state.OfficerReputation = 50;  // Start neutral
        
        // Discard obsolete schedule/duty/lance data
        DiscardObsoleteData(dataStore);
        
        ModLogger.Info("Migration", "Migrated save to new reputation system");
    }
    
    if (dataStore.IsSaving)
    {
        // Save new reputation system
        dataStore.SyncData("heat", ref _state.Heat);
        dataStore.SyncData("discipline", ref _state.Discipline);
        dataStore.SyncData("medicalRisk", ref _state.MedicalRisk);
        dataStore.SyncData("soldierReputation", ref _state.SoldierReputation);
        dataStore.SyncData("lordReputation", ref _state.LordReputation);
        dataStore.SyncData("officerReputation", ref _state.OfficerReputation);
    }
}
```

#### Deliverables
- [x] Expanded reputation system (LordReputation, OfficerReputation, SoldierReputation)
- [x] Clean codebase (-5500 lines deleted)
- [x] Save compatibility maintained (migration logic)
- [x] CompanyNeedsManager/State renamed and moved to Features/Company (needed for equipment gating, orders)
- [x] ArmyContextAnalyzer preserved and moved to Features/Context (needed for context detection)
- [x] All build errors fixed (72 errors → 0 errors)
- [x] All build warnings fixed (6 warnings → 0 warnings)
- [x] All comments updated to factual, professional style

#### Testing
- [x] Build succeeds with no errors or warnings
- [ ] Load old save → migration applies correctly (needs runtime testing)
- [ ] New save → all reputation fields persist (needs runtime testing)
- [ ] No crashes from missing schedule/duty/lance data (needs runtime testing)

#### Phase 1 Status: ✅ COMPLETE
Ready to proceed to Phase 2.

---

### Phase 2: Native Game Menu
**Duration**: Week 2-3 (~15 hours)
**Goal**: Replace custom UI with native Game Menu
**Status**: ⏳ READY TO START

#### Phase 2 Quick Start Guide

**Context:** Phase 1 deleted the old Schedule, Duties, Lance systems and Custom Camp UI. Phase 2 replaces the custom UI with a clean native Game Menu interface.

**What You Need to Know:**
1. The custom Camp Management UI is gone (`src/Features/Camp/UI/Management/` was deleted)
2. Custom event screen (`LanceLifeEventScreen`) is gone
3. You'll be creating native Game Menus using `CampaignGameStarter.AddGameMenu()` and `AddGameMenuOption()`
4. The main entry point is `EnlistedMenuBehavior.cs` in `src/Features/Interface/Behaviors/`
5. Menu text generators are already partially in place, you'll enhance and complete them

**Build Requirements:**
- Solution: `Enlisted.csproj`
- Configuration: `Enlisted RETAIL`
- Platform: `x64`
- Build Command: `dotnet build -c "Enlisted RETAIL" /p:Platform=x64`

**Key Files to Work With:**
- `src/Features/Interface/Behaviors/EnlistedMenuBehavior.cs` - Main menu behavior (already exists, needs updates)
- `src/Features/Camp/CampMenuHandler.cs` - Camp menu logic (already exists, needs native menu integration)
- `src/Features/Interface/Behaviors/EnlistedNewsBehavior.cs` - News/reports system (already integrated)
- `src/Features/Conditions/EnlistedMedicalMenuBehavior.cs` - Medical menu (already uses native menus)
- `src/Features/Equipment/Behaviors/QuartermasterManager.cs` - Equipment menu (already uses native menus)

**What to Build:**
1. Main "Enlisted Status" menu (hub)
2. Camp submenu with activities (Rest, Train, Morale, Equipment)
3. Reports submenu (Daily Brief, Company Status, Campaign Context)
4. Decisions submenu placeholder (will connect to Orders in Phase 4)
5. Navigation logic and text generators

#### Tasks

**2.1: Create Main Menu Structure**
```csharp
// File: src/Features/Interface/Behaviors/EnlistedMenuBehavior.cs

public void AddEnlistedGameMenus(CampaignGameStarter campaignGameStarter)
{
    // Main Enlisted Menu (entry point)
    campaignGameStarter.AddGameMenu(
        "enlisted_status",
        GetEnlistedStatusText,
        OnEnlistedStatusInit);
    
    // Camp submenu
    campaignGameStarter.AddGameMenuOption(
        "enlisted_status",
        "enlisted_camp",
        "Camp",
        args => 
        {
            args.optionLeaveType = GameMenuOption.LeaveType.Submenu;
            return true;
        },
        args => GameMenu.SwitchToMenu("enlisted_camp"));
    
    campaignGameStarter.AddGameMenu(
        "enlisted_camp",
        "What would you like to do?",
        null);
    
    campaignGameStarter.AddGameMenuOption(
        "enlisted_camp",
        "camp_rest",
        "Rest & Recover",
        args => true,
        args => OnRestSelected());
    
    campaignGameStarter.AddGameMenuOption(
        "enlisted_camp",
        "camp_train",
        "Train Skills",
        args => true,
        args => OnTrainSelected());
    
    // Reports submenu
    campaignGameStarter.AddGameMenuOption(
        "enlisted_status",
        "enlisted_reports",
        "Reports",
        args => 
        {
            args.optionLeaveType = GameMenuOption.LeaveType.Submenu;
            return true;
        },
        args => GameMenu.SwitchToMenu("enlisted_reports"));
    
    campaignGameStarter.AddGameMenu(
        "enlisted_reports",
        GetReportsText,
        null);
    
    // Decisions submenu (active events)
    campaignGameStarter.AddGameMenuOption(
        "enlisted_status",
        "enlisted_decisions",
        "Decisions",
        args => 
        {
            args.optionLeaveType = GameMenuOption.LeaveType.Submenu;
            args.IsEnabled = HasActiveDecisions();
            args.Tooltip = HasActiveDecisions() 
                ? TextObject.Empty 
                : new TextObject("No pending decisions");
            return true;
        },
        args => GameMenu.SwitchToMenu("enlisted_decisions"));
    
    // Status submenu
    campaignGameStarter.AddGameMenuOption(
        "enlisted_status",
        "enlisted_detail_status",
        "Status",
        args => 
        {
            args.optionLeaveType = GameMenuOption.LeaveType.Submenu;
            return true;
        },
        args => GameMenu.SwitchToMenu("enlisted_detail_status"));
    
    campaignGameStarter.AddGameMenu(
        "enlisted_detail_status",
        GetDetailedStatusText,
        null);
    
    // Talk to lord
    campaignGameStarter.AddGameMenuOption(
        "enlisted_status",
        "talk_to_lord",
        "Talk to my lord...",
        args => 
        {
            args.IsEnabled = IsLordAvailable();
            return true;
        },
        args => StartLordConversation());
    
    // Leave service
    campaignGameStarter.AddGameMenuOption(
        "enlisted_status",
        "leave_service",
        "Leave Service",
        args => 
        {
            args.optionLeaveType = GameMenuOption.LeaveType.Leave;
            return true;
        },
        args => ShowLeaveConfirmation());
}
```

**2.2: Create Text Generators**
```csharp
// Main menu text
private string GetEnlistedStatusText()
{
    var enlistment = EnlistmentBehavior.Instance;
    var escalation = EscalationManager.Instance;
    var orders = OrderManager.Instance;
    
    var sb = new StringBuilder();
    
    // Header
    sb.AppendLine($"Rank: {enlistment.CurrentRankTitle} (T{enlistment.EnlistmentTier})");
    sb.AppendLine($"Lord: {enlistment.CurrentLord?.Name}");
    sb.AppendLine($"Campaign: {GetCampaignContext()}");
    sb.AppendLine();
    
    // Active orders (if any)
    var activeOrders = orders?.GetActiveOrders() ?? new List<Order>();
    if (activeOrders.Count > 0)
    {
        sb.AppendLine("━━━ ORDERS ━━━");
        foreach (var order in activeOrders)
        {
            sb.AppendLine($"⚠️  {order.Title}");
            sb.AppendLine($"    \"{order.Description}\"");
            sb.AppendLine($"    - From: {order.Issuer}");
        }
        sb.AppendLine();
    }
    
    // Brief reports
    sb.AppendLine("━━━ REPORTS ━━━");
    sb.AppendLine(GetBriefReportSummary());
    sb.AppendLine();
    
    return sb.ToString();
}

// Detailed status text
private string GetDetailedStatusText()
{
    var enlistment = EnlistmentBehavior.Instance;
    var escalation = EscalationManager.Instance;
    var status = EnlistedStatusManager.Instance;
    
    return $@"Rank: {enlistment.CurrentRankTitle} (T{enlistment.EnlistmentTier})
Lord: {enlistment.CurrentLord?.Name}
Campaign: {status.GetCampaignContext()}

Your Role: {status.GetPrimaryRole()}
  {status.GetRoleDescription()}

Reputation:
  Lord:     {escalation.State.LordReputation}/100 ({GetReputationLevel(escalation.State.LordReputation)})
  Officers: {escalation.State.OfficerReputation}/100 ({GetReputationLevel(escalation.State.OfficerReputation)})
  Soldiers: {escalation.State.SoldierReputation}/100 ({GetReputationLevel(escalation.State.SoldierReputation)})

Personality: {GetPersonalityTraits()}
Specializations: {GetSpecializations()}";
}

private string GetReputationLevel(int value)
{
    // -100 to +100 scale
    if (value >= 80) return "Celebrated";
    if (value >= 60) return "Trusted";
    if (value >= 40) return "Respected";
    if (value >= 20) return "Promising";
    if (value >= 0) return "Neutral";
    if (value >= -20) return "Questionable";
    if (value >= -40) return "Disliked";
    if (value >= -60) return "Despised";
    if (value >= -80) return "Hated";
    return "Enemy";
}

// Reports text
private string GetReportsText()
{
    var service = ServiceRecordManager.Instance;
    var news = NewsManager.Instance;
    
    return $@"Service Record:
  Days Served: {service.CurrentTerm.DaysServed}
  Battles: {service.CurrentTerm.BattlesWon + service.CurrentTerm.BattlesLost}
  Victories: {service.CurrentTerm.BattlesWon}
  Kills: {service.CurrentTerm.TotalKills}
  
Recent Events:
  {GetRecentEventsText()}
  
Company Status:
  Readiness: {GetCompanyNeed("Readiness")}
  Morale: {GetCompanyNeed("Morale")}
  Supplies: {GetCompanyNeed("Supplies")}
  Equipment: {GetCompanyNeed("Equipment")}
  Rest: {GetCompanyNeed("Rest")}";
}
```

#### Deliverables
- [x] Native Game Menu interface (enlisted_status, submenus)
- [x] Text-based display (no custom UI)
- [x] Accordion-style navigation (Camp, Reports, Decisions, Status)
- [x] Camp activities menu (Rest, Train, Morale Boost, Equipment Check)
- [x] Reports menu integration with EnlistedNewsBehavior
- [x] Status menu showing rank, lord, reputation, traits
- [x] Integration with existing systems:
  - [x] QuartermasterManager (equipment/quartermaster access)
  - [x] EnlistedMedicalMenuBehavior (medical care access)
  - [x] EnlistedNewsBehavior (daily reports and news)
  - [x] CompanyNeedsManager (company status display)

#### Testing
- [x] Main menu displays correctly
- [x] All submenus accessible
- [x] Navigation works (back buttons, transitions)
- [x] Text wraps/formats properly
- [x] Keyboard shortcuts work
- [x] Integration with Quartermaster works
- [x] Integration with Medical menu works
- [x] Daily reports display correctly

#### Phase 2 Status: ✅ COMPLETE (December 21, 2025)
See Implementation Status section above for details.

---

### Phase 3: Trait Integration
**Duration**: Week 3-4 (~15 hours)
**Goal**: Integrate native trait system for identity

#### Tasks

**3.1: Create Trait Utilities**
```csharp
// File: src/Features/Identity/TraitHelper.cs

public static class TraitHelper
{
    // Get trait level (0-20 for hidden, -2 to +2 for personality)
    public static int GetTraitLevel(Hero hero, TraitObject trait)
    {
        return hero.GetTraitLevel(trait);
    }
    
    // Award trait XP
    public static void AwardTraitXP(Hero hero, TraitObject trait, int xp)
    {
        TraitLevelingHelper.AddTraitXp(hero, trait, xp);
    }
    
    // Get primary specialization
    public static string GetPrimarySpecialization(Hero hero)
    {
        var traits = new Dictionary<string, int>
        {
            ["Commander"] = GetTraitLevel(hero, DefaultTraits.Commander),
            ["Surgeon"] = GetTraitLevel(hero, DefaultTraits.Surgeon),
            ["Scout"] = GetTraitLevel(hero, DefaultTraits.ScoutSkills),
            ["Rogue"] = GetTraitLevel(hero, DefaultTraits.RogueSkills),
            ["Sergeant"] = GetTraitLevel(hero, DefaultTraits.SergeantCommandSkills),
            ["Engineer"] = GetTraitLevel(hero, DefaultTraits.EngineerSkills)
        };
        
        var highest = traits.OrderByDescending(x => x.Value).First();
        
        if (highest.Value >= 10) return highest.Key;
        return "Soldier"; // Default
    }
}
```

**3.2: Create Role Detection**
```csharp
// File: src/Features/Identity/EnlistedStatusManager.cs

public class EnlistedStatusManager : CampaignBehaviorBase
{
    public string GetPrimaryRole()
    {
        var hero = Hero.MainHero;
        
        // Priority order: Commander > Specialist > NCO > Default
        if (hero.GetTraitLevel(DefaultTraits.Commander) >= 10)
            return "Officer";
        if (hero.GetTraitLevel(DefaultTraits.ScoutSkills) >= 10)
            return "Scout";
        if (hero.GetTraitLevel(DefaultTraits.Surgeon) >= 10)
            return "Medic";
        if (hero.GetTraitLevel(DefaultTraits.EngineerSkills) >= 10)
            return "Engineer";
        if (hero.GetTraitLevel(DefaultTraits.RogueSkills) >= 10)
            return "Operative";
        if (hero.GetTraitLevel(DefaultTraits.SergeantCommandSkills) >= 8)
            return "NCO";
        
        return "Soldier"; // Default
    }
    
    public string GetRoleDescription()
    {
        var role = GetPrimaryRole();
        var hero = Hero.MainHero;
        
        return role switch
        {
            "Officer" => $"Commander {hero.GetTraitLevel(DefaultTraits.Commander)}, Leadership {hero.GetSkillValue(DefaultSkills.Leadership)}",
            "Scout" => $"Scout {hero.GetTraitLevel(DefaultTraits.ScoutSkills)}, Scouting {hero.GetSkillValue(DefaultSkills.Scouting)}",
            "Medic" => $"Surgeon {hero.GetTraitLevel(DefaultTraits.Surgeon)}, Medicine {hero.GetSkillValue(DefaultSkills.Medicine)}",
            "Engineer" => $"Engineer {hero.GetTraitLevel(DefaultTraits.EngineerSkills)}, Engineering {hero.GetSkillValue(DefaultSkills.Engineering)}",
            "Operative" => $"Rogue {hero.GetTraitLevel(DefaultTraits.RogueSkills)}, Roguery {hero.GetSkillValue(DefaultSkills.Roguery)}",
            "NCO" => $"Sergeant {hero.GetTraitLevel(DefaultTraits.SergeantCommandSkills)}, Leadership {hero.GetSkillValue(DefaultSkills.Leadership)}",
            _ => "Enlisted Soldier"
        };
    }
}
```

**3.3: Update Event Requirements**
```csharp
// File: src/Features/Events/EventRequirementChecker.cs

public bool MeetsTraitRequirement(EventDefinition eventDef)
{
    if (eventDef.Requirements?.TraitRequirements == null)
        return true;
    
    var hero = Hero.MainHero;
    
    foreach (var traitReq in eventDef.Requirements.TraitRequirements)
    {
        var trait = GetTraitByName(traitReq.TraitName);
        if (trait == null) continue;
        
        int level = hero.GetTraitLevel(trait);
        
        if (level < traitReq.MinLevel)
            return false;
        if (traitReq.MaxLevel.HasValue && level > traitReq.MaxLevel.Value)
            return false;
    }
    
    return true;
}
```

**3.4: Update Event Effects**
```csharp
// File: src/Features/Events/EventEffectApplier.cs

public void ApplyTraitXP(Dictionary<string, int> traitXP)
{
    if (traitXP == null) return;
    
    var hero = Hero.MainHero;
    
    foreach (var xp in traitXP)
    {
        var trait = GetTraitByName(xp.Key);
        if (trait == null)
        {
            ModLogger.Warn("Events", $"Unknown trait: {xp.Key}");
            continue;
        }
        
        TraitHelper.AwardTraitXP(hero, trait, xp.Value);
        
        ModLogger.Info("Events", 
            $"Awarded {xp.Value} XP to trait {xp.Key} (now {hero.GetTraitLevel(trait)})");
    }
}
```

#### Deliverables
- [x] Trait helper utilities
- [x] Role detection system
- [x] Event requirement support for traits
- [x] Event effect support for trait XP

#### Testing
- [ ] Role detection works correctly
- [ ] Trait requirements gate events
- [ ] Trait XP awarded correctly
- [ ] Trait levels increase appropriately

---

### Phase 4: Orders System
**Duration**: Week 4-6 (~25 hours)
**Goal**: Implement orders from chain of command

#### File Structure

Orders are a **separate system** from Events (not extending event models):

```
src/Features/Orders/ (NEW directory)
├── Behaviors/
│   └── OrderManager.cs (~300 lines)
├── Models/
│   ├── Order.cs
│   ├── OrderRequirements.cs
│   ├── OrderConsequences.cs
│   └── OrderOutcome.cs
└── OrderCatalog.cs (~150 lines)

src/Features/Events/ (existing, unchanged)
├── LanceLifeEventCatalog.cs
├── LanceLifeEventBehavior.cs
└── ... (organic events)

ModuleData/Enlisted/Orders/ (NEW directory)
├── orders_t1_t3.json
├── orders_t4_t6.json
└── orders_t7_t9.json
```

**Why separate?**
- Orders have different lifecycle (3-day frequency, accept/decline, expiration)
- Orders have issuer logic (chain of command)
- Orders track decline count (discharge risk)
- Cleaner separation of concerns

#### Tasks

**4.1: Create Order Models**
```csharp
// File: src/Features/Orders/Models/Order.cs

public class Order
{
    public string Id { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public string Issuer { get; set; } // "Sergeant", "Lieutenant", "Captain", "Lord"
    public CampaignTime IssuedTime { get; set; }
    public CampaignTime ExpirationTime { get; set; }
    public OrderRequirements Requirements { get; set; }
    public OrderConsequences Consequences { get; set; }
    public List<string> Tags { get; set; } // role, context tags
}

public class OrderRequirements
{
    public int TierMin { get; set; }
    public int TierMax { get; set; }
    public Dictionary<SkillObject, int> MinSkills { get; set; }
    public Dictionary<TraitObject, int> MinTraits { get; set; }
}

public class OrderConsequences
{
    public OrderOutcome Success { get; set; }
    public OrderOutcome Failure { get; set; }
    public OrderOutcome Decline { get; set; }
}

public class OrderOutcome
{
    public Dictionary<string, int> SkillXP { get; set; }
    public Dictionary<string, int> TraitXP { get; set; }
    public Dictionary<string, int> Reputation { get; set; } // "lord", "officer", "soldier"
    public Dictionary<string, int> CompanyNeeds { get; set; } // "readiness", "morale", etc.
    public Dictionary<string, int> Escalation { get; set; } // "heat", "discipline"
    public int Denars { get; set; }
    public int Renown { get; set; }
    public string Text { get; set; }
}
```

**4.2: Create Order Manager**
```csharp
// File: src/Features/Orders/Behaviors/OrderManager.cs

public class OrderManager : CampaignBehaviorBase
{
    // Singleton instance
    public static OrderManager Instance { get; private set; }
    
    private Order _currentOrder;
    private CampaignTime _lastOrderTime;
    private int _declineCount;
    
    public OrderManager()
    {
        Instance = this;
    }
    
    // Fire order every ~3 days (modified by context/rank)
    public override void RegisterEvents()
    {
        CampaignEvents.DailyTickEvent.AddListener(this, OnDailyTick);
    }
    
    private void OnDailyTick()
    {
        if (!EnlistmentBehavior.Instance.IsEnlisted) return;
        
        // Check if it's time for a new order
        if (_currentOrder == null && ShouldIssueOrder())
        {
            TryIssueOrder();
        }
    }
    
    private bool ShouldIssueOrder()
    {
        if (_lastOrderTime == null) return true;
        
        var daysSinceLastOrder = CampaignTime.Now.ToDays - _lastOrderTime.ToDays;
        var tier = EnlistmentBehavior.Instance.EnlistmentTier;
        var context = GetCampaignContext();
        
        // Base: 3 days
        int targetDays = 3;
        
        // Context modifiers
        if (context == "Siege" || context == "Battle") targetDays = 1; // High tempo
        else if (context == "War") targetDays = 2;
        else if (context == "Peace" || context == "Town") targetDays = 4;
        
        // Rank modifiers
        if (tier <= 3) targetDays = Math.Max(2, targetDays - 1); // More frequent for low ranks
        else if (tier >= 7) targetDays = Math.Min(5, targetDays + 1); // Less frequent for officers
        
        return daysSinceLastOrder >= targetDays;
    }
    
    private void TryIssueOrder()
    {
        var order = OrderCatalog.SelectOrder();
        if (order == null) return;
        
        // Determine issuer dynamically if "auto"
        if (order.Issuer == "auto")
        {
            int playerTier = EnlistmentBehavior.Instance.EnlistmentTier;
            order.Issuer = OrderCatalog.DetermineOrderIssuer(playerTier, order);
        }
        
        _currentOrder = order;
        _lastOrderTime = CampaignTime.Now;
        
        ModLogger.Info("Orders", $"Issued order: {order.Title} from {order.Issuer}");
        
        // Show notification
        ShowOrderNotification(order);
    }
    
    public void AcceptOrder()
    {
        if (_currentOrder == null) return;
        
        ExecuteOrder(_currentOrder);
        _currentOrder = null;
    }
    
    public void DeclineOrder()
    {
        if (_currentOrder == null) return;
        
        _declineCount++;
        
        // Apply decline consequences
        ApplyOrderOutcome(_currentOrder.Consequences.Decline);
        
        ModLogger.Info("Orders", $"Order declined ({_declineCount} total declines)");
        
        // Check for discharge
        if (_declineCount >= 5)
        {
            TriggerDischargeEvent();
        }
        
        _currentOrder = null;
    }
    
    private void ExecuteOrder(Order order)
    {
        // Determine success/failure
        bool success = EvaluateOrderSuccess(order);
        
        var outcome = success ? order.Consequences.Success : order.Consequences.Failure;
        ApplyOrderOutcome(outcome);
        
        ModLogger.Info("Orders", $"Order {(success ? "succeeded" : "failed")}: {order.Title}");
        
        // Show result
        ShowOrderResult(order, success, outcome);
    }
    
    private bool EvaluateOrderSuccess(Order order)
    {
        // Base success chance: 60%
        float successChance = 0.6f;
        
        var hero = Hero.MainHero;
        
        // Check skill requirements
        if (order.Requirements?.MinSkills != null)
        {
            foreach (var skillReq in order.Requirements.MinSkills)
            {
                int playerSkill = hero.GetSkillValue(skillReq.Key);
                int required = skillReq.Value;
                
                // +1% per skill point above requirement
                if (playerSkill >= required)
                {
                    successChance += (playerSkill - required) * 0.01f;
                }
                else
                {
                    // -2% per skill point below requirement
                    successChance -= (required - playerSkill) * 0.02f;
                }
            }
        }
        
        // Check trait requirements
        if (order.Requirements?.MinTraits != null)
        {
            foreach (var traitReq in order.Requirements.MinTraits)
            {
                int playerTrait = hero.GetTraitLevel(traitReq.Key);
                int required = traitReq.Value;
                
                // +2% per trait level above requirement
                if (playerTrait >= required)
                {
                    successChance += (playerTrait - required) * 0.02f;
                }
                else
                {
                    // -3% per trait level below requirement
                    successChance -= (required - playerTrait) * 0.03f;
                }
            }
        }
        
        // Clamp to 10-95%
        successChance = Math.Clamp(successChance, 0.1f, 0.95f);
        
        return MBRandom.RandomFloat < successChance;
    }
    
    private void ApplyOrderOutcome(OrderOutcome outcome)
    {
        if (outcome == null) return;
        
        // Apply skill XP
        if (outcome.SkillXP != null)
        {
            foreach (var xp in outcome.SkillXP)
            {
                var skill = GetSkillByName(xp.Key);
                if (skill != null)
                {
                    Hero.MainHero.AddSkillXp(skill, xp.Value);
                }
            }
        }
        
        // Apply trait XP
        if (outcome.TraitXP != null)
        {
            foreach (var xp in outcome.TraitXP)
            {
                var trait = GetTraitByName(xp.Key);
                if (trait != null)
                {
                    TraitHelper.AwardTraitXP(Hero.MainHero, trait, xp.Value);
                }
            }
        }
        
        // Apply reputation changes
        if (outcome.Reputation != null)
        {
            var escalation = EscalationManager.Instance;
            
            if (outcome.Reputation.ContainsKey("lord"))
                escalation.ModifyLordReputation(outcome.Reputation["lord"]);
            if (outcome.Reputation.ContainsKey("officer"))
                escalation.ModifyOfficerReputation(outcome.Reputation["officer"]);
            if (outcome.Reputation.ContainsKey("soldier"))
                escalation.ModifySoldierReputation(outcome.Reputation["soldier"]);
        }
        
        // Apply company need changes
        if (outcome.CompanyNeeds != null)
        {
            var needs = CompanyNeedsManager.Instance;
            
            foreach (var need in outcome.CompanyNeeds)
            {
                if (Enum.TryParse<CompanyNeed>(need.Key, true, out var needType))
                {
                    needs.ModifyNeed(needType, need.Value);
                }
            }
        }
        
        // Apply escalation changes
        if (outcome.Escalation != null)
        {
            var escalation = EscalationManager.Instance;
            
            if (outcome.Escalation.ContainsKey("heat"))
                escalation.ModifyHeat(outcome.Escalation["heat"]);
            if (outcome.Escalation.ContainsKey("discipline"))
                escalation.ModifyDiscipline(outcome.Escalation["discipline"]);
        }
        
        // Apply denars/renown
        if (outcome.Denars != 0)
            Hero.MainHero.ChangeHeroGold(outcome.Denars);
        if (outcome.Renown != 0)
            Hero.MainHero.AddRenown(outcome.Renown);
    }
    
    public List<Order> GetActiveOrders()
    {
        var orders = new List<Order>();
        if (_currentOrder != null)
        {
            orders.Add(_currentOrder);
        }
        return orders;
    }
}
```

**4.3: Create Order Catalog**
   ```csharp
// File: src/Features/Orders/OrderCatalog.cs

public static class OrderCatalog
{
    private static List<Order> _orders;
    
    public static void LoadOrders()
    {
        // Load from JSON
        var json = File.ReadAllText("ModuleData/Enlisted/Orders/orders.json");
        _orders = JsonConvert.DeserializeObject<List<Order>>(json);
        
        ModLogger.Info("Orders", $"Loaded {_orders.Count} orders");
    }
    
    public static Order SelectOrder()
    {
        var tier = EnlistmentBehavior.Instance.EnlistmentTier;
        var context = GetCampaignContext();
        var role = EnlistedStatusManager.Instance.GetPrimaryRole();
        
        // Filter by tier
        var eligibleOrders = _orders
            .Where(o => tier >= o.Requirements.TierMin && tier <= o.Requirements.TierMax)
            .ToList();
        
        // Filter by skill/trait requirements
        eligibleOrders = eligibleOrders
            .Where(o => MeetsSkillRequirements(o) && MeetsTraitRequirements(o))
            .ToList();
        
        if (eligibleOrders.Count == 0) return null;
        
        // Prefer orders matching role + context
        var contextOrders = eligibleOrders
            .Where(o => o.Tags.Contains(context.ToLower()))
            .ToList();
        
        var roleOrders = contextOrders
            .Where(o => o.Tags.Contains(role.ToLower()))
            .ToList();
        
        // Priority: Role + Context > Context > Role > Any Eligible
        if (roleOrders.Count > 0) return roleOrders.GetRandomElement();
        if (contextOrders.Count > 0) return contextOrders.GetRandomElement();
        if (eligibleOrders.Count > 0) return eligibleOrders.GetRandomElement();
        
        return null;
    }
    
    // Determine who issues the order based on rank
    // This is a static method in OrderCatalog, called by OrderManager
    public static string DetermineOrderIssuer(int playerTier, Order order)
    {
        var enlistment = EnlistmentBehavior.Instance;
        
        // Issuer is typically 2-3 tiers above player
        // T1-T2 → T4 NCO issues orders
        // T3-T4 → T6 Officer issues orders
        // T5-T6 → T8 Commander issues orders
        // T7+ → Lord issues orders (strategic/tactical)
        
        int issuerTier;
        if (playerTier <= 2) issuerTier = 4;      // NCO
        else if (playerTier <= 4) issuerTier = 6; // Officer
        else if (playerTier <= 6) issuerTier = 8; // Commander
        else issuerTier = 9;                       // Lord-level or direct from lord
        
        // High tier strategic orders come directly from lord
        var lord = enlistment.CurrentLord;
        if (playerTier >= 7 && lord != null && order.Tags.Contains("strategic"))
        {
            return lord.Name?.ToString() ?? "Your Lord";
        }
        
    // Get culture-specific rank title for issuer
    string culture = lord?.Culture?.StringId ?? "empire";
    string issuerRankTitle = RankHelper.GetRankTitle(issuerTier, culture);
    
    return issuerRankTitle;
}

// Note: RankHelper.GetRankTitle is already implemented in src/Features/Ranks/RankHelper.cs
// It reads from progression_config.json and returns culture-specific rank titles
// (e.g., "Centurion" for Empire T6, "Drengr" for Sturgia T6, etc.)
}
```

**4.4: Create Order JSON Definitions**

Create ~30-40 orders covering T1-T9, all role types, all contexts.

**Complete Order JSON Schema:**
```json
{
  "id": "t4_scout_position",
  "title": "Reconnaissance",
  "description": "Scout the ridge to the east. Report enemy strength and position.",
  "issuer": "auto",
  "_comment": "When 'auto', OrderManager calls DetermineOrderIssuer at runtime to set culture-specific rank",
  "requirements": {
    "tier_min": 4,
    "tier_max": 6,
    "min_skills": {
      "Scouting": 60,
      "Tactics": 40
    },
    "min_traits": {
      "ScoutSkills": 5
    }
  },
  "tags": ["scout", "war", "outdoor"],
  "consequences": {
    "success": {
      "skill_xp": {
        "Scouting": 80,
        "Tactics": 40
      },
      "trait_xp": {
        "ScoutSkills": 100
      },
      "reputation": {
        "lord": 5,
        "officer": 15,
        "soldier": 3
      },
      "company_needs": {
        "Readiness": 5,
        "Morale": 3
      },
      "escalation": {
        "heat": -1
      },
      "denars": 50,
      "text": "Excellent work. Your report helps the captain plan the assault."
    },
    "failure": {
      "reputation": {
        "officer": -15
      },
      "company_needs": {
        "Readiness": -10,
        "Morale": -5
      },
      "escalation": {
        "heat": 2
      },
      "text": "You were spotted. Had to retreat without intel. The captain is displeased."
    },
    "decline": {
      "reputation": {
        "officer": -20
      },
      "escalation": {
        "discipline": 2
      },
      "text": "The captain's jaw tightens. 'Find someone else who can handle it.'"
    }
  }
}
```

**Schema Field Reference:**

**Order Fields:**
- `id`: Unique order identifier
- `title`: Order name displayed to player
- `description`: Order objective description
- `issuer`: Set to `"auto"` to auto-determine from player tier (uses culture-specific ranks: Centurion, Drengr, Khan, etc.)

**Requirements:**
- `tier_min/tier_max`: Rank gates (1-9)
- `min_skills`: Dictionary of SkillName → MinValue (0-300 scale)
- `min_traits`: Dictionary of TraitName → MinValue (0-20 scale for hidden traits)

**Tags:**
- Role: `scout`, `officer`, `medic`, `operative`, `nco`, `soldier`
- Context: `war`, `siege`, `peace`, `town`, `outdoor`, `camp`
- Special: `strategic`, `tactical` (strategic orders from lord for T7+)

**Consequences (per outcome: success/failure/decline):**
- `skill_xp`: Dictionary of SkillName → XPAmount
- `trait_xp`: Dictionary of TraitName → XPAmount (use native trait names)
- `reputation`: Dictionary with keys: `lord`, `officer`, `soldier`
- `company_needs`: Dictionary with keys: `Readiness`, `Morale`, `Supplies`, `Equipment`, `Rest`
- `escalation`: Dictionary with keys: `heat`, `discipline`
- `denars`: Gold reward/penalty
- `renown`: Renown gain (for major successes)
- `text`: Outcome description text

**Issuer Auto-Determination:**
- T1-T2 player → T4 NCO rank (Principalis, Huscarl, etc.)
- T3-T4 player → T6 Officer rank (Centurion, Drengr, etc.)
- T5-T6 player → T8 Commander rank (Tribune, Jarl, etc.)
- T7+ player → Lord directly (for strategic orders) or senior commander

#### Deliverables
- [x] Order models and data structures
- [x] OrderManager behavior (selection, execution, tracking)
- [x] Order catalog loader
- [x] 30-40 order definitions (JSON)
- [x] Decline tracking (discharge at 5+)
- [x] Success/failure evaluation

#### Testing
- [ ] Orders issue every ~3 days (context/rank adjusted)
- [ ] Orders filter by tier/skill/trait requirements
- [ ] Accept/decline flow works
- [ ] Success/failure evaluated correctly
- [ ] Consequences apply correctly
- [ ] Decline tracking works (discharge at 5+)

---

### Phase 5: Company Need Integration
**Duration**: Week 6 (~10 hours)
**Goal**: Orders affect company-wide needs

#### Tasks

**5.1: Extend EscalationManager**
```csharp
// File: src/Features/Escalation/EscalationManager.cs

public void ModifyLordReputation(int delta)
{
    int oldValue = _state.LordReputation;
    _state.LordReputation = Math.Clamp(
        _state.LordReputation + delta, 
        EscalationState.LordReputationMin, 
        EscalationState.LordReputationMax);
    
    ModLogger.Info("Escalation", 
        $"LordReputation: {oldValue} -> {_state.LordReputation} ({delta:+#;-#;0})");
}

public void ModifyOfficerReputation(int delta)
{
    int oldValue = _state.OfficerReputation;
    _state.OfficerReputation = Math.Clamp(
        _state.OfficerReputation + delta, 
        EscalationState.OfficerReputationMin, 
        EscalationState.OfficerReputationMax);
    
    ModLogger.Info("Escalation", 
        $"OfficerReputation: {oldValue} -> {_state.OfficerReputation} ({delta:+#;-#;0})");
}

public void ModifySoldierReputation(int delta)
{
    int oldValue = _state.SoldierReputation;
    _state.SoldierReputation = Math.Clamp(
        _state.SoldierReputation + delta, 
        EscalationState.SoldierReputationMin, 
        EscalationState.SoldierReputationMax);
    
    ModLogger.Info("Escalation", 
        $"SoldierReputation: {oldValue} -> {_state.SoldierReputation} ({delta:+#;-#;0})");
}
```

**5.2: Access Company Needs System**

**Note:** `CompanyNeedsManager` is a **static class** (not an instance). Access via `ScheduleBehavior.Instance.CompanyNeeds` property.

   ```csharp
// File: src/Features/Orders/OrderEffectApplier.cs

public void ApplyCompanyNeedEffects(Dictionary<string, int> companyNeedChanges)
{
    if (companyNeedChanges == null) return;
    
    // Access CompanyNeedsState via ScheduleBehavior (moved to Features/Company in Phase 1)
    var needs = ScheduleBehavior.Instance?.CompanyNeeds;
    if (needs == null)
    {
        ModLogger.Warn("Orders", "Cannot apply company need effects: CompanyNeeds is null");
        return;
    }
    
    foreach (var change in companyNeedChanges)
    {
        if (Enum.TryParse<CompanyNeed>(change.Key, true, out var needType))
        {
            int oldValue = needs.GetNeedValue(needType);
            needs.SetNeed(needType, oldValue + change.Value);
            
            ModLogger.Info("Orders", 
                $"Order effect: {needType} {oldValue} -> {needs.GetNeedValue(needType)} ({change.Value:+#;-#;0})");
        }
    }
    
    // Check for critical thresholds using static CompanyNeedsManager methods
    var warnings = CompanyNeedsManager.CheckCriticalNeeds(needs);
    foreach (var warning in warnings)
    {
        InformationManager.DisplayMessage(new InformationMessage(
            warning.Value, 
            warning.Key == CompanyNeed.Supplies || warning.Key == CompanyNeed.Morale 
                ? Colors.Red 
                : Colors.Yellow));
    }
}

// Note: CompanyNeedsManager.CheckCriticalNeeds() is a static method
// that returns a dictionary of warnings which we display above
```

**5.3: Balance Order Impacts**

Adjust order consequence magnitudes to ensure balanced gameplay:

**T1-T3 Orders:** ±3 to ±8 company need points
**T4-T6 Orders:** ±5 to ±15 company need points
**T7-T9 Orders:** ±10 to ±30 company need points

Example balanced order:
```json
{
  "id": "t5_lead_patrol",
  "consequences": {
    "success": {
      "company_needs": {
        "Readiness": 8,
        "Morale": 10,
        "Rest": -5
      }
    },
    "failure": {
      "company_needs": {
        "Readiness": -15,
        "Morale": -15,
        "Equipment": -10,
        "Rest": -10
      }
    }
  }
}
```

#### Deliverables
- [x] Company need modification methods
- [x] Critical threshold warnings
- [x] Balanced order impacts (by rank)
- [x] Reputation modification methods

#### Testing
- [x] Orders affect company needs correctly
- [x] Magnitude scales with rank
- [x] Critical thresholds trigger warnings
- [x] Reputation changes apply correctly

---

### Phase 5.5: News System Enhancement
**Duration**: Week 6 (~5 hours)
**Goal**: Connect Orders system to news/reports for consequential feedback

#### Tasks

**5.5.1: Add Order Outcome Tracking**
```csharp
// File: src/Features/Orders/Behaviors/OrderManager.cs

private void ExecuteOrder(Order order)
{
    // Determine success/failure
    bool success = EvaluateOrderSuccess(order);
    
    var outcome = success ? order.Consequences.Success : order.Consequences.Failure;
    ApplyOrderOutcome(outcome);
    
    ModLogger.Info("Orders", $"Order {(success ? "succeeded" : "failed")}: {order.Title}");
    
    // NEW: Report to news system
    ReportOrderOutcome(order, success, outcome);
    
    // Show result
    ShowOrderResult(order, success, outcome);
}

private void ReportOrderOutcome(Order order, bool success, OrderOutcome outcome)
{
    if (EnlistedNewsBehavior.Instance == null) return;
    
    // Create brief summary for daily report
    string briefSummary = success 
        ? $"{order.Title} completed successfully"
        : $"{order.Title} - mission failed";
    
    // Create detailed summary for full report
    string detailedSummary = outcome.Text;
    
    // Add reputation context if significant
    if (outcome.Reputation != null)
    {
        var repEffects = new List<string>();
        if (outcome.Reputation.ContainsKey("lord") && Math.Abs(outcome.Reputation["lord"]) >= 10)
            repEffects.Add($"Lord reputation {outcome.Reputation["lord"]:+#;-#;0}");
        if (outcome.Reputation.ContainsKey("officer") && Math.Abs(outcome.Reputation["officer"]) >= 10)
            repEffects.Add($"Officer reputation {outcome.Reputation["officer"]:+#;-#;0}");
        
        if (repEffects.Count > 0)
            detailedSummary += $"\n({string.Join(", ", repEffects)})";
    }
    
    EnlistedNewsBehavior.Instance.AddOrderOutcome(
        orderTitle: order.Title,
        success: success,
        briefSummary: briefSummary,
        detailedSummary: detailedSummary,
        issuer: order.Issuer,
        dayNumber: (int)CampaignTime.Now.ToDays
    );
}

public void DeclineOrder()
{
    if (_currentOrder == null) return;
    
    _declineCount++;
    
    // Apply decline consequences
    ApplyOrderOutcome(_currentOrder.Consequences.Decline);
    
    // NEW: Report decline to news
    if (EnlistedNewsBehavior.Instance != null)
    {
        EnlistedNewsBehavior.Instance.AddOrderOutcome(
            orderTitle: _currentOrder.Title,
            success: false,
            briefSummary: $"Declined: {_currentOrder.Title}",
            detailedSummary: _currentOrder.Consequences.Decline.Text,
            issuer: _currentOrder.Issuer,
            dayNumber: (int)CampaignTime.Now.ToDays
        );
    }
    
    ModLogger.Info("Orders", $"Order declined ({_declineCount} total declines)");
    
    // Check for discharge
    if (_declineCount >= 5)
    {
        TriggerDischargeEvent();
    }
    
    _currentOrder = null;
}
```

**5.5.2: Add Reputation Change Reporting**
```csharp
// File: src/Features/Escalation/EscalationManager.cs

public void ModifyLordReputation(int delta)
{
    int oldValue = _state.LordReputation;
    _state.LordReputation = Math.Clamp(
        _state.LordReputation + delta, 
        EscalationState.LordReputationMin, 
        EscalationState.LordReputationMax);
    
    ModLogger.Info("Escalation", 
        $"LordReputation: {oldValue} -> {_state.LordReputation} ({delta:+#;-#;0})");
    
    // NEW: Report significant changes to news system
    if (Math.Abs(delta) >= 10 && EnlistedNewsBehavior.Instance != null)
    {
        string message = GetReputationChangeMessage("Lord", delta, _state.LordReputation);
        EnlistedNewsBehavior.Instance.AddReputationChange(
            target: "Lord",
            delta: delta,
            newValue: _state.LordReputation,
            message: message,
            dayNumber: (int)CampaignTime.Now.ToDays
        );
    }
}

public void ModifyOfficerReputation(int delta)
{
    int oldValue = _state.OfficerReputation;
    _state.OfficerReputation = Math.Clamp(
        _state.OfficerReputation + delta, 
        EscalationState.OfficerReputationMin, 
        EscalationState.OfficerReputationMax);
    
    ModLogger.Info("Escalation", 
        $"OfficerReputation: {oldValue} -> {_state.OfficerReputation} ({delta:+#;-#;0})");
    
    // NEW: Report significant changes
    if (Math.Abs(delta) >= 10 && EnlistedNewsBehavior.Instance != null)
    {
        string message = GetReputationChangeMessage("Officer", delta, _state.OfficerReputation);
        EnlistedNewsBehavior.Instance.AddReputationChange(
            target: "Officer",
            delta: delta,
            newValue: _state.OfficerReputation,
            message: message,
            dayNumber: (int)CampaignTime.Now.ToDays
        );
    }
}

public void ModifySoldierReputation(int delta)
{
    int oldValue = _state.SoldierReputation;
    _state.SoldierReputation = Math.Clamp(
        _state.SoldierReputation + delta, 
        EscalationState.SoldierReputationMin, 
        EscalationState.SoldierReputationMax);
    
    ModLogger.Info("Escalation", 
        $"SoldierReputation: {oldValue} -> {_state.SoldierReputation} ({delta:+#;-#;0})");
    
    // NEW: Report significant changes
    if (Math.Abs(delta) >= 10 && EnlistedNewsBehavior.Instance != null)
    {
        string message = GetReputationChangeMessage("Soldier", delta, _state.SoldierReputation);
        EnlistedNewsBehavior.Instance.AddReputationChange(
            target: "Soldier",
            delta: delta,
            newValue: _state.SoldierReputation,
            message: message,
            dayNumber: (int)CampaignTime.Now.ToDays
        );
    }
}

private string GetReputationChangeMessage(string target, int delta, int newValue)
{
    if (delta >= 20)
    {
        return target switch
        {
            "Lord" => "Your lord took special notice of your recent performance",
            "Officer" => "The captain publicly commended your work",
            "Soldier" => "The men respect you greatly after your recent actions",
            _ => $"{target} reputation significantly improved"
        };
    }
    else if (delta >= 10)
    {
        return target switch
        {
            "Lord" => "Your lord's confidence in you is growing",
            "Officer" => "You've impressed the officers with your recent work",
            "Soldier" => "Your standing with the men has improved",
            _ => $"{target} reputation improved"
        };
    }
    else if (delta <= -20)
    {
        return target switch
        {
            "Lord" => "You've seriously disappointed your lord",
            "Officer" => "The officers question your competence",
            "Soldier" => "The men have lost respect for you",
            _ => $"{target} reputation significantly damaged"
        };
    }
    else // delta <= -10
    {
        return target switch
        {
            "Lord" => "Your lord's confidence in you has declined",
            "Officer" => "The officers are disappointed in your recent performance",
            "Soldier" => "Your standing with the men has suffered",
            _ => $"{target} reputation declined"
        };
    }
}
```

**5.5.3: Add Company Need Change Reporting**
```csharp
// File: src/Features/Orders/OrderEffectApplier.cs

public void ApplyCompanyNeedEffects(Dictionary<string, int> companyNeedChanges)
{
    if (companyNeedChanges == null) return;
    
    var needs = ScheduleBehavior.Instance?.CompanyNeeds;
    if (needs == null)
    {
        ModLogger.Warn("Orders", "Cannot apply company need effects: CompanyNeeds is null");
        return;
    }
    
    foreach (var change in companyNeedChanges)
    {
        if (Enum.TryParse<CompanyNeed>(change.Key, true, out var needType))
        {
            int oldValue = needs.GetNeedValue(needType);
            needs.SetNeed(needType, oldValue + change.Value);
            
            ModLogger.Info("Orders", 
                $"Order effect: {needType} {oldValue} -> {needs.GetNeedValue(needType)} ({change.Value:+#;-#;0})");
            
            // NEW: Report significant changes to news system
            int newValue = needs.GetNeedValue(needType);
            if (ShouldReportNeedChange(needType, change.Value, oldValue, newValue))
            {
                string message = GetCompanyNeedChangeMessage(needType, change.Value, oldValue, newValue);
                EnlistedNewsBehavior.Instance?.AddCompanyNeedChange(
                    need: needType.ToString(),
                    delta: change.Value,
                    oldValue: oldValue,
                    newValue: newValue,
                    message: message,
                    dayNumber: (int)CampaignTime.Now.ToDays
                );
            }
        }
    }
    
    // Check for critical thresholds using static CompanyNeedsManager
    var warnings = CompanyNeedsManager.CheckCriticalNeeds(needs);
    foreach (var warning in warnings)
    {
        InformationManager.DisplayMessage(new InformationMessage(
            warning.Value,
            warning.Key == CompanyNeed.Supplies || warning.Key == CompanyNeed.Morale 
                ? Colors.Red 
                : Colors.Yellow));
    }
}

private bool ShouldReportNeedChange(CompanyNeed need, int delta, int oldValue, int newValue)
{
    // Report if change is significant (±10+)
    if (Math.Abs(delta) >= 10) return true;
    
    // Report if crossing critical threshold (30%)
    if (oldValue >= 30 && newValue < 30) return true;
    if (oldValue < 30 && newValue >= 30) return true;
    
    // Report if crossing excellent threshold (80%)
    if (oldValue < 80 && newValue >= 80) return true;
    
    return false;
}

private string GetCompanyNeedChangeMessage(CompanyNeed need, int delta, int oldValue, int newValue)
{
    // Critical thresholds
    if (newValue < 30)
    {
        return need switch
        {
            CompanyNeed.Supplies => "Supplies critically low - Equipment access restricted",
            CompanyNeed.Readiness => "Unit readiness critical - Combat effectiveness severely reduced",
            CompanyNeed.Morale => "Morale breaking - Risk of desertion",
            CompanyNeed.Rest => "Men exhausted - Need rest urgently",
            CompanyNeed.Equipment => "Equipment in poor condition - Combat capability compromised",
            _ => $"{need} is critically low"
        };
    }
    
    // Positive changes
    if (delta >= 15)
    {
        return need switch
        {
            CompanyNeed.Supplies => "Company well-supplied after resupply",
            CompanyNeed.Readiness => "Unit readiness greatly improved",
            CompanyNeed.Morale => "Morale lifted significantly",
            CompanyNeed.Rest => "Men well-rested and ready",
            CompanyNeed.Equipment => "Equipment in good condition",
            _ => $"{need} significantly improved"
        };
    }
    else if (delta >= 10)
    {
        return need switch
        {
            CompanyNeed.Supplies => "Supplies replenished",
            CompanyNeed.Readiness => "Unit readiness improving",
            CompanyNeed.Morale => "Morale improving",
            CompanyNeed.Rest => "Men recovering from fatigue",
            CompanyNeed.Equipment => "Equipment condition improved",
            _ => $"{need} improved"
        };
    }
    
    // Negative changes
    if (delta <= -15)
    {
        return need switch
        {
            CompanyNeed.Supplies => "Supplies depleted significantly",
            CompanyNeed.Readiness => "Unit readiness declining sharply",
            CompanyNeed.Morale => "Morale declining",
            CompanyNeed.Rest => "Men growing exhausted",
            CompanyNeed.Equipment => "Equipment wearing down",
            _ => $"{need} declined significantly"
        };
    }
    else if (delta <= -10)
    {
        return need switch
        {
            CompanyNeed.Supplies => "Supplies running low",
            CompanyNeed.Readiness => "Unit readiness declining",
            CompanyNeed.Morale => "Morale slipping",
            CompanyNeed.Rest => "Men growing tired",
            CompanyNeed.Equipment => "Equipment condition declining",
            _ => $"{need} declined"
        };
    }
    
    return $"{need} changed by {delta:+#;-#;0}";
}
```

**5.5.4: Extend News Behavior API**
```csharp
// File: src/Features/Interface/Behaviors/EnlistedNewsBehavior.cs

// Add these new methods to the existing EnlistedNewsBehavior class

/// <summary>
/// Track order outcomes for display in daily brief and detailed reports.
/// </summary>
private List<OrderOutcomeRecord> _orderOutcomes = new List<OrderOutcomeRecord>();

/// <summary>
/// Track reputation changes for display in reports.
/// </summary>
private List<ReputationChangeRecord> _reputationChanges = new List<ReputationChangeRecord>();

/// <summary>
/// Track company need changes for display in reports.
/// </summary>
private List<CompanyNeedChangeRecord> _companyNeedChanges = new List<CompanyNeedChangeRecord>();

public void AddOrderOutcome(string orderTitle, bool success, string briefSummary, 
    string detailedSummary, string issuer, int dayNumber)
{
    _orderOutcomes.Add(new OrderOutcomeRecord
    {
        OrderTitle = orderTitle,
        Success = success,
        BriefSummary = briefSummary,
        DetailedSummary = detailedSummary,
        Issuer = issuer,
        DayNumber = dayNumber
    });
    
    // Keep only last 10 order outcomes
    if (_orderOutcomes.Count > 10)
    {
        _orderOutcomes.RemoveAt(0);
    }
}

public void AddReputationChange(string target, int delta, int newValue, 
    string message, int dayNumber)
{
    _reputationChanges.Add(new ReputationChangeRecord
    {
        Target = target,
        Delta = delta,
        NewValue = newValue,
        Message = message,
        DayNumber = dayNumber
    });
    
    // Keep only last 10 reputation changes
    if (_reputationChanges.Count > 10)
    {
        _reputationChanges.RemoveAt(0);
    }
}

public void AddCompanyNeedChange(string need, int delta, int oldValue, int newValue, 
    string message, int dayNumber)
{
    _companyNeedChanges.Add(new CompanyNeedChangeRecord
    {
        Need = need,
        Delta = delta,
        OldValue = oldValue,
        NewValue = newValue,
        Message = message,
        DayNumber = dayNumber
    });
    
    // Keep only last 10 need changes
    if (_companyNeedChanges.Count > 10)
    {
        _companyNeedChanges.RemoveAt(0);
    }
}

public List<OrderOutcomeRecord> GetRecentOrderOutcomes(int maxDaysOld = 3)
{
    int currentDay = (int)CampaignTime.Now.ToDays;
    return _orderOutcomes
        .Where(o => currentDay - o.DayNumber <= maxDaysOld)
        .OrderByDescending(o => o.DayNumber)
        .ToList();
}

public List<ReputationChangeRecord> GetRecentReputationChanges(int maxDaysOld = 3)
{
    int currentDay = (int)CampaignTime.Now.ToDays;
    return _reputationChanges
        .Where(r => currentDay - r.DayNumber <= maxDaysOld)
        .OrderByDescending(r => r.DayNumber)
        .ToList();
}

public List<CompanyNeedChangeRecord> GetRecentCompanyNeedChanges(int maxDaysOld = 3)
{
    int currentDay = (int)CampaignTime.Now.ToDays;
    return _companyNeedChanges
        .Where(c => currentDay - c.DayNumber <= maxDaysOld)
        .OrderByDescending(c => c.DayNumber)
        .ToList();
}

// Add these record types to EnlistedNewsBehavior or a separate models file
public sealed class OrderOutcomeRecord
{
    public string OrderTitle { get; set; }
    public bool Success { get; set; }
    public string BriefSummary { get; set; }
    public string DetailedSummary { get; set; }
    public string Issuer { get; set; }
    public int DayNumber { get; set; }
}

public sealed class ReputationChangeRecord
{
    public string Target { get; set; } // "Lord", "Officer", "Soldier"
    public int Delta { get; set; }
    public int NewValue { get; set; }
    public string Message { get; set; }
    public int DayNumber { get; set; }
}

public sealed class CompanyNeedChangeRecord
{
    public string Need { get; set; }
    public int Delta { get; set; }
    public int OldValue { get; set; }
    public int NewValue { get; set; }
    public string Message { get; set; }
    public int DayNumber { get; set; }
}
```

**5.5.5: Update Daily Brief Generator**
```csharp
// File: src/Features/Interface/News/Generation/DailyReportGenerator.cs

public static List<string> Generate(DailyReportSnapshot snapshot, DailyReportGenerationContext context, int maxLines = 8)
{
    if (snapshot == null)
    {
        return new List<string>();
    }

    context ??= new DailyReportGenerationContext { DayNumber = snapshot.DayNumber };
    maxLines = Math.Max(1, Math.Min(maxLines, 12));

    var candidates = new List<Candidate>();

    // NEW: Recent order outcomes (HIGHEST PRIORITY)
    if (EnlistedNewsBehavior.Instance != null)
    {
        var recentOrders = EnlistedNewsBehavior.Instance.GetRecentOrderOutcomes(maxDaysOld: 1);
        foreach (var order in recentOrders)
        {
            candidates.Add(new Candidate
            {
                Text = order.BriefSummary,
                Priority = 98,
                Severity = order.Success ? 10 : 70,
                Confidence = 1.0f
            });
        }
        
        // NEW: Significant reputation changes
        var repChanges = EnlistedNewsBehavior.Instance.GetRecentReputationChanges(maxDaysOld: 1);
        foreach (var change in repChanges)
        {
            candidates.Add(new Candidate
            {
                Text = change.Message,
                Priority = 85,
                Severity = 30,
                Confidence = 0.9f
            });
        }
        
        // NEW: Company need changes
        var needChanges = EnlistedNewsBehavior.Instance.GetRecentCompanyNeedChanges(maxDaysOld: 1);
        foreach (var change in needChanges)
        {
            candidates.Add(new Candidate
            {
                Text = change.Message,
                Priority = 80,
                Severity = change.NewValue < 30 ? 80 : 20,
                Confidence = 0.9f
            });
        }
    }

    // ===== Existing logic (Company urgent objectives, Lance health, movement, etc.) =====
    if (!string.IsNullOrWhiteSpace(snapshot.BattleTag))
    {
        // ... existing battle/siege logic ...
    }
    
    // ... rest of existing generator logic ...
    
    // Sort and select top candidates
    var selected = candidates
        .OrderByDescending(c => c.Priority)
        .ThenByDescending(c => c.Severity)
        .Take(maxLines)
        .Select(c => c.Text)
        .ToList();
    
    return selected;
}
```

**5.5.6: Update Reports Menu Text**
```csharp
// File: src/Features/Interface/Behaviors/EnlistedMenuBehavior.cs

private string GetReportsText()
{
    var service = ServiceRecordManager.Instance;
    var orders = OrderManager.Instance;
    var news = EnlistedNewsBehavior.Instance;
    
    var sb = new StringBuilder();
    
    sb.AppendLine("Service Record:");
    sb.AppendLine($"  Days Served: {service.CurrentTerm.DaysServed}");
    sb.AppendLine($"  Battles: {service.CurrentTerm.BattlesWon + service.CurrentTerm.BattlesLost}");
    sb.AppendLine($"  Victories: {service.CurrentTerm.BattlesWon}");
    sb.AppendLine($"  Orders Completed: {orders?.CompletedOrderCount ?? 0}");
    sb.AppendLine();
    
    sb.AppendLine("Recent Activity:");
    
    // Show recent order outcomes
    if (news != null)
    {
        var recentOrders = news.GetRecentOrderOutcomes(maxDaysOld: 5);
        foreach (var order in recentOrders)
        {
            int daysAgo = (int)CampaignTime.Now.ToDays - order.DayNumber;
            string timeStr = daysAgo == 0 ? "today" : daysAgo == 1 ? "yesterday" : $"{daysAgo} days ago";
            
            sb.AppendLine($"  • {order.OrderTitle} ({timeStr})");
            sb.AppendLine($"    {order.DetailedSummary}");
            sb.AppendLine();
        }
        
        // Show significant reputation changes
        var recentRep = news.GetRecentReputationChanges(maxDaysOld: 5);
        foreach (var rep in recentRep)
        {
            int daysAgo = (int)CampaignTime.Now.ToDays - rep.DayNumber;
            string timeStr = daysAgo == 0 ? "today" : daysAgo == 1 ? "yesterday" : $"{daysAgo} days ago";
            
            sb.AppendLine($"  • {rep.Target} reputation {rep.Delta:+#;-#;0} ({timeStr})");
            sb.AppendLine($"    {rep.Message}");
            sb.AppendLine();
        }
    }
    
    // Company status
    sb.AppendLine("Company Status:");
    sb.AppendLine($"  Readiness: {GetCompanyNeed("Readiness")}");
    sb.AppendLine($"  Morale: {GetCompanyNeed("Morale")}");
    sb.AppendLine($"  Supplies: {GetCompanyNeed("Supplies")}");
    sb.AppendLine($"  Equipment: {GetCompanyNeed("Equipment")}");
    sb.AppendLine($"  Rest: {GetCompanyNeed("Rest")}");
    
    return sb.ToString();
}
```

**5.5.7: Remove Lance References**
```csharp
// Update terminology throughout news system:
// "Lance" → "Unit" or "Company"
// "Lance status" → "Unit status"
// "Lance health" → "Unit casualties"

// File: src/Features/Interface/News/Generation/DailyReportGenerator.cs
// Update all template IDs and text:
// "lance_health_dead" → "unit_casualties_dead"
// "lance_health_wounded" → "unit_casualties_wounded"
// "lance_health_sick" → "unit_casualties_sick"

// File: src/Features/Interface/Behaviors/EnlistedNewsBehavior.cs
// Update variable names:
// _dailyBriefLance → _dailyBriefUnit
```

#### Deliverables
- [x] OrderManager reports outcomes to news system
- [x] EscalationManager reports significant reputation changes
- [x] OrderEffectApplier reports company need changes
- [x] EnlistedNewsBehavior API extended with new tracking
- [x] Daily Brief generator prioritizes player actions
- [x] Reports menu shows recent activity with details
- [x] Lance terminology replaced with Unit/Company

#### Testing
- [x] Order completion shows in daily brief
- [x] Order failure shows in daily brief
- [x] Reputation changes (±10+) appear in reports
- [x] Company need changes appear when significant
- [x] Critical thresholds trigger news alerts
- [x] Reports menu shows last 5 days of activity
- [x] Order details include reputation impacts
- [x] Lance references removed from all news text

---

### Phase 5.6: Strategic Context Enhancement
**Duration**: Week 6-7 (~15 hours)
**Goal**: Apply AI research to make existing features strategically aware
**Status**: ⏳ READY TO START (after Phase 5.5)
**Reference**: `docs/research/ai-strategic-behavior-analysis-v2.md`

#### Overview

After researching Bannerlord's AI strategic behavior system, we can enhance Enlisted's existing features to be strategically aware without adding new features. The key insight: Bannerlord tracks strategic data (Objectives, Aggressiveness, Settlement threat intensity) but doesn't use it well. We can use this data to make Orders, Events, and Reports feel connected to actual strategic plans.

**All configuration is data-driven** (JSON/XML) for easy tuning and modding.

**All text uses light Bannerlord RP** — medieval/period-appropriate language, not modern military jargon. Context descriptions read like a soldier's account, not an operations manual.

#### File Structure

```
ModuleData/Enlisted/
├── strategic_context_config.json (NEW - ~150 lines)
│   ├── War stance thresholds
│   ├── Strategic context definitions (8 contexts)
│   ├── Settlement value parameters
│   ├── Coordination detection config
│   └── Company needs prediction templates
├── Languages/
│   └── strategic_context_strings.xml (NEW - ~30 strings)
│       ├── War stance descriptions
│       ├── Strategic context labels
│       └── Strategic value labels
└── Orders/
    ├── orders_t1_t3.json (UPDATED - add strategic tags)
    ├── orders_t4_t6.json (UPDATED - add strategic tags)
    └── orders_t7_t9.json (UPDATED - add strategic tags)

src/Features/Context/
└── ArmyContextAnalyzer.cs (ENHANCED)
    ├── Loads strategic_context_config.json
    ├── GetWarStance() - config-driven thresholds
    ├── GetLordStrategicContext() - context detection
    ├── GetSettlementStrategicValue() - config-driven calculation
    └── IsPartOfCoordinatedOperation() - config-driven parameters

src/Features/Orders/
└── OrderCatalog.cs (ENHANCED)
    └── SelectOrder() - strategic tag matching from config

src/Features/Company/
└── CompanyNeedsManager.cs (ENHANCED)
    └── PredictUpcomingNeeds() - prediction templates from config
```

#### What Gets Enhanced

1. **ArmyContextAnalyzer** - Understand strategic intent, not just tactical state
2. **Order Selection** - Orders reflect lord's actual strategic plans (tag-based from config)
3. **Event Context** - Events match strategic situation (same tags system)
4. **News Reports** - Explain strategic WHY behind tactical WHAT (strings from XML)
5. **Company Needs Predictions** - Predict needs based on upcoming operations (templates from JSON)

#### Quick Reference: JSON/XML Structure

**Three Files to Create/Update**:
1. **strategic_context_config.json** - All strategic assessment configuration
2. **strategic_context_strings.xml** - All localizable text
3. **orders_*.json** - Add strategic tags to existing orders

#### Tasks

**5.6.1: Create Strategic Context Configuration**

Create comprehensive JSON configuration for strategic assessment:

```json
// File: ModuleData/Enlisted/strategic_context_config.json

{
  "war_stance_thresholds": {
    "desperate": 0.3,
    "defensive": 0.5,
    "balanced": 0.7,
    "offensive": 0.9
  },
  "weights": {
    "territory_control": 0.4,
    "military_strength": 0.4,
    "economic_situation": 0.2
  },
  "strategic_contexts": {
    "coordinated_offensive": {
      "display_name": "Grand Campaign",
      "description": "The banners are gathering. Your lord rides with allied hosts to bring {target} under the sword.",
      "order_tags": ["assault", "tactical", "combat", "preparation", "intel"],
      "inappropriate_tags": ["leisure", "social", "camp_routine"]
    },
    "desperate_defense": {
      "display_name": "Last Stand",
      "description": "Dark days. The realm bleeds and every sword arm is needed. There will be no rest until the tide turns.",
      "order_tags": ["defense", "urgent", "critical", "combat"],
      "inappropriate_tags": ["leisure", "social", "training", "camp_routine"]
    },
    "economic_warfare": {
      "display_name": "Harrying the Lands",
      "description": "Your lord seeks to bleed the enemy's purse before the true campaign begins. Burn their stores, take their cattle.",
      "order_tags": ["raid", "supply", "scout", "tactical"],
      "inappropriate_tags": ["defense", "siege"]
    },
    "preparing_offensive": {
      "display_name": "Mustering for War",
      "description": "The host gathers strength. Supplies are stockpiled and blades sharpened. A great push is coming.",
      "order_tags": ["preparation", "intel", "supply", "training"],
      "inappropriate_tags": ["urgent", "combat"]
    },
    "strategic_expansion": {
      "display_name": "Pressing the Advantage",
      "description": "Fortune favors your lord's banner. Now is the hour to claim new lands while the enemy reels.",
      "order_tags": ["assault", "siege", "tactical", "scout"],
      "inappropriate_tags": ["defense", "urgent"]
    },
    "strategic_defense": {
      "display_name": "Holding the Line",
      "description": "The war goes poorly. Your lord rides to shield the realm's holdings from the enemy host.",
      "order_tags": ["defense", "patrol", "preparation", "intel"],
      "inappropriate_tags": ["assault", "raid"]
    },
    "territory_protection": {
      "display_name": "Riding the Marches",
      "description": "Peaceful times, but vigilance is required. The host patrols to ward off bandits and show the banner.",
      "order_tags": ["patrol", "scout", "defense"],
      "inappropriate_tags": ["assault", "siege", "urgent"]
    },
    "routine_operations": {
      "display_name": "Camp Life",
      "description": "No pressing matters. The company rests, trains, and tends to daily duties.",
      "order_tags": ["camp_routine", "training", "social", "patrol"],
      "inappropriate_tags": []
    }
  },
  "settlement_strategic_value": {
    "fortification_base": 0.3,
    "border_bonus": 0.2,
    "neighbor_multiplier": 0.05,
    "prosperity_scale": 0.00001
  },
  "coordination_detection": {
    "min_allied_lords": 2,
    "max_days_away": 3.0
  },
  "company_needs_predictions": {
    "coordinated_offensive": {
      "upcoming_needs": ["Supplies", "Rest", "Equipment"],
      "timeframe": "a fortnight or more",
      "action": "Ration your supplies, keep your blade sharp, and sleep when you can."
    },
    "preparing_offensive": {
      "upcoming_needs": ["Supplies", "Equipment"],
      "timeframe": "within the week",
      "action": "Fill your quiver, mend your mail, and rest well. Hard days are coming."
    },
    "desperate_defense": {
      "upcoming_needs": ["Readiness", "Morale", "Equipment"],
      "timeframe": "now and for days to come",
      "action": "Steel yourself. Every sword arm counts. Keep spirits high if you can."
    },
    "economic_warfare": {
      "upcoming_needs": ["Rest", "Supplies"],
      "timeframe": "the coming week",
      "action": "Travel light, eat well, and rest when the column halts."
    },
    "strategic_defense": {
      "upcoming_needs": ["Readiness", "Supplies"],
      "timeframe": "as long as the siege holds",
      "action": "Stand ready. Watch the walls. Ration carefully."
    },
    "routine_operations": {
      "upcoming_needs": [],
      "timeframe": "no pressing matters",
      "action": "Attend to your duties. Rest, train, and keep your kit in order."
    }
  }
}
```

**5.6.2: Create Strategic Context Strings**

Add localized strings for strategic context:

```xml
<!-- File: ModuleData/Enlisted/Languages/strategic_context_strings.xml -->

<?xml version="1.0" encoding="utf-8"?>
<base xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" xmlns:xsd="http://www.w3.org/2001/XMLSchema" type="string">
  <tags>
    <tag language="English" />
  </tags>
  <strings>
    <!-- War Stance Descriptions -->
    <string id="war_stance_desperate" text="The realm bleeds — every sword is needed" />
    <string id="war_stance_defensive" text="Hard times — the host holds what it can" />
    <string id="war_stance_balanced" text="The war hangs in the balance" />
    <string id="war_stance_offensive" text="Fortune favors our banners" />
    <string id="war_stance_dominant" text="Victory is within grasp" />
    <string id="war_stance_neutral" text="A time of peace" />
    
    <!-- Strategic Context Descriptions (loaded from config, these are fallbacks) -->
    <string id="context_coordinated_offensive" text="Grand campaign with allied hosts" />
    <string id="context_desperate_defense" text="Fighting for the realm's survival" />
    <string id="context_economic_warfare" text="Harrying the enemy lands" />
    <string id="context_preparing_offensive" text="Mustering for war" />
    <string id="context_strategic_expansion" text="Pressing the advantage" />
    <string id="context_strategic_defense" text="Holding the line" />
    <string id="context_territory_protection" text="Riding the marches" />
    <string id="context_routine_operations" text="Camp life" />
    
    <!-- Strategic Value Labels -->
    <string id="strategic_value_critical" text="a fortress of great renown" />
    <string id="strategic_value_high" text="a prize worth bleeding for" />
    <string id="strategic_value_moderate" text="a worthy holding" />
    <string id="strategic_value_limited" text="a minor holding" />
    
    <!-- Report Headers -->
    <string id="report_strategic_situation" text="━━━ THE STATE OF THINGS ━━━" />
    <string id="report_war_stance" text="The War: {WAR_STANCE}" />
    <string id="report_lord_operation" text="Your Lord's Purpose: {OPERATION}" />
    <string id="report_outlook" text="What Lies Ahead: {OUTLOOK}" />
    <string id="report_action" text="Your Part: {ACTION}" />
  </strings>
</base>
```

**5.6.3: Enhance ArmyContextAnalyzer with Strategic Assessment**

Add strategic understanding using the configuration files:

```csharp
// File: src/Features/Context/ArmyContextAnalyzer.cs
// ADD these new methods (existing methods stay unchanged)

// Static config loaded from JSON
private static StrategicContextConfig _config;

static ArmyContextAnalyzer()
{
    LoadConfiguration();
}

private static void LoadConfiguration()
{
    string configPath = "ModuleData/Enlisted/strategic_context_config.json";
    string json = File.ReadAllText(configPath);
    _config = JsonConvert.DeserializeObject<StrategicContextConfig>(json);
    
    ModLogger.Info("Context", $"Loaded strategic context config from {configPath}");
}

/// <summary>
/// Evaluates the faction's overall war situation to determine strategic stance.
/// Uses data from Bannerlord's AI system (MobileParty.Objective, army formations, etc.)
/// Configuration loaded from strategic_context_config.json
/// </summary>
public static WarStance GetWarStance(IFaction faction)
{
    if (faction == null || !faction.IsAtWarWith(Clan.PlayerClan.MapFaction))
        return WarStance.Neutral;
    
    // Load weights from config
    var weights = _config.Weights;
    
    // Calculate territory control ratio
    float territoryRatio = CalculateTerritoryControlRatio(faction, Clan.PlayerClan.MapFaction);
    
    // Calculate military strength ratio
    float strengthRatio = CalculateMilitaryStrengthRatio(faction, Clan.PlayerClan.MapFaction);
    
    // Calculate economic situation
    float economicScore = CalculateEconomicSituation(faction);
    
    // Overall score (0.0 = losing badly, 1.0 = winning decisively)
    float overallScore = (territoryRatio * weights.TerritoryControl) + 
                        (strengthRatio * weights.MilitaryStrength) + 
                        (economicScore * weights.EconomicSituation);
    
    // Use thresholds from config
    var thresholds = _config.WarStanceThresholds;
    return overallScore switch
    {
        var s when s < thresholds.Desperate => WarStance.Desperate,
        var s when s < thresholds.Defensive => WarStance.Defensive,
        var s when s < thresholds.Balanced => WarStance.Balanced,
        var s when s < thresholds.Offensive => WarStance.Offensive,
        _ => WarStance.Dominant
    };
}

private static float CalculateTerritoryControlRatio(IFaction ourFaction, IFaction enemyFaction)
{
    int ourSettlements = ourFaction.Settlements.Count;
    int enemySettlements = enemyFaction.Settlements.Count;
    int total = ourSettlements + enemySettlements;
    
    if (total == 0) return 0.5f;
    return (float)ourSettlements / total;
}

private static float CalculateMilitaryStrengthRatio(IFaction ourFaction, IFaction enemyFaction)
{
    // Use Bannerlord's existing TotalStrength property
    float ourStrength = ourFaction.TotalStrength;
    float enemyStrength = enemyFaction.TotalStrength;
    float total = ourStrength + enemyStrength;
    
    if (total < 1f) return 0.5f;
    return ourStrength / total;
}

private static float CalculateEconomicSituation(IFaction faction)
{
    if (!faction.IsKingdomFaction) return 0.5f;
    
    var kingdom = (Kingdom)faction;
    float totalProsperity = 0f;
    foreach (var settlement in kingdom.Settlements)
    {
        if (settlement.IsTown)
            totalProsperity += settlement.Town.Prosperity;
    }
    
    // Normalize to 0-1 scale (typical kingdom has 30k-100k total prosperity)
    return Math.Clamp(totalProsperity / 60000f, 0.1f, 1.0f);
}

/// <summary>
/// Determines if lord's current action is part of a coordinated operation.
/// Checks if multiple friendly armies are working toward similar objectives.
/// Configuration loaded from strategic_context_config.json
/// </summary>
public static bool IsPartOfCoordinatedOperation(MobileParty lordParty)
{
    if (lordParty == null || lordParty.TargetSettlement == null)
        return false;
    
    var targetSettlement = lordParty.TargetSettlement;
    var faction = lordParty.MapFaction;
    
    // Load detection parameters from config
    var detection = _config.CoordinationDetection;
    
    int nearbyAlliesWithSameTarget = 0;
    
    foreach (var party in MobileParty.AllLordParties)
    {
        if (party == lordParty) continue;
        if (party.MapFaction != faction) continue;
        if (party.TargetSettlement != targetSettlement) continue;
        
        // Same target within reasonable time
        float distance = party.Position2D.Distance(targetSettlement.Position2D);
        float daysAway = distance / (Campaign.Current.EstimatedAverageLordPartySpeed * CampaignTime.HoursInDay);
        
        if (daysAway < detection.MaxDaysAway)
            nearbyAlliesWithSameTarget++;
    }
    
    // Coordinated if meets minimum allied lords threshold from config
    return nearbyAlliesWithSameTarget >= detection.MinAlliedLords;
}

// Add configuration model classes
public class StrategicContextConfig
{
    public WarStanceThresholds WarStanceThresholds { get; set; }
    public WarStanceWeights Weights { get; set; }
    public Dictionary<string, StrategicContextDefinition> StrategicContexts { get; set; }
    public SettlementValueConfig SettlementStrategicValue { get; set; }
    public CoordinationDetectionConfig CoordinationDetection { get; set; }
    public Dictionary<string, CompanyNeedsPredictionConfig> CompanyNeedsPredictions { get; set; }
}

public class WarStanceThresholds
{
    public float Desperate { get; set; }
    public float Defensive { get; set; }
    public float Balanced { get; set; }
    public float Offensive { get; set; }
}

public class WarStanceWeights
{
    public float TerritoryControl { get; set; }
    public float MilitaryStrength { get; set; }
    public float EconomicSituation { get; set; }
}

public class StrategicContextDefinition
{
    public string DisplayName { get; set; }
    public string Description { get; set; }
    public List<string> OrderTags { get; set; }
    public List<string> InappropriateTags { get; set; }
}

public class SettlementValueConfig
{
    public float FortificationBase { get; set; }
    public float BorderBonus { get; set; }
    public float NeighborMultiplier { get; set; }
    public float ProsperityScale { get; set; }
}

public class CoordinationDetectionConfig
{
    public int MinAlliedLords { get; set; }
    public float MaxDaysAway { get; set; }
}

public class CompanyNeedsPredictionConfig
{
    public List<string> UpcomingNeeds { get; set; }
    public string Timeframe { get; set; }
    public string Action { get; set; }
}

/// <summary>
/// Gets strategic value of a settlement based on position and control.
/// Higher value = more strategically important target.
/// Configuration loaded from strategic_context_config.json
/// </summary>
public static float GetSettlementStrategicValue(Settlement settlement)
{
    if (settlement == null) return 0f;
    
    // Load values from config
    var valueConfig = _config.SettlementStrategicValue;
    
    float value = 0f;
    
    // Base economic value (prosperity)
    if (settlement.IsTown)
        value += settlement.Town.Prosperity * valueConfig.ProsperityScale;
    
    // Fortifications more valuable
    if (settlement.IsFortification)
        value += valueConfig.FortificationBase;
    
    // Border settlements more strategically important
    bool isBorderSettlement = IsBorderSettlement(settlement);
    if (isBorderSettlement)
        value += valueConfig.BorderBonus;
    
    // Central settlements (many neighbors) are strategic hubs
    int neighborCount = settlement.IsTown 
        ? settlement.Town.GetNeighborFortifications(MobileParty.NavigationType.All).Count 
        : 0;
    value += neighborCount * valueConfig.NeighborMultiplier;
    
    return Math.Clamp(value, 0.1f, 1.0f);
}

private static bool IsBorderSettlement(Settlement settlement)
{
    // Settlement is on border if it has neighbors of different factions
    if (!settlement.IsFortification) return false;
    
    var neighbors = settlement.Town.GetNeighborFortifications(MobileParty.NavigationType.All);
    foreach (var neighbor in neighbors)
    {
        if (neighbor.MapFaction != settlement.MapFaction)
            return true;
    }
    return false;
}

/// <summary>
/// Enhanced objective detection that understands strategic intent.
/// Logic is data-driven - context definitions loaded from strategic_context_config.json
/// </summary>
public static LordStrategicContext GetLordStrategicContext(MobileParty army)
{
    if (army == null) return LordStrategicContext.Unknown;
    
    var basicObjective = GetLordObjective(army); // Existing method
    var warStance = GetWarStance(army.MapFaction);
    var isCoordinated = IsPartOfCoordinatedOperation(army);
    
    // Interpret tactical action in strategic context
    return (basicObjective, warStance, isCoordinated) switch
    {
        (LordObjective.Besieging, WarStance.Desperate, _) => 
            LordStrategicContext.DesperateCounterOffensive,
            
        (LordObjective.Besieging, _, true) => 
            LordStrategicContext.CoordinatedOffensive,
            
        (LordObjective.Besieging, WarStance.Offensive, _) => 
            LordStrategicContext.StrategicExpansion,
            
        (LordObjective.Raiding, WarStance.Balanced or WarStance.Offensive, _) => 
            LordStrategicContext.EconomicWarfare,
            
        (LordObjective.Defending, WarStance.Desperate, _) => 
            LordStrategicContext.DesperateDefense,
            
        (LordObjective.Defending, _, _) => 
            LordStrategicContext.StrategicDefense,
            
        (LordObjective.Patrolling, WarStance.Defensive, _) => 
            LordStrategicContext.TerritoryProtection,
            
        (LordObjective.Resting, WarStance.Offensive, _) => 
            LordStrategicContext.PreparingOffensive,
            
        _ => LordStrategicContext.RoutineOperations
    };
}

/// <summary>
/// Gets strategic context definition from config.
/// Returns display name, description, and tag filters.
/// </summary>
public static StrategicContextDefinition GetContextDefinition(LordStrategicContext context)
{
    string key = context.ToString().ToSnakeCase(); // e.g., "coordinated_offensive"
    
    if (_config.StrategicContexts.TryGetValue(key, out var definition))
        return definition;
    
    // Fallback to routine operations
    return _config.StrategicContexts["routine_operations"];
}

// Add these enums
public enum WarStance
{
    Neutral,
    Desperate,      // < 0.3: Losing badly
    Defensive,      // 0.3-0.5: Unfavorable
    Balanced,       // 0.5-0.7: Even footing
    Offensive,      // 0.7-0.9: Winning
    Dominant        // > 0.9: Decisively winning
}

public enum LordStrategicContext
{
    Unknown,
    RoutineOperations,
    PreparingOffensive,
    CoordinatedOffensive,
    StrategicExpansion,
    EconomicWarfare,
    TerritoryProtection,
    StrategicDefense,
    DesperateDefense,
    DesperateCounterOffensive
}
```

**5.6.4: Add Config Loader to OrderCatalog**

Load strategic context config in OrderCatalog:

```csharp
// File: src/Features/Orders/OrderCatalog.cs
// ENHANCE existing SelectOrder() method

public static Order SelectOrder()
{
    var tier = EnlistmentBehavior.Instance.EnlistmentTier;
    var context = GetCampaignContext();
    var role = EnlistedStatusManager.Instance.GetPrimaryRole();
    
    // NEW: Get strategic context
    var lordParty = EnlistmentBehavior.Instance.CurrentLord?.PartyBelongedTo;
    var strategicContext = lordParty != null 
        ? ArmyContextAnalyzer.GetLordStrategicContext(lordParty)
        : LordStrategicContext.RoutineOperations;
    
    // Filter by tier
    var eligibleOrders = _orders
        .Where(o => tier >= o.Requirements.TierMin && tier <= o.Requirements.TierMax)
        .ToList();
    
    // Filter by skill/trait requirements
    eligibleOrders = eligibleOrders
        .Where(o => MeetsSkillRequirements(o) && MeetsTraitRequirements(o))
        .ToList();
    
    if (eligibleOrders.Count == 0) return null;
    
    // NEW: Score orders by strategic appropriateness (using config data)
    var scoredOrders = new List<(Order order, float score)>();
    foreach (var order in eligibleOrders)
    {
        float score = CalculateOrderStrategicScore(order, role, context, strategicContext);
        scoredOrders.Add((order, score));
    }
    
    // Use weighted random selection (higher score = more likely)
    return WeightedRandomSelect(scoredOrders);
}

private static float CalculateOrderStrategicScore(
    Order order, 
    string role, 
    string context, 
    LordStrategicContext strategicContext)
{
    float score = 1.0f; // Base score
    
    // Boost if matches role
    if (order.Tags.Contains(role.ToLower()))
        score *= 2.0f;
    
    // Boost if matches context
    if (order.Tags.Contains(context.ToLower()))
        score *= 1.5f;
    
    // NEW: Strategic context matching using config data
    var contextDef = ArmyContextAnalyzer.GetContextDefinition(strategicContext);
    
    bool strategicallyAppropriate = order.Tags.Any(t => contextDef.OrderTags.Contains(t));
    bool strategicallyInappropriate = order.Tags.Any(t => contextDef.InappropriateTags.Contains(t));
    
    if (strategicallyAppropriate)
        score *= 2.5f; // Strongly prefer strategically appropriate orders
    else if (strategicallyInappropriate)
        score *= 0.2f; // Strongly discourage inappropriate orders
    
    return score;
}

private static Order WeightedRandomSelect(List<(Order order, float score)> scoredOrders)
{
    if (scoredOrders.Count == 0) return null;
    
    float totalScore = scoredOrders.Sum(x => x.score);
    float roll = MBRandom.RandomFloat * totalScore;
    
    float cumulative = 0f;
    foreach (var (order, score) in scoredOrders)
    {
        cumulative += score;
        if (roll <= cumulative)
            return order;
    }
    
    return scoredOrders.Last().order; // Fallback
}
```

**5.6.5: Add Strategic Context to Order Descriptions**

Dynamically enhance order descriptions using config data:

```csharp
// File: src/Features/Orders/Behaviors/OrderManager.cs
// ENHANCE ShowOrderNotification method

private void ShowOrderNotification(Order order)
{
    var lordParty = EnlistmentBehavior.Instance.CurrentLord?.PartyBelongedTo;
    if (lordParty == null) return;
    
    var strategicContext = ArmyContextAnalyzer.GetLordStrategicContext(lordParty);
    var warStance = ArmyContextAnalyzer.GetWarStance(lordParty.MapFaction);
    
    // Build notification text
    var sb = new StringBuilder();
    sb.AppendLine($"Order from {order.Issuer}:");
    sb.AppendLine($"\"{order.Title}\"");
    sb.AppendLine();
    sb.AppendLine(order.Description);
    sb.AppendLine();
    
    // NEW: Add strategic context from config
    string strategicNote = GetStrategicContextNote(strategicContext, lordParty);
    if (!string.IsNullOrEmpty(strategicNote))
    {
        sb.AppendLine($"Context: {strategicNote}");
    }
    
    InformationManager.ShowInquiry(
        new InquiryData(
            "New Order",
            sb.ToString(),
            true,
            true,
            "Accept",
            "Decline",
            () => AcceptOrder(),
            () => DeclineOrder()
        )
    );
}

private string GetStrategicContextNote(LordStrategicContext context, MobileParty lordParty)
{
    // Load context definition from config (descriptions written in RP style)
    var contextDef = ArmyContextAnalyzer.GetContextDefinition(context);
    
    // Use description from config, replace {target} placeholder
    string description = contextDef.Description;
    if (lordParty?.TargetSettlement != null)
    {
        description = description.Replace("{target}", lordParty.TargetSettlement.Name.ToString());
        
        // Add settlement description if targeting a specific place
        if (context == LordStrategicContext.StrategicExpansion || 
            context == LordStrategicContext.CoordinatedOffensive)
        {
            float value = ArmyContextAnalyzer.GetSettlementStrategicValue(lordParty.TargetSettlement);
            string valueText = GetStrategicValueText(value);
            description += $" — {valueText}";
        }
    }
    
    return description;
}

private string GetStrategicValueText(float value)
{
    // Load from XML strings (written in RP style)
    // e.g., "a fortress of great renown", "a prize worth bleeding for"
    return value switch
    {
        >= 0.8f => GameTexts.FindText("strategic_value_critical").ToString(),
        >= 0.6f => GameTexts.FindText("strategic_value_high").ToString(),
        >= 0.4f => GameTexts.FindText("strategic_value_moderate").ToString(),
        _ => GameTexts.FindText("strategic_value_limited").ToString()
    };
}
```

**5.6.6: Enhance News Reports with Strategic Analysis**

Make daily briefs explain the strategic WHY using config data:

```csharp
// File: src/Features/Interface/News/Generation/DailyReportGenerator.cs
// ENHANCE existing Generate method

public static List<string> Generate(DailyReportSnapshot snapshot, 
    DailyReportGenerationContext context, int maxLines = 8)
{
    if (snapshot == null) return new List<string>();
    
    context ??= new DailyReportGenerationContext { DayNumber = snapshot.DayNumber };
    maxLines = Math.Max(1, Math.Min(maxLines, 12));
    
    var candidates = new List<Candidate>();
    
    // NEW: Strategic situation summary (if significant)
    var lordParty = EnlistmentBehavior.Instance.CurrentLord?.PartyBelongedTo;
    if (lordParty != null)
    {
        var warStance = ArmyContextAnalyzer.GetWarStance(lordParty.MapFaction);
        string strategicSummary = GetStrategicSummary(lordParty, warStance);
        
        if (!string.IsNullOrEmpty(strategicSummary))
        {
            candidates.Add(new Candidate
            {
                Text = strategicSummary,
                Priority = 95, // High priority - sets context for everything else
                Severity = warStance == WarStance.Desperate ? 90 : 40,
                Confidence = 0.9f
            });
        }
    }
    
    // Order outcomes (existing, keep as-is)
    if (EnlistedNewsBehavior.Instance != null)
    {
        var recentOrders = EnlistedNewsBehavior.Instance.GetRecentOrderOutcomes(maxDaysOld: 1);
        foreach (var order in recentOrders)
        {
            candidates.Add(new Candidate
            {
                Text = order.BriefSummary,
                Priority = 98,
                Severity = order.Success ? 10 : 70,
                Confidence = 1.0f
            });
        }
    }
    
    // ... rest of existing logic ...
    
    // Sort and select top candidates
    var selected = candidates
        .OrderByDescending(c => c.Priority)
        .ThenByDescending(c => c.Severity)
        .Take(maxLines)
        .Select(c => c.Text)
        .ToList();
    
    return selected;
}

private static string GetStrategicSummary(MobileParty lordParty, WarStance stance)
{
    var strategicContext = ArmyContextAnalyzer.GetLordStrategicContext(lordParty);
    
    // Load description from config
    var contextDef = ArmyContextAnalyzer.GetContextDefinition(strategicContext);
    
    // Don't show summary for routine operations
    if (strategicContext == LordStrategicContext.RoutineOperations)
        return null;
    
    // Use description from config, replace placeholders
    string description = contextDef.Description;
    if (lordParty?.TargetSettlement != null)
    {
        description = description.Replace("{target}", lordParty.TargetSettlement.Name.ToString());
    }
    
    return description;
}
```

**5.6.7: Predict Company Needs Based on Strategic Plans**

Use AI knowledge and config data to predict upcoming needs:

```csharp
// File: src/Features/Company/CompanyNeedsManager.cs
// ADD new prediction method (existing methods stay unchanged)

// Load config once
private static StrategicContextConfig _strategicConfig;

static CompanyNeedsManager()
{
    LoadStrategicConfig();
}

private static void LoadStrategicConfig()
{
    string configPath = "ModuleData/Enlisted/strategic_context_config.json";
    string json = File.ReadAllText(configPath);
    _strategicConfig = JsonConvert.DeserializeObject<StrategicContextConfig>(json);
}

/// <summary>
/// Predicts upcoming company need changes based on lord's strategic plans.
/// Uses Bannerlord AI data to anticipate what operations are coming.
/// Prediction data loaded from strategic_context_config.json
/// </summary>
public static CompanyNeedsPrediction PredictUpcomingNeeds()
{
    var lordParty = EnlistmentBehavior.Instance.CurrentLord?.PartyBelongedTo;
    if (lordParty == null)
        return new CompanyNeedsPrediction 
        { 
            Message = "Normal operations expected.",
            UpcomingNeeds = new CompanyNeed[0],
            TimeframeMessage = "standard tempo",
            RecommendedAction = "Maintain normal readiness levels"
        };
    
    var strategicContext = ArmyContextAnalyzer.GetLordStrategicContext(lordParty);
    
    // Load prediction from config
    string contextKey = strategicContext.ToString().ToSnakeCase();
    
    if (_strategicConfig.CompanyNeedsPredictions.TryGetValue(contextKey, out var predictionConfig))
    {
        // Get context definition for message
        var contextDef = ArmyContextAnalyzer.GetContextDefinition(strategicContext);
        
        // Convert string need names to enums
        var needs = predictionConfig.UpcomingNeeds
            .Select(n => Enum.Parse<CompanyNeed>(n))
            .ToArray();
        
        return new CompanyNeedsPrediction
        {
            Message = contextDef.Description,
            UpcomingNeeds = needs,
            TimeframeMessage = predictionConfig.Timeframe,
            RecommendedAction = predictionConfig.Action
        };
    }
    
    // Default fallback
    return new CompanyNeedsPrediction
    {
        Message = "Routine operations expected.",
        UpcomingNeeds = new CompanyNeed[0],
        TimeframeMessage = "standard tempo",
        RecommendedAction = "Maintain normal readiness levels"
    };
}

public class CompanyNeedsPrediction
{
    public string Message { get; set; }
    public CompanyNeed[] UpcomingNeeds { get; set; }
    public string TimeframeMessage { get; set; }
    public string RecommendedAction { get; set; }
}
```

private static string GetStrategicSummary(MobileParty lordParty, WarStance stance)
{
    var strategicContext = ArmyContextAnalyzer.GetLordStrategicContext(lordParty);
    
    // Load description from config (written in RP style)
    var contextDef = ArmyContextAnalyzer.GetContextDefinition(strategicContext);
    
    // Don't show summary for routine operations
    if (strategicContext == LordStrategicContext.RoutineOperations)
        return null;
    
    // Use description from config, replace placeholders
    string description = contextDef.Description;
    if (lordParty?.TargetSettlement != null)
    {
        description = description.Replace("{target}", lordParty.TargetSettlement.Name.ToString());
    }
    
    return description;
}

private string GetWarStanceText(WarStance stance)
{
    // Load from XML strings (written in RP style)
    // e.g., "The realm bleeds", "Fortune favors our banners"
    string stringId = $"war_stance_{stance.ToString().ToLower()}";
    return GameTexts.FindText(stringId)?.ToString() ?? stance.ToString();
}

private string GetStrategicContextText(LordStrategicContext context)
{
    // Load display name from config (written in RP style)
    // e.g., "Grand Campaign", "Mustering for War", "Holding the Line"
    var contextDef = ArmyContextAnalyzer.GetContextDefinition(context);
    return contextDef.DisplayName;
}
```

**5.6.8: Update Order JSON Files with Strategic Tags**

Update all order definitions to include appropriate strategic tags (these tags match the config):

```json
// File: ModuleData/Enlisted/Orders/orders_t4_t6.json (example)

{
  "id": "t5_scout_siege_approach",
  "title": "Scout the Walls",
  "description": "Slip close to the enemy walls and find the best ground for our siege works.",
  "issuer": "auto",
  "tags": ["scout", "war", "outdoor", "preparation", "intel"],
  "requirements": {
    "tier_min": 5,
    "tier_max": 7,
    "min_traits": { "ScoutSkills": 8 }
  },
  "consequences": {
    "success": {
      "reputation": { "lord": 10, "officer": 15 },
      "company_needs": { "Readiness": 10 },
      "text": "You return with a sketch of the walls and word of a weak point. The captain nods his approval."
    },
    "failure": {
      "reputation": { "officer": -10 },
      "company_needs": { "Readiness": -5 },
      "text": "A sentry spotted you and the alarm was raised. You returned with nothing but a close call."
    },
    "decline": {
      "reputation": { "officer": -15 },
      "text": "The captain's eyes narrow. 'I asked for a scout, not a camp follower.'"
    }
  }
}
```

**Tag Reference for Strategic Matching**:

Based on `strategic_context_config.json`, use these tags to ensure orders match strategic contexts:

| Context (RP Name) | Description | Appropriate Tags | Inappropriate Tags |
|-------------------|-------------|------------------|-------------------|
| Grand Campaign | Allied hosts ride together | assault, tactical, combat, preparation, intel | leisure, social, camp_routine |
| Last Stand | Realm survival at stake | defense, urgent, critical, combat | leisure, social, training, camp_routine |
| Harrying the Lands | Burning enemy stores | raid, supply, scout, tactical | defense, siege |
| Mustering for War | Gathering strength | preparation, intel, supply, training | urgent, combat |
| Pressing the Advantage | War is going well | assault, siege, tactical, scout | defense, urgent |
| Holding the Line | War is going poorly | defense, patrol, preparation, intel | assault, raid |
| Riding the Marches | Peaceful patrol | patrol, scout, defense | assault, siege, urgent |
| Camp Life | No pressing matters | camp_routine, training, social, patrol | (none) |

**Tag Update Checklist**:
- [ ] Review all 30-40 existing orders
- [ ] Add appropriate strategic tags to each
- [ ] Ensure combat orders avoid camp_routine/leisure/social
- [ ] Ensure leisure orders have camp_routine/social tags
- [ ] Ensure preparation orders have preparation/intel tags
- [ ] Test that orders feel appropriate in each strategic context

#### Data Files Summary

**New Files Created**:
1. `ModuleData/Enlisted/strategic_context_config.json` (~150 lines)
   - War stance thresholds and weights
   - Strategic context definitions (8 contexts)
   - Settlement value calculation parameters
   - Coordination detection parameters
   - Company needs prediction templates

2. `ModuleData/Enlisted/Languages/strategic_context_strings.xml` (~30 strings)
   - War stance descriptions
   - Strategic context labels
   - Strategic value labels
   - Report section headers

**Updated Files**:
3. `ModuleData/Enlisted/Orders/*.json` (existing orders)
   - Add strategic tags to all 30-40 orders
   - Ensure tags match config definitions
   - No structural changes, just additional tags field

**Total New Content**: ~200 lines JSON + ~30 lines XML

#### Implementation Notes

**Data-Driven Design**:
- ✅ All thresholds in JSON config (no hardcoded values)
- ✅ All strategic context definitions in JSON
- ✅ All text strings in XML (localizable)
- ✅ Order strategic matching via tags (JSON)
- ✅ Easy to balance/tune without code changes

**Writing Style (Light Bannerlord RP)**:
- ✅ Period-appropriate language ("the host gathers" not "forces are mobilizing")
- ✅ Soldier's perspective ("Hard days are coming" not "Extended operations expected")
- ✅ Medieval vocabulary ("fortnight", "marches", "host", "banner")
- ✅ Evocative but brief ("Fortune favors our banners" not "War situation favorable")
- ✅ No modern military jargon (no "ops", "intel", "assets" in player-facing text)

**What We're Using from AI Research**:
1. **MobileParty.Objective** (Defensive/Aggressive/Neutral) - Read to understand lord intent
2. **Settlement strategic value** - Calculate from neighbor control, position
3. **War situation assessment** - Territory ratio, strength ratio, economic score
4. **Coordinated operations detection** - Check if multiple armies targeting same settlement
5. **Strategic context interpretation** - Combine tactical action + war stance = strategic meaning

**What We're NOT Doing**:
- ❌ Not modifying vanilla AI behavior
- ❌ Not creating strategic planning layer for NPC lords
- ❌ Not implementing HTN planning
- ❌ Not implementing influence mapping
- ❌ Not adding new features

**What We ARE Doing**:
- ✅ Reading vanilla AI data to understand strategic context
- ✅ Using that context to make Enlisted features smarter
- ✅ Making orders, events, reports feel connected to actual strategy
- ✅ Explaining the strategic WHY to the player
- ✅ All configuration in JSON/XML (easily moddable/tunable)

#### Deliverables

**Configuration Files**:
- [ ] `ModuleData/Enlisted/strategic_context_config.json` created (~150 lines)
- [ ] `ModuleData/Enlisted/Languages/strategic_context_strings.xml` created (~30 strings)
- [ ] All order JSON files updated with strategic tags

**Code Enhancements**:
- [ ] ArmyContextAnalyzer enhanced with strategic assessment methods (loads from config)
- [ ] Config model classes added (StrategicContextConfig, etc.)
- [ ] Order selection uses strategic scoring (tag matching from config)
- [ ] Order notifications include strategic context (descriptions from config)
- [ ] Reports show strategic situation summary (text from XML/config)
- [ ] Company needs predictions based on strategic plans (templates from config)

**Integration**:
- [ ] All text strings loaded from XML (localizable)
- [ ] All thresholds/weights loaded from JSON (tunable)
- [ ] All strategic context definitions in JSON (moddable)
- [ ] All enhancements use READ-ONLY AI data (no modifications to vanilla)

#### Testing

**Configuration Loading**:
- [ ] strategic_context_config.json loads without errors
- [ ] strategic_context_strings.xml loads without errors
- [ ] All 8 strategic contexts defined in config
- [ ] All config values parse correctly (floats, ints, strings)
- [ ] Missing config keys handled gracefully (fallbacks work)

**Strategic Assessment**:
- [ ] War stance correctly reflects actual war situation (test in various scenarios)
- [ ] Strategic context matches lord's actual behavior
- [ ] Settlement strategic value calculated correctly (border, fortification, etc.)
- [ ] Coordinated operation detection works (multiple armies targeting same settlement)
- [ ] War stance updates when faction situation changes

**Order Selection**:
- [ ] Orders feel appropriate to strategic situation (90%+ appropriate)
- [ ] No inappropriate orders during critical moments (verified with config tags)
- [ ] Strategic tag matching works (preparation orders during PreparingOffensive, etc.)
- [ ] Weighted random selection provides variety while favoring appropriate orders
- [ ] Orders reflect all tags: role, context, AND strategic appropriateness

**Reports & Notifications**:
- [ ] Reports explain WHY lord is doing what they're doing
- [ ] Strategic summaries update when war situation changes
- [ ] Order notifications include strategic context from config
- [ ] XML strings load correctly (English, other languages if added)
- [ ] Placeholder replacement works ({target} → actual settlement name)

**Predictions**:
- [ ] Company needs predictions are accurate (±1-2 days)
- [ ] Prediction templates load from config
- [ ] Upcoming needs list is relevant to strategic context
- [ ] Recommended actions are helpful and specific

**Edge Cases**:
- [ ] Works when lord has no target (falls back to RoutineOperations)
- [ ] Works during peacetime (WarStance.Neutral)
- [ ] Works with minor factions (appropriate fallbacks)
- [ ] Works if config file is missing/corrupted (safe defaults)

**Writing Style (RP)**:
- [ ] All player-facing text uses period-appropriate language
- [ ] No modern military jargon in notifications/reports
- [ ] Context descriptions read naturally ("The banners are gathering..." not "Multi-army offensive initiated")
- [ ] Predictions feel like soldier's advice ("Fill your quiver, mend your mail...")
- [ ] Settlement descriptions evocative ("a fortress of great renown" not "critical strategic asset")

#### Acceptance Criteria
- Orders selected feel strategically appropriate 90%+ of the time
- Reports provide meaningful strategic context
- Player understands WHY orders are being given
- Company need predictions help player prepare
- No performance impact (all calculations cached/infrequent)
- Strategic context enhances immersion without feeling "meta" or omniscient

---

### Phase 6: Event Migration
**Duration**: Week 7-8 (~20 hours)
**Goal**: Migrate events from schedule-based to role-based

**Reference Documentation**: `docs/Features/Technical/event-system-schemas.md` (Schema v2.0)

#### Tasks

**6.1: Tag All Events**

Add role/context tags to all 80+ existing events:

**Role Tags:**
- `scout` - Reconnaissance, intelligence
- `officer` - Leadership, command
- `medic` - Medical, triage
- `operative` - Covert, criminal
- `nco` - Training, mentoring
- `soldier` - Generic (fallback)

**Context Tags:**
- `war` - Active warfare
- `siege` - Siege operations
- `peace` - Peacetime
- `town` - In settlement
- `outdoor` - Field/campaign
- `camp` - Camp/bivouac

Example tagged event:
```json
{
  "id": "lance_patrol_ambush",
  "tags": ["scout", "soldier", "war", "outdoor"],
  "requirements": {
    "tier_min": 2
  }
}
```

**6.2: Remove Schedule Triggers**

Old:
```json
{
  "triggers": {
    "schedule": ["morning", "afternoon"]
  }
}
```

New:
```json
{
  "triggers": {
    "role_based": true
  }
}
```

**6.3: Create Role-Based Event Trigger**
```csharp
// File: src/Features/Events/RoleBasedEventTrigger.cs

public class RoleBasedEventTrigger : CampaignBehaviorBase
{
    public override void RegisterEvents()
    {
        CampaignEvents.HourlyTickEvent.AddListener(this, OnHourlyTick);
    }
    
    private void OnHourlyTick()
    {
        // Check escalation events first (always priority)
        CheckEscalationEvents();
        
        // Random role-based events (5% chance per hour = ~1-2 per day)
        if (MBRandom.RandomFloat < 0.05f)
        {
            TryFireRoleBasedEvent();
        }
    }
    
    private void TryFireRoleBasedEvent()
    {
        var status = EnlistedStatusManager.Instance;
        var role = status.GetPrimaryRole();
        var context = GetCampaignContext();
        
        // Get event pool for role + context
        var eventPool = GetEventPoolForRoleAndContext(role, context);
        
        if (eventPool.Count == 0)
        {
            // Fallback to generic soldier events
            eventPool = GetEventPoolForRole("Soldier", context);
        }
        
        // Filter by requirements (tier, traits, reputation)
        var eligibleEvents = FilterEventsByRequirements(eventPool);
        
        if (eligibleEvents.Count > 0)
        {
            var selectedEvent = eligibleEvents.GetRandomElement();
            FireEvent(selectedEvent);
        }
    }
    
    private List<EventDefinition> GetEventPoolForRoleAndContext(
        string role, 
        string context)
    {
        var events = EventCatalog.GetEvents();
        
        return events.Where(e => 
            e.Tags.Contains(role.ToLower()) && 
            e.Tags.Contains(GetContextTag(context))
        ).ToList();
    }
}
```

#### Deliverables
- [x] All 80+ events tagged with role/context
- [x] Schedule triggers removed
- [x] Role-based event trigger implemented
- [x] Event pools balanced (all role/context combinations covered)

#### Testing
- [ ] Events fire at appropriate frequency (~1-2 per day)
- [ ] Role-based routing works correctly
- [ ] Context affects event pool selection
- [ ] No long dry spells (>3 days without event)

---

### Phase 7: Testing & Polish
**Duration**: Week 8-9 (~15 hours)
**Goal**: Bug fixes, balance, documentation

#### Tasks

**7.1: Full Playthrough Testing**
- [ ] T1-T3 progression (group tasks, learning)
- [ ] T4-T6 progression (solo missions, team leadership)
- [ ] T7-T9 progression (tactical command, strategic ops)
- [ ] Role transitions (Soldier → Scout → Officer)
- [ ] Context changes (Peace → War → Siege)

**7.2: Balance Testing**
- [ ] Order frequency feels right (not too many/few)
- [ ] Company needs stay in reasonable range (30-80)
- [ ] Reputation progression is achievable
- [ ] Trait progression feels earned
- [ ] Order difficulty matches tier

**7.3: Performance Testing**
- [ ] Menu refresh <50ms
- [ ] Hourly tick <1ms
- [ ] Event selection <1ms
- [ ] Save/load <2s

**7.4: Edge Case Testing**
- [ ] Save/load with active order
- [ ] Order expiration during battle
- [ ] Multiple declined orders
- [ ] Critical company needs
- [ ] Lord dies/changes

**7.5: Documentation**
- [ ] Update all code comments
- [ ] Add XML documentation
- [ ] Update feature specs
- [ ] Create player guide
- [ ] Create modder guide (for custom orders/events)

#### Deliverables
- [x] Stable, balanced system
- [x] All edge cases handled
- [x] Complete documentation
- [x] Player-facing guide
- [x] Modder-facing guide

---

## Data File Organization

```
ModuleData/Enlisted/
├── strategic_context_config.json (NEW in Phase 5.6 - ~150 lines)
│   ├── War stance thresholds & weights
│   ├── 8 strategic context definitions
│   ├── Settlement value calculation params
│   ├── Coordination detection config
│   └── Company needs prediction templates
├── Orders/
│   ├── orders_t1_t3.json (Group orders, 10-12 orders) [UPDATED: strategic tags added]
│   ├── orders_t4_t6.json (Solo/NCO orders, 12-15 orders) [UPDATED: strategic tags added]
│   └── orders_t7_t9.json (Tactical/Strategic orders, 8-12 orders) [UPDATED: strategic tags added]
├── Events/
│   ├── events_scout.json (Scout role events, ~15 events)
│   ├── events_officer.json (Officer role events, ~12 events)
│   ├── events_medic.json (Medic role events, ~10 events)
│   ├── events_operative.json (Operative role events, ~8 events)
│   ├── events_nco.json (NCO role events, ~10 events)
│   ├── events_soldier.json (Generic soldier events, ~25 events)
│   └── events_escalation.json (Escalation/crisis events, ~10 events)
└── Languages/
    ├── enlisted_strings.xml (All localized text)
    └── strategic_context_strings.xml (NEW in Phase 5.6 - ~30 strings)
        ├── War stance descriptions
        ├── Strategic context labels
        └── Strategic value labels
```

**Phase 5.6 Additions**:
- 1 new JSON config file (~150 lines)
- 1 new XML strings file (~30 strings)
- Strategic tags added to all existing order JSON files (no structural changes)

---

## Key Integration Points

### Traits → Orders
- **Role detection** (from traits) determines which orders you get
- **Trait requirements** gate advanced orders (e.g., need Scout trait 5+ for scout orders)
- **Order success** awards trait XP (via `TraitLevelingHelper.AddTraitXp`)
- **Trait progression** unlocks better orders (higher requirements met)

### Orders → Company Needs (CompanyNeedsState)
- **Success/failure** affects Readiness, Morale, Supplies, Equipment, Rest
- **Access pattern**: `ScheduleBehavior.Instance.CompanyNeeds` (property, not static)
- **Manager**: `CompanyNeedsManager` is a static class with helper methods
- **Company needs** gate equipment access (Supplies <30% = blocked)
- **Low needs** trigger warning messages and potential events (via `CompanyNeedsManager.CheckCriticalNeeds`)
- **Order magnitude** scales with rank (T1-T3 = ±3-8, T7-T9 = ±10-30)

### Orders → Reputation
- **Success** builds Officer/Lord/Soldier reputation
- **Failure** damages reputation
- **High reputation** unlocks better orders and provides buffs
- **Low reputation** limits opportunities and increases penalties

### Reputation → Orders
- **High Officer rep** (60+) = more responsibility, better tactical orders
- **High Lord rep** (60+) = strategic missions directly from lord
- **Low rep** (<30) = basic tasks only, no advanced orders
- **Discharge risk** at 5+ order declines

### Native Trait Integration
- **Uses native traits**: Commander, Surgeon, ScoutSkills, RogueSkills, etc.
- **Uses native API**: `Hero.GetTraitLevel()`, `TraitLevelingHelper.AddTraitXp()`
- **No custom traits**: Leverages Bannerlord's existing personality/skill traits
- **See traits-identity-system.md**: For complete trait integration details

---

## Success Metrics

### Performance
- Menu refresh <50ms
- Hourly tick <1ms
- Event selection <1ms
- Save/load <2s

### Engagement
- Order completion rate >80%
- Order decline rate <10%
- Event interaction rate >70%
- Role transition rate >50% (players try multiple roles)

### Balance
- Company needs stay in 30-80 range most of time
- Reputation progression achievable (reach 60+ by T6)
- Trait progression feels earned (10+ by T6, 15+ by T9)
- No content droughts (event every 1-2 days average)

### Stability
- No crashes in 10+ hour playthrough
- Save/load preserves all state correctly
- No infinite loops or deadlocks
- No memory leaks

---

## Code Metrics

### Deletion Summary
- Schedule System: -1700 lines
- Duties System: -800 lines
- Lance System: -1200 lines
- Custom Camp UI: -1800 lines
- **Total Deleted: -5500 lines**

### Addition Summary
- Orders System: +300 lines
- Expanded Reputation: +50 lines
- Native Game Menu: +300 lines
- Trait Integration: +150 lines
- Role-Based Events: +200 lines
- Order Catalog (JSON): +1500 lines
- **Total Added: +2500 lines**

### Net Change
**-3000 lines (65% reduction)**

**More importantly:**
- Zero custom UI (all native Game Menu)
- No ViewModels or XML
- Simpler, faster, more maintainable
- Easier for modders to extend

---

## Timeline Summary

| Phase | Duration | Effort | Deliverable |
|-------|----------|--------|-------------|
| **Phase 0** | 1 hour | 1 hour | Pre-Implementation Audit |
| **Phase 1** | Week 1-2 | 20 hours | Foundation (delete old, expand reputation) ✅ COMPLETE |
| **Phase 2** | Week 2-3 | 15 hours | Native Game Menu |
| **Phase 3** | Week 3-4 | 15 hours | Trait Integration |
| **Phase 4** | Week 4-6 | 25 hours | Orders System |
| **Phase 5** | Week 6 | 10 hours | Company Need Integration |
| **Phase 5.5** | Week 6 | 5 hours | News System Enhancement ✅ COMPLETE |
| **Phase 5.6** | Week 6-7 | 15 hours | Strategic Context Enhancement (NEW) |
| **Phase 6** | Week 7-8 | 20 hours | Event Migration |
| **Phase 7** | Week 8-9 | 15 hours | Testing & Polish |

**Total: 141 hours (~3.5 weeks full-time, or 7-9 weeks part-time)**

**Recommended Approach**: Implement phases sequentially, test after each phase, deploy incrementally.

**Phase 5.6 Note**: This new phase applies AI strategic behavior research (see `docs/research/ai-strategic-behavior-analysis-v2.md`) to make existing features strategically aware. It reads vanilla AI data (war stance, objectives, coordination) to make Orders, Events, and Reports feel connected to actual strategic plans.

---

## Future Enhancements (Post-Launch)

### Advanced Features
- **Conditional order chains**: Next order depends on previous outcome
- **Multi-objective orders**: Complex missions with multiple success conditions
- **Squad orders**: Lead your retinue (T7+) on group orders
- **Dynamic difficulty**: Order skill checks adjust based on player level
- **Timed orders**: Must respond within X hours or auto-decline
- **Order modding API**: Community-created orders
- ~~**Strategic context** ~~: Moved to Phase 5.6 (being implemented)

### Content Expansion
- **Culture-specific orders**: Vlandian vs Battanian vs Aserai missions
- **Siege specialist orders**: Engineer role during sieges
- **Diplomatic orders**: Negotiations, espionage (T8+)
- **Training orders**: Mentor new recruits (T6+ NCO)
- **Supply chain orders**: Logistics, procurement (Steward role)

### Advanced Strategic AI (Separate Enhancement)

Phase 5.6 makes Enlisted understand vanilla AI strategy. Future work could go further:

- **Faction Strategic Objectives System**: Create coordinated objectives for NPC factions
- **Player Threat Response**: Make AI specifically counter player as major threat
- **HTN Multi-Step Planning**: Give AI lords coherent multi-turn campaign plans
- **Influence Mapping**: Spatial reasoning for strategic positioning
- **Risk Assessment**: AI evaluates risk vs reward in decisions

**Note**: These would modify vanilla AI behavior (see `docs/research/ai-strategic-behavior-analysis-v2.md` for detailed implementation plan). Phase 5.6 only reads vanilla AI data without modifying it.

---

## Related Documentation

- **`traits-identity-system.md`**: Deep dive on trait integration and identity formation
- **`macro-schedule-simplification.md`**: Details on orders system and UI simplification
- **`quartermaster-equipment-quality.md`**: Equipment access and company supply mechanics
- **`story-blocks-master-reference.md`**: Existing events (to be migrated)

---

## Summary

This master plan unifies three major transformations plus strategic intelligence:

1. **Emergent Identity** - Traits + Skills + Reputation replace prescribed systems
2. **Orders from Command** - Explicit directives replace passive duties
3. **Native Interface** - Text-based Game Menu replaces custom UI
4. **Strategic Awareness** (NEW) - AI research makes features understand strategic context

Together, they create a **simpler, deeper, more authentic** military experience where:
- Identity emerges from how you play
- Missions have clear stakes and strategic purpose
- Your actions affect the entire company
- Rank progression feels earned
- Interface is fast and clean
- Orders, events, and reports reflect actual strategic plans (not random tasks)
- You understand WHY the lord is doing what they're doing

**Result**: 65% less code, 100% more focused gameplay, strategically coherent experience.

### Phase 5.6 Impact (Strategic Context Enhancement)

By reading Bannerlord's AI strategic data, we make existing features smarter:

**Before (Tactical Only)**:
- Order: "Scout the area" (generic task)
- Report: "Lord besieging Pravend" (what)
- Event: Random gambling event during siege

**After (Strategically Aware)**:
- Order: "Scout approaches to Pravend - Lord planning offensive as part of coordinated faction campaign. Your intelligence will inform assault plans." (strategic purpose)
- Report: "Strategic offensive underway. Lord coordinating with allied armies to capture Pravend (critical chokepoint). Expect sustained operations." (why + what + outlook)
- Event: Pre-battle tension event (contextually appropriate)

**Implementation**: Phase 5.6 adds ~15 hours to read vanilla AI data and use it to enhance existing ArmyContextAnalyzer, OrderCatalog, and News systems. Zero new features, just makes current features feel connected to actual strategy.
