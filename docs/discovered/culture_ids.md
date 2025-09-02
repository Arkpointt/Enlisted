# Culture IDs Reference

Generated from Bannerlord XML files on 2025-09-02 01:30:00 UTC

## Main Cultures (from mpcultures.xml)

- **empire** - Empire faction (red/gold colors)
- **aserai** - Aserai faction (orange/brown colors)  
- **sturgia** - Sturgia faction (blue/grey colors)
- **vlandia** - Vlandia faction (red colors)
- **khuzait** - Khuzait faction (green colors)
- **battania** - Battania faction (green/brown colors)

## Culture Properties

Each culture has:
- **id**: String identifier for code reference
- **name**: Localized display name with {=key} format
- **is_main_culture**: Boolean indicating major faction
- **color/color2**: Primary and secondary faction colors
- **cloth_alternative_color1/2**: Alternative color schemes
- **banner_background_color1/2**: Banner color schemes
- **banner_foreground_color1/2**: Banner foreground colors
- **faction_banner_key**: Banner design string

## Usage in SAS Implementation

```csharp
// For culture-based equipment filtering (Phase 2.2)
public List<ItemObject> GetCultureEquipment(string cultureId, int maxTier)
{
    var culture = MBObjectManager.Instance.GetObject<CultureObject>(cultureId);
    var availableGear = new List<ItemObject>();
    
    var allCharacters = MBObjectManager.Instance.GetObjectTypeList<CharacterObject>();
    foreach (var character in allCharacters)
    {
        if (character.Culture == culture && character.Tier <= maxTier)
        {
            // Extract equipment from this culture's troops
            foreach (var equipment in character.BattleEquipments)
            {
                // Add culture-appropriate gear
            }
        }
    }
    
    return availableGear;
}
```

## Culture Reference for Equipment Selection

- **Empire**: Roman-style equipment, heavy infantry focus
- **Aserai**: Desert equipment, cavalry and archer focus
- **Sturgia**: Nordic equipment, infantry and archer focus  
- **Vlandia**: Western European equipment, cavalry focus
- **Khuzait**: Steppe equipment, horse archer focus
- **Battania**: Celtic equipment, forest fighter focus
