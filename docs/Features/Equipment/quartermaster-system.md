# Quartermaster System

**Summary:** The Quartermaster manages all military logistics for the player's company including equipment purchases, provisions/rations, buyback services, baggage inspections, and the Officers Armory. The system uses company supply levels to gate access, and quartermaster reputation to determine pricing, food quality, and enforcement strictness.

**Status:** ✅ Current  
**Last Updated:** 2025-12-22  
**Related Docs:** [Equipment Quality](quartermaster-equipment-quality.md), [Company Supply](company-supply-simulation.md)

---

## Index

1. [Overview](#overview)
2. [Quartermaster NPC](#quartermaster-npc)
3. [Main Menu Structure](#main-menu-structure)
4. [Equipment Purchasing](#equipment-purchasing)
5. [Buyback System](#buyback-system)
6. [Provisions & Rations](#provisions--rations)
7. [Baggage Checks](#baggage-checks)
8. [Officers Armory](#officers-armory)
9. [Discharge & Retirement](#discharge--retirement)
10. [Reputation System](#reputation-system)
11. [Supply Integration](#supply-integration)
12. [Implementation Status](#implementation-status)

---

## Overview

The Quartermaster is the central logistics hub for enlisted soldiers, managing:

- **Equipment Access** - Buy armor, weapons, accessories with reputation-based discounts (0-30%)
- **Master at Arms Integration** - Category-filtered equipment browsing (armor/weapons/accessories)
- **Buyback Services** - Sell QM-purchased equipment back at 30-65% of value
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

1. **In Camp:** Talk to Quartermaster NPC
2. **Via Menu:** "Visit Quartermaster" option in camp menu
3. **Opening Greeting:**
   - First meeting: Introduction dialogue
   - High rep (65+): Friendly greeting
   - Neutral rep: Brief greeting
   - Low rep (<0): Hostile greeting

---

## Main Menu Structure

### Standard Menu (All Ranks)

```
=== QUARTERMASTER ===

[Player Rank: T3 - Corporal]
[Company Supply: 65% (Adequate)]
[QM Reputation: +35 (Friendly)]

Options:
  [1] Buy Armor
  [2] Buy Weapons  
  [3] Buy Accessories
  [4] Sell Equipment Back
  [5] Provisions (T5+ only) ← Greyed out for T1-T4
  [6] Inquire About Supply Situation
  [7] Leave
```

### Officer Menu (T5+, High Rep)

```
=== QUARTERMASTER ===

[Player Rank: T5 - Lieutenant]
[Company Supply: 45% (Low)]
[QM Reputation: +65 (Trusted)]

Options:
  [1] Buy Armor
  [2] Buy Weapons
  [3] Buy Accessories
  [4] Sell Equipment Back
  [5] Buy Provisions ← NOW AVAILABLE
  [6] Inquire About Supply Situation
  [7] Officers Armory (Rep 60+, Soldier 50+) ← Special access
  [8] Leave
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

### Currently Implemented

**Core Systems:**
- ✅ Quartermaster NPC generation and assignment
- ✅ Basic conversation tree and menu system
- ✅ Equipment purchase via Master at Arms integration
- ✅ Category filtering (armor/weapons/accessories)
- ✅ Reputation-based pricing (0-15% discount range)
- ✅ Supply level tracking (0-100%)

**Partially Implemented:**
- ⚠️ Buyback system (exists but doesn't restrict to QM-issued items)
- ⚠️ Provisions menu (exists but uses old "morale buff" model for T1-T4)
- ⚠️ Baggage stash (exists but no muster inspections)

### Planned Improvements

**Phase 1: Company Supply Integration**
- Wire supply computation (hybrid observation + simulation)
- Show supply % in Quartermaster UI
- Block equipment purchases when supply < 30%

**Phase 2: Reputation System Rework**
- Expand reputation to -50 to +100 range
- Implement full multiplier tables (30% discount to 40% markup)
- Apply to equipment, buyback, and food pricing

**Phase 3: QM-Purchased Tracking**
- Track all QM-issued equipment persistently
- Restrict buyback menu to tracked items only
- Implement discharge reclamation
- Add missing gear penalties

**Phase 4: Ration Exchange System (T1-T4)**
- Implement ration reclamation at each muster
- Track issued rations separately from personal food
- Integrate with company supply for availability
- Add personal food loss events (rats, spoilage, theft)

**Phase 5: Officer Provisions Shop (T5+)**
- Create separate T5+ food shop UI
- Implement inventory refresh every muster
- Apply premium pricing (150-200% of town markets)
- Supply-dependent stock quantities

**Phase 6: Baggage Checks**
- Add 30% muster inspection chance
- Implement contraband detection
- Reputation-based outcomes (lookaway/bribe/confiscate)
- Integrate with Scrutiny system

**Phase 7: Officers Armory**
- Create high-rep equipment menu
- Apply quality modifiers (Fine/Masterwork)
- Gate by tier + QM rep + soldier rep
- Test access granted/denied

### Related Documentation

- **Equipment Quality:** `quartermaster-equipment-quality.md` - Quality modifiers, item tiers
- **Company Supply:** `company-supply-simulation.md` - Supply calculation, degradation
- **Food System:** `player-food-ration-system.md` - Detailed ration mechanics (being replaced by this doc)
- **Provisions:** `provisions-system.md` - Old provisions system (being replaced by this doc)
- **Master Implementation:** `Quartermaster_Master_Implementation.md` - Technical implementation plan (being replaced by this doc)
- **Dialogue:** `quartermaster-dialogue-implementation.md` - Full conversation trees (being replaced by this doc)

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

