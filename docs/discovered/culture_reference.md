# Culture Reference - Complete Guide

**Comprehensive culture and troop data for the Enlisted military service system**

## 🎯 **Culture Selection System**

**Lord Selection = Culture Inheritance**: Players choose their military culture by enlisting with lords of different factions. The enlisted lord's culture becomes the player's military culture for **troop selection** and progression.

**Implementation**: `EnlistmentBehavior.Instance.CurrentLord.Culture.StringId` filters available **real Bannerlord troops** for promotion choices.

**Troop Selection Flow**:
1. **Promotion Available** → *"Promotion available! Press 'P' to advance."*
2. **Player Opens Menu** → Shows real culture troops: *"Imperial Legionary", "Aserai Mameluke"* 
3. **Player Chooses Troop** → Equipment applied from `CharacterObject.BattleEquipments`
4. **Formation Auto-Detected** → From troop's `IsRanged`/`IsMounted` properties for duties system

## 💰 **Realistic Military Economics**

**1-Year Progression**: 18,000 XP total over 365 days of service
**Daily Wages**: 24-150 gold/day progression (early game skill-building, not wealth generation)

| **Tier** | **Rank** | **XP Req** | **Timeline** | **Daily Wage** | **Purpose** |
|-----------|----------|------------|--------------|----------------|-------------|
| 1 | Recruit | 0 | Start | 24-30 gold | Basic training |
| 2 | Private | 500 | ~3 weeks | 30-37 gold | Formation choice |
| 3 | Corporal | 1,500 | ~2 months | 38-61 gold | Officer roles |
| 4 | Sergeant | 3,500 | ~3 months | 50-81 gold | Veteran status |
| 5 | Staff Sergeant | 7,000 | ~5 months | 68-111 gold | Leadership |
| 6 | Master Sergeant | 12,000 | ~9 months | 92-150 gold | Senior roles |
| 7 | Veteran | 18,000 | ~12 months | 120-150 gold | **Retirement eligible** |

**Equipment Policy**: Each promotion **replaces** equipment (turn in old, receive new). Keep final equipment at retirement only.

## 🏛️ **Main Cultures** (from mpcultures.xml)

- **empire** - Empire faction (red/gold colors) - Roman-style equipment, heavy infantry focus
- **aserai** - Aserai faction (orange/brown colors) - Desert equipment, cavalry and archer focus  
- **sturgia** - Sturgia faction (blue/grey colors) - Nordic equipment, infantry and archer focus
- **vlandia** - Vlandia faction (red colors) - Western European equipment, cavalry focus
- **khuzait** - Khuzait faction (green colors) - Steppe equipment, horse archer focus
- **battania** - Battania faction (green/brown colors) - Celtic equipment, forest fighter focus

## 🛡️ **Culture Equipment Templates** (VERIFIED from Game XML)

**CONFIRMED**: All cultures use consistent naming patterns: `{culture}_{item_type}_{tier}` format

### Empire
| StringId | Name | Tier | Equipment Focus |
|----------|------|------|----------------|
| guard_empire | Guard | 3 | imperial_spear_t2, legionary_mail, ironlame_spiked_kettle |
| spc_wanderer_empire_0-3 | Scholar/Bull/Silent/Boar | 2-3 | npc_companion_equipment_template_empire |
| **Weapons** | **Verified Game IDs** | **Tiers** | empire_sword_1_t2, empire_sword_2_t3, empire_sword_6_t5 |

### Aserai  
| StringId | Name | Tier | Equipment Focus |
|----------|------|------|----------------|
| guard_aserai | Guard | 3 | eastern_spear_2_t3, southern_lamellar_armor, emirs_helmet |
| **Weapons** | **VERIFIED Game IDs** | **T2-T4** | aserai_sword_1_t2, aserai_sword_3_t3, aserai_sword_4_t4, aserai_sword_5_t4 |
| **Armor** | **VERIFIED Game IDs** | **All** | aserai_tunic_waistcoat, aserai_female_civil_a, long_desert_robe, layered_robe |
| **2H Weapons** | **VERIFIED Game IDs** | **T3+** | aserai_2haxe_1_t3, bamboo_axe_t4 |

### Khuzait
| StringId | Name | Tier | Equipment Focus |
|----------|------|------|----------------|
| guard_khuzait | Guard | 2 | steppe_sword_t2, khuzait_armor, steppe_cap |
| **Weapons** | **VERIFIED Game IDs** | **T2-T5** | khuzait_sword_1_t2, khuzait_sword_2_t3, khuzait_sword_3_t3, khuzait_sword_4_t4 |
| **Noble Weapons** | **VERIFIED Game IDs** | **T5** | khuzait_noble_sword_1_t5, khuzait_noble_sword_2_t5, khuzait_noble_sword_3_t5 |
| **Armor** | **VERIFIED Game IDs** | **All** | khuzait_civil_coat, khuzait_fortified_armor, khuzait_lamellar_strapped, eastern_lamellar_armor |

### Vlandia
| StringId | Name | Tier | Equipment Focus |
|----------|------|------|----------------|
| guard_vlandia | Guard | 3 | vlandia_sword_2_t3, mail_hauberk, great_helmet |
| **Weapons** | **VERIFIED Game IDs** | **T4** | vlandia_axe_2_t4 |
| **Crossbows** | **VERIFIED Game IDs** | **T3+** | crossbow_c, crossbow_e (both culture="Culture.vlandia") |

### Sturgia
| StringId | Name | Tier | Equipment Focus |
|----------|------|------|----------------|
| guard_sturgia | Guard | 2 | sturgia_axe_t2, nordic_short_sword_t2, nordic_huscarl_helmet |
| **Weapons** | **VERIFIED Game IDs** | **T4-T5** | sturgia_axe_4_t4, sturgia_axe_5_t5 |
| **Noble Weapons** | **VERIFIED Game IDs** | **T5** | sturgia_noble_sword_1_t5, sturgia_noble_sword_2_t5, sturgia_noble_sword_3_t5, sturgia_noble_sword_4_t5 |

### Battania
| StringId | Name | Tier | Equipment Focus |
|----------|------|------|----------------|
| guard_battania | Guard | 2 | battania_sword_t2, highland_armor, battania_reinforced_leather_cap |
| **Weapons** | **VERIFIED Game IDs** | **T4-T5** | battania_axe_2_t4, battania_axe_3_t5 |
| **2H Weapons** | **VERIFIED Game IDs** | **T2+** | battania_2haxe_1_t2 |
| **Noble Weapons** | **VERIFIED Game IDs** | **T5** | battania_noble_sword_1_t5 |

## ⚔️ **Equipment Categories by Tier**

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

## 🎖️ **Formation Names by Culture**

### Infantry Formations
- **Empire**: Legionary
- **Aserai**: Footman
- **Khuzait**: Spearman
- **Vlandia**: Man-at-Arms
- **Sturgia**: Warrior
- **Battania**: Clansman

### Archer Formations
- **Empire**: Sagittarius
- **Aserai**: Marksman
- **Khuzait**: Hunter
- **Vlandia**: Crossbowman
- **Sturgia**: Bowman
- **Battania**: Skirmisher

### Cavalry Formations
- **Empire**: Equites
- **Aserai**: Mameluke
- **Khuzait**: Lancer
- **Vlandia**: Knight
- **Sturgia**: Druzhnik
- **Battania**: Mounted Warrior

### Horse Archer Formations
- **Empire**: Equites Sagittarii
- **Aserai**: Desert Horse Archer
- **Khuzait**: Horse Archer
- **Vlandia**: Mounted Crossbowman
- **Sturgia**: Mounted Archer
- **Battania**: Mounted Skirmisher

## 🔍 **CRITICAL DISCOVERY: Mixed Item Naming Patterns**

**IMPORTANT**: Bannerlord uses **TWO item identification systems**:

### Primary Pattern: Culture Prefix + Tier
```
{culture}_{item_type}_{variant}_t{tier}
Examples: aserai_sword_1_t2, khuzait_noble_sword_2_t5, battania_axe_3_t5
```

### Secondary Pattern: Generic Names + Culture XML Attribute  
```
{generic_name} with culture="Culture.{culture}" in XML
Examples: desert_robe (culture="Culture.aserai"), crossbow_c (culture="Culture.vlandia")
```

**Implication for Equipment Generation**: 
1. **First**: Try culture prefix pattern
2. **Fallback**: Use generic items with culture XML attribute  
3. **Validation**: Always check existence with `MBObjectManager.GetObject<ItemObject>(itemId)`

## 🔧 **Equipment Discovery Implementation**

```csharp
// Get culture-appropriate equipment for enlisted soldiers
public List<ItemObject> GetCultureEquipment(string cultureId, int maxTier)
{
    var culture = MBObjectManager.Instance.GetObject<CultureObject>(cultureId);
    var availableGear = new List<ItemObject>();
    
    var allCharacters = MBObjectManager.Instance.GetObjectTypeList<CharacterObject>();
    foreach (var character in allCharacters)
    {
        if (character.Culture == culture && character.Tier <= maxTier)
        {
            foreach (var equipment in character.BattleEquipments)
            {
                // Extract culture-appropriate gear
                for (EquipmentIndex i = EquipmentIndex.Weapon0; i <= EquipmentIndex.HorseHarness; i++)
                {
                    var item = equipment[i].Item;
                    if (item != null && !availableGear.Contains(item))
                        availableGear.Add(item);
                }
            }
        }
    }
    
    return availableGear;
}
```

## 📊 **Culture-Specific Economics**

### Equipment Cost Modifiers
- **Khuzait**: 0.8× (steppe economy - cheapest)
- **Battania**: 0.8× (tribal economy)
- **Aserai**: 0.9× (desert economy)
- **Sturgia**: 0.9× (nordic economy)
- **Empire**: 1.0× (base economy)
- **Vlandia**: 1.2× (wealthy western kingdoms - most expensive)

### Troop Availability by Culture
Each culture provides **3-6 different troop equipment styles per tier**:
- **Basic troops**: Recruit, warrior variations
- **Specialized troops**: Archer, cavalry variants
- **Elite troops**: Noble, champion variations
- **Guard troops**: Settlement guards with quality equipment

**Usage**: Enlisted soldiers can choose from any troop equipment style appropriate to their culture, tier, and formation specialization.
