# Enlisted – Serve as a Soldier (Developer Guide)

This is the full README for developers. It explains how the mod works internally and how to build and deploy it.

Overview
- A Bannerlord mod that lets the player enlist under a lord and play as a soldier. Targeting .NET Framework 4.7.2.
- Entry point: SubModule.cs. Harmony is used for non-invasive game patches.

Features
- Enlist/discharge via lord dialog (priority 110)
- Join the commander’s army using native APIs
- Player party hidden; camera follows commander
- Auto-join commander battles on the correct side
- Wages while enlisted; promotion tracking groundwork
- Protection from independent hostile encounters

How the code works
- Entry point: SubModule.cs
  - Registers CampaignBehaviors during game start.
- Behaviors (orchestrators)
  - Behaviors/EnlistmentBehavior.cs: Coordinates enlist/leave lifecycle, save/load, visibility/camera changes.
  - Behaviors/WageBehavior.cs: Pays daily wages when enlisted.
  - Behaviors/PromotionBehavior.cs: Tracks rank/progress using Services/PromotionRules.cs.
- Services (business logic)
  - Services/ArmyService.cs: Create/join/leave army; configure escort AI when needed.
  - Services/PartyIllusionService.cs: Hide/show player party; set/restore camera follow.
  - Services/DialogService.cs: Adds dialog lines in lord_talk and hero_main_options and wires conditions/actions.
  - Services/PromotionRules.cs: Encapsulates thresholds and checks for promotions.
- Patches (Harmony)
  - Patches/BattleParticipationPatch.cs: Detects commander entering combat; ensures player joins battle appropriately.
  - Patches/BanditEncounterPatch.cs: Cancels hostile encounters when enlisted.
  - Patches/HidePartyNamePlatePatch.cs, Patches/SuppressArmyMenuPatch.cs, Patches/EngagementPatch.cs: UI/UX polish and safe engagement behavior.
- Models (persistent state)
  - Models/EnlistmentState.cs, Models/PromotionState.cs: Saved via Bannerlord save system to persist enlistment and rank state.
- Utils
  - Utils/Constants.cs: Centralized constants and message keys.
  - Utils/DebugHelper.cs: Logging helpers used in try/catch blocks around patches/services.
  - Utils/ReflectionHelpers.cs: Thin wrappers to keep compatibility with game API variations.

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
- Open Enlisted.sln in Visual Studio 2022.
- Select Debug | x64 and Build > Build Solution.
- The post-build step copies Enlisted.dll and SubModule.xml to:
  C:\Program Files (x86)\Steam\steamapps\common\Mount & Blade II Bannerlord\Modules\Enlisted\bin\Win64_Shipping_Client
- Ensure the TaleWorlds.* and 0Harmony references in Enlisted.csproj match your Bannerlord installation paths.

Configuration
- Settings.cs holds mod tunables (e.g., wages, feature toggles).
- All user-facing strings should use TaleWorlds.Localization.TextObject.

Contributing
- Follow the coding standards in Documentation/BLUEPRINT.md.
- One patched game class per file with names like <GameClass>_<Method>_<Prefix|Postfix|Transpiler>.
- Wrap patch bodies in try/catch and log via DebugHelper to avoid crashing the game.
