# Unicode Character Fixes - January 2, 2025

## Issue
Players reported seeing unknown symbols (boxes/question marks) in flavor text for company reports, kingdom reports, and player status menus. Investigation revealed the use of Unicode emoji characters that are not supported on all systems or fonts.

## Root Cause
The localization files contained 9 instances of Unicode emoji characters and special symbols that render incorrectly on systems without full Unicode/emoji font support. These appeared primarily in muster system UI titles and menu selection symbols.

## Affected Strings

### Main Localization File (enlisted_strings.xml)
All problematic characters have been replaced with ASCII-safe alternatives:

1. **Enlisted_Symbol_Selected** (Line 561)
   - Before: `✓ {NAME}` (Unicode checkmark U+2713)
   - After: `[X] {NAME}` (ASCII brackets)

2. **Enlisted_Symbol_Unselected** (Line 562)
   - Before: `○ {NAME}` (Unicode white circle U+25CB)
   - After: `[ ] {NAME}` (ASCII brackets)

3. **muster_intro_title** (Line 1339)
   - Before: `⚔  PAY MUSTER - DAY {MUSTER_DAY}  ⚔` (Crossed swords emoji U+2694)
   - After: `===  PAY MUSTER - DAY {MUSTER_DAY}  ===` (ASCII equals signs)

4. **muster_pay_title** (Line 1366)
   - Before: `💰  PAYMASTER'S LINE  💰` (Money bag emoji U+1F4B0)
   - After: `===  PAYMASTER'S LINE  ===` (ASCII equals signs)

5. **muster_baggage_title** (Line 1392)
   - Before: `⚠️  BAGGAGE CHECK  ⚠️` (Warning sign emoji U+26A0 + variation selector)
   - After: `[!]  BAGGAGE CHECK  [!]` (ASCII brackets with exclamation)

6. **muster_recruit_title** (Line 1409)
   - Before: `👥  GREEN RECRUIT  👥` (Busts in silhouette emoji U+1F465)
   - After: `===  GREEN RECRUIT  ===` (ASCII equals signs)

7. **muster_promotion_title** (Line 1421)
   - Before: `⭐  PROMOTION ACKNOWLEDGED  ⭐` (Star emoji U+2B50)
   - After: `***  PROMOTION ACKNOWLEDGED  ***` (ASCII asterisks)

8. **muster_retinue_title** (Line 1428)
   - Before: `🏴  RETINUE MUSTER  🏴` (Black flag emoji U+1F3F4)
   - After: `===  RETINUE MUSTER  ===` (ASCII equals signs)

9. **muster_complete_title** (Line 1435)
   - Before: `⚔  MUSTER COMPLETE - DAY {MUSTER_DAY}  ⚔` (Crossed swords emoji U+2694)
   - After: `===  MUSTER COMPLETE - DAY {MUSTER_DAY}  ===` (ASCII equals signs)

### Template File (enlisted_strings_template.xml)
Fixed corrupted character encoding in lines 308-309:
- **Enlisted_Symbol_Selected**: Changed from corrupted `âœ"` to `[X]`
- **Enlisted_Symbol_Unselected**: Changed from corrupted `â—‹` to `[ ]`

## Impact
All affected strings now use only ASCII characters (codes 0-127) that are universally supported across all systems, fonts, and languages. The visual structure and intent of the UI elements are preserved while ensuring compatibility.

## Testing Recommendations
1. Launch game and verify muster system UI titles display correctly
2. Test duty/profession selection menu to verify checkbox symbols render properly
3. Verify all muster stage titles (Pay Line, Baggage Check, etc.) display correctly on various systems
4. Check that the ASCII alternatives maintain clear visual distinction and readability

## Files Modified
- `ModuleData/Languages/enlisted_strings.xml` (9 changes)
- `ModuleData/Languages/_TEMPLATE/enlisted_strings_template.xml` (2 changes)
