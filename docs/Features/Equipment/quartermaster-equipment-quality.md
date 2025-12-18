# Quartermaster Equipment & Supply System

> **Purpose**: Document how company supply state and quartermaster relationship affect equipment access and pricing, with special "Officers Armory" for elite gear.

**Last Updated**: December 18, 2025  
**Status**: Design Phase

---

## Overview - Simplified System

### **Three-Tier System:**

1. **Company Supply** → Gates equipment access (can you buy at all?)
2. **Quartermaster Rep** → Affects equipment price (discount based on relationship)
3. **Officers Armory** → Special access to premium gear (high rep required)

**Philosophy:**
- Low supplies = Can't equip troops (realistic military logistics)
- Good QM rep = Better prices (he's doing you a favor)
- Officers Armory = Reward for elite soldiers with connections

---

## System 1: Baggage Checks (Contraband Search)

### **Purpose:** Periodic inventory checks for contraband items at muster

**Trigger:** Every muster (payday), **30% chance** of baggage check

**What is Contraband?**
- Stolen military equipment (looted from friendly/neutral parties)
- High-value civilian goods (jewelry, fine silks, etc.)
- Illegal items (marked as contraband in item properties)
- **Missing issued equipment** (sold military gear you were issued)

### **Military Equipment Tracking:**

When player buys equipment from the quartermaster, items are **marked as "QM-purchased"** (tracked property).

**Rules:**
1. **QM-purchased gear is tracked** - Quartermaster keeps records of what you bought
2. **Player owns the gear** - Can keep, use, or sell back to QM
3. **Can sell back to quartermaster** - QM buyback price based on rep
4. **Can keep old gear** - Upgrading doesn't require returning old items
5. **All QM gear returned on discharge** - When leaving service, must return ALL QM-purchased items

### **Quartermaster Buyback System:**

**Buyback Price Scale (Based on QM Rep):**

| QM Rep | Buyback Price | Example (100g town value) |
|--------|---------------|---------------------------|
| **-50 to -25** | 30% of town price | 30g |
| **-25 to -10** | 40% of town price | 40g |
| **-10 to 10** | 50% of town price | 50g (base) |
| **10 to 35** | 55% of town price | 55g |
| **35 to 65** | 60% of town price | 60g |
| **65 to 100** | 65% of town price | **65g (max)** |

**Anti-Exploit Math:**
```
Town sells sword for: 100g
Best purchase (Rep 65+, 30% discount): 70g
Best buyback (Rep 65+): 65g
Net result: -5g loss

Player ALWAYS loses money selling back, even at max rep.
Higher rep = smaller loss (but still a loss).
```

**Why This Works:**
- At **0 rep**: Buy 100g, sell back 50g = **-50g loss**
- At **high rep (65+)**: Buy 70g, sell back 65g = **-5g loss**
- **No profit possible** at any rep level
- Higher rep minimizes loss (incentive to build relationship)
- Prevents any buyback exploit

### **Baggage Check Flow:**

```
[Muster Day - Pay Distribution]
  ↓
Roll for baggage check (30% chance)
  ↓
IF baggage check triggered:
  "The quartermaster approaches with a ledger.
   'Routine inspection. Open your bags.'"
  ↓
Search player inventory for contraband items
  ↓
IF contraband found → Consequences based on QM Rep
IF no contraband → "Everything in order. Carry on."
```

**Note:** Baggage checks are for **contraband**, not normal Quartermaster-issued gear. QM-issued items are tracked for **buyback restrictions** and **discharge reclaim**. (Optional later: we can escalate on *missing issued gear* if we decide we want that pressure.)

### **Consequences by QM Reputation:**

| QM Rep | Outcome | Options Available |
|--------|---------|-------------------|
| **65 to 100** (Trusted) | **Look the other way** | "I didn't see anything. But be more careful." *(No penalty)* |
| **35 to 65** (Close) | **Bribe option** | Pay **50% of item value** to keep it, or forfeit item |
| **10 to 35** (Friendly) | **Bribe option (expensive)** | Pay **75% of item value** to keep it, or forfeit item |
| **-10 to 10** (Neutral) | **Confiscation** | Item confiscated, +5 Heat |
| **-25 to -10** (Unfriendly) | **Confiscation + Fine** | Item confiscated, +10 Heat, lose **100g** |
| **-50 to -25** (Hostile) | **Severe Penalty** | Item confiscated, +15 Heat, lose **200g**, -10 QM rep |

### **Equipment Change Flow:**

```
Player visits Quartermaster → Master at Arms
  ↓
Select new troop loadout (e.g., Vlandian Sergeant)
  ↓
Purchase new gear at QM discount (based on rep)
  ↓
New items marked as "QM-purchased" and tracked
  ↓
Player can:
  - Keep old gear (accumulate equipment)
  - Sell old gear back to QM (buyback price based on rep)
  - Sell old gear to town merchants (if not QM-purchased)
```

**Discharge Flow (Leaving Service):**

```
Player leaves service (retirement, desertion, discharge)
  ↓
QM: "Return all military equipment before you go."
  ↓
Scan inventory for ALL QM-purchased items
  ↓
IF player has QM-purchased items:
  - Automatically removed from inventory
  - "Equipment confiscated and returned to quartermaster"
  ↓
IF player already sold QM-purchased items:
  - Must pay replacement cost for missing items
  - Deducted from final pay / pension
```

**This allows:**
- ✅ Keeping multiple gear sets (player choice)
- ✅ Selling back to QM at buyback rate (flexibility)
- ✅ Keeping looted gear (not QM-purchased, yours forever)
- ✅ Trading gear with town merchants (only non-QM gear)

### **Player Experience Examples:**

**Example 1: Trusted QM (Rep 70)**
```
Baggage check triggered.

Contraband found: "Stolen Sword" (value: 300g)

QM: "Hmm. Where'd you get this?"
Player: [Lie] "Found it in the dirt after the battle."
QM: *glances around* "Must have fallen off a cart. Keep it quiet."

Outcome: Keep item, no penalty
```

**Example 2: Close QM (Rep 45)**
```
Baggage check triggered.

Contraband found: "Fine Silk" (value: 200g)

QM: "This isn't standard issue. You know the rules."
Player options:
  [Pay 100g] Keep the item
  [Refuse] Lose item + 5 Heat

If paid: "Alright. I'll mark it as 'lost in transit.' Don't make a habit of this."
```

**Example 3: Neutral QM (Rep 5)**
```
Baggage check triggered.

Contraband found: "Looted Jewelry" (value: 150g)

QM: "This doesn't belong to you. I'm confiscating it."
Player: "Wait—"
QM: "No exceptions. And I'm reporting this."

Outcome: Item confiscated, +5 Heat, cannot bribe
```

**Example 4: Hostile QM (Rep -30)**
```
Baggage check triggered.

Contraband found: "Stolen Armor" (value: 400g)

QM: "I KNEW you were a thief! This is going straight to the captain."

Outcome: 
  - Item confiscated
  - +15 Heat
  - Lose 200g (fine)
  - -10 QM rep (now -40)
  
Message: "The quartermaster storms off with your gear. 
          This will not end well for you."
```

**Example 5: Buyback System (Rep 45)**
```
Player bought "Imperial Scale Armor" from QM for 160g (20% discount, base 200g town price)
Player's QM Rep: 45 (Close)
  ↓
Player visits QM:
  [Sell Back Equipment] → Opens buyback menu
  ↓
QM offers buyback:
  "Imperial Scale Armor"
  Town Value: 200g
  Buyback Price: 120g (60% at Rep 45)
  ↓
Player sells back: +120g
  
Net result: Paid 160g, got 120g back = -40g loss
Higher rep minimizes loss, but still loses money.
```

**Example 6: Best Case Buyback (Rep 75)**
```
Player bought "Vlandian Sword" from QM for 140g (30% discount, base 200g)
Player's QM Rep: 75 (Trusted)
  ↓
Player sells back to QM:
  Buyback Price: 130g (65% at Rep 75 - maximum)
  ↓
Player loses: -10g (5% loss)
  
Even at max rep, you lose money selling back.
But the loss is minimal compared to low rep (50% loss).
```

**Example 7: Leaving Service**
```
Player retires after 2 years service
Inventory contains:
  - Vlandian Sword (QM-purchased, 200g value)
  - Imperial Armor (QM-purchased, 300g value)
  - Looted Battanian Bow (from enemy, 150g value)
  ↓
Discharge process:
  QM: "Return military property."
  - Vlandian Sword: Confiscated
  - Imperial Armor: Confiscated
  - Looted Bow: Kept (not QM-purchased)
  ↓
Player keeps looted gear, loses QM-purchased gear.
```

### **How to Avoid Baggage Checks:**

1. **Don't loot contraband** - Only take unmarked items from enemies
2. **Sell contraband quickly** - Visit towns to offload before muster
3. **Build QM rep** - High rep = QM looks the other way
4. **Hide items** - Stash contraband in settlement chests (future feature)
5. **Bribe before muster** - Talk to QM and "donate" gold to skip check (Rep 35+)

### **Strategic Gameplay:**

**Risk/Reward Decision:**
- Looting valuable gear after battles = profit
- BUT: 30% chance per muster of getting caught
- High QM rep = insurance policy (he'll cover for you)
- Low QM rep = very risky (severe penalties)

**Reputation as Insurance:**
- Rep 65+ = Can loot freely (QM protects you)
- Rep 35-65 = Can bribe if caught (expensive but doable)
- Rep <35 = Don't loot or face severe consequences

### **Why You Can't Exploit the Buyback System:**

**Attempted Exploit:**
```
1. Buy gear from QM at 30% discount (100g town price → 70g)
2. Sell back to QM
3. Repeat infinitely for profit
```

**Why It Doesn't Work:**

| QM Rep | Purchase Price | Buyback Price | Net Result |
|--------|----------------|---------------|------------|
| **0** (Neutral) | 100g (no discount) | 50g (50% of 100g) | **-50g loss** |
| **45** (Close) | 80g (20% off 100g) | 60g (60% of 100g) | **-20g loss** |
| **75** (Trusted) | 70g (30% off 100g) | 65g (65% of 100g) | **-5g loss** |

**At ALL Rep Levels:** You ALWAYS lose money selling back  
**At High Rep (75):** Smallest loss (5%), but still a loss

**No profit possible:**
- Buyback is ALWAYS less than purchase price
- Higher rep minimizes loss, but never eliminates it
- System prevents any buyback exploits

**Legitimate uses:**
- ✅ Get better gear for cheaper (save money on upgrades)
- ✅ Change loadouts flexibly (minimize loss with high rep)
- ✅ Recover some gold when switching gear (better than nothing)
- ✅ Upgrade as you rank up (affordable progression)

**QM-Purchased gear rules:**
- ✅ Can keep multiple gear sets (player choice)
- ✅ Can sell back to QM at buyback rate (always a loss)
- ✅ Confiscated when leaving service
- ❌ Cannot sell to town merchants (they recognize military gear)
- ❌ Buyback always loses money (no exploit possible)

---

## System 2: Company Supply (Access Gate)

### **Purpose:** Determines if equipment selection is available at all

**Tracked By:** `LanceNeedsState.Supplies` (0-100%)

| Supply Level | State | Troop Selection Available? | UI Behavior |
|--------------|-------|----------------------------|-------------|
| 0-29% | Critical | ❌ **BLOCKED** | Button greyed out, tooltip: "Low Supply - Cannot issue equipment" |
| 30-49% | Low | ✅ Available (warning) | Button enabled, yellow warning: "Supply running low" |
| 50-100% | Adequate+ | ✅ Available | Normal |

### **Player Experience:**

```
[Visit Quartermaster Menu]

Supplies at 25%:
  [Master at Arms] ← GREYED OUT
  Tooltip: "Low Supply - Cannot issue equipment
           Complete supply duties or wait for resupply"
           
Supplies at 45%:
  [Master at Arms] ← YELLOW WARNING
  Tooltip: "Supply Low (45%) - Equipment available but limited"
  
Supplies at 70%:
  [Master at Arms] ← Normal
```

**Why This Works:**
- Realistic military logistics (can't equip soldiers with no supplies)
- Creates urgency to maintain supply levels
- Simple binary: Can/can't access equipment

---

## System 3: Quartermaster Buyback (Sell Gear Back)

### **Purpose:** Sell QM-purchased equipment back to quartermaster for gold

**How It Works:**

Player visits Quartermaster menu:
```
[Master at Arms] - Buy equipment (with discount)
[Sell Equipment] ← NEW OPTION - Sell back QM-purchased gear
```

**Sell Equipment Menu:**
```
Shows only QM-purchased items from player inventory:

  [Vlandian Sword]
    Town Value: 200g
    Buyback Price: 130g (65% at Rep 75)
    [Sell]
    
  [Imperial Armor] 
    Town Value: 300g
    Buyback Price: 195g (65% at Rep 75)
    [Sell]
    
  [Looted Bow] ← NOT SHOWN (not QM-purchased)
```

**Buyback minimizes loss:**
- Bought gear at discount, can sell back for partial refund
- Flexibility to change loadouts with smaller loss at high rep
- High rep = minimal loss (5% at max rep, vs 50% at low rep)
- **Never profitable** - always lose some gold selling back

---

## System 4: Quartermaster Reputation (Price Discount)

### **Purpose:** Affects the gold cost of equipment upgrades

**Tracked By:** Hero relation with Quartermaster NPC (-50 to +100)

| QM Relation | Discount | Price Multiplier | Description |
|-------------|----------|------------------|-------------|
| **-50 to -25** | Heavy Markup | **1.4x** | "Hostile - He's gouging you" |
| **-25 to -10** | Markup | **1.2x** | "Unfriendly - Charging extra" |
| **-10 to 10** | None | **1.0x** | "Neutral - Standard pricing" |
| **10 to 35** | Small | **0.9x** | "Friendly - Small favor (10% off)" |
| **35 to 65** | Good | **0.8x** | "Close - Looking out for you (20% off)" |
| **65 to 100** | Excellent | **0.7x** | "Trusted - Best discount (30% off)" |

### **Example:**

```
Base Equipment Cost: 200g (Vlandian Sergeant gear)

QM Relation -40 (Hostile): 280g (+40% markup)
  "He glares. 'You're lucky I'm selling to you at all.'"
  
QM Relation -15 (Unfriendly): 240g (+20% markup)
  "He scowls. 'This ain't charity. Pay up.'"
  
QM Relation 5 (Neutral): 200g (standard)
  "He shrugs. 'Standard military rate.'"
  
QM Relation 20 (Friendly): 180g (-10%)
  "He nods. 'I'll knock a bit off for you.'"
  
QM Relation 50 (Close): 160g (-20%)
  "He smiles. 'You've earned a proper discount.'"
  
QM Relation 85 (Trusted): 140g (-30%)
  "He leans in. 'Best I can do. Don't tell anyone.'"
```

**How to Build QM Rep:**
- **+3 to +5**: Complete quartermaster duties (inventory, distribution)
- **+5 to +10**: Successfully complete supply missions (no losses)
- **+2**: Volunteer for extra quartermaster work detail
- **+5**: Gift valuable items to quartermaster
- **+10**: Defend supply wagons during battles (save the supplies)
- **+3**: Help organize camp logistics
- **+5**: Pass a baggage check with no contraband (shows honesty)
- **-5 to -10**: Caught with contraband at baggage check
- **-10**: Additional penalty if hostile QM confiscates items
- **-5**: Steal from supply wagons (caught)
- **-3**: Skip quartermaster duties when assigned
- **-15**: Lose supplies through negligence

---

## System 5: Officers Armory (Premium Gear)

### **Purpose:** Unlock elite equipment options beyond standard troop selection

### **Requirements to Access:**

1. **Rank:** T5+ (Officer rank)
2. **Quartermaster Rep:** 60+ (Trusted tier)
3. **Lance Rep:** 50+ (Respected in unit)

### **What It Is:**

A special equipment pool with **higher tier gear** and **quality modifiers**:

**Standard Quartermaster (T3):**
- Vlandian Sergeant (T3 gear)
- Vlandian Infantry (T2 gear)
- Vlandian Footman (T1 gear)

**Officers Armory (T5+ with high rep):**
- Vlandian Banner Knight (T5 gear)
- Vlandian Champion (T4+ gear)
- **Fine/Masterwork modifiers available** (Balanced Sword, Reinforced Armor)
- **Named/Heirloom items** (if implemented later)

### **Access Flow:**

```
[Visit Quartermaster Menu]

Player: T5, QM Rep 45, Lance Rep 55

[Master at Arms] - Standard troop selection
[Officers Armory] ← NEW OPTION (if qualified)

Click Officers Armory:
  "The quartermaster leads you to a locked room at the back.
   'Officers only. Pick what you need.'"
   
  Available Troops:
    [Vlandian Banner Knight] - 800g (QM discount: 600g)
      - Masterwork Arming Sword (+5 damage, +2 speed)
      - Reinforced Plate Armor (+5 armor, +8 HP)
      - Balanced Kite Shield (+2 armor, +3 HP)
```

### **Benefits:**

1. **Higher Tier Options** - Access gear 1-2 tiers above your rank
2. **Quality Modifiers** - Fine/Masterwork items (better stats)
3. **Prestige** - Visual distinction (you look like an officer)

### **Limitations:**

- **Higher Cost** - Officers gear is expensive (even with discount)
- **Reputation Loss** - Lose access if QM rep drops below 30
- **Rank Gate** - Must maintain T5+ rank

---

## Implementation Design

### **Step 1: Check Supply Level (Gate Access)**

When player opens quartermaster menu:

```csharp
public bool CanAccessTroopSelection()
{
    var supplies = ScheduleBehavior.Instance?.LanceNeeds?.Supplies ?? 100;
    
    if (supplies < 30)
    {
        // BLOCKED
        InformationManager.DisplayMessage(new InformationMessage(
            "Low Supply - Cannot issue equipment. Complete supply duties or wait for resupply.",
            Colors.Red));
        return false;
    }
    
    if (supplies < 50)
    {
        // WARNING
        InformationManager.DisplayMessage(new InformationMessage(
            $"Supply Low ({supplies}%) - Equipment available but limited.",
            Colors.Yellow));
    }
    
    return true;
}
```

### **Step 2: Calculate QM Buyback Price**

When player sells equipment back:

```csharp
public float GetQuartermasterBuybackMultiplier()
{
    var quartermaster = GetQuartermasterHero();
    if (quartermaster == null) return 0.5f;
    
    int qmRelation = Hero.MainHero.GetRelation(quartermaster);
    
    return qmRelation switch
    {
        < -25 => 0.3f,   // Hostile: 30% of town price
        < -10 => 0.4f,   // Unfriendly: 40% of town price
        < 10  => 0.5f,   // Neutral: 50% of town price
        < 35  => 0.55f,  // Friendly: 55% of town price
        < 65  => 0.6f,   // Close: 60% of town price
        _     => 0.65f   // Trusted: 65% of town price (MAX - prevents profit)
    };
}

public int CalculateBuybackPrice(EquipmentElement equipment)
{
    // Get base town value
    int townValue = equipment.Item.Value;
    
    // Apply buyback multiplier
    float buybackMultiplier = GetQuartermasterBuybackMultiplier();
    int buybackPrice = (int)(townValue * buybackMultiplier);
    
    return buybackPrice;
}
```

### **Step 3: Calculate QM Discount**

When showing equipment cost:

```csharp
public float GetQuartermasterDiscountMultiplier()
{
    var quartermaster = GetQuartermasterHero();
    if (quartermaster == null) return 1.0f;
    
    int qmRelation = Hero.MainHero.GetRelation(quartermaster);
    
    return qmRelation switch
    {
        < -25 => 1.4f,   // Hostile: 40% markup
        < -10 => 1.2f,   // Unfriendly: 20% markup
        < 10  => 1.0f,   // Neutral: Standard
        < 35  => 0.9f,   // Friendly: 10% discount
        < 65  => 0.8f,   // Close: 20% discount
        _     => 0.7f    // Trusted: 30% discount (max)
    };
}

public int CalculateEquipmentCost(CharacterObject troop)
{
    // Base cost from EquipmentManager
    int baseCost = EquipmentManager.Instance.CalculateEquipmentCost(troop, formation);
    
    // Apply QM discount
    float discount = GetQuartermasterDiscountMultiplier();
    int finalCost = (int)(baseCost * discount);
    
    return finalCost;
}
```

### **Step 4: Officers Armory Access Check**

```csharp
public bool CanAccessOfficersArmory()
{
    var enlistment = EnlistmentBehavior.Instance;
    if (!enlistment.IsEnlisted) return false;
    
    // Requirement 1: T5+ rank
    if (enlistment.EnlistmentTier < 5)
        return false;
    
    // Requirement 2: QM Rep 60+
    var quartermaster = GetQuartermasterHero();
    if (quartermaster != null && Hero.MainHero.GetRelation(quartermaster) < 60)
        return false;
    
    // Requirement 3: Lance Rep 50+
    // (Assuming LanceReputation is tracked somewhere)
    if (GetLanceReputation() < 50)
        return false;
    
    return true;
}

public List<CharacterObject> GetOfficersArmoryTroops()
{
    var culture = EnlistmentBehavior.Instance.CurrentLord?.Culture?.StringId;
    int playerTier = EnlistmentBehavior.Instance.EnlistmentTier;
    
    // Get troops from player tier up to +2 tiers
    var troops = new List<CharacterObject>();
    for (int tier = playerTier; tier <= Math.Min(playerTier + 2, 6); tier++)
    {
        troops.AddRange(GetTroopsForCultureAndTier(culture, tier));
    }
    
    return troops;
}
```

### **Step 5: UI Integration**

**Modify:** `src/Features/Equipment/Behaviors/TroopSelectionManager.cs`

```csharp
private void AddTroopSelectionMenus(CampaignGameStarter starter)
{
    // Standard troop selection
    starter.AddGameMenuOption(
        "enlisted_quartermaster_menu",
        "qm_master_at_arms",
        "{=eq_master_at_arms}Master at Arms",
        args =>
        {
            // Check supply level
            var supplies = ScheduleBehavior.Instance?.LanceNeeds?.Supplies ?? 100;
            args.optionLeaveType = GameMenuOption.LeaveType.Manage;
            
            if (supplies < 30)
            {
                args.IsEnabled = false;
                args.Tooltip = new TextObject("Low Supply - Cannot issue equipment");
                return true;
            }
            
            if (supplies < 50)
            {
                args.Tooltip = new TextObject($"Supply Low ({supplies}%)");
            }
            
            return true;
        },
        args => ShowMasterAtArmsPopup()
    );
    
    // Officers Armory (conditional)
    starter.AddGameMenuOption(
        "enlisted_quartermaster_menu",
        "qm_officers_armory",
        "{=eq_officers_armory}Officers Armory",
        args =>
        {
            args.optionLeaveType = GameMenuOption.LeaveType.Manage;
            
            if (!CanAccessOfficersArmory())
            {
                args.IsEnabled = false;
                args.Tooltip = new TextObject("Requires: T5 Rank, QM Rep 60+, Lance Rep 50+");
                return false; // Hide option if not qualified
            }
            
            return true;
        },
        args => ShowOfficersArmoryPopup()
    );
}
```

---

## Player Experience Examples

### **Example 1: Critical Supply (Blocked)**

```
Player: T2 Infantry
Company Supplies: 25% (Critical)
QM Relation: 10 (Neutral)

[Visit Quartermaster Menu]
  [Master at Arms] ← GREYED OUT
  Tooltip: "Low Supply - Cannot issue equipment"
  
Message: "The quartermaster shakes his head. 
         'We're running on empty. No equipment changes until we resupply.'"
         
Action: Player must complete supply duties or wait for resupply caravan
```

### **Example 2: Low Supply, Bad QM Rep**

```
Player: T2 Infantry
Company Supplies: 45% (Low)
QM Relation: -15 (Unfriendly)

[Master at Arms] - Available (yellow warning)

Troop Selection:
  [Vlandian Footman] - Base: 150g → Final: 180g (20% markup)
  [Vlandian Spearman] - Base: 175g → Final: 210g (20% markup)
  
Message: "The quartermaster scowls. 
         'Supplies are low. You want gear? Pay extra.'"
```

### **Example 3: Good Supply, Good QM Rep**

```
Player: T3 Infantry
Company Supplies: 75% (Good)
QM Relation: 35 (Friendly)

[Master at Arms] - Normal

Troop Selection:
  [Vlandian Sergeant] - Base: 300g → Final: 240g (20% discount)
  [Vlandian Pikeman] - Base: 275g → Final: 220g (20% discount)
  
Message: "The quartermaster nods. 
         'I'll cut you a deal. You've been pulling your weight.'"
```

### **Example 4: Officer with High Rep (Officers Armory Access)**

```
Player: T5 Cavalry Officer
Company Supplies: 80% (Good)
QM Relation: 70 (Trusted)
Lance Rep: 60 (Respected)

[Master at Arms] - Normal
[Officers Armory] ← NEW OPTION

Click Officers Armory:

Message: "The quartermaster glances around, then unlocks a back room.
         'Officers only. This is the good stuff. Don't tell the grunts.'"

Troop Selection:
  [Vlandian Banner Knight] - Base: 800g → Final: 560g (30% discount)
    - Masterwork Arming Sword (+5 damage, +2 speed)
    - Masterwork Plate Armor (+5 armor, +8 HP)
    - Masterwork Heater Shield (+4 armor, +6 HP)
    - Fine Warhorse (+1 speed, +1 maneuver)
    
  [Vlandian Champion] - Base: 600g → Final: 420g (30% discount)
    - Balanced Greatsword (+3 damage, +1 speed)
    - Reinforced Coat of Plates (+3 armor, +5 HP)
```

---

## Gameplay Implications

### **Player Incentives:**

1. **Build QM Relationship (Multiple Benefits)**
   - **Baggage Checks:** Rep 65+ = QM looks the other way on contraband
   - **Equipment Discount:** Rep 65+ = 30% off all gear
   - **Officers Armory:** Rep 60+ unlocks elite gear access
   - **Insurance Policy:** High rep protects you from mistakes

2. **Maintain Company Supplies (Critical)**
   - Below 30% = Can't equip troops at all
   - Perform quartermaster duties (inventory, distribution)
   - Complete foraging/supply missions
   - Don't skip "Work Detail" or supply-related orders

3. **Manage Looting Risk**
   - **High QM Rep (65+):** Loot freely, QM covers for you
   - **Medium QM Rep (35-65):** Loot cautiously, be ready to bribe
   - **Low QM Rep (<35):** Don't loot or face severe penalties
   - Sell contraband before muster to avoid checks

4. **Unlock Officers Armory (Elite Gear)**
   - Reach T5 rank (Officer tier)
   - Build QM rep to 60+ (Trusted tier)
   - Build lance rep to 50+
   - Access gear 1-2 tiers above your rank
   - Get Fine/Masterwork quality modifiers

### **Progression Arc:**

**T1-T2 (Laborer/Soldier):**
- Focus on supplies (can't afford to be blocked)
- QM rep secondary (Rep -10 to +30 range, small discounts)
- Start building relationship through duties

**T3-T4 (Veteran/NCO):**
- Supplies usually stable (lord manages)
- QM rep matters (gear getting expensive, aim for +35 to +65 = 20% purchase discount)
- Higher rep = better buyback when switching gear (60% vs 50%)
- Start building toward Officers Armory access (need Rep 60+)

**T5-T6 (Officer/Captain):**
- Officers Armory unlocked at Rep 60+ (if lance rep 50+)
- Push for Rep 65+ for max 30% purchase discount on elite gear
- Max buyback at 65% minimizes loss when changing loadouts
- Prestige and power from Masterwork gear

---

## Integration Points

### **Existing Systems:**

1. **`TroopSelectionManager.cs`**
   - Modify `ShowMasterAtArmsPopup()` to check supply level
   - Calculate QM discount on equipment cost display
   - Add `ShowOfficersArmoryPopup()` for T5+

2. **`LanceNeedsState`** (already exists in `ScheduleBehavior`)
   - Read `Supplies` value (0-100)
   - Gate equipment access at <30% supplies

3. **`EquipmentManager.cs`** (already exists)
   - Already calculates base equipment cost
   - Add QM discount multiplier to final cost

4. **Quartermaster NPC**
   - Track QM hero in lord's party/retinue
   - Use native relationship system (`Hero.GetRelation()`)

### **New Systems Needed:**

1. **Quartermaster NPC Assignment**
   - Create/identify QM hero when player enlists
   - Store reference in `EnlistmentBehavior` or `QuartermasterManager`

2. **Lance Reputation Tracking**
   - Track separate "lance reputation" value (0-100)
   - Increase via: duties, battles, events
   - Decrease via: skipping duties, friendly fire, theft

3. **Officers Armory Troop Pool**
   - Define which troops are "officers armory" eligible (T4-T6)
   - Apply Fine/Masterwork modifiers to officers armory gear

4. **UI Updates**
   - Grey out button with tooltip when supplies <30%
   - Show QM discount in equipment cost display
   - Add "Officers Armory" menu option for qualified players

---

## Technical Notes

### **Supply Level Check:**

```csharp
var supplies = ScheduleBehavior.Instance?.LanceNeeds?.Supplies ?? 100;
bool canEquip = supplies >= 30;
```

### **QM Relationship:**

```csharp
var quartermaster = GetQuartermasterHero(); // New method needed
int relation = Hero.MainHero.GetRelation(quartermaster);
```

### **Equipment Cost Calculation:**

```csharp
// Base cost (already exists)
int baseCost = EquipmentManager.Instance.CalculateEquipmentCost(troop, formation);

// Apply QM discount
float discount = GetQuartermasterDiscountMultiplier(); // 0.5x to 1.2x
int finalCost = (int)(baseCost * discount);
```

### **Officers Armory Modifiers (Future):**

For Officers Armory gear, apply quality modifiers:
```csharp
// Get Fine/Masterwork modifier for item
var modifier = GetRandomModifierForQuality(item, ItemQuality.Fine);

// Apply to equipment
var equipmentElement = new EquipmentElement(item, modifier);
```

---

## Implementation Checklist

### **Phase 1: Quartermaster NPC System**

- [ ] Create `QuartermasterManager` behavior (or add to existing)
- [ ] Assign QM hero when player enlists (retinue member or generated)
- [ ] Track QM hero reference in save data
- [ ] Add methods to get/build QM relationship

### **Phase 1b: Baggage Check System**

- [ ] Add QM-purchased equipment tracking:
  - Mark items as "QM-purchased" when bought from quartermaster
  - Store list of QM-purchased equipment IDs in save data
  - Track throughout service (persists in inventory)

- [ ] Add contraband detection system:
  - Mark items as contraband (stolen, illegal, high-value civilian)
  - Track item source (looted from friendly/neutral vs enemy)

- [ ] Implement baggage check trigger:
  - Hook into muster/payday event
  - 30% chance to trigger check
  - Scan player inventory for contraband only

- [ ] Add consequence logic based on QM rep:
  - Rep 65+: Look the other way (no penalty for contraband)
  - Rep 35-65: Bribe option (50-75% of value)
  - Rep <35: Confiscate + penalties (Heat, fines)

- [ ] Add QM buyback system:
  - Add "Sell to Quartermaster" option in QM menu
  - Calculate buyback price based on QM rep (30-65% of town value, max)
  - Only QM-purchased items can be sold back
  - Remove item from QM-purchased registry when sold back
  - Ensure buyback is always less than best purchase price (no profit)

- [ ] Add discharge/retirement equipment confiscation:
  - On leaving service, scan for all QM-purchased items
  - Automatically remove from inventory
  - If already sold/lost, deduct replacement cost from final pay

- [ ] Add UI/events:
  - Baggage check notification event
  - Bribe inquiry (if rep allows)
  - Confiscation result event
  - Missing gear confrontation event

- [ ] Add optional "pre-emptive bribe" option:
  - Talk to QM before muster
  - Pay gold to skip next baggage check
  - Only available at Rep 35+

### **Phase 2: Supply Gate**

- [ ] Modify `TroopSelectionManager.ShowMasterAtArmsPopup()`:
  - Check supply level before opening
  - Block if supplies <30%
  - Show warning if supplies <50%

- [ ] Update UI:
  - Grey out button when blocked
  - Add tooltip with supply state

### **Phase 3: QM Discount**

- [ ] Add `GetQuartermasterDiscountMultiplier()` method
- [ ] Modify equipment cost calculation:
  - Apply discount to base cost
  - Display both base and discounted price
  - Show QM relation hint in tooltip

### **Phase 4: Officers Armory**

- [ ] Add lance reputation tracking:
  - New field in `EnlistmentBehavior` or separate tracker
  - Increase via duties/battles/events
  - Decrease via misconduct

- [ ] Add `CanAccessOfficersArmory()` check:
  - T5+ rank
  - QM rep 40+
  - Lance rep 50+

- [ ] Create `ShowOfficersArmoryPopup()`:
  - Show troops T4-T6 (higher than standard)
  - Apply Fine/Masterwork modifiers (future)
  - Higher costs (but with 50% discount = reasonable)

- [ ] Add menu option to quartermaster menu

### **Phase 5: Testing**

- [ ] Test supply blocking (<30%)
- [ ] Test QM discount tiers (all relation levels)
- [ ] Test Officers Armory access requirements
- [ ] Test edge cases (QM dies, player demoted, etc.)

---

## Future Enhancements

### **Baggage Check Expansions:**
- [ ] **Stash System**: Hide contraband in settlement chests to avoid checks
- [ ] **Informant Risk**: Other soldiers can report you to QM (reduce check chance with high lance rep)
- [ ] **Smuggler Contacts**: NPC who buys contraband at premium (bypasses muster risk)
- [ ] **False Accusations**: Low QM rep = random accusations even without contraband
- [ ] **Witness Protection**: Other soldiers vouch for you during checks (lance rep matters)

### **Equipment System:**
- [ ] **Item Modifiers in Officers Armory**: Apply Fine/Masterwork quality to gear
- [ ] **Lance Reputation Events**: Specific events to raise/lower lance rep
- [ ] **Black Market**: Buy gear without supply constraints (expensive)
- [ ] **Looting System**: Salvage gear from battlefields (bypasses QM/supply)
- [ ] **Crafting Integration**: Player-crafted gear ignores supply constraints
- [ ] **Lord Rewards**: Lord can grant Officers Armory access as reward
- [ ] **Regional Supply Variation**: Some regions have better supply than others
- [ ] **Emergency Requisition**: Spend gold to bypass supply gate (expensive)

---

## Summary

### **Four-System Integration:**

| System | Purpose | Gating Factor | Player Impact |
|--------|---------|---------------|---------------|
| **Baggage Checks** | Can you keep contraband? | QM Rep + 30% chance at muster | Confiscation/fines or look away |
| **Supply Gate** | Can you access equipment? | Company Supplies <30% | BLOCKED if low |
| **QM Discount** | How much does it cost? | QM Relationship (-50 to +100) | +40% markup to -30% discount |
| **Officers Armory** | Can you get elite gear? | T5 + QM Rep 60+ + Lance Rep 50+ | +1-2 tier gear |

**Why This Works:**
- ✅ **Interconnected** - QM rep affects multiple systems (checks, price, elite gear)
- ✅ **Risk/Reward** - Looting is profitable but dangerous without good QM rep
- ✅ **Realistic** - Military quartermasters control supplies AND enforce rules
- ✅ **Rewarding** - High rep = protection, savings, and elite access
- ✅ **Strategic Depth** - Players must balance looting profit vs. getting caught
- ✅ **Clear Progression** - Rep thresholds create goals (35 for bribes, 60 for armory, 65 for protection)
- ✅ **Exploit-Proof** - Issued equipment is tracked and can't be sold for profit
- ✅ **Fair Economics** - Discount saves money on legitimate upgrades, not infinite money glitch

---

**Status**: Ready for implementation approval.

