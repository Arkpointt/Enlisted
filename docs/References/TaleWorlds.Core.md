# TaleWorlds.Core Reference

## Relevance to Enlisted Mod: MEDIUM-HIGH

TaleWorlds.Core contains fundamental game types that are used across all game modes. This includes items, skills, characters, cultures, and base game mechanics. Understanding this assembly helps with equipment systems, skill calculations, and culture-specific behavior.

---

## Key Classes and Systems

### Items and Equipment

| Class | Purpose | Enlisted Usage |
|-------|---------|----------------|
| `ItemObject` | Definition of an item | Equipment kits, quartermaster |
| `EquipmentElement` | Single equipment slot | Player equipment management |
| `Equipment` | Full equipment set | Backup/restore equipment |
| `ItemModifier` | Item quality modifiers | Equipment quality handling |
| `WeaponComponentData` | Weapon-specific data | Weapon type detection |
| `ArmorComponent` | Armor-specific data | Armor type detection |

### Skills and Attributes

| Class | Purpose | Enlisted Usage |
|-------|---------|----------------|
| `SkillObject` | Skill definition (Athletics, etc.) | Formation-based skill training |
| `CharacterAttribute` | Attribute definition | Character stats |
| `DefaultSkills` | Static skill references | Access specific skills |
| `DefaultCharacterAttributes` | Static attribute refs | Access specific attributes |

### Characters and Cultures

| Class | Purpose | Enlisted Usage |
|-------|---------|----------------|
| `BasicCharacterObject` | Base character definition | Troop types |
| `CultureObject` | Culture definition (Vlandia, etc.) | Culture-specific equipment |
| `Banner` | Clan/kingdom banners | Visual elements |

### Core Game

| Class | Purpose | Enlisted Usage |
|-------|---------|----------------|
| `Game` | Main game instance | Access current game state |
| `GameType` | Game mode type | Check if campaign |
| `InformationManager` | Display messages | Show player notifications |
| `TextObject` | Localized text | Display text to player |

---

## Important Classes for Enlisted

### InformationManager
Used to display messages to the player:

```csharp
// Simple message
InformationManager.DisplayMessage(new InformationMessage("Message text"));

// With color
InformationManager.DisplayMessage(new InformationMessage(
    "Message text", 
    Colors.Green));

// Inquiry (popup)
InformationManager.ShowInquiry(new InquiryData(
    "Title",
    "Message",
    true, true,
    "Accept", "Decline",
    OnAccept, OnDecline));
```

### TextObject
For localized/parameterized text:

```csharp
var text = new TextObject("{=some_id}You earned {GOLD} gold.");
text.SetTextVariable("GOLD", 100);
string result = text.ToString();
```

### Equipment
Managing character equipment:

```csharp
// Get player equipment
var equipment = Hero.MainHero.BattleEquipment;

// Copy equipment
var backup = new Equipment(equipment);

// Check slot
var weapon = equipment[EquipmentIndex.Weapon0];
if (!weapon.IsEmpty)
{
    var item = weapon.Item;
}
```

---

## Equipment Slots

| Index | Purpose |
|-------|---------|
| `EquipmentIndex.Weapon0` | Primary weapon |
| `EquipmentIndex.Weapon1` | Secondary weapon |
| `EquipmentIndex.Weapon2` | Third weapon |
| `EquipmentIndex.Weapon3` | Fourth weapon |
| `EquipmentIndex.Head` | Helmet |
| `EquipmentIndex.Body` | Body armor |
| `EquipmentIndex.Leg` | Leg armor |
| `EquipmentIndex.Gloves` | Gloves |
| `EquipmentIndex.Cape` | Cape/cloak |
| `EquipmentIndex.Horse` | Mount |
| `EquipmentIndex.HorseHarness` | Mount armor |

---

## Skill System

Skills we care about for formation training:

| Skill | Formation Type |
|-------|----------------|
| `DefaultSkills.Athletics` | Infantry |
| `DefaultSkills.Polearm` | Infantry |
| `DefaultSkills.OneHanded` | Infantry |
| `DefaultSkills.TwoHanded` | Infantry |
| `DefaultSkills.Bow` | Ranged |
| `DefaultSkills.Crossbow` | Ranged |
| `DefaultSkills.Throwing` | Ranged |
| `DefaultSkills.Riding` | Cavalry |
| `DefaultSkills.Tactics` | Command training |
| `DefaultSkills.Medicine` | Field Medic duty |
| `DefaultSkills.Engineering` | Siegewright duty |
| `DefaultSkills.Scouting` | Pathfinder duty |
| `DefaultSkills.Steward` | Provisioner duty |

---

## Item Types

Check item type for equipment logic:

```csharp
var item = equipmentElement.Item;

// Check if weapon
if (item.HasWeaponComponent)
{
    var weaponClass = item.PrimaryWeapon.WeaponClass;
    // WeaponClass.OneHandedSword, TwoHandedAxe, Bow, etc.
}

// Check if armor
if (item.HasArmorComponent)
{
    var armorType = item.ArmorComponent.MaterialType;
}

// Check if horse
if (item.HasHorseComponent)
{
    var horse = item.HorseComponent;
}
```

---

## Files to Study

| File | Why |
|------|-----|
| `ItemObject.cs` | Item definitions |
| `Equipment.cs` | Equipment management |
| `SkillObject.cs` | Skill system |
| `DefaultSkills.cs` | Skill references |
| `InformationManager.cs` | Player messaging |
| `TextObject.cs` | Text/localization |
| `Game.cs` | Game state access |

---

## Cultures

Available cultures for equipment matching:

| Culture ID | Name |
|------------|------|
| `vlandia` | Vlandia |
| `battania` | Battania |
| `empire` | Empire |
| `sturgia` | Sturgia |
| `aserai` | Aserai |
| `khuzait` | Khuzait |

Access via:
```csharp
var culture = lord.Culture;
var cultureId = culture.StringId; // "vlandia", etc.
```

---

## Banner System

Banners for visual representation:

```csharp
// Get clan banner
var banner = Clan.PlayerClan.Banner;

// Get kingdom banner  
var kingdomBanner = kingdom.Banner;
```

---

## Known Patterns

### Checking Item Validity
```csharp
if (equipmentElement.IsEmpty) return;
if (equipmentElement.Item == null) return;
```

### Safe Skill Access
```csharp
var skillValue = hero.GetSkillValue(DefaultSkills.Athletics);
```

### Culture Matching
```csharp
bool isSameCulture = hero.Culture == lord.Culture;
```

