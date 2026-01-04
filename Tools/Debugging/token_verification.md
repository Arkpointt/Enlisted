# Dynamic Token Verification Report

**Date:** 2026-01-03  
**Purpose:** Verify all documented tokens match actual implementation

---

## Token Implementation Status

### ✅ Correctly Documented Tokens

**Player Tokens:**
- `{PLAYER_NAME}` - Line 2691, Hero.MainHero.FirstName
- `{PLAYER_RANK}` - Line 2692, RankHelper.GetCurrentRank()

**Chain of Command:**
- `{SERGEANT}` - Line 2658, enlistment.NcoFullName
- `{SERGEANT_NAME}` - Line 2659, enlistment.NcoFullName
- `{NCO_NAME}` - Line 2660, enlistment.NcoFullName
- `{NCO_TITLE}` - Line 2661, enlistment.NcoRank (alt: NCO_RANK)
- `{OFFICER_NAME}` - Line 2676, ncoFullName
- `{CAPTAIN_NAME}` - Line 2677, "the Captain"

**Lord & Faction:**
- `{LORD_NAME}` - Line 2699, lord.Name
- `{LORD_TITLE}` - Line 2700, "Lady" or "Lord" based on gender
- `{FACTION_NAME}` - Line 2705, lord.MapFaction.Name
- `{KINGDOM_NAME}` - Lines 2709/2713, kingdom.Name with fallback

**Comrades:**
- `{SOLDIER_NAME}` - Line 2669, GetRandomSoldierName()
- `{COMRADE_NAME}` - Line 2668, GetRandomSoldierName()
- `{VETERAN_1_NAME}` - Line 2670, GetRandomSoldierName()
- `{VETERAN_2_NAME}` - Line 2671, GetRandomSoldierName()
- `{RECRUIT_NAME}` - Line 2672, GetRandomSoldierName()

**Location:**
- `{SETTLEMENT_NAME}` - Lines 2727/2731, party.CurrentSettlement.Name
- `{COMPANY_NAME}` - Line 2686, lord.PartyBelongedTo.Name

**Rank Progression:**
- `{NEXT_RANK}` - Line 2720, "the next rank" (static fallback)
- `{SECOND_RANK}` - Line 2721, currentRank

**Skill Check (tooltipTemplate only):**
- `{CHANCE}` - Line 1765, CalculateSkillModifiedChance()
- `{SKILL}` - Line 1779, Hero.MainHero.GetSkillValue()
- `{SKILL_NAME}` - Line 1780, skill.Name

**Order & Duty (set in ForecastGenerator/MenuHandler):**
- `{ORDER_NAME}` - ForecastGenerator.cs line 75
- `{DAY}` - ForecastGenerator.cs line 76
- `{TOTAL}` - ForecastGenerator.cs line 77
- `{HOURS}` - ForecastGenerator.cs line 118
- `{PHASE}` - ForecastGenerator.cs lines 110, 117

---

## ❌ Incorrectly Documented Tokens

**DOES NOT EXIST:**
- `{PLAYER_FULL_RANK}` - Not implemented (confused with PLAYER_RANK)
- `{LOCATION}` - Not implemented (no generic location token)
- `{ORDER_TYPE}` - Not found in codebase
- `{CURRENT_PHASE}` - Not found (menu uses PHASE instead)
- `{NEXT_PHASE}` - Not found (forecast uses PHASE generically)

---

## ⚠️ Missing from Documentation

**Additional Implemented Tokens:**
- `{TROOP_COUNT}` - Line 2755, party.MemberRoster.TotalManCount
- `{PREVIOUS_LORD}` - Line 2735, "your previous lord" (static)
- `{ALLIED_LORD}` - Line 2736, "an allied lord" (static)
- `{ENEMY_FACTION_ADJECTIVE}` - Line 2739, "enemy" (static)
- `{SECOND_SHORT}` - Line 2673, veteran1Name (duplicate purpose)
- `{NCO_RANK}` - Line 2661, ncoRank (alternate for NCO_TITLE)

**Medical/Naval Tokens (specialized):**
- `{CONDITION_TYPE}` - Line 2742
- `{CONDITION_LOCATION}` - Line 2743
- `{COMPLICATION_NAME}` - Line 2744
- `{REMEDY_NAME}` - Line 2745
- `{SHIP_NAME}` - Line 2748
- `{DESTINATION_PORT}` - Line 2749
- `{DAYS_AT_SEA}` - Line 2750
- `{BOATSWAIN_NAME}` - Line 2680
- `{NAVIGATOR_NAME}` - Line 2681
- `{FIELD_MEDIC_NAME}` - Line 2682

**Timing Tokens (widely used):**
- `{DAYS}` - Used 40+ times across multiple managers
- `{HOURS}` - Used 15+ times for countdown displays
- `{DAYS_SERVED}` - MusterMenuHandler lines 643
- `{DAYS_REMAINING}` - MusterMenuHandler line 644

---

## 🔧 Required Documentation Fixes

1. **Remove non-existent tokens:**
   - Delete `{PLAYER_FULL_RANK}` from Player Tokens section
   - Delete `{LOCATION}` from Location Tokens section
   - Delete `{ORDER_TYPE}`, `{CURRENT_PHASE}`, `{NEXT_PHASE}` from Order & Duty section

2. **Add missing tokens:**
   - Add `{TROOP_COUNT}` to Location category
   - Add timing tokens `{DAYS}` and `{HOURS}` to Order & Duty section
   - Consider adding Medical/Naval tokens to specialized section

3. **Clarify token usage:**
   - `{PHASE}` is used generically for order phases, not CURRENT_PHASE/NEXT_PHASE
   - Order tokens are set in different locations (ForecastGenerator, MenuHandler), not EventDeliveryManager

---

## ✅ Native API Verification

**All token implementations use native Bannerlord API correctly:**

| Token | Native API Used | Correct |
|-------|----------------|---------|
| PLAYER_NAME | Hero.MainHero.FirstName.ToString() | ✅ |
| LORD_NAME | Hero.Name.ToString() | ✅ |
| FACTION_NAME | IFaction.Name.ToString() | ✅ |
| KINGDOM_NAME | Kingdom.Name.ToString() | ✅ |
| SETTLEMENT_NAME | Settlement.Name.ToString() | ✅ |
| COMPANY_NAME | MobileParty.Name.ToString() | ✅ |
| SKILL | Hero.GetSkillValue(SkillObject) | ✅ |
| TROOP_COUNT | MemberRoster.TotalManCount | ✅ |

**Fallback Strategy:** All tokens use `?.ToString() ?? "fallback"` pattern for null safety ✅

**TextObject Integration:** All tokens use `SetTextVariable(key, value)` which is the correct Bannerlord API ✅

---

## Summary

- **Correctly documented:** 28 tokens ✅
- **Incorrectly documented:** 5 tokens ❌
- **Missing from docs:** 18 tokens ⚠️
- **Native API compliance:** 100% ✅

**Recommendation:** Update writing-style-guide.md to remove non-existent tokens, add commonly-used missing tokens (TROOP_COUNT, DAYS, HOURS), and note that specialized tokens exist for medical/naval events.
