# ⚠️ LEGACY DOCUMENT - REPLACED

**Status:** 🗄️ Deprecated  
**Replaced By:** [Order Progression System](order-progression-system.md)

---

## This Document Is Outdated

This specification described the **OLD instant-resolution orders system** that has been **replaced** by the Order Progression System.

**Old System (This Doc):**
- Orders assigned every 3-5 days
- Accept → Skill Check → Immediate Result
- Instant success/failure with popup
- One-and-done resolution

**New System (Active):**
- Orders assigned based on world state (orchestrator-coordinated)
- Accept → Multi-Day Progression → Accumulated Consequences
- 4 phases per day (6am, 12pm, 6pm, 12am)
- Events fire during slot phases
- Order states: FORECAST → SCHEDULED → PENDING → ACTIVE → COMPLETE

---

## Where To Find Current Information

| Topic | Current Documentation |
|-------|----------------------|
| **Order Progression System** | [Order Progression System](order-progression-system.md) |
| **Migration Guide** | [Order System Migration](../../Archive/ORDER-SYSTEM-MIGRATION.md) |
| **Orders Content** | [Orders Content](../Content/orders-content.md) |
| **Content System Architecture** | [Content System Architecture](../Content/content-system-architecture.md) |

---

## What Changed

### Order Issuance
- **Old:** Fixed 3-5 day timer
- **New:** Orchestrator-coordinated based on world state (garrison = slow, siege = fast)

### Order Execution
- **Old:** Single skill check, immediate result
- **New:** Multi-day progression with phase-by-phase status updates

### Order Events
- **Old:** None - just success/failure roll
- **New:** Events can fire during slot phases (15-35% base chance per slot)

### Order Outcomes
- **Old:** Binary success/failure
- **New:** Perfect/Adequate/Failed based on accumulated event outcomes

### Player Visibility
- **Old:** No warning, sudden popup
- **New:** FORECAST (12-24h) → SCHEDULED (8-18h) → PENDING (accept/decline) → ACTIVE → COMPLETE

---

**This file kept for reference only. Do not implement this system.**

**For implementation, use:**
- [Order Progression System](order-progression-system.md)
- [ORDER-SYSTEM-MIGRATION.md](../../Archive/ORDER-SYSTEM-MIGRATION.md) for removal instructions
