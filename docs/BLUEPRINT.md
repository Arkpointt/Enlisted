# Enlisted - Project Blueprint

**Summary:** Complete guide to the Enlisted mod's architecture, coding standards, and development practices. This is the single source of truth for understanding how this project works and how we write code.

**Last Updated:** 2025-12-22  
**Target Game:** Bannerlord v1.3.11  
**Related Docs:** [DEVELOPER-GUIDE.md](DEVELOPER-GUIDE.md), [Reference/native-apis.md](Reference/native-apis.md)

---

## Quick Orientation

**New to this project? Read this section first.**

This is an **Enlisted mod for Mount & Blade II: Bannerlord v1.3.11** that transforms the game into a soldier career simulator. Players enlist with lords, follow orders, manage reputation, and progress through military ranks.

**Critical Project Constraints:**
1. **Target:** Bannerlord v1.3.11 specifically (not latest version)
2. **API Verification:** ALWAYS use local decompile at `C:\Dev\Enlisted\Decompile\` (not online docs)
3. **Old-style .csproj:** Must manually add new files to `Enlisted.csproj` with `<Compile Include="..."/>`
4. **Build:** Use `dotnet build -c "Enlisted RETAIL" /p:Platform=x64`
5. **Logging:** Use `ModLogger` in `Modules\Enlisted\Debugging\` folder
6. **ReSharper:** Follow ReSharper recommendations (never suppress without documented reason)

### Understanding the Project

**Scope:** The mod focuses on the **enlisted lord's party** (the Company). If the Company is part of a larger Army, acknowledge that as context only. Deep army-wide simulation is future work.

**Key Nuances:**
- Player uses an **invisible party** while enlisted (see `Features/Core/enlistment.md`)
- **Native integration** preferred over custom UI (use game menus, trait system, etc.)
- **Data-driven content** via JSON events/orders + XML localization
- **Emergent identity** from player choices (not menu selections)
- Comments describe **current behavior** (no changelog-style "Phase X added..." framing)

### Finding Documentation

1. **[INDEX.md](INDEX.md)** - Complete catalog of all docs organized by category
2. **[Features/Core/core-gameplay.md](Features/Core/core-gameplay.md)** - Best overview of how everything works
3. **[Content/event-catalog-by-system.md](Content/event-catalog-by-system.md)** - All events/decisions/orders
4. **[Reference/native-apis.md](Reference/native-apis.md)** - Bannerlord API snippets

### Creating New Documentation

**Format Requirements:**
```markdown
# Title

**Summary:** 2-3 sentences explaining what this covers and when to reference it

**Status:** ✅ Current | ⚠️ In Progress | 📋 Specification | 📚 Reference  
**Last Updated:** YYYY-MM-DD  
**Related Docs:** [Link 1], [Link 2]

---

## Index
1. [Section 1](#section-1)
2. [Section 2](#section-2)

---

## Section 1
[Content describing CURRENT behavior, not planning/changelog language]
```

**File Naming:** Use kebab-case (`my-new-feature.md`)

**Documentation Location Guide:**
- Core systems → `Features/Core/`
- Equipment/logistics → `Features/Equipment/`
- Combat mechanics → `Features/Combat/`
- Events/content → `Features/Content/`
- Campaign/world → `Features/Campaign/`
- UI systems → `Features/UI/`
- Technical specs → `Features/Technical/`
- Content catalog → `Content/`
- API/research → `Reference/`

---

## Common Tasks

**Add a new C# file:**
1. Create file in appropriate `src/Features/` subfolder
2. Add `<Compile Include="path/to/file.cs"/>` to `Enlisted.csproj`
3. Build to verify

**Add new event/decision/order:**
1. Add JSON definition to `ModuleData/Enlisted/Events/`
2. Add XML localization to `ModuleData/Enlisted/Languages/`
3. Use placeholder variables in text (e.g., `{PLAYER_NAME}`, `{SERGEANT}`, `{LORD_NAME}`)
4. Update `Content/event-catalog-by-system.md`

**Placeholder variables:** See [Event Catalog - Placeholder Variables](Content/event-catalog-by-system.md#placeholder-variables) for complete list

**Check if feature exists:**
1. Search INDEX.md for topic
2. Check `Features/Core/core-gameplay.md` for mentions
3. Use grep to search `src/` for implementation

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

**Dynamic Dialogue Pattern (Data-Driven Approach):**
For context-sensitive dialogue that varies by game state (like quartermaster responses), we use a JSON-driven catalog with dynamic runtime evaluation:
1. **Define in JSON:** Store dialogue nodes with context conditions (`supply_level`, `reputation`, etc.).
2. **Dynamic Registration:** At startup, register dialogue lines for every contextual variant of a node ID.
3. **Runtime Evaluation:** Use Bannerlord `OnConditionDelegate` to call `GetCurrentDialogueContext()` every time a line is considered for display.
4. **Specificity Matching:** The system selects the most specific matching variant (highest specificity count).
5. **XML Support:** Use standard `textId` fields for localization, with JSON `text` fields as fallbacks.

**Example Implementation:**
```csharp
// Condition delegate evaluated every time dialogue navigates to this node
ConversationSentence.OnConditionDelegate condition = () => {
    var currentContext = GetCurrentDialogueContext();
    return nodeContext.Matches(currentContext);
};

// Register with Bannerlord system
starter.AddDialogLine(id, inputToken, outputToken, text, condition, consequence, priority);
```

**Benefits:** Full localization support, real-time reaction to game state changes during a single conversation session, and decoupling of content from C# code.

#### JSON for Content/Config
- **Content:** `ModuleData/Enlisted/Events/*.json` (events, automatic decisions)
- **Config:** `ModuleData/Enlisted/*.json`
- **Orders:** `ModuleData/Enlisted/Orders/orders_t1_t3.json`, `orders_t4_t6.json`, `orders_t7_t9.json` (17 total)
- **Decisions:** `ModuleData/Enlisted/Decisions/*.json` (34 player-initiated Camp Hub decisions)

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
  - Retinue/              # Commander's Retinue (T7+), Service Records, trickle/requisition
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
| `Orders/*.json` | Mission definitions for the Orders system (order_* prefix). |
| `Events/*.json` | Role-based narrative events, automatic decisions, player event popups. |
| `Decisions/*.json` | Player-initiated Camp Hub decisions (dec_* prefix, 34 total). |

---

## Menu System

**Primary Documentation:** See [UI Systems Master Reference](Features/UI/ui-systems-master.md)

| Menu ID | Purpose |
|---------|---------|
| `enlisted_camp_hub` | Central navigation hub with accordion-style decision sections. |
| `enlisted_medical` | Medical care and treatment (when player has active condition). |

**Decision Sections (within Camp Hub):**
- TRAINING - Training-related player decisions (dec_weapon_drill, dec_spar, etc.)
- SOCIAL - Social interaction decisions (dec_join_men, dec_write_letter, etc.)
- ECONOMIC - Economic decisions (dec_gamble_low, dec_side_work, etc.)
- CAREER - Career advancement decisions (dec_request_audience, dec_volunteer_duty, etc.)
- INFORMATION - Intelligence gathering (dec_listen_rumors, dec_scout_area, etc.)
- EQUIPMENT - Gear management (dec_maintain_gear, dec_visit_quartermaster)
- RISK_TAKING - High-risk actions (dec_dangerous_wager, dec_prove_courage, etc.)
- CAMP_LIFE - Self-care and rest (dec_rest, dec_seek_treatment)
- LOGISTICS - Quartermaster-related (from events_player_decisions.json)

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
