# Feature Spec: Town Access System

## Table of Contents

- [Overview](#overview)
- [Purpose](#purpose)
- [Inputs/Outputs](#inputsoutputs)
- [Behavior](#behavior)
- [Technical Implementation](#technical-implementation)
- [Edge Cases](#edge-cases)
- [Acceptance Criteria](#acceptance-criteria)
- [Debugging](#debugging)
- [Key Insights](#key-insights)

---

## Overview
Synthetic outside encounter system that enables full settlement exploration for invisible enlisted parties without encounter type assertion crashes.

## Purpose
Provide seamless town and castle access for enlisted soldiers while maintaining proper encounter state management and avoiding engine assertions that crash the game.

## Inputs/Outputs

**Inputs:**
- Enlisted lord's current settlement location
- Player's enlisted status (invisible/inactive party state)
- Existing encounter state from army settlement entry

**Outputs:**  
- Access to town_outside/castle_outside menus with proper encounter types
- Full settlement functionality (tavern, arena, trade, smithy, etc.)
- Synthetic encounter creation and cleanup tracking
- Seamless return to enlisted status with state restoration

## Behavior

**Settlement Detection:**
1. Lord enters town/castle -> Settlement entry event detected
2. "Visit Town/Castle" option becomes available in enlisted menu
3. Button text dynamically updates based on settlement type

**Town Access Flow:**
1. Player clicks "Visit Town/Castle" -> Initiate synthetic encounter sequence
2. Clean existing menu stack -> Exit from enlisted menus safely
3. Create synthetic outside encounter -> Temporary party activation for encounter
4. Switch to outside menu -> town_outside or castle_outside
5. Player uses vanilla town options -> "Enter through gates", "Visit tavern", etc.

**Return Flow:**
1. Player clicks "Return to Army Camp" from any settlement menu
2. Clean up synthetic encounter -> End encounter and deactivate party
3. Restore invisible enlisted state -> Return to hidden party following
4. Return to enlisted menu -> Resume normal military service

## Technical Implementation

**Files:**
- `EnlistedMenuBehavior.cs` - Synthetic encounter creation and management
- `EnlistmentBehavior.cs` - Settlement entry detection for button visibility

**Key APIs:**
```csharp
// Settlement entry detection
CampaignEvents.SettlementEntered.AddNonSerializedListener(this, OnSettlementEnteredForButton);

// Synthetic encounter creation
EncounterManager.StartSettlementEncounter(MobileParty.MainParty, settlement);

// State management
bool _syntheticOutsideEncounter; // Track synthetic encounters
MobileParty.MainParty.IsActive = false; // Restore invisible state
```

**Critical Sequence:**
```csharp
// 1. Clean existing state
if (PlayerEncounter.Current != null)
    PlayerEncounter.Finish(true);

// 2. Temporarily activate for encounter
bool wasActive = MobileParty.MainParty.IsActive;
if (!wasActive) MobileParty.MainParty.IsActive = true;

// 3. Create synthetic encounter
EncounterManager.StartSettlementEncounter(MobileParty.MainParty, settlement);
_syntheticOutsideEncounter = true;

// 4. Show outside menu safely
GameMenu.SwitchToMenu(settlement.IsTown ? "town_outside" : "castle_outside");
```

## Edge Cases

**Encounter Already Active:**
- Detects existing encounters for same settlement
- Reuses existing outside menus when available
- Prevents duplicate encounter creation

**No Outside Menu on Stack:**
- Creates synthetic encounter when none exists
- Handles invisible enlisted party state correctly
- Provides proper engine-expected encounter flow

**Multiple Settlement Types:**
- Supports both towns (town_outside) and castles (castle_outside)
- Dynamic menu selection based on settlement type
- Excludes villages (not relevant for army operations)

**State Cleanup:**
- Tracks synthetic encounters with boolean flag
- Restores party invisibility when returning to enlisted status
- Complete encounter cleanup prevents assertion errors

## Acceptance Criteria

- [x] "Visit Town/Castle" button appears only when lord is in appropriate settlement
- [x] Button text updates dynamically ("Visit Town" vs "Visit Castle")
- [x] No "Player encounter must be null!" assertion errors
- [x] No "LocationEncounter should be TownEncounter" assertion errors  
- [x] Full access to all settlement locations (tavern, arena, trade, smithy)
- [x] Seamless return navigation from all settlement menus
- [x] Proper state restoration (invisible party, enlisted status)
- [x] Works for both individual lords and army scenarios

## Debugging

**Common Issues:**
- **Button not appearing**: Check lord settlement entry detection and _lordJustEnteredSettlement flag
- **Assertion errors**: Verify synthetic encounter creation sequence and cleanup
- **Return not working**: Check _syntheticOutsideEncounter flag and party state restoration

**Log Categories:**
- "Interface" - Town access operations and synthetic encounters
- "Settlement" - Settlement entry detection and button visibility
- Look for encounter creation and cleanup messages in debug logs

**Testing:**
- Enlist with lord, follow to town, check button appears
- Click Visit Town, verify outside menu opens without assertions
- Access town locations (tavern, trade, etc.), confirm no crashes
- Return to camp, verify synthetic encounter cleanup and invisible state restoration

## Key Insights

**Why This Solution Works:**
1. **Addresses Root Cause**: Invisible enlisted parties need synthetic encounters for settlement access
2. **Engine-Friendly**: Uses proper StartSettlementEncounter API instead of forcing encounter types
3. **Safe Entry Points**: town_outside/castle_outside menus are validated by engine
4. **Complete State Management**: Proper creation, tracking, and cleanup of synthetic encounters

**Previous Failed Approaches:**
- Direct town menu activation (wrong encounter type)
- Manual encounter type creation (engine validation failures)  
- Mixed pattern approaches (incomplete state management)

This system enables full town access functionality for enlisted soldiers while avoiding assertion crashes and state conflicts.
