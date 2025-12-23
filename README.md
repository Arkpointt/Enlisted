# enlisted (military service mod)

serve as a soldier in any lord’s warband. follow orders, earn wages, climb ranks, and leave service when you’re done.

### index

- [requirements](#requirements)
- [install](#install)
- [update](#update)
- [quick start (first time)](#quick-start-first-time)
- [features (high level)](#features-high-level)
- [what to expect (important rules)](#what-to-expect-important-rules)
- [settings / config](#settings--config)
- [logs + troubleshooting](#logs--troubleshooting)
- [more docs](#more-docs)

### requirements

- mount & blade ii: bannerlord (supported versions depend on the release you installed)
- `bannerlord.harmony` (required)

### install

1. download the mod release (zip).
2. extract the `Enlisted` folder into:
   - `<bannerlord>\Modules\`
3. in the launcher, enable:
   - `bannerlord.harmony`
   - `enlisted`
4. recommended load order:
   - harmony -> native -> sandboxcore -> sandbox -> storymode -> custombattle -> enlisted

### update

- overwrite your existing `Modules\Enlisted\` folder with the new one.
- if you use other mods that patch menus/encounters/party state, re-check load order after updating.

### quick start (first time)

1. talk to a lord -> choose the enlistment dialog option.
2. after you enlist, use the **enlisted status** menu as your hub:
   - quartermaster, camp, duty selection, leave, etc.
   - to leave service properly: **camp -> request discharge** (resolves at next pay muster: **final muster**)
3. at the t1->t2 proving event, choose your formation (infantry/archer/cavalry/horse archer when available).

### features (high level)

- **enlistment**
  - enlist with a lord and live inside their campaign loop (movement, battles, camp).
  - includes safety handling for battles/encounters/captivity so you don’t get stuck in weird states.
- **bag check / baggage train**
  - early enlistment inventory handling to prevent “walk in with endgame kit” without deleting your stuff.
- **rank + proving events**
  - promotion is not just xp; key ranks use narrative “proving” events (including formation choice at t1->t2).
- **formation training**
  - small daily skill xp baseline while actively enlisted, based on your chosen formation (infantry/archer/cavalry/horse archer).
- **duties system**
  - pick/request military duties for bonuses and pay modifiers (tier/formations gate options).
- **pay system (muster ledger)**
  - wages accrue daily into a ledger and pay out at pay muster events (with iou/promissory options when disrupted).
- **pay tension**
  - when pay is late, tension rises and unlocks special choices (corruption/loyalty pressure valves, including leaving without penalty at high tension).
- **squad identity**
  - you're assigned a culture-flavored squad identity and see it reflected in menus, ranks, and story hooks.
- **camp activities**
  - a menu of action-based activities (training/tasks/social/squad) that spend/restore fatigue and grant xp.
- **quartermaster + equipment**
  - buy formation/tier/culture-appropriate gear; includes provisions (rations) and relationship flavor when enabled.
- **companions**
  - your companions stay with you; you can toggle who fights vs who stays back (safer).
- **commander retinue (high tiers)**
  - at commander tiers, you gain a personal force and manage it alongside your service.
- **town/castle access while enlisted**
  - safe settlement access patterns so you can still use towns/castles while your party is otherwise “embedded”.
- **story content (events + decisions)**
  - data-driven content packs: automatic events + player-initiated decisions, all driven from json.
- **optional systems**
  - camp life snapshot, escalation tracks, and player conditions can be toggled in config.

### what to expect (important rules)

- **you serve; you don't command**
  - you manage your own small scope (your duty, your squad menus, camp actions), not the lord's strategy.
- **loot**
  - low tiers do not get normal loot screens; you’ll get compensation instead.
  - higher tiers regain native loot access.
- **pay**
  - wages accrue into a muster ledger and pay out at pay muster events.
  - when pay is late, “pay tension” rises and unlocks pressure-valve options (including leaving without penalty at high tension).
- **leave vs discharge**
  - leave is temporary and time-limited.
  - discharge (final muster) is the "leave properly" path and resolves at the next pay muster.
- **soldier reputation**
  - your standing with your fellow soldiers affects available options and promotions.
  - help your comrades → better event options, they cover for you, promotion eligibility.
  - betray or ignore them → worse options, isolation, blocked promotions, possible sabotage.

### settings / config

configs live in:

- `Modules\Enlisted\ModuleData\Enlisted\`

common files:
- `enlisted_config.json` (main toggles and pacing)
- `progression_config.json` (tiers/ranks)
- `Events\*.json` (event content packs)
- `Orders\*.json` (military orders by tier)

### translations

the mod is fully translatable and supports multiple languages. to add a translation:

- see **[Translation Guide](ModuleData/Languages/README.md)** for complete instructions
- copy the `_TEMPLATE` folder in `ModuleData/Languages/`
- translate 1,822 strings to your language
- submit via pull request or share with the community

current languages: **english** (default)

### logs + troubleshooting

#### where logs are

**PRIMARY LOCATION (ENLISTED MOD LOGS):**

`<bannerlord>\Modules\Enlisted\Debugging\`

**Example full path:**

`C:\Program Files (x86)\Steam\steamapps\common\Mount & Blade II Bannerlord\Modules\Enlisted\Debugging\`

**IMPORTANT:** The mod writes all logs directly to the `Debugging` subfolder inside the Enlisted module directory. This is NOT the game's crash logs and NOT your Documents folder.

**files you should share when reporting issues:**
- `Session-A_{timestamp}.log` (latest session)
- `Conflicts-A_{timestamp}.log` (latest conflicts report)
- `Current_Session_README.txt` (points to the current files and explains what to share)

**note:** The mod no longer creates `enlisted.log` or `conflicts.log` (legacy filenames). All logs use timestamped Session-A/B/C and Conflicts-A/B/C rotation.

**fallback location (only if the mod can't write to the module folder):**
- `%userprofile%\Documents\Mount and Blade II Bannerlord\Logs\Enlisted\`

**bannerlord crash dumps (separate - when the game itself hard-crashes):**
- `C:\ProgramData\Mount and Blade II Bannerlord\crashes\`

#### error codes (important)

when something breaks, enlisted will often log a **stable error code** like:
- `[E-CAMPUI-031] Failed to display Camp Area screen`

these codes are meant to be searchable and consistent across versions.
when reporting an issue, please include:
- the **error code(s)** you see (copy/paste the lines)
- `Session-A_*.log`

note: exceptions include **full stack traces**, but they’re **de-duplicated** (the first occurrence per unique exception in a session) to avoid log spam.

also note: some issues are intentionally logged **once per session** (look for codes beginning with `E-` or `W-` in a single line), especially for:
- **dlc missing / feature gating** (example: `W-DLC-001`, `W-DLC-002`)
- **reflection / api drift** where a patch can’t apply (example: `W-REFLECT-001`)
- **ui fallback paths** (example: `E-QM-014`)
- **save/load wrappers + migrations** (example: `E-SAVELOAD-001`, `E-SAVELOAD-002`)

#### common issues

- **mod menu options don’t show**
  - confirm `bannerlord.harmony` is enabled and loaded before enlisted.
- **war sails / naval features don’t work**
  - if you don’t have the War Sails (NavalDLC) expansion enabled, naval-only features will be gated.
  - check the session log for a `W-DLC-*` warning.
- **events don’t fire**
  - confirm the event systems are enabled in `enlisted_config.json`.
- **something feels “stuck” after leaving a menu/encounter**
  - send `Session-A_*.log` + your mod list/load order.
- **save won’t load / loads into broken state**
  - check for `E-SAVELOAD-*` in `Session-A_*.log`, then share the session log + conflicts log (and the save file if possible).

### more docs

full documentation entry point:
- `docs/index.md`

