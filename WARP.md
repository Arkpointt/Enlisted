# WARP.md

Guidance for Warp AI agents working in this Bannerlord mod codebase.

## 🚨 Critical Rules (Read First)

1. **Target Version:** Bannerlord **v1.3.13** — never assume APIs from later versions
2. **API Verification:** Use local decompile at `C:\Dev\Enlisted\Decompile\` (not online docs)
3. **New C# Files:** Must be manually added to `Enlisted.csproj` via `<Compile Include="..."/>`
4. **Tooltips:** Cannot be null — every event/decision option needs a tooltip (<80 chars)
5. **JSON Field Order:** Fallback fields (`title`, `setup`, `text`) must immediately follow their ID fields
6. **Code Quality:** Fix all ReSharper warnings; never suppress without documented reason

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

## ⚠️ Common Pitfalls

| Problem | Solution |
|---------|----------|
| Gold not showing in UI | Use `GiveGoldAction.ApplyBetweenCharacters()`, not `ChangeHeroGold()` |
| Crash iterating equipment | Use numeric loop to `NumEquipmentSetSlots`, not `Enum.GetValues()` |
| New file not compiling | Add to `Enlisted.csproj` `<Compile Include="..."/>` |
| API doesn't exist | Verify against local decompile, not online docs |
| Tooltips null | Every option must have a tooltip — validator catches this |
| JSON validation fails | Check field ordering (fallback immediately after ID) |
| Reputation/needs wrong | Use centralized managers (EscalationManager, CompanyNeedsManager) |

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
