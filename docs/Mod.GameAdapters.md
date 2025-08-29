# Mod.GameAdapters

TaleWorlds APIs, Harmony patches, and event bridges for the Enlisted mod.

This directory contains:
- Harmony patches for extending TaleWorlds functionality
- Game event bridges and adapters
- Direct TaleWorlds API integrations

## Structure

- `Patches/` - All Harmony patches (see blueprint section 4.2.1)
- `Events/` - Event bridges and adapters
- `APIs/` - Direct TaleWorlds API integrations

## Harmony Patch Policy

All patches must:
- Be placed in `Patches/` directory
- Include structured header comments (see blueprint 4.2.1)
- Use proper error handling and null checks
- Be configurable where appropriate
- Follow the blueprint's Harmony conventions

## Current Patches (active)

- `MobilePartyTrackerPatches.cs`
  - Redirects tracker to commander while enlisted; blocks tracking `MainParty`
- `HidePartyNamePlatePatch.cs`
  - Hides `MainParty` nameplate/shield while enlisted by patching `PartyNameplateVM.RefreshBinding`