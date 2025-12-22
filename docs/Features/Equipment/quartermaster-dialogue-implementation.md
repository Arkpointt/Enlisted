# Quartermaster Dialogue & Menu Implementation

> **Purpose**: Document the quartermaster NPC dialogue system, menu structure, and event responses needed to support equipment, provisions, and baggage check systems.

**Last Updated**: December 18, 2025  
**Status**: Implementation Planning

---

## Overview

The Quartermaster is a central NPC who manages:
- **Equipment** - Master at Arms troop selection (T1-T9)
- **Provisions** - Food shop for officers (T5+ only)
- **Buyback** - Selling QM-purchased equipment back
- **Baggage Checks** - Contraband searches at muster
- **Ration Exchange** - Issuing/reclaiming food rations (T1-T4)

**Implementation Strategy:**
- Single NPC: The Quartermaster (lord's retinue member or generated)
- Dialogue system: Standard Bannerlord conversation tree
- Menu system: Custom UI with category-based equipment browsing
- Event responses: Context-aware dialogue based on player rank, rep, and company state

---

## Quartermaster NPC Setup

### **NPC Generation/Assignment**

**When Player Enlists:**
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
    
    // Store reference
    _assignedQuartermaster = quartermaster;
    
    ModLogger.Info("QM", $"Quartermaster assigned: {quartermaster.Name}");
}

private Hero GenerateQuartermaster(Hero lord)
{
    // Create NPC with appropriate culture
    CharacterObject template = GetQuartermasterTemplate(lord.Culture);
    Hero qm = HeroCreator.CreateSpecialHero(template);
    
    // Set properties
    qm.SetName(GenerateQuartermasterName(lord.Culture), 
               GenerateQuartermasterName(lord.Culture));
    
    // Set initial relationship
    Hero.MainHero.SetPersonalRelation(qm, 0); // Neutral starting rep
    
    return qm;
}
```

**Quartermaster Characteristics:**
- Age: 35-50 (experienced veteran)
- Skills: High Steward, Trade (logistics specialist)
- Role: Non-combatant, stays in lord's party
- Personality: Professional, stern, can be bribed if high rep

---

## Main Menu Structure

### **Quartermaster Menu (All Ranks)**

**Access:** Talk to Quartermaster NPC in camp/settlement

```
=== QUARTERMASTER ===

[Player Rank: T3 - Lance Corporal]
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

**For T5+ Officers:**
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
  [7] Officers Armory (Rep 60+, Rank T5+) ← Special access
  [8] Leave
```

### **Menu Option Details**

#### **[1-3] Buy Armor/Weapons/Accessories**

Opens category-filtered equipment menu:

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
  
[Select troop] [Preview Equipment] [Back]
```

**Category Filtering:**
- **Armor:** Head, Body, Leg, Hand armor pieces
- **Weapons:** Melee, Ranged, Shields
- **Accessories:** Horse, saddle, banner (if applicable)

**Implementation Note:**
```csharp
// Open equipment menu with category filter
public void OpenEquipmentMenu(EquipmentCategory category)
{
    var availableTroops = GetAvailableTroops(Hero.MainHero);
    
    // Filter by category
    if (category == EquipmentCategory.Armor)
    {
        // Show only armor differences between loadouts
    }
    else if (category == EquipmentCategory.Weapons)
    {
        // Show only weapon differences
    }
    
    // Open UI with filtered list
    ShowEquipmentUI(availableTroops, category);
}
```

#### **[4] Sell Equipment Back**

Opens buyback menu showing QM-purchased items:

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

#### **[5] Buy Provisions (T5+ Only)**

Opens officer food shop:

```
=== BUY PROVISIONS ===

Company Supply: 75% (Good stock)
Your Reputation: +45 (Friendly) - Prices: 175% of market

Available Provisions (refreshes next muster in 8 days):
  [Grain] - 6 available - 17g each (market: 10g)
  [Butter] - 4 available - 70g each (market: 40g)
  [Cheese] - 3 available - 87g each (market: 50g)
  [Meat] - 3 available - 52g each (market: 30g)
  [Fish] - 2 available - 61g each (market: 35g)

Your Gold: 1250g
Days of Food Remaining: 12 days

💡 Town markets are cheaper but require travel.

[Select item] [Buy Multiple] [Back]
```

**Low Supply Example:**
```
=== BUY PROVISIONS ===

Company Supply: 25% (CRITICAL)
Your Reputation: +45 (Friendly)

⚠️ SUPPLY CRISIS - Limited Stock Available

Available Provisions:
  [Grain] - 2 available - 17g each (market: 10g)
  
Next resupply: 4 days (next muster)

⚠️ We're running dangerously low. I recommend buying from town markets 
   or completing supply duties.

[Buy Grain] [Back]
```

#### **[6] Inquire About Supply Situation**

Dialogue option, no menu:

```
[Quartermaster explains current supply state]

If 70%+: "Supplies are good. No concerns at the moment."

If 50-69%: "We're running a bit low. Nothing critical yet, but keep an 
            eye on it. Complete some supply duties if you can."

If 30-49%: "Supply situation is concerning. We're rationing carefully. 
            If this continues, we'll have to halt equipment changes."

If <30%: "We're in crisis. I can't issue any equipment changes until 
          we resupply. Food rations are suspended. Get supplies NOW."
```

#### **[7] Officers Armory (Special Access)**

Only appears if:
- Player Rank: T5+
- QM Rep: 60+
- Lance Rep: 50+

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

---

## Dialogue System

### **Standard Greeting (Context-Aware)**

**First Meeting:**
```
QM: "You must be the new recruit. I'm [Name], the quartermaster. 
     I manage equipment and supplies for this company.
     
     When you need gear, come see me. But don't expect charity - 
     I keep careful records of what's issued."

Options:
  [1] What equipment can I get?
  [2] How does this work?
  [3] I understand. (End)
```

**Subsequent Meetings (Neutral Rep):**
```
QM: "What do you need?"

[Opens main menu]
```

**High Reputation (65+):**
```
QM: "[Player Name], good to see you. What can I do for you today?"

[Opens main menu with friendly tone]
```

**Low Reputation (<0):**
```
QM: "You again. Make it quick."

[Opens main menu with hostile tone]
```

**After Baggage Check (Contraband Found):**
```
QM: "We need to talk about what I found in your bags..."

[Triggers baggage check dialogue tree]
```

---

## Event Dialogues

### **1. Muster: Food Ration Exchange (T1-T4)**

**Standard Ration Issue (Supplies 70%+, QM Rep 0-65):**
```
QM: "Here's your ration for the next twelve days. Make it last."

[Hands over grain/butter/cheese/meat based on rep]

Options:
  [1] Thank you. (End)
  [2] Is this all I get? (Complaint)
```

**If Player Complains:**
```
QM: "You're a soldier, not a nobleman. Take what you're given."

[-2 QM Rep]
[End]
```

**High Rep Ration Issue (QM Rep 80+):**
```
QM: "Here's your ration, [Name]. I managed to get you some meat this time. 
     Don't tell the others."

[Hands over meat - premium ration]

[+1 QM Rep]
[End]
```

**No Ration Available (Supplies <50%, Failed Roll):**
```
QM: "Bad news. Supplies didn't come through this muster. 
     No rations available right now.
     
     You'll have to buy from town or forage until things improve.
     Sorry, but that's the reality of campaign life."

Options:
  [1] I understand. (End)
  [2] This is unacceptable! (Complaint)
```

**If Player Complains About No Ration:**
```
QM: "You think I'm happy about this? Take it up with the enemy 
     who's blocking our supply lines. Or better yet, help fix it 
     by doing some supply runs."

[End]
```

### **2. Muster: T5 Officer Transition**

**At T5 Promotion (First Officer Muster):**
```
QM: "Congratulations on your promotion, Lieutenant.
     
     As an officer, you're no longer issued rations. You're expected to 
     manage your own provisions.
     
     I have a shop available - prices are higher than town markets, but 
     it's convenient when you're in the field. Stock refreshes every muster.
     
     [Reclaims any issued rations]
     
     Your ration days are over. Welcome to the officer corps."

Options:
  [1] I understand. Show me the provisions shop.
      → Opens provisions menu
  [2] This seems expensive...
      → "You're an officer now. Act like one."
  [3] Thank you. (End)
```

### **3. Baggage Check Events**

> **Integration**: These dialogues trigger during muster (30% chance). See `docs/StoryBlocks/content-index.md` for the parent event catalog. The Scrutiny escalation track affects outcomes.

| Scenario | Event ID | QM Rep Range | Outcome |
|----------|----------|--------------|---------|
| No contraband | `evt_baggage_clear` | Any | Pass |
| Contraband + high rep | `evt_baggage_lookaway` | 65+ | QM ignores |
| Contraband + mid rep | `evt_baggage_bribe` | 35-65 | Bribe option |
| Contraband + low rep | `evt_baggage_confiscate` | <35 | Confiscation + Scrutiny |
| Contraband + hostile | `evt_baggage_report` | <-25 | Severe penalty |

**No Contraband Found:**
```
QM: "Routine inspection."

[Searches bags]

QM: "Everything's in order. Carry on."

[End]
```

**Contraband Found (High Rep 65+):**
```
QM: [Glances at your pack, sees valuable contraband]
     [Looks around]
     [Looks back at you]
     
     "I didn't see anything. But be more careful."

[Looks the other way - no penalty]
[End]
```

**Contraband Found (Medium Rep 35-65):**
```
QM: "Well well, what do we have here..."

[Holds up contraband item]

QM: "This isn't standard issue. Where'd you get it?"

Options:
  [1] I... found it. (Lie)
  [2] Looted from a battle. (Truth)
  [3] Offer bribe - {BRIBE_AMOUNT}g
```

**If Player Offers Bribe (Rep 35-65):**
```
QM: [Looks at the gold]
     [Looks around]
     
     "I suppose I could overlook this. This time."

[Takes bribe - 50-75% of item value]
[Player keeps item]
[End]
```

**If Player Lies/Admits (Rep 35-65):**
```
QM: "Look, I like you, so I'll give you a choice.
     
     Pay me [BRIBE_AMOUNT]g and I forget this happened. 
     Or I confiscate it. Your call."

Options:
  [1] Pay bribe - {BRIBE_AMOUNT}g
  [2] Let him take it
```

**Contraband Found (Low Rep <35):**
```
QM: "Caught you. This is going in my report."

[Confiscates item]
[+5 to +15 Scrutiny depending on rep]
[Fine of 100-200g depending on rep]

QM: "Don't let me catch you again."

[-5 QM Rep]
[End]
```

**Contraband Found (Very Low Rep <-25):**
```
QM: "THIEF! You're stealing from your own comrades!"

[Confiscates item]
[+15 Scrutiny]
[Fine of 200g]
[-10 QM Rep]

QM: "The commander will hear about this. 
     I'll be watching you very closely from now on."

[End]
```

### **4. Equipment Purchase**

**Standard Purchase (Neutral Tone):**
```
QM: "That'll be [COST]g. Don't lose this gear - I keep records."

[Equipment issued, gold deducted]

Options:
  [1] Understood. (End)
```

**Purchase with Low Gold (Can't Afford):**
```
QM: "You don't have enough. Come back when you've got the coin."

[Transaction cancelled]
[End]
```

**Purchase During Low Supply (<30%):**
```
QM: "We're critically low on supplies. I can't issue new equipment right now.
     
     Complete some supply runs, or wait for the next caravan. 
     We should have stock again in [DAYS] days."

[Equipment menu blocked]
[End]
```

### **5. Equipment Buyback**

**Standard Buyback (Rep 45):**
```
Player: "I want to sell this back."

QM: [Examines equipment]
     "I'll give you [BUYBACK_PRICE]g for it. That's the best I can do."

Options:
  [1] Accept - Sell for {BUYBACK_PRICE}g
  [2] Decline - Keep it
```

**High Rep Buyback (Rep 75+):**
```
Player: "I want to sell this back."

QM: "For you, I can offer [BUYBACK_PRICE]g. That's top rate."

[Friendly tone - slightly better price due to rep]

Options:
  [1] Accept - Sell for {BUYBACK_PRICE}g
  [2] Thanks, but I'll keep it.
```

**Low Rep Buyback (Rep 0-10):**
```
Player: "I want to sell this back."

QM: "I'll take it off your hands for [BUYBACK_PRICE]g. 
     And that's being generous."

[Dismissive tone - lower buyback rate]

Options:
  [1] Accept - Sell for {BUYBACK_PRICE}g
  [2] That's robbery! (Complain)
  [3] Nevermind.
```

**If Player Complains About Buyback Price:**
```
QM: "Then don't sell it. My rates are fair. Build your reputation if 
     you want better prices."

[End]
```

### **6. Provisions Shop (T5+)**

**Standard Provisions Purchase:**
```
QM: "What provisions do you need?"

[Opens provisions menu]

[Player selects item and quantity]

QM: "That'll be [COST]g. Food's been expensive lately."

[Transaction completed]
[End]
```

**High Rep Provisions (Rep 65+):**
```
QM: "What do you need today?"

[Opens provisions menu with best discount]

QM: "For you, [COST]g. That's as low as I can go."

[Transaction completed]
[End]
```

**Low Supply Provisions (<30%):**
```
Player: "I need provisions."

QM: "I've only got grain right now. Two bags. That's it.
     
     We're in crisis mode. I'd recommend hitting the town markets 
     until our supply situation improves."

[Opens limited provisions menu - 2 grain only]
[End]
```

**Out of Stock Item:**
```
Player: [Tries to buy more than available]

QM: "I only have [QUANTITY] of those. Come back after the next muster 
     for a fresh shipment."

[Limits purchase to available quantity]
[End]
```

### **7. Discharge/Retirement**

**Standard Discharge (Has QM-Purchased Gear):**
```
QM: "Leaving us, eh? I'll need all military equipment back before you go."

[Scans inventory for QM-purchased items]

QM: "Let's see... I'm reclaiming:"
     - [List of QM-purchased items]

[Items removed from inventory]

QM: "Equipment returned. Good luck out there."

[End]
```

**Discharge (Missing Gear):**
```
QM: "You're missing some gear from my records. Where is it?"

[Player sold/lost QM-purchased items]

QM: "You owe [REPLACEMENT_COST]g for missing equipment."

[Deducts from final pay/pension]

QM: "It's been deducted from your final pay. Don't come crying to me."

[End]
```

**Discharge (High Rep 65+, Has Gear):**
```
QM: "Sorry to see you go, [Name]. You were a good soldier.
     
     I'll need the military equipment back, but..."
     
     [Lowers voice]
     
     "...if some of that looted gear you've got happens to stay with you, 
      I didn't see it. Consider it a parting gift."

[Reclaims QM-purchased items only]
[Winks]

QM: "Take care of yourself."

[End]
```

### **8. Special: Officers Armory Access**

**First Time Accessing (Unlock):**
```
QM: "You've proven yourself, Lieutenant. Your reputation precedes you.
     
     I have access to... special inventory. Elite equipment reserved for 
     officers with the right connections.
     
     These pieces have quality modifiers - better than standard issue. 
     But they cost more, and this is a privilege. Don't abuse it.
     
     Want to see what I've got?"

Options:
  [1] Yes, show me. → Opens Officers Armory
  [2] Not right now. (End)
```

**Subsequent Access:**
```
QM: [Looks around]
     "Officers Armory?"

[Opens elite equipment menu]
```

**Lost Access (Rep Dropped Below 60):**
```
Player: [Tries to access Officers Armory]

QM: "Your reputation isn't what it used to be. 
     Officers Armory is off limits until you rebuild trust."

[Access denied]
[End]
```

---

## Reputation Impact Dialogue Variants

### **Reputation Milestones**

**Reaching +50 Rep (Friendly):**
```
QM: "You're proving yourself to be reliable. Keep it up."

[End]
```

**Reaching +75 Rep (Trusted):**
```
QM: "I've got to say, you're one of the best soldiers I've worked with.
     If you need anything, just ask. Within reason, of course."

[Unlocks: Better buyback rates, looks other way on contraband]
[End]
```

**Dropping Below 0 Rep (Unfriendly):**
```
QM: "We've got a problem. Your conduct lately... it's not acceptable.
     
     You're still getting your equipment and rations, but don't expect 
     any favors from me."

[End]
```

**Dropping Below -25 Rep (Hostile):**
```
QM: "I'm watching you. One more incident and I'm recommending discharge."

[+5 Scrutiny]
[End]
```

---

## Implementation: Dialogue Triggers

### **Conversation Entry Points**

```csharp
// Main conversation starter
public void OnQuartermasterConversation()
{
    Hero qm = GetQuartermaster();
    
    // Check for pending events
    if (HasPendingBagageCheck())
    {
        StartBagageCheckDialogue();
        return;
    }
    
    if (IsFirstMeetingWithQM())
    {
        StartIntroductionDialogue();
        return;
    }
    
    if (JustPromotedToT5())
    {
        StartOfficerTransitionDialogue();
        return;
    }
    
    // Standard greeting based on rep
    StartGreetingDialogue();
}

private void StartGreetingDialogue()
{
    int rep = GetQMReputation();
    
    if (rep >= 65)
    {
        ShowDialogue("qm_greeting_friendly");
    }
    else if (rep >= 0)
    {
        ShowDialogue("qm_greeting_neutral");
    }
    else
    {
        ShowDialogue("qm_greeting_hostile");
    }
    
    // Open main menu after greeting
    OpenQuartermasterMenu();
}
```

### **Menu Options Logic**

```csharp
public void OpenQuartermasterMenu()
{
    List<MenuOption> options = new List<MenuOption>();
    
    // Always available
    options.Add(new MenuOption("Buy Armor", () => OpenEquipmentCategory(EquipmentCategory.Armor)));
    options.Add(new MenuOption("Buy Weapons", () => OpenEquipmentCategory(EquipmentCategory.Weapons)));
    options.Add(new MenuOption("Buy Accessories", () => OpenEquipmentCategory(EquipmentCategory.Accessories)));
    
    // Has QM-purchased items
    if (HasQMPurchasedEquipment())
    {
        options.Add(new MenuOption("Sell Equipment Back", () => OpenBuybackMenu()));
    }
    
    // T5+ only
    if (GetPlayerTier() >= 5)
    {
        options.Add(new MenuOption("Buy Provisions", () => OpenProvisionsShop()));
    }
    
    // Always available
    options.Add(new MenuOption("Inquire About Supply Situation", () => ShowSupplyDialogue()));
    
    // Special access
    if (HasOfficersArmoryAccess())
    {
        options.Add(new MenuOption("Officers Armory ⚜️", () => OpenOfficersArmory()));
    }
    
    options.Add(new MenuOption("Leave", () => CloseMenu()));
    
    ShowMenu(options);
}

private bool HasOfficersArmoryAccess()
{
    return GetPlayerTier() >= 5 
        && GetQMReputation() >= 60 
        && GetLanceReputation() >= 50;
}
```

---

## UI Implementation Notes

### **Menu System Architecture**

```
QuartermasterMenuBehavior
  ├─ OpenMainMenu()
  ├─ OpenEquipmentCategory(category)
  │   └─ Opens Master at Arms UI with filter
  ├─ OpenProvisionsShop()
  │   └─ Custom UI: Food shop with quantity selector
  ├─ OpenBuybackMenu()
  │   └─ Custom UI: List QM-purchased items with sell buttons
  ├─ OpenOfficersArmory()
  │   └─ Opens Master at Arms UI with elite filter
  └─ ShowSupplyDialogue()
      └─ Simple text popup based on supply level
```

### **Equipment Category Filtering**

```csharp
public void OpenEquipmentCategory(EquipmentCategory category)
{
    var availableTroops = GetAvailableTroops();
    
    // Build UI showing only relevant equipment differences
    var uiData = new EquipmentUIData
    {
        Category = category,
        AvailableTroops = availableTroops,
        CurrentEquipment = GetCurrentEquipment(),
        QMRepDiscount = GetQMDiscount(),
        CompanySupply = GetCompanySupply()
    };
    
    // Filter troops based on category focus
    if (category == EquipmentCategory.Armor)
    {
        // Highlight armor slots in comparison
        uiData.HighlightSlots = new[] { 
            EquipmentIndex.Head, 
            EquipmentIndex.Body, 
            EquipmentIndex.Leg, 
            EquipmentIndex.Gloves 
        };
    }
    // ... other categories
    
    ShowEquipmentSelectionUI(uiData);
}
```

### **Provisions Shop UI**

```csharp
public void OpenProvisionsShop()
{
    var shopData = new ProvisionsShopData
    {
        CompanySupply = GetCompanySupply(),
        QMReputation = GetQMReputation(),
        PlayerGold = Hero.MainHero.Gold,
        DaysUntilNextMuster = GetDaysUntilNextMuster(),
        AvailableItems = GenerateProvisionInventory()
    };
    
    ShowProvisionsShopUI(shopData);
}

private List<ProvisionItem> GenerateProvisionInventory()
{
    int supply = GetCompanySupply();
    List<ProvisionItem> items = new List<ProvisionItem>();
    
    if (supply >= 70)
    {
        // Full stock
        items.Add(new ProvisionItem("grain", 6, 10));
        items.Add(new ProvisionItem("butter", 4, 40));
        items.Add(new ProvisionItem("cheese", 3, 50));
        items.Add(new ProvisionItem("meat", 3, 30));
        items.Add(new ProvisionItem("fish", 2, 35));
    }
    else if (supply >= 50)
    {
        // Limited stock
        items.Add(new ProvisionItem("grain", 4, 10));
        items.Add(new ProvisionItem("butter", 2, 40));
        items.Add(new ProvisionItem("meat", 2, 30));
    }
    else if (supply >= 30)
    {
        // Minimal stock
        items.Add(new ProvisionItem("grain", 2, 10));
        items.Add(new ProvisionItem("butter", 1, 40));
    }
    else
    {
        // Critical
        items.Add(new ProvisionItem("grain", 2, 10));
    }
    
    return items;
}
```

---

## Placeholder Dialogue Implementation

### **Dialogue String IDs (For Localization)**

```xml
<!-- Greetings -->
<string id="qm_greeting_first" text="You must be the new recruit. I'm {QM_NAME}, the quartermaster..." />
<string id="qm_greeting_neutral" text="What do you need?" />
<string id="qm_greeting_friendly" text="{PLAYER_NAME}, good to see you. What can I do for you today?" />
<string id="qm_greeting_hostile" text="You again. Make it quick." />

<!-- Ration Exchange (T1-T4) -->
<string id="qm_ration_standard" text="Here's your ration for the next twelve days. Make it last." />
<string id="qm_ration_premium" text="Here's your ration, {PLAYER_NAME}. I managed to get you some {FOOD_TYPE} this time. Don't tell the others." />
<string id="qm_ration_none" text="Bad news. Supplies didn't come through this muster. No rations available right now. You'll have to buy from town or forage until things improve." />

<!-- T5 Officer Transition -->
<string id="qm_officer_transition" text="Congratulations on your promotion, Lieutenant. As an officer, you're no longer issued rations. You're expected to manage your own provisions..." />

<!-- Baggage Checks -->
<string id="qm_baggage_clear" text="Routine inspection... Everything's in order. Carry on." />
<string id="qm_baggage_contraband_lookaway" text="I didn't see anything. But be more careful." />
<string id="qm_baggage_contraband_bribe" text="Well well, what do we have here... This isn't standard issue. Where'd you get it?" />
<string id="qm_baggage_contraband_confiscate" text="Caught you. This is going in my report." />

<!-- Equipment Purchase -->
<string id="qm_equipment_purchase" text="That'll be {COST}g. Don't lose this gear - I keep records." />
<string id="qm_equipment_cant_afford" text="You don't have enough. Come back when you've got the coin." />
<string id="qm_equipment_blocked" text="We're critically low on supplies. I can't issue new equipment right now. Complete some supply runs, or wait for the next caravan." />

<!-- Buyback -->
<string id="qm_buyback_offer" text="I'll give you {BUYBACK_PRICE}g for it. That's the best I can do." />
<string id="qm_buyback_premium" text="For you, I can offer {BUYBACK_PRICE}g. That's top rate." />
<string id="qm_buyback_complaint" text="Then don't sell it. My rates are fair. Build your reputation if you want better prices." />

<!-- Provisions (T5+) -->
<string id="qm_provisions_standard" text="What provisions do you need?" />
<string id="qm_provisions_low_stock" text="I've only got grain right now. Two bags. That's it. We're in crisis mode. I'd recommend hitting the town markets until our supply situation improves." />
<string id="qm_provisions_out_of_stock" text="I only have {QUANTITY} of those. Come back after the next muster for a fresh shipment." />

<!-- Supply Situation -->
<string id="qm_supply_good" text="Supplies are good. No concerns at the moment." />
<string id="qm_supply_low" text="We're running a bit low. Nothing critical yet, but keep an eye on it. Complete some supply duties if you can." />
<string id="qm_supply_critical" text="We're in crisis. I can't issue any equipment changes until we resupply. Food rations are suspended. Get supplies NOW." />

<!-- Officers Armory -->
<string id="qm_armory_unlock" text="You've proven yourself, Lieutenant. Your reputation precedes you. I have access to... special inventory. Elite equipment reserved for officers with the right connections..." />
<string id="qm_armory_access" text="Officers Armory?" />
<string id="qm_armory_denied" text="Your reputation isn't what it used to be. Officers Armory is off limits until you rebuild trust." />

<!-- Discharge -->
<string id="qm_discharge_standard" text="Leaving us, eh? I'll need all military equipment back before you go." />
<string id="qm_discharge_missing_gear" text="You're missing some gear from my records. Where is it? You owe {REPLACEMENT_COST}g for missing equipment." />
<string id="qm_discharge_friendly" text="Sorry to see you go, {PLAYER_NAME}. You were a good soldier. Take care of yourself." />

<!-- Reputation Milestones -->
<string id="qm_rep_friendly" text="You're proving yourself to be reliable. Keep it up." />
<string id="qm_rep_trusted" text="I've got to say, you're one of the best soldiers I've worked with. If you need anything, just ask. Within reason, of course." />
<string id="qm_rep_unfriendly" text="We've got a problem. Your conduct lately... it's not acceptable. You're still getting your equipment and rations, but don't expect any favors from me." />
<string id="qm_rep_hostile" text="I'm watching you. One more incident and I'm recommending discharge." />
```

---

## Implementation Checklist

### **Phase 1: NPC Setup**
- [ ] Create quartermaster NPC generation system
- [ ] Assign quartermaster when player enlists
- [ ] Store quartermaster reference in save data
- [ ] Set up basic conversation hook

### **Phase 2: Main Menu**
- [ ] Implement main quartermaster menu
- [ ] Add "Buy Armor" option → Opens equipment with armor filter
- [ ] Add "Buy Weapons" option → Opens equipment with weapons filter
- [ ] Add "Buy Accessories" option → Opens equipment with accessories filter
- [ ] Add "Sell Equipment Back" option → Opens buyback menu
- [ ] Add "Buy Provisions" option (T5+ only) → Opens food shop
- [ ] Add "Inquire About Supply" option → Shows dialogue
- [ ] Add "Officers Armory" option (conditional) → Opens elite menu
- [ ] Implement menu access logic (rank/rep checks)

### **Phase 3: Equipment Menus**
- [ ] Implement category filtering for Master at Arms UI
- [ ] Highlight relevant equipment slots based on category
- [ ] Show discount percentage in UI
- [ ] Block access when supply < 30%
- [ ] Test filtering for all three categories

### **Phase 4: Provisions Shop (T5+)**
- [ ] Create provisions shop UI
- [ ] Implement inventory generation based on supply level
- [ ] Implement price calculation with QM rep discount
- [ ] Add quantity selector for purchases
- [ ] Show "Next resupply in X days"
- [ ] Test with all supply levels (70%+, 50-69%, 30-49%, <30%)

### **Phase 5: Buyback System**
- [ ] Create buyback menu UI
- [ ] List all QM-purchased equipment
- [ ] Calculate buyback prices based on QM rep
- [ ] Implement sell transaction
- [ ] Remove item from QM-purchased registry on sale
- [ ] Test buyback at different rep levels

### **Phase 6: Dialogue System**
- [ ] Implement greeting dialogues (first meeting, neutral, friendly, hostile)
- [ ] Implement ration exchange dialogues (standard, premium, none available)
- [ ] Implement T5 officer transition dialogue
- [ ] Implement baggage check dialogues (all variants)
- [ ] Implement supply inquiry dialogues (all supply levels)
- [ ] Implement discharge dialogues (standard, missing gear, friendly)
- [ ] Implement reputation milestone dialogues

### **Phase 7: Event Responses**
- [ ] Hook ration exchange into muster event
- [ ] Hook baggage checks into muster event (30% chance)
- [ ] Hook T5 transition into promotion event
- [ ] Hook discharge gear reclamation into retirement/discharge event
- [ ] Test all event triggers

### **Phase 8: Officers Armory**
- [ ] Implement Officers Armory access check (T5+, QM 60+, Lance 50+)
- [ ] Create unlock dialogue
- [ ] Filter elite equipment with quality modifiers
- [ ] Test access granted/denied

### **Phase 9: Polish**
- [ ] Add all placeholder dialogue strings
- [ ] Implement rep-based dialogue variants
- [ ] Add UI tooltips and hints
- [ ] Test full conversation flow
- [ ] Test menu transitions

---

**Status**: Ready for implementation planning and prototyping.

**Integration Points:**
- **EnlistmentBehavior**: Quartermaster assignment, ration exchange, muster events
- **EquipmentBehavior**: Master at Arms integration, category filtering
- **QuartermasterManager**: Equipment tracking, buyback system
- **SupplyBehavior**: Supply level checks, blocking logic
- **DialogueBehavior**: Conversation trees, rep-based variants

**Next Steps:**
1. Review dialogue flow and menu structure
2. Create quartermaster NPC templates for each culture
3. Design provisions shop UI mockup
4. Implement Phase 1 (NPC setup)
5. Prototype main menu and category filtering

