# Enlisted – Military Service Mod

Serve as a soldier in any lord's warband. Follow orders, earn wages, climb ranks, and retire with honor.

## Features

### 🛡️ Enlistment
- Talk to any lord: "I wish to serve in your warband."
- You join their party as a regular soldier.
- **No Personal Loot/Prisoners**: Spoils go to your lord.
- **No Starvation**: Your lord provides food.
- **Wages**: Paid daily based on your rank.

### ⚔️ Battle System
- **Automatic Deployment**: You spawn in your assigned formation (Infantry, Archer, Cavalry, etc.).
- **Strict Command Structure**:
  - You are a soldier, not a general.
  - You cannot issue orders (F1-F9 disabled).
  - You must follow your Sergeant's commands.
- **No Order of Battle**: The deployment screen is suppressed for immersion.

### 🎖️ Rank & Progression
- **7 Tiers**: From Levy to Household Guard.
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

### 🚪 Retirement
- **Term of Service**: 252 days (3 years).
- **Honorable Discharge**: Retire with a gold bonus and relation boost.
- **Re-enlist**: Sign on for another year for a large signing bonus.
- **Desertion**: Leave early at the cost of honor (crime rating + relation penalty).

## Installation

1. **Requirements**:
   - Mount & Blade II: Bannerlord v1.3.8
   - Bannerlord.Harmony

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
- `enlisted.log`: General mod activity.
- `conflicts.log`: Diagnostics for mod conflicts.
