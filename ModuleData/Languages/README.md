# Enlisted Mod - Translation Guide

This folder contains localization files for the Enlisted mod. The mod is fully translatable and supports multiple languages.

## Current Languages

- **English** (Default) - `enlisted_strings.xml`

## How to Add a New Language Translation

### Step 1: Create Your Language Folder

Create a new subfolder named with your language code (2 letters):

```
ModuleData/Languages/
  ├─ language_data.xml         (English - keep this)
  ├─ enlisted_strings.xml       (English - keep this)
  └─ FR/                        (Your new language folder)
      ├─ language_data.xml      (Your language config)
      └─ enlisted_strings_fr.xml (Your translated strings)
```

**Common Language Codes:**
- `FR` = French (Français)
- `DE` = German (Deutsch)
- `ES` = Spanish (Español)
- `IT` = Italian (Italiano)
- `PL` = Polish (Polski)
- `RU` = Russian (Русский)
- `TR` = Turkish (Türkçe)
- `BR` = Brazilian Portuguese (Português do Brasil)
- `JP` = Japanese (日本語)
- `KO` = Korean (한국어)
- `CNs` = Simplified Chinese (简体中文)
- `CNt` = Traditional Chinese (繁體中文)

### Step 2: Create language_data.xml

Create `ModuleData/Languages/FR/language_data.xml` (replace FR with your language code):

```xml
<?xml version="1.0" encoding="utf-8"?>
<LanguageData id="Français" name="Français" subtitle_extension="fr" supported_iso="fr,fra,fr-fr,fr-be,fr-ca,fr-ch" under_development="false">
  <LanguageFile xml_path="FR/enlisted_strings_fr.xml" />
</LanguageData>
```

**Important Fields:**
- `id` = Display name in the game menu (use native language name)
- `name` = Same as id (use native language name, not English)
- `subtitle_extension` = 2-letter code for subtitles
- `supported_iso` = Comma-separated list of ISO language codes
- `under_development` = Set to "true" if translation is incomplete
- `xml_path` = Path to your translated strings file (relative to Languages/)

**Example for German:**
```xml
<LanguageData id="Deutsch" name="Deutsch" subtitle_extension="de" supported_iso="de,ger,de-de,de-at,de-ch" under_development="false">
  <LanguageFile xml_path="DE/enlisted_strings_de.xml" />
</LanguageData>
```

### Step 3: Translate the Strings File

1. Copy the English `enlisted_strings.xml` to your language folder
2. Rename it to `enlisted_strings_XX.xml` (XX = your language code)
3. Translate the `text` attributes (DO NOT change `id` attributes)

**Example:**

English:
```xml
<string id="Enlisted_Menu_Status_Title" text="Status Report" />
```

French:
```xml
<string id="Enlisted_Menu_Status_Title" text="Rapport de Statut" />
```

German:
```xml
<string id="Enlisted_Menu_Status_Title" text="Statusbericht" />
```

**CRITICAL RULES:**
- ✅ **DO** translate the `text` attribute
- ✅ **DO** keep all `{PLACEHOLDER}` tokens unchanged
- ✅ **DO** preserve `\n` for line breaks
- ✅ **DO** preserve XML entities (`&quot;`, `&lt;`, `&gt;`, `&amp;`)
- ❌ **DO NOT** change `id` attributes
- ❌ **DO NOT** translate placeholder names like `{PLAYER_NAME}`, `{RANK}`, etc.
- ❌ **DO NOT** remove strings

### Step 4: Handle Special Characters

**XML Entities** (required for special characters):
- `&quot;` = " (quotation mark)
- `&lt;` = < (less than)
- `&gt;` = > (greater than)
- `&amp;` = & (ampersand)
- `&apos;` = ' (apostrophe)

**Example:**
```xml
<string id="example" text="He said &quot;Hello&quot; and left." />
```

### Step 5: Test Your Translation

1. Place your language folder in `ModuleData/Languages/`
2. Launch Mount & Blade II: Bannerlord
3. Go to **Options → Gameplay → Language**
4. Select your language from the dropdown
5. Restart the game
6. Check if your translations appear in dialogs and menus

If your language doesn't appear in the dropdown, check:
- `language_data.xml` has correct XML syntax
- `id` attribute matches expected format
- Folder name matches language code
- File paths in `xml_path` are correct

## Translation Status

Mark your translation as work-in-progress by setting:
```xml
under_development="true"
```

This allows players to test incomplete translations. Change to `"false"` when complete.

## Placeholders Reference

The mod uses dynamic placeholders that get replaced with game data at runtime.

### Character Names

| Placeholder | Description | Example |
|-------------|-------------|---------|
| `{PLAYER_NAME}` | Player's character name | "John" |
| `{PLAYER_RANK}` | Player's current rank | "Veteran Soldier" |
| `{SERGEANT}` | NCO full name with rank | "Sergeant Harald" |
| `{SERGEANT_NAME}` | Same as {SERGEANT} | "Sergeant Harald" |
| `{NCO_NAME}` | Same as {SERGEANT} | "Sergeant Harald" |
| `{NCO_RANK}` | NCO rank title only | "Sergeant" |
| `{OFFICER_NAME}` | Generic officer reference | "Sergeant Harald" |
| `{CAPTAIN_NAME}` | Captain reference | "the Captain" |
| `{SOLDIER_NAME}` | Random soldier name | "Bjorn" |
| `{COMRADE_NAME}` | Same as {SOLDIER_NAME} | "Bjorn" |
| `{VETERAN_1_NAME}` | Veteran soldier (experienced) | "Erik" |
| `{VETERAN_2_NAME}` | Second veteran soldier | "Magnus" |
| `{RECRUIT_NAME}` | New recruit name | "Young Olaf" |
| `{SECOND_SHORT}` | Another soldier name | "Sven" |

### Naval Crew (War Sails DLC)

| Placeholder | Description | Example |
|-------------|-------------|---------|
| `{BOATSWAIN_NAME}` | Ship's boatswain | "the Boatswain" |
| `{NAVIGATOR_NAME}` | Ship's navigator | "the Navigator" |
| `{FIELD_MEDIC_NAME}` | Medical specialist | "the Field Medic" |
| `{SHIP_NAME}` | Ship name | "Sea Wolf" |
| `{DESTINATION_PORT}` | Destination port | "port" |
| `{DAYS_AT_SEA}` | Days at sea | "7" |

### Lord & Faction

| Placeholder | Description | Example |
|-------------|-------------|---------|
| `{LORD_NAME}` | Enlisted lord's name | "Caladog" |
| `{LORD_TITLE}` | Lord or Lady | "Lord" |
| `{PREVIOUS_LORD}` | Previous lord (transfer events) | "your previous lord" |
| `{ALLIED_LORD}` | Allied lord reference | "an allied lord" |
| `{FACTION_NAME}` | Your faction's name | "Vlandia" |
| `{KINGDOM_NAME}` | Kingdom name | "Kingdom of Vlandia" |
| `{ENEMY_FACTION_ADJECTIVE}` | Enemy descriptor | "enemy" |

### Location & Party

| Placeholder | Description | Example |
|-------------|-------------|---------|
| `{SETTLEMENT_NAME}` | Current settlement | "Sargot" |
| `{COMPANY_NAME}` | Your party name | "Wolf Company" |
| `{TROOP_COUNT}` | Total troop count | "87" |

### Rank & Progression

| Placeholder | Description | Example |
|-------------|-------------|---------|
| `{RANK}` | Current rank name | "Serjeant" |
| `{TIER}` | Current tier number | "3" |
| `{NEXT_RANK}` | Next rank in progression | "the next rank" |
| `{SECOND_RANK}` | Current rank (variant) | "Veteran Soldier" |

### Medical Events

| Placeholder | Description | Example |
|-------------|-------------|---------|
| `{CONDITION_TYPE}` | Type of illness/injury | "illness" |
| `{CONDITION_LOCATION}` | Body part affected | "your arm" |
| `{COMPLICATION_NAME}` | Medical complication | "infection" |
| `{REMEDY_NAME}` | Treatment/medicine | "medicine" |

### Generic Values

| Placeholder | Description | Example |
|-------------|-------------|---------|
| `{GOLD}` | Gold amount | "150" |
| `{DAYS}` | Days count | "42" |
| `{COUNT}` | Generic count | "5" |
| `{REL}` | Relationship number | "15" |
| `{REL_DESC}` | Relationship description | "Friendly" |

**CRITICAL: Never translate placeholder names!** They must remain in English (e.g., `{PLAYER_NAME}`, not `{NOM_JOUEUR}`). The game replaces these with actual values at runtime.

**Full reference:** See [Event Catalog - Placeholder Variables](../../docs/Content/event-catalog-by-system.md#placeholder-variables)

## Line Breaks

Use `\n` for line breaks in dialog text:

```xml
<string id="example" text="First line.\n\nSecond paragraph." />
```

- `\n` = Single line break
- `\n\n` = Paragraph break (blank line between)

## Getting Help

- **Report Issues:** Open an issue on GitHub if translations don't work
- **Share Translations:** Submit a pull request with your language folder
- **Community:** Join our Discord for translation help

## Template Example

See the `_TEMPLATE/` folder (if provided) for a complete example of a translation setup.

## Credits

Translations are community contributions. Add your name here when submitting:

- **English:** Enlisted Mod Team (Original)
- **French:** [Your Name]
- **German:** [Your Name]
- etc.

---

**Thank you for helping make Enlisted accessible to more players!**

For technical questions or if your translation isn't loading, check that:
1. Your `language_data.xml` has valid XML syntax
2. The `id` attribute uses the native language name (e.g., "Français" not "French")
3. The `xml_path` points to your translated file correctly
4. All `string` tags have both `id` and `text` attributes
5. Your language code matches Bannerlord's supported languages

The game will fall back to English for any missing strings, so partial translations are okay!

