# Menu Interface System

**Professional military menu interface with organized duty/profession selection**

## Overview

Enhanced menu system providing comprehensive military service management with clean section organization, detailed descriptions, and tier-based progression.

## Purpose

Provides players with:
- Clear organization of duties vs professions
- Detailed descriptions of each military role
- Tier-based progression visibility 
- Streamlined navigation and professional presentation
- Connected daily XP processing

## Key Features

### Main Enlisted Status Menu
- **Master at Arms** - Troop selection with close button support
- **Visit Quartermaster** - Equipment variant selection
- **My Lord...** - Conversation with nearby lords
- **Report for Duty** - Organized duty/profession selection
- **Ask commander for leave** - Positioned at bottom for logical flow

### Duty Selection Interface

#### Section Organization
- **DUTIES** section header with visual separation
- **PROFESSIONS** section header with visual separation  
- Visual spacer between sections for clean layout

#### Duty Selection (Available T1+)
- **Enlisted** - General military service (+4 XP for non-formation skills)
- **Forager** - Supply procurement (Charm, Roguery, Trade)
- **Sentry** - Guard duty (Scouting, Tactics)
- **Messenger** - Communications (Scouting, Charm, Trade)
- **Pioneer** - Engineering work (Engineering, Steward, Smithing)

#### Profession Selection (Available T3+)
- **Quartermaster's Aide** - Logistics management (Steward, Trade)
- **Field Medic** - Medical duties (Medicine)
- **Siegewright's Aide** - Siege engineering (Engineering, Smithing)
- **Drillmaster** - Training management (Leadership, Tactics)
- **Saboteur** - Special operations (Roguery, Engineering, Smithing)

## Technical Implementation

### Menu Structure
```csharp
// Main menu: enlisted_status (WaitGameMenu)
// - Professional status display with real-time updates
// - Clean navigation to sub-menus

// Duty selection: enlisted_duty_selection (WaitGameMenu)  
// - Organized section headers
// - Dynamic checkmark system
// - Detailed description display
// - Connected to daily XP processing
```

### Key Behaviors
- **Dynamic text variables** for menu options with checkmarks (✓/○)
- **Tier-based availability** with helpful messages for locked professions
- **Real-time refresh** maintaining menu state consistency
- **Connected XP processing** linking menu selection to daily skill training

### Files
- `src/Features/Interface/Behaviors/EnlistedMenuBehavior.cs` - Main menu system
- `src/Features/Enlistment/Behaviors/EnlistmentBehavior.cs` - Selection persistence
- `src/Features/Assignments/Behaviors/EnlistedDutiesBehavior.cs` - XP processing

## User Experience

### At Tier 1-2
- **All duties visible and selectable**
- **All professions visible but locked** with helpful messages
- **Clean section organization** with headers and spacing

### At Tier 3+
- **All duties available**
- **All professions unlocked and selectable**
- **Full descriptions** show effects of each choice

### Description System
- **Top of menu shows detailed descriptions** for currently selected duty/profession
- **"None" shows simple text** when no profession selected
- **Rich military context** explaining daily activities and skill training

## Acceptance Criteria

✅ **Menu Organization**
- DUTIES and PROFESSIONS sections clearly separated
- Visual spacing between sections
- Headers are non-clickable visual organizers

✅ **Progression System**
- Professions visible at T1, selectable at T3
- Helpful tier requirement messages
- No "None" profession in menu (internal default only)

✅ **Navigation**
- Back button at top for easy access
- Leave option at bottom of main menu
- Close button in Master at Arms popup

✅ **Information Display**
- Detailed descriptions at top of duty menu
- Dynamic checkmarks showing current selections
- Clean names without skill bonus clutter

✅ **XP Integration**
- Selected duties/professions connect to daily XP processing
- Duty changes persist properly
- Formation training works with selections

## Debugging

### Common Issues
- **Professions not appearing**: Check tier requirement and availability conditions
- **Checkmarks not updating**: Verify `SetDynamicMenuText()` is called in refresh
- **XP not applying**: Ensure selected duties connect to `EnlistedDutiesBehavior.AssignDuty()`

### Verification
- Check dynamic text variables are being set properly
- Verify menu option priorities for correct display order
- Confirm tier checks work for profession restrictions
- Test duty/profession selection persistence across menu navigation
