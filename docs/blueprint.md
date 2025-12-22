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
-   **Compatible Version**: 1.3.12

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
`C:\Dev\Enlisted\Decompile\` (Targeting v1.3.12)

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
  - Identity/             # Role detection (Traits) and Reputation helpers
  - Escalation/           # Lord/Officer/Soldier reputation and Scrutiny/Discipline
  - Company/              # Company-wide Needs (Readiness, Morale, etc.)
  - Context/              # Army context and objective analysis
  - Interface/            # Native Game Menus and News/Reports
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

### Native Game Menus
We use `CampaignGameStarter.AddGameMenu()` and `AddGameMenuOption()` to build all interfaces. Avoid custom Gauntlet UI (ViewModels/XML).

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

| Menu ID | Purpose |
|---------|---------|
| `enlisted_status` | Main hub: view rank, orders, and reports summary. |
| `enlisted_camp` | Camp activities: Rest, Train, Morale, Equipment Check. |
| `enlisted_reports` | Detailed reports: Daily brief, service record, company status. |
| `enlisted_detail_status`| Detailed identity: Traits, role, and reputation levels. |
| `enlisted_decisions` | Pending event choices. |
| `enlisted_medical` | Medical care and treatment. |
| `enlisted_quartermaster`| Equipment management. |
