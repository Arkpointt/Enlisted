# Enlisted – Military Service Mod

Serve as a soldier in any lord's warband. Follow orders, earn wages, climb ranks, and retire with honor.

## Features

### 🛡️ Enlistment
- Talk to any lord: "I wish to serve in your warband."
- You join their party as a regular soldier.
- **Army Leader Restriction**: Cannot enlist while leading your own army (must disband first).
- **No Personal Loot/Prisoners**: Spoils go to your lord.
- **No Starvation**: Your lord provides food (embedded food sharing; starvation cohesion penalties are compensated).
- **Wages**: Daily wages accrue into a muster ledger; paid periodically at pay muster incidents (~12 days) with multiple payout options (Standard Pay, Corruption Challenge, Side Deal, Final Muster).

### ⚔️ Battle System
- **Automatic Deployment**: You spawn in your assigned formation (Infantry, Archer, Cavalry, etc.).
- **Auto-Join Battles**: Automatically join your lord's side when they enter combat - no menu interruptions.
- **Strict Command Structure**:
  - You are a soldier, not a general.
  - You cannot issue orders (F1-F9 disabled).
  - You must follow your Sergeant's commands.
- **No Order of Battle**: The deployment screen is suppressed for immersion.

### 🎖️ Rank & Progression
- **6 Tiers**: Levy (Tier 1) → Footman (Tier 2) → Serjeant (Tier 3) → Man-at-Arms (Tier 4) → Banner Serjeant (Tier 5) → Household Guard (Tier 6).
- **Promotions**: Earn XP from battles and kills (+25 daily, +25 per battle, +1-2 per kill) to reach the next tier.
- **XP Thresholds**: Tier 2 (800 XP), Tier 3 (3,000 XP), Tier 4 (6,000 XP), Tier 5 (11,000 XP), Tier 6 (19,000 XP).
- **Rewards**: Higher daily wages (accrued to muster ledger), access to better equipment, and command privileges (Tier 4+: personal retinue).

### 📋 Duties System
- **Daily Assignments**: Choose a duty in the Camp Menu to earn bonus XP and Gold.
  - **Combat Duties**: Guard, Sentry, Patrol.
  - **Support Duties**: Forager, Cook, Messenger.
  - **Specialist Duties**: Quartermaster Hand, Field Medic (requires skills).
- **Passive Training**: Gain skill XP daily based on your formation (e.g., Athletics for Infantry, Riding for Cavalry).

### 📦 Equipment
- **Quartermaster**: Visit the Quartermaster from the **Enlisted Status** menu to **purchase** gear (prices use `soldier_tax`), and sell gear back via **buyback**.
- **Tiered Kits**: Unlocks culture-specific equipment matching your rank (e.g., Legionary gear for Empire).
- **Discounts**: Provisioner duty / Quartermaster role gets 15% better Quartermaster prices.

### ⚓ Naval Support
- Fully compatible with naval mods.
- Your party follows the lord onto ships seamlessly.
- **Ship Protection**: Your ships are protected from damage while enlisted.
- **Naval Exclusion**: Lords cannot use your ships for navigation.
- **Stranding Protection**: Prevents the Naval DLC stranded-at-sea UI while enlisted when your lord/army has ships; only triggers raft state when no naval capability remains.

### 🚪 Discharge & Retirement
- **Pending Discharge**: Request discharge in the Camp ("My Camp") menu; resolves at the next pay muster (Final Muster branch).
- **Discharge Bands**: Service length determines rewards:
  - **Washout** (<100 days): Relation penalties, no pension, gear stripped.
  - **Honorable** (100-199 days): Relation bonuses, severance gold, pension 50/day, keep armor/lose weapons.
  - **Veteran/Heroic** (200+ days): Larger relation bonuses, severance gold, pension 100/day, same gear handling.
  - **Smuggle** (deserter): Keep all gear, crime +30, relation penalties, no pension.
- **Pensions**: Daily payments pause on re-enlistment, stop if relation drops or at war, update on next retirement to new band.
- **Re-entry System**: When re-enlisting with the same faction, your reservist record provides benefits:
  - **Washout/Deserter**: Start at Tier 1 (raw recruit), probation status (reduced wages, fatigue cap).
  - **Honorable Discharge**: Start at Tier 3 (NCO path), +500 XP bonus, +5 relation bonus.
  - **Veteran/Heroic Discharge**: Start at Tier 4 (officer path), +1,000 XP bonus, +10 relation bonus.
- **Probation**: Applied on washout/deserter re-entry; reduces wage multiplier and caps fatigue; clears on pay muster resolution or after configurable duration.

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
- `enlisted_config.json`: Wages, XP thresholds, pay muster intervals, discharge/retirement rules, probation settings.
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
- Note how you installed and steps to reproduce the issue.