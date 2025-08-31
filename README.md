# Enlisted – Serve as a Soldier

A Bannerlord mod that lets the player enlist under a lord and play as a soldier. Targeting .NET Framework 4.7.2. Harmony is used for non-invasive game patches.

Features (current)
- Enlist/discharge via lord dialog (high-priority option)
- Commander follow: camera follows the commander; player party escorts the commander; when commander is in an army, player party merges for blob treatment
- Main party visuals hidden while enlisted: nameplate/shield suppressed; UI tracker redirects to commander; periodic enforcement
- Soldier status and report menus: persistent enlisted panel keeps time controls responsive (Finish encounter → drain menus → StartWait + StoppablePlay)
- Deferred safety: post-load/post-battle restore is deferred until menus/encounter clear; camera/escort re-applied safely
- Conditional AI ignore: refresh IgnoreByOtherPartiesTill while not in an army to prevent stray targeting of hidden party
- Structured logging written to a debugging folder, with clear markers for deferred/applied operations

How the code works
- Entry point: `src/Mod.Entry/SubModule.cs`
  - Initializes logging and applies Harmony patches, then registers behaviors during game start.
- Behavior
  - `src/Features/Enlistment/Application/EnlistmentBehavior.cs`: Orchestrates enlist/leave lifecycle, commander follow/army merge, encounter auto-join, enlisted menus/time control, deferred restore, and main party visual/tracker suppression.
- Patches (Harmony)
  - `src/Mod.GameAdapters/Patches/MobilePartyTrackerPatches.cs`: Redirects the map tracker to the commander and blocks tracking the MainParty while enlisted.
  - `src/Mod.GameAdapters/Patches/HidePartyNamePlatePatch.cs`: Hides the MainParty nameplate/shield while enlisted by patching `PartyNameplateVM.RefreshBinding`.
- Models (persistent state)
  - `src/Features/Enlistment/Domain/EnlistmentState.cs`: Persists enlistment state via the Bannerlord save system.
- Core (logging)
  - `src/Mod.Core/Logging/LoggingService.cs`: Centralized structured logging to a debugging folder.

Core flows
1) Enlist
- Finish any active encounter, drain menus, enable StoppablePlay.
- Persist enlistment, hide/deactivate main party, set escort to commander, set camera follow, register commander in tracker.

2) Battle participation
- Auto-join commander battles: activate briefly and nudge near commander for inclusion; avoid nudging for non-commander friendlies.
- While in towns/castles, do not attempt joins; handle after exit.

3) Post-battle and load
- Defer restore until safe tick; re-hide/reactivate escort/camera; optionally re-open enlisted status menu.

4) Discharge
- Leave army, restore visibility/activation and camera to player, clear escorts/trackers.

Build
- Open `Enlisted.sln` in Visual Studio 2022, select configuration "Enlisted EDITOR" and Build.
- Or use CLI:
  - `dotnet build Enlisted.sln -c "Enlisted EDITOR"`
- Output is configured to copy directly into the game module folder:
  `C:\Program Files (x86)\Steam\steamapps\common\Mount & Blade II Bannerlord\Modules\Enlisted\bin\Win64_Shipping_wEditor\Enlisted.dll`
- Ensure the TaleWorlds.* and 0Harmony references in `Enlisted.csproj` match your Bannerlord installation paths.

Configuration
- No user settings are required currently. All user-facing strings use `TaleWorlds.Localization.TextObject`.

Contributing
- Follow the engineering standards in `docs/BLUEPRINT.md`.
- One patched game class per file under `src/Mod.GameAdapters/Patches`.
- Guard all patches, keep them minimal, and log via `LoggingService`.
