# Enlisted Menu Color Scheme

> Professional military intelligence brief theme for GameMenu text displays

|| |
|---|---|
| **Implemented** | December 23, 2025 |
| **Status** | Active |
| **Brush File** | `GUI/Brushes/EnlistedColors.xml` |
| **Primary Usage** | Reports menu, Company Status section |

---

## Overview

The Enlisted mod uses a semantic color palette to improve readability and create visual hierarchy in text-based GameMenu displays. The color scheme follows a "military intelligence brief" theme where section headers, status labels, and severity indicators use distinct colors for quick scanning.

---

## Color Palette

### Semantic Colors

| Style Name | Hex Color | RGB | Purpose | Usage |
|------------|-----------|-----|---------|-------|
| **Header** | `#00CCFFFF` | Cyan | Section headers | Major report sections (DAILY BRIEF, COMPANY STATUS, etc.) |
| **Label** | `#8CDBC4FF` | Teal | Information labels | Status field names (READINESS:, MORALE:, SUPPLIES:, etc.) |
| **Success** | `#90EE90FF` | Light Green | Positive status | Good supplies (60+), healthy conditions |
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

### Files Modified

1. **`GUI/Brushes/EnlistedColors.xml`** - New custom brush definition
2. **`SubModule.xml`** - Registered brush XML in `<Xmls>` section
3. **`src/Features/Interface/Behaviors/EnlistedMenuBehavior.cs`** - Updated text builders to use color spans

### Code Pattern

Section headers use the Header style:
```csharp
sb.AppendLine("<span style=\"Header\">_____ COMPANY STATUS _____</span>");
```

Status labels use the Label style (teal):
```csharp
sb.AppendLine($"<span style=\"Label\">READINESS:</span> {description}");
```

Status values use conditional colors based on severity:
```csharp
var colorStyle = value >= 60 ? "Default" : value >= 40 ? "Warning" : "Alert";
var coloredDescription = $"<span style=\"{colorStyle}\">{description}</span>";
```

---

## Visual Example

Before (All White):
```
=== COMPANY STATUS ===
READINESS: The company is prepared for action...
MORALE: The company's mood is steady...
SUPPLIES: Food is scarce. Men go hungry...
EQUIPMENT: Gear is serviceable...
REST: The company has had adequate rest...
```

After (Color-Coded):
```
_____ COMPANY STATUS _____                    ← Cyan header
READINESS: prepared for action...             ← Teal label + Cream text
MORALE: mood is steady...                     ← Teal label + Cream text
SUPPLIES: Food is scarce. Men go hungry...    ← Teal label + RED text (Alert)
EQUIPMENT: serviceable...                     ← Teal label + Cream text
REST: adequate rest...                        ← Teal label + Cream text
```

---

## Status Coloring Rules

### Readiness, Morale, Equipment, Rest
- **60-100:** Default (Cream) - Satisfactory conditions
- **40-59:** Warning (Gold) - Needs attention
- **0-39:** Alert (Red) - Critical, immediate action required

### Supplies
- **60-100:** Success (Green) - Well-stocked
- **40-59:** Warning (Gold) - Running low
- **0-39:** Alert (Red) - Critical shortage

Supplies uses green for good status because it's a critical resource that players actively manage.

---

## Design Rationale

### Why These Colors?

**Cyan Headers:** Stand out without overwhelming, creates clear section breaks for quick navigation.

**Teal Labels:** Distinct from body text, easy to scan vertically for specific status fields.

**Color-Coded Values:** Immediate visual feedback on what needs attention. Red text draws the eye to problems without needing to read every line.

**Warm Cream Body Text:** Softer than pure white, reduces eye strain during extended play sessions while maintaining excellent readability.

### Accessibility

- All colors have sufficient contrast against dark GameMenu backgrounds
- Color is supplementary to text content, not required for comprehension
- Status severity is also communicated through text descriptions

---

## Extension Points

### Adding New Colors

To add new semantic colors, edit `GUI/Brushes/EnlistedColors.xml`:

```xml
<Style Name="YourName" FontColor="#RRGGBBAA" TextGlowColor="#000000FF" FontSize="22" />
```

Use in code:
```csharp
sb.AppendLine($"<span style=\"YourName\">Your text here</span>");
```

### Style Override

To change an existing color, modify the `FontColor` hex value in `EnlistedColors.xml`. Changes take effect on next game launch (mod reload).

---

## Known Limitations

- Colors only work in `RichTextWidget` elements (GameMenu option lists, info text)
- Not compatible with standard `TextObject` console output
- Nested spans can cause rendering issues in some contexts
- Font size is fixed at 22 (GameMenu standard)

---

## Future Enhancements

Potential areas for expansion:

- **Recent Activity feed** - Color-code gains (green) vs losses (red)
- **Personal feed items** - Different colors for achievement types
- **Escalation warnings** - Red/orange for approaching thresholds
- **Campaign context** - Color lord's objective based on campaign phase

