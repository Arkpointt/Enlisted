# Enlisted - Project Blueprint

**Summary:** Complete guide to the Enlisted mod's architecture, coding standards, and development practices. This is the single source of truth for understanding how this project works and how we write code.

**Last Updated:** 2026-01-01
**Target Game:** Bannerlord v1.3.13
**Related Docs:** [DEVELOPER-GUIDE.md](DEVELOPER-GUIDE.md), [Reference/native-apis.md](Reference/native-apis.md)

---

## Quick Orientation

**New to this project? Read this section first.**

This is an **Enlisted mod for Mount & Blade II: Bannerlord v1.3.13** that transforms the game into a soldier career simulator. Players enlist with lords, follow orders, manage reputation, and progress through military ranks.

**Critical Project Constraints:**
1. **Target:** Bannerlord v1.3.13 specifically (not latest version)
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
4. **[Features/Technical/conflict-detection-system.md](Features/Technical/conflict-detection-system.md)** - System interactions and validation rules
5. **[Reference/native-apis.md](Reference/native-apis.md)** - Bannerlord API snippets

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
1. Add JSON definition to `ModuleData/Enlisted/Events/` or `Decisions/`
2. Add XML localization entries to `ModuleData/Languages/enlisted_strings.xml`
   - Place in appropriate section (Events, Decisions, Orders)
   - Use `&#xA;` for newlines in XML attributes
   - Escape special characters: `&` → `&amp;`, `'` → `&apos;`, `"` → `&quot;`
3. Use placeholder variables in text (e.g., `{PLAYER_NAME}`, `{SERGEANT}`, `{LORD_NAME}`)
4. **Run validation:** `python tools/events/validate_events.py` to check for missing fallback fields
5. Run `python tools/events/sync_event_strings.py --check` to verify all strings present
6. Update `Content/event-catalog-by-system.md`

**Placeholder variables:** See [Event Catalog - Placeholder Variables](Content/event-catalog-by-system.md#placeholder-variables) for complete list

**Check if feature exists:**
1. Search INDEX.md for topic
2. Check `Features/Core/core-gameplay.md` for mentions
3. Use grep to search `src/` for implementation

**Before committing:**
```powershell
# Validate all events have proper structure, references, logic, and consistency
python tools/events/validate_content.py

# Build and check for errors
cd C:\Dev\Enlisted\Enlisted
dotnet build -c "Enlisted RETAIL" /p:Platform=x64
```

**Upload to Steam Workshop:**
1. Update `"changenote"` in `tools/workshop/workshop_upload.vdf` with version and changes
2. Build if code changed: `dotnet build -c "Enlisted RETAIL" /p:Platform=x64`
3. Run upload: `cd C:\Dev\steamcmd; .\steamcmd.exe +login milkyway12 +workshop_build_item "C:\Dev\Enlisted\Enlisted\tools\workshop\workshop_upload.vdf" +quit`
4. See [Steam Workshop Upload](#steam-workshop-upload) section for full details

---

## Code Review Checklist

Before committing code, verify:

**Code Quality:**
- [ ] All ReSharper/Rider warnings addressed
- [ ] No unused `using` directives
- [ ] Braces used for all single-line control statements
- [ ] No redundant namespace qualifiers
- [ ] No unused variables, parameters, or methods
- [ ] No redundant default parameter values in method calls

**Functionality:**
- [ ] Code builds without errors
- [ ] Relevant tests pass (if applicable)
- [ ] Logging added for significant actions/errors
- [ ] Null checks added where needed

**Documentation:**
- [ ] Comments describe current behavior (not changelog)
- [ ] XML localization strings added for new events
- [ ] Tooltips provided for all event options
- [ ] New files added to `Enlisted.csproj`

**Data Files:**
- [ ] JSON fallback fields immediately follow ID fields
- [ ] Order events include skillXp in effects (tooltips cannot be null, XP cannot be missing)
- [ ] Event validation passes: `python tools/events/validate_content.py`

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
   - [Dual Build Configuration](#dual-build-configuration)
   - [Build Commands](#build-commands)
   - [Conditional Compilation](#conditional-compilation)
   - [Battle AI File Organization](#battle-ai-file-organization)
   - [Critical Battle AI Rules](#critical-battle-ai-rules)
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
- **Fix all warnings before committing** - don't suppress with pragmas unless truly necessary
- **Run Qodana analysis** before major commits to catch code quality issues
- Exception: Only suppress if there's a specific compatibility reason with a comment explaining why

**Handling False Positives:**

When Qodana/ReSharper reports a warning that cannot be fixed without breaking compilation, use `[SuppressMessage]` attributes with clear justification:

```csharp
[System.Diagnostics.CodeAnalysis.SuppressMessage("ReSharper", "RedundantNameQualifier",
    Justification = "ConfigurationManager conflicts with TaleWorlds.Library.ConfigurationManager")]
private void MyMethod()
{
    var config = Enlisted.Mod.Core.Config.ConfigurationManager.LoadConfig();
}
```

**Common False Positive Scenarios:**
- **Namespace conflicts:** When two types share the same name (e.g., `ConfigurationManager` in both `Enlisted.Mod.Core.Config` and `TaleWorlds.Library`), the fully qualified name is required
- **Harmony patches:** Parameters marked as unused but required by Harmony's signature matching
- **Reflection-called members:** Properties/methods called via Bannerlord's reflection system (save/load, UI binding)

**Verification Process:**
1. Try to fix the warning normally (add `using`, remove qualifier, etc.)
2. Build the project - if compilation fails, it's a false positive
3. Add `[SuppressMessage]` with clear `Justification` explaining why
4. Document the pattern in BLUEPRINT.md if it's a recurring issue

**Pre-commit checklist:**
```powershell
# Run Qodana analysis (if available)
qodana scan --show-report

# Build and check for errors
dotnet build -c "Enlisted RETAIL" /p:Platform=x64

# Validate events
python tools/events/validate_events.py
```

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

#### Code Quality Standards

**Braces:**
- Always use braces for single-line `if`, `for`, `while`, and `foreach` statements
- Exception: Single-line property getters/setters

✅ **Good:**
```csharp
if (condition)
{
    return value;
}
```

❌ **Bad:**
```csharp
if (condition)
    return value;
```

**Redundant Code:**
- Remove unused `using` directives
- Remove redundant namespace qualifiers (use `using` statements instead)
- Remove redundant default parameter values in method calls
- Remove unused local variables, parameters, and private methods
- Remove unused property accessors (getters/setters that are never called)

**Null Safety:**
- Address `PossibleNullReferenceException` warnings with null checks or null-conditional operators
- Use `?.` and `??` operators where appropriate

**Variable Initialization:**
- Remove redundant default member initializers (e.g., `= null`, `= 0`, `= false` for fields)
- Combine declaration and assignment when possible

---

### API Verification

**ALWAYS verify against the local native decompile FIRST**

- **Location:** `C:\Dev\Enlisted\Decompile\`
- **Target Version:** v1.3.13
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

**Manual Registration for Dynamic Text Variables:**
When a dialogue node needs to generate text dynamically (not just select from pre-defined variants), manually register both the NPC line and player responses:
```csharp
// Register NPC line with text variable setter
starter.AddDialogLine(
    "qm_supply_response_dynamic",
    "qm_supply_response",
    "qm_supply_response",
    "{=qm_supply_report}{SUPPLY_STATUS}",
    () => IsQuartermasterConversation() && SetSupplyStatusText(),  // Sets {SUPPLY_STATUS}
    null,
    200);

// Register player options that would normally come from JSON
starter.AddPlayerLine(
    "qm_supply_continue",
    "qm_supply_response",
    "qm_hub",
    "{=qm_continue}[Continue]",
    IsQuartermasterConversation,
    null,
    100);
```
**Why:** The JSON dialogue loader won't register player options for manually-registered NPC lines. You must register the complete conversation flow manually when using dynamic text generation.

**Benefits:** Full localization support, real-time reaction to game state changes during a single conversation session, and decoupling of content from C# code.

#### JSON for Content/Config
- **Content:** `ModuleData/Enlisted/Events/*.json` (events, automatic decisions)
- **Config:** `ModuleData/Enlisted/*.json`
- **Orders:** `ModuleData/Enlisted/Orders/orders_t1_t3.json`, `orders_t4_t6.json`, `orders_t7_t9.json` (17 total)
- **Decisions:** `ModuleData/Enlisted/Decisions/*.json` (34 player-initiated Camp Hub decisions)

#### Critical JSON Rules

**1. Fallback fields must immediately follow their ID fields:**

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

**2. Always include fallback text, even with XML localization:**

The fallback text in JSON serves two purposes:
- Displays if XML localization is missing (dev safety net)
- Source of truth for what the string should say

Never use empty fallback text (`"title": ""`). Always provide the actual text.

---

### Tooltip Best Practices

**Tooltips cannot be null.** Every event option, decision, order, and UI element that can have a tooltip must include one.

#### Guidelines:
- Factual, concise, brief description of what it does
- One sentence, under 80 characters
- Format: action + side effects + cooldown/restrictions
- Example: "Trains equipped weapon. Causes fatigue. Chance of injury. 3 day cooldown."

#### Examples:

**Training/Actions:**
```json
{"tooltip": "Trains equipped weapon"}
{"tooltip": "Build stamina and footwork"}
{"tooltip": "Maintain gear to prevent degradation"}
```

**Stat/Reputation Changes:**
```json
{"tooltip": "Harsh welcome. +5 Officer rep. -3 Retinue Loyalty."}
{"tooltip": "Risky move. +10 Courage. -5 Discipline. Injury chance."}
{"tooltip": "Show respect. +3 Lord rep. +2 Morale."}
```

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

**Requirements/Restrictions:**
```json
{"tooltip": "Requires Tier 7+ to unlock"}
{"tooltip": "Greyed out: Company Morale must be below 50"}
```

---

### Logging Standards

**ALL MOD LOGS OUTPUT TO:** `<BannerlordInstall>\Modules\Enlisted\Debugging\`

Example full path: `C:\Program Files (x86)\Steam\steamapps\common\Mount & Blade II Bannerlord\Modules\Enlisted\Debugging\`

The mod writes session logs directly to the `Debugging` folder inside the Enlisted module directory. This is NOT the game's crash logs folder and NOT Documents.

**Installation Locations:**
- **Steam Workshop:** `C:\Program Files (x86)\Steam\steamapps\workshop\content\261550\3621116083\`
- **Manual/Nexus:** `C:\Program Files (x86)\Steam\steamapps\common\Mount & Blade II Bannerlord\Modules\Enlisted\`

Note: Having both versions installed creates duplicate entries in the launcher and can cause conflicts. The mod includes automatic conflict detection at startup - check `Conflicts-A_{timestamp}.log` for mod compatibility issues.

#### Session Rotation System

The logger uses a **three-session rotation** to keep logs organized and manageable:

- **Session-A** - Current/newest session (e.g., `Session-A_2025-12-31_14-30-00.log`)
- **Session-B** - Previous session (renamed from old Session-A)
- **Session-C** - Oldest session (renamed from old Session-B)
- **Current_Session_README.txt** - Points to the active session file

When you start a new game session:
1. Old Session-B → Session-C (oldest is deleted)
2. Old Session-A → Session-B
3. New Session-A is created with current timestamp

**Where to look for logs:**
- **Game Install:** `C:\Program Files (x86)\Steam\steamapps\common\Mount & Blade II Bannerlord\Modules\Enlisted\Debugging\`
  - Check **Session-A** for your current/most recent gameplay
  - Check **Session-B** if you need the previous session
  - Check **Session-C** for the oldest kept session
  - Read **Current_Session_README.txt** to see which file is active
- **Workspace:** `C:\Dev\Enlisted\Enlisted\Debugging\`
  - Contains development scripts and build placeholders only
  - NOT where runtime logs are written

#### Usage:
```csharp
ModLogger.Info("Category", "message");
ModLogger.Debug("Category", "detailed info");
ModLogger.Warn("Category", "warning");
ModLogger.Error("Category", "error details");
ModLogger.LogOnce("UniqueKey", "Category", "message"); // Only logs once per session
```

#### Categories:
- Enlistment, Combat, Equipment, Events, Orders, Reputation
- Identity, Company, Context, Interface, Ranks, Conversations
- Retinue, Camp, Conditions, Supply, Logistics

#### What to Log:
- Actions (enlistment, promotions, discharges)
- Errors and warnings
- State changes (reputation, needs, orders)
- Event triggers and outcomes
- Supply changes (when significant, >10%)

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
All features include logging for error catching and diagnostics to help troubleshoot issues, game updates, or mod conflicts in production. The logger includes throttling and de-duplication to prevent log spam.

---

## Build & Deployment

### Single Build with Optional Battle AI SubModule

The project uses a **single build configuration** that includes all features, including Battle AI. Users can **disable Battle AI via checkbox** in the Bannerlord launcher without redownloading or switching mods.

**Key Benefits:**
- ✅ Single download for all users
- ✅ Battle AI can be toggled in Bannerlord launcher (native UI)
- ✅ No performance cost when disabled (SubModule never initializes)
- ✅ Simple build process
- ✅ Easy distribution (one Steam Workshop item)

### Build Commands

**Command Line (PowerShell):**
```powershell
cd C:\Dev\Enlisted\Enlisted; dotnet build -c "Enlisted RETAIL" /p:Platform=x64
```

**Visual Studio/Rider:**
- Select configuration: **"Enlisted RETAIL"**
- Platform: **x64**
- Build

### Output Location

```
C:\Program Files (x86)\Steam\steamapps\common\Mount & Blade II Bannerlord\Modules\Enlisted\bin\Win64_Shipping_Client\
```

### How Battle AI Optional SubModule Works

**SubModule.xml contains two SubModule entries:**
```xml
<Module>
    <Name value="Enlisted"/>
    <Id value="Enlisted"/>
    <Version value="v0.9.0"/>
    <SubModules>
        <!-- Core SubModule (Required) -->
        <SubModule>
            <Name value="Enlisted Core"/>
            <DLLName value="Enlisted.dll"/>
            <SubModuleClassType value="Enlisted.Mod.Entry.SubModule"/>
        </SubModule>
        
        <!-- Battle AI SubModule (Optional - can be disabled in launcher) -->
        <SubModule>
            <Name value="Enlisted Battle AI"/>
            <DLLName value="Enlisted.dll"/>
            <SubModuleClassType value="Enlisted.Features.Combat.BattleAI.BattleAISubModule"/>
        </SubModule>
    </SubModules>
</Module>
```

**In Bannerlord Launcher, users see:**
- ☑️ **Enlisted Core** (keep enabled)
- ☑️ **Enlisted Battle AI** (can uncheck to disable)

When "Enlisted Battle AI" is unchecked, the `BattleAISubModule` class never initializes, so there's **zero performance cost**.

### Conditional Compilation

Battle AI code uses `#if BATTLE_AI` preprocessor directives. The `BATTLE_AI` constant is **always defined** in the build, so all Battle AI code is always compiled into the DLL.

**Project Configuration (.csproj):**
```xml
<PropertyGroup Condition="'$(Configuration)|$(Platform)' == 'Enlisted RETAIL|x64'">
  <OutputPath>...\Modules\Enlisted\bin\Win64_Shipping_Client\</OutputPath>
  <DefineConstants>TRACE;BATTLE_AI</DefineConstants>
  <!-- BATTLE_AI always defined -->
</PropertyGroup>
```

**Battle AI SubModule Entry Point:**
```csharp
#if BATTLE_AI
using TaleWorlds.MountAndBlade;

namespace Enlisted.Features.Combat.BattleAI
{
    /// <summary>
    /// Optional SubModule for Battle AI systems.
    /// Users can disable this in the Bannerlord launcher.
    /// </summary>
    public class BattleAISubModule : MBSubModuleBase
    {
        protected override void OnSubModuleLoad()
        {
            base.OnSubModuleLoad();
            ModLogger.Info("BattleAI", "Battle AI SubModule loaded");
        }
        
        protected override void OnBeforeInitialModuleScreenSetAsRoot()
        {
            base.OnBeforeInitialModuleScreenSetAsRoot();
            // Register Battle AI behaviors here
        }
    }
}
#endif
```

### Battle AI File Organization

All Battle AI code must be organized in dedicated folders and wrapped in conditional compilation:

```
src/Features/Combat/BattleAI/
├── BattleAISubModule.cs                # SubModule entry point
├── Behaviors/
│   └── EnlistedBattleAIBehavior.cs     # Main mission behavior
├── Orchestration/
│   ├── BattleOrchestrator.cs           # Strategic coordinator
│   └── BattleContext.cs                # Battle state tracking
├── Formation/
│   ├── FormationController.cs          # Formation AI enhancements
│   └── FormationRoleAssigner.cs        # Role assignment logic
└── Agent/
    ├── AgentCombatEnhancer.cs          # Agent-level improvements
    └── TargetingLogic.cs               # Smart targeting

ModuleData/Enlisted/Config/
└── battle_ai_config.json               # AI tuning parameters (if needed)
```

**File Template:**
```csharp
#if BATTLE_AI
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using Enlisted.Mod.Core.Logging;

namespace Enlisted.Features.Combat.BattleAI.Orchestration
{
    /// <summary>
    /// Battle AI component - part of optional Battle AI SubModule.
    /// </summary>
    public class MyBattleAIClass
    {
        // Implementation here
        
        private void LogSomething()
        {
            ModLogger.Debug("BattleAI", "Your message here");
        }
    }
}
#endif
```

### Critical Battle AI Rules

**⚠️ CRITICAL: Keeping Battle AI Toggle-able**

To ensure users can disable Battle AI via the Bannerlord launcher checkbox:

1. **NEVER initialize Battle AI from Core SubModule**
   - Core SubModule (`Enlisted.Mod.Entry.SubModule`) must NOT reference Battle AI
   - Core SubModule must NOT register Battle AI behaviors
   - Core SubModule must NOT call Battle AI initialization code

2. **ALL Battle AI initialization MUST happen in BattleAISubModule**
   - Entry point: `Enlisted.Features.Combat.BattleAI.BattleAISubModule`
   - Mission behaviors registered ONLY through BattleAISubModule
   - No Battle AI code runs if BattleAISubModule isn't enabled

3. **Verify separation:**
   - If Battle AI checkbox is unchecked, BattleAISubModule never loads
   - Zero Battle AI code should execute when disabled
   - Core mod must function completely without Battle AI

**Other Rules:**

1. **Enlisted-Only Activation:** Battle AI ONLY runs when player is enlisted. If not enlisted, native AI runs unmodified.
2. **Field Battles Only:** Battle AI is for field battles only. Siege and naval combat use native AI.
3. **No Cheating:** AI improvements come from better decision-making, not stat buffs or information cheating.
4. **Performance First:** All Battle AI systems must be performance-conscious (appropriate update intervals, early bailouts).

### Adding Battle AI Files to .csproj

Battle AI files must be added to the .csproj (they use `#if BATTLE_AI` internally):

```xml
<ItemGroup>
  <!-- Base combat features -->
  <Compile Include="src\Features\Combat\Behaviors\EnlistedEncounterBehavior.cs"/>
  <Compile Include="src\Features\Combat\Behaviors\EnlistedFormationAssignmentBehavior.cs"/>
  <Compile Include="src\Features\Combat\Behaviors\EnlistedKillTrackerBehavior.cs"/>
</ItemGroup>

<ItemGroup>
  <!-- Battle AI SubModule (optional, users can disable in launcher) -->
  <Compile Include="src\Features\Combat\BattleAI\BattleAISubModule.cs"/>
  <Compile Include="src\Features\Combat\BattleAI\Behaviors\EnlistedBattleAIBehavior.cs"/>
  <Compile Include="src\Features\Combat\BattleAI\Orchestration\BattleOrchestrator.cs"/>
  <Compile Include="src\Features\Combat\BattleAI\Orchestration\BattleContext.cs"/>
  <!-- Add more Battle AI files here as they're created -->
</ItemGroup>
```

---

## Steam Workshop Upload

**Workshop ID**: `3621116083`
**Location**: https://steamcommunity.com/sharedfiles/filedetails/?id=3621116083

### Quick Upload Process

1. **Build the mod** (if code changed):
   ```powershell
   cd C:\Dev\Enlisted\Enlisted
   dotnet build -c "Enlisted RETAIL" /p:Platform=x64
   ```

2. **Update changelog** in `tools/workshop/workshop_upload.vdf`:
   - Edit the `"changenote"` field with version and changes
   - Use Steam BBCode formatting: `[b]bold[/b]`, `[list][*]item[/list]`, etc.

3. **Upload to Steam Workshop**:
   ```powershell
   cd C:\Dev\steamcmd
   .\steamcmd.exe +login milkyway12 +workshop_build_item "C:\Dev\Enlisted\Enlisted\tools\workshop\workshop_upload.vdf" +quit
   ```
   - **IMPORTANT**: All commands (`+login`, `+workshop_build_item`, `+quit`) must be in ONE command
   - SteamCMD uses cached credentials (no password needed if recently logged in)
   - Enter password/Steam Guard code if prompted (requires manual run in regular PowerShell if credentials expired)
   - Find cached username: Check `C:\Dev\steamcmd\config\config.vdf` under `"Accounts"` section

### Workshop Files

- `tools/workshop/workshop_upload.vdf` - Main upload config (changenote, description, tags)
- `tools/workshop/WorkshopUpdate.xml` - TaleWorlds official uploader config (alternative method)
- `tools/workshop/preview.png` - Workshop thumbnail (512x512 or 1024x1024)
- `tools/workshop/upload.ps1` - Helper script (requires interactive terminal)

### Updating Description

Edit the `"description"` field in `workshop_upload.vdf` using Steam BBCode:
- Headings: `[h1]Title[/h1]`, `[h2]Subtitle[/h2]`
- Lists: `[list][*]Item 1[*]Item 2[/list]`
- Bold/Italic: `[b]bold[/b]`, `[i]italic[/i]`
- Links: `[url=https://example.com]Link Text[/url]`
- Character limit: ~8000 characters

### Common Issues

**"File Not Found" error:**
- Check `"previewfile"` path is absolute: `C:\Dev\Enlisted\Enlisted\tools\workshop\preview.png`
- Verify `"contentfolder"` points to: `C:\Program Files (x86)\Steam\steamapps\common\Mount & Blade II Bannerlord\Modules\Enlisted`

**"Not logged on" error:**
- **CRITICAL**: SteamCMD sessions don't persist between separate command invocations
- **MUST** include `+login username` in the same command as `+workshop_build_item`
- **WRONG**: `.\steamcmd.exe +login milkyway12 +quit` then `.\steamcmd.exe +workshop_build_item ...` (login is lost)
- **CORRECT**: `.\steamcmd.exe +login milkyway12 +workshop_build_item "path/to/file.vdf" +quit` (all in one command)
- Cached credentials expire after ~2 weeks, then password/Steam Guard required
- Username is stored in `C:\Dev\steamcmd\config\config.vdf` under `"Accounts"` section

**Upload succeeds but mod doesn't update:**
- Workshop may take 5-10 minutes to propagate changes
- Users may need to unsubscribe/resubscribe to force update

**"Read-Host: Windows PowerShell is in NonInteractive mode" error:**
- The Cursor IDE terminal is non-interactive and cannot prompt for passwords
- Use cached credentials: `.\steamcmd.exe +login username` (no password parameter)
- If credentials expired, run the command manually in a regular PowerShell window
- Alternative: Use `upload.ps1` script in a regular terminal (not Cursor terminal)

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
`C:\Dev\Enlisted\Decompile\` (Targeting v1.3.13)

This is the authoritative source for verifying Bannerlord API usage. See [API Verification](#api-verification) section above.

---

## File Structure

```
src/
|- Mod.Entry/              # SubModule + Harmony init
|- Mod.Core/               # Logging, config, save system, helpers
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

### Save System & Serialization
Bannerlord's save system requires explicit registration of custom types. All mod-specific serializable types are registered in `EnlistedSaveDefiner` (`src/Mod.Core/SaveSystem/`).

**Adding new serializable types:**
1. Add the class/enum to `EnlistedSaveDefiner.DefineClassTypes()` or `DefineEnumTypes()` with a unique save ID
2. If using `Dictionary<T1,T2>` or `List<T>` with custom types, add container definition in `DefineContainerDefinitions()`
3. Implement `SyncData()` in the behavior that owns the state, using `SaveLoadDiagnostics.SafeSyncData()` wrapper

```csharp
// In behavior's SyncData:
public override void SyncData(IDataStore dataStore)
{
    SaveLoadDiagnostics.SafeSyncData(this, dataStore, () =>
    {
        dataStore.SyncData("myKey", ref _myField);
    });
}
```

**Key rules:**
- Custom classes need `AddClassDefinition(typeof(MyClass), saveId)` in the definer
- Container types like `Dictionary<string, int>` need `ConstructContainerDefinition(typeof(...))`
- Use primitive types in SyncData when possible (serialize dictionaries element-by-element)
- Enums can be cast to/from int for serialization

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
| `enlisted_muster_*` | Multi-stage muster system sequence (8 stages, every 12 days). See [Muster System](Features/Core/muster-system.md). |

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

**Muster System (Pay Day Ceremony):**
- Multi-stage GameMenu sequence (8 stages) occurring every 12 days
- Replaces simple pay inquiry popup with comprehensive muster experience
- Stages: Intro → Pay Line → Baggage Check → Inspection → Recruit → Promotion Recap → Retinue → Complete
- Integrates pay, rations, baggage checks, equipment inspections, rank progression
- Configurable time pause behavior (default: paused during muster)
- See [Muster System](Features/Core/muster-system.md) for complete flow

**Event Delivery:**
- Uses `MultiSelectionInquiryData` popups for narrative events
- Triggered by: EventPacingManager, EscalationManager, DecisionManager
- Muster-specific events (inspection, recruit, baggage) integrated as menu stages
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
**Problem:** Tooltips set to null or missing, players don't understand consequences
**Solution:** Tooltips cannot be null. Every option must have a factual, concise tooltip

### 8. Mixing JSON Field Order
**Problem:** Localization breaks when ID/fallback fields are separated
**Solution:** Always put fallback field immediately after ID field

### 9. Missing XML Localization Strings
**Problem:** Events show raw string IDs (e.g., `ll_evt_example_opt_text`) instead of actual text
**Solution:**
1. Run `python tools/events/sync_event_strings.py` to automatically extract missing strings from JSON event files and append them to `enlisted_strings.xml`
2. The script extracts all string IDs (`titleId`, `setupId`, `textId`, `resultTextId`, `resultFailureTextId`) and their fallback texts from JSON, properly escaping special characters (`&#xA;` for newlines, `&apos;` for apostrophes, `&quot;` for quotes)
3. All existing events now have complete localization (504 strings added Dec 2025 covering escalation thresholds, training events, and general content)
4. **Validation:** Run `python tools/events/validate_content.py` before committing to catch missing fallback fields, invalid skill names, missing XP, and logic errors. This enforces the Critical JSON Rules above.

### 10. Missing SaveableTypeDefiner Registration
**Problem:** "Cannot Create Save" error when serializing custom types
**Solution:** Register new classes/enums in `EnlistedSaveDefiner`, add container definitions for `Dictionary<T1,T2>` or `List<T>` with custom types

### 11. Single-Line Statements Without Braces
**Problem:** Reduces readability and increases risk of logic errors when modifying code
**Solution:** Always use braces for `if`, `for`, `while`, `foreach` statements, even for single lines

### 12. Redundant Namespace Qualifiers
**Problem:** Makes code verbose and harder to read (e.g., `TaleWorlds.CampaignSystem.Hero` when `using TaleWorlds.CampaignSystem;` exists)
**Solution:** Add proper `using` statements and remove full namespace paths in code

### 13. Unused Code
**Problem:** Clutters codebase, confuses developers, increases maintenance burden
**Solution:** Remove unused using directives, variables, parameters, methods, and property accessors

### 14. Redundant Default Parameters
**Problem:** Passing default parameter values explicitly is redundant and verbose
**Solution:** Omit parameters that match the method's default value (e.g., `Finish()` instead of `Finish(true)` if `true` is the default)

### 15. Missing XP Rewards in Order Events
**Problem:** Order events only grant reputation without skill XP, causing "0 XP in muster reports" complaints
**Solution:**
1. **All order event options must grant XP** - Use `effects.skillXp` (not `rewards.skillXp`)
2. Match XP to activity type:
   - Guard/Sentry duty → Athletics (10-20), Tactics (12-18)
   - Patrol → Scouting (15-24), Athletics (10-18)
   - Medical → Medicine (12-32)
   - Equipment → Crafting (10-25)
   - Leadership → Leadership (14-30), Tactics (18-32)
3. Failed skill checks should still grant reduced XP (50% of success)
4. **Validation:** Run `python tools/events/validate_content.py` - warns if order events lack XP rewards
5. XP flow: `Hero.AddSkillXp()` (skill progression) + `EnlistmentBehavior.AddEnlistmentXP()` (rank progression)