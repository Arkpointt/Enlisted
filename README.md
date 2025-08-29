# Enlisted – Serve as a Soldier

A Bannerlord mod that lets the player enlist under a lord and play as a soldier. Targeting .NET Framework 4.7.2. Harmony is used for non-invasive game patches.

Features (current)
- Enlist/discharge via lord dialog (high-priority option)
- Commander follow: camera follows the commander; player party escorts the commander
- Main party visuals hidden while enlisted: nameplate/shield suppressed; UI tracker redirected away from MainParty
- Soldier status and wait menus: minimal menus to reflect enlisted status and settlement waiting
- Structured logging written to a debugging folder for troubleshooting

How the code works
- Entry point: `src/Mod.Entry/SubModule.cs`
  - Initializes logging and applies Harmony patches, then registers behaviors during game start.
- Behavior
  - `src/Features/Enlistment/Application/EnlistmentBehavior.cs`: Orchestrates enlist/leave lifecycle, tracking/following the commander, minimal menus, and visual/tracker suppression for MainParty while enlisted.
- Patches (Harmony)
  - `src/Mod.GameAdapters/Patches/MobilePartyTrackerPatches.cs`: Redirects the map tracker to the commander and blocks tracking the MainParty while enlisted.
  - `src/Mod.GameAdapters/Patches/HidePartyNamePlatePatch.cs`: Hides the MainParty nameplate/shield while enlisted by patching `PartyNameplateVM.RefreshBinding`.
- Models (persistent state)
  - `src/Features/Enlistment/Domain/EnlistmentState.cs`: Persists enlistment state via the Bannerlord save system.
- Core (logging)
  - `src/Mod.Core/Logging/LoggingService.cs`: Centralized structured logging to a debugging folder.

Core flows
1) Enlist
- DialogService exposes enlist option.
- EnlistmentBehavior calls ArmyService to create/join army and set escort AI.
- PartyIllusionService hides player party and sets camera to follow commander.
- WageBehavior starts payments; EnlistmentState is saved.

2) Battle participation
- BattleParticipationPatch tracks commander battle entry and attaches the player to the same battle/side.
- BanditEncounterPatch prevents independent hostile encounters while enlisted.

3) Discharge
- EnlistmentBehavior/ArmyService leave army and clear AI/escort.
- PartyIllusionService restores visibility and camera; wages stop; state is cleared.

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
