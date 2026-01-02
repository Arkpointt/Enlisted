# Baggage Stowage System - Implementation Guide

**Date:** 2026-01-02  
**Status:** Events and localization complete, minimal code changes needed

---

## What Changed

### 1. Event File Renamed
- **Old:** `events_bagcheck.json` (removed)
- **New:** `events_baggage_stowage.json` (created)
- **Event ID:** `evt_baggage_stowage_first_enlistment`

### 2. Skill Check System Added
Four skill-based options for initial baggage stowage (anyone can try, skills improve odds):

| Option | Skill | Tooltip | Effect |
|--------|-------|---------|--------|
| **Pay** | None | "Pay 200g + 5% of item value. Standard storage." | Standard stowage |
| **Charm** | Charm | "{CHANCE}% (Charm {SKILL}). Free storage, +2 QM rep. Fail: Pay fee." | Free + QM rep |
| **Trade** | Trade | "{CHANCE}% (Trade {SKILL}). Half price (100g flat). Fail: Pay full." | Reduced fee |
| **Roguery** | Roguery | "{CHANCE}% (Roguery {SKILL}). Free storage. Fail: 250g, +2 Scrutiny." | Free stowage |
| **Sell** | None | "Sell all at 60%. No storage. Clean break." | No baggage |
| **Abort** | None | "Leave service before it starts. -10 Lord reputation." | Cancel enlistment |

### 3. Simplified Mechanics
All options result in the same storage outcome. The skill checks just determine:
- **Success:** Better fee (or free) and no penalties
- **Failure:** Standard or higher fee, possible reputation/scrutiny penalties

No special tracking needed. The baggage system works the same regardless of how you stowed items.

---

## Code Changes Required

### Existing Effect Handlers Used
The event uses these existing effects that should already work:
- `bagCheckChoice`: "stash" or "sell" - triggers the existing stowage logic
- `gold`: Deduct gold for fees
- `qmRep`: Adjust quartermaster reputation  
- `scrutiny`: Add scrutiny points
- `lordRep`: Adjust lord reputation

### Verify Effect Handlers in EventDeliveryManager.cs
Check that these handlers exist and work correctly:

```csharp
// Gold deduction
if (effects.ContainsKey("gold"))
{
    var amount = Convert.ToInt32(effects["gold"]);
    // Should deduct from player gold
}

// QM reputation
if (effects.ContainsKey("qmRep"))
{
    var amount = Convert.ToInt32(effects["qmRep"]);
    // Should adjust QM relationship
}

// Scrutiny
if (effects.ContainsKey("scrutiny"))
{
    var amount = Convert.ToInt32(effects["scrutiny"]);
    // Should add scrutiny points
}
```

---

## Testing Checklist

- [ ] New event triggers on first enlistment
- [ ] All 6 options display with correct tooltips
- [ ] Skill check tooltips show dynamic success chances
- [ ] Charm success: Free storage, +2 QM rep
- [ ] Charm failure: Pay standard fee, -1 QM rep
- [ ] Trade success: 100g cost only
- [ ] Trade failure: Standard fee (200g + 5%)
- [ ] Roguery success: Free storage
- [ ] Roguery failure: 250g fee, +2 Scrutiny, -3 QM rep
- [ ] Pay: Standard fee
- [ ] Sell: 60% value, no baggage
- [ ] Abort: Enlistment cancelled, -10 Lord rep
- [ ] Items correctly transferred to baggage stash on stow
- [ ] Items correctly sold on liquidation

---

## Summary

✅ **Complete:**
- New event JSON with 4 skill check options
- XML localization strings
- Tooltip templates with dynamic success chances
- Skill-based risk/reward balance

⚠️ **Verify:**
- Existing effect handlers work for gold, qmRep, scrutiny
- bagCheckChoice properly triggers stash/sell logic

---

**Next Steps:**
1. Build and test the event in-game
2. Verify all 6 options work correctly
3. Check skill check calculations display properly
4. Confirm baggage is stored/sold as expected
