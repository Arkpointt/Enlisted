# TaleWorlds.PlayerServices API Research

## **Key Findings from Decompiled PlayerServices v1.2.12**

### **PlayerId Structure** (Critical for ImageIdentifierVM)
```csharp
// From TaleWorlds.PlayerServices\PlayerServices\PlayerId.cs
public struct PlayerId : IComparable<PlayerId>, IEquatable<PlayerId>
{
    public ulong Id1 { get; }
    public ulong Id2 { get; }
    public bool IsValid { get; }
    public PlayerIdProvidedTypes ProvidedType { get; }
    
    // Static property for empty player ID
    public static PlayerId Empty { get; }
    
    // Key constructors:
    public PlayerId(byte providedType, ulong id1, ulong id2)
    public PlayerId(Guid guid)
    public PlayerId(ulong part1, ulong part2, ulong part3, ulong part4)
}
```

### **ImageIdentifier Structure** (TaleWorlds.Core)
```csharp
// From TaleWorlds.Core\ImageIdentifier.cs
public class ImageIdentifier
{
    public ImageIdentifierType ImageTypeCode { get; private set; }
    public string AdditionalArgs { get; private set; }
    public string Id { get; private set; }
    
    // CRITICAL: Constructor for ItemObject (what we need)
    public ImageIdentifier(ItemObject itemObject, string bannerCode = "")
    {
        this.ImageTypeCode = ImageIdentifierType.Item;
        this.Id = itemObject.StringId;  // ✅ Key for equipment images
        this.AdditionalArgs = bannerCode;
    }
    
    // Other constructors for characters, crafting pieces, etc.
    public ImageIdentifier(CharacterCode characterCode)
    public ImageIdentifier(CraftingPiece craftingPiece, string pieceUsageId)
}
```

### **Why We Need TaleWorlds.PlayerServices Reference**

❌ **Without Reference**: `CS0012: The type 'PlayerId' is defined in an assembly that is not referenced`  
✅ **With Reference**: `ImageIdentifierVM` can access `PlayerId` for avatar/player-related functionality  

**Dependency Chain**: `ImageIdentifierVM` → `ImageIdentifier` → `PlayerId` (for certain image types)

### **Correct Modern Usage Pattern**

✅ **Assembly Reference**:
```xml
<Reference Include="TaleWorlds.PlayerServices">
  <HintPath>C:\Program Files (x86)\Steam\steamapps\common\Mount &amp; Blade II Bannerlord\bin\Win64_Shipping_wEditor\TaleWorlds.PlayerServices.dll</HintPath>
  <Private>False</Private>
</Reference>
```

✅ **ViewModel Pattern**:
```csharp
[DataSourceProperty]
public ImageIdentifierVM Image { get; private set; }

// For ItemObject (equipment images):
Image = new ImageIdentifierVM(itemObject, ""); // ✅ VERIFIED current API

// For empty slots:
Image = new ImageIdentifierVM(0); // ✅ VERIFIED fallback
```

✅ **Template Pattern**:
```xml
<ImageIdentifierWidget DataSource="{Image}" 
                       AdditionalArgs="@AdditionalArgs" 
                       ImageId="@Id" 
                       ImageTypeCode="@ImageTypeCode" 
                       LoadingIconWidget="LoadingIconWidget">
  <Children>
    <Standard.CircleLoadingWidget Id="LoadingIconWidget" />
  </Children>
</ImageIdentifierWidget>
```

### **Equipment Image Loading Process**

1. **ItemObject** → **ImageIdentifier** (creates `Id` from `itemObject.StringId`)
2. **ImageIdentifier** → **ImageIdentifierVM** (ViewModel wrapper for data binding)
3. **ImageIdentifierVM** → **ImageIdentifierWidget** (renders actual item image in UI)
4. **LoadingIconWidget** → Shows loading spinner until image loads

### **Key Properties Exposed to Templates**

- **`@Id`**: Item's StringId (e.g., "iron_sword_t2")
- **`@ImageTypeCode`**: ImageIdentifierType.Item for equipment
- **`@AdditionalArgs`**: Banner code or additional visual info
- **Loading support**: Built-in loading spinner functionality

## **Implementation Status** ✅

The **current modern API** is fully implemented:
- ✅ Proper assembly reference (`TaleWorlds.PlayerServices`)
- ✅ Correct constructor usage (`new ImageIdentifierVM(item, "")`)
- ✅ Proper template binding (`DataSource="{Image}"`)
- ✅ All current v1.2.12 API compatibility

**Equipment images should now load correctly** using the official current Bannerlord image system.
