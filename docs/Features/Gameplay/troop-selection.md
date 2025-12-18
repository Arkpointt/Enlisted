# Feature Spec: Troop Selection (Legacy)

> **Status:** DEPRECATED (Phase 7)
> 
> The troop selection system has been replaced by the **Proving Event** system. Formation choice now happens during the T1->T2 promotion event, providing a narrative experience instead of a menu.

## Overview

The original troop selection system let enlisted players choose a Bannerlord troop template to determine their formation and equipment. This has been superseded by:

- **Formation Choice**: Made during the T1->T2 proving event ("Finding Your Place")
- **Equipment Access**: Purchased from the Quartermaster based on formation + tier + culture
- **Starter Duty**: Auto-assigned based on chosen formation

## Legacy Behavior (Removed)

~~The Master at Arms popup allowed players to:~~
- ~~Pick a troop template from the lord's culture~~
- ~~Derive formation from troop properties (IsRanged, IsMounted)~~
- ~~Auto-equip gear from the selected troop at Tier 1~~

## Current Behavior (Phase 7)

### Formation Selection
- **T1**: Everyone starts as Infantry
- **T1->T2 Promotion**: Proving event presents formation options:
  - "I fight best on foot" -> Infantry
  - "Give me a bow and I'll put arrows where they need to go" -> Archer
  - "I belong in the saddle" -> Cavalry
  - "Horse archery is my calling" -> Horse Archer (Khuzait/Aserai only)
- **T2+**: Formation is locked (cannot change)

### Equipment Access
- **T1**: Basic levy gear from bag check or starter kit
- **T2+**: Quartermaster only — buy gear for your formation/tier/culture
- No auto-equip on promotion; player visits Quartermaster for new kit

### Technical Notes

The `TroopSelectionManager.cs` class still exists for:
- Data access (troop trees, culture lookup)
- Migration of existing saves (deriving formation from selected troop)

The menu trigger in `EnlistmentBehavior.cs` is disabled. Equipment is now handled by `QuartermasterManager.GetAvailableEquipmentByFormation()`.

## Related Docs
- [Quartermaster](../UI/quartermaster.md) — Equipment purchasing
- [Duties System](../Core/duties-system.md) — Formation-based duties
- [Enlistment](../Core/enlistment.md) — Promotion and proving events
