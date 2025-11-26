# Bannerlord.Harmony Reference

## Relevance to Enlisted Mod: MEDIUM

This is Bannerlord's wrapper/integration layer for the Harmony library. It ensures Harmony works correctly with Bannerlord's specific runtime environment.

---

## What It Contains

- Harmony library integration
- Bannerlord-specific Harmony setup
- Compatibility patches
- Runtime integration

---

## Relationship to 0Harmony

```
0Harmony (base library)
    └── Bannerlord.Harmony (integration layer)
            └── Our Mod (uses Harmony for patching)
```

Bannerlord.Harmony ensures:
- Correct Harmony initialization
- Compatible with Bannerlord's .NET runtime
- Proper cleanup on unload

---

## Why We Don't Directly Use It

We use the standard Harmony API:

```csharp
var harmony = new Harmony("com.enlisted.mod");
harmony.PatchAll();
```

Bannerlord.Harmony works behind the scenes to make this work correctly.

---

## Troubleshooting

If Harmony patches fail:
1. Check Bannerlord.Harmony is loaded
2. Verify correct Harmony version
3. Check for conflicting mods

---

## Files to Study

Only if debugging Harmony issues:
- How Bannerlord initializes Harmony
- Version compatibility handling
- Patch application order

---

## Skip for Normal Development

This is infrastructure code. Use the standard Harmony API (0Harmony reference) for patch development.

