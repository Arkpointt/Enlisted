# Blueprint

Architecture and standards for the Enlisted mod (v0.9.0).

## Index

- [Build](#build)
- [Steam Workshop Upload](#steam-workshop-upload)
- [Dependencies](#dependencies)
- [Native Reference (Decompile)](#native-reference-decompile)
- [Structure](#structure)
- [Adding New Files](#adding-new-files)
- [Logging](#logging)
- [Key Patterns](#key-patterns)
- [Configuration](#configuration)
- [Menu System](#menu-system)

---

## Build

-   **Visual Studio**: Config "Enlisted RETAIL" -> Build
-   **CLI**: `dotnet build -c "Enlisted RETAIL" /p:Platform=x64`
-   **Output**: `<Bannerlord>/Modules/Enlisted/bin/Win64_Shipping_Client/`

---

## Steam Workshop Upload

-   **Workshop ID**: `3621116083`
-   **Config**: `tools/workshop/WorkshopUpdate.xml` and `workshop_upload.vdf`
-   **Process**: Build first, then upload via SteamCMD.
-   **Compatible Version**: 1.3.11

---

## Dependencies

```xml
<DependedModules>
  <DependedModule Id="Bannerlord.Harmony" />
</DependedModules>
```

---

## Native Reference (Decompile)

Decompiled Bannerlord source for API reference:
`C:\Dev\Enlisted\Decompile\` (Targeting v1.3.11)

---

## Structure

```
src/
- Mod.Entry/              # SubModule + Harmony init
- Mod.Core/               # Logging, config, helpers
- Mod.GameAdapters/       # Harmony patches
- Features/
  - Enlistment/           # Core service state, retirement
  - Orders/               # Mission-driven directives (Chain of Command)
  - Content/              # Events, Decisions, and narrative delivery system
  - Identity/             # Role detection (Traits) and Reputation helpers
  - Escalation/           # Lord/Officer/Soldier reputation and Scrutiny/Discipline
  - Company/              # Company-wide Needs (Readiness, Morale, Supply)
  - Context/              # Army context and objective analysis
  - Interface/            # Camp Hub menu, News/Reports
  - Equipment/            # Quartermaster and gear management
  - Ranks/                # Promotions and culture-specific titles
  - Conversations/        # Dialog management
  - Combat/               # Battle participation and formation assignment
  - Conditions/           # Player medical status (injury/illness)
  - Retinue/              # Service Records and Retinue/Companion management
  - Camp/                 # Camp activities and rest logic
```

```
ModuleData/
- Enlisted/               # JSON config + content (Orders, Events, etc.)
- Languages/              # XML localization (enlisted_strings.xml)
```

---

## Adding New Files

**CRITICAL**: This project uses an old-style `.csproj` with explicit file includes. New `.cs` files are NOT automatically compiled.

1.  Create the `.cs` file.
2.  **Manually add it to `Enlisted.csproj`** in the `<Compile Include="..."/>` ItemGroup.
3.  Build and verify.

---

## Logging

**ALL MOD LOGS OUTPUT TO:** `<BannerlordInstall>\Modules\Enlisted\Debugging\`

Example full path: `C:\Program Files (x86)\Steam\steamapps\common\Mount & Blade II Bannerlord\Modules\Enlisted\Debugging\`

The mod writes session logs directly to the `Debugging` folder inside the Enlisted module directory. This is NOT the game's crash logs folder and NOT Documents.

Files written here:
- `Session-A_{timestamp}.log` - Current session (newest)
- `Session-B_{timestamp}.log` - Previous session
- `Session-C_{timestamp}.log` - Oldest kept session
- `Conflicts-A_{timestamp}.log` - Current conflicts diagnostics
- `Conflicts-B_{timestamp}.log` - Previous conflicts
- `Conflicts-C_{timestamp}.log` - Oldest kept conflicts
- `Current_Session_README.txt` - Summary of active logs

The mod no longer creates `enlisted.log` or `conflicts.log` (legacy filenames). All logs use timestamped rotation.

Use `ModLogger` for all logging. Category-based verbosity control via `settings.json`.

---

## Key Patterns

### UI Architecture
- **GameMenus:** Main navigation and status displays via `CampaignGameStarter.AddGameMenu()` and `AddGameMenuOption()`
- **Popups:** Event delivery via `MultiSelectionInquiryData` for narrative choices
- **Gauntlet:** Quartermaster equipment grid (limited custom UI for complex selection interfaces)
- **Localization:** Dual-field pattern in JSON (`titleId` + `title`) ensures fallback text always displays

### Party Following & Visibility
```csharp
party.SetMoveEscortParty(lordParty, NavigationType.Default, false);
party.IsVisible = false;  // Hide on map
```

### Gold Transactions
Always use `GiveGoldAction` to ensure the party treasury is updated correctly in the UI.
```csharp
GiveGoldAction.ApplyBetweenCharacters(null, Hero.MainHero, amount);
```

### Reputation & Needs
Always use the centralized managers (`EscalationManager`, `CompanyNeedsManager`) to modify state, as they handle clamping and logging.

---

## Configuration

| File | Purpose |
|------|---------|
| `enlisted_config.json` | Feature flags and core balancing. |
| `progression_config.json` | Tier XP thresholds and culture-specific rank titles. |
| `Orders/*.json` | Mission definitions for the Orders system. |
| `Events/*.json` | Role-based narrative and social events. |
| `Activities/activities.json` | Data-driven camp actions. |

---

## Menu System

**Primary Documentation:** See [UI Systems Master Reference](Features/UI/ui-systems-master.md)

| Menu ID | Purpose |
|---------|---------|
| `enlisted_camp_hub` | Central navigation hub with accordion-style decision sections. |
| `enlisted_medical` | Medical care and treatment (when player has active condition). |

**Decision Sections (within Camp Hub):**
- OPPORTUNITIES - Context-triggered automatic decisions
- TRAINING - Training-related player choices
- SOCIAL - Social interaction decisions
- CAMP LIFE - Camp life decisions
- LOGISTICS - Quartermaster-related decisions (if visible)

**Event Delivery:**
- Uses `MultiSelectionInquiryData` popups for narrative events
- Triggered by: EventPacingManager, EscalationManager, DecisionManager
- See [Event Delivery System](Features/UI/ui-systems-master.md#event-delivery-system)

**Localization:**
- JSON files use dual fields (`titleId` + `title`) for robust fallback text
- XML strings in `ModuleData/Languages/enlisted_strings.xml`
- See [Localization System](Features/UI/ui-systems-master.md#localization-system) for troubleshooting
