# Quartermaster System

**Summary:** The Quartermaster manages all military logistics for the player's company including equipment purchases with quality modifiers, provisions/rations, buyback services, baggage inspections, and the Officers Armory. The system uses a data-driven dialogue engine with dynamic runtime context evaluation, allowing the Quartermaster to react to supply levels, reputation, and company events in real-time.

**Status:** ✅ Current  
**Last Updated:** 2025-12-23 (Dialogue flow restructured: two-level hub with dynamic contextual responses)  
**Related Docs:** [Company Supply Simulation](company-supply-simulation.md), [Provisions & Rations System](provisions-rations-system.md)

**System Overview:**
The Quartermaster system uses a conversation-driven interface where face-to-face dialogue opens visual equipment browsers. The QM provides dynamic contextual responses that inform the player about supply status, their rank, reputation standing, and discount percentages before showing equipment categories. Equipment has quality modifiers affecting stats and prices. Players can purchase gear, upgrade equipped items, sell equipment back, and buy provisions. The system integrates deeply with company supply levels and reputation mechanics through a context-aware dialogue catalog.

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
- **Officers Armory** - Elite equipment with quality modifiers (T7+, high reputation)
- **Supply Gating** - Company supply < 30% blocks equipment changes
- **Context-Aware Dialogue** - Real-time reaction to supply levels and events during conversation

**Core Relationships:**

| System | Effect on Player |
|--------|------------------|
| **Company Supply** | < 30%: Equipment blocked; affects greetings and responses |
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

- **Player Rank (Primary Factor):**
  - **T7+ Officers:** Deferential "my lord" treatment, respectful tone
  - **T5-T6 NCOs:** Addressed by rank title (Sergeant, Decanus, etc.), professional respect
  - **T1-T4 Enlisted:** Informal "soldier" address, casual peer-level tone
- **Archetype Personality:** Each QM has a distinct voice (Veteran: military direct, Merchant: business-focused, Bookkeeper: precise logistics, Scoundrel: informal deals, Believer: faith-based, Eccentric: superstitious)
- **First Meeting vs Returning:** New recruits get an introduction ("New face. What do they call you?"), while returning visits are more familiar
- **PayTension Level:** High tension (60+) or critical tension (80+) overrides normal greetings with warnings about pay issues and potential mutiny
- **Reputation Tone:** 
  - Hostile (< 0): Terse, unwelcoming
  - Neutral (0-30): Professional but distant
  - Friendly (31-60): More detailed, helpful
  - Trusted (61+): Candid, includes advice
- **Supply Level Context:** QM mentions supply situation in greeting when notable
- **Mood:** Based on camp morale and supply stress
- **Strategic Context:** References current army operations (winter, siege prep, raiding, battle)

### Contextual Dialogue System

The QM responds intelligently to game context using a JSON-driven dialogue catalog with over 150 localized strings:

**Dynamic Context Evaluation:**
The system evaluates the game state (supply, events, reputation) at **runtime** every time a dialogue node is displayed. This ensures that if supplies are consumed during a visit, the QM's greetings and responses update immediately.

**Supply Categories & Responses:**

| Level | Range | Tone | Example Greeting (by Rank) |
|-------|-------|------|---------------------------|
| **Excellent** | 80-100% | Positive | Officer: "My lord. Good to see you. What do you require?"<br>NCO: "Sergeant. Good to see you. What do you need?"<br>Enlisted: "Back again? What do you need, soldier?" |
| **Good** | 60-79% | Adequate | Officer: "My lord. Your requisition forms are in order."<br>NCO: "Sergeant. Equipment requisition? Let me find the form."<br>Enlisted: "Soldier. Equipment requisition? Let me find the form." |
| **Fair** | 40-59% | Concerned | "What can I do for you?" (rank-neutral when stressed) |
| **Low** | 20-39% | Urgent | "Stock's running thin. What do you need?" (rank-neutral when stressed) |
| **Critical** | 0-19% | Alarmed | "We've next to nothing left. Make it quick." (rank-neutral in crisis) |

**Browse Responses:**
Equipment browsing dialogue reflects multiple contextual factors:
- **Supply Status:** "Plenty in stock" (80-100%) / "Fair bit" (60-79%) / "Stock's thin" (20-39%) / "Pickings are slim" (0-19%)
- **Rank Acknowledgment:** Officers (T7+) are always addressed as "my lord", NCOs (T5-T6) by rank title, Enlisted (T1-T4) as "soldier"
- **Discount Information:** Explicitly states discount percentage based on reputation (0-30%)
- **Price Attitude:** "Good prices for you" (trusted) / "Fair deal" (friendly) / "Standard prices" (neutral) / "Full price, no discounts" (hostile)
- **Standing Hints:** "You've earned it" / "You've earned some pull" / "Don't expect favors" / "No discounts for troublemakers"

**Technical Implementation:**
- **JSON Data:** Dialogue structure defined in `ModuleData/Enlisted/Dialogue/qm_dialogue.json`
- **XML Localization:** Strings stored in `ModuleData/Languages/enlisted_qm_dialogue.xml`
- **Specificity Matching:** Most specific context match wins (e.g., `is_introduced + supply_excellent` vs `is_introduced`)
- **Runtime Delegates:** Bannerlord condition delegates call `GetCurrentDialogueContext()` on every display

---

## Conversation Flow

### Main Conversation Hub

The quartermaster system uses a conversation-driven interface where dialogue options open the visual equipment browser:

```
[Player approaches Quartermaster]

QM: [Greeting based on supply level, reputation, mood]

Player Options:
  → "I'm looking for some new gear. What've you got?"
  → "I want to improve what I'm carrying."
  → "I've got some equipment to offload... quietly."
  → "I could use some provisions."
  → "How are we looking? Supply-wise, I mean."
  → "That's all for now."
```

### Equipment Category Selection

When player selects "I'm looking for some new gear":

```
QM: [Dynamic browse response with context - see examples below]

Player Options:
  → "Weapons."
  → "Armor."
  → "Accessories."
  → "A horse." (only if mounts available in troop tree)
  → "Never mind."
```

**QM Browse Response Format:**

The QM provides contextual information before showing categories:

1. **Supply Status**: Current stock situation ("Plenty in stock" / "Stock's thin" / "Pickings are slim")
2. **Rank Acknowledgment**: For T5+ soldiers, mentions rank and standing
3. **Price Information**: Discount percentage based on reputation (0-30%)
4. **Category Prompt**: Lists available categories

**Browse Response Examples:**

*Tier 3, Good Supply, Neutral Reputation (10% discount):*
```
"We've a fair bit in stock. Prices are standard. Nothing fancy. 
What are you after—weapons, armor, accessories?"
```

*Tier 6 Sergeant, Excellent Supply, Friendly Reputation (20% discount):*
```
"Plenty in stock. Recent resupply came through. You've earned 
some pull around here, Sergeant. I'll cut you a fair deal—20% 
off standard. What are you after—weapons, armor, accessories?"
```

*Tier 2, Low Supply, Hostile Reputation (0% discount):*
```
"Stock's thin right now. We're stretched. Full price. No discounts 
for troublemakers. What are you after—weapons, armor, accessories?"
```

*Tier 8 Captain, Critical Supply, Trusted Reputation (30% discount):*
```
"Pickings are slim. We're rationing everything. For an officer of 
your standing, Captain, I can show you the better pieces. Prices 
are good for you—30% off. You've earned it. What are you after—
weapons, armor, accessories?"
```

**Equipment Categories:**

| Category | Contains | Notes |
|----------|----------|-------|
| **Weapons** | Swords, spears, bows, arrows, throwing weapons | Shields excluded (moved to Accessories) |
| **Armor** | Helmets, body armor, gloves, boots | All armor pieces shown together; capes excluded (moved to Accessories) |
| **Accessories** | Capes, shields, horse harness | Non-armor wearables and mount equipment |
| **Mounts** | Horses | Only shown if mounts available in player's troop tree at current tier |

### Gauntlet UI Opens

After category selection, the QM confirms and the conversation closes. The **visual Gauntlet equipment grid** opens:

- Shows available equipment with quality modifiers
- Displays prices (modified by quality and reputation)
- Shows stats and tooltips
- Player browses, purchases, or exits
- On exit, conversation resumes at equipment category hub for continued shopping

### Supply Inquiry (Pure Dialogue)

When player asks "How are our supplies?":

```
QM: [Detailed contextual report - see Contextual Dialogue System section]

[Returns to conversation hub - no UI opens]
```

### Example Full Interaction

```
Player: [Visits Quartermaster in camp]

QM (Veteran, Friendly, Good Supply, T6 Sergeant, 20% discount): 
    "Supplies are holding well. What do you require?"

Player: "I'm looking for some new gear. What've you got?"

QM: "We've a fair bit in stock. You've earned some pull around here, 
     Sergeant. I'll cut you a fair deal—20% off standard. What are you 
     after—weapons, armor, accessories?"

Player: "Armor."

QM: "Right. Let's see what we've got."

[Gauntlet UI opens showing ALL armor options: helmets, body armor, gloves, boots]
[Player browses across all armor types]
[Player selects Fine Vlandian Hauberk (Body) - 416g after 20% discount]
[Player purchases, UI closes]

[Returns to equipment category hub]

QM: "We've a fair bit in stock. You've earned some pull around here, 
     Sergeant. I'll cut you a fair deal—20% off standard. What are you 
     after—weapons, armor, accessories?"

Player: "Never mind."

[Returns to main hub]

QM: "Supplies are holding well. What do you require?"

Player: "How are we looking? Supply-wise, I mean."

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
- Masterwork/Legendary quality is obtained through the equipment upgrade system

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

### Provision Bundles (All Ranks)

In addition to issued rations (T1-T4) and the provisions shop (T5+), the quartermaster offers **prepared provision bundles** that provide immediate morale and fatigue benefits. These are available via the "I could use some provisions" conversation option.

**Available Bundles:**

| Bundle | Cost | Duration | Benefits | Requirements |
|--------|------|----------|----------|--------------|
| **Supplemental Rations** | 10g | 1 day | +2 morale | None |
| **Officer's Fare** | 30g | 2 days | +4 morale, +2 fatigue recovery | None |
| **Commander's Feast** | 75g | 3 days | +8 morale, +5 fatigue recovery | Tier 4+ |

**Key Features:**
- QM reputation discount applies to bundle costs
- Benefits apply immediately upon purchase
- Duration tracks how long the quality food lasts
- Not reclaimed at muster (these are personal purchases, not issued rations)
- Available to all ranks (Commander's Feast requires T4+)

**Use Case:**
Provision bundles are ideal for quick morale/fatigue boosts before battles or during tough campaigns. They're more expensive per day than standard food but provide immediate gameplay benefits beyond just preventing starvation.

---

## Quartermaster's Deal

### Overview

At pay muster, soldiers can trade reduced wages for a chance at surplus equipment through the **Quartermaster's Deal** option. This represents the QM offloading excess stock to soldiers willing to take a gamble on what's available.

**Available:** Always (Pay Line menu option 3)

### Mechanics

**Cost:**
- 40% of wages owed (forfeit 60%)
- 6 fatigue

**Success Rate:**
- **Base:** 70% chance to receive equipment
- **Modified by QM Reputation:**
  - Rep 75+: 85% (+15%)
  - Rep 50-74: 75% (+5%)
  - Rep 25-49: 70% (base)
  - Rep <25: 60% (-10%)

**On Failure:**
- Player still receives 40% pay
- No equipment awarded

**On Success:**

1. **Tier Roll:**
   - 90% chance: Current tier equipment
   - 10% chance: Tier +1 equipment (capped at T9)

2. **Equipment Selection:**
   - Items selected from player's formation and culture
   - Weighted by player skills:
     - OneHanded skill → One-handed weapons
     - TwoHanded skill → Two-handed weapons
     - Polearm skill → Polearms
     - Bow skill → Bows
     - Crossbow skill → Crossbows
     - Athletics skill → Light armor (<10kg)
     - Riding skill → Heavy armor, horses
   - Higher skill = higher weight (0 skill = 1.0x, 100 skill = 3.0x)

3. **Item Delivery:**
   - Item added directly to inventory
   - **Exempt from contraband checks** during baggage inspection
   - Tracked in muster outcome record

### Example Outcomes

**Success (70% roll, T3 Infantry with OneHanded 80):**
```
The quartermaster rummages through the supply wagon...

"Got something here that might suit you. Consider us square."

> Received: Fine Arming Sword (Tier 3)
> Received: 62 denars (40% pay)
```

**Tier Bonus (10% roll, rolled up to T4):**
```
The quartermaster grins.

"You're in luck. Found something from the veteran's stock."

> Received: Reinforced Scale Armor (Tier 4)
> Received: 62 denars (40% pay)
```

**Failure (30% roll):**
```
The quartermaster rummages through the supply wagon...

"Nothing suitable today. You get the coin instead."

> Received: 62 denars (40% pay)
```

### Integration Notes

- **QM Reputation Impact:** Better reputation = better odds
- **Formation Matters:** Infantry get infantry equipment, cavalry get cavalry equipment
- **Skill Weighting:** Respects player build (sword users get swords, archers get bows)
- **Contraband Exemption:** Items are official quartermaster issue, not flagged
- **No Resale:** Like all QM purchases, items can be sold back through buyback system

---

## Baggage Checks

### Muster Inspections

Every pay muster has a **30% chance** of triggering a baggage inspection. This occurs during the [Baggage Check stage](../Core/muster-system.md#3-baggage-check) of the muster system sequence (stage 3, after the Pay Line).

**Important:** This is a **security inspection** for contraband items, separate from the logistics-based baggage access system (see [Baggage Train Availability](baggage-train-availability.md)). All soldiers have full baggage access during muster regardless of inspection outcome. The inspection only determines if contraband is discovered and confiscated.

```
⚠️  CONTRABAND DISCOVERED  ⚠️

The quartermaster's hand stops in your pack. He pulls out an item
and raises an eyebrow.

Found: Noble Armor (value 1200 denars)

"This doesn't belong to a soldier of your rank," he says quietly.
"I can overlook it... for a price. Or we can do this by the book."

[QM Reputation: 45 - Neutral]
```

### Contraband Detection

The quartermaster scans for:
- Illegal goods (smuggled items)
- Stolen gear (high-value civilian goods)
- Flagged contraband items
- Items inappropriate for player's rank

### Outcomes by Reputation

| Scenario | QM Rep | Event ID | Outcome |
|----------|--------|----------|---------|
| No contraband | Any | — | Skip to next stage |
| Contraband + Trusted | 65+ | `evt_baggage_lookaway` | QM looks away, auto-pass, no penalty |
| Contraband + Friendly | 35-64 | `evt_baggage_bribe` | Bribe option (Charm check, 50% success) or smuggle (Roguery 40+) |
| Contraband + Neutral | < 35 | `evt_baggage_confiscate` | Confiscation only, fine + 2 Scrutiny |
| Contraband + Hostile | < -25 | `evt_baggage_report` | Severe penalties, discipline increase |

**Integration with Muster Menu:**
- Baggage check appears as stage 3 in the muster flow
- Player sees contraband details and QM reputation before choosing
- Options presented based on current reputation tier
- Skip conditions apply (no contraband, high rep, 70% no-trigger chance)
- See [Muster System - Baggage Check](../Core/muster-system.md#3-baggage-check) for complete flow

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
- Player Rank: **T7+** (Officer tier)
- QM Reputation: **60+**

**Gate Responses:**
- **Below T7:** "That stock is for officers, {PLAYER_RANK}. You're not one yet. Earn your commission first."
- **T7+ but Rep < 60:** "You've got the rank, {PLAYER_RANK} {PLAYER_NAME}, but I don't know you well enough. Build some trust first."

### How It Works

Officers' Armory provides access to:
1. **Higher-Tier Equipment:** Troops from 1-2 tiers above your normal access
2. **Better Quality Modifiers:** Fine/Masterwork/Legendary items based on reputation

**Tier Access by Reputation:**
| QM Rep | Tier Bonus | Example (T7 Player) | Quality Pool |
|--------|------------|---------------------|--------------|
| 60-74 | +1 | Access to T8 equipment | Common, Fine |
| 75-89 | +1 to +2 (weighted) | Access to T8-T9 equipment | Common, Fine, 30% Masterwork |
| 90+ | +2 | Access to T9 equipment | Fine, Masterwork, 15% Legendary |

### First-Time Access

```
Player: "What about premium officer equipment?"

QM: [If T7+ and Rep 60+]
    "Ah, you've earned it. I've got access to elite stock - equipment from 
    higher-tier troops with better quality. It's expensive, but worth it.
    
    Browse weapons, armor, or mounts - same as usual, but better gear."
```

### Elite Equipment

```
=== OFFICERS ARMORY ===

[Browse Response - Same as regular quartermaster]
Player: "I need a weapon."

QM: [Opens weapon grid]

[Equipment Grid shows:]
  Fine Bastard Sword (T8) - 720g ⚜️
    * +10% damage, +5% speed
    * [OFFICER EXCLUSIVE] badge
  
  Masterwork Glaive (T9) - 1850g ⚜️
    * +15% damage, +10% speed
    * [OFFICER EXCLUSIVE] badge
    
  [Regular T7 equipment also shown for comparison]
```

### Quality Modifiers

Officers' Armory items use Bannerlord's native `ItemModifier` system with reputation-based quality rolls:

**60-74 Reputation:**
- Common quality (50%)
- Fine quality (50%)

**75-89 Reputation:**
- Common quality (35%)
- Fine quality (35%)
- Masterwork quality (30%)

**90+ Reputation:**
- Fine quality (50%)
- Masterwork quality (35%)
- Legendary quality (15%)

### Supply Integration

Officers' Armory items are included in muster stock rolls with **1.5× higher out-of-stock chance** (premium items are scarcer):

| Supply Level | Regular Out-of-Stock | Officer Items Out-of-Stock |
|--------------|----------------------|----------------------------|
| 60-100% | 0% | 0% |
| 40-59% | 20% | 30% |
| 15-39% | 50% | 75% |
| < 15% | Menu blocked | Menu blocked |

### Losing Access

If reputation drops below 60:

```
Player: "What about premium officer equipment?"

QM: "You've got the rank, {PLAYER_RANK} {PLAYER_NAME}, but I don't know 
    you well enough. Build some trust first."

[Redirected to hub]
```

### Implementation Notes

- Officers' Armory uses the same Gauntlet UI as regular equipment browsing
- Higher-tier items are marked with `IsOfficerExclusive = true` flag
- Quality modifiers are applied at variant build time, not at purchase
- If player is at max tier (T9), quality bonus is the main benefit (no higher tiers exist)

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

**Context-Aware Dialogue (JSON-Driven):**
- **Data-Driven Architecture** - Dialogue nodes, conditions, and options defined in JSON.
- **Dynamic Context Evaluation** - Game state (supply, events, rep) checked at runtime on every display.
- **Specificity Matching** - System selects most appropriate variant based on context complexity.
- **Supply-Aware Greetings** - 5 distinct hub variants reacting to party logistics state.
- **Contextual Browse Responses** - 5 variants reacting to supply level before opening store.
- **Roleplay Options** - Player lines read like natural dialogue ("I could use some provisions").

**Equipment & Logistics:**
- **Quality System** - Six tiers affecting stats and price using native `ItemModifier`.
- **Upgrade System** - Improve gear quality with reputation-based access and pricing.
- **Buyback Restrictions** - Fixed exploit; only QM-issued items on player person can be sold.
- **Visual Store** - Gauntlet UI for equipment browsing with quality badges and tooltips.
- **Rations & Provisions** - T1-T4 issued rations, T5+ officer provisions shop.

**First Meeting Introduction:**
- One-time intro when meeting a new Quartermaster sets the relationship tone
- Player chooses style: direct (-2 rep), military (0), friendly (+3), or flattering (+5)
- QM acknowledges with archetype-specific response (28 variants: 4 tones × 7 archetypes)
- Player style stored in `_qmPlayerStyle` and persists across saves
- Intro flag (`_hasMetQuartermaster`) skips intro on return visits

**Tier Gate RP Responses:**
- Character-driven explanations when actions are blocked (not UI blocks)
- Horse requests blocked for infantry with archetype-specific refusals
- Officers' Armory blocked below T7 or below 60 rep with appropriate dialogue
- Upgrade tier locks explained via QM dialogue

**Sell Reputation Gating:**
- Hostile rep blocks all sell access with QM refusal dialogue
- Wary rep reduces buyback rates with suspicious tone
- Higher rep unlocks better rates and friendlier tone
- Only QM-purchased items from player's person can be sold (baggage train exploit fixed)

**Inventory & Pricing System:**
- Stock refreshes at muster completion (12-day cycle) via `QuartermasterManager.RollStockAvailability()`
- Refresh happens in `MusterMenuHandler.CompleteMusterSequence()` after all muster stages complete
- Stock quantity varies by supply level (excellent: 3-5, low: 1-2, critical: 1)
- Supply scarcity adds pricing markup (+10% to +50%)
- QM rep modifies prices (-15% trusted to +25% hostile)
- Out-of-stock items show greyed cards with "Restocks at muster" hint
- Newly unlocked items (from promotions) are marked in the shop after stock refresh

**Provisions Gauntlet UI:**
- Visual grid UI for T7+ officers (same style as equipment browser)
- Food items with icons, prices, quantities from QM inventory
- Buy 1 / Buy All quantity options
- T1-T6 see ration info + supplement option, T7+ see full shop
- Food filtering uses `IsFood` property + keyword patterns

**Gauntlet UI Technical Requirements:**
All QM Gauntlet screens use these patterns for proper rendering:
- **Dark overlay:** `Sprite="BlankWhiteSquare_9" Color="#000000CC"` (not BrushWidget)
- **Popup frame:** `Brush="Encyclopedia.Frame"` with dimension constants
- **Header bar:** `Sprite="StdAssets\tabbar_popup"` for title styling
- **List backgrounds:** `Brush="Encyclopedia.List.Background"`
- **Close button:** `Brush="Popup.CloseButton"`
- **Item images:** `ImageIdentifierWidget` with DataSource binding to `ItemImageIdentifierVM`

Using non-existent brush names (like `Popup.Background.Medium`) causes UI to render with only partial elements visible.

### Future Enhancements

**Camp News Integration:**
- Daily Report entries for quartermaster events
- Track significant transactions (upgrades, stock changes)
- News templates for equipment activities

### Related Documentation

- **[Company Supply Simulation](company-supply-simulation.md)** - How company supply is calculated and affects quartermaster access
- **[Provisions & Rations System](provisions-rations-system.md)** - Detailed food mechanics for T1-T4 issued rations and T5+ officer provisions

### Source Files

| File | Purpose |
|------|---------|
| `src/Features/Conversations/Behaviors/EnlistedDialogManager.cs` | Dialogue registration and action handling |
| `src/Features/Conversations/Data/QMDialogueCatalog.cs` | JSON dialogue loader and context matching |
| `src/Features/Equipment/Behaviors/QuartermasterManager.cs` | Core quartermaster logic |
| `src/Features/Equipment/Behaviors/TroopSelectionManager.cs` | Equipment selection integration |
| `src/Features/Equipment/Managers/QMInventoryState.cs` | Inventory tracking and muster refresh |
| `src/Features/Equipment/UI/QuartermasterEquipmentSelectorBehavior.cs` | Gauntlet layer management for QM screens |
| `src/Features/Equipment/UI/QuartermasterEquipmentSelectorVM.cs` | Main equipment UI view model |
| `src/Features/Equipment/UI/QuartermasterEquipmentItemVM.cs` | Individual item view model |
| `src/Features/Equipment/UI/QuartermasterUpgradeVM.cs` | Upgrade screen view model |
| `src/Features/Equipment/UI/QuartermasterUpgradeItemVM.cs` | Individual upgrade item view model |
| `src/Features/Equipment/UI/QuartermasterProvisionsVM.cs` | Provisions shop UI view model |
| `src/Features/Equipment/UI/QuartermasterProvisionItemVM.cs` | Provision item view model |
| `GUI/Prefabs/Equipment/QuartermasterEquipmentGrid.xml` | Equipment grid Gauntlet layout |
| `GUI/Prefabs/Equipment/QuartermasterUpgradeScreen.xml` | Upgrade screen Gauntlet layout |
| `ModuleData/Enlisted/Dialogue/qm_dialogue.json` | Main hub, browse, armor slots |
| `ModuleData/Enlisted/Dialogue/qm_gates.json` | Tier gate responses |
| `ModuleData/Enlisted/Dialogue/qm_intro.json` | First meeting introduction flow |
| `ModuleData/Languages/enlisted_qm_dialogue.xml` | All QM localization strings |

---

**End of Document**

