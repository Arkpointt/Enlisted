# Enlisted - Bannerlord v1.3.13 Mod

## Working Agreements

- Target Bannerlord **v1.3.13** - NEVER assume APIs from later versions
- Verify ALL Bannerlord APIs against local `Decompile/` directory (NOT online docs)
- Run `python Tools/Validation/validate_content.py` before committing
- Run `dotnet build -c "Enlisted RETAIL" /p:Platform=x64` to build
- Ask before adding new production dependencies

## Repository Expectations

- New C# files MUST be manually added to `Enlisted.csproj`
- JSON field order: fallback field MUST immediately follow ID field
- All event options MUST have tooltips (<80 chars)
- Use `ModLogger.Log()` with error codes: `E-SYSTEM-###`
- Braces required on all control statements

## Critical Patterns (Will Break Mod if Violated)

```csharp
// Gold transactions - use GiveGoldAction, NOT ChangeHeroGold
GiveGoldAction.ApplyBetweenCharacters(null, Hero.MainHero, amount);

// Equipment iteration - use numeric loop, NOT Enum.GetValues
for (int i = 0; i < (int)EquipmentIndex.NumEquipmentSetSlots; i++)

// Hero access - use null-safe guard
var hero = CampaignSafetyGuard.SafeMainHero;
if (hero == null) return;

// Always check IsAlive before tracking
if (hero.IsAlive) VisualTrackerManager.RegisterObject(hero);
```

## Key Documentation

Read these before making changes:

- `@docs/BLUEPRINT.md` - Architecture, standards, common pitfalls
- `@docs/INDEX.md` - Master documentation catalog
- `@src/WARP.md` - Critical C# patterns
- `@ModuleData/WARP.md` - JSON content rules
- `@docs/Features/Content/writing-style-guide.md` - Voice, tone for content

## Project Structure

```
src/Features/     - All gameplay features (C#)
ModuleData/       - JSON config, events, orders, decisions
docs/             - All documentation
Tools/Validation/ - Validators (run before commit)
Decompile/        - Bannerlord v1.3.13 API reference (AUTHORITATIVE)
```

## Commands

```bash
# Build
dotnet build -c "Enlisted RETAIL" /p:Platform=x64

# Validate (ALWAYS before commit)
python Tools/Validation/validate_content.py

# Sync localization
python Tools/Validation/sync_event_strings.py
```

## Pre-Commit Checklist

- [ ] APIs verified against `Decompile/` (v1.3.13)
- [ ] New C# files added to `Enlisted.csproj`
- [ ] JSON field order correct (fallback after ID)
- [ ] Tooltips on all event options
- [ ] Validation passes
- [ ] Build succeeds
