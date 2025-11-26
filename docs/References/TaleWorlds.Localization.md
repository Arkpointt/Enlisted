# TaleWorlds.Localization Reference

## Relevance to Enlisted Mod: LOW-MEDIUM

The Localization system handles text translation and variable substitution. While we don't currently support multiple languages, using `TextObject` properly allows for future localization.

---

## Key Classes and Systems

### Core Classes

| Class | Purpose | Enlisted Usage |
|-------|---------|----------------|
| `TextObject` | Localizable text | All displayed text |
| `MBTextManager` | Text management | Text registration |
| `LocalizedTextManager` | Language handling | Language switching |

---

## TextObject Usage

### Basic Text
```csharp
var text = new TextObject("Simple text");
string result = text.ToString();
```

### With Variables
```csharp
var text = new TextObject("{=enlisted_wage}You earned {GOLD} gold for your service.");
text.SetTextVariable("GOLD", wageAmount);
string result = text.ToString();
```

### With Localization ID
```csharp
// The {=id} prefix is for localization lookup
var text = new TextObject("{=enlisted_enlist_success}You have enlisted with {LORD_NAME}.");
text.SetTextVariable("LORD_NAME", lord.Name);
```

---

## Variable Types

| Type | Usage |
|------|-------|
| `{VARIABLE}` | Simple substitution |
| `{?CONDITION}text{\\?}` | Conditional text |
| `{.s}` | Subject pronoun |
| `{.o}` | Object pronoun |

---

## Our Text Patterns

We use inline text without localization IDs for simplicity:

```csharp
// Current approach
var message = new TextObject("You have been promoted to {TIER_NAME}!");
message.SetTextVariable("TIER_NAME", tierName);

// Future localization-ready approach
var message = new TextObject("{=enlisted_promotion}You have been promoted to {TIER_NAME}!");
message.SetTextVariable("TIER_NAME", tierName);
```

---

## Adding Localization Support

To support multiple languages:

1. Create language XML files:
```xml
<strings>
  <string id="enlisted_promotion" text="You have been promoted to {TIER_NAME}!" />
</strings>
```

2. Register in SubModule.xml:
```xml
<XmlNode>
  <XmlName id="Strings" />
  <IncludedGameTypes>
    <GameType value="Campaign" />
  </IncludedGameTypes>
</XmlNode>
```

---

## Not Currently Prioritized

Full localization requires:
- XML string files per language
- Consistent use of localization IDs
- Translation work

For initial release, English-only with `TextObject` structure allows easy future localization.

