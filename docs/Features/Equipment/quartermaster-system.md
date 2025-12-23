# Quartermaster System

**Summary:** The Quartermaster manages all military logistics for the player's company including equipment purchases with quality modifiers, provisions/rations, buyback services, baggage inspections, and the Officers Armory. The system uses company supply levels to gate access, and quartermaster reputation to determine pricing, equipment quality, food quality, and enforcement strictness.

**Status:** ✅ Current  
**Last Updated:** 2025-12-23  
**Related Docs:** [Company Supply Simulation](company-supply-simulation.md), [Provisions & Rations System](provisions-rations-system.md)

**System Overview:**
The Quartermaster system uses a conversation-driven interface where face-to-face dialogue opens visual equipment browsers. Equipment has quality modifiers affecting stats and prices. Players can purchase gear, upgrade equipped items, sell equipment back, and buy provisions. The system integrates deeply with company supply levels and reputation mechanics.

---

## Index

1. [Overview](#overview)
2. [Quartermaster NPC](#quartermaster-npc)
3. [Main Menu Structure](#main-menu-structure)
4. [Equipment Purchasing](#equipment-purchasing)
5. [Equipment Quality System](#equipment-quality-system)
6. [Buyback System](#buyback-system)
7. [Provisions & Rations](#provisions--rations)
8. [Baggage Checks](#baggage-checks)
9. [Officers Armory](#officers-armory)
10. [Discharge & Retirement](#discharge--retirement)
11. [Reputation System](#reputation-system)
12. [Supply Integration](#supply-integration)
13. [Implementation Status](#implementation-status)

---

## Overview

The Quartermaster is the central logistics hub for enlisted soldiers, managing:

- **Equipment Access** - Buy armor, weapons, accessories with reputation-based discounts (0-30%)
- **Equipment Quality** - Items have quality modifiers (Poor/Inferior/Common/Fine/Masterwork/Legendary)
- **Upgrade System** - Pay to improve equipped gear quality based on reputation
- **Master at Arms Integration** - Category-filtered equipment browsing (armor/weapons/accessories)
- **Enhanced UI** - Tooltips show stat modifiers, upgrade indicators, color-coded quality tiers
- **Buyback Services** - Sell QM-purchased equipment back at 30-65% of value (quality-aware)
- **Food Rations (T1-T4)** - Issued rations every 12 days, quality based on reputation
- **Officer Provisions (T5+)** - Premium food shop at 150-200% of town prices
- **Baggage Inspections** - Muster contraband checks with reputation-based outcomes
- **Officers Armory** - Elite equipment with quality modifiers (T5+, high reputation)
- **Supply Gating** - Company supply < 30% blocks equipment changes

**Core Relationships:**

| System | Effect on Player |
|--------|------------------|
| **Company Supply** | < 30%: Equipment blocked; affects ration availability |
| **QM Reputation** | -50 to +100: Equipment discounts, buyback rates, food quality |
| **Player Tier** | T1-T4: Issued rations; T5+: Buy provisions |
| **Soldier Reputation** | + QM Rep ≥ 110: Unlocks Officers Armory |

---

## Quartermaster NPC

### NPC Generation

The quartermaster is assigned when the player enlists:

```csharp
private void OnPlayerEnlisted(Hero lord)
{
    // Try to find existing quartermaster in lord's retinue
    Hero quartermaster = FindQuartermasterInRetinue(lord);
    
    if (quartermaster == null)
    {
        // Generate new quartermaster NPC
        quartermaster = GenerateQuartermaster(lord);
        lord.Clan.AddCompanion(quartermaster);
    }
    
    _assignedQuartermaster = quartermaster;
}
```

### NPC Characteristics

- **Age:** 35-50 (experienced veteran)
- **Skills:** High Steward, Trade (logistics specialist)
- **Role:** Non-combatant, stays in lord's party
- **Archetype:** Varies by culture (veteran/merchant/bookkeeper/scoundrel/believer/eccentric)
- **Starting Relationship:** Neutral (0)

### Accessing the Quartermaster

The Quartermaster is accessed through **face-to-face conversation** (not text menus). The conversation opens the visual Gauntlet UI for equipment browsing.

**Access Methods:**
1. **In Camp:** "Speak with the Quartermaster" option in camp menu
2. **Conversation Flow:** Player selects category → Gauntlet UI opens → Browse/purchase → Return to conversation hub

**Opening Greeting:**

The QM's greeting varies dynamically based on multiple factors:

- **Archetype Personality:** Each QM has a distinct voice (Veteran: military direct, Merchant: business-focused, Bookkeeper: precise logistics, Scoundrel: informal deals, Believer: faith-based, Eccentric: superstitious)
- **Reputation Tone:** 
  - Hostile (< 0): Terse, unwelcoming
  - Neutral (0-30): Professional but distant
  - Friendly (31-60): More detailed, helpful
  - Trusted (61+): Candid, includes advice
- **Supply Level Context:** QM mentions supply situation in greeting when notable
- **Mood:** Based on camp morale and supply stress
- **Strategic Context:** References current army operations (winter, siege prep, raiding, battle)

### Contextual Dialogue System

The QM responds intelligently to game context with over 150 localized dialogue strings covering different scenarios:

**Supply Inquiry Response:**

When player asks "How are our supplies?", the QM provides a detailed report that varies by:

- **Archetype Flavor:** Each personality type describes supplies differently
  - Veteran: "Supplies are solid" vs "Running low"
  - Merchant: "Stock is excellent" vs "Margins are getting tight"
  - Bookkeeper: "Supply levels: 80% or above" vs "CRITICAL"
  - Scoundrel: "We're flush" vs "Scraping the bottom"
  - Believer: "The Lord provides" vs "We face a trial"
  - Eccentric: "The stars align favorably" vs "Dark portents"

- **Supply Level Categories:**
  - Excellent (80+): Positive, confident tone
  - Good (60-79): Adequate, watchful tone
  - Fair (40-59): Concerned, recommends action
  - Low (20-39): Urgent, emphasizes need
  - Critical (<20): Alarmed, demands immediate action

- **Additional Context Notes:**
  - Equipment condition mentioned if significantly worse than supplies
  - Morale mentioned if critically low (<30) or exceptionally high (80+)
  - Strategic context added (winter, siege prep, raiding, long march)

**Browse Responses:**

Equipment browsing dialogue reflects current conditions:
- **Critical Supplies:** QM warns about slim pickings
- **Low Equipment:** Mentions worn/rough gear condition
- **Hostile Reputation:** Terse, unwelcoming responses
- **Trusted Reputation:** Friendly, offers "the good stuff"

**Sell Responses:**

Selling dialogue varies by mood and context:
- **Content Mood:** Welcoming, fair pricing mentioned
- **Stressed Mood:** Busy, asks to make it quick
- **Grim Mood:** Warns prices are low, supply issues
- **Low Supplies:** Additional warnings about not buying much

**Upgrade Responses:**

Upgrade dialogue reflects relationship and archetype:
- **Trusted:** Enthusiastic, emphasizes quality
- **Hostile:** Demands payment upfront, no favors
- **Neutral:** Professional, discusses pricing
- **Archetype Variations:** Each personality has unique approach to craftsmanship

**Technical Implementation:**
- ~150 dialogue strings in XML for full localization support
- Dynamic string IDs built from game state: `qm_supply_{archetype}_{level}_{reptone}`
- Comprehensive error handling with fallback strings
- Safe helpers prevent crashes from invalid data (null checks, value clamping, archetype validation)

---

## Conversation Flow

### Main Conversation Hub

The quartermaster system uses a conversation-driven interface where dialogue options open the visual equipment browser:

```
[Player approaches Quartermaster]

QM: [Greeting based on archetype, reputation, supply level, mood]

Player Options:
  → "I need equipment."
  → "I want to upgrade my gear."
  → "I want to sell something."
  → "I need provisions." (T5+ only)
  → "How are our supplies?"
  → "Nothing for now."
```

### Equipment Category Selection

When player selects "I need equipment.":

```
QM: [Browse response based on supply/reputation/mood]

Player Options:
  → "Weapons."
  → "Armor."
  → "Accessories."
  → "Mounts."
  → "Never mind."
```

### Armor Slot Drill-Down

Armor requires slot selection:

```
QM: "What piece are you looking for?"

Player Options:
  → "Body armor."
  → "Helmet."
  → "Gloves."
  → "Boots."
  → "Cape or cloak."
  → "Something else entirely."
```

### Gauntlet UI Opens

After category/slot selection, the conversation closes and the **visual Gauntlet equipment grid** opens:

- Shows available equipment with quality modifiers
- Displays prices (modified by quality and reputation)
- Shows stats and tooltips
- Player browses, purchases, or exits
- On exit, conversation resumes at hub for continued shopping

### Supply Inquiry (Pure Dialogue)

When player asks "How are our supplies?":

```
QM: [Detailed contextual report - see Contextual Dialogue System section]

[Returns to conversation hub - no UI opens]
```

### Example Full Interaction

```
Player: [Visits Quartermaster]

QM (Veteran, Friendly, Good Supply): 
    "Good to see you. Supplies are holding steady, and I've 
     got decent kit in stock."

Player: "I need equipment."

QM: "Let me see what's in stock for you."

Player: "Armor."

QM: "What piece are you looking for?"

Player: "Body armor."

QM: "Right. Let's see what we've got."

[Gauntlet UI opens showing body armor options]
[Player browses, selects Worn Vlandian Sergeant Armor - 416g (20% discount)]
[Player purchases, UI closes]

QM: "Anything else?"

Player: "How are our supplies?"

QM: "Supplies are at 65%. Adequate for now, but we're watching it. 
     Equipment condition is a bit rough - lot of worn pieces - but 
     serviceable."

Player: "That's all for now."

QM: "Come back when you need something."

[Conversation ends]
```

---

## Equipment Purchasing

### Category-Filtered Browsing

Each purchase option opens the Master at Arms UI with category filtering:

**Buy Armor:**
```
=== BUY ARMOR ===

Available Troops (filtered by your rank and formation):
  [Vlandian Squire] - Cost: 280g (20% discount)
  [Vlandian Sergeant] - Cost: 520g (20% discount)
  [Vlandian Knight] - Cost: 1240g (20% discount) ← Requires T5+
  
Current Equipment: Vlandian Recruit (T1)
Current Loadout Value: 150g

Filters:
  [Show: Armor Only]
  [Your Formation: Infantry]
  [Your Rank: T3 - Available up to Sergeant]
```

### Pricing Structure

**Equipment Purchase Price:**
```
finalPrice = item.Value × repMultiplier × campMoodMultiplier × dutyDiscountMultiplier
```

| QM Rep Band | Multiplier | Meaning |
|-------------|------------|---------|
| -50 to -25 | 1.4x | 40% markup |
| -25 to -10 | 1.2x | 20% markup |
| -10 to 10 | 1.0x | Standard |
| 10 to 35 | 0.9x | 10% discount |
| 35 to 65 | 0.8x | 20% discount |
| 65 to 100 | 0.7x | 30% discount |

**Additional Modifiers:**
- **Camp Mood:** 0.98-1.15 (morale affects prices slightly)
- **Duty Discount:** 0.85 for provisioner/QM officer roles
- **Supply Gate:** Company Supply < 30% blocks all purchases

### Purchase Restrictions

**Blocked Scenarios:**
- Company supply < 30%: "We can't issue equipment right now. Supplies are critically low."
- Insufficient funds: "You don't have enough. Come back when you've got the coin."
- Too high tier: "That equipment is above your rank."

---

## Equipment Quality System

All equipment in the Quartermaster's stock has quality modifiers applied, using Bannerlord's native `ItemModifier` system. Quality affects item stats (damage, armor, speed) and prices.

### Quality Tiers

| Tier | Display Name | Color | Price Multiplier | Stats |
|------|--------------|-------|------------------|-------|
| **Poor** | Poor | Gray | ~0.5x | Reduced stats (damaged/rusty) |
| **Inferior** | Worn | Brown | ~0.7-0.85x | Below average |
| **Common** | Standard | White | 1.0x | No modifier (baseline) |
| **Fine** | Fine | Green | ~1.2x | Above average |
| **Masterwork** | Masterwork | Blue | ~1.5x | High quality |
| **Legendary** | Legendary | Gold | ~2.0x | Exceptional |

### Quality Distribution by QM Reputation

Quality is rolled when equipment variants are built (affected by `RollItemQualityByReputation()`):

| QM Reputation | Poor | Inferior | Common | Fine+ |
|---------------|------|----------|--------|-------|
| **< 0** | 50% | 40% | 10% | 0% |
| **0-30** | 30% | 50% | 20% | 0% |
| **31-60** | 15% | 45% | 35% | 5% |
| **61+** | 5% | 30% | 50% | 15% |

**Key Points:**
- Low reputation = mostly damaged gear (Poor/Inferior)
- High reputation = better stock (Common/Fine)
- Masterwork/Legendary quality comes from upgrade system (Phase 3, future)

### Quality Effects

**Stat Modifications:**
- **Weapons:** Damage, speed, reach affected by quality
- **Armor:** Armor values modified (Poor = reduced protection, Fine = enhanced)
- **Prices:** Quality modifier's `PriceMultiplier` applied before reputation discounts

**Example:**
```
Base Item: Bastard Sword (500 denars, 35 swing damage)

Poor Quality (Rusty Bastard Sword):
- Price: 250 denars (0.5x modifier × 500)
- Damage: ~28 swing (reduced by modifier)

Fine Quality (Fine Bastard Sword):
- Price: 600 denars (1.2x modifier × 500)
- Damage: ~38 swing (increased by modifier)
```

### UI Display

**In Gauntlet Equipment Grid:**
- Item name shows quality prefix: "Rusty Shortsword", "Fine Bastard Sword"
- Quality tier displayed below item name with color coding
- Color-coded quality badges with text labels for accessibility:
  - Poor: Light gray (#909090FF) - "Poor"
  - Inferior: Peru/tan (#CD853FFF) - "Worn"
  - Common: Off-white (#E8E8E8FF) - "Standard"
  - Fine: Light green (#90EE90FF) - "Fine"
  - Masterwork: Cornflower blue (#6495EDFF) - "Masterwork"
  - Legendary: Gold (#FFD700FF) - "Legendary"
- Prices reflect both quality modifier and reputation discount
- Stats shown are modified values (what player will actually get)
- **Tooltips** show detailed stat breakdown:
  - Quality name
  - Weapon: Damage, speed, missile speed modifiers
  - Armor: Armor value modifiers
  - Price multiplier percentage
- **Upgrade Indicators:** Gold "UPGRADE" badge on upgradeable items (top-right corner)

**Items Without Modifier Groups:**
- Some items don't support quality modifiers (banners, quest items)
- Display as "Standard" quality with no color coding
- Not shown in upgrade interface
- Price calculations and purchase work normally

### Stock Floor

To prevent bad RNG from blocking player progress, at least 1 item per major equipment slot is guaranteed to be in stock after rolling availability:
- Weapon0, Head, Body, Leg, Gloves, Cape, Horse slots checked
- If all items out of stock in a slot, the first item is marked available

### Purchase with Quality

When purchasing equipment:
1. Quality modifier applied to equipped item via `EquipmentElement(item, modifier)`
2. Player receives the actual modified item with adjusted stats
3. Success message shows modified name: "Purchased Rusty Shortsword for 120 denars"
4. If weapon slots full, item goes to inventory with modifier preserved

---

## Equipment Upgrade System

Players can pay the Quartermaster to improve the quality of their currently equipped gear. This is an alternative to buying new equipment when supply is low or reputation is insufficient for Officers Armory access.

### Accessing Upgrades

**Conversation Option:**
```
Player: "I want to upgrade my gear."

QM: [Upgrade response based on reputation and archetype]
```

**Upgrade Response Examples:**
- **Trusted Rep:** "Bring me what you've got. I'll make it shine. My work's expensive but worth it."
- **Hostile Rep:** "Fine. Gold up front. Don't expect any favors."
- **Neutral Rep:** "Let's see what you have. Upgrades aren't cheap."

**Opens Upgrade Interface** showing all equipped items with available upgrade paths.

### Upgrade Interface

```
=== IMPROVE EQUIPMENT ===

Your Equipped Items:

[Bastard Sword]
  Current: Worn (Inferior quality)
  Available Upgrades:
    → Standard (Common) - 85g
    → Fine - 320g
    → Masterwork - 640g (LOCKED: Requires Rep 30+)

[Vlandian Scale Armor]
  Current: Standard (Common quality)
  Available Upgrades:
    → Fine - 280g
    → Masterwork - 560g (LOCKED: Requires Rep 30+)
    → Legendary - 1120g (LOCKED: Requires Rep 61+)

[Imperial Helmet]
  Current: Fine quality
  Available Upgrades:
    → Masterwork - 180g
    → Legendary - 540g (LOCKED: Requires Rep 61+)

[Worn Boots]
  Already at maximum quality for this item.

[Banner]
  This item cannot be improved.

[Done] [Back to Conversation]
```

### Upgrade Cost Formula

```
UpgradeCost = (TargetQualityPrice - CurrentQualityPrice) × ServiceMarkup

Where:
- TargetQualityPrice = BaseValue × TargetModifier.PriceMultiplier
- CurrentQualityPrice = BaseValue × CurrentModifier.PriceMultiplier (or BaseValue if no modifier)
- ServiceMarkup varies by reputation
```

### Reputation Effects on Upgrades

| QM Reputation | Service Markup | Available Tiers | Example (500g sword, Common → Fine) |
|---------------|----------------|-----------------|-------------------------------------|
| < 30 | 2.0× | Fine only | 600g |
| 30-60 | 1.5× | Fine, Masterwork | 450g |
| 61+ | 1.25× | Fine, Masterwork, Legendary | 375g |

**Key Points:**
- Low reputation: Higher costs, limited to Fine quality
- High reputation: Better prices, access to Masterwork/Legendary
- Service markup applied to price difference only
- Some items may not have all quality tiers available

### Upgrade Restrictions

**Items That Cannot Be Upgraded:**
- Items without modifier groups (banners, quest items)
- Items already at Legendary quality
- Items already at maximum quality tier for their modifier group

**Reputation Locks:**
- Masterwork: Requires QM Rep 30+
- Legendary: Requires QM Rep 61+

**Transaction Validation:**
- Must have sufficient gold
- Must still be enlisted (service hasn't ended)
- Reputation requirement must be met at transaction time

### Edge Case Handling

**External Interruptions:**
- Battle start: Upgrade screen force-closes
- Player capture: Screen closes gracefully
- Settlement departure: Screen closes
- Save/Load: Screen closes automatically

**Service End During Upgrade:**
- Transaction blocked if enlistment ends
- Clear error message: "You are no longer enlisted. Service has ended."

**Reputation Change During Screen:**
- Double-checked at transaction time
- If dropped below requirement: "The quartermaster will no longer perform this upgrade. Your standing may have changed."

**Overflow Protection:**
- Very expensive items (>500k base value) use safe arithmetic
- Costs clamped to int.MaxValue with warning log

**Tracking Purchases:**
All quartermaster-issued equipment is tracked in a persistent registry for:
- Buyback restrictions (only QM-issued items can be sold back)
- Discharge reclamation (all QM gear returned when leaving service)
- Missing gear penalties (fines if items lost/sold)

---

## Buyback System

### Sell Equipment Back

Only quartermaster-purchased equipment appears in the buyback menu:

```
=== SELL EQUIPMENT BACK ===

QM Reputation: +45 (Friendly) - Buyback Rate: 60%

Your QM-Purchased Equipment:
  [Vlandian Sword] - Town Value: 200g, Buyback: 120g
  [Imperial Scale Armor] - Town Value: 450g, Buyback: 270g
  [Heavy Cavalry Helmet] - Town Value: 180g, Buyback: 108g
  
Total Buyback Value: 498g

⚠️ Note: Selling back always results in a loss. Higher reputation minimizes loss.

[Select item to sell] [Sell All] [Back]
```

### Buyback Rates by Reputation

| QM Rep Band | Multiplier | Example (200g sword) |
|-------------|------------|---------------------|
| -50 to -25 | 0.30x | 60g |
| -25 to -10 | 0.40x | 80g |
| -10 to 10 | 0.50x | 100g |
| 10 to 35 | 0.55x | 110g |
| 35 to 65 | 0.60x | 120g |
| 65 to 100 | 0.65x | 130g |

**Anti-Exploit Design:**
- Best buy multiplier: 0.70x (30% discount)
- Best sell multiplier: 0.65x (35% loss)
- **Buyback is always a loss**, even at maximum reputation

---

## Provisions & Rations

### T1-T4: Issued Rations System

**Ration Exchange at Muster (Every 12 Days):**

1. **Reclaim old issued ration** (if issuing a new one)
2. **Check supply availability** (based on company supply)
3. **Issue new ration** (if available) or none (if supplies low)

**Ration Availability by Company Supply:**

| Company Supply | Ration Chance | Player Experience |
|----------------|---------------|-------------------|
| 70-100% | 100% | Always get rations |
| 50-69% | 80% | Occasional shortages |
| 30-49% | 50% | Frequent shortages |
| < 30% | 0% | No rations ever |

**Ration Quality by QM Reputation:**

| QM Reputation | Food Item | Value | Flavor |
|---------------|-----------|-------|--------|
| -50 to -10 | grain | 10g | "Moldy and barely edible" |
| -9 to 19 | grain | 10g | "Standard rations" |
| 20 to 49 | butter | 40g | "Better than grain" |
| 50 to 79 | cheese | 50g | "Quartermaster is generous" |
| 80 to 100 | meat | 30g | "Quartermaster favors you" |

**Key Features:**
- Issued rations are **tracked** and reclaimed at each muster (can't accumulate)
- Personal food (bought/foraged) is **never reclaimed**
- Rations immune to loss events (rats, spoilage, theft)
- Player must supplement with personal food during shortages

### T5+: Officer Provisions Shop

**Transition at T5 Promotion:**
```
Quartermaster: "Congratulations on your promotion, Lieutenant.
    
As an officer, you're no longer issued rations. You're expected to 
manage your own provisions.
    
I have a shop available - prices are higher than town markets, but 
it's convenient when you're in the field. Stock refreshes every muster."

[Reclaims any issued rations]
```

**Shop Pricing:**

| QM Reputation | Price Multiplier | Example (Grain: 10g base) |
|---------------|------------------|---------------------------|
| -50 to 0 | 2.0x (200%) | 20g |
| 1 to 30 | 1.9x (190%) | 19g |
| 31 to 60 | 1.75x (175%) | 17.5g |
| 61 to 100 | **1.5x (150%)** | **15g (minimum)** |

**Inventory by Supply Level:**

**High Supply (70-100%):**
- Grain (6), Butter (4), Cheese (3), Meat (3), Fish (2)

**Medium Supply (50-69%):**
- Grain (4), Butter (2), Meat (2)

**Low Supply (30-49%):**
- Grain (2), Butter (1)

**Critical Supply (<30%):**
- Grain (2) only

**Shop UI:**
```
=== QUARTERMASTER PROVISIONS ===

Company Supply: 75% (Good stock)
Your Reputation: +45 (Friendly - 175% of market prices)

Available Provisions (refreshes next muster in 8 days):
  [Grain] - 6 available - 17g each (market: 10g)
  [Butter] - 4 available - 70g each (market: 40g)
  [Cheese] - 3 available - 87g each (market: 50g)
  [Meat] - 3 available - 52g each (market: 30g)
  [Fish] - 2 available - 61g each (market: 35g)

Your Gold: 1250g
Days of Food Remaining: 12 days

💡 Town markets are cheaper but require travel.
```

---

## Baggage Checks

### Muster Inspections

Every pay muster has a **30% chance** of triggering a baggage inspection:

```
Quartermaster: "Routine inspection."

[Searches bags]

[Outcome depends on contraband found and QM reputation]
```

### Contraband Detection

The quartermaster scans for:
- Illegal goods (smuggled items)
- Stolen gear (high-value civilian goods)
- Flagged contraband items
- (Optional) Missing QM-issued items

### Outcomes by Reputation

| Scenario | QM Rep | Event ID | Outcome |
|----------|--------|----------|---------|
| No contraband | Any | `evt_baggage_clear` | Pass inspection |
| Contraband + Trusted | 65+ | `evt_baggage_lookaway` | QM looks away, no penalty |
| Contraband + Friendly | 35-65 | `evt_baggage_bribe` | Bribe option (50-75% of value) |
| Contraband + Neutral | < 35 | `evt_baggage_confiscate` | Confiscation + fine + scrutiny |
| Contraband + Hostile | < -25 | `evt_baggage_report` | Severe penalties |

**Example: High Rep Outcome**
```
QM: [Glances at your pack, sees valuable contraband]
    [Looks around]
    [Looks back at you]
    
    "I didn't see anything. But be more careful."

[Looks the other way - no penalty]
```

**Example: Low Rep Outcome**
```
QM: "Caught you. This is going in my report."

[Confiscates item]
[+10 Scrutiny]
[Fine of 150g]
[-5 QM Rep]

QM: "Don't let me catch you again."
```

---

## Officers Armory

### Access Requirements

**Eligibility Gate:**
- Player Rank: **T5+**
- QM Reputation: **60+**
- Soldier Reputation: **50+**

### First-Time Access

```
QM: "You've proven yourself, Lieutenant. Your reputation precedes you.
    
I have access to... special inventory. Elite equipment reserved for 
officers with the right connections.
    
These pieces have quality modifiers - better than standard issue. 
But they cost more, and this is a privilege. Don't abuse it."
```

### Elite Equipment

```
=== OFFICERS ARMORY ===

⚜️ ELITE EQUIPMENT - Officers Only ⚜️

You have earned access to premium equipment with quality modifiers.

Available Elite Loadouts:
  [Vlandian Knight] (Fine Quality) - 1450g
    * +10% armor, +5% weapon damage
  
  [Imperial Legionary] (Masterwork) - 1850g
    * +15% armor, +10% weapon damage
    
⚠️ This is a privilege. Don't abuse it.

[Select loadout] [Preview] [Back]
```

### Quality Modifiers

The system uses Bannerlord's native `ItemModifier` system:

- **Fine:** +10% armor, +5% damage
- **Masterwork:** +15% armor, +10% damage

### Losing Access

If reputation drops below 60:

```
QM: "Your reputation isn't what it used to be. 
    Officers Armory is off limits until you rebuild trust."

[Access denied]
```

---

## Discharge & Retirement

### Equipment Reclamation

**Standard Discharge (Has QM-Purchased Gear):**
```
QM: "Leaving us, eh? I'll need all military equipment back before you go."

[Scans inventory for QM-purchased items]

QM: "Let's see... I'm reclaiming:"
    - [List of QM-purchased items]

[Items removed from inventory]

QM: "Equipment returned. Good luck out there."
```

**Missing Gear:**
```
QM: "You're missing some gear from my records. Where is it?"

[Player sold/lost QM-purchased items]

QM: "You owe [REPLACEMENT_COST]g for missing equipment."

[Deducts from final pay/pension]
```

**High Rep Discharge (65+):**
```
QM: "Sorry to see you go, [Name]. You were a good soldier.
    
I'll need the military equipment back, but..."
    
[Lowers voice]
    
"...if some of that looted gear you've got happens to stay with you, 
 I didn't see it. Consider it a parting gift."

[Reclaims QM-purchased items only]
[Winks]
```

### Ration Reclamation

**T1-T4 Enlisted:**
- All issued rations are reclaimed (tracked items)
- Personal food (bought/foraged) is retained
- No penalty for consumed rations

**T5+ Officers:**
- No rations to reclaim (officers don't receive issued rations)
- Any purchased provisions remain personal property

---

## Reputation System

### Reputation Range

**QM Reputation:** -50 to +100

### Reputation Effects

| Rep Band | Name | Equipment Price | Buyback Rate | Food Quality | Baggage Check |
|----------|------|----------------|--------------|--------------|---------------|
| 80-100 | Trusted | 0.7x (30% off) | 0.65x | Meat | Looks away |
| 65-79 | Friendly | 0.7x (30% off) | 0.65x | Cheese | Looks away |
| 35-64 | Friendly | 0.8x (20% off) | 0.60x | Cheese | Bribe option |
| 10-34 | Neutral | 0.9x (10% off) | 0.55x | Butter | Bribe option |
| -9 to 9 | Neutral | 1.0x (standard) | 0.50x | Grain | Confiscation |
| -24 to -10 | Unfriendly | 1.2x (20% markup) | 0.40x | Grain | Confiscation |
| -50 to -25 | Hostile | 1.4x (40% markup) | 0.30x | Grain | Severe penalties |

### Building Reputation

**Positive Gains:**
- Complete equipment transactions (+1-2 per purchase)
- Chat with quartermaster (+5)
- High-quality service (+5-10)
- Buying provisions regularly (+1 per transaction)

**Negative Losses:**
- Caught with contraband (-5 to -10)
- Missing equipment at discharge (-10)
- Complaints about pricing (-2)
- Refusing reasonable requests (-5)

### Reputation Milestones

**Reaching +50 (Friendly):**
```
QM: "You're proving yourself to be reliable. Keep it up."
[Unlocks: Better buyback rates]
```

**Reaching +75 (Trusted):**
```
QM: "I've got to say, you're one of the best soldiers I've worked with.
    If you need anything, just ask. Within reason, of course."
[Unlocks: Looks other way on contraband]
```

**Dropping Below -25 (Hostile):**
```
QM: "I'm watching you. One more incident and I'm recommending discharge."
[+5 Scrutiny]
```

---

## Supply Integration

### Company Supply System

The Quartermaster system is tightly integrated with company supply (0-100%):

**Supply Sources:**
- 40% observed from lord's food situation
- 60% simulated from logistics (movement, combat, terrain)

**Supply Effects on Quartermaster:**

| Supply Level | Equipment Access | Ration Availability (T1-T4) | Officer Shop Stock (T5+) |
|--------------|-----------------|----------------------------|--------------------------|
| 70-100% | Full access | 100% | Full stock (5 types) |
| 50-69% | Full access | 80% | Limited (3 types) |
| 30-49% | Full access | 50% | Minimal (2 types) |
| < 30% | **BLOCKED** | 0% | Critical (1 type) |

### Supply-Based Messaging

**Supply Inquiry Dialogue:**

```
Player: "How are our supplies?"

QM (70%+): "Supplies are good. No concerns at the moment."

QM (50-69%): "We're running a bit low. Nothing critical yet, but keep 
             an eye on it. Complete some supply duties if you can."

QM (30-49%): "Supply situation is concerning. We're rationing carefully. 
             If this continues, we'll have to halt equipment changes."

QM (<30%): "We're in crisis. I can't issue any equipment changes until 
           we resupply. Food rations are suspended. Get supplies NOW."
```

---

## Implementation Status

### Current System Features

**Conversation Interface:**
- Face-to-face dialogue with the Quartermaster NPC
- Category selection for equipment types (weapons, armor, accessories, mounts)
- Armor purchases include slot drill-down (helmet, body, gloves, boots, cape)
- Supply inquiry provides dynamic contextual reports
- Dialogue opens Gauntlet visual equipment browser
- Smooth transitions between conversation and UI with return flow
- External interruptions handled gracefully (battle, capture, settlement departure, save/load)

**Equipment Quality System:**
- Six quality tiers (Poor/Inferior/Common/Fine/Masterwork/Legendary)
- Native ItemModifier integration affects item stats and prices
- Quality distribution based on quartermaster reputation (low rep = damaged gear)
- Quality modifiers applied to purchased items (both equipped and inventory)
- Items without modifier groups display as Common quality
- Stock floor guarantees at least 1 item per major equipment slot

**Upgrade System:**
- Players can pay to improve quality of equipped gear
- Reputation gates access to higher tiers (Masterwork requires 30+ rep, Legendary requires 61+ rep)
- Upgrade costs use service markup formula based on reputation
- Robust error handling for external interruptions and invalid states
- Transaction validation prevents upgrades after service ends or reputation drops
- Proper cleanup prevents memory leaks

**User Interface:**
- Color-coded quality display with text labels for accessibility
- Tooltips show base vs modified stats with detailed breakdowns
- Upgrade indicators appear on items that can be improved
- Readable color palette (light gray, peru/tan, off-white, light green, cornflower blue, gold)
- Long item names truncated to prevent overlap
- Quality-aware pricing throughout (purchase and buyback)
- Sell functionality uses popup inquiry with quality variants tracked separately

**Contextual Dialogue:**
- Quartermaster responses vary by archetype personality (Veteran/Merchant/Bookkeeper/Scoundrel/Believer/Eccentric)
- Tone adjusts based on reputation (Hostile/Neutral/Friendly/Trusted)
- Supply level affects responses (Excellent/Good/Fair/Low/Critical)
- Strategic context referenced when relevant (winter, siege prep, raiding, battle, long march)
- Approximately 150 dialogue strings in XML for full localization
- Error handling with fallback strings ensures dialogue never breaks
- Input validation prevents crashes from invalid game state

**Core Logistics:**
- Quartermaster NPC generated and assigned when player enlists
- Equipment purchase with quality modifiers and reputation discounts
- Category filtering for browsing (armor/weapons/accessories/mounts)
- Reputation-based pricing (0-30% discount range)
- Supply level gating (company supply < 30% blocks equipment access)
- Buyback system accepts only quartermaster-issued equipment
- Provisions and rations (T1-T4 receive issued rations, T5+ purchase from shop)

### Planned Future Enhancements

**Camp News Integration:**
- Daily Report entries for quartermaster events
- Track significant transactions (upgrades, stock changes)
- News templates for equipment activities
- Camp News section shows current quartermaster status
- Appropriate priority weighting relative to combat events

**Additional Features:**
- Baggage inspections at muster with contraband detection
- Enhanced ration exchange tracking for enlisted soldiers
- Officer provisions shop improvements
- Discharge equipment reclamation with missing gear penalties
- Expanded reputation range (-50 to +100)

### Related Documentation

- **[Company Supply Simulation](company-supply-simulation.md)** - How company supply is calculated and affects quartermaster access
- **[Provisions & Rations System](provisions-rations-system.md)** - Detailed food mechanics for T1-T4 issued rations and T5+ officer provisions
- **[Quartermaster Hero System](../Core/quartermaster-hero-system.md)** - Quartermaster NPC generation and assignment

### Source Files

| File | Purpose |
|------|---------|
| `src/Features/Equipment/Behaviors/QuartermasterManager.cs` | Core quartermaster logic |
| `src/Features/Equipment/Behaviors/TroopSelectionManager.cs` | Equipment selection integration |
| `src/Features/Equipment/UI/QuartermasterEquipmentSelectorVM.cs` | Main UI view model |
| `src/Features/Equipment/UI/QuartermasterEquipmentItemVM.cs` | Individual item view model |
| `src/Features/Equipment/UI/QuartermasterEquipmentSelectorBehavior.cs` | UI behavior controller |
| `src/Features/Equipment/UI/QuartermasterEquipmentRowVM.cs` | Row display view model |

---

**End of Document**

