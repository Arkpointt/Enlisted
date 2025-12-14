# Lance Life Simulation - Integration Guide

**Purpose**: Document required changes to existing systems for Lance Life Simulation integration

**Date**: December 13, 2025

---

## Overview

The Lance Life Simulation adds **player's lance member simulation** (injuries, deaths, cover requests, promotion escalation). This needs to integrate with two existing systems:

1. **AI Camp Schedule** - Lord's lance daily scheduling (Phase 5-6 feature)
2. **Camp Activities Menu** - Player-initiated camp actions

**Key Distinction**:
- **AI Camp Schedule**: Simulates what the **lord's lance** (NPCs) are doing throughout the day
- **Lance Life Simulation**: Simulates what the **player's lance members** (your unit) are experiencing (health, welfare, career)

These are **parallel systems** that need coordination points.

---

## Integration Point 1: AI Camp Schedule ↔ Lance Life Simulation

### Problem

When a lance member gets injured or takes leave in the Lance Life Simulation, they should be removed from the AI Camp Schedule. When they recover, they should return.

### Required Changes

#### File: `src/Features/Camp/AIScheduleBehavior.cs` (or wherever AI Schedule lives)

**Add Interface** for external systems to modify schedule:

```csharp
/// <summary>
/// Public API for external systems to modify lance member availability.
/// Used by Lance Life Simulation to handle injuries/leave.
/// </summary>
public interface ILanceScheduleModifier
{
    /// <summary>
    /// Remove a lance member from schedule due to unavailability.
    /// </summary>
    void RemoveMemberFromSchedule(string memberId, CampaignTime startDate, CampaignTime endDate, string reason);
    
    /// <summary>
    /// Return a member to schedule after recovery/return.
    /// </summary>
    void AddMemberToSchedule(string memberId, CampaignTime effectiveDate);
    
    /// <summary>
    /// Temporarily assign player to cover a duty.
    /// </summary>
    void AssignPlayerCoverDuty(string dutyId, CampaignTime date, int estimatedHours);
    
    /// <summary>
    /// Get current duty assignment for a member (for display purposes).
    /// </summary>
    string GetMemberCurrentDuty(string memberId);
    
    /// <summary>
    /// Check if a member is currently scheduled.
    /// </summary>
    bool IsMemberScheduled(string memberId);
}
```

**Implementation Example**:

```csharp
public class AIScheduleBehavior : CampaignBehaviorBase, ILanceScheduleModifier
{
    // Existing schedule data structures...
    private Dictionary<string, List<ScheduledActivity>> _memberSchedules = new();
    private HashSet<string> _unavailableMembers = new();
    
    public void RemoveMemberFromSchedule(string memberId, CampaignTime startDate, CampaignTime endDate, string reason)
    {
        _unavailableMembers.Add(memberId);
        
        // Remove from all scheduled activities in date range
        var schedulesToRemove = _memberSchedules
            .Where(kvp => kvp.Key == memberId)
            .Where(kvp => kvp.Value.Any(s => s.StartTime >= startDate && s.StartTime <= endDate))
            .ToList();
        
        foreach (var schedule in schedulesToRemove)
        {
            _memberSchedules.Remove(schedule.Key);
        }
        
        ModLogger.Info("AISchedule", $"Removed {memberId} from schedule: {reason} ({startDate} to {endDate})");
        
        // Trigger re-planning to cover duties
        TriggerScheduleRegeneration();
    }
    
    public void AddMemberToSchedule(string memberId, CampaignTime effectiveDate)
    {
        _unavailableMembers.Remove(memberId);
        ModLogger.Info("AISchedule", $"Returned {memberId} to schedule availability");
        
        // Member will be included in next schedule generation
        TriggerScheduleRegeneration();
    }
    
    public void AssignPlayerCoverDuty(string dutyId, CampaignTime date, int estimatedHours)
    {
        // Add player to schedule for this specific duty
        var playerActivity = new ScheduledActivity
        {
            DutyId = dutyId,
            StartTime = date,
            DurationHours = estimatedHours,
            AssignedTo = "player",
            Type = ActivityType.CoveringForMember
        };
        
        if (!_memberSchedules.ContainsKey("player"))
            _memberSchedules["player"] = new List<ScheduledActivity>();
        
        _memberSchedules["player"].Add(playerActivity);
        
        ModLogger.Info("AISchedule", $"Assigned player to cover duty: {dutyId}");
    }
}
```

#### File: `src/Features/Lances/Behaviors/LanceLifeSimulationBehavior.cs`

**Hook into AI Schedule** when member state changes:

```csharp
public class LanceLifeSimulationBehavior : CampaignBehaviorBase
{
    private ILanceScheduleModifier _scheduleModifier;
    
    public override void SyncData(IDataStore dataStore)
    {
        // ... existing sync code ...
        
        // Get AI Schedule interface on load
        _scheduleModifier = Campaign.Current.GetCampaignBehavior<AIScheduleBehavior>() as ILanceScheduleModifier;
    }
    
    /// <summary>
    /// Called when a member gets injured.
    /// </summary>
    public void OnMemberInjured(string memberId, HealthState severity)
    {
        // Update local state
        var state = _memberStates[memberId];
        state.Health = severity;
        state.Activity = ActivityState.SickBay;
        _memberStates[memberId] = state;
        
        // Remove from AI Schedule
        var recoveryDays = GetEstimatedRecoveryDays(severity);
        var endDate = CampaignTime.Now + CampaignTime.Days(recoveryDays);
        
        _scheduleModifier?.RemoveMemberFromSchedule(
            memberId, 
            CampaignTime.Now, 
            endDate, 
            $"Injured ({severity})"
        );
        
        ModLogger.Info("LanceLifeSim", $"Member {memberId} injured, removed from schedule for {recoveryDays} days");
    }
    
    /// <summary>
    /// Called when a member recovers.
    /// </summary>
    public void OnMemberRecovered(string memberId)
    {
        // Update local state
        var state = _memberStates[memberId];
        state.Health = HealthState.Healthy;
        state.Activity = ActivityState.OnDuty;
        _memberStates[memberId] = state;
        
        // Return to AI Schedule
        _scheduleModifier?.AddMemberToSchedule(memberId, CampaignTime.Now);
        
        ModLogger.Info("LanceLifeSim", $"Member {memberId} recovered, returned to schedule");
    }
    
    /// <summary>
    /// Called when player accepts a cover request.
    /// </summary>
    public void OnPlayerAcceptsCoverRequest(string requestingMemberId, string dutyId)
    {
        // Assign player to cover duty
        _scheduleModifier?.AssignPlayerCoverDuty(dutyId, CampaignTime.Now, estimatedHours: 2);
        
        // Apply fatigue cost to player
        var fatigue = CampFatigueBehavior.Instance;
        fatigue?.ModifyFatigue(2); // Cover duty costs 2 fatigue
        
        // Improve relationship with requesting member
        ModifyMemberRelationship(requestingMemberId, +10);
        
        ModLogger.Info("LanceLifeSim", $"Player covering {dutyId} for {requestingMemberId}");
    }
}
```

---

## Integration Point 2: Camp Activities Menu ↔ Lance Life Simulation

### Problem

The Camp Activities Menu should display:
1. Visual indicators when lance members are injured/unavailable
2. "Check on Wounded" option when members are in sick bay
3. "Honor the Fallen" option when members have died

### Required Changes

#### File: `ModuleData/Enlisted/Activities/activities.json`

**Add new lance-focused activities**:

```json
{
  "id": "check_wounded",
  "category": "lance",
  "location": "medical_tent",
  "titleId": "activity_check_wounded_title",
  "title": "Check on the Wounded",
  "descriptionId": "activity_check_wounded_desc",
  "description": "Visit injured lance mates in the medical tent.",
  "fatigue_cost": 0,
  "xp_rewards": {},
  "effects": {
    "lance_reputation": 1
  },
  "conditions": {
    "all": [
      "is_enlisted",
      "has_injured_lance_members"
    ]
  },
  "result_text": "You spend time with the wounded. They appreciate your concern.",
  "cooldown_hours": 24
},
{
  "id": "honor_fallen",
  "category": "lance",
  "location": "lords_tent",
  "titleId": "activity_honor_fallen_title",
  "title": "Honor the Fallen",
  "descriptionId": "activity_honor_fallen_desc",
  "description": "Remember fallen lance mates.",
  "fatigue_cost": 0,
  "xp_rewards": {},
  "effects": {
    "lance_reputation": 2
  },
  "conditions": {
    "all": [
      "is_enlisted",
      "has_fallen_lance_members"
    ]
  },
  "result_text": "You remember those who didn't make it back. Their sacrifice won't be forgotten.",
  "cooldown_hours": 168
}
```

#### File: `src/Features/Camp/UI/CampActivitiesVM.cs` (or CampAreaVM.cs)

**Add condition evaluators**:

```csharp
/// <summary>
/// Evaluates lance-specific conditions for activity availability.
/// </summary>
private bool EvaluateLanceCondition(string condition)
{
    var lanceSim = LanceLifeSimulationBehavior.Instance;
    if (lanceSim == null) return false;
    
    switch (condition)
    {
        case "has_injured_lance_members":
            return lanceSim.GetInjuredMembers().Count > 0;
        
        case "has_fallen_lance_members":
            return lanceSim.GetFallenMembersSinceLastMemorial().Count > 0;
        
        case "player_has_cover_duty":
            return lanceSim.PlayerHasActiveCoverDuties();
        
        default:
            return false;
    }
}

/// <summary>
/// Override existing condition evaluation to include lance conditions.
/// </summary>
protected override bool EvaluateActivityCondition(string condition)
{
    // Try existing conditions first
    if (base.EvaluateActivityCondition(condition))
        return true;
    
    // Try lance-specific conditions
    return EvaluateLanceCondition(condition);
}
```

#### File: `src/Features/Lances/Behaviors/EnlistedLanceMenuBehavior.cs` (existing)

**Add welfare section** to roster display:

```csharp
/// <summary>
/// Build lance roster display with injury indicators.
/// </summary>
private string BuildLanceRosterSection()
{
    var sb = new StringBuilder();
    sb.AppendLine("--- LANCE ROSTER ---");
    
    var lanceSim = LanceLifeSimulationBehavior.Instance;
    var members = lanceSim?.GetActiveLanceMembers() ?? new List<LanceMemberState>();
    
    foreach (var member in members)
    {
        var statusIcon = GetHealthIcon(member.Health);
        var relationshipIcon = GetRelationshipIndicator(member.RelationshipWithPlayer);
        
        sb.AppendLine($"{statusIcon} {member.Rank} {member.Name} {relationshipIcon}");
        
        // Show additional info for injured
        if (member.Health != HealthState.Healthy)
        {
            sb.AppendLine($"   └─ {GetHealthDescription(member.Health)}");
        }
    }
    
    // Show fallen members count
    var fallenCount = lanceSim?.GetFallenMembersSinceLastMemorial().Count ?? 0;
    if (fallenCount > 0)
    {
        sb.AppendLine($"\n💀 {fallenCount} fallen since last memorial");
    }
    
    return sb.ToString();
}

private string GetHealthIcon(HealthState health)
{
    return health switch
    {
        HealthState.Healthy => "✓",
        HealthState.MinorInjury => "⚠",
        HealthState.MajorInjury => "🏥",
        HealthState.Incapacitated => "⛑",
        HealthState.Dead => "💀",
        _ => "?"
    };
}

private string GetHealthDescription(HealthState health)
{
    return health switch
    {
        HealthState.MinorInjury => "Light wounds, recovering",
        HealthState.MajorInjury => "Serious injury, in medical tent",
        HealthState.Incapacitated => "Critical condition",
        _ => "Unknown"
    };
}
```

---

## Integration Point 3: Event System Coordination

### Problem

Lance Life Simulation events need to respect the existing event evaluation cadence and safety checks.

### Required Changes

#### File: `src/Features/Lances/Events/LanceLifeEventsAutomaticBehavior.cs` (existing)

**Add Lance Simulation event category** to evaluation:

```csharp
/// <summary>
/// Evaluate lance simulation events (injuries, cover requests, escalation).
/// </summary>
private void EvaluateLanceSimulationEvents()
{
    var lanceSim = LanceLifeSimulationBehavior.Instance;
    if (lanceSim == null) return;
    
    var events = _eventCatalog
        .Where(e => e.Category == "lance_simulation")
        .ToList();
    
    foreach (var evt in events)
    {
        if (!CanEventFire(evt)) continue;
        
        // Check simulation-specific conditions
        if (evt.Id.Contains("cover_request") && !lanceSim.ShouldGenerateCoverRequest())
            continue;
        
        if (evt.Id.Contains("injury") && !HasInjuryOccurred())
            continue;
        
        if (evt.Id.Contains("escalation") && !lanceSim.IsPlayerReadyForLanceLeader())
            continue;
        
        // Queue event using existing safety system
        QueueEventForNextSafeMoment(evt);
        return; // Only one simulation event per evaluation
    }
}

/// <summary>
/// Add to daily tick processing.
/// </summary>
public override void OnDailyTick()
{
    // ... existing evaluation logic ...
    
    // Evaluate lance simulation events
    EvaluateLanceSimulationEvents();
}
```

---

## Integration Point 4: Display Coordination

### Problem

AI Camp Schedule viewer and Lance Life Simulation roster should show consistent data.

### Solution

**Add cross-reference** in schedule display:

#### File: AI Schedule UI (wherever it displays)

```csharp
/// <summary>
/// Show member availability status from Lance Life Simulation.
/// </summary>
private string GetMemberAvailabilityStatus(string memberId)
{
    var lanceSim = LanceLifeSimulationBehavior.Instance;
    if (lanceSim == null) return "Available";
    
    var state = lanceSim.GetMemberState(memberId);
    if (state == null) return "Available";
    
    return state.Value.Activity switch
    {
        ActivityState.OnDuty => "Available",
        ActivityState.SickBay => "🏥 In Medical Tent",
        ActivityState.OnLeave => "On Leave",
        ActivityState.Detached => "Detached Duty",
        _ => "Unavailable"
    };
}

/// <summary>
/// Display schedule with availability indicators.
/// </summary>
private void DisplaySchedule()
{
    foreach (var memberId in _lanceMembers)
    {
        var availability = GetMemberAvailabilityStatus(memberId);
        var duty = _scheduleModifier?.GetMemberCurrentDuty(memberId) ?? "Unassigned";
        
        Console.WriteLine($"{memberId}: {duty} ({availability})");
    }
}
```

---

## Testing Integration Points

### Test Case 1: Injury Removes from Schedule

1. Player is enlisted with lance
2. AI Schedule has member assigned to "Guard Duty"
3. Injury event fires for that member
4. **Expected**: Member removed from Guard Duty
5. **Expected**: AI Schedule shows gap or assigns replacement

### Test Case 2: Cover Request Adds Player to Schedule

1. Player has no scheduled duties
2. Lance member requests cover for "Message Runner"
3. Player accepts
4. **Expected**: Player now scheduled for "Message Runner"
5. **Expected**: AI Schedule shows player assignment
6. **Expected**: Player gains 2 fatigue

### Test Case 3: Recovery Returns to Schedule

1. Member is injured (removed from schedule 7 days ago)
2. 7 days pass
3. Recovery event fires
4. **Expected**: Member returned to available pool
5. **Expected**: AI Schedule includes member in next generation

### Test Case 4: Camp Activities Show Injured

1. Lance member is injured
2. Player opens Camp Activities → Medical Tent
3. **Expected**: "Check on Wounded" option visible
4. Player selects it
5. **Expected**: Lance Rep +1, relationship +10 with injured member

---

## Configuration Changes Needed

### File: `ModuleData/Enlisted/enlisted_config.json`

**Add coordination settings**:

```json
{
  "integration": {
    "ai_schedule_lance_simulation_sync": true,
    "auto_remove_injured_from_schedule": true,
    "auto_return_recovered_to_schedule": true,
    "player_cover_duty_fatigue_cost": 2,
    "show_welfare_options_in_camp_activities": true
  }
}
```

---

## Summary of Required Changes

### Must Implement:
1. ✅ `ILanceScheduleModifier` interface in AI Schedule
2. ✅ Hook calls from Lance Life Simulation to AI Schedule on injury/recovery
3. ✅ New camp activities: "Check on Wounded", "Honor the Fallen"
4. ✅ Condition evaluators: `has_injured_lance_members`, `has_fallen_lance_members`
5. ✅ Lance roster display with health indicators

### Optional Enhancements:
- Visual indicators in AI Schedule viewer showing unavailable members
- Cross-reference between schedule and roster (click member name → see schedule)
- Player schedule display showing cover duties
- Notification when player has an active cover duty

### No Changes Needed:
- Event delivery system (already handles lance_simulation category)
- Save/load system (each behavior handles its own data)
- Menu navigation (Lance menu and Camp Activities are separate entry points)

---

## Implementation Order

1. **Week 1**: Add `ILanceScheduleModifier` interface to AI Schedule
2. **Week 2**: Implement Lance Life Simulation with schedule hooks
3. **Week 3**: Add new camp activities JSON entries
4. **Week 4**: Update camp activities condition evaluators
5. **Week 5**: Update lance menu with health indicators
6. **Week 6**: Integration testing

This ensures AI Schedule is ready before Lance Life Simulation starts calling it.

---

**Document Version**: 1.0  
**Last Updated**: December 13, 2025  
**Status**: Integration Requirements Complete
