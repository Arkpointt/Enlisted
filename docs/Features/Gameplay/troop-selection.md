# Feature Spec: Troop Selection System

## Overview

On promotion, players choose from real Bannerlord troops and receive their equipment. Equipment is tracked for
accountability.

## Purpose

Provide authentic military progression where you become actual troop types (Imperial Legionary, Aserai Mameluke) and
inherit their gear. Equipment is free but tracked - missing gear on troop change results in pay deduction.

## Inputs/Outputs

**Inputs:**

- Player promotion trigger (XP threshold reached)
- Current enlisted lord's culture
- Player's new tier level
- Available troops in the game matching culture and tier

**Outputs:**

- Equipment replaced with selected troop's gear
- Equipment tracked for accountability
- Missing gear check (gold deducted if gear lost)
- Visual feedback showing equipment changes

## Behavior

1. **Promotion Trigger**: Player reaches XP threshold → "Promotion!" notification
2. **Accountability Check**: System checks for missing equipment from previous issue
3. **Gold Deduction**: Player charged for any missing gear
4. **Troop Menu**: Shows real troops filtered by culture and tier
5. **Selection**: Player picks troop (Imperial Legionary, Battanian Fian, etc.)
6. **Equipment Issue**: New troop's equipment assigned and tracked
7. **Feedback**: Character model updates, confirmation shown

## Equipment Accountability

When changing troop types:

- System compares current gear vs issued gear
- Missing items = gold deducted from pay
- Player notified of missing items and cost
- New equipment issued and tracking reset

**Exception - Retirement**: No accountability check. Player keeps all gear as reward for service.

## Technical Implementation

**Files:**

- `TroopSelectionManager.cs` - Troop filtering, selection, and equipment tracking
- `EquipmentManager.cs` - Equipment backup/restore for enlistment lifecycle
- `QuartermasterManager.cs` - Equipment variant selection (free, 2-item limit)

**Key Classes:**

- `IssuedItemRecord` - Tracks item ID, name, value, slot for accountability
- `_issuedEquipment` - Dictionary storing issued gear per slot

**Key Methods:**

- `RecordIssuedEquipment()` - Store current gear for tracking
- `CheckMissingEquipment()` - Compare current vs issued, return debt
- `ClearIssuedEquipment()` - Clear tracking (retirement/full discharge)
- `MarkIssuedItemReturned()` - Remove item from tracking when returned via quartermaster
- `ShowMasterAtArmsPopup()` - Display troop selection popup (preserves time state)

## Edge Cases

**Missing Equipment on Troop Change:**

- Calculate total value of missing items
- Deduct from player gold
- Show popup with missing items list
- Log transaction for debugging

**Retirement with Missing Gear:**

- No accountability check (retirement perk)
- Player keeps current equipment
- Personal belongings returned to inventory

**Insufficient Gold for Missing Gear:**

- Debt logged but not blocked
- Player still gets new equipment

## Acceptance Criteria

- ✅ Players see real troop names in promotion menu
- ✅ Equipment tracked from moment of issue
- ✅ Missing gear detected on troop change
- ✅ Gold deducted for missing equipment
- ✅ Clear notification of missing items and cost
- ✅ Retirement skips accountability (keeps all gear)
- ✅ Personal belongings restored to inventory on retirement

## Debugging

**Log Categories:**

- "TroopSelection" - Troop filtering and menu operations
- "Equipment" - Tracking and accountability

**Key Log Points:**

- "Missing equipment check: X items, Y gold debt"
- "Retirement: skipping equipment accountability"
- "Issued equipment recorded: X items tracked"
