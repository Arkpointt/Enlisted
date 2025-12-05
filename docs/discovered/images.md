# Image System API Research - Updated for v1.3.4

## Index

- [Key Findings from Decompiled v1.3.4](#key-findings-from-decompiled-v134)
  - [ImageIdentifier Structure](#imageidentifier-structure-taleworldscore---v134)
  - [ItemImageIdentifier](#itemimageidentifier-concrete-implementation-for-items)
  - [ImageIdentifierVM Structure](#imageidentifiervm-structure-v134)
  - [ItemImageIdentifierVM](#itemimageidentifiervm-concrete-implementation-for-items)
  - [Correct Modern Usage Pattern](#correct-modern-usage-pattern-v134)
  - [Template Pattern](#template-pattern)
  - [Equipment Image Loading Process](#equipment-image-loading-process-v134)
  - [Key Properties Exposed to Templates](#key-properties-exposed-to-templates-v134)
- [Implementation Status](#implementation-status-)
- [Breaking Changes from Previous Versions](#breaking-changes-from-previous-versions)

---

## **Key Findings from Decompiled v1.3.4**

### **ImageIdentifier Structure** (TaleWorlds.Core - v1.3.4)
```csharp
// From TaleWorlds.Core\ImageIdentifiers\ImageIdentifier.cs
// NOTE: ImageIdentifier is now ABSTRACT in v1.3.4
public abstract class ImageIdentifier
{
    public string Id { get; set; }
    public string TextureProviderName { get; protected set; }
    public string AdditionalArgs { get; protected set; }
    
    // No direct constructor - use concrete implementations
}
```

### **ItemImageIdentifier** (Concrete Implementation for Items)
```csharp
// From TaleWorlds.Core\ImageIdentifiers\ItemImageIdentifier.cs
public class ItemImageIdentifier : ImageIdentifier
{
    // CRITICAL: Constructor for ItemObject (what we need)
    public ItemImageIdentifier(ItemObject item, string bannerCode = "")
    {
        this.Id = item?.StringId ?? "";
        this.AdditionalArgs = bannerCode;
        this.TextureProviderName = "ItemImageTextureProvider";
    }
}
```

### **ImageIdentifierVM Structure** (v1.3.4)
```csharp
// From TaleWorlds.Core.ViewModelCollection\ImageIdentifiers\ImageIdentifierVM.cs
// NOTE: ImageIdentifierVM is now ABSTRACT in v1.3.4
public abstract class ImageIdentifierVM : ViewModel
{
    protected ImageIdentifier ImageIdentifier { get; set; }
    
    [DataSourceProperty]
    public string Id { get; set; }
    
    [DataSourceProperty]
    public string AdditionalArgs { get; set; }
    
    [DataSourceProperty]
    public string TextureProviderName { get; set; }
    
    [DataSourceProperty]
    public bool IsEmpty { get; }
    
    [DataSourceProperty]
    public bool IsValid { get; }
}
```

### **ItemImageIdentifierVM** (Concrete Implementation for Items)
```csharp
// From TaleWorlds.Core.ViewModelCollection\ImageIdentifiers\ItemImageIdentifierVM.cs
public class ItemImageIdentifierVM : ImageIdentifierVM
{
    private readonly ItemObject _itemObject;
    private readonly string _bannerCode;
    
    // CRITICAL: Constructor for ItemObject (equipment images)
    public ItemImageIdentifierVM(ItemObject itemObject, string bannerCode = "")
    {
        this._itemObject = itemObject;
        this._bannerCode = bannerCode;
        this.ImageIdentifier = new ItemImageIdentifier(this._itemObject, this._bannerCode);
    }
    
    public ItemImageIdentifierVM Clone()
    {
        return new ItemImageIdentifierVM(this._itemObject, this._bannerCode);
    }
}
```

### **Correct Modern Usage Pattern (v1.3.4)**

✅ **ViewModel Pattern**:
```csharp
[DataSourceProperty]
public ItemImageIdentifierVM Image { get; private set; }

// For ItemObject (equipment images):
Image = new ItemImageIdentifierVM(itemObject, ""); // ✅ VERIFIED v1.3.4 API

// For empty slots (use null or create empty ItemImageIdentifier):
Image = new ItemImageIdentifierVM(null, ""); // ✅ VERIFIED fallback
```

✅ **Template Pattern**:
```xml
<ImageIdentifierWidget DataSource="{Image}" 
                       AdditionalArgs="@AdditionalArgs" 
                       ImageId="@Id" 
                       TextureProviderName="@TextureProviderName" 
                       LoadingIconWidget="LoadingIconWidget">
  <Children>
    <Standard.CircleLoadingWidget Id="LoadingIconWidget" />
  </Children>
</ImageIdentifierWidget>
```

### **Equipment Image Loading Process (v1.3.4)**

1. **ItemObject** → **ItemImageIdentifier** (creates `Id` from `itemObject.StringId`, sets `TextureProviderName = "ItemImageTextureProvider"`)
2. **ItemImageIdentifier** → **ItemImageIdentifierVM** (ViewModel wrapper for data binding)
3. **ItemImageIdentifierVM** → **ImageIdentifierWidget** (renders actual item image in UI)
4. **LoadingIconWidget** → Shows loading spinner until image loads

### **Key Properties Exposed to Templates (v1.3.4)**

- **`@Id`**: Item's StringId (e.g., "iron_sword_t2")
- **`@TextureProviderName`**: "ItemImageTextureProvider" for equipment
- **`@AdditionalArgs`**: Banner code or additional visual info
- **`@IsEmpty`**: Whether the image identifier is empty
- **`@IsValid`**: Whether the image identifier is valid
- **Loading support**: Built-in loading spinner functionality

## **Implementation Status** ✅

The **v1.3.4 API** is fully implemented:
- ✅ Correct class usage (`ItemImageIdentifier` and `ItemImageIdentifierVM`)
- ✅ Correct constructor usage (`new ItemImageIdentifierVM(item, "")`)
- ✅ Proper template binding (`DataSource="{Image}"`)
- ✅ All v1.3.4 API compatibility verified

**Equipment images should now load correctly** using the official v1.3.4 Bannerlord image system.

## **Breaking Changes from Previous Versions**

- ❌ **OLD**: `ImageIdentifier` was a concrete class
- ✅ **NEW**: `ImageIdentifier` is now abstract, use `ItemImageIdentifier` for items
- ❌ **OLD**: `ImageIdentifierVM` was a concrete class  
- ✅ **NEW**: `ImageIdentifierVM` is now abstract, use `ItemImageIdentifierVM` for items
- ❌ **OLD**: `ImageTypeCode` property existed
- ✅ **NEW**: Use `TextureProviderName` property instead (e.g., "ItemImageTextureProvider")
