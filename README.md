# Enlisted – Military Service Mod

Serve as a soldier in any lord's warband. Follow orders, earn wages, climb ranks, and retire with honor.

## What's New

**Latest Updates:**
- ✨ **Camp Life Simulation**: Dynamic camp conditions affect Quartermaster mood, pricing, and availability
- 👥 **Companion Management**: Full control from T1 with battle participation toggle
- ⚔️ **Commander's Retinue**: T7-T9 grants personal force (15/25/35 soldiers) with recruit development
- 😴 **Fatigue System**: Stamina-based action gating for camp activities and choices
- 🎯 **Lance System**: Culture-specific military units with provisional and final assignment
- 🏋️ **Formation Training**: Automatic daily skill XP based on your military formation
- 💰 **Enhanced Pay System**: Promissory notes, free desertion, and pay tension consequences
- 🎖️ **Cultural Ranks**: 9 tiers with authentic rank names for all 7 cultures
- 📜 **Story Packs**: Data-driven lance life events with modular content system
- 🚪 **Temporary Leave**: Time-limited leave system with transfer support
- 🌍 **Full Localization**: Complete translation support for all languages

## Quick Start

1. **Enlist**: Talk to any lord → "I wish to serve in your warband"
2. **Choose Formation**: At T1→T2 promotion, pick Infantry/Archer/Cavalry/Horse Archer
3. **Join a Lance**: Assigned provisional lance at T1, finalize at T2
4. **Earn & Progress**: Battle, train daily, climb ranks (T1→T9)
5. **Manage Resources**: Fatigue, wages, equipment, companions
6. **Navigate Challenges**: Pay tension, camp conditions, lance life events
7. **Retire or Transfer**: Request discharge, take leave, or switch lords

## Features

### 🛡️ Enlistment
- Talk to any lord: "I wish to serve in your warband."
- You join their party as a regular soldier.
- **Army Leader Restriction**: Cannot enlist while leading your own army (must disband first).
- **No Personal Loot (T1-T3)**: Low-rank soldiers don't loot. You receive gold share compensation instead.
- **Veteran Loot (T4+)**: Veterans and above earn loot privileges — native loot screens work.
- **No Starvation**: Your lord provides food (starvation cohesion penalties are compensated).
- **Temporary Leave**: Request time-limited leave (14 days) to handle personal business. Return before expiration or face desertion penalties.

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
- **Promissory Notes (IOU)**: When camp conditions disrupt payroll (losses, sieges, logistics strain), accept an IOU to defer payment. Your backpay is preserved and you'll get another chance to collect in 3 days instead of waiting the full ~12 day cycle.

### 😤 Pay Tension System
When pay is late, tension builds (0-100 scale):

| Tension | Effects |
|---------|---------|
| 0-19 | Normal operations |
| 20-39 | Grumbling (-3 morale) |
| 40-59 | Growing unrest (-6 morale, +5% discipline incidents) |
| 60-79 | Severe (-10 morale, **free desertion available**) |
| 80-100 | Crisis (-15 morale, mutiny risk, NPC desertions) |

**Options During Pay Disruption:**
- **Accept IOU**: Take a promissory note, preserve backpay, retry in 3 days (no penalties).
- **Desperate Measures** (Corrupt Path, 40+ tension):
  - Bribe Paymaster's Clerk (50g risk/reward)
  - Skim Supplies (Quartermaster/Armorer duty only)
  - Find Black Market (50+ tension)
  - Sell Issued Equipment (60+ tension)
  - Listen to Desertion Talk (70+ tension)
- **Help the Lord** (Loyal Path, 40+ tension):
  - Collect Debts (-10 tension, Charm skill check)
  - Escort Merchant (-15 tension, costs fatigue)
  - Negotiate Loan (-20 tension, requires Trade 50+)
  - Volunteer for Raid (-25 tension, combat risk, requires war)
- **Free Desertion**: Leave at 60+ tension with minimal penalties (-5 relation only).

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
  - Each culture has unique rank titles (Vlandia, Sturgia, Khuzait, Battania, Aserai, Mercenary).

- **Proving Events**: Promotions through narrative events:
  - T1→T2 "Finding Your Place": Choose your formation (Infantry/Archer/Cavalry/Horse Archer*)
  - T2→T3 "The Sergeant's Test": Prove your judgment
  - T3→T4 "Crisis of Command": Show leadership under fire
  - T4→T5 "The Lance Vote": Earn peer trust
  - T5→T6 "Audience with the Lord": Declare loyalty
  - *Horse Archer only available for Khuzait and Aserai cultures

- **Promotion Requirements**: XP, days served, events completed, battles survived, reputation.

- **Formation Training**: Automatic daily skill XP based on your formation choice:
  - **Infantry**: Athletics, One-Handed, Two-Handed, Polearm, Throwing
  - **Archer**: Bow, Crossbow, Athletics, One-Handed
  - **Cavalry**: Riding, One-Handed, Polearm, Athletics, Two-Handed
  - **Horse Archer**: Riding, Bow, Throwing, Athletics, One-Handed

### 🎯 Lance System
- **Lance Assignment**: Join a culture-specific military unit at enlistment.
  - **Provisional Lance** (T1): Temporary assignment based on lord's culture
  - **Final Lance** (T2+): Choose from 3-5 culture-appropriate lances during promotion
  - **Lance Identity**: Each lance has unique name, style, and role (Infantry/Ranged/Cavalry/Horse Archer)
  - **Cultural Styles**: Legion (Empire), Feudal (Vlandia), Tribal (Sturgia/Battania), Horde (Khuzait), Mercenary (Aserai/Universal)

- **Lance Roster**: View your position within the lance hierarchy based on rank.

### 📋 Duties System
- **Formation-Based Assignments**: Formation determines available duties.
  - **Infantry**: Runner, Quartermaster, Field Medic, Armorer, Engineer
  - **Archer**: Scout, Lookout
  - **Cavalry/Horse Archer**: Scout, Messenger

- **Duty Request System** (T2+): Request transfers with lance leader approval.

### 👥 Companion Management & Commander's Retinue
- **Companions** (T1+): Managed from day one of enlistment
  - Always in your personal party, never transferred to lord
  - **Battle Participation Toggle**: Choose which companions fight vs. stay safe
  - **Fight**: Companion spawns in battle, faces all risks, gains XP
  - **Stay Back**: Companion stays in roster, immune to battle casualties, no XP
  
- **Commander's Retinue** (T7-T9): Personal military force granted at commander rank
  - **T7 (Commander I)**: 15 raw recruits automatically granted on promotion
  - **T8 (Commander II)**: +10 more recruits (25 total)
  - **T9 (Commander III)**: +10 more recruits (35 total)
  - Recruits match your formation type (Infantry/Archer/Cavalry/Horse Archer)
  - Raw recruits develop through combat: Recruit → Regular → Veteran
  - Automatic reinforcements via trickle system (1 recruit every 2-3 days)
  
- **Unified Squad Command** (T1-T9):
  - Player + companions (+ retinue at T7+) fight together in same formation
  - Player commands own squad, not entire army
  - Formation based on player's duty assignment
  - All squad members spawn together and fight as unified force

### 😴 Fatigue System
- **Fatigue Counter**: Stamina system (24/24) for gating actions and choices
- **Usage**: Pay muster options, camp activities, lance life events
- **Recovery**: Rest and specific camp actions restore fatigue
- **Probation**: Caps maximum fatigue while probation is active

### 🏕️ Camp Life Simulation
- **Daily Camp Conditions**: Internal simulation tracking army conditions
  - **Logistics Strain**: Supply shortages, distance from settlements
  - **Morale Shock**: Battle trauma and low spirits
  - **Territory Pressure**: Losing ground, hostile territory
  - **Contraband Heat**: Crackdown risk from illicit activities
- **Dynamic Quartermaster**: Mood, pricing, and availability vary with camp conditions
- **Promissory Notes**: IOUs when payroll is disrupted by losses or sieges

### ⚔️ Lance Life Events
Random events that shape your military career:
- **Camp Events**: Social interactions, training opportunities, supply issues
- **Post-Battle Events**: Victory celebrations, looting the dead, theft invitations
- **Pay Tension Events**: Grumbling, confrontations, desertion plots, mutiny brewing
- **Loyal Path Missions**: Collect debts, escort merchants, negotiate loans, raid enemies
- **Story Packs**: Data-driven content system for modular story expansion
- **Consequences**: Your choices affect Heat, discipline, reputation, and lord relations

### 🏥 Player Conditions (Optional)
When enabled, adds risk and consequences to military life:
- **Injuries**: Sprains, bruises, cuts, fractures from combat and events
  - Minor/Moderate/Severe severity levels
  - Affects multiple body locations (arm, leg, torso, head)
  - Recovery time varies by severity
- **Illnesses**: Camp fever, flux, exhaustion from camp conditions
  - Natural recovery over time
  - Can affect performance and event availability
- **Event Integration**: Risky event choices can cause injuries or illness
- **Toggle**: Enable/disable via `player_conditions.enabled` in config

### 📦 Equipment & Quartermaster
- **Quartermaster Hero**: Persistent NPC with unique personality
  - **Three Archetypes**: Veteran (pragmatic), Scoundrel (opportunistic), Believer (pious)
  - **Relationship System**: Build trust (0-100) for discounts and special options
  - **Discounts**: 5% at Trusted (40+), 10% at Respected (60+), 15% at Battle Brother (80+)
  - **PayTension-Aware Dialog**: Unique advice based on archetype during financial crisis
  - **Mood System**: Camp conditions affect Quartermaster demeanor and pricing
- **Equipment Shop**: Purchase gear based on formation, tier, and culture
  - Stockouts possible during high logistics strain
  - Buyback system for selling unwanted gear
- **Provisions System**: Purchase rations for morale bonuses
  - **Basic Rations** (25g): +2 morale, 3 days
  - **Good Fare** (50g): +4 morale, -1 fatigue/day, 3 days
  - **Officer's Table** (100g): +6 morale, -2 fatigue/day, 3 days
- **Retinue Provisioning** (T7+): Feed your personal soldiers
  - Tier options from Bare Minimum to Officer Quality
  - Affects retinue morale and loyalty
- **NEW Item Indicators**: Newly unlocked items show `[NEW]`
- **Tier-Gated Loot**: T4+ veterans access native loot screens
- **Formation-Specific Kits**: Equipment tailored to your military role

## Cultural Rank Progression

Each culture has authentic military ranks reflecting their traditions:

**Empire (Legion/Discipline)**: Tiro → Miles → Immunes → Principalis → Evocatus → Centurion → Primus Pilus → Tribune → Legate

**Vlandia (Feudal/Chivalry)**: Peasant → Levy → Footman → Man-at-Arms → Sergeant → Knight Bachelor → Cavalier → Banneret → Castellan

**Sturgia (Tribal/Shield Wall)**: Thrall → Ceorl → Fyrdman → Drengr → Huskarl → Varangian → Champion → Thane → High Warlord

**Khuzait (Steppe/Horde)**: Outsider → Nomad → Noker → Warrior → Veteran → Bahadur → Arban → Zuun → Noyan

**Battania (Celtic/Guerrilla)**: Woodrunner → Clan Warrior → Skirmisher → Raider → Oathsworn → Fian → Highland Champion → Clan Chief → High King's Guard

**Aserai (Desert/Mercantile)**: Tribesman → Skirmisher → Footman → Veteran → Guard → Faris → Emir's Chosen → Sheikh → Grand Vizier

**Mercenary (Universal)**: Follower → Recruit → Free Sword → Veteran → Blade → Chosen → Captain → Commander → Marshal

### ⚓ Naval Support
- Compatible with naval mods
- Party follows lord onto ships seamlessly
- Ship protection while enlisted

### 🚪 Discharge & Retirement
- **Pending Discharge**: Request in "My Camp" menu; resolves at next pay muster
- **Discharge Bands**: Service length determines rewards:
  - **Washout** (<100 days): Penalties, no pension
  - **Honorable** (100-199): Bonuses, pension 50/day
  - **Veteran/Heroic** (200+): Larger bonuses, pension 100/day
- **Lance Fund Return**: Accumulated 5% deductions returned on honorable discharge
- **Free Desertion**: Available at pay tension ≥ 60 with minimal penalties (-5 relation only)

### 🔄 Transfer & Grace Period
- **Transfer Service**: While on leave, transfer to another lord in same faction
  - Preserves tier, XP, service date, and kill tracking
  - Maintains service record continuity
- **Grace Period**: When lord dies, time to find new service without discharge penalties
- **Re-enlistment**: Can re-enlist with same or different lord after discharge

## Menu Structure

**Enlisted Status** (main hub):
- Visit Quartermaster — Equipment and supplies
- My Lance — Roster, relationships, and lance identity
- My Camp — Service records, pay status, camp activities, companion assignments
- My Lord... — Speak with your commander
- Visit Settlement — Enter towns/castles
- Report for Duty — View/request assignments
- Ask for Leave — Request temporary leave (14 days maximum)
- Leave Without Penalty — Available when pay is severely late (tension ≥ 60)
- Desert the Army — Abandon post (penalties)

**Status Display** shows:
- Rank (culture-specific title), Formation, Lance
- Fatigue (Current/Max)
- Camp Status (conditions snapshot)
- Days from Town
- Wage (with modifiers)
- Pay Status (when late)
- Owed Backpay
- XP Progress

**My Camp Submenu**:
- Service Records — View your military career stats
- Request Discharge — Initiate pending discharge (resolves at next pay muster)
- Companion Assignments — Toggle companion battle participation (T4+)
- Camp Activities — Rest, socialize, and other fatigue-based actions

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

**Core Configuration:**
- `enlisted_config.json`: Main settings including:
  - Wages, XP, pay muster intervals
  - Discharge rules and pension rates
  - Feature toggles (`camp_life.enabled`, `lance_life.enabled`, `lance_personas.enabled`)
  - Fatigue settings and leave duration
- `progression_config.json`: Tier thresholds and culture-specific rank names
- `lances_config.json`: Lance definitions, cultural styles, and assignments
- `duties_system.json`: Duty definitions and formation training XP
- `equipment_kits.json`: Quartermaster equipment by tier, culture, and formation
- `equipment_pricing.json`: Equipment pricing and buyback rates

**Event Content:**
- `Events/`: Lance life event definitions (JSON-based story system)
- `StoryPacks/`: Modular story content packs
- `LancePersonas/`: Name pools for lance character flavor (optional)

**Localization:**
- `Languages/enlisted_strings.xml`: All player-facing text (supports translation)

## Advanced Features

### Data-Driven Content System
- **JSON-Based Events**: All lance life events defined in JSON for easy modding
- **Story Packs**: Modular content system for adding/removing story collections
- **Localization Ready**: Full translation support via XML language files
- **No Code Required**: Add new events, lances, equipment without recompiling

### Optional Systems (Toggle in Config)
- **Camp Life Simulation**: `camp_life.enabled` — Dynamic camp conditions and consequences
- **Lance Life Events**: `lance_life.enabled` — Random military life events
- **Lance Personas**: `lance_personas.enabled` — Named lance member flavor text
- **Player Conditions**: `player_conditions.enabled` — Injuries and illnesses system

### Mod-Friendly Architecture
- Event-driven design minimizes conflicts
- No global economy or AI patches
- Compatible with naval mods, overhaul mods, and total conversions
- Clean separation of data and logic

## Troubleshooting

**Log Files** in `<Bannerlord>\Modules\Enlisted\Debugging\`:
- `Session-A/B/C_{timestamp}.log`: Rotating session logs (A = newest)
- `Conflicts-A/B/C_{timestamp}.log`: Mod conflict diagnostics
- `Current_Session_README.txt`: Pointers to current logs

**Common Issues:**
- **No XP from training**: Check `duties_system.json` exists in ModuleData folder
- **Companions missing**: Check tier (T1-T3 = lord's party, T4+ = your party)
- **Quartermaster prices high**: Check camp conditions (logistics strain affects pricing)
- **Events not firing**: Verify event system enabled in `enlisted_config.json`

## Reporting Issues
- Share log files via Drive/Dropbox/GitHub Gist (Steam doesn't accept uploads)
- Include: Session-A log, Conflicts-A log, crash folder if applicable
- Note: Bannerlord version, Harmony version, full mod list with load order
- Describe steps to reproduce
- Mention: Current tier, days enlisted, lord faction

## Modding & Extensibility

**Adding Custom Content:**
- **New Lance Life Events**: Create JSON files in `ModuleData/Enlisted/Events/`
- **New Story Packs**: Add packs to `ModuleData/Enlisted/StoryPacks/LanceLife/`
- **New Lances**: Edit `lances_config.json` to add culture-specific units
- **Custom Equipment**: Modify `equipment_kits.json` for formation-specific gear
- **Translations**: Add language files in `ModuleData/Languages/`

**Documentation for Modders:**
- `docs/Features/` — Feature specifications and technical details
- `docs/research/` — System design and implementation notes
- `docs/modderreadme.md` — Modding guide and API reference

## Contributing

See `CONTRIBUTING.md` for guidelines on:
- Reporting bugs and requesting features
- Submitting code contributions
- Content creation standards
- Testing procedures

## License

See `LICENSE` file for details.

---

**Enjoy your military service! May your blade stay sharp and your pay arrive on time.**
