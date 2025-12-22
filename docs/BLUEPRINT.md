# Enlisted - Project Blueprint

**Summary:** Complete guide to the Enlisted mod's architecture, coding standards, and development practices. This is the single source of truth for understanding how this project works and how we write code.

**Last Updated:** 2025-12-22  
**Target Game:** Bannerlord v1.3.11  
**Related Docs:** [DEVELOPER-GUIDE.md](DEVELOPER-GUIDE.md), [Reference/campaignsystem-apis.md](Reference/campaignsystem-apis.md)

---

## Index

1. [Overview & Philosophy](#overview--philosophy)
2. [Engineering Standards](#engineering-standards)
   - [Code Quality](#code-quality)
   - [API Verification](#api-verification)
   - [Data File Conventions](#data-file-conventions)
   - [Tooltip Best Practices](#tooltip-best-practices)
   - [Logging Standards](#logging-standards)
3. [Build & Deployment](#build--deployment)
4. [Steam Workshop Upload](#steam-workshop-upload)
5. [Dependencies](#dependencies)
6. [Native Reference (Decompile)](#native-reference-decompile)
7. [File Structure](#file-structure)
8. [Adding New Files](#adding-new-files)
9. [Key Patterns](#key-patterns)
10. [Configuration](#configuration)
11. [Menu System](#menu-system)
12. [Common Pitfalls](#common-pitfalls)

---

## Overview & Philosophy

Enlisted transforms Bannerlord into a soldier career simulator. Players enlist with a lord, follow orders, manage reputation, and progress through ranks from recruit to commander.

**Design Principles:**
- Emergent identity from choices, not menus
- Native Bannerlord integration (traits, skills, game menus)
- Minimal custom UI (use game's native systems)
- Choice-driven narrative progression
- Realistic military hierarchy and consequences

---

## Engineering Standards

### Code Quality

#### ReSharper Linter
- **Always follow ReSharper recommendations** (available in Rider or Visual Studio)
- Fix warnings, don't suppress with pragmas
- Exception: Only suppress if there's a specific compatibility reason with a comment explaining why

#### Comment Style
Comments should be factual descriptions of current behavior, written as a human developer would—professional and natural.

✅ **Good:**
```csharp
// Checks if the player can re-enlist with this faction based on cooldown and discharge history.
private bool CanReenlistWithFaction(Kingdom faction)
```

❌ **Bad:**
```csharp
// Phase 2: Added re-enlistment check. Previously used FactionVeteranRecord, now uses FactionServiceRecord.
// Changed from using days to using CampaignTime for better accuracy.
private bool CanReenlistWithFaction(Kingdom faction)
```

**Rules:**
- Describe WHAT the code does NOW, not what it used to do or when it changed
- No "Phase X" references in code comments
- No changelog-style framing ("Added X", "Changed from Y")
- No "legacy" or "migration" mentions in doc comments
- Write professionally and naturally

#### Code Organization
- Reuse existing patterns (e.g., copy OrderCatalog structure for new catalogs)
- Keep related functionality together
- Use clear, descriptive names
- Group related fields/properties/methods

---

### API Verification

**ALWAYS verify against the local native decompile FIRST**

- **Location:** `C:\Dev\Enlisted\Decompile\`
- **Target Version:** v1.3.11
- **Key Assemblies:**
  - `TaleWorlds.CampaignSystem` - Party, Settlement, Campaign behaviors
  - `TaleWorlds.Core` - CharacterObject, ItemObject
  - `TaleWorlds.Library` - Vec2, MBList, utility classes
  - `TaleWorlds.MountAndBlade` - Mission, Agent, combat
  - `SandBox.View` - Menu views, map handlers

**Process:**
1. Check decompile for actual API (authoritative)
2. Use official docs only for quick lookups (secondary)
3. Don't rely on external docs or AI assumptions
4. Verify method signatures, property types, enum values

**Example:**
```
Need: Party position
Check: C:\Dev\Enlisted\Decompile\TaleWorlds.CampaignSystem\TaleWorlds\CampaignSystem\Party\MobileParty.cs
Find: GetPosition2D() → Vec2 (NOT Position2D property)
```

---

### Data File Conventions

#### XML for Player-Facing Text
- **Localization:** `ModuleData/Languages/enlisted_strings.xml`
- **Gauntlet UI:** `GUI/Prefabs/**.xml`
- **In Code:** Use `TextObject("{=stringId}Fallback text")`
- Add string keys to enlisted_strings.xml even if only using English

#### JSON for Content/Config
- **Content:** `ModuleData/Enlisted/Events/*.json`
- **Config:** `ModuleData/Enlisted/*.json`
- **Orders/Decisions:** `ModuleData/Enlisted/Orders/*.json`, `Decisions/*.json`

#### Critical JSON Rule
In JSON, **fallback fields must immediately follow their ID fields** for proper parser association:

✅ **Correct:**
```json
{
  "titleId": "event_title_key",
  "title": "Fallback Title",
  "setupId": "event_setup_key",
  "setup": "Fallback setup text..."
}
```

❌ **Wrong:**
```json
{
  "titleId": "event_title_key",
  "setupId": "event_setup_key",
  "title": "Fallback Title",
  "setup": "Fallback setup text..."
}
```

---

### Tooltip Best Practices

**Every event option and decision must have a tooltip** explaining consequences.

#### Guidelines:
- Tooltips appear on hover in `MultiSelectionInquiryData` popups
- One sentence, under 80 characters
- Explain what happens, requirements, or trade-offs
- Be concise and clear

#### Examples:

**Consequences:**
```json
{"tooltip": "Accept discharge. 90-day re-enlistment block applies."}
{"tooltip": "Desert immediately. Criminal record and faction hostility."}
```

**Skill checks:**
```json
{"tooltip": "Charm check determines outcome."}
{"tooltip": "Requires Leadership 50+ to attempt."}
```

**Training:**
```json
{"tooltip": "Train with the weapon you carry into battle"}
{"tooltip": "Work on the skill that needs most improvement"}
{"tooltip": "Build stamina and footwork"}
```

**Requirements:**
```json
{"tooltip": "Requires Tier 7+ to unlock"}
{"tooltip": "Only available when Company Morale is below 50"}
```

---

### Logging Standards

**ALL MOD LOGS OUTPUT TO:** `<BannerlordInstall>\Modules\Enlisted\Debugging\`

Example full path: `C:\Program Files (x86)\Steam\steamapps\common\Mount & Blade II Bannerlord\Modules\Enlisted\Debugging\`

The mod writes session logs directly to the `Debugging` folder inside the Enlisted module directory. This is NOT the game's crash logs folder and NOT Documents.

#### Usage:
```csharp
ModLogger.Info("Category", "message");
ModLogger.Debug("Category", "detailed info");
ModLogger.Warn("Category", "warning");
ModLogger.Error("Category", "error details");
```

#### Categories:
- Enlistment, Combat, Equipment, Events, Orders, Reputation
- Identity, Company, Context, Interface, Ranks, Conversations
- Retinue, Camp, Conditions

#### What to Log:
- Actions (enlistment, promotions, discharges)
- Errors and warnings
- State changes (reputation, needs, orders)
- Event triggers and outcomes

Configure levels in `settings.json`:
```json
{
  "LogLevels": {
    "Default": "Info",
    "Battle": "Debug",
    "Equipment": "Warn"
  }
}
```

#### Performance-Friendly Logging
All features include logging for error catching and diagnostics to help troubleshoot issues, game updates, or mod conflicts in production.

---

## Build & Deployment

**Visual Studio:**
- Configuration: "Enlisted RETAIL"
- Platform: x64
- Build

**Command Line:**
```bash
dotnet build -c "Enlisted RETAIL" /p:Platform=x64
```

**Output Location:**
```
<Bannerlord>/Modules/Enlisted/bin/Win64_Shipping_Client/
```

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

This is the authoritative source for verifying Bannerlord API usage. See [API Verification](#api-verification) section above.

---

## File Structure

```
src/
|- Mod.Entry/              # SubModule + Harmony init
|- Mod.Core/               # Logging, config, helpers
|- Mod.GameAdapters/       # Harmony patches
|- Features/
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
|- Enlisted/               # JSON config + content (Orders, Events, etc.)
|- Languages/              # XML localization (enlisted_strings.xml)
```

---

## Adding New Files

**CRITICAL**: This project uses an old-style `.csproj` with explicit file includes. New `.cs` files are NOT automatically compiled.

1. Create the `.cs` file
2. **Manually add it to `Enlisted.csproj`** in the `<Compile Include="..."/>` ItemGroup
3. Build and verify

Example:
```xml
<Compile Include="src\Features\MyFeature\MyNewClass.cs"/>
```

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

### Deferred Operations
Use for encounter transitions:
```csharp
NextFrameDispatcher.RunNextFrame(() => GameMenu.ActivateGameMenu("menu_id"));
```

### Safe Hero Access
Null-safe during character creation:
```csharp
var hero = CampaignSafetyGuard.SafeMainHero;
```

### Equipment Slot Iteration
Use numeric loop (not `Enum.GetValues`):
```csharp
// ❌ WRONG: Includes invalid count values, causes IndexOutOfRangeException
foreach (EquipmentIndex slot in Enum.GetValues(typeof(EquipmentIndex))) { ... }

// ✅ CORRECT: Iterate valid indices only (0-11)
for (int i = 0; i < (int)EquipmentIndex.NumEquipmentSetSlots; i++)
{
    var slot = (EquipmentIndex)i;
}
```

---

## Configuration

| File | Purpose |
|------|---------|
| `enlisted_config.json` | Feature flags and core balancing. |
| `progression_config.json` | Tier XP thresholds and culture-specific rank titles. |
| `Orders/*.json` | Mission definitions for the Orders system. |
| `Events/*.json` | Role-based narrative and social events. |
| `Activities/activities.json` | Data-driven camp actions. |
| `Decisions/*.json` | Decision definitions for Camp Hub. |

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

---

## Common Pitfalls

### 1. Using `ChangeHeroGold` Instead of `GiveGoldAction`
**Problem:** `ChangeHeroGold` modifies internal gold not visible in party UI  
**Solution:** Use `GiveGoldAction.ApplyBetweenCharacters()`

### 2. Iterating Equipment with `Enum.GetValues`
**Problem:** Includes invalid count enum values, causes crashes  
**Solution:** Use numeric loop to `NumEquipmentSetSlots`

### 3. Modifying Reputation/Needs Directly
**Problem:** Bypasses clamping and logging  
**Solution:** Always use managers (EscalationManager, CompanyNeedsManager)

### 4. Not Adding New Files to .csproj
**Problem:** New .cs files exist but aren't compiled  
**Solution:** Manually add `<Compile Include="..."/>` entries

### 5. Relying on External API Documentation
**Problem:** Outdated or incorrect API references  
**Solution:** Always verify against local decompile first

### 6. Ignoring ReSharper Warnings
**Problem:** Code quality degrades over time  
**Solution:** Fix warnings, don't suppress unless absolutely necessary

### 7. Forgetting Tooltips in Events
**Problem:** Players don't understand consequences  
**Solution:** Every option needs a clear, concise tooltip

### 8. Mixing JSON Field Order
**Problem:** Localization breaks when ID/fallback fields are separated  
**Solution:** Always put fallback field immediately after ID field
