# Enlisted Mod – Developer Guide

This document gives modders a concise view of what the Enlisted project currently provides, how it is structured, and how to extend it safely.

## 1. Feature Overview

| Area | Current Behavior |
| --- | --- |
| Enlistment | Player joins any lord via dialog, attaches to the lord's party, stays hidden on the map, and follows army orders. Financial isolation ensures only enlistment wages appear in clan finances. |
| Veteran Retirement | After 252 days (first term), players can retire with gold bonuses, relation gains, and faction reputation. Re-enlistment options with preserved tier and kill tracking per faction. |
| Duties & Ranks | JSON-driven assignments with wage and XP pacing, tier gating (T1-T6), and officer slots that hook into native party roles. |
| Equipment | Master-at-Arms and Quartermaster menus switch the player to real troop loadouts; equipment is replaced (not duplicated) and restored at discharge. |
| Battle Integration | Real-time monitoring joins the player to battles seamlessly when the lord engages. +25 XP per battle, +1 XP per kill. No loot (spoils go to lord), no starvation (lord provides food). |
| Safety | 14-day grace periods for lord death/capture/army defeat, auto-cleaned encounter states, one-day post-release ignore window, and logging under `Modules/Enlisted/Debugging`. |
| Service Transfer | During leave or grace, players can transfer service to another lord in the same faction while preserving all progression. |

## 2. Architecture Snapshot

```
src/
├─ Mod.Entry/            # SubModule + Harmony bootstrap
├─ Mod.Core/             # Logging, settings, helpers
├─ Mod.GameAdapters/     # Harmony patches (visibility, expenses, voting, etc.)
└─ Features/
   ├─ Enlistment/        # Service state, battle participation, grace logic, veteran retirement
   ├─ Assignments/       # Duties system + officer hooks
   ├─ Equipment/         # Troop selection + gear replacement
   ├─ Combat/            # Battle participation, kill tracking, formation assignment
   ├─ Interface/         # Status menus, enlisted menu system
   └─ Conversations/     # Dialog flow for enlist / leave / retirement / transfer
```

Harmony patches live in `src/Mod.GameAdapters/Patches/`. We currently ship seventeen targeted patches covering battle commands, discharge penalties, encounter suppression, expense isolation, loot blocking, starvation suppression, waiting state override, kingdom decisions, influence messages, visibility/nameplate control, town leave button hiding, order of battle suppression, and officer duty integration. Each patch has a narrow purpose and fails open on errors.

## 3. Development Guidelines

- Build with `dotnet build -c "Enlisted EDITOR"` (or the same configuration in Visual Studio). DLLs copy directly into `<BannerlordInstall>/Modules/Enlisted/`.
- Keep comments short and factual—state the intent and any constraints; avoid marketing language.
- Favor public APIs; only add Harmony when the engine exposes no safe hook.
- When touching encounter/battle transitions, defer heavy work via `NextFrameDispatcher.RunNextFrame(...)` to avoid zero-delta assertions.
- Logging defaults to silent success. Use `ModConfig.Settings.Logging.Debug` when deep tracing is required.

## 4. System Notes

### EnlistmentBehavior
- Tracks `_enlistedLord`, enlistment tier/XP, kill counts, and grace periods.
- Uses real-time ticks for lord following and daily ticks for wages/XP.
- Veteran retirement system: 252-day first term, 84-day renewals, per-faction tracking.
- Battle XP: +25 per battle, +1 per kill (tracked via EnlistedKillTrackerBehavior).
- After surrender, vanilla capture flows naturally, then a 14-day grace period starts with a one-day ignore window for safe NPC interaction.

### Duties & Equipment
- Duties are defined via JSON under `ModuleData/Enlisted/` with tier gating and officer slots.
- Officer duties call native `SetPartySurgeon/Engineer/Scout/Quartermaster` to avoid fragile reflection.
- Troop selection uses actual culture loadouts; equipment is replaced on promotion and restored on discharge.

### Interface & Settlement Access
- Menus live in `EnlistedMenuBehavior` and Gauntlet prefabs under `GUI/Prefabs/`.
- Town access uses a synthetic outside encounter so invisible parties can enter settlements without assertions.
- Custom menus appear automatically when enlisted; status menu shows after battles.

## 5. Logging & Diagnostics

All logs are written to `<BannerlordInstall>/Modules/Enlisted/Debugging/`:

- `enlisted.log` – core behavior and patch output (primary file).
- `discovery.log`, `dialog.log`, `api.log` – optional tracing when discovery flags are enabled.

Each session prints a GUID for correlation. Enable debug logging via `ModConfig.Settings.Logging.Debug = true` when troubleshooting.

## 6. Documentation Map

| Document | Purpose |
| --- | --- |
| `docs/BLUEPRINT.md` | Architecture standards, Harmony policy, build notes. |
| `docs/Features/*.md` | Specs for enlistment, encounter safety, duties, menu interface, town access, etc. |
| `docs/ModuleData/Enlisted/README.md` | Configuration schema and examples. |
| `docs/discovered/*` | API references verified against the official v1.3.6 documentation. |

Read the relevant feature spec before modifying behavior; update the spec when the implementation changes.

## 7. Contribution Checklist

1. Update or add the feature spec that matches your change.
2. Keep comments professional and focused on intent.
3. Run `dotnet build -c "Enlisted EDITOR"` (close the Bannerlord launcher if the DLL is locked).
4. Verify the debugging folder logs the new behavior.
5. Document any risky change in `docs/BLUEPRINT.md` or the relevant feature page.

That is the current state of Enlisted: a focused enlistment experience with clear docs and minimal logging so other modders can understand and extend it quickly.
