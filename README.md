# Enlisted – Military Service Mod

Serve as a soldier in any lord's warband. Follow orders, earn wages, climb ranks, and retire with honor.

## What's New (v0.5.8)
- Siege auto-resolve crash guard: stabilizes PlayerEncounter.Finish after sieges.
- Abandon army suppressed while enlisted (raid, army_wait, encounter menus) with deferred-safe patches.
- Formation assignment: confirms duty formation and teleports you in; no command authority below Tier 4 (expected).
- Sea-lord conversation path fixed so enlisted dialogs appear correctly.
- Cleanup: removed obsolete CombatStats behaviors; version bump to 0.5.8.

## Features

### 🛡️ Enlistment
- Talk to any lord: "I wish to serve in your warband."
- You join their party as a regular soldier.
- **Army Leader Restriction**: Cannot enlist while leading your own army (must disband first).
- **No Personal Loot/Prisoners**: Spoils go to your lord.
- **No Starvation**: Your lord provides food (embedded food sharing; starvation cohesion penalties are compensated).
- **Wages**: Paid daily based on your rank.

### ⚔️ Battle System
- **Automatic Deployment**: You spawn in your assigned formation (Infantry, Archer, Cavalry, etc.).
- **Auto-Join Battles**: Automatically join your lord's side when they enter combat - no menu interruptions.
- **Strict Command Structure**:
  - You are a soldier, not a general.
  - You cannot issue orders (F1-F9 disabled).
  - You must follow your Sergeant's commands.
- **No Order of Battle**: The deployment screen is suppressed for immersion.

### 🎖️ Rank & Progression
- **6 Tiers**: Levy → Footman → Serjeant → Man-at-Arms → Banner Serjeant → Household Guard.
- **Promotions**: Earn XP from battles and kills to reach the next tier.
- **Rewards**: Higher daily wages and access to better equipment.

### 📋 Duties System
- **Daily Assignments**: Choose a duty in the Camp Menu to earn bonus XP and Gold.
  - **Combat Duties**: Guard, Sentry, Patrol.
  - **Support Duties**: Forager, Cook, Messenger.
  - **Specialist Duties**: Quartermaster Hand, Field Medic (requires skills).
- **Passive Training**: Gain skill XP daily based on your formation (e.g., Athletics for Infantry, Riding for Cavalry).

### 📦 Equipment
- **Quartermaster**: Visit the Quartermaster in the Camp Menu to purchase gear.
- **Tiered Kits**: Unlocks culture-specific equipment matching your rank (e.g., Legionary gear for Empire).
- **Allowance**: Officers receive equipment discounts.

### ⚓ Naval Support
- Fully compatible with naval mods.
- Your party follows the lord onto ships seamlessly.
- **Ship Protection**: Your ships are protected from damage while enlisted.
- **Naval Exclusion**: Lords cannot use your ships for navigation.
- **Stranding Protection**: Prevents the Naval DLC stranded-at-sea UI while enlisted when your lord/army has ships; only triggers raft state when no naval capability remains.

### 🚪 Retirement
- **Term of Service**: 252 days (3 years).
- **Honorable Discharge**: Retire with a gold bonus and relation boost.
- **Re-enlist**: Sign on for another year for a large signing bonus.
- **Desertion**: Leave early at the cost of honor (crime rating + relation penalty).

## Installation

1. **Requirements**:
   - Mount & Blade II: Bannerlord v1.3.10
   - Bannerlord.Harmony v2.3.6+

2. **Setup**:
   - Extract the `Enlisted` folder to `<Bannerlord>\Modules\`.
   - Enable `Bannerlord.Harmony` and `Enlisted` in the launcher.
   - **Load Order**: Harmony -> Native -> ... -> Enlisted.

## Configuration

Customize the mod via JSON files in `Modules\Enlisted\ModuleData\Enlisted\`:
- `enlisted_config.json`: Wages, XP thresholds, retirement rules.
- `duties_system.json`: Duty definitions and rewards.
- `settings.json`: Logging levels.

## Troubleshooting

Logs are located in `<Bannerlord>\Modules\Enlisted\Debugging\`:
- `Session-A/B/C_{timestamp}.log`: Rotating session logs (A = newest). Share Session-A.
- `Conflicts-A/B/C_{timestamp}.log`: Rotating conflict diagnostics (A = newest). Share Conflicts-A.
- `Current_Session_README.txt`: Quick pointers to the newest logs.
- Legacy aliases remain (`enlisted.log`, `conflicts.log`) but the rotating files are canonical.
- `dialogue.log`: Conversation system events.

**Note**: Conflict diagnostics are written at startup and refreshed when a campaign begins. Always include the newest Session-A and Conflicts-A when reporting issues.

## Reporting issues
- Steam comments cannot take file uploads; share links instead (Drive/Dropbox/GitHub Gist/Pastebin).
- Include logs: `Modules\Enlisted\Debugging\enlisted.log` and `conflicts.log` (zip if large).
- If crashing, include crash folder from `C:\ProgramData\Mount and Blade II Bannerlord\crashes\` (zip).
- List Bannerlord version, Harmony version, and full mod list with load order.
- Note how you installed (Steam Workshop vs Nexus) and steps to reproduce the issue.