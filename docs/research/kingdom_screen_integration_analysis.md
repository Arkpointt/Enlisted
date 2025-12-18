# Kingdom Screen Integration Analysis

**Purpose:** Research how the native Kingdom Screen architecture can be adapted for the Enlisted Camp Menu system, particularly for lance scheduling (policies) and event tracking.  
**Date:** December 15, 2025  
**Status:** Research Document

---

## Executive Summary

The Kingdom Screen is an excellent model for the Enlisted Camp Menu system. It provides:
- Multi-tab navigation with sub-VMs
- A "Policies" system that maps perfectly to lance scheduling
- A "Decisions" overlay system for handling pending events/requests
- Clean separation between read-only viewing and permission-based actions

**Recommendation:** Adapt the Kingdom Screen pattern for a new "Camp Management Screen" that consolidates lance management, scheduling, activities, and event tracking into a single polished interface.

---

## Kingdom Screen Architecture

### Core Components

```
GauntletKingdomScreen (ScreenBase, IGameStateListener)
├── KingdomState (pushed to GameStateManager)
├── KingdomManagementVM (main ViewModel)
│   ├── KingdomArmyVM       [Tab 0]
│   ├── KingdomSettlementVM [Tab 1]
│   ├── KingdomClanVM       [Tab 2]
│   ├── KingdomPoliciesVM   [Tab 3] ← **Most relevant for scheduling**
│   └── KingdomDiplomacyVM  [Tab 4]
├── KingdomDecisionsVM      [Overlay for pending events]
└── KingdomGiftFiefPopupVM  [Popup overlay]
```

### Key Patterns

#### 1. GameState-Based Navigation
```csharp
// Native: Push a state, screen auto-creates
Game.Current.GameStateManager.PushState(Game.Current.GameStateManager.CreateState<KingdomState>());

// The screen is registered via attribute
[GameStateScreen(typeof(KingdomState))]
public class GauntletKingdomScreen : ScreenBase, IGameStateListener
```

This allows direct navigation to specific content:
```csharp
kingdomState.InitialSelectedPolicy = targetPolicy;
kingdomState.InitialSelectedArmy = targetArmy;
// etc.
```

#### 2. Tab Controller Pattern
The `KingdomManagementVM` manages tab visibility:
```csharp
private void SetSelectedCategory(int index)
{
    Clan.Show = false;
    Settlement.Show = false;
    Policy.Show = false;
    Army.Show = false;
    Diplomacy.Show = false;
    
    switch (index)
    {
        case 0: Clan.Show = true; break;
        case 1: Settlement.Show = true; break;
        case 2: Policy.Show = true; break;
        case 3: Army.Show = true; break;
        default: Diplomacy.Show = true; break;
    }
}
```

#### 3. Policies System (Perfect Model for Scheduling)
The `KingdomPoliciesVM` provides:
- Two lists: ActivePolicies, OtherPolicies
- Selection tracking with `CurrentSelectedPolicy`
- Detail panel showing policy effects
- Action button (Propose/Disavow) with cost/permissions
- Approval likelihood indicator

**XML Structure:**
```xml
<PoliciesPanel>
    <!-- Left Panel: List -->
    <NavigatableListPanel DataSource="{ActivePolicies}">
        <PolicyActiveTuple IsSelected="@IsSelected"/>
    </NavigatableListPanel>
    <NavigatableListPanel DataSource="{OtherPolicies}">
        <PolicyOtherTuple IsSelected="@IsSelected"/>
    </NavigatableListPanel>
    
    <!-- Right Panel: Details -->
    <TextWidget Text="@CurrentSelectedPolicy.Name"/>
    <TextWidget Text="@CurrentSelectedPolicy.Explanation"/>
    <ListPanel DataSource="{CurrentSelectedPolicy.PolicyEffectList}">
        <RichTextWidget Text="@Text"/>
    </ListPanel>
    
    <!-- Action Button -->
    <ButtonWidget Command.Click="ExecuteProposeOrDisavow" 
                  IsEnabled="@CanProposeOrDisavowPolicy"/>
</PoliciesPanel>
```

#### 4. Decisions System (Event Tracking)
The `KingdomDecisionsVM` handles pending decisions:
- Tracks `_examinedDecisionsSinceInit` and `_solvedDecisionsSinceInit`
- Shows popup overlay when decisions need attention
- Maps decision types to specific VMs
- Supports "follow-up decisions" chain

```csharp
public void OnFrameTick()
{
    IsActive = IsCurrentDecisionActive;
    // Check for new decisions
    if (!source.Any()) return;
    // Handle follow-up or next decision
    HandleNextDecision();
}
```

---

## Proposed Camp Management Screen

### Tab Structure

```
CampManagementScreen
├── CampState (for navigation)
├── CampManagementVM
│   ├── CampLanceVM         [Tab 0] "Lance Roster"
│   ├── CampScheduleVM      [Tab 1] "Orders" ← **Policy-style scheduling**
│   ├── CampActivitiesVM    [Tab 2] "Activities"
│   ├── CampReportsVM       [Tab 3] "Reports" (bulletin/news)
│   └── CampArmyStatusVM    [Tab 4] "Army" (other lance statuses - T5+)
├── CampDecisionsVM         [Overlay for cover requests, events]
└── CampPopups              [Equipment, training, etc.]
```

### Schedule Tab (Orders) - Policy Pattern Applied

The Schedule tab uses the **exact same pattern** as Policies:

```
┌─────────────────────────────────────────────────────────────────────┐
│  CAMP                                                    [Close]   │
│                                                                     │
│  [Lance] [ORDERS] [Activities] [Reports] [Army]                    │
├─────────────────────────────────────────────────────────────────────┤
│                                                                     │
│  ┌─────────────────┐    ┌────────────────────────────────────────┐ │
│  │ TODAY'S ORDERS  │    │  SENTRY DUTY                           │ │
│  │                 │    │                                        │ │
│  │ ▶ Dawn: Rest   │    │  Lord Sigmund requires vigilant eyes   │ │
│  │   Morn: Sentry │    │  on the perimeter. Your lance will     │ │
│  │   Aft:  Patrol │    │  watch the northern approach.          │ │
│  │   Eve:  Free   │    │                                        │ │
│  │   Dusk: Watch  │    │  Effects:                              │ │
│  │   Night: Rest  │    │  - +5 Combat Readiness                 │ │
│  │                 │    │  - -10 Fatigue                         │ │
│  │ ─────────────── │    │  - 2 hours duration                    │ │
│  │ AVAILABLE       │    │                                        │ │
│  │                 │    │  [Set Schedule]     [Request Change]  │ │
│  │   Training     │    │                                        │ │
│  │   Foraging     │    │  Approval: [====|     ] 65%            │ │
│  │   Scouting     │    │                                        │ │
│  │   Work Detail  │    └────────────────────────────────────────┘ │
│  └─────────────────┘                                               │
│                                                                     │
│                                      [Done]                        │
└─────────────────────────────────────────────────────────────────────┘
```

### ViewModel Mapping

| Kingdom Component | Camp Equivalent | Purpose |
|-------------------|-----------------|---------|
| `KingdomPoliciesVM` | `CampScheduleVM` | Schedule management |
| `ActivePolicies` | `TodaysSchedule` | Current day's assigned blocks |
| `OtherPolicies` | `AvailableActivities` | Pool of activities to assign |
| `KingdomPolicyItemVM` | `ScheduleBlockItemVM` | Individual time slot |
| `PolicyEffectList` | `ActivityEffects` | Effects of activity |
| `CanProposeOrDisavowPolicy` | `CanModifySchedule` | T5+ permission check |
| `ExecuteProposeOrDisavow` | `ExecuteRequestChange` | Request schedule modification |
| `PolicyLikelihood` | `ApprovalLikelihood` | Chance lord approves request |

### Permission System (Rank-Based)

```csharp
public class CampScheduleVM : ViewModel
{
    // T5-T6 officers can set schedule
    public bool CanModifySchedule => PlayerRank >= 5;
    
    // T3-T4 can request changes (approval needed)
    public bool CanRequestChange => PlayerRank >= 3 && PlayerRank < 5;
    
    // T1-T2 view only
    public bool IsReadOnly => PlayerRank < 3;
}
```

### Decisions System for Camp Events

The `CampDecisionsVM` handles:

1. **Cover Requests** (from other lances)
   ```
   "5th Lance requests cover for patrol duty"
   [Accept: -6 fatigue, +relations]
   [Decline: They go alone, may take losses]
   ```

2. **Lance Life Events**
   ```
   "Harald twisted his ankle on patrol"
   [Cover his duty: -2 hours free time]
   [Refuse: -5 relations with Harald]
   ```

3. **Army Orders**
   ```
   "Lord Sigmund changed tomorrow's orders"
   [Acknowledge]
   ```

### Implementation Order

**Phase 1: Core Screen Setup** (Week 1)
1. Create `CampState` for game state management
2. Create `CampManagementVM` with tab controller
3. Implement basic tab switching
4. Create XML prefab based on `KingdomManagement.xml`

**Phase 2: Schedule Tab** (Week 2)
1. Create `CampScheduleVM` mirroring `KingdomPoliciesVM`
2. Implement `ScheduleBlockItemVM` for time slots
3. Add activity selection from available pool
4. Wire up with existing `ScheduleBehavior`

**Phase 3: Decisions Overlay** (Week 3)
1. Create `CampDecisionsVM` mirroring `KingdomDecisionsVM`
2. Implement decision types: CoverRequest, LanceEvent, OrderChange
3. Add overlay popup UI
4. Connect to Lance Life Events system

**Phase 4: Officer Mode** (Week 4)
1. Add rank-based permission checks
2. Implement schedule modification for T5-T6
3. Implement request system for T3-T4
4. Add approval likelihood calculation

---

## Key Code Patterns to Copy

### 1. Tab Control Widget

From `KingdomManagement.xml`:
```xml
<KingdomTabControlListPanel 
    ArmiesButton="ArmiesTabButton" ArmiesPanel="..\..\ArmiesPanel"
    ClansButton="ClanTabButton" ClansPanel="..\..\ClansPanel"
    ... >
```

**Adapt to:**
```xml
<CampTabControlListPanel
    LanceButton="LanceTabButton" LancePanel="..\..\LancePanel"
    ScheduleButton="ScheduleTabButton" SchedulePanel="..\..\SchedulePanel"
    ActivitiesButton="ActivitiesTabButton" ActivitiesPanel="..\..\ActivitiesPanel"
    ... >
```

### 2. List with Selection Tracking

From `KingdomPoliciesVM.cs`:
```csharp
private void OnPolicySelect(KingdomPolicyItemVM policy)
{
    if (CurrentSelectedPolicy != null)
        CurrentSelectedPolicy.IsSelected = false;
    CurrentSelectedPolicy = policy;
    if (CurrentSelectedPolicy != null)
        CurrentSelectedPolicy.IsSelected = true;
}
```

### 3. Decision Handling

From `KingdomDecisionsVM.cs`:
```csharp
public void HandleDecision(KingdomDecision curDecision)
{
    _examinedDecisionsSinceInit.Add(curDecision);
    _queryData = new InquiryData(...);
    InformationManager.ShowInquiry(_queryData);
}
```

### 4. Screen State Navigation

From `GauntletKingdomScreen.cs`:
```csharp
void IGameStateListener.OnActivate()
{
    // Load sprite categories
    _kingdomCategory = UIResourceManager.LoadSpriteCategory("ui_kingdom");
    
    // Create layer
    _gauntletLayer = new GauntletLayer("KingdomScreen", 1, true);
    _gauntletLayer.LoadMovie("KingdomManagement", DataSource);
    
    // Handle initial selection
    if (_kingdomState.InitialSelectedPolicy != null)
        DataSource.SelectPolicy(_kingdomState.InitialSelectedPolicy);
}
```

---

## Integration with Existing Systems

### Current CampBulletinScreen

The existing `CampBulletinScreen` uses layer-based overlay. This should be **retained** as the quick-access version (from game menus).

The new `CampManagementScreen` would be:
- Full-screen experience (like Kingdom Screen)
- Accessible via "K" key or dedicated button
- Contains all camp functionality in one place

### Schedule System Integration

```csharp
// Existing
public class ScheduleBehavior : CampaignBehaviorBase
{
    public DailySchedule CurrentSchedule { get; }
    public ScheduledBlock GetCurrentActiveBlock() { }
}

// New: Connect to CampScheduleVM
public class CampScheduleVM : ViewModel
{
    public CampScheduleVM()
    {
        _scheduleBehavior = ScheduleBehavior.Instance;
        LoadTodaysSchedule();
        LoadAvailableActivities();
    }
    
    public void LoadTodaysSchedule()
    {
        TodaysSchedule.Clear();
        var schedule = _scheduleBehavior.CurrentSchedule;
        foreach (var block in schedule.Blocks.OrderBy(b => (int)b.TimeBlock))
        {
            TodaysSchedule.Add(new ScheduleBlockItemVM(block, OnBlockSelect));
        }
    }
}
```

### Lance Life Events Integration

Events generated by the Lance Life system would feed into `CampDecisionsVM`:

```csharp
// When lance life generates event
LanceLifeBehavior.OnLanceMemberNeedsHelp += (member, helpType) =>
{
    var decision = new LanceHelpDecision(member, helpType);
    CampDecisionsVM.AddPendingDecision(decision);
};
```

---

## Benefits of This Approach

1. **Native Feel** - Uses proven Bannerlord patterns players recognize
2. **Complete Integration** - All camp features in one screen
3. **Officer Progression** - Meaningful difference between ranks
4. **Event Tracking** - Proper handling of camp events/requests
5. **Scalable** - Easy to add new tabs as features grow
6. **Policy Model** - Perfect fit for schedule management

---

## Files to Reference

### Native Code
- `Decompile\SandBox.GauntletUI\GauntletKingdomScreen.cs`
- `Decompile\TaleWorlds.CampaignSystem.ViewModelCollection\ViewModelCollection\KingdomManagement\KingdomManagementVM.cs`
- `Decompile\TaleWorlds.CampaignSystem.ViewModelCollection\ViewModelCollection\KingdomManagement\Policies\KingdomPoliciesVM.cs`
- `Decompile\TaleWorlds.CampaignSystem.ViewModelCollection\ViewModelCollection\KingdomManagement\Decisions\KingdomDecisionsVM.cs`

### Native Prefabs
- `Modules\SandBox\GUI\Prefabs\KingdomManagement\KingdomManagement.xml`
- `Modules\SandBox\GUI\Prefabs\KingdomManagement\Policies\PoliciesPanel.xml`
- `Modules\SandBox\GUI\Prefabs\KingdomManagement\Decision\KingdomDecision.xml`

### Enlisted Existing
- `src\Features\Camp\UI\Bulletin\CampBulletinScreen.cs`
- `src\Features\Camp\UI\Bulletin\CampBulletinVM.cs`
- `src\Features\Schedule\Behaviors\ScheduleBehavior.cs`
- `src\Features\Schedule\Models\DailySchedule.cs`

---

## Conclusion

The Kingdom Screen architecture is an excellent foundation for the Enlisted Camp Management system. The Policies tab pattern maps directly to lance scheduling, and the Decisions overlay system handles camp events elegantly.

**Recommended Next Steps:**
1. Create `CampState` and `CampManagementScreen` skeleton
2. Implement Schedule tab using Policy pattern
3. Connect to existing schedule system
4. Add Decisions overlay for events
5. Implement rank-based permissions
6. Polish UI with Kingdom-style brushes/sprites

This approach aligns perfectly with v0.7.0 (AI Daily Schedule) and sets up infrastructure for v0.8.0 (Lance Life Simulation) and v1.0.0 (Army Lance Activity).

