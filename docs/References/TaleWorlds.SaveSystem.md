# TaleWorlds.SaveSystem Reference

## Relevance to Enlisted Mod: MEDIUM

The SaveSystem handles game save/load operations. Understanding it helps ensure our enlistment state persists correctly across saves and handles version compatibility.

---

## Key Classes and Systems

### Core Save Classes

| Class | Purpose | Enlisted Usage |
|-------|---------|----------------|
| `SaveManager` | Manages save operations | Save/load lifecycle |
| `MetaData` | Save file metadata | Version info |
| `SaveableTypeDefiner` | Registers saveable types | Register custom types |
| `IDataStore` | Data storage interface | Our SyncData implementation |

### Type Registration

| Class | Purpose | Enlisted Usage |
|-------|---------|----------------|
| `SaveableTypeDefiner` | Define saveable types | Register enlistment data types |
| `DefineContainerDefinitions` | Define containers | Register collections |

---

## How We Use Save System

Our enlistment data is saved through `CampaignBehaviorBase.SyncData`:

```csharp
public override void SyncData(IDataStore dataStore)
{
    // Save/load enlistment state
    dataStore.SyncData("_enlistedLord", ref _enlistedLord);
    dataStore.SyncData("_enlistmentTier", ref _enlistmentTier);
    dataStore.SyncData("_enlistmentXP", ref _enlistmentXP);
    dataStore.SyncData("_daysServed", ref _daysServed);
    dataStore.SyncData("_enlistmentTime", ref _enlistmentTime);
    dataStore.SyncData("_isOnLeave", ref _isOnLeave);
    // ... more fields
}
```

---

## SaveableTypeDefiner

If we had custom classes to save, we'd register them:

```csharp
public class SaveableEnlistedTypeDefiner : SaveableTypeDefiner
{
    public SaveableEnlistedTypeDefiner() : base(/* unique ID */) { }

    protected override void DefineClassTypes()
    {
        AddClassDefinition(typeof(CustomEnlistmentData), 1);
    }

    protected override void DefineContainerDefinitions()
    {
        ConstructContainerDefinition(typeof(List<CustomEnlistmentData>));
    }
}
```

---

## IDataStore Methods

| Method | Purpose |
|--------|---------|
| `SyncData<T>(string key, ref T value)` | Save/load a value |
| `SyncDataAsEmpty(string key)` | Mark field as intentionally empty |

---

## Save Compatibility

When game updates change save format:
- Old saves may not load properly
- New fields need default values
- Removed fields cause warnings

Our approach:
```csharp
public override void SyncData(IDataStore dataStore)
{
    // Old field with default
    dataStore.SyncData("_enlistedLord", ref _enlistedLord);
    
    // Initialize if null (new save or old save without this field)
    if (_enlistedLord == null && IsEnlisted)
    {
        // Handle gracefully
    }
}
```

---

## Files to Study

| File | Why |
|------|-----|
| `IDataStore.cs` | Interface we use |
| `SaveableTypeDefiner.cs` | Type registration |
| `MetaData.cs` | Save metadata |

---

## Key Insight

The save system automatically handles:
- Hero references (converted to IDs)
- CampaignTime values
- Basic types (int, float, bool, string)
- Collections of basic types

We just need to call `SyncData` with the right key and reference.

