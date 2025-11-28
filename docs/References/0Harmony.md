# 0Harmony Reference

## Relevance to Enlisted Mod: CRITICAL

Harmony is the patching library that allows us to modify game behavior without changing the game's files. Every Harmony patch in our mod depends on this library. Understanding Harmony is essential for extending or debugging our patches.

---

## What is Harmony?

Harmony is a library for patching .NET methods at runtime. It allows you to:
- Run code before a method executes (Prefix)
- Run code after a method executes (Postfix)
- Completely replace a method (Transpiler)
- Modify return values
- Skip original method execution

---

## Key Concepts

### Patch Types

| Type | Purpose | When to Use |
|------|---------|-------------|
| `Prefix` | Runs before original method | Block execution, modify parameters |
| `Postfix` | Runs after original method | Modify return value, add behavior |
| `Transpiler` | Modifies IL code directly | Complex modifications (advanced) |
| `Finalizer` | Runs even if method throws | Exception handling |

### Return Values

| Prefix Returns | Effect |
|----------------|--------|
| `void` | Always run original |
| `true` | Run original method |
| `false` | Skip original method |

### Special Parameters

| Parameter | Purpose |
|-----------|---------|
| `__instance` | The object the method is called on |
| `__result` | The return value (Postfix) or ref to modify (Prefix) |
| `__state` | Pass data from Prefix to Postfix |
| `___fieldName` | Access private field `fieldName` |

---

## Our Harmony Pattern

All our patches follow this structure:

```csharp
// Header comment explaining the patch
// Target: ClassName.MethodName
// Type: Prefix/Postfix
// Why: Reason for the patch
// Safety: When patch is safe to run
// Notes: Additional context

[HarmonyPatch(typeof(TargetClass), "MethodName")]
[HarmonyPriority(999)] // Optional: control patch order
public class DescriptivePatchName
{
    static bool Prefix(/* parameters */)
    {
        try
        {
            // Check if patch should apply
            if (!ShouldApplyPatch())
                return true; // Run original
            
            // Patch logic
            return false; // Skip original
        }
        catch (Exception ex)
        {
            ModLogger.Error("Patch", $"Error: {ex.Message}");
            return true; // On error, run original
        }
    }
}
```

---

## Harmony Initialization

We initialize Harmony in SubModule.cs:

```csharp
protected override void OnSubModuleLoad()
{
    _harmony = new Harmony("com.enlisted.mod");
    _harmony.PatchAll(typeof(SubModule).Assembly);
}

protected override void OnSubModuleUnloaded()
{
    _harmony?.UnpatchAll("com.enlisted.mod");
}
```

---

## Finding Target Methods

### By Name
```csharp
[HarmonyPatch(typeof(MobileParty), "IsVisible", MethodType.Setter)]
```

### By TargetMethod
```csharp
[HarmonyPatch]
public class MyPatch
{
    static MethodBase TargetMethod()
    {
        return typeof(SomeClass).GetMethod("SomeMethod", 
            BindingFlags.Instance | BindingFlags.NonPublic);
    }
}
```

### For Properties
```csharp
// Getter
[HarmonyPatch(typeof(MobileParty), "EffectiveEngineer", MethodType.Getter)]

// Setter
[HarmonyPatch(typeof(MobileParty), "IsActive", MethodType.Setter)]
```

---

## Common Patch Patterns

### Block Method Execution
```csharp
static bool Prefix()
{
    if (ShouldBlock())
        return false; // Skip original
    return true; // Run original
}
```

### Modify Return Value
```csharp
static void Postfix(ref bool __result)
{
    if (ShouldModify())
        __result = false; // Change the return value
}
```

### Access Instance Fields
```csharp
static void Postfix(MyClass __instance, ref int ___privateField)
{
    // __instance is the object
    // ___privateField accesses private field "privateField"
}
```

### Pass State Between Prefix and Postfix
```csharp
static void Prefix(out bool __state)
{
    __state = SomeCondition();
}

static void Postfix(bool __state)
{
    if (__state)
    {
        // Use the state from prefix
    }
}
```

---

## Priority System

When multiple mods patch the same method, priority controls order:

| Priority | Meaning |
|----------|---------|
| `Priority.First` (0) | Run first |
| `Priority.High` (100) | Run early |
| `Priority.Normal` (400) | Default |
| `Priority.Low` (600) | Run late |
| `Priority.Last` (800) | Run last |

We use `[HarmonyPriority(999)]` to run before other mods.

---

## Debugging Patches

### Check if Patch Applied
```csharp
var patches = Harmony.GetPatchInfo(targetMethod);
if (patches != null)
{
    foreach (var patch in patches.Prefixes)
        Debug.Log($"Prefix: {patch.owner}");
}
```

### Enable Harmony Debug Logging
```csharp
Harmony.DEBUG = true; // Logs to Harmony.log.txt
```

---

## Our Active Patches

| Patch | Target | Purpose |
|-------|--------|---------|
| `VisibilityEnforcementPatch` | `MobileParty.IsVisible` setter | Keep player invisible when enlisted |
| `EncounterSuppressionPatch` | `EncounterManager.StartPartyEncounter` | Block unwanted encounters |
| `DischargePenaltySuppressionPatch` | `ChangeRelationAction.ApplyRelationChangeBetweenHeroes` | Suppress discharge penalties |
| `HidePartyNamePlatePatch` | `PartyNameplateVM.DetermineIsVisibleOnMap` | Hide player nameplate |
| `DutiesOfficerRolePatches` | `MobileParty.EffectiveEngineer/Scout/etc` | Assign player as party officer |
| `VotingSuppressionPatch` | `GameMenu.ActivateGameMenu` | Block kingdom decision menus |
| `LootRestrictionPatch` | `PlayerEncounter.DoLootParty` | Restrict loot for enlisted |

---

## Files to Study

| File | Why |
|------|-----|
| `HarmonyLib/Harmony.cs` | Main Harmony class |
| `HarmonyLib/HarmonyMethod.cs` | Patch method configuration |
| `HarmonyLib/PatchProcessor.cs` | How patches are applied |
| `HarmonyLib/Priority.cs` | Priority constants |

---

## Known Issues / Gotchas

1. **Method Signature Changes**: Game updates can change method signatures, breaking patches
2. **Private Members**: Use `___fieldName` syntax to access privates
3. **Virtual Methods**: May need to patch base class instead of derived
4. **Generic Methods**: Require special handling with MakeGenericMethod
5. **Inlined Methods**: Very short methods may be inlined, making them unpatchable

