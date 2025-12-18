# Dynamic Text and Placeholder System

This document specifies the placeholder system for personalizing event text. Events should feel like they're happening to YOU, in THIS army, with THESE people.

---

## Table of Contents

1. [Purpose](#purpose)
2. [Standard Placeholders](#standard-placeholders)
3. [Usage Rules](#usage-rules)
4. [Context-Specific Placeholders](#context-specific-placeholders)
5. [Placeholder Resolution](#placeholder-resolution)
6. [Writing Guidelines](#writing-guidelines)
7. [Examples](#examples)

---

## Purpose

Generic text feels lifeless:
> "The sergeant calls for drill."

Personalized text feels real:
> "{SERGEANT_NAME} bellows across the camp. 'Get moving, {PLAYER_NAME}! You think {ENEMY_FACTION} is going to wait for you to finish your breakfast?'"

Placeholders let us write events once but have them feel unique to each playthrough.

---

## Standard Placeholders

### Always Available

These placeholders are always resolvable:

| Placeholder | Resolves To | Example |
|-------------|-------------|---------|
| `{PLAYER_NAME}` | Player's character name | "Alaric" |
| `{LORD_NAME}` | Enlisted lord's name | "Count Aldric" |
| `{LORD_TITLE}` | Lord's title only | "Count" |
| `{FACTION_NAME}` | Lord's faction name | "Vlandia" |
| `{FACTION_ADJECTIVE}` | Faction as adjective | "Vlandian" |
| `{LANCE_NAME}` | Player's lance name | "The Iron Spur" |
| `{PARTY_NAME}` | Lord's party name | "Count Aldric's Party" |

### Time and Location

| Placeholder | Resolves To | Example |
|-------------|-------------|---------|
| `{TIME_OF_DAY}` | Current time period | "dawn", "evening" |
| `{CURRENT_LOCATION}` | Nearest settlement or region | "near Pravend" |
| `{SETTLEMENT_NAME}` | Current/target settlement | "Sargot" |
| `{REGION_NAME}` | Geographic region | "the Vlandian highlands" |

### Player State

| Placeholder | Resolves To | Example |
|-------------|-------------|---------|
| `{PLAYER_TIER}` | Tier as word | "veteran" |
| `{PLAYER_DUTY}` | Current duty | "scout" |
| `{PLAYER_FORMATION}` | Formation type | "infantry" |
| `{DAYS_ENLISTED}` | Days served | "45" |

### Camp State

| Placeholder | Resolves To | Example |
|-------------|-------------|---------|
| `{SUPPLY_STATUS}` | Logistics description | "running low" |
| `{MORALE_STATUS}` | Morale description | "shaken after losses" |
| `{DAYS_FROM_TOWN}` | Days since settlement | "six days" |

---

## Context-Specific Placeholders

These placeholders are **only available when the trigger implies they exist**. Using them in the wrong context will show fallback text or break immersion.

### Combat / Enemy Context

**Available when:** `battle_won`, `battle_lost`, `pursuing`, `siege_ongoing`, `raid_completed`

| Placeholder | Resolves To | Example |
|-------------|-------------|---------|
| `{ENEMY_FACTION}` | Enemy faction name | "Battania" |
| `{ENEMY_FACTION_ADJECTIVE}` | Enemy as adjective | "Battanian" |
| `{ENEMY_LORD}` | Enemy commander (if known) | "Caladog" |
| `{ENEMY_PARTY}` | Enemy party name | "Caladog's Army" |
| `{BATTLE_LOCATION}` | Where battle occurred | "the fields near Marunath" |
| `{CASUALTIES_DESC}` | Casualty description | "heavy losses" |

### Siege Context

**Available when:** `siege_ongoing`, `siege_started`, `siege_ended`

| Placeholder | Resolves To | Example |
|-------------|-------------|---------|
| `{BESIEGED_SETTLEMENT}` | Settlement under siege | "Ortysia" |
| `{SIEGE_DAYS}` | Days of siege | "twelve" |
| `{DEFENDER_FACTION}` | Defending faction | "the Empire" |

### Naval Context (War Sails)

**Available when:** `at_sea`, `naval_battle`, `long_voyage`

| Placeholder | Resolves To | Example |
|-------------|-------------|---------|
| `{SHIP_NAME}` | Current ship name | "The Black Wake" |
| `{CAPTAIN_NAME}` | Ship captain | "Captain Hrolf" |
| `{DESTINATION_PORT}` | Target port | "Nordheim" |
| `{DAYS_AT_SEA}` | Voyage duration | "eighteen" |

### Injury/Medical Context

**Available when:** `has_injury`, `has_illness`

| Placeholder | Resolves To | Example |
|-------------|-------------|---------|
| `{INJURY_TYPE}` | Injury description | "twisted knee" |
| `{INJURY_LOCATION}` | Body part | "leg" |
| `{ILLNESS_TYPE}` | Illness name | "camp fever" |
| `{RECOVERY_DAYS}` | Est. recovery time | "four or five days" |

### Lance / Social Context

| Placeholder | Resolves To | Example |
|-------------|-------------|---------|
| `{LANCE_MATE_NAME}` | Random lance member | "Borcha" |
| `{SERGEANT_NAME}` | Lance sergeant | "Sergeant Morcar" |
| `{RIVAL_NAME}` | A rival soldier (if exists) | "that bastard Ingolf" |

---

## Usage Rules

### Rule 1: Only Use What Exists

X **Wrong:** Using `{ENEMY_LORD}` in a training event
```
"Train hard, {PLAYER_NAME}. {ENEMY_LORD} won't show mercy."
```
This breaks if there's no current enemy.

[x] **Right:** Using `{ENEMY_FACTION}` only when contextually appropriate
```
// Event trigger: battle_won
"We beat the {ENEMY_FACTION_ADJECTIVE} bastards today."
```

### Rule 2: Provide Fallbacks

For optional placeholders, the system should gracefully handle missing data:

```csharp
// If ENEMY_LORD is null, use faction instead
"{ENEMY_LORD|the enemy}" -> "Caladog" or "the enemy"
```

### Rule 3: Keep It Natural

Placeholders should fit natural speech patterns.

X **Awkward:**
```
"{PLAYER_NAME}, {LORD_NAME} of {FACTION_NAME} requires your presence."
```

[x] **Natural:**
```
"{LORD_TITLE} {LORD_NAME} wants to see you, {PLAYER_NAME}."
```

### Rule 4: Don't Overuse

One or two placeholders per paragraph. More feels robotic.

X **Too many:**
```
"{PLAYER_NAME}, as a {PLAYER_TIER} {PLAYER_FORMATION} of {LANCE_NAME} serving {LORD_NAME} of {FACTION_NAME}..."
```

[x] **Just enough:**
```
"The sergeant catches your eye. 'You're with me today, {PLAYER_NAME}.'"
```

---

## Placeholder Resolution

### Implementation Pattern

```csharp
public static string ResolvePlaceholders(string text, EventContext context)
{
    var result = text;
    
    // Always available
    result = result.Replace("{PLAYER_NAME}", Hero.MainHero.Name.ToString());
    result = result.Replace("{LORD_NAME}", context.Lord?.Name.ToString() ?? "the lord");
    result = result.Replace("{LORD_TITLE}", GetLordTitle(context.Lord));
    result = result.Replace("{FACTION_NAME}", context.Lord?.MapFaction?.Name.ToString() ?? "the realm");
    result = result.Replace("{LANCE_NAME}", context.LanceName ?? "your lance");
    
    // Time/location
    result = result.Replace("{TIME_OF_DAY}", GetTimeOfDayString());
    result = result.Replace("{CURRENT_LOCATION}", GetCurrentLocationString());
    
    // Context-specific (only if available)
    if (context.EnemyFaction != null)
    {
        result = result.Replace("{ENEMY_FACTION}", context.EnemyFaction.Name.ToString());
        result = result.Replace("{ENEMY_FACTION_ADJECTIVE}", GetFactionAdjective(context.EnemyFaction));
    }
    
    if (context.EnemyLord != null)
    {
        result = result.Replace("{ENEMY_LORD}", context.EnemyLord.Name.ToString());
    }
    
    // Fallback pattern: {PLACEHOLDER|fallback}
    result = ResolveFallbacks(result);
    
    return result;
}

private static string ResolveFallbacks(string text)
{
    // Match {SOMETHING|fallback} and use fallback if SOMETHING wasn't replaced
    var regex = new Regex(@"\{(\w+)\|([^}]+)\}");
    return regex.Replace(text, match =>
    {
        var placeholder = match.Groups[1].Value;
        var fallback = match.Groups[2].Value;
        // If the placeholder still exists (wasn't resolved), use fallback
        if (text.Contains($"{{{placeholder}}}"))
            return fallback;
        return match.Value;
    });
}
```

### Context Object

```csharp
public class EventContext
{
    // Always available
    public Hero Lord { get; set; }
    public string LanceName { get; set; }
    public MobileParty Party { get; set; }
    
    // Combat context
    public IFaction EnemyFaction { get; set; }
    public Hero EnemyLord { get; set; }
    public MapEvent RecentBattle { get; set; }
    
    // Siege context
    public Settlement BesiegedSettlement { get; set; }
    public int SiegeDays { get; set; }
    
    // Naval context (War Sails)
    public string ShipName { get; set; }
    public Hero Captain { get; set; }
    public int DaysAtSea { get; set; }
    
    // Injury context
    public string InjuryType { get; set; }
    public string InjuryLocation { get; set; }
    public string IllnessType { get; set; }
    
    // Social context
    public string LanceMateName { get; set; }
    public string SergeantName { get; set; }
}
```

---

## Writing Guidelines

### For External AI Brainstorming

Add these rules to the brainstorm prompt:

```
### Personalization Rules

Use placeholders to make events feel personal:

**Always available:**
- {PLAYER_NAME} — Player's name
- {LORD_NAME} — Enlisted lord
- {LANCE_NAME} — Player's lance unit
- {FACTION_NAME} — Lord's faction

**Use only when trigger implies they exist:**
- {ENEMY_FACTION}, {ENEMY_LORD} — Only for combat/pursuit events
- {BESIEGED_SETTLEMENT} — Only during sieges
- {INJURY_TYPE}, {INJURY_LOCATION} — Only for injury events
- {SHIP_NAME}, {CAPTAIN_NAME} — Only for naval events

**Rules:**
1. Only use context-specific placeholders when the event trigger guarantees they exist
2. Use 1-2 placeholders per paragraph maximum
3. Write naturally — placeholders should fit speech patterns
4. When in doubt, use generic text over a potentially broken placeholder
```

### Tone Examples

**Training Event (no enemy context):**
```
"{SERGEANT_NAME} barks across the yard. 'Form up! {PLAYER_NAME}, 
you're on the front line today. Let's see what {LANCE_NAME} is made of.'"
```

**Post-Battle Event (enemy context available):**
```
"The {ENEMY_FACTION_ADJECTIVE} dead litter the field. {LORD_NAME} 
rides past, surveying the carnage. You catch his eye — a nod of 
acknowledgment. {LANCE_NAME} held the line when it mattered."
```

**Siege Event:**
```
"Day {SIEGE_DAYS} beneath the walls of {BESIEGED_SETTLEMENT}. The 
{DEFENDER_FACTION} aren't going to starve any time soon, and neither 
patience nor supplies are infinite."
```

**Injury Event:**
```
"The surgeon prods your {INJURY_LOCATION}. 'That {INJURY_TYPE} will 
take time to heal, {PLAYER_NAME}. {RECOVERY_DAYS}, if you rest it 
properly. Longer if you're stupid about it.'"
```

**Naval Event:**
```
"Eighteen days aboard {SHIP_NAME}. {CAPTAIN_NAME} says we'll see 
{DESTINATION_PORT} within the week, weather willing. The crew's 
getting restless."
```

---

## Examples

### Full Event with Placeholders

```json
{
  "id": "post_battle_rally",
  "title": "After the Fight",
  "type": "general_event",
  
  "triggers": { "all": ["battle_won"] },
  "time_of_day": ["morning", "afternoon", "evening"],
  
  "requires_context": ["enemy_faction"],
  
  "setup": "The {ENEMY_FACTION_ADJECTIVE} broke and ran. Around you, {LANCE_NAME} catches their breath, checking wounds, counting heads. {LORD_NAME} rides down the line. When he reaches you, he pauses.\n\n'You fought well today, {PLAYER_NAME}.'",
  
  "options": [
    {
      "id": "humble",
      "text": "'Just doing my duty, {LORD_TITLE}.'",
      "risk": "safe",
      "rewards": { "skill_xp": { "Charm": 15 } }
    },
    {
      "id": "proud",
      "text": "'We showed those {ENEMY_FACTION_ADJECTIVE} bastards.'",
      "risk": "safe",
      "rewards": { "skill_xp": { "Leadership": 15 } }
    },
    {
      "id": "request",
      "text": "'The lads need rest, {LORD_TITLE}. And ale.'",
      "risk": "risky",
      "rewards": { "skill_xp": { "Charm": 20, "Leadership": 10 } },
      "note": "Lord might grant request or rebuke"
    }
  ]
}
```

### Dawn Muster with Personalization

```json
{
  "id": "dawn_muster",
  "title": "Morning Muster",
  
  "triggers": { "all": ["is_enlisted"] },
  "time_of_day": ["dawn"],
  
  "setup": "{SERGEANT_NAME}'s voice shatters the {TIME_OF_DAY} quiet. 'On your feet, {LANCE_NAME}! {LORD_NAME} wants us formed up and ready before the sun clears the hills.'\n\nAround you, soldiers groan and fumble for boots. {LANCE_MATE_NAME} is still snoring.",
  
  "options": [
    {
      "id": "quick",
      "text": "Fall in first, set the example",
      "risk": "safe",
      "rewards": { "skill_xp": { "Leadership": 15 } }
    },
    {
      "id": "help",
      "text": "Kick {LANCE_MATE_NAME} awake, fall in together",
      "risk": "safe", 
      "rewards": { "skill_xp": { "Charm": 15 } }
    },
    {
      "id": "slow",
      "text": "Take your time, fall in with the rest",
      "risk": "safe",
      "rewards": {}
    }
  ]
}
```

### Siege Event with Context Requirements

```json
{
  "id": "siege_wall_watch",
  "title": "Wall Watch",
  
  "triggers": { "all": ["siege_ongoing"] },
  "time_of_day": ["night"],
  "required_activity_states": ["besieging"],
  
  "requires_context": ["besieged_settlement", "defender_faction"],
  
  "setup": "Night watch on the siege lines. {BESIEGED_SETTLEMENT}'s walls loom black against the stars. Somewhere up there, {DEFENDER_FACTION} sentries are watching you right back.\n\nThe camp behind you is quiet. {SIEGE_DAYS} days of this. The waiting is almost worse than fighting.",
  
  "options": [
    {
      "id": "vigilant",
      "text": "Stay sharp, watch for sorties",
      "risk": "safe",
      "costs": { "fatigue": 2 },
      "rewards": { "skill_xp": { "Scouting": 25 } }
    },
    {
      "id": "study",
      "text": "Study the walls, look for weaknesses",
      "risk": "safe",
      "costs": { "fatigue": 2 },
      "rewards": { "skill_xp": { "Engineering": 20, "Tactics": 15 } }
    },
    {
      "id": "doze",
      "text": "Find a quiet spot, rest your eyes",
      "risk": "risky",
      "costs": { "discipline": 1 },
      "rewards": { "fatigue_relief": 1 }
    }
  ]
}
```

---

## Placeholder Quick Reference

### For Brainstorming (Copy/Paste)

```
ALWAYS AVAILABLE:
{PLAYER_NAME}        - Player's character name
{LORD_NAME}          - Enlisted lord's name  
{LORD_TITLE}         - Lord's title (Count, Jarl, etc.)
{FACTION_NAME}       - Lord's faction
{LANCE_NAME}         - Player's lance unit
{SERGEANT_NAME}      - Lance sergeant
{LANCE_MATE_NAME}    - Random lance member
{TIME_OF_DAY}        - Current time (dawn, evening, etc.)

COMBAT CONTEXT (battle/pursuit triggers only):
{ENEMY_FACTION}      - Enemy faction name
{ENEMY_FACTION_ADJECTIVE} - "Vlandian", "Battanian", etc.
{ENEMY_LORD}         - Enemy commander
{CASUALTIES_DESC}    - "light losses", "heavy casualties"

SIEGE CONTEXT (siege triggers only):
{BESIEGED_SETTLEMENT} - Settlement under siege
{SIEGE_DAYS}         - Days besieging
{DEFENDER_FACTION}   - Who's defending

NAVAL CONTEXT (naval triggers only):
{SHIP_NAME}          - Current ship
{CAPTAIN_NAME}       - Ship captain
{DAYS_AT_SEA}        - Voyage length
{DESTINATION_PORT}   - Where we're going

INJURY CONTEXT (injury events only):
{INJURY_TYPE}        - "twisted knee", "arrow wound"
{INJURY_LOCATION}    - "arm", "leg", "torso"
{ILLNESS_TYPE}       - "camp fever", "flux"
{RECOVERY_DAYS}      - "three or four days"
```

---

*Document version: 1.0*
*Companion to: Lance Life Events Master Documentation v2*
