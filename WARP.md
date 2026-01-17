# WARP.md - Enlisted Project Routing

Guidance for Warp AI agents working in this Bannerlord mod codebase.

## ⏰ Current Context

* **Date:** January 2026 (verify via system if uncertain)
* **Target:** Bannerlord **v1.3.13**
* **Do NOT** assume APIs/patterns from earlier versions or online docs

## 🧠 REQUIRED: Read Before Answering

**ALWAYS read these files before answering ANY question about this project:**

1. `docs/BLUEPRINT.md` — Architecture, coding standards, common pitfalls, quick commands
2. `docs/INDEX.md` — Navigate to the correct documentation for any feature/system
3. For content/events/orders: `docs/Features/Content/content-index.md`
4. For APIs: Verify against local `Decompile/` (auto-detected, see PROJECT-RESOURCES.md)

**Do NOT hallucinate features.** If you're unsure whether something exists, search the codebase or ask.

## 🚨 TOP 5 Critical Rules

1. **Target Version:** Bannerlord **v1.3.13** — verify APIs against local `Decompile/`, never online docs
2. **New C# Files:** Must be manually added to `Enlisted.csproj` via `<Compile Include="..."/>`
3. **Tooltips Required:** Every event/decision option needs a tooltip (<80 chars) — validator will fail otherwise
4. **JSON Field Order:** Fallback fields (`title`, `setup`, `text`) must immediately follow their ID fields
5. **Context-Aware Rules:** When editing C# code, see `src/WARP.md`; for JSON content, see `ModuleData/WARP.md`

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
| `src/Features/` | All gameplay code (see `src/WARP.md` for C# rules) |
| `ModuleData/Enlisted/` | JSON config, events, orders, decisions (see `ModuleData/WARP.md` for content rules) |
| `ModuleData/Languages/enlisted_strings.xml` | Localized strings |
| `Tools/Validation/` | Content validators |
| `docs/` | All documentation |
| `../Decompile/` | Native Bannerlord API reference (v1.3.13) |
| `<BannerlordInstall>/Modules/Enlisted/Debugging/` | Runtime mod logs |

## 🧭 Task Routing

| Working on... | Read First |
|---------------|------------|
| C# code (any) | `src/WARP.md` for critical patterns |
| JSON content (events, orders) | `ModuleData/WARP.md` for field ordering, tooltips |

| Battle AI | `docs/Features/Combat/BATTLE-AI-IMPLEMENTATION-SPEC.md` |

## 📚 Documentation Quick Reference

| Need to... | Read |
|------------|------|
| Understand project architecture | [docs/BLUEPRINT.md](docs/BLUEPRINT.md) |
| Find documentation for a feature | [docs/INDEX.md](docs/INDEX.md) |
| Common pitfalls & code patterns | [docs/BLUEPRINT.md](docs/BLUEPRINT.md) (comprehensive list) |
| Write events/decisions/orders | [docs/Features/Content/writing-style-guide.md](docs/Features/Content/writing-style-guide.md) |
| Check JSON schemas | [docs/Features/Content/event-system-schemas.md](docs/Features/Content/event-system-schemas.md) |
|| Find all content (events, orders) | [docs/Features/Content/content-index.md](docs/Features/Content/content-index.md) |

## 🔗 Context7 Libraries

For up-to-date third-party library docs, use Context7 MCP with these IDs:

| Library | Context7 ID |
|---------|-------------|
| Harmony | `/pardeike/harmony` |
| Newtonsoft.Json | `/jamesnk/newtonsoft.json` |
| C# Language | `/websites/learn_microsoft_en-us_dotnet_csharp` |
| Pydantic AI | `/pydantic/pydantic-ai` |

**Note:** TaleWorlds/Bannerlord APIs are NOT in Context7 — use local `Decompile/` instead.

## 🎯 Project Overview

**Enlisted** is a C# Bannerlord mod that transforms the game into a soldier career simulator:
* Player enlists with a lord, follows orders, earns wages
* 9-tier rank progression (T1 recruit → T9 commander)
* 245 narrative content pieces (events, decisions, orders)
* JSON-driven content with XML localization
* Old-style `.csproj` with explicit file includes
