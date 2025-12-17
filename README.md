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
   - harmony → native → sandboxcore → sandbox → storymode → custombattle → enlisted

### update

- overwrite your existing `Modules\Enlisted\` folder with the new one.
- if you use other mods that patch menus/encounters/party state, re-check load order after updating.

### quick start (first time)

1. talk to a lord → choose the enlistment dialog option.
2. after you enlist, use the **enlisted status** menu as your hub:
   - quartermaster, camp, duty selection, leave, etc.
3. at the t1→t2 proving event, choose your formation (infantry/archer/cavalry/horse archer when available).

### features (high level)

- **enlistment**
  - enlist with a lord and live inside their campaign loop (movement, battles, camp).
  - includes safety handling for battles/encounters/captivity so you don’t get stuck in weird states.
- **bag check / baggage train**
  - early enlistment inventory handling to prevent “walk in with endgame kit” without deleting your stuff.
- **rank + proving events**
  - promotion is not just xp; key ranks use narrative “proving” events (including formation choice at t1→t2).
- **formation training**
  - automatic daily skill xp based on your chosen formation (infantry/archer/cavalry/horse archer).
- **duties system**
  - pick/request military duties for bonuses and pay modifiers (tier/formations gate options).
- **pay system (muster ledger)**
  - wages accrue daily into a ledger and pay out at pay muster events (with iou/promissory options when disrupted).
- **pay tension**
  - when pay is late, tension rises and unlocks special choices (corruption/loyalty pressure valves, including leaving without penalty at high tension).
- **lance identity**
  - you’re assigned a culture-flavored lance identity and see it reflected in menus, ranks, and story hooks.
- **camp activities**
  - a menu of action-based activities (training/tasks/social/lance) that spend/restore fatigue and grant xp.
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

- **you serve; you don’t command**
  - you manage your own small scope (your duty, your lance menus, camp actions), not the lord’s strategy.
- **loot**
  - low tiers do not get normal loot screens; you’ll get compensation instead.
  - higher tiers regain native loot access.
- **pay**
  - wages accrue into a muster ledger and pay out at pay muster events.
  - when pay is late, “pay tension” rises and unlocks pressure-valve options (including leaving without penalty at high tension).
- **leave vs discharge**
  - leave is temporary and time-limited.
  - discharge (final muster) is the “leave properly” path and resolves at the next pay muster.

### settings / config

configs live in:

- `Modules\Enlisted\ModuleData\Enlisted\`

common files:
- `enlisted_config.json` (main toggles and pacing)
- `progression_config.json` (tiers/ranks)
- `duties_system.json` (duties + formation training config)
- `Events\*.json` (event content packs)

### logs + troubleshooting

#### where logs are

primary location (next to the mod):
- `<bannerlord>\Modules\Enlisted\Debugging\`

you should usually share:
- `Session-A_*.log` (latest session)
- `Conflicts-A_*.log` (latest conflicts report)
- `Current_Session_README.txt` (points to the current files)

fallback location (if the game can’t write into the module folder):
- `%userprofile%\Documents\Mount and Blade II Bannerlord\Logs\Enlisted\`

bannerlord crash dumps (when the game hard-crashes):
- `C:\ProgramData\Mount and Blade II Bannerlord\crashes\`

#### common issues

- **mod menu options don’t show**
  - confirm `bannerlord.harmony` is enabled and loaded before enlisted.
- **events don’t fire**
  - confirm the event systems are enabled in `enlisted_config.json`.
- **something feels “stuck” after leaving a menu/encounter**
  - send `Session-A_*.log` + your mod list/load order.

### more docs

full documentation entry point:
- `docs/index.md`

