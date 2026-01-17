# C# Development Rules - Enlisted

This WARP.md applies when working in src/. Root WARP.md still applies.

## Critical Patterns (WILL BREAK THE MOD IF IGNORED)

### Gold Transactions

✅ `GiveGoldAction.ApplyBetweenCharacters(null, Hero.MainHero, amount)` — Grant gold
✅ `GiveGoldAction.ApplyBetweenCharacters(Hero.MainHero, null, amount)` — Deduct gold
❌ `Hero.MainHero.ChangeHeroGold(amount)` — NOT visible in UI

### Equipment Iteration

✅ **Correct:** Use numeric loop

```csharp
for (int i = 0; i < (int)EquipmentIndex.NumEquipmentSetSlots; i++)
{
    var slot = (EquipmentIndex)i;
    var element = equipment[slot];
    // ...
}
```

❌ **WRONG:** `foreach (EquipmentIndex slot in Enum.GetValues(...))` — CRASHES

### Item Comparison

✅ `element.Item.StringId == targetItem.StringId`
❌ `element.Item == targetItem` — Reference equality fails

### Save System

- Register new types in `EnlistedSaveDefiner`
- Persist ALL state flags in `SyncData()` including in-progress flags
- Use `SaveLoadDiagnostics.SafeSyncData()` wrapper

```csharp
// Persist ALL flags, including in-progress
SyncData(dataStore, "_eventScheduled", ref _scheduled);
SyncData(dataStore, "_eventCompleted", ref _completed);
SyncData(dataStore, "_eventInProgress", ref _inProgress);  // Don't forget!
```

### Deferred Operations

✅ `NextFrameDispatcher.RunNextFrame(() => GameMenu.ActivateGameMenu("menu_id"))`
❌ Immediate menu activation during encounters

### Safe Hero Access

✅ `CampaignSafetyGuard.SafeMainHero` — Null-safe
❌ `Hero.MainHero` — Can be null during character creation

### Hero Tracking

✅ **Always check if alive:**

```csharp
if (hero.IsAlive)
{
    VisualTrackerManager.RegisterObject(hero);
}
```

❌ No alive check — Crashes if dead

### Localization

✅ **Correct:** Localized with fallback

```csharp
new TextObject("{=my_string_id}Fallback text here")
// Add to ModuleData/Languages/enlisted_strings.xml:
// <string id="my_string_id" text="Localized text" />
```

❌ `new TextObject("Hardcoded text")` — Missing localization

### Reputation/Needs Changes

✅ Use centralized managers (handles clamping, logging)

```csharp
EscalationManager.Instance.ModifyReputation(ReputationType.Soldier, 5, "reason");
CompanyNeedsManager.Instance.ModifyNeed(NeedType.Morale, -10, "reason");
```

❌ Direct modification bypasses validation

## New File Checklist

1. Create file in `src/Features/[Category]/`
2. Add to `Enlisted.csproj`: `<Compile Include="src\..."/>`
3. Run: `python Tools/Validation/validate_content.py`
4. Build: `dotnet build -c "Enlisted RETAIL" /p:Platform=x64`

## Full Reference

See `docs/BLUEPRINT.md` for complete patterns and comprehensive pitfalls list.
