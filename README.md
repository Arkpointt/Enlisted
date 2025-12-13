# Enlisted – Military Service Mod

Serve as a soldier in any lord's warband. Follow orders, earn wages, climb ranks, and retire with honor.

## Features

### 🛡️ Enlistment
- Talk to any lord: "I wish to serve in your warband."
- You join their party as a regular soldier.
- **Army Leader Restriction**: Cannot enlist while leading your own army (must disband first).
- **No Personal Loot (T1-T3)**: Low-rank soldiers don't loot. You receive gold share compensation instead.
- **Veteran Loot (T4+)**: Veterans and above earn loot privileges — native loot screens work.
- **No Starvation**: Your lord provides food (starvation cohesion penalties are compensated).

### 💰 Pay System
- **Daily Wages**: Base pay scales with tier (T1: 3 denars → T9: 120 denars).
- **Pay Muster**: Wages accumulate in a ledger; paid at muster events (~12 days).
- **Pay Modifiers**:
  - **Culture**: Aserai +10%, Sturgia -10%, etc.
  - **Wartime Hazard**: +20% when at war.
  - **Lord Wealth**: -25% to +25% based on lord's treasury.
  - **Duty Multiplier**: Specialized duties earn more.
- **Lance Fund**: 5% deduction for shared supplies (returned on discharge).
- **Battle Bonuses**: Gold share from victories based on tier and enemy casualties.

### 😤 Pay Tension System
When pay is late, tension builds (0-100 scale):

| Tension | Effects |
|---------|---------|
| 0-19 | Normal operations |
| 20-39 | Grumbling (-3 morale) |
| 40-59 | Growing unrest (-6 morale, +5% discipline incidents) |
| 60-79 | Severe (-10 morale, **free desertion available**) |
| 80-100 | Crisis (-15 morale, mutiny risk, NPC desertions) |

**Three Paths at High Tension:**
- **Corrupt**: Theft, black market, skim supplies — gold now, consequences later.
- **Loyal**: Help the lord (collect debts, escort merchants, raid enemies) — reduce tension.
- **Leave**: Desert freely at 60+ tension with minimal penalties.

### ⚔️ Battle System
- **Automatic Deployment**: You spawn in your assigned formation.
- **Auto-Join Battles**: Join your lord's battles automatically.
- **Strict Command Structure**: You follow orders, not give them (F1-F9 disabled).
- **Battle Loot Share**: T1-T3 receive gold compensation; T4+ get native loot screens.

### 🎖️ Rank & Progression
- **9 Tiers** with culture-specific rank names:
  - **Enlisted** (T1-T4): Tiro → Miles → Immunes → Principalis (Empire)
  - **Officer** (T5-T6): Evocatus → Centurion
  - **Commander** (T7-T9): Primus Pilus → Tribune → Legate
  - Each culture has unique rank titles.

- **Proving Events**: Promotions through narrative events:
  - T1→T2 "Finding Your Place": Choose your formation
  - T2→T3 "The Sergeant's Test": Prove your judgment
  - T3→T4 "Crisis of Command": Show leadership under fire
  - T4→T5 "The Lance Vote": Earn peer trust
  - T5→T6 "Audience with the Lord": Declare loyalty

- **Promotion Requirements**: XP, days served, events completed, battles survived, reputation.

### 📋 Duties System
- **Formation-Based Assignments**: Formation determines available duties.
  - **Infantry**: Runner, Quartermaster, Field Medic, Armorer, Engineer
  - **Archer**: Scout, Lookout
  - **Cavalry/Horse Archer**: Scout, Messenger

- **Duty Request System** (T2+): Request transfers with lance leader approval.
- **Passive Training**: Daily skill XP based on formation.

### ⚔️ Lance Life Events
Random events that shape your military career:
- **Camp Events**: Social interactions, training opportunities, supply issues.
- **Post-Battle Events**: Victory celebrations, looting the dead, theft invitations.
- **Pay Tension Events**: Grumbling, confrontations, desertion plots, mutiny brewing.
- **Loyal Path Missions**: Collect debts, escort merchants, negotiate loans, raid enemies.
- **Consequences**: Your choices affect Heat, discipline, reputation, and lord relations.

### 📦 Equipment
- **Quartermaster**: Purchase gear based on formation, tier, and culture.
- **NEW Item Indicators**: Newly unlocked items show `[NEW]`.
- **Tier-Gated Loot**: T4+ veterans access native loot screens.

### ⚓ Naval Support
- Compatible with naval mods.
- Party follows lord onto ships seamlessly.
- Ship protection while enlisted.

### 🚪 Discharge & Retirement
- **Pending Discharge**: Request in "My Camp" menu; resolves at next pay muster.
- **Discharge Bands**: Service length determines rewards:
  - **Washout** (<100 days): Penalties, no pension.
  - **Honorable** (100-199): Bonuses, pension 50/day.
  - **Veteran/Heroic** (200+): Larger bonuses, pension 100/day.
- **Lance Fund Return**: Accumulated deductions returned on honorable discharge.

## Menu Structure

**Enlisted Status** (main hub):
- Visit Quartermaster — Equipment
- My Lance — Roster and relationships
- My Camp — Service records, pay status, camp activities, retinue
- My Lord... — Speak with your commander
- Visit Settlement — Enter towns/castles
- Report for Duty — View/request assignments
- Ask for Leave — Request temporary leave
- Leave Without Penalty — Available when pay is severely late
- Desert the Army — Abandon post (penalties)

**Status Display** shows:
- Rank, Formation, Fatigue
- Wage (with modifiers)
- Pay Status (when late)
- Owed Backpay
- XP Progress

## Installation

1. **Requirements**:
   - Mount & Blade II: Bannerlord v1.3.10+
   - Bannerlord.Harmony v2.3.6+

2. **Setup**:
   - Extract `Enlisted` folder to `<Bannerlord>\Modules\`.
   - Enable `Bannerlord.Harmony` and `Enlisted` in the launcher.
   - **Load Order**: Harmony → Native → ... → Enlisted.

## Configuration

Customize via JSON files in `Modules\Enlisted\ModuleData\Enlisted\`:
- `enlisted_config.json`: Wages, XP, pay muster intervals, discharge rules.
- `duties_system.json`: Duty definitions, training rewards.
- `progression_config.json`: Tier thresholds, culture-specific ranks.
- `settings.json`: Logging levels.

## Troubleshooting

Logs in `<Bannerlord>\Modules\Enlisted\Debugging\`:
- `Session-A/B/C_{timestamp}.log`: Rotating session logs (A = newest).
- `Conflicts-A/B/C_{timestamp}.log`: Mod conflict diagnostics.
- `Current_Session_README.txt`: Pointers to current logs.

## Reporting Issues
- Share log files via Drive/Dropbox/GitHub Gist (Steam doesn't accept uploads).
- Include: Session-A log, Conflicts-A log, crash folder if applicable.
- Note: Bannerlord version, Harmony version, full mod list with load order.
- Describe steps to reproduce.
