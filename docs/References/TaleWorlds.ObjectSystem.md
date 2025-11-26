# TaleWorlds.ObjectSystem Reference

## Relevance to Enlisted Mod: LOW

The ObjectSystem handles game object registration, identification, and lookup. It's the foundation for finding game objects by ID.

---

## Key Classes

| Class | Purpose | Enlisted Usage |
|-------|---------|----------------|
| `MBObjectBase` | Base class for game objects | Most game objects inherit this |
| `MBObjectManager` | Object registration/lookup | Find objects by ID |

---

## Object Lookup Pattern

```csharp
// Find item by ID
var item = MBObjectManager.Instance.GetObject<ItemObject>("item_id");

// Find culture by ID
var culture = MBObjectManager.Instance.GetObject<CultureObject>("vlandia");

// Find character template
var troop = MBObjectManager.Instance.GetObject<CharacterObject>("troop_id");
```

---

## When We Use It

For equipment kits, we look up items by ID:

```csharp
var helmet = MBObjectManager.Instance.GetObject<ItemObject>("empire_helmet");
if (helmet != null)
{
    // Use the item
}
```

---

## Object IDs

Every game object has a `StringId`:
- Items: `"sword_steel"`, `"leather_armor"`
- Cultures: `"vlandia"`, `"empire"`, `"battania"`
- Characters: `"aserai_infantry"`, `"vlandian_knight"`

These IDs are defined in XML files and can be looked up.

---

## Files to Study

| File | Why |
|------|-----|
| `MBObjectManager.cs` | Object lookup |
| `MBObjectBase.cs` | Base object class |

---

## Practical Use in Enlisted

When creating culture-specific equipment kits:

```csharp
string cultureId = lord.Culture.StringId;
string itemId = $"{cultureId}_infantry_helmet";
var helmet = MBObjectManager.Instance.GetObject<ItemObject>(itemId);
```

