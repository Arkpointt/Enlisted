# Debug: Trigger Muster On-Demand

## Quick Start

To trigger the muster system at any time for testing:

### Method 1: In-Game Debug Button (Easiest!)

**1. Enable debug tools in settings:**

Edit `ModuleData/Enlisted/settings.json`:
```json
{
  "EnableDebugTools": true
}
```

**2. In-game access:**
- While enlisted, open the main enlisted menu (press 'L' or click your party)
- Click "Debug Tools" at the bottom of the menu
- Select "🔧 Trigger Muster" from the popup
- Muster sequence starts immediately!

The debug option only appears when `EnableDebugTools: true` and you're enlisted.

### Method 2: Via Console Command (if you have a console mod installed)

```
Enlisted.Debugging.Behaviors.DebugToolsBehavior.TriggerMuster()
```

### Method 3: Temporary Code Injection

If you're actively developing, you can call it from any behavior:

```csharp
Enlisted.Debugging.Behaviors.DebugToolsBehavior.TriggerMuster();
```

## What It Does

- Bypasses the normal 12-day muster cycle
- Immediately opens the muster intro stage
- Runs through all 8 muster stages (intro, pay, baggage check, inspection, recruit, promotion recap, retinue, complete)
- Perfect for testing:
  - Pay options (accept, recount, side deal, IOU)
  - Baggage inspections (contraband detection)
  - Equipment inspection events
  - Green recruit mentoring
  - Promotion recap display (if you were promoted recently)
  - Retinue muster (T7+ only)
  - XP sources breakdown
  - Period summaries

## Requirements

- Must be enlisted with a lord
- `MusterMenuHandler` must be registered (automatically happens on game start)
- Debug tools enabled in `settings.json` (for menu option)

## Notes

- The muster will use your current pay owed, XP, tier, and service record
- All muster outcomes (pay, inspections, etc.) will apply as normal
- Cooldowns for inspection/recruit events still apply (12-day and 10-day)
- Promotion recap only shows if you were actually promoted during the period

## Logging

All debug muster triggers are logged to:
```
C:\Program Files (x86)\Steam\steamapps\common\Mount & Blade II Bannerlord\Modules\Enlisted\Debugging\Session-A_*.log
```

Search for `[Debug] TriggerMuster` to find debug trigger events.

## Visual Example

When `EnableDebugTools: true`, the main enlisted menu includes a "Debug Tools" option:

```
ENLISTED STATUS MENU
─────────────────────────────
[Camp]
[Orders]
[Decisions]
[Reports]
[Status]
[Debug Tools]    <-- Click here
[Visit Settlement]
```

Clicking "Debug Tools" opens a popup with multiple debug options:

```
DEBUG TOOLS
─────────────────────────────
Select a debug action:

○ Give 1000 Gold
○ Give XP to Rank Up
○ Test Onboarding Screen
○ Force Event Selection
○ Reset Event Window
○ List Eligible Events
○ Clear Event Cooldowns
○ Show Event Pacing Info
○ 🔧 Trigger Muster    <-- Select this

[OK]  [Cancel]
```

The 🔧 wrench emoji makes it easy to spot in the popup!

