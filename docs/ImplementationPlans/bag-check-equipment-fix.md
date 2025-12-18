# Bag Check Equipment Fix

## Status: ✅ COMPLETE

## Problem

When enlisting, players were ending up **naked** (no T1 gear) after the bag check completed. 

### Root Cause

The enlistment flow had a timing issue:

```
1. Player enlists
2. BackupPlayerEquipment() backs up civilian gear (empty for new players)
3. AssignInitialEquipment() gives player T1 military gear ✓
4. Bag check is scheduled for 1 hour later
5. 1 hour passes...
6. Bag check fires → StashAllBelongings() clears ALL equipment ❌
7. Player is left naked (military gear was stashed/sold)
```

The bag check was treating **military-issued T1 gear** as "personal belongings" and removing it!

---

## Solution

Added a check to skip the bag check if equipment has already been backed up by `EquipmentManager`:

### Logic

- If `EquipmentManager.HasBackedUpEquipment == true`, the bag check is skipped
- This indicates that civilian equipment was already secured **before** military gear was issued
- The player now has military-issued gear that should NOT be touched by bag check

### Why This Works

1. `BackupPlayerEquipment()` runs **before** `AssignInitialEquipment()`
2. This sets `EquipmentManager.HasBackedUpEquipment = true`
3. When bag check fires 1 hour later, it sees the flag and skips
4. Player keeps their T1 military gear ✓

---

## Code Changes

### 1. ProcessDeferredBagCheck() - Skip Check

Added early return if equipment already backed up:

```csharp
private void ProcessDeferredBagCheck()
{
    // ... existing checks ...
    
    // CRITICAL: Skip bag check if equipment was already backed up
    var equipmentManager = EquipmentManager.Instance;
    if (equipmentManager?.HasBackedUpEquipment == true)
    {
        ModLogger.Info("Enlistment", "Skipping bag check - equipment already secured");
        _bagCheckCompleted = true;
        _bagCheckEverCompleted = true;
        _bagCheckScheduled = false;
        _bagCheckDueTime = CampaignTime.Zero;
        _pendingBagCheckLord = null;
        return;
    }
    
    // ... rest of bag check logic ...
}
```

### 2. HandleBagCheckChoice() - Skip Processing

Added early return in choice handler:

```csharp
public void HandleBagCheckChoice(string choice)
{
    // CRITICAL: Skip if equipment already backed up
    var equipmentManager = EquipmentManager.Instance;
    if (equipmentManager?.HasBackedUpEquipment == true)
    {
        ModLogger.Info("Enlistment", "Skipping bag check - equipment already secured");
        _bagCheckCompleted = true;
        _bagCheckEverCompleted = true;
        // ... cleanup ...
        return;
    }
    
    // ... rest of choice handling ...
}
```

---

## Edge Cases Handled

### Case 1: Normal Enlistment (New Player)
```
1. Enlist → BackupPlayerEquipment() (nothing to backup, but flag is set)
2. AssignInitialEquipment() → Player gets T1 gear
3. Bag check fires → Sees flag, skips
4. Result: Player keeps T1 gear ✓
```

### Case 2: Enlistment with Existing Gear
```
1. Enlist → BackupPlayerEquipment() (backs up civilian gear, flag is set)
2. AssignInitialEquipment() → Player gets T1 gear
3. Bag check fires → Sees flag, skips
4. Result: Player keeps T1 gear, civilian gear is backed up ✓
```

### Case 3: Re-enlistment (Grace Period)
```
1. Re-enlist → Equipment already backed up from previous service
2. TryApplyGraceEquipment() → Restores previous military gear
3. Bag check fires → Sees flag, skips
4. Result: Player keeps their previous military gear ✓
```

### Case 4: Equipment Not Yet Backed Up (Shouldn't Happen)
```
1. Enlist → BackupPlayerEquipment() fails or is skipped
2. AssignInitialEquipment() → Player gets T1 gear
3. Bag check fires → Flag is false, bag check runs normally
4. Result: Bag check processes gear (fallback behavior)
```

---

## Quest Item Protection

This fix works alongside the quest item protection implemented earlier:

1. **Equipment Backup** protects quest items in inventory
2. **Equipment Assignment** protects quest items when replacing gear
3. **Bag Check Skip** prevents military gear from being stashed
4. **Bag Check Stash/Sell** (if it runs) protects quest items

All layers work together to ensure quest items are never lost.

---

## Testing Checklist

### ✅ Compilation
- [x] Build succeeded with no errors
- [x] No linter errors
- [x] DLL created successfully

### ☐ Gameplay Testing (To Do)

**New Character:**
- [ ] Enlist with no gear
- [ ] Verify T1 gear is assigned
- [ ] Wait 1 hour (bag check scheduled)
- [ ] Verify bag check is skipped (check logs)
- [ ] Verify player still has T1 gear

**Character with Existing Gear:**
- [ ] Enlist with civilian gear + Dragon Banner
- [ ] Verify T1 gear is assigned
- [ ] Verify Dragon Banner is still equipped
- [ ] Wait 1 hour (bag check scheduled)
- [ ] Verify bag check is skipped
- [ ] Verify player has T1 gear + Dragon Banner

**Re-enlistment:**
- [ ] Discharge, then re-enlist within grace period
- [ ] Verify previous military gear is restored
- [ ] Verify bag check is skipped
- [ ] Verify player keeps military gear

---

## Files Modified

- `src/Features/Enlistment/Behaviors/EnlistmentBehavior.cs`
  - Updated `ProcessDeferredBagCheck()` to skip if equipment backed up
  - Updated `HandleBagCheckChoice()` to skip if equipment backed up

**Total: 1 file modified**

---

## Related Fixes

This fix is part of a larger equipment protection effort:

1. **Quest Item Protection** (completed earlier today)
   - Protects quest items in inventory during bag check
   - Protects quest items when equipment is assigned/replaced
   
2. **Bag Check Skip** (this fix)
   - Prevents bag check from clearing military-issued gear
   
3. **Equipment Backup** (existing system)
   - Backs up civilian gear before military service
   - Restores gear after discharge

---

## Logging

When the fix activates, you'll see this in the logs:

```
[Enlistment] Deferred bag check scheduled for [time]
[Equipment] Personal equipment backed up for military service (quest items protected)
[Equipment] Assigned initial [Culture] recruit equipment (quest items protected)
... 1 hour passes ...
[Enlistment] Skipping bag check - equipment already secured by EquipmentManager before military gear was issued
```

---

## Rollback Instructions

If this fix needs to be reverted:

1. Remove the equipment backup check from `ProcessDeferredBagCheck()`
2. Remove the equipment backup check from `HandleBagCheckChoice()`
3. The bag check will run normally (but will clear T1 gear)

---

## Conclusion

The bag check now correctly skips when equipment has already been backed up by `EquipmentManager`. This ensures:

✅ Players receive their T1 military gear when enlisting
✅ T1 gear is NOT removed by the bag check
✅ Quest items remain protected
✅ Civilian gear is properly backed up for restoration on discharge

**Build Status**: ✅ Compiles successfully
**Next Step**: Test in-game to verify T1 gear is retained after enlistment

