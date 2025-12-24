# Enlisted Menu Color Scheme

> Professional military intelligence brief theme for GameMenu text displays

|| |
|---|---|
| **Implemented** | December 23, 2025 |
| **Status** | Active |
| **Brush File** | `GUI/Brushes/EnlistedColors.xml` |
| **Primary Usage** | All GameMenu text displays |

---

## Overview

The Enlisted mod uses a semantic color palette to improve readability and create visual hierarchy in text-based GameMenu displays. The color scheme follows a "military intelligence brief" theme where section headers, status labels, and severity indicators use distinct colors for quick scanning.

Players can instantly identify problems at a glance - critical supplies, low reputation, and exhausted troops jump out in red/gold while healthy stats stay calm and readable.

---

## Menus Using Color Scheme

| Menu | Colored Elements |
|------|------------------|
| **Main Camp Hub** | COMPANY REPORT, COMPANY STATUS, RECENT ACTIONS headers + all status labels/values |
| **Reports Menu** | DAILY BRIEF, RECENT ACTIVITY, COMPANY STATUS, CAMPAIGN CONTEXT headers + all status lines |
| **Status Detail** | REPUTATION, ROLE & SPECIALIZATIONS, PERSONALITY TRAITS headers + reputation values |
| **Decisions Menu** | COMPANY REPORT header |

---

## Color Palette

### Semantic Colors

| Style Name | Hex Color | RGB | Purpose | Usage |
|------------|-----------|-----|---------|-------|
| **Header** | `#00CCFFFF` | Cyan | Section headers | Major report sections (DAILY BRIEF, COMPANY STATUS, etc.) |
| **Label** | `#8CDBC4FF` | Teal | Information labels | Status field names (READINESS:, MORALE:, SUPPLIES:, etc.) |
| **Success** | `#90EE90FF` | Light Green | Positive status | Good supplies (60+), high reputation |
| **Warning** | `#FFD700FF` | Gold | Caution status | Fair conditions (40-59), needs attention |
| **Alert** | `#FF6B6BFF` | Soft Red | Critical status | Poor conditions (<40), immediate attention required |
| **Default** | `#EEE0C7FF` | Warm Cream | Body text | Standard descriptive text, neutral information |

### Native Comparison

The custom colors integrate with Bannerlord's existing GameMenu system:

- **Header** matches native `Link` style color for consistency
- **Label** matches native `Link.Hero` style (turquoise/teal)
- **Success/Warning/Alert** are custom semantic colors for status indicators
- **Default** uses a warmer cream tone than native white for reduced eye strain

---

## Implementation

### Files

| File | Purpose |
|------|---------|
| `GUI/Brushes/EnlistedColors.xml` | Custom brush definition with color styles |
| `src/Features/Interface/Behaviors/EnlistedMenuBehavior.cs` | Text builders using color spans |

### Code Patterns

**Section headers** use the Header style (cyan):
```csharp
sb.AppendLine("<span style=\"Header\">_____ COMPANY STATUS _____</span>");
```

**Status labels** use the Label style (teal):
```csharp
sb.AppendLine($"<span style=\"Label\">READINESS:</span> {description}");
```

**Status values** use conditional colors based on severity:
```csharp
var colorStyle = value >= 60 ? "Default" : value >= 40 ? "Warning" : "Alert";
var coloredDescription = $"<span style=\"{colorStyle}\">{description}</span>";
```

**Supplies** use Success (green) for good status since it's a critical resource:
```csharp
var suppliesColor = needs.Supplies >= 60 ? "Success" : needs.Supplies >= 40 ? "Warning" : "Alert";
```

**Reputation** values use Success/Warning/Alert based on standing:
```csharp
var lordColor = escalation.State.LordReputation >= 60 ? "Success" : escalation.State.LordReputation >= 40 ? "Warning" : "Alert";
```

---

## Visual Example

**Before (All White):**
```
=== COMPANY STATUS ===
READINESS: The company is prepared for action...
MORALE: The company's mood is steady...
SUPPLIES: Food is scarce. Men go hungry...
EQUIPMENT: Gear is serviceable...
REST: The company has had adequate rest...
```

**After (Color-Coded):**
```
_____ COMPANY STATUS _____              ← CYAN header (stands out)
READINESS: prepared for action...       ← TEAL label + cream text
MORALE: mood is steady...               ← TEAL label + cream text
SUPPLIES: Food is scarce...             ← TEAL label + RED text (Alert!)
EQUIPMENT: serviceable...               ← TEAL label + cream text
REST: adequate rest...                  ← TEAL label + cream text
```

---

## Coloring Rules

### Company Needs (Readiness, Morale, Equipment, Rest)

| Value Range | Color | Meaning |
|-------------|-------|---------|
| 60-100 | Default (Cream) | Satisfactory conditions |
| 40-59 | Warning (Gold) | Needs attention |
| 0-39 | Alert (Red) | Critical, immediate action required |

### Supplies

| Value Range | Color | Meaning |
|-------------|-------|---------|
| 60-100 | Success (Green) | Well-stocked |
| 40-59 | Warning (Gold) | Running low |
| 0-39 | Alert (Red) | Critical shortage |

Supplies uses green for good status because it's a critical resource players actively manage.

### Reputation

| Value Range | Color | Meaning |
|-------------|-------|---------|
| 60-100 | Success (Green) | Good standing |
| 40-59 | Warning (Gold) | Needs improvement |
| 0-39 | Alert (Red) | Dangerous territory |

---

## Design Rationale

### Why These Colors?

**Cyan Headers:** Stand out without overwhelming, creates clear section breaks for quick navigation.

**Teal Labels:** Distinct from body text, easy to scan vertically for specific status fields.

**Color-Coded Values:** Immediate visual feedback on what needs attention. Red text draws the eye to problems without reading every line.

**Warm Cream Body Text:** Softer than pure white, reduces eye strain during extended play sessions.

### Accessibility

- All colors have sufficient contrast against dark GameMenu backgrounds
- Color is supplementary to text content, not required for comprehension
- Status severity is also communicated through text descriptions

---

## Extension Points

### Adding New Colors

Edit `GUI/Brushes/EnlistedColors.xml`:

```xml
<Style Name="YourName" FontColor="#RRGGBBAA" TextGlowColor="#000000FF" FontSize="22" />
```

Use in code:
```csharp
sb.AppendLine($"<span style=\"YourName\">Your text here</span>");
```

### Changing Existing Colors

Modify the `FontColor` hex value in `EnlistedColors.xml`. Changes take effect on next game launch.

---

## Technical Notes

### Where Colors Work

Colors work in any GameMenu text that uses `RichTextWidget`, which includes:
- Menu description text (the `{REPORTS_TEXT}` variable, etc.)
- Menu option text (the button labels)

### Where Colors Don't Work

- Console output / debug logging
- Standard `TextObject` without RichText rendering
- Some native Bannerlord UI elements

### Brush Loading

The brush XML is auto-loaded from `GUI/Brushes/` by the game engine. No explicit registration in SubModule.xml is required for brush files. The brush overrides `GameMenu.InfoText` to add the custom styles to the native brush.
