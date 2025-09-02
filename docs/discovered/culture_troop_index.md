# Culture-Based Troop Equipment Index

Generated from Bannerlord XML data on 2025-09-02 02:56:00 UTC

## Empire

### Guard Templates
| StringId | Name | Tier | Equipment Sets |
|----------|------|------|----------------|
| guard_empire | Guard | 3 | imperial_spear_t2, legionary_mail, ironlame_spiked_kettle_over_mail |
| spc_wanderer_empire_0 | Scholar | 2 | npc_companion_equipment_template_empire |
| spc_wanderer_empire_1 | Bull | 2 | npc_companion_equipment_template_empire |
| spc_wanderer_empire_2 | Silent | 2 | npc_companion_equipment_template_empire |
| spc_wanderer_empire_3 | Boar | 3 | npc_companion_equipment_template_empire |

### Equipment Rosters Used
- **npc_companion_equipment_template_empire** (Tier 2-3)
- **imperial_spear_t2** (Tier 2 weapons)
- **legionary_mail** (Tier 3 armor)
- **ironlame_spiked_kettle_over_mail** (Tier 3 helmets)

## Aserai

### Guard Templates
| StringId | Name | Tier | Equipment Sets |
|----------|------|------|----------------|
| guard_aserai | Guard | 3 | eastern_spear_2_t3, southern_lamellar_armor, emirs_helmet |

### Equipment Rosters Used
- **npc_wanderer_equipment_template_aserai** (Tier 2-3)
- **eastern_spear_2_t3** (Tier 3 weapons)
- **southern_lamellar_armor** (Tier 3 armor)
- **emirs_helmet** (Tier 3 helmets)

## Sturgia

### Guard Templates
| StringId | Name | Tier | Equipment Sets |
|----------|------|------|----------------|
| guard_sturgia | Guard | 2 | sturgia_axe_t2, nordic_short_sword_t2, nordic_huscarl_helmet_over_imperial_mail |

### Equipment Rosters Used
- **npc_wanderer_equipment_template_sturgia** (Tier 2-3)
- **sturgia_axe_t2** (Tier 2 weapons)
- **nordic_huscarl_helmet_over_imperial_mail** (Tier 3 helmets)

## Vlandia

### Guard Templates
| StringId | Name | Tier | Equipment Sets |
|----------|------|------|----------------|
| guard_vlandia | Guard | 3 | vlandia_sword_2_t3, mail_hauberk, great_helmet |

### Equipment Rosters Used
- **npc_wanderer_equipment_template_vlandia** (Tier 2-3)
- **vlandia_sword_2_t3** (Tier 3 weapons)
- **mail_hauberk** (Tier 3 armor)
- **great_helmet** (Tier 3 helmets)

## Battania

### Guard Templates
| StringId | Name | Tier | Equipment Sets |
|----------|------|------|----------------|
| guard_battania | Guard | 2 | battania_sword_t2, highland_armor, battania_reinforced_leather_cap |

### Equipment Rosters Used
- **npc_wanderer_equipment_template_battania** (Tier 2-3)
- **battania_sword_t2** (Tier 2 weapons)
- **highland_armor** (Tier 2-3 armor)

## Khuzait

### Guard Templates
| StringId | Name | Tier | Equipment Sets |
|----------|------|------|----------------|
| guard_khuzait | Guard | 2 | steppe_sword_t2, khuzait_armor, steppe_cap |

### Equipment Rosters Used
- **npc_wanderer_equipment_template_khuzait** (Tier 2-3)
- **steppe_sword_t2** (Tier 2 weapons)
- **khuzait_armor** (Tier 2-3 armor)

## Equipment Categories by Tier

### Tier 1-2 Equipment
- **Weapons**: Basic swords, spears, axes (t1-t2 suffix)
- **Armor**: Padded armor, leather armor, basic mail
- **Helmets**: Leather caps, basic helmets

### Tier 3-4 Equipment  
- **Weapons**: Quality swords, spears, crossbows (t3-t4 suffix)
- **Armor**: Mail hauberks, lamellar armor, reinforced leather
- **Helmets**: Mail coifs, reinforced helmets, great helmets

### Tier 5-6 Equipment
- **Weapons**: Elite weapons, noble equipment (t5-t6 suffix)
- **Armor**: Plate armor, heavy mail, lordly equipment
- **Helmets**: Great helmets, crowned helmets, noble headgear

## Usage in Implementation

```csharp
// Get culture-appropriate equipment for enlisted soldiers
public List<ItemObject> GetCultureEquipment(string cultureId, int maxTier)
{
    var culture = MBObjectManager.Instance.GetObject<CultureObject>(cultureId);
    var availableGear = new List<ItemObject>();
    
    // Filter equipment by culture and tier
    var allCharacters = MBObjectManager.Instance.GetObjectTypeList<CharacterObject>();
    foreach (var character in allCharacters)
    {
        if (character.Culture == culture && character.Tier <= maxTier)
        {
            // Extract equipment from character's battle equipment sets
            foreach (var equipment in character.BattleEquipments)
            {
                // Add culture-appropriate, tier-appropriate gear
            }
        }
    }
    
    return availableGear;
}
```
