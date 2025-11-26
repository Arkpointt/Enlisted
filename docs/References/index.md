# Decompiled References Index

This folder contains documentation for TaleWorlds assemblies in `C:\Dev\Enlisted\DECOMPILE`. Each document summarizes the assembly's relevance to the Enlisted mod and highlights important classes and patterns.

---

## Quick Reference by Relevance

### CRITICAL - Must Understand
| Reference | Purpose |
|-----------|---------|
| [TaleWorlds.CampaignSystem](TaleWorlds.CampaignSystem.md) | Campaign layer: heroes, parties, encounters, menus, events |
| [0Harmony](0Harmony.md) | Patching library for modifying game behavior |

### HIGH - Frequently Used
| Reference | Purpose |
|-----------|---------|
| [TaleWorlds.MountAndBlade](TaleWorlds.MountAndBlade.md) | Battle/mission mechanics, agents, formations |
| [SandBox](SandBox.md) | Campaign behavior implementations, mission setup |
| [SandBox.View](Sandbox.View.md) | Party visuals, map rendering (PartyVisualManager) |
| [SandBox.GauntletUI](SandBox.GauntletUI.md) | Gauntlet UI framework - our equipment screens |

### MEDIUM - Occasionally Useful
| Reference | Purpose |
|-----------|---------|
| [TaleWorlds.Core](TaleWorlds.Core.md) | Items, equipment, skills, cultures |
| [SandBox.ViewModelCollection](Sandbox.ViewModelCollection.md) | UI ViewModels (nameplate patches) |
| [TaleWorlds.SaveSystem](TaleWorlds.SaveSystem.md) | Save/load system |
| [TaleWorlds.ObjectSystem](TaleWorlds.ObjectSystem.md) | Object lookup by ID |
| [Bannerlord.Harmony](Bannerlord.Harmony.md) | Harmony integration layer |

### LOW - Rarely Needed
| Reference | Purpose |
|-----------|---------|
| [TaleWorlds.Engine](TaleWorlds.Engine.md) | Game engine internals |
| [TaleWorlds.Library](TaleWorlds.Library.md) | Utility classes, math |
| [TaleWorlds.Localization](TaleWorlds.Localization.md) | Text/translation system |

---

## By Feature Area

### Enlistment Core
- [TaleWorlds.CampaignSystem](TaleWorlds.CampaignSystem.md) - Hero management, party following, encounters
- [0Harmony](0Harmony.md) - Patching for visibility, encounter suppression

### Battle System
- [TaleWorlds.MountAndBlade](TaleWorlds.MountAndBlade.md) - Kill tracking, formations, mission behavior
- [SandBox](SandBox.md) - Mission setup patterns

### Menu System
- [TaleWorlds.CampaignSystem](TaleWorlds.CampaignSystem.md) - GameMenu API

### Equipment/Quartermaster System
- [SandBox.GauntletUI](SandBox.GauntletUI.md) - Gauntlet UI framework, our equipment screens
- [TaleWorlds.Core](TaleWorlds.Core.md) - Items, equipment slots
- [TaleWorlds.ObjectSystem](TaleWorlds.ObjectSystem.md) - Item lookup

### Party Visibility
- [SandBox.View](Sandbox.View.md) - PartyVisualManager for safe hiding
- [SandBox.ViewModelCollection](Sandbox.ViewModelCollection.md) - Nameplate hiding

### Save/Load
- [TaleWorlds.SaveSystem](TaleWorlds.SaveSystem.md) - SyncData patterns

---

## Common Tasks Quick Reference

| Task | Reference |
|------|-----------|
| Subscribe to game events | [TaleWorlds.CampaignSystem](TaleWorlds.CampaignSystem.md) |
| Create a game menu | [TaleWorlds.CampaignSystem](TaleWorlds.CampaignSystem.md) |
| Create a Gauntlet UI screen | [SandBox.GauntletUI](SandBox.GauntletUI.md) |
| Track kills in battle | [TaleWorlds.MountAndBlade](TaleWorlds.MountAndBlade.md) |
| Hide the player party | [SandBox.View](Sandbox.View.md) |
| Patch game methods | [0Harmony](0Harmony.md) |
| Save mod data | [TaleWorlds.SaveSystem](TaleWorlds.SaveSystem.md) |
| Find items by ID | [TaleWorlds.ObjectSystem](TaleWorlds.ObjectSystem.md) |
| Display messages to player | [TaleWorlds.Core](TaleWorlds.Core.md) |

---

## Decompile Location

All decompiled source is at: `C:\Dev\Enlisted\DECOMPILE\`
