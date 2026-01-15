# Enlisted - Bannerlord v1.3.13 Mod

**Summary:** C# mod transforming Bannerlord into a soldier career simulator. Player enlists with a lord, follows orders, earns wages, progresses through 9 ranks. 245+ narrative content pieces, data-driven via JSON + XML.

---

## Quick Commands

```bash
# Build
dotnet build -c "Enlisted RETAIL" /p:Platform=x64

# Validate (ALWAYS before commit)
python Tools/Validation/validate_content.py

# Sync localization strings
python Tools/Validation/sync_event_strings.py

# Upload to Steam Workshop
.\Tools\Steam\upload.ps1
```

---

## Critical Rules (Will Break Mod)

### 1. Target Bannerlord v1.3.13
- NEVER assume APIs from later versions
- ALWAYS verify against local `Decompile/` directory (NOT online docs)

### 2. New C# Files Must Be Registered
```xml
<!-- Add to Enlisted.csproj manually -->
<Compile Include="src\Features\MyFeature\MyNewClass.cs"/>
```

### 3. Gold Transactions
```csharp
// CORRECT - visible in UI
GiveGoldAction.ApplyBetweenCharacters(null, Hero.MainHero, amount);

// WRONG - not visible
Hero.MainHero.ChangeHeroGold(amount);
```

### 4. Equipment Iteration
```csharp
// CORRECT - numeric loop
for (int i = 0; i < (int)EquipmentIndex.NumEquipmentSetSlots; i++)

// WRONG - crashes (includes count values)
foreach (EquipmentIndex slot in Enum.GetValues(typeof(EquipmentIndex)))
```

### 5. Hero Safety
```csharp
// CORRECT - null-safe
var hero = CampaignSafetyGuard.SafeMainHero;
if (hero == null) return;

// Check IsAlive before tracking
if (hero.IsAlive) VisualTrackerManager.RegisterObject(hero);
```

### 6. JSON Field Order
Fallback MUST immediately follow ID:
```json
{ "titleId": "key", "title": "Fallback", "setupId": "key2", "setup": "Text" }
```

### 7. Event Tooltips Required
- All options need tooltips (<80 chars)
- Format: action + effects + cooldown

### 8. Save System Registration
```csharp
// In EnlistedSaveDefiner - missing = "Cannot Create Save" error
DefineEnumType(typeof(MyNewEnum));
DefineClassType(typeof(MyNewClass));
```

---

## Key Documentation

| Topic | File |
|-------|------|
| Architecture & Standards | [docs/BLUEPRINT.md](docs/BLUEPRINT.md) |
| Documentation Index | [docs/INDEX.md](docs/INDEX.md) |
| C# Patterns | [src/WARP.md](src/WARP.md) |
| JSON Content Rules | [ModuleData/WARP.md](ModuleData/WARP.md) |
| Writing Style | [docs/Features/Content/writing-style-guide.md](docs/Features/Content/writing-style-guide.md) |
| Technical Reference | [Tools/TECHNICAL-REFERENCE.md](Tools/TECHNICAL-REFERENCE.md) |

---

## Project Structure

```
src/Features/        C# gameplay features
ModuleData/Enlisted/ JSON events, orders, decisions
ModuleData/Languages/enlisted_strings.xml  Localization
docs/                All documentation
Tools/Validation/    Validators
Decompile/           Bannerlord v1.3.13 API (AUTHORITATIVE)
```

### Key Feature Folders
- `Enlistment/` - Service state, retirement
- `Orders/` - Mission directives
- `Content/` - Events, decisions, narrative
- `Escalation/` - Reputation, scrutiny/discipline
- `Company/` - Readiness, supply needs
- `Equipment/` - Quartermaster, gear

---

## Code Standards

- Braces required on all control statements
- Use `ModLogger.Log()` with error codes: `E-SYSTEM-###`
- Localized strings: `new TextObject("{=id}Fallback")`
- Private fields: `_camelCase`
- Comments describe current behavior (not changelog)

### Safe Patterns
```csharp
// Deferred menu activation
NextFrameDispatcher.RunNextFrame(() => GameMenu.ActivateGameMenu("menu_id"));

// Item comparison by StringId
if (element.Item.StringId == targetItem.StringId)

// Settlement safety check
if (!PlayerEncounter.InsideSettlement) PlayerEncounter.Finish();

// Centralized managers
EscalationManager.Instance.ModifyReputation(ReputationType.Soldier, 5, "reason");
```

---

## Validation Checklist

Before committing:
- [ ] APIs verified against `Decompile/`
- [ ] New C# files in `Enlisted.csproj`
- [ ] JSON field order correct
- [ ] Tooltips on all event options
- [ ] `python Tools/Validation/validate_content.py` passes
- [ ] Build succeeds

---

## Context7 Libraries

For third-party library docs, use Context7 MCP:

| Library | ID |
|---------|-----|
| Harmony | `/pardeike/harmony` |
| Newtonsoft.Json | `/jamesnk/newtonsoft.json` |
| C# Language | `/websites/learn_microsoft_en-us_dotnet_csharp` |
| Pydantic AI | `/pydantic/pydantic-ai` |

TaleWorlds APIs: Use local `Decompile/` instead.

---

## Common Pitfalls

1. Using `ChangeHeroGold` instead of `GiveGoldAction`
2. Equipment iteration with `Enum.GetValues`
3. Not checking `Hero.IsAlive` before tracking
4. Finishing `PlayerEncounter` while in settlement
5. Not adding new files to `.csproj`
6. Missing tooltips in events
7. Wrong JSON field order
8. Not persisting in-progress flags in `SyncData()`
9. Missing `SaveableTypeDefiner` registration
10. Relying on external API docs (wrong version)

See [docs/BLUEPRINT.md](docs/BLUEPRINT.md) for complete pitfalls list with solutions.

---

## Deprecated Systems

- **Morale System** - Removed 2026-01-11, save loading only
- **Company Rest** - Removed 2026-01-11, save loading only
- Player Fatigue (0-24 budget) remains functional

---

## External Resources

- **Steam Workshop:** https://steamcommunity.com/sharedfiles/filedetails/?id=3621116083
- **Requires:** Harmony for Bannerlord

---

**Remember:** When in doubt, check `Decompile/`. Never hallucinate APIs.
