# WARP.md

Guidance for Warp AI agents working in this Bannerlord mod codebase.

> **How this file works:** Warp automatically applies these rules to all agent interactions.
> Subdirectory WARP.md files (like `Tools/CrewAI/WARP.md`) take precedence for that area.
> See `Tools/AGENT-WORKFLOW.md` for multi-agent workflow details and CrewAI integration.

## 🚨 Critical Rules (Read First)

1. **Target Version:** Bannerlord **v1.3.13** — never assume APIs from later versions
2. **API Verification:** Use local decompile at `C:\Dev\Enlisted\Decompile\` (not online docs)
3. **New C# Files:** Must be manually added to `Enlisted.csproj` via `<Compile Include="..."/>`
4. **Tooltips:** Cannot be null — every event/decision option needs a tooltip (<80 chars)
5. **JSON Field Order:** Fallback fields (`title`, `setup`, `text`) must immediately follow their ID fields
6. **Code Quality:** Fix all ReSharper warnings; never suppress without documented reason
7. **CrewAI for Complex Work:** Use `"Use CrewAI [crew_name] to [task]"` for multi-file features, bug hunting, planning docs

## ⚡ Quick Commands

```powershell path=null start=null
# Build
dotnet build -c "Enlisted RETAIL" /p:Platform=x64

# Validate content (run before committing)
python Tools/Validation/validate_content.py

# Sync localization strings
python Tools/Validation/sync_event_strings.py

# Upload to Steam Workshop (interactive window required)
.\Tools\Steam\upload.ps1
```

## 📁 Key Paths

| Path | Purpose |
|------|---------|
| `src/Features/` | All gameplay code (Enlistment, Orders, Content, Combat, Equipment, etc.) |
| `ModuleData/Enlisted/` | JSON config, events, orders, decisions |
| `ModuleData/Languages/enlisted_strings.xml` | Localized strings |
| `Tools/Validation/` | Content validators |
| `docs/` | All documentation |
| `C:\Dev\Enlisted\Decompile\` | Native Bannerlord API reference (v1.3.13) |
| `<BannerlordInstall>\Modules\Enlisted\Debugging\` | Runtime mod logs |

## 📚 Documentation Quick Reference

| Need to... | Read |
|------------|------|
| Understand project architecture | [docs/BLUEPRINT.md](docs/BLUEPRINT.md) |
| Find documentation for a feature | [docs/INDEX.md](docs/INDEX.md) |
| Understand core gameplay systems | [docs/Features/Core/core-gameplay.md](docs/Features/Core/core-gameplay.md) |
| Write events/decisions/orders | [docs/Features/Content/writing-style-guide.md](docs/Features/Content/writing-style-guide.md) |
| Check JSON schemas | [docs/Features/Content/event-system-schemas.md](docs/Features/Content/event-system-schemas.md) |
| Find all content (events, orders) | [docs/Features/Content/content-index.md](docs/Features/Content/content-index.md) |
| Use validation tools | [Tools/README.md](Tools/README.md) |
| Technical patterns (logging, save) | [Tools/TECHNICAL-REFERENCE.md](Tools/TECHNICAL-REFERENCE.md) |

## 🔧 Multi-Agent Workflow

**See [Tools/AGENT-WORKFLOW.md](Tools/AGENT-WORKFLOW.md)** for complete workflow documentation.

**Single agent (default):** Describe your task naturally. Warp will analyze → implement → validate.

**Invoke specific phases:**
- `[ANALYZE]` — Read-only investigation
- `[ANALYZE:CODE]` — Force C# analysis
- `[ANALYZE:CONTENT]` — Force content/JSON analysis
- `[ANALYZE:VOICE]` — Narrative style review
- `[ANALYZE:BALANCE]` — Effects/economy review
- `[IMPLEMENT]` — Skip analysis, go to implementation
- `[VALIDATE]` — Run QA validation only

## 💡 Prompt Best Practices

Better prompts = better results + fewer wasted AI credits.

**Be specific:** Instead of vague requests, include:
- Exact error codes or log snippets
- File paths you suspect are involved
- What you've already tried
- Expected outcome format

**Planning workflow for new features:**
1. Discuss the idea with Warp (me) — I'll ask questions, probe edge cases
2. Once scope is clear, I craft a prompt for `planning_crew`
3. CrewAI produces a design doc in `ANEWFEATURE/` with `Status: 📋 Planning`
4. Review and iterate until approved
5. Implementation via Warp directly or `full_feature_crew`

| ❌ Vague | ✅ Specific |
|----------|------------|
| "Fix the bug" | "Investigate E-ENCOUNTER-042 crash when opening camp menu after battle" |
| "Add an event" | "Add a T3 barracks event in `Events/camp/` where sergeant offers training" |
| "Why doesn't this work?" | "OrderProgressTracker.RecordProgress() not persisting after save/load" |

**Attach relevant context:** Use `@filename` to attach files instead of making the agent search. Saves time and credits.

**Start fresh conversations:** Don't continue unrelated tasks in the same conversation—it confuses context and wastes tokens.

## 🤖 CrewAI - Three Flows

**For complex multi-agent work**, use CrewAI at `Tools/CrewAI/`.

All workflows are **Flow-based** with state persistence - if a run fails, re-running resumes from the last successful step.

```bash
# Activate first
cd Tools/CrewAI && .\.venv\Scripts\Activate.ps1

# Design a feature (PlanningFlow: research → advise → design → validate → auto-fix)
enlisted-crew plan -f "feature-name" -d "description"

# Find & fix bugs (BugHuntingFlow: investigate → route → analyze → fix → validate)
enlisted-crew hunt-bug -d "bug description" -e "E-XXX-*"

# Build from approved plan (ImplementationFlow: verify → route → implement → validate → docs)
# Smart: Detects partial implementations, routes around completed work
enlisted-crew implement -p "docs/CrewAI_Plans/feature.md"

# Quick pre-commit check
enlisted-crew validate

# Test flow performance (runs crew multiple times, provides metrics)
crewai test -n 3 -m gpt-5
```

**CrewAI writes files directly:** All flows apply changes to disk (C#, JSON, localization, .csproj). Review with `git diff` after running.

**Testing:** Use `crewai test -n 3 -m gpt-5` to validate crew performance across iterations. See `Tools/CrewAI/test_flows.ps1` for automated testing script.

**When to use CrewAI vs Warp directly:**
- Quick fixes, single-file changes → Warp directly
- Multi-file features, planning, bug hunting → CrewAI

**Setup:** See [Tools/CrewAI/CREWAI.md](Tools/CrewAI/CREWAI.md)  
**Requirements:** OpenAI API key in `.env` file  
**Models:** OpenAI GPT-5 family (GPT-5.2, GPT-5 mini, GPT-5 nano)  
**Memory:** Enabled with text-embedding-3-large for superior knowledge retrieval  
**State Persistence:** All flows resume on failure (`persist=True`)

## 📂 Project Structure

```
Enlisted/
├── src/
│   ├── Mod.Entry/          SubModule + Harmony init
│   ├── Mod.Core/           Logging, config, save system, helpers
│   ├── Mod.GameAdapters/   Harmony patches
│   └── Features/           All gameplay features
│       ├── Enlistment/     Core service state, XP, retirement
│       ├── Orders/         Mission-driven directives
│       ├── Content/        Events, Decisions, Orchestrator
│       ├── Combat/         Battle participation, formation
│       ├── Equipment/      Quartermaster, gear management
│       └── ...             (14 feature folders total)
├── ModuleData/
│   ├── Enlisted/           JSON config + content files
│   └── Languages/          XML localization
├── Tools/                  Validators, upload scripts
├── docs/                   All documentation
└── GUI/                    Gauntlet UI prefabs
```

## 🛠️ Common Tasks

### Add New C# File
1. Create file in appropriate `src/Features/` subfolder
2. Add to `Enlisted.csproj`: `<Compile Include="src\Features\MyFeature\MyClass.cs"/>`
3. Run `python Tools/Validation/validate_content.py` (catches missing files)
4. Build and fix warnings

### Add New Event/Decision/Order
1. Read [writing-style-guide.md](docs/Features/Content/writing-style-guide.md) for voice/tone
2. Add JSON definition to `ModuleData/Enlisted/Events/` or `Decisions/`
3. Follow field ordering: `titleId` → `title` → `setupId` → `setup`
4. Include tooltips for all options (<80 chars, factual)
5. Run validator: `python Tools/Validation/validate_content.py`
6. Sync strings: `python Tools/Validation/sync_event_strings.py`

### Before Committing
```powershell path=null start=null
python Tools/Validation/validate_content.py
dotnet build -c "Enlisted RETAIL" /p:Platform=x64
```

## ⚠️ Common Pitfalls (WILL BREAK THE MOD)

| Problem | Solution | Impact |
|---------|----------|--------|
| Gold not showing in UI | Use `GiveGoldAction.ApplyBetweenCharacters()`, not `ChangeHeroGold()` | Players lose money silently |
| Crash iterating equipment | Use numeric loop to `NumEquipmentSetSlots`, not `Enum.GetValues()` | **Crashes game** |
| New file not compiling | Add to `Enlisted.csproj` `<Compile Include="..."/>` | Code doesn't run, validator catches |
| "Cannot Create Save" crash | Register new types in `EnlistedSaveDefiner` | **Blocks saving** |
| Order events without XP | All order event options MUST have `effects.skillXp` | Validator error, breaks progression |
| In-progress flags not persisted | Persist ALL workflow flags in `SyncData()` (not just completed/scheduled) | Duplicate events on load |
| Encounter finished in settlement | Check `PlayerEncounter.InsideSettlement` before finishing | Menus disappear unexpectedly |
| Dead hero tracking | Check `Hero.IsAlive` before `VisualTrackerManager` calls | **Crashes game** |
| Item comparison by reference | Use `item.StringId` comparison, not `==` | Equipment confiscation fails |
| Hardcoded strings | Use `TextObject("{=key}Fallback")` + XML | Missing localization |
| API doesn't exist | Verify against local decompile at `C:\Dev\Enlisted\Decompile\` | Wrong API usage |
| Tooltips null | Every option must have a tooltip (<80 chars, factual) | Validator error |
| JSON validation fails | Fallback fields immediately after ID fields | Content won't load |
| Reputation/needs modified directly | Use `EscalationManager`, `CompanyNeedsManager` | Bypasses clamping/logging |

## 🔑 Critical Code Patterns

### Localization (TextObject)
```csharp
// ✅ CORRECT: Localized with fallback
new TextObject("{=my_string_id}Fallback text here")
// Add to ModuleData/Languages/enlisted_strings.xml:
// <string id="my_string_id" text="Localized text" />

// ❌ WRONG: Hardcoded string
new TextObject("Hardcoded text")
```

### Save System (New Types)
```csharp
// When adding new serializable class/enum:
// 1. Register in EnlistedSaveDefiner.DefineClassTypes() or DefineEnumTypes()
// 2. Persist ALL state flags in SyncData(), including in-progress flags:
SyncData(dataStore, "_eventScheduled", ref _scheduled);
SyncData(dataStore, "_eventCompleted", ref _completed);
SyncData(dataStore, "_eventInProgress", ref _inProgress);  // Don't forget!
```

### Item Comparison
```csharp
// ✅ CORRECT: StringId comparison
if (element.Item != null && element.Item.StringId == targetItem.StringId) { }

// ❌ WRONG: Reference equality (fails for equipped items)
if (element.Item == targetItem) { }
```

### Encounter Transitions
```csharp
// ✅ CORRECT: Deferred menu activation
NextFrameDispatcher.RunNextFrame(() => GameMenu.ActivateGameMenu("menu_id"));

// ❌ WRONG: Immediate activation during encounter
GameMenu.ActivateGameMenu("menu_id");
```

### Hero Safety
```csharp
// ✅ CORRECT: Null-safe during character creation
var hero = CampaignSafetyGuard.SafeMainHero;
if (hero == null) return;

// ❌ WRONG: Direct access
var hero = Hero.MainHero;  // Can be null during creation
```

### Hero Tracking
```csharp
// ✅ CORRECT: Check if alive before tracking
if (hero.IsAlive)
{
    VisualTrackerManager.RegisterObject(hero);
}

// ❌ WRONG: No alive check
VisualTrackerManager.RegisterObject(hero);  // Crashes if dead
```

### Gold Transactions
```csharp
// ✅ CORRECT: Updates party treasury (visible in UI)
GiveGoldAction.ApplyBetweenCharacters(null, Hero.MainHero, amount);  // Grant
GiveGoldAction.ApplyBetweenCharacters(Hero.MainHero, null, amount);  // Deduct

// ❌ WRONG: Modifies internal gold not visible in party UI
Hero.MainHero.ChangeHeroGold(amount);
```

### Equipment Iteration
```csharp
// ✅ CORRECT: Iterate valid equipment slots (0-11)
for (int i = 0; i < (int)EquipmentIndex.NumEquipmentSetSlots; i++)
{
    var slot = (EquipmentIndex)i;
    var element = equipment[slot];
    // ...
}

// ❌ WRONG: Includes invalid count enum values, causes crashes
foreach (EquipmentIndex slot in Enum.GetValues(typeof(EquipmentIndex))) { }
```

### Reputation/Needs Changes
```csharp
// ✅ CORRECT: Use centralized managers (handles clamping, logging)
EscalationManager.Instance.ModifyReputation(ReputationType.Soldier, 5, "reason");
CompanyNeedsManager.Instance.ModifyNeed(NeedType.Morale, -10, "reason");

// ❌ WRONG: Direct modification bypasses validation
_soldierReputation += 5;
```

## 🎯 Project Overview

**Enlisted** is a C# Bannerlord mod that transforms the game into a soldier career simulator:
- Player enlists with a lord, follows orders, earns wages
- 9-tier rank progression (T1 recruit → T9 commander)
- 245 narrative content pieces (events, decisions, orders)
- JSON-driven content with XML localization
- Old-style `.csproj` with explicit file includes

## 📋 Code Quality Checklist

- [ ] ReSharper warnings fixed (no suppressions without reason)
- [ ] Braces on all control statements
- [ ] No unused imports/variables/methods
- [ ] New files added to `.csproj`
- [ ] Tooltips present for all options
- [ ] JSON field ordering correct
- [ ] Build succeeds
- [ ] Validator passes
