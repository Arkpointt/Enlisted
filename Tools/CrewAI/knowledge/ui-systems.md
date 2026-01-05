# Enlisted UI Systems

**Canonical Reference:** `docs/Features/UI/ui-systems-master.md`

## Core UI Behaviors

| Behavior | Location | Purpose |
|----------|----------|---------|
| EnlistedMenuBehavior | `src/Features/Interface/Behaviors/EnlistedMenuBehavior.cs` | Main menu coordinator, accordion display |
| CampMenuHandler | `src/Features/Camp/CampMenuHandler.cs` | Camp Hub menu with phase-aware opportunities |
| MusterMenuHandler | `src/Features/Enlistment/Behaviors/MusterMenuHandler.cs` | 12-day muster sequence (pay, promotions, stats) |
| EnlistedNewsBehavior | `src/Features/Interface/Behaviors/EnlistedNewsBehavior.cs` | News feed integration |
| EnlistedCombatLogBehavior | `src/Features/Interface/Behaviors/EnlistedCombatLogBehavior.cs` | Combat log messages |

## Menu Systems

### Camp Hub Menu (Main Enlisted Menu)

**Accessed via:** C key (camp menu) or clan menu
**Handler:** `EnlistedMenuBehavior`

**Accordion Sections:**
1. **NOW** - Current phase opportunities (pre-scheduled by ContentOrchestrator)
2. **AHEAD** - Tomorrow's opportunities (24h rolling window)
3. **DECISIONS** - Player-initiated actions (training, social, medical)
4. **REPORTS** - Company status, daily brief, forecasts
5. **RETINUE** (T7+) - Personal retinue management
6. **QUARTERMASTER** - Equipment, provisions, baggage access

### Decisions Menu

**Content:** `ModuleData/Enlisted/Decisions/*.json`
**Categories:**
- Training (weapon drill, tactics, conditioning)
- Social (war stories, card game, help wounded)
- Medical (surgeon, rest, treatment)
- Economic (gambling, trade)

**Key fields:**
```json
{
  "id": "dec_rest",
  "category": "decision",
  "targetDecision": "dec_rest_sleep",
  "requirements": { "notAtSea": true }
}
```

### Muster Menu System

**Triggers:** Every 12 days at 6am daily tick
**Sequence:**
1. Muster Intro - Period recap, attendance
2. Pay Line - Wage payment, tension check
3. Green Recruit (T1→T2 only) - Mentoring event
4. Promotion Recap (if promoted) - Tier advancement ceremony
5. Retinue Muster (T7+ only) - Retinue loyalty/status
6. Muster Complete - Dismiss

**Implementation:** Multi-stage GameMenu, not popup inquiry

## Event Delivery

### Popup Inquiry System

**Handler:** `EventDeliveryManager.Instance.QueueEvent()`
**Display:** `MultiSelectionInquiryData` with options
**Content:** Events from `ModuleData/Enlisted/Events/*.json`

**Event flow:**
```
Event queued → Requirements checked → Popup shown → 
Player chooses → Effects applied → Result text shown
```

### Map Incidents

**Triggers:** Battle end, settlement entry/exit, hourly checks
**Contexts:** `leaving_battle`, `entering_town`, `during_siege`, `waiting_in_settlement`
**Manager:** `MapIncidentManager`

## Localization

**Strings file:** `ModuleData/Languages/enlisted_strings.xml`
**Pattern:** Dual-field fallback
```json
{
  "titleId": "dec_rest_title",
  "title": "Rest"
}
```

If `titleId` not found in XML, uses `title` fallback.

## Gauntlet Integration

**Custom screens:**
- Camp Hub (accordion menu with phase-aware opportunities)
- Quartermaster Equipment Selector
- Quartermaster Provisions
- Troop Selection Manager

**Location:** `src/Features/Equipment/UI/` and `src/Features/Interface/Behaviors/`

**Key classes:**
- `QuartermasterEquipmentSelectorBehavior` - Equipment screen
- `QuartermasterProvisionsBehavior` - Provisions screen
- `TroopSelectionManager` - Companion/troop assignments

## News Feed System

**Purpose:** Aggregates notifications into a scrollable feed
**Categories:**
- Order completion
- Promotion
- Reputation changes
- Company incidents
- Routine outcomes

**Integration:** Combat log messages also appear in news feed for persistence

## Color Coding

| Message Type | Color | Usage |
|--------------|-------|-------|
| Positive | Green | XP gains, rewards, success |
| Costs | Yellow | Gold spent, fatigue |
| Rewards | Cyan | Order completion, promotions |
| Warnings | Red | Injuries, failures, discipline |

## Common UI Patterns

**Checking if menu is open:**
```csharp
var currentMenu = Campaign.Current?.CurrentMenuContext?.GameMenu?.StringId;
if (currentMenu.StartsWith("enlisted_muster_")) {
    // Block opportunities during muster
}
```

**Phase-aware display:**
```csharp
var currentPhase = WorldStateAnalyzer.GetCurrentDayPhase();
var opportunities = ContentOrchestrator.Instance.GetCurrentPhaseOpportunities();
// Display with phase indicator (Dawn/Midday/Dusk/Night)
```

**Commitment model (future opportunities):**
```csharp
var scheduledOpp = orchestrator.GetAllTodaysOpportunities();
if (IsPhaseFuture(opp.Phase, currentPhase)) {
    // Show "Schedule" button (greys out, auto-fires at phase)
} else {
    // Show "Do Now" button (fires immediately)
}
```

## Error Codes

| Code | Meaning |
|------|---------|
| E-UI-046 | Menu/accordion failure |
| E-UI-047 | Gauntlet layer error |

See `error-codes.md` for complete list.
