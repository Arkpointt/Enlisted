# Event Status Visualization

**Summary:** Color coding system for scheduled, completed, and missed events/opportunities in the DECISIONS accordion, with persistence of missed events so players can see what they missed.

**Status:** 📋 Planning  
**Last Updated:** 2026-01-03  
**Related Docs:** [Content System Architecture](../Features/Content/content-system-architecture.md), [Orchestrator Opportunity Unification](../ORCHESTRATOR-OPPORTUNITY-UNIFICATION.md), [Systems Integration Analysis](systems-integration-analysis.md)

---

## Executive Summary

The current DECISIONS accordion on the main Enlisted status menu shows opportunities as a flat list without visual distinction between different states. This feature adds:

1. **Color coding** for event states (scheduled, available, completed, missed)
2. **Missed event persistence** so players can see what they missed for context
3. **Status indicators** that communicate urgency and outcome at a glance

---

## Index

1. [Current State](#current-state)
2. [Proposed Color Coding](#proposed-color-coding)
3. [Missed Event Persistence](#missed-event-persistence)
4. [Implementation Design](#implementation-design)
5. [UI/UX Mockups](#uiux-mockups)
6. [Implementation Tasks](#implementation-tasks)

---

## Current State

### How Opportunities Display Today

The DECISIONS accordion currently shows:
- Opportunities for the **current phase only**
- All shown in default text color (cream `#FAF4DEFF`)
- [NEW] badge when accordion is first expanded with unseen opportunities
- No distinction between committed/available states
- Missed opportunities simply **disappear** when phase ends

### Current ScheduledOpportunity State Model

```csharp
public class ScheduledOpportunity
{
    public bool Consumed { get; set; }         // True if event was delivered
    public bool PlayerCommitted { get; set; }  // True if player clicked to schedule
    
    // Derived states:
    public bool IsAvailableToCommit => !Consumed && !PlayerCommitted;
    public bool IsScheduledToFire => PlayerCommitted && !Consumed;
}
```

**Missing States:**
- `Completed` - Event was delivered and player made a choice
- `Missed` - Phase ended without player engaging
- `Expired` - Time window passed completely

---

## Proposed Color Coding

### Event State Color Map

| State | Color Name | Hex Value | Visual Description | When Used |
|-------|------------|-----------|-------------------|-----------|
| **Available** | Default/Cream | `#FAF4DEFF` | Standard text | Current phase, can click |
| **Committed** | Intel/Blue | `#6B9FFFFF` | Light blue | Player clicked, waiting for phase |
| **Upcoming** | Rumor/Lavender | `#B8A8D8FF` | Muted purple | Future phase, not clickable yet |
| **Completed** | Success/Green | `#90EE90FF` | Light green | Event delivered and resolved |
| **Missed** | Warning/Gold | `#FFD700FF` | Gold/amber | Phase passed without engagement |
| **Expired** | Alert/Red (dim) | `#FF6B6B88` | Dimmed red | Day ended, cleanup pending |

### Color Rationale

These colors align with the existing `EnlistedColors.xml` palette:

- **Intel Blue** (`#6B9FFF`) - Already used for tactical information, implies "scheduled for action"
- **Rumor Lavender** (`#B8A8D8`) - Used for foreshadowing, perfect for "upcoming but not yet"
- **Success Green** (`#90EE90`) - Universal positive outcome color
- **Warning Gold** (`#FFD700`) - Attention-drawing but not alarming, "you missed this"
- **Alert Red dim** (`#FF6B6B88`) - Semi-transparent to feel "past tense"

### XML Brush Additions

Add to `GUI/Brushes/EnlistedColors.xml`:

```xml
<!-- Event Status Styles for DECISIONS accordion -->
<Style Name="Event.Available" FontColor="#FAF4DEFF" TextGlowColor="#000000FF" FontSize="22" />
<Style Name="Event.Committed" FontColor="#6B9FFFFF" TextGlowColor="#000000FF" FontSize="22" TextOutlineAmount="0.5" TextOutlineColor="#000000FF" />
<Style Name="Event.Upcoming" FontColor="#B8A8D8FF" TextGlowColor="#000000FF" FontSize="22" />
<Style Name="Event.Completed" FontColor="#90EE90FF" TextGlowColor="#000000FF" FontSize="22" />
<Style Name="Event.Missed" FontColor="#FFD700FF" TextGlowColor="#000000FF" FontSize="22" />
<Style Name="Event.Expired" FontColor="#FF6B6B88" TextGlowColor="#000000FF" FontSize="22" />
```

---

## Missed Event Persistence

### Design Goals

1. **Context preservation** - Player understands what opportunities were available
2. **No spam** - Missed events don't clutter the menu indefinitely
3. **Learning aid** - Helps players learn daily rhythm and opportunity timing
4. **Narrative continuity** - "You missed the card game" feels like a real thing that happened

### Persistence Rules

| Rule | Value | Rationale |
|------|-------|-----------|
| **Persistence Duration** | Until next phase transition | Long enough to notice, not forever |
| **Maximum Missed Shown** | 3 | Prevent accordion bloat |
| **Display Order** | After available, before completed | Natural priority order |
| **Clickable?** | No (greyed out) | Can't change the past |
| **Tooltip** | "You missed this opportunity" | Clear explanation |

### State Transitions

```
Phase Dawn starts:
  - Opportunities generated for Dawn → Available
  - Previous Night's uncommitted → Missed (persisted)
  
Player commits to "Card Game":
  - "Card Game" → Committed (blue)
  
Dawn phase ends → Midday starts:
  - Dawn's Committed + Consumed → Completed (green)
  - Dawn's Available (not committed) → Missed (gold)
  - Midday opportunities → Available
  - Previous Missed (from Night) → Expired (removed)
```

### Extended ScheduledOpportunity Model

```csharp
public class ScheduledOpportunity
{
    // Existing
    public string OpportunityId { get; set; }
    public DayPhase Phase { get; set; }
    public bool Consumed { get; set; }
    public bool PlayerCommitted { get; set; }
    
    // NEW: Status tracking
    public OpportunityStatus Status { get; set; } = OpportunityStatus.Available;
    
    // NEW: When the opportunity transitioned to Missed/Completed
    public CampaignTime? ResolvedTime { get; set; }
    
    // Derived
    public bool IsAvailableToCommit => Status == OpportunityStatus.Available;
    public bool IsScheduledToFire => Status == OpportunityStatus.Committed;
    public bool IsPersistentDisplay => Status == OpportunityStatus.Missed 
                                    || Status == OpportunityStatus.Completed;
}

public enum OpportunityStatus
{
    Available,   // Can click now
    Committed,   // Waiting to fire
    Upcoming,    // Future phase
    Completed,   // Delivered and resolved
    Missed,      // Phase passed, not engaged
    Expired      // Day ended, cleanup
}
```

---

## Implementation Design

### Menu Display Logic

In `EnlistedMenuBehavior.RefreshMainMenuDecisionSlots()`:

```csharp
private void RefreshMainMenuDecisionSlots()
{
    var orchestrator = ContentOrchestrator.Instance;
    if (orchestrator == null) return;
    
    var allOpps = orchestrator.GetAllTodaysOpportunities();
    var currentPhase = WorldStateAnalyzer.GetCurrentDayPhase();
    
    // Categorize opportunities for display
    var displayList = new List<DisplayOpportunity>();
    
    // 1. Available (current phase, can click)
    foreach (var opp in allOpps.Where(o => o.Phase == currentPhase && o.Status == OpportunityStatus.Available))
    {
        displayList.Add(new DisplayOpportunity(opp, "Event.Available", canClick: true));
    }
    
    // 2. Committed (any phase, waiting to fire)
    foreach (var opp in allOpps.Where(o => o.Status == OpportunityStatus.Committed))
    {
        var phaseLabel = opp.Phase == currentPhase ? "" : $" ({opp.Phase})";
        displayList.Add(new DisplayOpportunity(opp, "Event.Committed", canClick: false, suffix: phaseLabel));
    }
    
    // 3. Missed (past phases, persisted for context) - max 3
    foreach (var opp in allOpps
        .Where(o => o.Status == OpportunityStatus.Missed)
        .OrderByDescending(o => o.ResolvedTime)
        .Take(3))
    {
        displayList.Add(new DisplayOpportunity(opp, "Event.Missed", canClick: false, 
            tooltip: "You missed this opportunity."));
    }
    
    // 4. Upcoming (future phases today)
    foreach (var opp in allOpps.Where(o => o.Phase > currentPhase && o.Status == OpportunityStatus.Upcoming))
    {
        displayList.Add(new DisplayOpportunity(opp, "Event.Upcoming", canClick: false,
            suffix: $" ({opp.Phase})"));
    }
    
    // 5. Completed (today's resolved events)
    foreach (var opp in allOpps.Where(o => o.Status == OpportunityStatus.Completed))
    {
        displayList.Add(new DisplayOpportunity(opp, "Event.Completed", canClick: false,
            suffix: " ✓"));
    }
    
    // Populate menu slots from displayList...
}
```

### Phase Transition Handler

In `ContentOrchestrator.OnPhaseTransition()`:

```csharp
private void HandlePhaseTransition(DayPhase previousPhase, DayPhase newPhase)
{
    if (_scheduledOpportunities == null) return;
    
    // Previous phase opportunities that weren't engaged → Missed
    if (_scheduledOpportunities.TryGetValue(previousPhase, out var previousOpps))
    {
        foreach (var opp in previousOpps.Where(o => o.Status == OpportunityStatus.Available))
        {
            opp.Status = OpportunityStatus.Missed;
            opp.ResolvedTime = CampaignTime.Now;
            ModLogger.Debug(LogCategory, $"Opportunity missed: {opp.OpportunityId}");
        }
        
        // Committed that fired → Completed
        foreach (var opp in previousOpps.Where(o => o.Status == OpportunityStatus.Committed && o.Consumed))
        {
            opp.Status = OpportunityStatus.Completed;
            opp.ResolvedTime = CampaignTime.Now;
        }
    }
    
    // Expire old Missed from 2+ phases ago
    CleanupExpiredOpportunities(newPhase);
    
    // Mark new phase opportunities as available
    if (_scheduledOpportunities.TryGetValue(newPhase, out var newOpps))
    {
        foreach (var opp in newOpps.Where(o => o.Status == OpportunityStatus.Upcoming))
        {
            opp.Status = OpportunityStatus.Available;
        }
    }
}

private void CleanupExpiredOpportunities(DayPhase currentPhase)
{
    // Remove Missed opportunities older than 1 phase transition
    var phasesToClean = Enum.GetValues(typeof(DayPhase))
        .Cast<DayPhase>()
        .Where(p => p < currentPhase - 1) // More than 1 phase behind
        .ToList();
    
    foreach (var phase in phasesToClean)
    {
        if (_scheduledOpportunities.TryGetValue(phase, out var opps))
        {
            opps.RemoveAll(o => o.Status == OpportunityStatus.Missed);
        }
    }
}
```

---

## UI/UX Mockups

### Accordion Display Example

```
▼ DECISIONS (4)

  [●] Join the dice game           ← Available (cream, clickable)
  [●] Help unload supply wagon     ← Available (cream, clickable)
  
  [◐] Card game (Dusk)             ← Committed (blue, future phase)
  
  [○] Morning drill                ← Missed (gold, greyed)
  [○] Water detail                 ← Missed (gold, greyed)
  
  [✓] Sparring match               ← Completed (green)
```

### Legend

| Symbol | Meaning |
|--------|---------|
| `[●]` | Available - can click |
| `[◐]` | Committed - scheduled |
| `[◇]` | Upcoming - future phase |
| `[○]` | Missed - phase passed |
| `[✓]` | Completed - done |

### Tooltip Examples

**Committed:**
> "Scheduled for Dusk. You'll participate when the phase arrives."

**Missed:**
> "You missed this opportunity. The dice game ended without you."

**Upcoming:**
> "Available at Dusk. Check back later."

---

## Implementation Tasks

### Phase 1: Status Model (2 hrs)

| Task | File | Effort |
|------|------|--------|
| Add `OpportunityStatus` enum | `ContentOrchestrator.cs` | 15 min |
| Add `Status` and `ResolvedTime` to `ScheduledOpportunity` | `ContentOrchestrator.cs` | 15 min |
| Update state transition logic on phase change | `ContentOrchestrator.cs` | 45 min |
| Update `CleanupMissedOpportunities` to set status | `ContentOrchestrator.cs` | 30 min |
| Test state transitions through Dawn→Night cycle | Manual testing | 15 min |

### Phase 2: Color Styles (1 hr)

| Task | File | Effort |
|------|------|--------|
| Add Event.* styles to brush file | `GUI/Brushes/EnlistedColors.xml` | 15 min |
| Verify styles load correctly | Game test | 15 min |
| Document color usage in this spec | This file | 15 min |

### Phase 3: Menu Integration (3 hrs)

| Task | File | Effort |
|------|------|--------|
| Create `DisplayOpportunity` helper class | `EnlistedMenuBehavior.cs` | 20 min |
| Update `RefreshMainMenuDecisionSlots()` to categorize | `EnlistedMenuBehavior.cs` | 45 min |
| Apply color styles based on status | `EnlistedMenuBehavior.cs` | 45 min |
| Add status symbols to display text | `EnlistedMenuBehavior.cs` | 30 min |
| Add tooltips for non-available states | `EnlistedMenuBehavior.cs` | 30 min |
| Test display across phase transitions | Manual testing | 30 min |

### Phase 4: Polish (1 hr)

| Task | File | Effort |
|------|------|--------|
| Add combat log message when opportunities missed | `ContentOrchestrator.cs` | 20 min |
| Add Daily Brief integration for missed opportunities | `EnlistedNewsBehavior.cs` | 20 min |
| Test edge cases (save/load, no opportunities, etc.) | Manual testing | 20 min |

---

## Success Metrics

### Visual Clarity

| Metric | Target |
|--------|--------|
| Players can distinguish states at a glance | 5 distinct colors visible |
| Missed opportunities are noticed | Player feedback positive |
| No color confusion with existing UI | Colors feel consistent |

### Player Experience

1. **"I see what's happening"** - Color coding makes state obvious
2. **"I know what I missed"** - Missed events persist long enough to notice
3. **"I understand the rhythm"** - Phase-based opportunities feel natural
4. **"No clutter"** - Expired events clean up automatically

---

## Configuration (Future)

If players want to customize:

```json
// enlisted_config.json
{
  "ui": {
    "decisions_accordion": {
      "show_missed": true,
      "missed_max_display": 3,
      "show_completed": true,
      "show_upcoming": true,
      "color_coding_enabled": true
    }
  }
}
```

**Note:** Initial implementation will hardcode these values. Config support added if player demand exists.

---

## Appendix: Phase Reference

| Phase | Game Hours | Description |
|-------|-----------|-------------|
| Dawn | 6-12 | Morning activities |
| Midday | 12-18 | Afternoon activities |
| Dusk | 18-22 | Evening activities |
| Night | 22-6 | Night activities |

---

**End of Document**
