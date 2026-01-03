# Bug Investigation Report
**Date:** January 1, 2025  
**Reporter:** Deacon (user)  
**Status:** Issues CONFIRMED

---

## Summary

Three interconnected issues reported by user:
1. ✅ **CONFIRMED** - Quartermaster menu showing companion recruitment options
2. ✅ **CONFIRMED** - XP display showing "Next Tier Requirement: 0XP" 
3. ✅ **CONFIRMED** - Player stuck at Tier 2 with 354,681 XP (should be Tier 9+)

---

## Issue 1: Quartermaster Shows Companion Recruitment Menu

### Symptoms
- User opens quartermaster dialogue
- Sometimes sees "pay 10k to join you or close the menu" (companion recruitment)
- Quartermaster services unavailable except via "Access Baggage Train"

### Root Cause
**Conversation routing failure.** The quartermaster dialogue condition check at `EnlistedDialogManager.cs:1406-1416`:

```csharp
private bool IsQuartermasterConversation()
{
    var enlistment = EnlistmentBehavior.Instance;
    if (enlistment == null || !enlistment.IsEnlisted)
    {
        return false;
    }

    var qm = enlistment.QuartermasterHero;
    return qm != null && qm.IsAlive && Hero.OneToOneConversationHero == qm;
}
```

This check fails when:
- `QuartermasterHero` is null or dead
- The conversation hero doesn't match the quartermaster hero
- When the check fails, Bannerlord falls back to native companion recruitment dialogue

### Why It Happens
1. **Hero Creation Failure**: `CreateQuartermasterForLord()` may fail silently (returns null)
2. **Hero Lost**: Quartermaster hero died or was removed from party
3. **Conversation Mismatch**: Opening conversation with wrong hero due to timing/state issues
4. **Occupation Setting**: At line 10250 of `EnlistmentBehavior.cs`, QM is set to `Occupation.Soldier`, but this may not fully prevent companion recruitment dialogue from triggering

### Evidence Locations
- `EnlistmentBehavior.cs:10173-10192` - GetOrCreateQuartermaster()
- `EnlistmentBehavior.cs:10199-10280` - CreateQuartermasterForLord()
- `EnlistedDialogManager.cs:1406-1416` - IsQuartermasterConversation()
- `EnlistedMenuBehavior.cs:4156-4208` - OnQuartermasterSelected()

---

## Issue 2: XP Display Shows "0XP" as Next Requirement

### Symptoms
- Service Records shows: "Next Tier Requirement: 0XP"
- Player has 354,681 XP at Tier 2

### Root Cause
**Array indexing logic error or corrupted data.** The display code at `CampMenuHandler.cs:880-884`:

```csharp
if (enlistment.EnlistmentTier < 6)
{
    var tierXp = Mod.Core.Config.ConfigurationManager.GetTierXpRequirements();
    var nextTierXp = enlistment.EnlistmentTier < tierXp.Length ? tierXp[enlistment.EnlistmentTier] : tierXp[tierXp.Length - 1];
    sb.AppendLine($"{nextTierLabel}: {nextTierXp} XP");
}
```

**Expected behavior:**
- Player at Tier 2 → `tierXp[2]` should return 3000 (Tier 3 requirement)
- The condition `enlistment.EnlistmentTier < 6` is true (2 < 6)
- Should display "Next Tier Requirement: 3000 XP"

**Actual behavior:**
- Displays "Next Tier Requirement: 0XP"

### Possible Causes
1. **Config Loading Failure**: `progression_config.json` failed to load
2. **Array Corruption**: The `GetTierXpRequirements()` array wasn't populated correctly
3. **Tier Data Corruption**: `enlistment.EnlistmentTier` has an invalid value (negative, NaN, etc.)
4. **Max Tier Override**: Something set max tier to 2 or less

### Investigation Required
Need to check game log file at:
`C:\Program Files (x86)\Steam\steamapps\common\Mount & Blade II Bannerlord\Modules\Enlisted\Debugging\enlisted.log`

Look for:
- Config loading errors
- "Cannot promote" messages with reasons
- Tier XP array initialization logs

---

## Issue 3: Player Stuck at Tier 2 with Excessive XP

### Symptoms
- 354,681 XP at Tier 2
- Should be at Tier 9 (requires 65,000 XP cumulative)
- No promotion events triggered

### Expected Requirements for Tier 3
From `progression_config.json` and `PromotionBehavior.cs`:
- **XP**: 3,000 (player has 354,681 ✓)
- **Days in Rank**: 35 days
- **Battles**: 6 battles
- **Soldier Reputation**: ≥10
- **Leader Relation**: ≥10
- **Max Discipline**: <7

### Root Cause
**Promotion system not running OR blocked by requirements.**

The hourly promotion check (`PromotionBehavior.cs:267-273`) should trigger automatically:

```csharp
private void OnHourlyTick()
{
    _lastPromotionCheck = CampaignTime.Now;
    CheckForPromotion();
}
```

### Possible Blocking Conditions

1. **PromotionBehavior not initialized**: Behavior didn't register with CampaignEvents
2. **Requirements not met**: Despite massive XP, other requirements could block:
   - Days in Rank < 35
   - Battles < 6
   - Soldier Reputation < 10
   - Leader Relation < 10
   - Discipline ≥ 7
3. **Promotion previously declined**: Check at line 316-320 prevents auto-promotion if player declined
4. **Pending promotion stuck**: `_pendingPromotionTier` may be stuck on a value
5. **Event system failure**: Proving event not found or EventDeliveryManager unavailable

### Debug Logging
The system should log at 75%+ progress (line 305-309):
```
"Promotion to T3 blocked (XX% progress): [reasons]"
```

If no logs appear, the hourly tick isn't running.

---

## Verification Steps

### For the User (Deacon)
1. Check if log file exists and has recent entries:
   - `C:\Program Files (x86)\Steam\steamapps\common\Mount & Blade II Bannerlord\Modules\Enlisted\Debugging\enlisted.log`
2. In Service Records menu, note exact values:
   - Days Served
   - Battles count
   - Fatigue level
3. Check Character stats:
   - Relation with current lord
   - Any active discipline penalties
4. Try talking to lord ("My Lord..." option) and see if promotion request is available

### For Developer Testing
1. Add diagnostic logging to `GetTierXpRequirements()` to verify array contents
2. Add logging to `CheckForPromotion()` to confirm it's being called
3. Log the result of `CanPromote()` every hour when XP > next tier requirement
4. Verify `QuartermasterHero` is not null before opening conversation
5. Add error handling for quartermaster hero creation failures

---

## Recommended Fixes

### High Priority
1. **Add null check guard** before opening QM conversation
2. **Add fallback** if QM hero creation fails (direct menu access)
3. **Log XP array contents** on game load to catch config loading failures
4. **Add promotion debugging command** to manually check promotion status

### Medium Priority
1. **Add save validation** to detect corrupted tier/XP data
2. **Add recovery mechanism** for lost/dead quartermaster heroes
3. **Improve error messages** when promotion is blocked

### Low Priority
1. **Add UI indicator** when close to promotion but blocked
2. **Add admin command** to manually trigger promotion for testing
3. **Add save file migration** to fix corrupted progression data

---

## Next Steps

**Immediate:** Request game log file from user to confirm root causes.

**Development:** Add diagnostic logging to identify which specific requirements are blocking promotion.

**Testing:** Create save file with similar state (T2, high XP) to reproduce issues in controlled environment.
