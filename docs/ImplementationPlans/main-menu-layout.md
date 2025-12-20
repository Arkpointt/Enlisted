# Main Enlisted Menu Layout - Technical Specification

> **Note**: This is a detailed technical spec. For the complete implementation plan including Decisions menu and Duty Events system, see `docs/Features/enlisted-interface-master-plan.md`.

## Overview

The main enlisted status menu (`enlisted_status`) serves as the player's primary interface for viewing their current military service status, schedule, orders, and accessing core game functions. This menu displays critical information at a glance and provides navigation to all enlisted-player features.

## Purpose

Provide a clear, organized view of:
- Player's identity and service record (lord, rank, days served)
- Current situation (what the lord is doing, daily report)
- Daily schedule (4 time blocks with assigned duties)
- Active orders requiring player attention (time-sensitive tasks)
- Navigation to core features (Camp, Decisions, Conversations, Settlement visits, Leaving service)

The menu balances information density with readability, ensuring players can quickly understand their current situation and available actions without scrolling or hunting through submenus.

## Inputs/Outputs

### Inputs
- Enlistment state (lord, rank, tier, days enlisted)
- Current lord activity (besieging, traveling, raiding, etc.)
- Daily report excerpt (from `EnlistedNewsBehavior`)
- Player schedule (from `ScheduleBehavior.CurrentSchedule` - 4 time blocks)
- Current time block (from `CampaignTriggerTrackerBehavior.GetTimeBlock()`)
- Active orders (from duty/quest system - time-sensitive tasks)
- Current fatigue (displayed in header)
- Optional “Company” warning line (from `CampLifeBehavior` snapshot; only shown when notable)

### Outputs
- Formatted menu text with all status information
- Menu options for navigation (Camp, Decisions, etc.)
- Visual highlighting of current time block
- Dynamic Orders section (empty when no orders, populated otherwise)

## Behavior

### Menu Structure

```
Lord: [Lord Name] | Fatigue [Current]/[Max]

Company: Log [Strain]% | Mor [Morale]% | Pay [OK/Late/DUE]   (only shown when notable)

Lord's Work: [Current Activity]

Service: [Rank Name] (T[Tier])

Report: [Report excerpt - max ~2 lines, ~160 chars]

Schedule:
  [Morning: Duty Name]   ← Current block highlighted
  Afternoon: Duty Name
  Dusk: Duty Name
  Night: Duty Name

Orders:
  • [Order 1] [Expires: Time]
  • [Order 2] [Expires: Time]
  (OR empty if no orders - header remains)

Now: [Current situation - reality layer]
```

### Layout Sections

#### 1. Header Section
- **Lord**: Displayed at top with fatigue in the native game menu title
  - Format: `"Lord: {Lord Name} | Fatigue {Current}/{Max}"`
- **Rank**: Player's current rank title and tier
  - Uses culture-specific rank names via `RankHelper.GetCurrentRank()`
- **Enlisted**: Days since enlistment began
  - Calculated from `EnlistmentBehavior.EnlistedDay`
- **Lord's Work**: Current lord activity
  - Uses existing `GetCurrentObjectiveDisplay()` method

#### 2. Daily Report Section
- Pulled from `EnlistedNewsBehavior.GetLatestDailyReportExcerpt()`
- **Must be short** to avoid forcing scroll in the GameMenu UI.
  - Recommended cap: **2 lines**, **~160 chars**
- Single paragraph format for compactness
- Blank line separator after

#### 3. Schedule Section (NEW)
- Shows all 4 time blocks: Morning, Afternoon, Dusk, Night
- Each line shows: `[TimeBlock: Duty Name]` (current block) or `TimeBlock: Duty Name` (others)
- Current time block is visually highlighted (e.g., brackets `[Morning]` vs plain `Afternoon`)
- Duties pulled from `ScheduleBehavior.CurrentSchedule` (AI Camp Schedule system)
- If no duty assigned for a block, shows "Free Time"
- **Duty title length** should be truncated to avoid wrapping (recommended cap: ~22 chars)

#### 4. Orders Section (NEW)
- **Always shows the header**: `"Orders:"`
- Dynamically populated with time-sensitive orders/tasks
- Each order shows:
  - Bullet point prefix
  - Order description
  - Expiration time (Tonight, Tomorrow, etc.)
- When empty: Just header with blank space below (no "No orders" message)
- No long separator lines (GameMenu width varies by UI scale)

#### 5. Menu Options Section
- Existing 5 menu options remain unchanged:
  1. Camp (Submenu)
  2. Decisions (Submenu)
  3. My Lord... (Conversation)
  4. Visit Settlement (Conditional - only at towns/castles)
  5. Leave / Discharge / Desert (Leave)

### Visual Formatting

- **No long ASCII/Unicode separators**: they wrap on some UI scales/resolutions and create visual “broken line” artifacts.
- **Section spacing**: Single blank line between major sections; rely on headings (`Schedule:`, `Orders:`, `Now:`).
- **Current time block**: Use brackets `[Morning: Training]` vs plain `Afternoon: Guard Duty`
- **Orders**: Bullet character `•` for each order line
- **Text alignment**: Left-aligned, no centering

## Edge Cases

### No Orders
- Header "Orders:" remains visible
- Empty space below header (no "None" or "No orders" text)
- Separator line still present

### No Daily Report
- Skip the Daily Report section entirely (no placeholder text)
- Schedule section moves up

### No Schedule Data
- Show all 4 time blocks with "Free Time" as default
- Still highlight current time block

### Settlement Conditional
- "Visit Settlement" option only appears when lord is at a town or castle
- Hidden completely when traveling/on map

### Long Lord Names
- Truncate or abbreviate if needed to fit on one line with fatigue
- Priority: Fatigue display must always be visible

### Time Block Transitions
- Menu auto-refreshes on hourly tick (handled by `OnEnlistedStatusTick`)
- Current time block highlighting updates automatically

## Acceptance Criteria

1. ✅ Lord name, rank, and days enlisted appear at the top in correct format
2. ✅ Lord's Work displays current activity using existing objective system
3. ✅ Report shows excerpt from news system and remains short enough to avoid scroll (recommended cap ~2 lines / ~160 chars)
4. ✅ Schedule section displays all 4 time blocks with assigned duties
5. ✅ Current time block is visually highlighted (brackets or similar)
6. ✅ Orders section always shows header, dynamically populates with active orders
7. ✅ Orders section is empty (but header visible) when no orders exist
8. ✅ Menu options remain at bottom, unchanged from current implementation
9. ✅ Menu refreshes on hourly tick to update current time block
10. ✅ All text fits within menu bounds without scrolling
11. ✅ No “broken” separator artifacts (do not use long separator strings that wrap)
12. ✅ "Visit Settlement" option only appears when at a town or castle

## Implementation Notes

### Phase 1: Foundation
- Add helper methods for formatting new sections
- Create schedule display logic (pull from `EnlistedDutiesBehavior`)
- Create orders display logic (stub with empty list for now)
- Update `BuildCompactEnlistedStatusText()` with new layout

### Phase 2: Schedule Integration
- Hook into `EnlistedDutiesBehavior.GetPlayerSchedule()`
- Get current time block from `CampaignTriggerTrackerBehavior.GetTimeBlock()`
- Format time block highlighting (current vs others)
- Handle "Free Time" defaults for unassigned blocks

### Phase 3: Orders System
- Design orders data structure (or reuse existing quest/duty system)
- Create orders provider/manager
- Add expiration time formatting ("Tonight", "Tomorrow", etc.)
- Integrate orders into menu display

### Phase 4: Polish
- Ensure menu auto-refresh works correctly
- Test with various scenarios (no orders, no report, settlement visits)
- Verify text fits within menu bounds
- Add localization strings if needed

### Dependencies
- `EnlistmentBehavior` - rank, tier, days enlisted, lord
- `EnlistedDutiesBehavior` - player schedule (4 time blocks)
- `EnlistedNewsBehavior` - daily report excerpt
- `CampaignTriggerTrackerBehavior` - current time block
- Orders system (TBD - may need new behavior or use existing quest/duty system)

### Localization
Most text is dynamic from existing systems. New strings needed:
- `"Rank:"` label
- `"Enlisted:"` label
- `"Lord's Work:"` label
- `"Schedule:"` label
- `"Orders:"` label
- Time block names: `"Morning"`, `"Afternoon"`, `"Dusk"`, `"Night"` (already exist in `TimeBlock` enum)
- `"Free Time"` default duty text

## Related Systems
- `EnlistedMenuBehavior` - main menu display and initialization
- `EnlistedDutiesBehavior` - schedule and duty assignment
- `EnlistedNewsBehavior` - daily report generation
- `CampaignTriggerTrackerBehavior` - time block tracking
- Orders/Quest system (TBD) - active orders tracking

## Related Documentation
- **Master Implementation Plan**: `docs/Features/enlisted-interface-master-plan.md` - Complete vision and implementation roadmap
- **Duty Events Catalog**: `docs/Features/Events/duty-events-catalog.md` - Event content and chains
- **Duty Events Architecture**: `docs/Features/Events/duty-events-architecture.md` - Technical deep dive

