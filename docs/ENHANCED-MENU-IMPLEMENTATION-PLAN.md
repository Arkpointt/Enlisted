# Enhanced Military Menu Implementation Plan
**Based on SAS Decompiled Analysis + Our Enhanced Architecture**

**Status**: Implementation Plan  
**Target**: SAS-style comprehensive menu with our enhanced features  
**Branch**: `feature/enhanced-menu`

## 🎯 **Implementation Goal**

Create a **professional military interface that matches SAS visual style and functionality** while integrating with our **superior configuration-driven duties system, formation specializations, and 1-year progression**.

## 📊 **SAS Menu Analysis Results**

### **🔍 Core SAS Menu Structure** (from decompiled `Test.cs`):

**Main Menu (`party_wait`)** - Line 862:
```csharp
campaignStarter.AddWaitGameMenu("party_wait", 
    "Party Leader: {PARTY_LEADER}\n{PARTY_TEXT}", 
    new OnInitDelegate(this.wait_on_init), 
    new OnConditionDelegate(this.wait_on_condition), 
    null, 
    new OnTickDelegate(this.wait_on_tick), 
    3, 0, 0f, 0, null);
```

**Status Information Display** (`updatePartyMenu` - Lines 2836-2957):
```
Party Leader: Temion
Party Objective: Travelling to Poros  
Enlistment Time: Summer 1, 1084
Enlistment Tier: 1
Formation: Infantry  
Wage: 3💰
Current Experience: 21
Next Level Experience: 600
When not fighting: You are currently assigned to perform grunt work. Most tasks are unpleasant, tiring or involve menial labor. (Passive Daily Athletics XP)
```

**Interactive Services** (Lines 864-1280):
- Visit Weaponsmith → Equipment selection system
- Train with troops → Arena training missions
- Battle Commands → Formation command preferences  
- Talk to... → Party member conversations
- Show reputation with factions → Faction relationship display
- Ask for a different assignment → Dialog-based role changes

## 🚀 **Enhanced Implementation Plan**

### **Phase 1: SAS-Style Status Display** ⭐ **HIGH PRIORITY**

**Goal**: Match SAS information density and format while integrating our enhanced progression system

**Implementation** (`EnlistedMenuBehavior.cs` enhancement):
```csharp
private void RefreshEnlistedStatusDisplay()
{
    var sb = new StringBuilder();
    var enlistment = EnlistmentBehavior.Instance;
    var duties = EnlistedDutiesBehavior.Instance;
    
    if (!enlistment?.IsEnlisted == true)
    {
        MBTextManager.SetTextVariable("ENLISTED_STATUS_TEXT", "You are not currently enlisted.");
        return;
    }

    var lord = enlistment.CurrentLord;
    
    // SAS-style header with party leader
    sb.AppendLine($"Party Leader: {lord.Name}");
    
    // Dynamic objectives (SAS pattern - army vs party)
    if (lord.PartyBelongedTo.Army != null)
    {
        sb.AppendLine($"Army Objective: {GetArmyObjective(lord.PartyBelongedTo.Army)}");
    }
    else
    {
        sb.AppendLine($"Party Objective: {GetPartyObjective(lord.PartyBelongedTo)}");
    }
    
    // Enhanced military progression (our system + SAS display format)
    sb.AppendLine($"Enlistment Time: {GetEnlistmentTimeDisplay()}");
    sb.AppendLine($"Enlistment Tier: {enlistment.EnlistmentTier}");
    sb.AppendLine($"Formation: {GetFormationDisplayName()}");
    sb.AppendLine($"Wage: {CalculateWageDisplay()}💰");
    sb.AppendLine($"Current Experience: {enlistment.EnlistmentXP}");
    sb.AppendLine($"Next Level Experience: {GetNextTierXP()}");
    
    // Enhanced assignment description (our duties system)
    sb.AppendLine($"When not fighting: {GetEnhancedAssignmentDescription()}");

    MBTextManager.SetTextVariable("ENLISTED_STATUS_TEXT", sb.ToString());
}

// SAS-style army/party objective detection
private string GetArmyObjective(Army army)
{
    var leaderParty = army.LeaderParty;
    if (leaderParty.SiegeEvent != null)
        return $"Besieging {leaderParty.SiegeEvent.BesiegedSettlement.Name}";
    if (leaderParty.MapEvent != null)
        return $"Engaging enemy forces";
    if (leaderParty.TargetSettlement != null)
        return $"Travelling to {leaderParty.TargetSettlement.Name}";
    return "Army operations";
}

// Enhanced assignment description (our configuration-driven system)
private string GetEnhancedAssignmentDescription()
{
    var duties = EnlistedDutiesBehavior.Instance;
    var activeDuties = duties?.GetActiveDutiesDisplay() ?? "None assigned";
    var officerRole = duties?.GetCurrentOfficerRole() ?? "";
    
    if (activeDuties == "None assigned")
    {
        return "You have no current assigned duties. You spend your idle time with fellow soldiers.";
    }
    
    var description = $"You are currently assigned: {activeDuties}.";
    
    // Add officer role benefits (our enhanced system)
    if (!string.IsNullOrEmpty(officerRole))
    {
        description += $" As party {officerRole.ToLower()}, your {GetOfficerSkillName(officerRole)} skill affects the entire party.";
    }
    
    // Add passive XP information
    var passiveXP = duties?.GetDailySkillXP() ?? "";
    if (!string.IsNullOrEmpty(passiveXP))
    {
        description += $" (Passive Daily {passiveXP})";
    }
    
    return description;
}
```

### **Phase 2: Interactive Military Services** ⭐ **HIGH PRIORITY**

**Goal**: Implement SAS-style interactive services adapted to our enhanced duties system

**Services to Add**:

**1. Equipment Management** (SAS "Visit Weaponsmith" equivalent):
```csharp
starter.AddGameMenuOption("enlisted_status", "enlisted_equipment_request",
    "Request equipment upgrade ({EQUIPMENT_COST}💰)",
    IsEquipmentRequestAvailable,
    OnEquipmentRequestSelected, false, 2);

private bool IsEquipmentRequestAvailable(MenuCallbackArgs args)
{
    var cost = CalculateEquipmentUpgradeCost();
    MBTextManager.SetTextVariable("EQUIPMENT_COST", cost.ToString());
    
    args.IsEnabled = Hero.MainHero.Gold >= cost;
    if (!args.IsEnabled)
    {
        args.Tooltip = new TextObject($"You need {cost} gold for equipment upgrades.");
    }
    
    return EnlistmentBehavior.Instance?.IsEnlisted == true;
}
```

**2. Time-Based Military Activities** (SAS "Skills Tent" equivalent):
```csharp
starter.AddGameMenuOption("enlisted_status", "enlisted_activities",
    "Military training activities",
    IsActivitiesAvailable,
    OnActivitiesSelected, false, 3);

// New menu: enlisted_activities
starter.AddWaitGameMenu("enlisted_activities",
    "You are preparing for military duties...\n{ACTIVITY_STATUS}",
    new OnInitDelegate(OnActivitiesInit),
    new OnConditionDelegate(IsEnlistedCondition),
    null, null,
    GameMenu.MenuAndOptionType.WaitMenuShowOnlyProgressOption,
    GameOverlays.MenuOverlayType.None, 0f, 
    GameMenu.MenuFlags.None, null);

// Hunt and forage (16 hours) - Enhanced for our duties system
starter.AddGameMenuOption("enlisted_activities", "activity_forage",
    "Hunt and fetch supplies (16 hours)",
    args => CanPerformActivity("foraging"),
    args => StartMilitaryActivity("foraging", 16, "Scouting", "party_food"),
    false, 1);

// Train with troops (8 hours) - Enhanced for our formation system  
starter.AddGameMenuOption("enlisted_activities", "activity_training",
    "Train with troops (8 hours)",
    args => CanPerformActivity("training"),
    args => StartMilitaryActivity("training", 8, "Leadership", "troop_xp"),
    false, 2);

// Volunteer scout patrol (24 hours) - Enhanced for our duties system
starter.AddGameMenuOption("enlisted_activities", "activity_scouting", 
    "Volunteer for scout patrol (24 hours)",
    args => CanPerformActivity("scouting"),
    args => StartMilitaryActivity("scouting", 24, "Scouting", "party_vision"),
    false, 3);
```

**3. Advanced Assignment Management** (SAS dialog-based role changes):
```csharp
// Enhanced assignment conversation using our duties system
private void AddAdvancedDutiesDialog(CampaignGameStarter starter)
{
    starter.AddPlayerLine("enlisted_request_duty_change",
        "enlisted_service_options",
        "enlisted_duty_change_response",
        "I would like to discuss my current duties.",
        CanRequestDutyChange, null, 100);

    // Dynamic duty options based on our configuration
    foreach (var duty in GetAvailableDuties())
    {
        starter.AddPlayerLine($"enlisted_request_{duty.Id}",
            "enlisted_duty_options",
            "enlisted_duty_approval",
            $"I wish to serve as {duty.DisplayName}.",
            () => CanAssignDuty(duty),
            () => RequestDutyAssignment(duty), 100);
    }
    
    // Lord approval system (SAS pattern with our enhancements)
    // - Check skill requirements from our configuration
    // - Check relationship requirements 
    // - Allow bribery for difficult assignments
    // - Update our duties system instead of hardcoded assignment enum
}
```

### **Phase 3: Battle Integration Enhancement** ⭐ **MEDIUM PRIORITY**

**Goal**: Add SAS-style battle encounter options with our formation system integration

**Battle Encounter Options** (from `EncounterMenuPatch.cs`):
```csharp
// Add to existing encounter menus when enlisted
starter.AddGameMenuOption("encounter", "enlisted_wait_reserve",
    "Wait in reserve",
    IsWaitInReserveAvailable,
    OnWaitInReserveSelected, true, -1);

private bool IsWaitInReserveAvailable(MenuCallbackArgs args)
{
    var enlistment = EnlistmentBehavior.Instance;
    if (!enlistment?.IsEnlisted == true) return false;
    
    var lordParty = enlistment.CurrentLord?.PartyBelongedTo;
    if (lordParty?.Army == null || lordParty.MapEvent == null) return false;
    
    // SAS pattern: Require 100+ troops in army for reserve option
    var armySize = lordParty.Army.TotalStrength;
    if (armySize < 100)
    {
        args.IsEnabled = false;
        args.Tooltip = new TextObject("Army too small to wait in reserve (need 100+ troops).");
    }
    
    return armySize >= 100;
}

// Enhanced formation-specific battle commands display
starter.AddGameMenuOption("enlisted_status", "battle_commands",
    "Battle Commands: {COMMAND_MODE}",
    IsBattleCommandsAvailable,
    OnBattleCommandsToggle, false, 7);
```

### **Phase 4: Advanced Status Tracking** ⭐ **LOW PRIORITY**

**Goal**: Comprehensive service record and reputation tracking

**Service Record Enhancement**:
```csharp
// Detailed service history (replacing placeholder)
private void RefreshServiceRecordDisplay()
{
    var sb = new StringBuilder();
    
    // Experience breakdown by source
    sb.AppendLine("Experience Breakdown:");
    sb.AppendLine($"  Daily Service: {GetDailyServiceXP()} XP");
    sb.AppendLine($"  Duty Bonuses: {GetDutyBonusXP()} XP");
    sb.AppendLine($"  Battle Participation: {GetBattleXP()} XP");
    sb.AppendLine();
    
    // Enhanced relationship tracking (our system)
    sb.AppendLine("Military Relationships:");
    sb.AppendLine($"  Lord {enlistment.CurrentLord.Name}: {GetLordRelationship()} (Service: {GetServiceDays()} days)");
    sb.AppendLine($"  Faction {faction.Name}: {GetFactionRelationship()}");
    sb.AppendLine();
    
    // Battle history with formation tracking
    sb.AppendLine("Combat Record:");
    sb.AppendLine($"  Battles Participated: {GetBattleCount()}");
    sb.AppendLine($"  Formation: {GetFormationExperience()}");
    sb.AppendLine($"  Officer Duties Performed: {GetOfficerDutiesHistory()}");
    
    MBTextManager.SetTextVariable("SERVICE_RECORD_TEXT", sb.ToString());
}
```

## 💻 **Detailed Implementation Tasks**

### **Task 1: Enhanced Status Display Implementation**

**File**: `src/Features/Interface/Behaviors/EnlistedMenuBehavior.cs`

**Changes Required**:
1. **Replace simple status with SAS-style comprehensive display**
2. **Add army/party objective detection logic**
3. **Integrate formation display with culture-specific names**  
4. **Add enhanced assignment descriptions using our duties configuration**
5. **Include visual formatting (gold coins, proper spacing)**

**New Methods to Add**:
```csharp
private string GetArmyObjective(Army army)           // Army operation detection
private string GetPartyObjective(MobileParty party) // Party operation detection  
private string GetEnlistmentTimeDisplay()           // Formatted enlistment date
private string GetFormationDisplayName()            // Culture-specific formation names
private string GetWageDisplay()                     // Wage with bonuses breakdown
private string GetEnhancedAssignmentDescription()   // Our duties system integration
```

### **Task 2: Time-Based Military Activities**

**New File**: `src/Features/Interface/Behaviors/MilitaryActivitiesBehavior.cs`

**Implementation**:
```csharp
public sealed class MilitaryActivitiesBehavior : CampaignBehaviorBase
{
    private Dictionary<string, CampaignTime> _activityCooldowns = new();
    
    // Time-based activities system
    public void StartMilitaryActivity(string activityType, float hours, string targetSkill, string benefit)
    {
        // SAS pattern: Use wait menu for time passage
        Campaign.Current.TimeControlMode = CampaignTimeControlMode.StoppableFastForward;
        
        // Our enhancement: Integrate with duties system for bonuses
        var bonus = GetDutyBonus(activityType);
        var finalHours = hours * (1f - bonus);
        
        // Schedule completion with skill XP and benefit
        ScheduleActivityCompletion(activityType, finalHours, targetSkill, benefit);
    }
    
    private void ScheduleActivityCompletion(string activity, float hours, string skill, string benefit)
    {
        // Register for hourly tick to track progress
        // Apply benefits when activity completes
        // Integrate with our duties system for enhanced rewards
    }
}
```

### **Task 3: Interactive Assignment Management**

**File**: `src/Features/Conversations/Behaviors/EnlistedDialogManager.cs`

**Enhanced Dialog Integration**:
```csharp
// SAS-style assignment dialog with our duties system
private void AddAdvancedDutiesManagementDialogs(CampaignGameStarter starter)
{
    // Main duties discussion entry
    starter.AddPlayerLine("enlisted_discuss_duties_advanced",
        "enlisted_service_options", 
        "enlisted_duties_main",
        "I would like to change my current duties.",
        CanDiscussDuties, null, 120);
    
    // Dynamic duty options from our configuration
    var availableDuties = GetConfiguredDuties();
    foreach (var duty in availableDuties)
    {
        AddDutyRequestDialog(starter, duty);
    }
}

private void AddDutyRequestDialog(CampaignGameStarter starter, DutyDefinition duty)
{
    starter.AddPlayerLine($"request_duty_{duty.Id}",
        "enlisted_duties_options",
        $"duty_response_{duty.Id}",
        $"I wish to serve as {duty.DisplayName}.",
        () => CanRequestSpecificDuty(duty),
        null, 100);
    
    // SAS approval pattern with our configuration requirements
    starter.AddDialogLine($"approve_duty_{duty.Id}",
        $"duty_response_{duty.Id}",
        "close_window",
        GetDutyApprovalResponse(duty),
        () => CheckDutyRequirements(duty),
        () => AssignDutyThroughDialog(duty), 100);
}

private bool CheckDutyRequirements(DutyDefinition duty)
{
    // Our enhanced requirements system (from JSON configuration)
    var skillRequired = duty.UnlockConditions?.SkillRequired ?? 0;
    var relationshipRequired = duty.UnlockConditions?.RelationshipRequired ?? 0;
    var tierRequired = duty.MinTier;
    
    var hasSkill = GetRelevantSkillValue(duty) >= skillRequired;
    var hasRelationship = GetLordRelationship() >= relationshipRequired;
    var hasTier = EnlistmentBehavior.Instance.EnlistmentTier >= tierRequired;
    
    return hasSkill && hasRelationship && hasTier;
}
```

### **Task 4: Advanced Equipment System Integration**

**File**: `src/Features/Equipment/Behaviors/EnhancedEquipmentManager.cs` (NEW)

**SAS Equipment Selector Adaptation**:
```csharp
public sealed class EnhancedEquipmentManager : CampaignBehaviorBase
{
    // SAS-inspired equipment selection using TaleWorlds inventory system
    public void ShowEquipmentSelectionMenu(EquipmentIndex slot)
    {
        var availableItems = GetCultureAppropriateEquipment(slot);
        var itemRoster = CreateEquipmentRoster(availableItems);
        
        // Use TaleWorlds inventory system (not custom Gauntlet like SAS)
        InventoryManager.Instance.OpenScreenAsReceiveItems(itemRoster, 
            new TextObject("Military Equipment"), 
            OnEquipmentSelectionComplete);
    }
    
    private void OnEquipmentSelectionComplete(List<TransferredItemInfo> items)
    {
        // Apply selected equipment using our enhanced system
        // Integrate with formation detection and officer roles
        // Calculate costs using our realistic pricing system
    }
}
```

### **Task 5: Battle Encounter Enhancement**

**File**: `src/Features/Combat/Behaviors/EnhancedEncounterBehavior.cs` (NEW)

**SAS Battle Options Integration**:
```csharp
public sealed class EnhancedEncounterBehavior : CampaignBehaviorBase
{
    public override void RegisterEvents()
    {
        CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
    }
    
    private void OnSessionLaunched(CampaignGameStarter starter)
    {
        AddEnhancedEncounterOptions(starter);
    }
    
    private void AddEnhancedEncounterOptions(CampaignGameStarter starter)
    {
        // SAS "Wait in reserve" equivalent
        starter.AddGameMenuOption("encounter", "enlisted_wait_reserve",
            "Wait in reserve",
            IsReserveAvailable,
            OnWaitInReserve, true, -1);
            
        // SAS "Defect to other side" with our reputation system
        starter.AddGameMenuOption("encounter", "enlisted_defect",
            "Defect to other side", 
            IsDefectionAvailable,
            OnDefection, false, -1);
    }
}
```

## 🎯 **Our Enhanced Features Integration**

### **Superior Configuration System**:
- **SAS**: Hardcoded 10 assignments in enum
- **OURS**: JSON-configured duties with dynamic loading
- **Integration**: Use our flexible duties system with SAS dialog patterns

### **Enhanced Formation System**:
- **SAS**: Basic 4-formation detection
- **OURS**: 4-formation + culture-specific names + specializations
- **Integration**: Display culture-appropriate formation names in SAS format

### **Advanced Officer Integration**:
- **SAS**: Simple assignment enum → officer role patches
- **OURS**: Configuration-driven duties → dual approach officer roles  
- **Integration**: Show officer role benefits in assignment descriptions

### **Professional Progression**:
- **SAS**: XP-based tier system
- **OURS**: 1-year realistic military progression
- **Integration**: Display progression in SAS format but with our enhanced timeline

## 📋 **Implementation Priority Order**

### **🥇 IMMEDIATE (Week 1)**:
1. **Enhanced Status Display** - Match SAS information density and format
2. **Army/Party Objective Detection** - Dynamic operational status display
3. **Assignment Description Enhancement** - Integrate our duties system with SAS format

### **🥈 HIGH PRIORITY (Week 2)**:  
1. **Time-Based Activities System** - Military training activities with time costs
2. **Equipment Request System** - SAS-style equipment management with realistic pricing
3. **Advanced Assignment Dialogs** - SAS dialog patterns with our configuration system

### **🥉 MEDIUM PRIORITY (Week 3)**:
1. **Battle Encounter Integration** - Enhanced encounter options for enlisted soldiers
2. **Service Record Enhancement** - Comprehensive tracking and display
3. **Reputation System Integration** - Faction and lord relationship display

### **🎯 LOW PRIORITY (Week 4)**:
1. **Visual Polish** - Culture-based background meshes, enhanced formatting
2. **Advanced Battle Commands** - Formation-specific command preferences
3. **Interactive Party Member System** - Talk to army members and companions

## 🎯 **EXACT SAS MENU RECREATION PLAN**

### **📊 Target Menu Format** (Matching SAS Screenshots EXACTLY):

**Main Status Menu** (`enlisted_status` enhanced):
```
Party Leader: Sir Derthert
Army Objective: Besieging Charas
Enlistment Time: Summer 15, 1084  
Enlistment Tier: 4
Formation: Imperial Legionary  
Wage: 145(+25)💰
Current Experience: 2400
Next Level Experience: 3500
When not fighting: You are currently assigned: Field Medic, Runner (2/2). As party surgeon, your Medicine skill affects the entire party. (Passive Daily Medicine, Athletics XP)
```

**Interactive Options** (SAS functionality + our enhancements):
```
🏥 Request field medical treatment
⚔️ Military training activities  
🛡️ Request equipment upgrade (45💰)
🎖️ Discuss duty assignments
⚡ Battle Commands: Formation Only
📊 Show military service record
🏆 Show reputation with factions
🚪 Ask commander for leave
```

### **Configuration Integration Strategy**

**Our Enhanced Duties → SAS Assignment Display**:
```csharp
// Map our JSON duties to SAS-style assignment descriptions
private string GetSASStyleAssignmentDescription()
{
    var duties = EnlistedDutiesBehavior.Instance;
    var activeDuties = duties?.GetActiveDutiesForDisplay() ?? new List<string>();
    
    if (activeDuties.Count == 0)
    {
        return "You have no current assigned duties. You spend your idle time with fellow soldiers.";
    }
    
    var description = $"You are currently assigned: {string.Join(", ", activeDuties)}";
    
    // Add our enhanced officer role display (SAS format)
    var officerRole = duties?.GetCurrentOfficerRole();
    if (!string.IsNullOrEmpty(officerRole))
    {
        var skillName = GetOfficerSkillName(officerRole);
        var skillValue = Hero.MainHero.GetSkillValue(GetOfficerSkill(officerRole));
        description += $" As party {officerRole.ToLower()}, your {skillName} skill ({skillValue}) affects the entire party.";
    }
    
    // Add passive XP information from our configuration
    var passiveXP = GetPassiveXPDisplay(activeDuties);
    if (!string.IsNullOrEmpty(passiveXP))
    {
        description += $" (Passive Daily {passiveXP})";
    }
    
    return description;
}
```

**Our Formation System → SAS Culture Display**:
```csharp
private string GetSASFormationDisplay()
{
    var duties = EnlistedDutiesBehavior.Instance;
    var formationType = duties?.GetPlayerFormationType() ?? "infantry";
    var culture = EnlistmentBehavior.Instance?.CurrentLord?.Culture?.StringId ?? "empire";
    
    // Use our culture-specific formation names (from culture_reference.md)
    var formationNames = new Dictionary<string, Dictionary<string, string>>
    {
        ["infantry"] = new Dictionary<string, string>
        {
            ["empire"] = "Imperial Legionary", ["aserai"] = "Aserai Footman", 
            ["khuzait"] = "Khuzait Spearman", ["vlandia"] = "Vlandian Man-at-Arms",
            ["sturgia"] = "Sturgian Warrior", ["battania"] = "Battanian Clansman"
        },
        // ... other formations
    };
    
    return formationNames[formationType][culture] ?? formationType.ToTitleCase();
}
```

### **Technical Implementation Notes**

**Menu Registration Pattern** (SAS format using TaleWorlds APIs):
```csharp
// EXACT SAS pattern recreation
starter.AddWaitGameMenu("enlisted_status", 
    "Party Leader: {PARTY_LEADER}\n{PARTY_TEXT}",
    new OnInitDelegate(OnEnlistedStatusInit),
    new OnConditionDelegate(OnEnlistedStatusCondition),
    null, // No consequence
    new OnTickDelegate(OnEnlistedStatusTick), // Real-time updates like SAS
    GameMenu.MenuAndOptionType.WaitMenuShowOnlyProgressOption,
    GameOverlays.MenuOverlayType.None,
    0f, // No wait time
    GameMenu.MenuFlags.None, null);
```

**Status Update Pattern** (SAS real-time updates):
```csharp
private void OnEnlistedStatusTick(MenuCallbackArgs args)
{
    // SAS pattern: Continuous information refresh
    UpdatePartyMenuSASStyle(args);
    
    // SAS pattern: Automatic menu exit if not enlisted
    if (!EnlistmentBehavior.Instance?.IsEnlisted == true)
    {
        GameMenu.ExitToLast();
    }
}
```

**Integration with Our Superior Systems**:
- **Duties System**: `EnlistedDutiesBehavior.Instance` methods for dynamic duty information
- **Formation System**: Culture-specific names from our `culture_reference.md`
- **Officer Roles**: Our dual approach (APIs + patches) with SAS display format
- **Progression**: Our 1-year system displayed in SAS XP format

## 🎮 **Expected Final Menu Experience**

### **Main Status Display** (SAS format + our enhancements):
```
Party Leader: Sir Derthert
Army Objective: Besieging Charas  
Enlistment Time: Summer 15, 1084
Enlistment Tier: 4  
Formation: Imperial Legionary (Infantry)
Wage: 145(+25)💰
Current Experience: 2400
Next Level Experience: 3500
When not fighting: You are currently assigned: Field Medic, Runner (2/2). As party surgeon, your Medicine skill affects the entire party. (Passive Daily Medicine, Athletics XP)
```

### **Interactive Options** (SAS functionality + our features):
- **🏥 Request field medical treatment** - Enhanced healing with Field Medic bonuses
- **⚔️ Military training activities** - Time-based skill development (hunt, train, scout)
- **🛡️ Request equipment upgrade** - Formation-appropriate gear with realistic pricing
- **🎖️ Discuss duty assignments** - Dialog-based role changes with our configuration system
- **⚡ Battle Commands: Formation Only** - Formation-specific command preferences
- **📊 Show military service record** - Comprehensive service history and achievements
- **🏆 Show reputation with factions** - Enhanced relationship tracking display

## 🔥 **IMMEDIATE IMPLEMENTATION STEPS**

### **Step 1: Enhanced Status Display** (Priority 1 - Start Now)

**File**: `src/Features/Interface/Behaviors/EnlistedMenuBehavior.cs`

**Replace current `RefreshEnlistedStatusDisplay()` with SAS-exact format**:
```csharp
private void RefreshEnlistedStatusDisplay()
{
    var enlistment = EnlistmentBehavior.Instance;
    var duties = EnlistedDutiesBehavior.Instance;
    
    if (!enlistment?.IsEnlisted == true)
    {
        MBTextManager.SetTextVariable("PARTY_LEADER", "");
        MBTextManager.SetTextVariable("PARTY_TEXT", "You are not currently enlisted.");
        return;
    }

    var lord = enlistment.CurrentLord;
    
    // EXACT SAS FORMAT: Build comprehensive status text
    var statusBuilder = new StringBuilder();
    
    // Army vs Party objectives (SAS pattern)
    if (lord.PartyBelongedTo.Army != null)
    {
        statusBuilder.AppendLine($"Army Objective: {GetDetailedArmyObjective(lord.PartyBelongedTo.Army)}");
    }
    else  
    {
        statusBuilder.AppendLine($"Party Objective: {GetDetailedPartyObjective(lord.PartyBelongedTo)}");
    }
    
    // Core military information (SAS format)
    statusBuilder.AppendLine($"Enlistment Time: {GetSASEnlistmentTimeDisplay()}");
    statusBuilder.AppendLine($"Enlistment Tier: {enlistment.EnlistmentTier}");
    statusBuilder.AppendLine($"Formation: {GetSASFormationDisplay()}");
    statusBuilder.AppendLine($"Wage: {GetSASWageDisplay()}💰");
    statusBuilder.AppendLine($"Current Experience: {enlistment.EnlistmentXP}");
    
    // Next level XP (our 1-year progression in SAS format)
    var nextXP = GetNextTierXPRequirement(enlistment.EnlistmentTier);
    if (enlistment.EnlistmentTier < 7)
    {
        statusBuilder.AppendLine($"Next Level Experience: {nextXP}");
    }
    
    // Enhanced assignment description (our duties → SAS format)
    statusBuilder.AppendLine($"When not fighting: {GetSASStyleAssignmentDescription()}");

    // Set SAS-style text variables
    MBTextManager.SetTextVariable("PARTY_LEADER", lord.EncyclopediaLinkWithName);
    MBTextManager.SetTextVariable("PARTY_TEXT", statusBuilder.ToString());
}

// SAS objective detection with our enhancements
private string GetDetailedArmyObjective(Army army)
{
    var leader = army.LeaderParty;
    
    // Enhanced objective detection (more detailed than SAS)
    if (leader.SiegeEvent != null)
    {
        var settlement = leader.SiegeEvent.BesiegedSettlement;
        return $"Besieging {settlement.EncyclopediaLinkWithName}";
    }
    
    if (leader.MapEvent != null)
    {
        return "Engaging enemy forces";
    }
    
    if (leader.TargetSettlement != null)
    {
        return $"Travelling to {leader.TargetSettlement.EncyclopediaLinkWithName}";
    }
    
    if (leader.TargetParty != null)
    {
        return $"Following {leader.TargetParty.EncyclopediaLinkWithName}";
    }
    
    return "Army operations";
}

// SAS time display format
private string GetSASEnlistmentTimeDisplay()
{
    // Match SAS format: "Summer 15, 1084"
    var enlistmentDate = enlistment?.EnlistmentDate ?? CampaignTime.Now;
    return enlistmentDate.ToString(); // Will show season and year like SAS
}

// SAS wage display with bonuses
private string GetSASWageDisplay()
{
    var baseWage = CalculateBaseWage();
    var totalWage = CalculateCurrentDailyWage();
    var bonus = totalWage - baseWage;
    
    // SAS format: "145(+25)" when bonus applies
    if (bonus > 0)
        return $"{baseWage}(+{bonus})";
    else
        return baseWage.ToString();
}
```

### **Step 2: SAS-Style Menu Options** (Priority 1 - Immediate)

**Add missing SAS menu options to match screenshots**:
```csharp
// Equipment request system (SAS "Visit Weaponsmith")
starter.AddGameMenuOption("enlisted_status", "enlisted_request_gear",
    "Request new gear ({GEAR_COST}💰)",
    IsGearRequestAvailable,
    OnGearRequestSelected, false, 2);

// Training activities (SAS "Train with troops") 
starter.AddGameMenuOption("enlisted_status", "enlisted_training_activities",
    "Military training activities",
    IsTrainingAvailable,
    OnTrainingSelected, false, 3);

// Battle commands (SAS "Battle Commands: All/Formation")
starter.AddGameMenuOption("enlisted_status", "enlisted_battle_commands",
    "Battle Commands: {COMMAND_MODE}",
    IsBattleCommandsAvailable,
    OnBattleCommandsToggle, false, 4);

// Reputation display (SAS "Show reputation with factions")
starter.AddGameMenuOption("enlisted_status", "enlisted_reputation",
    "Show reputation with factions", 
    IsReputationAvailable,
    OnReputationSelected, false, 5);

// Assignment management (SAS "Ask for a different assignment")  
starter.AddGameMenuOption("enlisted_status", "enlisted_assignment_change",
    "Ask for a different assignment",
    IsAssignmentChangeAvailable,
    OnAssignmentChangeSelected, false, 6);
```

### **Step 3: Time-Based Activities Implementation** (Priority 2)

**New File**: `src/Features/Interface/Behaviors/MilitaryActivitiesBehavior.cs`

**SAS "Skills Tent" Recreation**:
```csharp
public sealed class MilitaryActivitiesBehavior : CampaignBehaviorBase
{
    private Dictionary<string, CampaignTime> _activityCooldowns = new();
    
    private void AddMilitaryActivitiesMenu(CampaignGameStarter starter)
    {
        // SAS pattern: "You are leaving your tent and..."
        starter.AddWaitGameMenu("enlisted_activities",
            "You are preparing for military duties...\n{ACTIVITY_OPTIONS}",
            new OnInitDelegate(OnActivitiesInit),
            new OnConditionDelegate(IsActivitiesAvailable),
            null, null,
            GameMenu.MenuAndOptionType.WaitMenuShowOnlyProgressOption,
            GameOverlays.MenuOverlayType.None, 0f,
            GameMenu.MenuFlags.None, null);

        // SAS activities with our enhancements
        AddTimeBasedActivity(starter, "hunt_food", "Hunt and fetch food", 16, "Scouting", "party_food");
        AddTimeBasedActivity(starter, "train_troops", "Train with troops", 8, "Leadership", "troop_xp");  
        AddTimeBasedActivity(starter, "scout_patrol", "Volunteer to be scout", 24, "Scouting", "party_vision");
        AddTimeBasedActivity(starter, "equipment_maintenance", "Maintain equipment", 4, "Engineering", "gear_condition");
        
        // SAS-style paid activities
        AddPaidActivity(starter, "buy_beer", "Buy beer for your party", 200, "party_morale");
        AddPaidActivity(starter, "medical_supplies", "Pay for extra bandages and medicine", 40, "medical_bonus");
        AddPaidActivity(starter, "gambling", "Gamble", 50, "random_outcome");
    }
    
    private void AddTimeBasedActivity(CampaignGameStarter starter, string id, string name, 
                                    int hours, string skill, string benefit)
    {
        starter.AddGameMenuOption("enlisted_activities", $"activity_{id}",
            $"{name} ({hours} hours)",
            args => CanPerformActivity(id, hours),
            args => StartActivity(id, hours, skill, benefit),
            false, -1);
    }
}
```

### **Step 4: Assignment Dialog Integration** (Priority 2)

**Enhance**: `src/Features/Conversations/Behaviors/EnlistedDialogManager.cs`

**SAS Assignment Dialog Pattern with Our Configuration**:
```csharp
private void AddSASStyleAssignmentDialogs(CampaignGameStarter starter)
{
    // Entry point for assignment discussion  
    starter.AddPlayerLine("enlisted_discuss_assignment",
        "enlisted_service_options",
        "enlisted_assignment_main", 
        "I would like to discuss my current assignment.",
        CanDiscussAssignments, null, 110);

    // Lord response with SAS pattern
    starter.AddDialogLine("enlisted_assignment_response",
        "enlisted_assignment_main",
        "enlisted_assignment_options",
        "{HERO}, I heard you are not happy with your current assignment. What would you rather do instead?",
        null, SetAssignmentDialogVariables, 100);

    // Dynamic duty options from our JSON configuration
    GenerateDynamicDutyDialogs(starter);
}

private void GenerateDynamicDutyDialogs(CampaignGameStarter starter)
{
    // Read our duties_system.json and create dialog options
    var dutiesConfig = LoadDutiesConfiguration();
    
    foreach (var duty in dutiesConfig.Duties.Values)
    {
        // SAS pattern: Skill/relationship-based approval
        AddDutyRequestOption(starter, duty);
        AddDutyApprovalResponse(starter, duty);
        AddDutyBriberyOption(starter, duty); // SAS bribery system
    }
}
```

## ✅ **Implementation Success Criteria**

### **Visual Match** (Must match SAS screenshots):
- ✅ **Header format**: "Party Leader: {Name}"
- ✅ **Information order**: Army/Party objective → Enlistment details → Formation → Wages → XP → Assignment
- ✅ **Option layout**: Icons + descriptions matching SAS style
- ✅ **Gold formatting**: Use 💰 symbol and bonus display format

### **Functional Match** (Must provide SAS capabilities):
- ✅ **Interactive services**: Equipment requests, training, assignments  
- ✅ **Real-time updates**: Information refreshes automatically
- ✅ **Role management**: Dialog-based assignment changes with requirements
- ✅ **Battle integration**: Enhanced encounter options when appropriate

### **Enhanced Integration** (Our superior features):
- ✅ **Configuration-driven**: All duties loaded from JSON (vs SAS hardcoded)
- ✅ **Formation specializations**: Culture-specific names and formation benefits
- ✅ **Officer roles**: Dual approach integration with skill display
- ✅ **Realistic progression**: 1-year timeline with authentic military economics

## 🔍 **EXACT SAS FEATURE IMPLEMENTATIONS STUDIED**

### **1. Army/Party Objective Detection** (Lines 2859-2884 in Test.cs)

**SAS Implementation**:
```csharp
// SAS checks Army vs Party membership first
if (Test.followingHero.PartyBelongedTo.Army == null || Test.followingHero.PartyBelongedTo.AttachedTo == null) {
    s += "Party Objective : " + Test.GetMobilePartyBehaviorText(Test.followingHero.PartyBelongedTo);
} else {
    s += "Army Objective : " + Test.GetMobilePartyBehaviorText(Test.followingHero.PartyBelongedTo.Army.LeaderParty);
}
```

**❌ CRITICAL ERROR**: DefaultBehavior and ShortTermBehavior are **SAS-SPECIFIC** - NOT TaleWorlds APIs!

**✅ ACTUAL TaleWorlds APIs for Objective Detection** (from our verified engine-signatures.md):
```csharp
// VERIFIED TaleWorlds APIs we can use:
TaleWorlds.CampaignSystem.Party.MobileParty :: TargetParty { get; }
TaleWorlds.CampaignSystem.Party.MobileParty :: TargetSettlement { get; } 
TaleWorlds.CampaignSystem.Party.MobileParty :: CurrentSettlement { get; }
TaleWorlds.CampaignSystem.Party.MobileParty :: MapEvent { get; }
TaleWorlds.CampaignSystem.Party.MobileParty :: SiegeEvent { get; }
TaleWorlds.CampaignSystem.Party.MobileParty :: Army { get; }
```

**Our Simplified Implementation** (using only verified APIs):
```csharp
private string GetPartyObjective(MobileParty party) {
    if (party.SiegeEvent != null)
        return $"Besieging {party.SiegeEvent.BesiegedSettlement?.Name}";
    if (party.MapEvent != null)
        return "Engaging enemy forces";
    if (party.TargetSettlement != null)
        return $"Travelling to {party.TargetSettlement.Name}";
    if (party.TargetParty != null)
        return $"Following {party.TargetParty.Name}";
    if (party.CurrentSettlement != null)
        return $"Stationed at {party.CurrentSettlement.Name}";
    return "Patrol duties";
}
```

### **2. Battle Commands System** (Lines 901-922 in Test.cs)

**SAS Implementation**:
```csharp
// Boolean toggle with two menu options showing different states
public static bool AllBattleCommands = false;

// Option 1: Shows when AllBattleCommands = true
starter.AddGameMenuOption("party_wait", "battle_commands_all", "Battle Commands : All",
    args => Test.AllBattleCommands, // Only show when true
    args => { Test.AllBattleCommands = false; GameMenu.ActivateGameMenu("party_wait"); });

// Option 2: Shows when AllBattleCommands = false  
starter.AddGameMenuOption("party_wait", "battle_commands_formation", "Battle Commands : Player Formation Only",
    args => !Test.AllBattleCommands, // Only show when false
    args => { Test.AllBattleCommands = true; GameMenu.ActivateGameMenu("party_wait"); });
```

**Battle Commands Patch** (BattleCommandsPatch.cs):
- Patches `BehaviorComponent.InformSergeantPlayer` 
- Shows formation commands with audio (attack horn vs move horn)
- Filters based on `AllBattleCommands` setting and player formation type

### **3. Faction Reputation Display** (Lines 1254-1279 in Test.cs)

**SAS Implementation**:
```csharp
// Simple separate menu with kingdom list
starter.AddGameMenu("faction_reputation", "{REPUTATION}", delegate(MenuCallbackArgs args) {
    string s = "";
    foreach (Kingdom kingdom in Campaign.Current.Kingdoms) {
        s += kingdom.Name.ToString() + " : " + Test.GetFactionRelations(kingdom).ToString() + "\n";
    }
    text.SetTextVariable("REPUTATION", s);
}, 0, 0, null);

// Back option returns to party_wait
starter.AddGameMenuOption("faction_reputation", "faction_reputation_back", "Back",
    args => true,
    args => GameMenu.ActivateGameMenu("party_wait"));
```

**SAS Reputation Tracking**:
```csharp
public static void ChangeFactionRelation(IFaction faction, int amount) {
    // Custom tracking system separate from game reputation
    Test.FactionReputation.Add(faction, Math.Max(0, value + amount));
}
```

### **4. Assignment Change Dialog** (Lines 1593-1870 in Test.cs)

**❌ CRITICAL ERROR**: DialogFlow.CreateDialogFlow is **SAS-SPECIFIC** - NOT TaleWorlds API!

**✅ ACTUAL TaleWorlds Implementation** (using verified APIs from engine-signatures.md):
```csharp
// Use standard TaleWorlds dialog registration (like our existing working dialogs)
private void AddAssignmentChangeDialogs(CampaignGameStarter starter) {
    // Entry point for assignment discussion  
    starter.AddPlayerLine("enlisted_request_assignment_change",
        "enlisted_service_options",
        "enlisted_assignment_options",
        "I would like to discuss my current duties.",
        CanRequestAssignmentChange, null, 110);

    // Lord response 
    starter.AddDialogLine("enlisted_assignment_response",
        "enlisted_assignment_options", 
        "enlisted_duty_choices",
        "{HERO}, what duties would you prefer?",
        null, SetAssignmentDialogVariables, 100);

    // Generate dynamic duty options from our JSON configuration
    foreach (var duty in GetConfiguredDuties()) {
        starter.AddPlayerLine($"request_duty_{duty.Id}",
            "enlisted_duty_choices",
            $"duty_approval_{duty.Id}",
            $"I wish to serve as {duty.DisplayName}.",
            () => CanRequestSpecificDuty(duty), null, 100);
            
        // Lord approval with skill/relationship checks
        starter.AddDialogLine($"approve_duty_{duty.Id}",
            $"duty_approval_{duty.Id}",
            "close_window",
            GetApprovalResponse(duty),
            () => MeetsRequirements(duty),
            () => AssignDutyThroughDialog(duty), 100);
    }
}

// Use verified TaleWorlds relationship API
private bool MeetsRequirements(DutyDefinition duty) {
    var skillRequired = duty.UnlockConditions?.SkillRequired ?? 0;
    var relationRequired = duty.UnlockConditions?.RelationshipRequired ?? 0;
    
    var hasSkill = Hero.MainHero.GetSkillValue(GetRelevantSkill(duty)) >= skillRequired;
    var hasRelation = _enlistedLord.GetRelation(Hero.MainHero) >= relationRequired; // Verified TaleWorlds API
    
    return hasSkill && hasRelation;
}
```

**Assignment Descriptions** (Lines 3030-3069):
```
Grunt Work: "You are currently assigned to perform grunt work. Most tasks are unpleasant, tiring or involve menial labor. (Passive Daily Athletics XP)"
Surgeon: "You are currently assigned as the surgeon. You spend your time taking care of the wounded men. (Medicine XP from party)"
Engineer: "You are currently assigned as the engineer. The party relies on your knowledge of siegecraft to build war machines. (Engineering XP from party)"
```

### **5. Party Member Conversation System** (Lines 1462-1504 in Test.cs)

**❌ CRITICAL ERROR**: MenuCallbackArgs.MenuContext.SetRepeatObjectList might be SAS-SPECIFIC!

**✅ ACTUAL TaleWorlds Implementation** (using verified APIs):
```csharp
// Use TaleWorlds conversation system (like our existing working dialogs)
private void AddPartyConversationMenu(CampaignGameStarter starter) {
    // Main "Talk to..." option
    starter.AddGameMenuOption("enlisted_status", "enlisted_talk_to_members",
        "Talk to...",
        IsPartyConversationAvailable,
        OnPartyConversationSelected, false, 7);

    // Separate menu for member selection
    starter.AddGameMenu("enlisted_party_members", 
        "Army Members\n{MEMBER_LIST}",
        OnPartyMembersInit,
        GameOverlays.MenuOverlayType.None, 
        GameMenu.MenuFlags.None, null);
}

private void OnPartyMembersInit(MenuCallbackArgs args) {
    var memberList = new List<string>();
    var lord = EnlistmentBehavior.Instance.CurrentLord;
    
    // Use verified TaleWorlds APIs
    if (lord.PartyBelongedTo.Army != null) {
        foreach (var party in lord.PartyBelongedTo.Army.Parties) { // Verified API
            foreach (var element in party.MemberRoster.GetTroopRoster()) { // Verified API
                if (element.Character.IsHero) {
                    memberList.Add(element.Character.HeroObject.Name.ToString());
                }
            }
        }
    }
    
    MBTextManager.SetTextVariable("MEMBER_LIST", string.Join("\n", memberList));
}
```

### **6. Equipment Selector System** (Lines 2072-2187 in Test.cs)

**✅ VERIFIED TaleWorlds Equipment Discovery** (from our docs):
```csharp
// Use verified TaleWorlds APIs from engine-signatures.md
public List<ItemObject> GetCultureEquipmentForTier(int maxTier) {
    var culture = EnlistmentBehavior.Instance.CurrentLord.Culture;
    var availableGear = new List<ItemObject>();
    
    // Use verified TaleWorlds API
    var allCharacters = MBObjectManager.Instance.GetObjectTypeList<CharacterObject>(); // ✅ VERIFIED
    
    foreach (var character in allCharacters) {
        if (character.Culture == culture && character.Tier <= maxTier) {
            foreach (var equipment in character.BattleEquipments) { // ✅ VERIFIED
                for (EquipmentIndex i = EquipmentIndex.Weapon0; i <= EquipmentIndex.HorseHarness; i++) {
                    var item = equipment[i].Item;
                    if (item != null && !availableGear.Contains(item)) {
                        availableGear.Add(item);
                    }
                }
            }
        }
    }
    
    return availableGear;
}

// Use TaleWorlds inventory system instead of custom UI
private void ShowEquipmentSelection() {
    var equipmentRoster = CreateEquipmentRoster();
    InventoryManager.Instance.OpenScreenAsReceiveItems(equipmentRoster, // ✅ VERIFIED API
        new TextObject("Military Equipment"),
        OnEquipmentSelectionComplete);
}
```

### **7. Enhanced Assignment Descriptions** (Our System Integration)

**How to Integrate Our Duties → SAS Format**:
```csharp
private string GetSASStyleAssignmentDescription() {
    var duties = EnlistedDutiesBehavior.Instance;
    var activeDuties = duties?.ActiveDuties ?? new List<string>();
    
    if (activeDuties.Count == 0) {
        return "You have no current assigned duties. You spend your idle time drinking, gambling, and chatting with the idle soldiers."; // EXACT SAS default
    }
    
    // Convert our JSON duties to SAS description format
    var descriptions = new List<string>();
    foreach (var dutyId in activeDuties) {
        if (_config.Duties.TryGetValue(dutyId, out var duty)) {
            descriptions.Add($"{duty.Description} (Passive Daily {duty.TargetSkill} XP)");
        }
    }
    
    return $"You are currently assigned: {string.Join(", ", activeDuties.Select(id => GetDutyDisplayName(id)))}. {string.Join(" ", descriptions)}";
}
```

## 🔧 **EXACT Implementation Tasks**

## ✅ **VERIFIED TALEWORLDS-ONLY IMPLEMENTATION TASKS**

### **TASK 1: Convert to SAS Menu Format** 
**File**: `src/Features/Interface/Behaviors/EnlistedMenuBehavior.cs`
- Change to `"Party Leader: {PARTY_LEADER}\n{PARTY_TEXT}"` format ✅ **VERIFIED TaleWorlds API**
- Add `OnTickDelegate` for real-time updates ✅ **VERIFIED TaleWorlds API**
- Use simplified objective detection with `TargetSettlement`, `MapEvent`, `SiegeEvent` ✅ **ALL VERIFIED**

### **TASK 2: Add Verified Menu Options**
**File**: `src/Features/Interface/Behaviors/EnlistedMenuBehavior.cs`
- **"Battle Commands: {MODE}"** - Simple boolean toggle ✅ **Basic logic, no special APIs**
- **"Show reputation with factions"** - Use `Campaign.Current.Kingdoms` ✅ **NEED TO VERIFY** 
- **"Ask for a different assignment"** - Use standard TaleWorlds dialog system ✅ **VERIFIED APIs**

### **TASK 3: Assignment Dialog Using TaleWorlds APIs**
**File**: `src/Features/Conversations/Behaviors/EnlistedDialogManager.cs`
- Use `starter.AddPlayerLine` / `starter.AddDialogLine` ✅ **VERIFIED APIs**
- Use `Hero.GetRelation(Hero)` for relationship checks ✅ **NEED TO VERIFY EXACT METHOD NAME**
- Use `GiveGoldAction.ApplyBetweenCharacters` for bribery ✅ **VERIFIED API**
- Connect to our `EnlistedDutiesBehavior.AssignDuty()` method ✅ **OUR VERIFIED CODE**

### **TASK 4: Equipment Using TaleWorlds APIs Only**
**File**: `src/Features/Equipment/Behaviors/EquipmentRequestManager.cs` (NEW)
- Use `MBObjectManager.Instance.GetObjectTypeList<CharacterObject>()` ✅ **VERIFIED API**
- Use `character.BattleEquipments` for equipment extraction ✅ **VERIFIED API**
- Use `InventoryManager.Instance.OpenScreenAsReceiveItems()` ✅ **VERIFIED API**
- NO custom Gauntlet UI - stick to TaleWorlds systems

**This implementation plan creates an exact SAS menu experience while leveraging our superior configuration-driven architecture, formation specializations, and enhanced military progression system.**
