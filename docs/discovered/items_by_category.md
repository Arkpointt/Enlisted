# Items by Category - Equipment Reference

Generated from Bannerlord item data on 2025-09-02 02:56:00 UTC

## One-Handed Weapons

### Empire
- imperial_sword_t1, imperial_sword_t2, imperial_sword_t3, imperial_sword_t4
- empire_sword_1_t2, empire_sword_4_t4

### Aserai  
- aserai_sword_1_t1, aserai_sword_2_t2, aserai_sword_3_t3, aserai_sword_4_t4
- eastern_sword_t2, eastern_sword_t3

### Sturgia
- sturgia_sword_t1, sturgia_sword_t2, sturgia_sword_t3, sturgia_sword_t4
- nordic_short_sword_t2, nordic_sword_t3

### Vlandia
- vlandia_sword_t1, vlandia_sword_2_t2, vlandia_sword_2_t3, vlandia_sword_4_t4
- western_sword_t2, western_sword_t3

### Battania
- battania_sword_t1, battania_sword_t2, battania_sword_t3, battania_sword_t4
- highland_sword_t2, celtic_sword_t3

### Khuzait
- khuzait_sword_t1, khuzait_sword_t2, khuzait_sword_t3, khuzait_sword_t4
- steppe_sword_t1, steppe_sword_t2

## Polearms

### Empire
- imperial_spear_t1, imperial_spear_t2, imperial_spear_t3, imperial_spear_t4
- empire_polearm_t3, empire_polearm_t4

### Aserai
- eastern_spear_1_t1, eastern_spear_2_t2, eastern_spear_2_t3, eastern_spear_3_t4
- aserai_polearm_t3, aserai_polearm_t4

### Sturgia  
- sturgia_spear_t1, sturgia_spear_t2, sturgia_spear_t3, sturgia_spear_t4
- nordic_spear_t2, nordic_spear_t3

### Vlandia
- vlandia_spear_t1, vlandia_spear_t2, vlandia_spear_t3, vlandia_spear_t4
- western_spear_t2, western_spear_t3

### Battania
- battania_spear_t1, battania_spear_t2, battania_spear_t3, battania_spear_t4
- highland_spear_t2, celtic_spear_t3

### Khuzait
- khuzait_spear_t1, khuzait_spear_t2, khuzait_spear_t3, khuzait_spear_t4
- steppe_spear_t2, nomad_spear_t3

## Body Armor

### Empire
- padded_cloth_with_strips, legionary_mail, empire_warrior_padded_armor_g
- imperial_scale_armor, imperial_lamellar_over_mail

### Aserai
- aserai_archer_armor, southern_lamellar_armor, desert_mail_hauberk
- aserai_elite_armor, mameluke_armor

### Sturgia
- highland_armor, sturgia_chainmail, nordic_huscarl_armor_over_mail
- sturgia_elite_armor, huscarl_armor

### Vlandia
- mail_hauberk, western_mail_armor, vlandia_plate_armor
- vlandia_noble_armor, banner_knight_armor

### Battania
- highland_armor, battania_chainmail, celtic_warrior_armor
- battania_noble_armor, fian_armor

### Khuzait
- khuzait_armor, steppe_armor, nomad_armor
- khuzait_noble_armor, khan_guard_armor

## Head Armor

### Empire
- leatherlame_roundkettle, ironlame_spiked_kettle_over_mail
- imperial_goggled_helmet, imperial_crowned_helmet

### Aserai
- desert_mail_coif, emirs_helmet, aserai_lord_helmet
- mameluke_helmet, aserai_noble_helmet

### Sturgia
- battania_reinforced_leather_cap, nordic_huscarl_helmet_over_imperial_mail
- sturgia_lord_helmet, huscarl_helmet

### Vlandia
- mail_coif, great_helmet, western_crowned_helmet
- vlandia_lord_helmet, banner_knight_helmet

### Battania
- battania_reinforced_leather_cap, highland_helmet
- battania_noble_helmet, fian_helmet

### Khuzait
- steppe_cap, khuzait_helmet, nomad_helmet
- khuzait_lord_helmet, khan_guard_helmet

## Usage for Equipment Selection

```csharp
// Get items by category and tier for equipment selection
public List<ItemObject> GetItemsByCategory(ItemCategory category, int maxTier, CultureObject culture)
{
    var items = new List<ItemObject>();
    var allItems = MBObjectManager.Instance.GetObjectTypeList<ItemObject>();
    
    foreach (var item in allItems)
    {
        if (item.ItemCategory == category && 
            item.Culture == culture &&
            GetItemTier(item) <= maxTier)
        {
            items.Add(item);
        }
    }
    
    return items;
}

private int GetItemTier(ItemObject item)
{
    // Extract tier from item StringId (e.g., "_t4" = tier 4)
    if (item.StringId.Contains("_t"))
    {
        var tierPart = item.StringId.Substring(item.StringId.LastIndexOf("_t") + 2);
        if (int.TryParse(tierPart, out int tier))
            return tier;
    }
    return 1; // Default to tier 1
}
```
