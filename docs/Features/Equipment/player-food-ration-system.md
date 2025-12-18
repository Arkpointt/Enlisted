# Player Food Ration System

> **Purpose**: Document how the player receives food rations at muster, with quality tied to QM reputation and events triggered by food surplus/spoilage.

**Last Updated**: December 18, 2025  
**Status**: Design Phase

---

## Overview

The food system **changes based on rank**, reflecting the player's progression from soldier to officer:

### **T1-T4 (Enlisted/NCO): Issued Rations System**

Player receives **issued rations** at muster (every 12 days), with quality determined by Quartermaster reputation and company supply levels.

**Key Innovation: Ration Exchange System**

At each muster, the quartermaster:
1. **If issuing a new ration**, **reclaims** the previous issued ration (if it still exists)
2. **Issues** a new ration (or sometimes none, based on supplies)
3. **If no new ration is issued**, nothing is reclaimed (the player keeps what they have)

This creates **real scarcity** - the player can't just accumulate endless food. They must actively manage their food supply by buying from towns or foraging when the QM can't provide rations.

**T1-T4 Principles:**
- Player is 1 party member → needs ~0.6 food for 12 days between musters
- QM tracks "issued" food separately from "personal" food (bought/foraged)
- Company supply levels determine if rations are available
- QM reputation determines food quality when rations ARE available
- Player must supplement with personal food during shortages
- Uses vanilla food consumption math; we only add/remove items from the player's inventory (and do not change AI/native food behaviors).

### **T5+ (Officer): Quartermaster Provisions System**

At T5 (Officer rank), the player **stops receiving issued rations** and instead **buys provisions** from the Quartermaster for their own needs (and later, for their troops).

**Officer Provisioning:**
1. **No issued rations** - Player is responsible for their own food
2. **QM Shop available** - Buy food at 150-200% of town market prices
3. **Inventory refreshes every muster** - New stock every 12 days
4. **Supply-dependent availability** - High supply = better selection, low supply = limited stock
5. **QM reputation affects prices** - Max discount brings it to 150% (still more than towns)

This reflects the officer's responsibility to manage their own logistics and prepares them for leading troops with food requirements.

---

## Native Food Consumption

### **Vanilla Food Math:**

From `DefaultMobilePartyFoodConsumptionModel.cs`:

```csharp
public override int NumberOfMenOnMapToEatOneFood => 20;

public override ExplainedNumber CalculateDailyBaseFoodConsumptionf(
    MobileParty party,
    bool includeDescription = false)
{
    int num = party.Party.NumberOfAllMembers + party.Party.NumberOfPrisoners / 2;
    return new ExplainedNumber((float) -(num < 1 ? 1.0 : (double) num) / (float) this.NumberOfMenOnMapToEatOneFood);
}
```

**Key Formula:**
```
Daily Food Consumption = (Party Members + Prisoners/2) / 20
```

**For Player (1 party member):**
```
Daily Consumption = 1 / 20 = 0.05 food per day
```

**To last 12 days:**
```
Food Needed = 0.05 × 12 = 0.6 food
```

**However**, food items in Bannerlord are whole units. The player needs **1 food item** to last 20 days (as a solo party member).

---

## Food Item Types

From `DefaultItems.cs`, the two main food items are:

| Item ID | Name | Value | Weight | Days for 1 Player |
|---------|------|-------|--------|-------------------|
| `grain` | Grain | 10g | 10.0 | 20 days |
| `meat` | Meat | 30g | 10.0 | 20 days |

**Additional Food Items** (from native game):
- `butter` - Higher value, same nutrition
- `cheese` - Higher value, same nutrition
- `fish` - Similar to meat
- `olives` - Lower value
- `dates` - Lower value

**All food items provide the same nutrition per unit** (1 food = 20 days for 1 person). The difference is **value/cost** and **flavor**.

---

## Muster Food Ration System

### **Current Muster System:**

From `EnlistmentBehavior.cs`:

```csharp
// Accrue daily wage into muster ledger (no clan finance involvement)
var wage = CalculateDailyWage();
if (wage > 0 && !_payMusterPending)
{
    _pendingMusterPay += wage;
}

// Schedule pay muster when due; gate to one active incident at a time
if (!_payMusterPending && CampaignTime.Now >= _nextPayday)
{
    _payMusterPending = true;
    // ... trigger muster event
}
```

**Muster occurs every 12 days** (`_nextPayday` is set to `CampaignTime.DaysFromNow(12)`).

### **New: Ration Exchange at Muster**

At each muster, the quartermaster performs a **ration exchange**:

**Step 1: Check Supply Availability**
- Based on company supply levels, determine if rations are available
- Low supplies = no new ration issued

**Step 2: Reclaim Old Issued Ration (Only If We Are Issuing A New One)**
- QM takes back the previously issued ration item (if it still exists in inventory/stowage)
- Implementation uses a persistent "issued ration registry" (not a magic item flag)
- Personal food (bought/foraged) is never touched

**Step 3: Issue New Ration (If Available)**
- Quality determined by QM reputation
- Recorded into the issued ration registry for next time

**Step 4: Pay Wages**
- Gold payment (existing system)

### **Ration Availability (Company Supply)**

Whether the player receives a NEW ration depends on company supply levels:

| Company Supply | Ration Chance | Player Experience |
|----------------|---------------|-------------------|
| **70-100%** | 100% | Always get rations |
| **50-69%** | 80% | Occasional shortages (1 in 5 musters) |
| **30-49%** | 50% | Frequent shortages (half the time) |
| **< 30%** | 0% | No rations ever (equipment also blocked) |

**When no ration is issued:**
- Old ration is NOT reclaimed (player keeps what they have)
- Player receives message: "Supplies are too low. No rations available this muster."
- Player must buy food from towns (20-50g) or forage
- If player has 8+ days of food remaining, this is manageable
- If player has <3 days, this creates a survival crisis

**This creates tension:** Player can't coast on accumulated rations. They must actively manage food during campaign.

### **Food Quality Based on QM Rep (When Rations Available):**

| QM Reputation | Food Item | Value | Flavor Text |
|---------------|-----------|-------|-------------|
| **-50 to -10** | `grain` | 10g | "You receive a small sack of grain. It's moldy and barely edible." |
| **-9 to 19** | `grain` | 10g | "You receive a sack of grain. Standard rations." |
| **20 to 49** | `butter` | 40g | "You receive a pat of butter. Better than grain, at least." |
| **50 to 79** | `cheese` | 50g | "You receive a wedge of cheese. The quartermaster is being generous." |
| **80 to 100** | `meat` | 30g | "You receive a portion of salted meat. The quartermaster favors you." |

### **Implementation:**

```csharp
// Track issued rations (save/load this list)
private List<ItemRosterElement> _issuedFoodRations = new List<ItemRosterElement>();

private void ProcessMusterFoodRation()
{
    Hero hero = Hero.MainHero;
    MobileParty party = MobileParty.MainParty;
    
    // Step 1: Check if rations are available (company supply)
    int companySupply = GetCompanySupply(); // 0-100%
    bool rationAvailable = DetermineRationAvailability(companySupply);
    
    if (!rationAvailable)
    {
        // No rations this muster
        InformationManager.DisplayMessage(new InformationMessage(
            new TextObject("{=no_rations}Supplies are too low. No rations available this muster.").ToString(),
            Colors.Red));
        ModLogger.Info("Muster", $"No rations issued (Company Supply: {companySupply}%)");
        return;
    }
    
    // Step 2: Reclaim old issued rations ONLY when we are issuing a new one.
    // This matches the intended behavior: if supplies are too low, we do not confiscate the player's last issued ration.
    ReclaimIssuedRations();

    // Step 3: Issue new ration (quality based on QM rep) and TRACK it
    Hero quartermaster = GetQuartermaster();
    int qmRep = quartermaster != null ? hero.GetRelation(quartermaster) : 0;
    
    ItemObject newRation = GetFoodItemForReputation(qmRep);
    EquipmentElement rationElement = new EquipmentElement(newRation);
    
    // Add to inventory
    party.ItemRoster.AddToCounts(newRation, 1);
    
    // TRACK as issued ration (prevents hiding in stowage)
    _issuedFoodRations.Add(new ItemRosterElement(rationElement, 1));
    
    // Display message
    string flavorText = GetFoodFlavorText(qmRep);
    InformationManager.DisplayMessage(new InformationMessage(
        new TextObject("{=food_ration_received}" + flavorText).ToString()));
    
    ModLogger.Info("Muster", $"Food ration issued and tracked: {newRation.Name} (QM Rep: {qmRep}, Supply: {companySupply}%)");
}

private void ReclaimIssuedRations()
{
    if (_issuedFoodRations == null || _issuedFoodRations.Count == 0)
        return;
    
    MobileParty party = MobileParty.MainParty;
    
    foreach (var rationElement in _issuedFoodRations)
    {
        ItemObject item = rationElement.EquipmentElement.Item;
        int amountToReclaim = rationElement.Amount;
        
        // Check main inventory
        int inInventory = party.ItemRoster.GetItemNumber(item);
        
        // Check stowage (if player has stowage system)
        int inStowage = GetItemCountInStowage(item);
        
        int totalAvailable = inInventory + inStowage;
        int actuallyReclaimed = Math.Min(amountToReclaim, totalAvailable);
        
        if (actuallyReclaimed > 0)
        {
            // Reclaim from main inventory first
            int fromInventory = Math.Min(actuallyReclaimed, inInventory);
            if (fromInventory > 0)
            {
                party.ItemRoster.AddToCounts(item, -fromInventory);
            }
            
            // Then reclaim from stowage if needed (prevents hiding)
            int remaining = actuallyReclaimed - fromInventory;
            if (remaining > 0 && inStowage > 0)
            {
                RemoveItemFromStowage(item, remaining);
            }
            
            ModLogger.Info("Muster", 
                $"Reclaimed issued ration: {item.Name} x{actuallyReclaimed}");
        }
        else
        {
            // Ration was consumed or sold - that's fine, no penalty
            ModLogger.Info("Muster", 
                $"Issued ration consumed before muster: {item.Name}");
        }
    }
    
    // Clear tracking for next cycle
    _issuedFoodRations.Clear();
}

private bool DetermineRationAvailability(int companySupply)
{
    if (companySupply >= 70) return true; // Always available
    if (companySupply >= 50) return MBRandom.RandomFloat < 0.80f; // 80% chance
    if (companySupply >= 30) return MBRandom.RandomFloat < 0.50f; // 50% chance
    return false; // Never available below 30%
}

private ItemObject GetFoodItemForReputation(int qmRep)
{
    if (qmRep >= 80)
        return MBObjectManager.Instance.GetObject<ItemObject>("meat");
    if (qmRep >= 50)
        return MBObjectManager.Instance.GetObject<ItemObject>("cheese");
    if (qmRep >= 20)
        return MBObjectManager.Instance.GetObject<ItemObject>("butter");
    
    // Default: grain (for rep < 20)
    return MBObjectManager.Instance.GetObject<ItemObject>("grain");
}

private string GetFoodFlavorText(int qmRep)
{
    if (qmRep >= 80)
        return "You receive a portion of salted meat. The quartermaster favors you.";
    if (qmRep >= 50)
        return "You receive a wedge of cheese. The quartermaster is being generous.";
    if (qmRep >= 20)
        return "You receive a pat of butter. Better than grain, at least.";
    if (qmRep >= -9)
        return "You receive a sack of grain. Standard rations.";
    
    // Low rep
    return "You receive a small sack of grain. It's moldy and barely edible.";
}
```

---

## Managing Food Scarcity

### **The Challenge: Ration Exchange System**

With the ration exchange system, the player **cannot accumulate** military rations. Each muster:
- Old issued ration is reclaimed
- New ration is issued (or not, based on supplies)
- Net result: Player has 0-1 issued rations at any time

**This creates real scarcity:**
- Player must buy/forage personal food during shortages
- Player must plan ahead when supplies are low (50-69% = 20% chance no ration)
- Critical situations when supplies <30% (no rations ever)

**Math of Scarcity:**
- Player needs 0.6 food per 12-day cycle
- Issued ration (1.0 food) lasts 20 days
- **BUT** if no ration issued at next muster, player has ~8 days to find food

### **Player Food Sources:**

For **T1–T4**, the player can supplement issued rations with **personal food** (never reclaimed):

| Source | Method | Cost | Notes |
|--------|--------|------|-------|
| **Buy from Town** | Visit marketplace | 20-50g per food | Most reliable, always available |
| **Forage** (Duty) | Complete foraging duty | Free | +3-8% Company Supply, +1 grain |
| **Ask Comrades** | Event/dialogue | -5 Lance Rep | One-time emergency option |
| **Hunt Wildlife** | Event (Cunning 30+) | Free | Random encounter, +1 meat |

For **T5+**, see the Officer Provisions section: the Quartermaster sells provisions at **premium pricing** (150–200% of town values).

**Strategic Food Management:**

**Scenario 1: Supplies 70%+ (Stable)**
- Issued rations always available
- No action needed unless planning long siege
- Optional: Buy 1-2 personal food as buffer

**Scenario 2: Supplies 50-69% (Risky)**
- 20% chance no ration at muster
- Recommended: Keep 1 personal food as backup
- Cost: ~30g every 2-3 musters

**Scenario 3: Supplies 30-49% (Crisis)**
- 50% chance no ration at muster
- Required: Actively buy/forage food
- Plan for 50-75g food costs per muster cycle
- Prioritize supply-generating duties (foraging, inventory management)

**Scenario 4: Supplies <30% (Critical)**
- No rations ever
- Equipment menu blocked
- Must buy food every 12 days (~40g)
- Crisis events may offer food options (see below)

---

---

---

## Personal Food Loss Events

### **Problem: Building Reserves Without Risk**

If players build large personal food reserves (3+ items), they can bypass the scarcity system entirely. We need random loss events to create ongoing food pressure.

**Important:** These events ONLY affect **personal food** (bought/foraged). Issued rations are immune (they're reclaimed at muster anyway).

### **When to Trigger:**

Check daily if player has **2+ personal food items** (excluding currently issued ration):

```csharp
private void CheckPersonalFoodLossEvents()
{
    MobileParty party = MobileParty.MainParty;
    
    // Count personal food (total food minus currently issued rations)
    int totalFood = party.ItemRoster.TotalFood;
    int issuedFood = CountIssuedRations(); // 0 or 1
    int personalFood = totalFood - issuedFood;
    
    // Only trigger if player has 2+ personal food items
    if (personalFood < 2) return;
    
    // Base chance: 5% per day with 2-3 personal food
    float chance = 0.05f;
    
    // Increase chance if player is hoarding (4+ personal food)
    if (personalFood >= 4) chance = 0.10f;
    if (personalFood >= 6) chance = 0.15f;
    
    if (MBRandom.RandomFloat < chance)
    {
        TriggerPersonalFoodLossEvent();
    }
}
```

### **Food Loss Event Types:**

These events target ONLY personal food items (never the issued ration).

---

#### **1. Spoilage (Summer/Heat)**

```
Event: "Food Spoilage"

The summer heat has spoiled some of your personal food stores. 
Flies buzz around the rotten provisions.

Effect: 
  - Lose 1 personal food item (random, NOT the issued ration)
```

**Conditions:**
- Summer season
- Not in settlement
- 2+ personal food items
- 5-10% chance per day

**Trigger Frequency:** Higher in summer, desert terrain

---

#### **2. Rats in Camp**

```
Event: "Rats in Camp"

You discover rats have gotten into your personal food supplies. 
Several provisions are ruined.

Options:
  [Accept Loss] → Lose 1 personal food item
  [Hunt the Rats] (Cunning 50+) → 50% chance: No loss + 5 Cunning XP
                                    50% chance: Still lose food
  [Set Traps] (Costs 5g) → Lose food but gain 1 fur (worth 10g)
```

**Conditions:**
- Any season
- In camp (not settlement)
- 2+ personal food items
- 5% chance per day

---

#### **3. Theft by Comrade**

```
Event: "Missing Provisions"

You notice some of your personal food supplies have gone missing. 
A lance mate eyes you guiltily.

Options:
  [Confront Him] → Lose 1 food, gain event "Lance Mate Debt"
                   (He owes you; +10 rep when he repays)
  [Let It Slide] → Lose 1 food, +5 Lance Rep (generous)
  [Report to Leader] → Lose 1 food, thief punished, -10 Lance Rep
```

**Conditions:**
- With lance
- 3+ personal food items
- Lance Rep < 70
- 3% chance per day

**Follow-up:** "Lance Mate Debt" can trigger "Debt Repaid" event later where they give you food or gold.

---

#### **4. Damaged Supplies (Battle)**

```
Event: "Battle Damage"

Your pack was damaged in the recent battle. Some of your 
personal provisions are ruined or scattered.

Effect: 
  - Lose 1 personal food item
  - If you have 4+ personal food, lose 2 instead
```

**Conditions:**
- Triggered after any battle (not daily check)
- 2+ personal food items
- 20% chance after battle (scales with battle intensity)

---

#### **5. Beggar/Refugee Request**

```
Event: "Desperate Refugee"

A starving refugee approaches your camp, begging for food. 
She has a small child with her.

Options:
  [Give Food] → Lose 1 food, +5 Honor, +10 Charm XP
  [Refuse] → No loss, -3 Honor
  [Sell Food] → Lose 1 food, +20 gold, -5 Honor, -5 Lance Rep
```

**Conditions:**
- War zone or recently raided area
- 2+ personal food items
- 3% chance per day

---

#### **6. Inspection/Bribe (Corrupt Officials)**

```
Event: "Checkpoint Shakedown"

Guards at a checkpoint demand "tribute" - they eye your provisions hungrily.

Options:
  [Pay Bribe] → Pay 30g, keep food
  [Give Food] → Lose 1 food, no gold cost
  [Refuse] (Roguery 40+) → 60% chance: No loss
                           40% chance: Lose 1 food + 10g "fine"
```

**Conditions:**
- Traveling through non-friendly territory
- 2+ personal food items
- 2% chance per day

---

## Food Shortage Events

### **Problem: Ration Not Issued + No Personal Food**

With the ration exchange system, shortages are a **regular occurrence** during campaigns with low supplies. The player must actively manage food.

**Trigger Check** (on daily tick):

```csharp
private void CheckFoodShortageEvents()
{
    MobileParty party = MobileParty.MainParty;
    
    // Calculate days of food remaining
    int daysOfFood = party.GetNumDaysForFoodToLast();
    
    // Only trigger if player has < 3 days of food
    if (daysOfFood >= 3) return;
    
    // Check if starving
    if (party.Party.IsStarving)
    {
        TriggerStarvationEvent();
    }
    else if (daysOfFood < 3)
    {
        TriggerLowFoodWarning();
    }
}
```

### **Low Food Warning:**

```
Warning: "Running Low on Food"

You're running low on rations. You should buy food from a town or forage before you starve.

Days of food remaining: 2
```

### **Starvation Event:**

```
Event: "Starving"

You have no food left. Your stomach growls painfully.

Options:
  [Buy Food from Town] → Costs 20-50g depending on settlement
  [Forage] (Cunning 30+) → Gain 1 food (grain), +5 Cunning XP
  [Ask Quartermaster] (QM Rep 30+) → Gain 1 food (grain), -10 QM Rep
  [Endure] → -10 HP, -5 Morale
```

**Conditions:**
- Party.IsStarving == true
- Player has < 1 day of food

---

## T5+ Officer Provisioning System

### **The Officer's Burden: Self-Sufficient Logistics**

At **T5 (Officer rank)**, the player transitions from receiving issued rations to **managing their own provisions**. This reflects the increased responsibility and autonomy of officer rank.

**Key Changes at T5:**
- ✅ No more issued rations (ration exchange system ends)
- ✅ Must buy food from Quartermaster for personal use
- ✅ Prepares player for later troop leadership with food requirements
- ✅ QM shop available with limited, supply-dependent inventory

### **Quartermaster Food Shop (Officers Only)**

**Access:** T5+ ranks only, available via Quartermaster menu

**Pricing Structure:**

| QM Reputation | Price Multiplier | Example (Grain base: 10g) |
|---------------|------------------|---------------------------|
| **-50 to 0** | 2.0x (200%) | 20g per grain |
| **1 to 30** | 1.9x (190%) | 19g per grain |
| **31 to 60** | 1.75x (175%) | 17.5g per grain |
| **61 to 100** | **1.5x (150%)** | **15g per grain** (minimum) |

**Key Point:** Even with maximum QM reputation, officers pay **150% of town market prices**. This is a premium for convenience and immediate availability during campaigns.

**Why the Premium?**
- QM provides food in the field (no need to visit towns)
- Inventory refreshes every muster (guaranteed availability)
- Officers are paid more (can afford the premium)
- Reflects the cost of military logistics vs. civilian markets

### **Inventory: Supply-Dependent Stock**

The QM's food inventory **refreshes every muster (12 days)** and depends on company supply levels:

#### **High Supply (70-100%): Full Stock**

| Food Item | Quantity | Base Price | Officer Price (Max Discount) |
|-----------|----------|------------|------------------------------|
| Grain | 6 | 10g | 15g |
| Butter | 4 | 40g | 60g |
| Cheese | 3 | 50g | 75g |
| Meat | 3 | 30g | 45g |
| Fish | 2 | 35g | 52.5g |

**Total Available:** ~1.5 food items worth (~30 days for 1 person)

#### **Medium Supply (50-69%): Limited Stock**

| Food Item | Quantity | Base Price | Officer Price (Max Discount) |
|-----------|----------|------------|------------------------------|
| Grain | 4 | 10g | 15g |
| Butter | 2 | 40g | 60g |
| Meat | 2 | 30g | 45g |

**Total Available:** ~0.8 food items worth (~16 days for 1 person)

#### **Low Supply (30-49%): Minimal Stock**

| Food Item | Quantity | Base Price | Officer Price (Max Discount) |
|-----------|----------|------------|------------------------------|
| Grain | 2 | 10g | 15g |
| Butter | 1 | 40g | 60g |

**Total Available:** ~0.3 food items worth (~6 days for 1 person) - **Not enough for full muster cycle!**

#### **Critical Supply (<30%): Bare Minimum**

| Food Item | Quantity | Base Price | Officer Price (Max Discount) |
|-----------|----------|------------|------------------------------|
| Grain | 2 | 10g | 15g |

**Total Available:** 0.2 food items worth (~4 days for 1 person) - **Critical shortage!**

**During critical supply, officers must:**
- Buy from town markets (cheaper but requires travel)
- Use foraging duties
- Risk starvation if siege/campaign prevents town access

### **Officer Food Management Strategy**

**Solo Officer (1 party member):**

**Monthly Consumption:**
- 30 days ÷ 20 days per food = **1.5 food items per month**

**Cost Comparison:**
- **Town markets:** 1.5 food × ~30g avg = **~45g per month**
- **QM (max discount):** 1.5 food × ~45g avg = **~68g per month**
- **Premium cost:** ~23g per month for convenience

**Verdict:** QM is convenient but more expensive. Smart officers buy from towns when possible.

**When to Use QM:**
- Active campaign (no time to visit towns)
- Siege/war (towns inaccessible)
- Emergency (ran out of food suddenly)
- High wages (premium doesn't matter)

**Officer with Small Detachment (5 troops - future feature):**

**Monthly Consumption:**
- 5 party members × 30 days ÷ 20 = **7.5 food items per month**

**Cost at QM (max discount):**
- 7.5 food × ~45g avg = **~338g per month**

**This is significant!** Officers leading troops must carefully manage logistics.

### **Implementation: Officer Provisioning**

```csharp
private void ProcessMusterForOfficer()
{
    // Officers (T5+) don't get issued rations
    // They buy from QM shop instead
    
    Hero hero = Hero.MainHero;
    int playerTier = GetPlayerTier();
    
    if (playerTier < 5)
    {
        // T1-T4: Process issued rations (existing system)
        ProcessMusterFoodRation();
        return;
    }
    
    // T5+: Refresh QM food inventory
    RefreshQuartermasterFoodInventory();
    
    // Display message
    InformationManager.DisplayMessage(new InformationMessage(
        new TextObject("{=officer_provisions}Muster complete. Provisions available from Quartermaster.")
            .ToString()));
    
    ModLogger.Info("Muster", $"Officer muster: QM food shop refreshed (T{playerTier})");
}

private void RefreshQuartermasterFoodInventory()
{
    int companySupply = GetCompanySupply(); // 0-100%
    
    // Clear old inventory
    _qmFoodShop.Clear();
    
    // Determine inventory based on supply level
    if (companySupply >= 70)
    {
        // High supply: Full stock
        _qmFoodShop.Add(new QMFoodItem("grain", 6, 10));
        _qmFoodShop.Add(new QMFoodItem("butter", 4, 40));
        _qmFoodShop.Add(new QMFoodItem("cheese", 3, 50));
        _qmFoodShop.Add(new QMFoodItem("meat", 3, 30));
        _qmFoodShop.Add(new QMFoodItem("fish", 2, 35));
    }
    else if (companySupply >= 50)
    {
        // Medium supply: Limited stock
        _qmFoodShop.Add(new QMFoodItem("grain", 4, 10));
        _qmFoodShop.Add(new QMFoodItem("butter", 2, 40));
        _qmFoodShop.Add(new QMFoodItem("meat", 2, 30));
    }
    else if (companySupply >= 30)
    {
        // Low supply: Minimal stock
        _qmFoodShop.Add(new QMFoodItem("grain", 2, 10));
        _qmFoodShop.Add(new QMFoodItem("butter", 1, 40));
    }
    else
    {
        // Critical supply: Bare minimum
        _qmFoodShop.Add(new QMFoodItem("grain", 2, 10));
    }
    
    ModLogger.Info("QM", $"Food inventory refreshed: {_qmFoodShop.Count} items (Supply: {companySupply}%)");
}

private int CalculateOfficerFoodPrice(int basePrice, int qmRep)
{
    // Base multiplier: 2.0x (200%)
    float multiplier = 2.0f;
    
    // Apply QM reputation discount
    if (qmRep >= 61)
        multiplier = 1.5f;  // Max discount: 150%
    else if (qmRep >= 31)
        multiplier = 1.75f; // Medium: 175%
    else if (qmRep >= 1)
        multiplier = 1.9f;  // Low: 190%
    // else: 2.0x (200% for negative rep)
    
    return (int)(basePrice * multiplier);
}

public void BuyFoodFromQuartermaster(string foodItemId, int quantity)
{
    // Find item in QM shop
    var shopItem = _qmFoodShop.FirstOrDefault(i => i.ItemId == foodItemId);
    if (shopItem == null || shopItem.Quantity < quantity)
    {
        InformationManager.DisplayMessage(new InformationMessage(
            new TextObject("{=qm_out_of_stock}The quartermaster doesn't have that much in stock.")
                .ToString(), Colors.Red));
        return;
    }
    
    // Calculate price
    Hero hero = Hero.MainHero;
    Hero qm = GetQuartermaster();
    int qmRep = qm != null ? hero.GetRelation(qm) : 0;
    int pricePerItem = CalculateOfficerFoodPrice(shopItem.BasePrice, qmRep);
    int totalCost = pricePerItem * quantity;
    
    // Check if player can afford
    if (hero.Gold < totalCost)
    {
        InformationManager.DisplayMessage(new InformationMessage(
            new TextObject("{=qm_cant_afford}You can't afford that. Cost: {COST}g")
                .SetTextVariable("COST", totalCost).ToString(), Colors.Red));
        return;
    }
    
    // Process purchase
    GiveGoldAction.ApplyBetweenCharacters(hero, null, totalCost);
    
    ItemObject foodItem = MBObjectManager.Instance.GetObject<ItemObject>(foodItemId);
    MobileParty.MainParty.ItemRoster.AddToCounts(foodItem, quantity);
    
    // Reduce QM shop stock
    shopItem.Quantity -= quantity;
    
    // Display message
    InformationManager.DisplayMessage(new InformationMessage(
        new TextObject("{=qm_food_bought}Purchased {QUANTITY}x {ITEM} for {COST}g")
            .SetTextVariable("QUANTITY", quantity)
            .SetTextVariable("ITEM", foodItem.Name)
            .SetTextVariable("COST", totalCost).ToString()));
    
    ModLogger.Info("QM", $"Officer bought food: {quantity}x {foodItemId} for {totalCost}g (Rep: {qmRep})");
}

public class QMFoodItem
{
    public string ItemId { get; set; }
    public int Quantity { get; set; }
    public int BasePrice { get; set; }
    
    public QMFoodItem(string itemId, int quantity, int basePrice)
    {
        ItemId = itemId;
        Quantity = quantity;
        BasePrice = basePrice;
    }
}
```

### **UI Integration: QM Food Shop**

**Access:** Quartermaster menu (T5+ only)

**Menu Option:**
```
[Master at Arms] - Equipment selection (existing)
[Buy Provisions] - Food shop (NEW for T5+)
[Sell Equipment] - Buyback system (existing)
[Leave]
```

**Shop UI:**

```
=== QUARTERMASTER PROVISIONS ===

Company Supply: 75% (Good stock)
Your Reputation: +45 (15% discount - 175% of market prices)

Available Provisions:
  [1] Grain (6 available) - 17g each (market: 10g)
  [2] Butter (4 available) - 70g each (market: 40g)
  [3] Cheese (3 available) - 87g each (market: 50g)
  [4] Meat (3 available) - 52g each (market: 30g)
  [5] Fish (2 available) - 61g each (market: 35g)
  
Your gold: 420g
Days of food remaining: 8 days

Next resupply: 5 days (next muster)

[Select item to purchase] [Leave]
```

### **Transition at T5 Promotion**

When player is promoted to T5:

```csharp
private void OnPromotedToOfficer()
{
    // ... existing promotion logic ...
    
    // If player has issued rations, reclaim them (clean slate)
    if (_issuedFoodRations.Count > 0)
    {
        ReclaimIssuedRations();
        
        InformationManager.DisplayMessage(new InformationMessage(
            new TextObject("{=officer_rations_end}" +
                "As an officer, you are no longer issued rations. " +
                "You must purchase provisions from the Quartermaster or town markets.")
                .ToString()));
    }
    
    // Unlock QM food shop
    _qmFoodShopUnlocked = true;
    
    ModLogger.Info("Promotion", "T5 reached: Issued rations ended, QM food shop unlocked");
}
```

---

## Integration with Existing Systems

### **1. Muster Event (EnlistmentBehavior)**

Modify the existing muster event to include food ration:

```csharp
private void OnMusterDay()
{
    // NEW: Process ration exchange (reclaim old, issue new)
    ProcessMusterFoodRation();
    
    // Existing: Pay wages
    int wages = _pendingMusterPay;
    GiveGoldAction.ApplyBetweenCharacters(null, Hero.MainHero, wages);
    
    // Display combined message (ration message shown separately in ProcessMusterFoodRation)
    InformationManager.DisplayMessage(new InformationMessage(
        new TextObject("{=muster_complete}Muster complete! Wages: {GOLD}g.")
            .SetTextVariable("GOLD", wages).ToString()));
    
    // Reset muster tracking
    _pendingMusterPay = 0;
    _payMusterPending = false;
    _nextPayday = CampaignTime.DaysFromNow(12);
}
```

### **2. Daily Tick (Food Event Checks)**

Add food loss and shortage checks to the daily tick:

```csharp
private void OnDailyTick()
{
    // ... existing daily tick logic ...
    
    // NEW: Check for personal food loss events (rats, spoilage, theft)
    if (IsEnlisted && !IsOnLeave)
    {
        CheckPersonalFoodLossEvents(); // Random loss if 2+ personal food
        CheckFoodShortageEvents();      // Warn player if <3 days of food
    }
}
```

### **3. Post-Battle Event Check**

Add battle damage check after battles:

```csharp
private void OnBattleEnd()
{
    // ... existing post-battle logic ...
    
    // Check for food damage in battle
    if (HasPersonalFood(2))
    {
        if (MBRandom.RandomFloat < 0.20f) // 20% chance
        {
            TriggerBattleFoodDamageEvent();
        }
    }
}
```

### **3. Discharge/Desertion Integration**

When player leaves service (discharge, desertion, or lord death), all issued rations are reclaimed:

```csharp
private void OnServiceEnded()
{
    // Reclaim any remaining issued rations
    ReclaimIssuedRations();
    
    // ... existing discharge logic ...
    
    ModLogger.Info("Discharge", 
        "All issued rations reclaimed. Personal food retained.");
}
```

**Behavior:**
- **Issued rations:** Reclaimed (both inventory and stowage)
- **Personal food:** Player keeps (bought/foraged items)
- **No penalty:** Unlike equipment, consumed rations don't incur charges

This prevents players from deserting with stockpiled military rations.

### **4. Quartermaster Relationship**

QM reputation already exists in the mod. We use `Hero.GetRelation(quartermaster)` to determine food quality.

**Finding the Quartermaster:**

```csharp
private Hero GetQuartermaster()
{
    // Option 1: Dedicated QM NPC (if implemented)
    Hero qm = GetQuartermasterNPC();
    if (qm != null) return qm;
    
    // Option 2: Use lord as fallback
    return _enlistedLord;
}
```

### **4. Company Supply System**

This food ration system is **separate** from the Company Supply system:

| System | Scope | Tracked Value |
|--------|-------|---------------|
| **Company Supply** | Lord's entire party | 0-100% (food 40% + non-food 60%) |
| **Player Food Rations** | Player's personal inventory | Food items in `MobileParty.MainParty.ItemRoster` |

**Relationship:**
- Company Supply affects **equipment access** (< 30% blocks QM menu)
- Player Food affects **player survival** (starving = HP loss, morale loss)
- Both systems observe vanilla food, neither modifies AI behavior

---

## Event Frequency Balancing

### **Expected Player Experience (Ration Exchange System):**

**First 12 Days (Company Supply: 70%+):**
- Start: 0 food
- Muster 1 (Day 12): Old ration reclaimed (none), new ration issued (+1 grain)
- Consumed: ~0.6 food
- Remaining: ~0.4 food (8 days)
- **Player action:** None needed

**Days 13-24 (Supplies still 70%+):**
- Muster 2 (Day 24): Old ration reclaimed (-1 grain = 0.4 food lost), new ration issued (+1 grain)
- Consumed: ~0.6 food
- Remaining: ~0.4 food (8 days)
- **Player action:** None needed
- **Result:** Stable cycle, player always has 8 days of food after muster

**Days 25-36 (Supplies drop to 55% - campaign stress):**
- Muster 3 (Day 36): 80% chance of ration (20% chance none)
- **If ration issued:** Remaining ~0.4 food (8 days) - manageable
- **If NO ration issued (20% chance):** Remaining ~0.4 food (old ration kept) - still have 8 days
- **Player action:** Buy 1 personal food as backup (~30g) - smart players prepare

**Days 37-48 (Supplies at 55%, player has 1 personal + issued):**
- Muster 4 (Day 48): Old issued ration reclaimed, 80% chance new ration
- **If ration issued:** Have 1 issued + 0.7 personal = 1.7 food (~34 days)
- **If NO ration (20%):** Have 0 issued + 0.7 personal = 0.7 food (~14 days)
- **Player action:** Monitor supplies, buy more food if shortage streak continues

**Days 49-60 (Supplies crash to 40% - siege/war):**
- Muster 5 (Day 60): 50% chance no ration (coin flip!)
- **If lucky (50%):** Get ration, ~8 days of food
- **If unlucky (50%):** No ration, must buy food immediately (~40g)
- **Player action:** MUST actively manage food, forage duties become critical

**Expected Outcome:**
- **No accumulation** - Exchange system prevents hoarding issued rations
- **Regular scarcity** - Player experiences food pressure during campaigns
- **Personal food attrition** - Building reserves is smart but has risk
- **Player agency** - Must actively buy/forage during low supply periods
- **QM rep matters** - Better food quality feels rewarding
- **Supply pressure is real** - Food scarcity mirrors equipment scarcity
- **Cost of war** - Campaigns cost 50-150g in food over 60 days (rations + reserves + loss events)

### **Personal Food Reserve Management:**

**Smart Player (Buys Backup Food):**
- Day 1: Buy 2 grain (personal reserves) = 60g spent
- Day 12 (Muster): Get 1 butter (issued), have 3 food total
- Day 18: Rats in Camp event → Lose 1 grain (personal)
- Day 24 (Muster): Exchange 1 butter for 1 meat, have 1 grain + 1 meat = 2 food
- Day 30: Battle Damage event → Lose 1 grain (personal)
- Day 36 (Muster): Exchange 1 meat for 1 butter (if available), buy 2 more grain = 60g
- **Result:** Spent 120g over 36 days, experienced 2 loss events, never starved

**Risky Player (No Backup Food):**
- Day 1: 0 food
- Day 12 (Muster): Get 1 butter (issued), have 1 food
- Day 24 (Muster): Company supplies 45% → Coin flip... NO RATION!
- Day 25: Emergency! Buy 1 grain from town = 30g
- Day 30: Running low again
- Day 36 (Muster): Coin flip... GET RATION! (lucky)
- **Result:** Spent 30g, experienced food anxiety, got lucky

**Cost Comparison:**
- **Smart player:** ~120g per 36 days, stable food supply
- **Risky player:** ~30-90g per 36 days, stressful experience
- **Personal food loss:** 1-2 items per month (replace for ~30-60g)

---

## Implementation Checklist

### **Phase 1: Basic Ration System**

- [ ] Add `_issuedFoodRations` list to `EnlistmentBehavior` (save/load)
- [ ] Add `ProcessMusterFoodRation()` method to `EnlistmentBehavior`
- [ ] Implement `ReclaimIssuedRations()` method (scans inventory AND stowage)
- [ ] Implement `GetFoodItemForReputation(int qmRep)` logic
- [ ] Add food ration to existing muster event
- [ ] Test: Player receives 1 tracked food every 12 days at muster
- [ ] Test: Issued ration is reclaimed at next muster (cannot hide in stowage)

### **Phase 2: QM Reputation Integration**

- [ ] Implement `GetQuartermaster()` method
- [ ] Add QM rep checks to food quality determination
- [ ] Add flavor text for different food qualities
- [ ] Test: Food quality changes based on QM rep

### **Phase 3: Ration Exchange & Anti-Abuse System**

- [ ] Implement `DetermineRationAvailability()` based on company supply
- [ ] Add "No rations available" message when supplies low
- [ ] Integrate with company supply system (0-100%)
- [ ] Implement stowage scanning in `ReclaimIssuedRations()`:
  - [ ] Add `GetItemCountInStowage(ItemObject item)` method
  - [ ] Add `RemoveItemFromStowage(ItemObject item, int amount)` method
  - [ ] Ensure reclamation checks ALL storage locations
- [ ] Add discharge/desertion ration reclamation
- [ ] Test: Old ration is reclaimed from inventory
- [ ] Test: Old ration is reclaimed from stowage (cannot hide)
- [ ] Test: Personal food (bought) is NOT reclaimed
- [ ] Test: No ration issued when supplies <50% (random chance)
- [ ] Test: Rations reclaimed on discharge

### **Phase 4: Personal Food Loss Events**

- [ ] Implement `CheckPersonalFoodLossEvents()` daily check
- [ ] Implement `CountIssuedRations()` to exclude issued food from event targets
- [ ] Create "Spoilage" event (simple loss, summer/heat)
- [ ] Create "Rats in Camp" event (skill check option)
- [ ] Create "Theft by Comrade" event (choice-driven, lance rep impact)
- [ ] Create "Battle Damage" event (post-battle trigger)
- [ ] Create "Refugee Request" event (moral choice)
- [ ] Create "Checkpoint Shakedown" event (bribe/roguery)
- [ ] Test: Events only affect personal food (never issued rations)
- [ ] Test: Events fire when player has 2+ personal food items
- [ ] Test: Frequency increases with larger personal food stockpiles

### **Phase 5: Shortage Events**

- [ ] Implement `CheckFoodShortageEvents()` daily check
- [ ] Add low food warning (< 3 days)
- [ ] Create "Starving" event with multiple options
- [ ] Test: Events fire when player runs out of food

### **Phase 6: Balancing**

- [ ] Adjust ration availability percentages (70%/50%/30% thresholds)
- [ ] Balance QM rep thresholds for food quality
- [ ] Balance personal food loss event frequencies (5-15% per day)
- [ ] Test personal food loss with different reserve sizes (2, 4, 6 items)
- [ ] Ensure personal food loss doesn't feel punishing (should lose 1-2 items per month)
- [ ] Test long-term scarcity experience (30-60 day campaigns)
- [ ] Ensure player has fair warning before starvation
- [ ] Balance gold cost of backup food purchases (should be meaningful but not crippling)
- [ ] Test that issued rations are NEVER targeted by loss events

### **Phase 7: T5+ Officer Provisioning System**

- [ ] Add `_qmFoodShop` list to track inventory (save/load)
- [ ] Add `_qmFoodShopUnlocked` flag (set to true at T5 promotion)
- [ ] Modify `ProcessMusterForOfficer()` to check tier:
  - [ ] T1-T4: Process issued rations (existing system)
  - [ ] T5+: Refresh QM food shop inventory
- [ ] Implement `RefreshQuartermasterFoodInventory()`:
  - [ ] Clear old inventory
  - [ ] Add items based on company supply (70%/50%/30%/below thresholds)
  - [ ] Different quantities and varieties per supply level
- [ ] Implement `CalculateOfficerFoodPrice()`:
  - [ ] Base multiplier: 2.0x (200%)
  - [ ] QM rep discount: Down to 1.5x (150%) at max rep
  - [ ] Never cheaper than 150% of town prices
- [ ] Implement `BuyFoodFromQuartermaster()`:
  - [ ] Check stock availability
  - [ ] Calculate price with QM rep discount
  - [ ] Process gold transaction
  - [ ] Add food to inventory (NOT tracked as issued)
  - [ ] Reduce shop stock
- [ ] Implement `OnPromotedToOfficer()`:
  - [ ] Reclaim any remaining issued rations
  - [ ] Display transition message
  - [ ] Unlock QM food shop
- [ ] Add QM Food Shop UI:
  - [ ] Add "Buy Provisions" option to QM menu (T5+ only)
  - [ ] Display available items with prices
  - [ ] Show company supply level
  - [ ] Show QM rep and discount percentage
  - [ ] Show days until next resupply (muster)
- [ ] Test: T5 promotion ends issued rations
- [ ] Test: QM shop refreshes every muster with supply-dependent inventory
- [ ] Test: Prices are 150-200% of town markets
- [ ] Test: Low supply (< 30%) = only 2 grain available
- [ ] Test: High supply (70%+) = full variety and stock
- [ ] Test: Purchased food is personal (NOT reclaimed, loss events can target)

---

## Technical Notes

### **Food Item Access:**

```csharp
// Get food item by string ID
ItemObject grain = MBObjectManager.Instance.GetObject<ItemObject>("grain");
ItemObject meat = MBObjectManager.Instance.GetObject<ItemObject>("meat");
ItemObject butter = MBObjectManager.Instance.GetObject<ItemObject>("butter");
ItemObject cheese = MBObjectManager.Instance.GetObject<ItemObject>("cheese");

// Add to player inventory
MobileParty.MainParty.ItemRoster.AddToCounts(grain, 1);

// Remove from player inventory (only for issued ration)
MobileParty.MainParty.ItemRoster.AddToCounts(grain, -1);

// Check days of food
int daysOfFood = MobileParty.MainParty.GetNumDaysForFoodToLast();

// Check if starving
bool isStarving = MobileParty.MainParty.Party.IsStarving;
```

### **Tracking Issued vs. Personal Food:**

**IMPORTANT: Prevent Abuse via Baggage/Stowage**

Like military equipment, issued rations must be tracked to prevent players from hiding them in baggage train stowage or other storage. We use the same tracking system as QM-purchased equipment.

**Tracking System:**

```csharp
// Save/load this list
private List<ItemRosterElement> _issuedFoodRations = new List<ItemRosterElement>();

private void ProcessMusterFoodRation()
{
    Hero hero = Hero.MainHero;
    MobileParty party = MobileParty.MainParty;
    
    // Step 1: Reclaim ALL issued rations (main inventory AND stowage)
    ReclaimIssuedRations();
    
    // Step 2: Check if rations are available
    int companySupply = GetCompanySupply();
    bool rationAvailable = DetermineRationAvailability(companySupply);
    
    if (!rationAvailable)
    {
        InformationManager.DisplayMessage(new InformationMessage(
            new TextObject("{=no_rations}Supplies are too low. No rations available this muster.").ToString(),
            Colors.Red));
        return;
    }
    
    // Step 3: Issue new ration and TRACK it
    Hero quartermaster = GetQuartermaster();
    int qmRep = quartermaster != null ? hero.GetRelation(quartermaster) : 0;
    
    ItemObject newRation = GetFoodItemForReputation(qmRep);
    EquipmentElement rationElement = new EquipmentElement(newRation);
    
    // Add to inventory
    party.ItemRoster.AddToCounts(newRation, 1);
    
    // TRACK as issued ration (for reclamation next muster)
    _issuedFoodRations.Add(new ItemRosterElement(rationElement, 1));
    
    ModLogger.Info("Muster", $"Food ration issued and tracked: {newRation.Name}");
}

private void ReclaimIssuedRations()
{
    if (_issuedFoodRations == null || _issuedFoodRations.Count == 0)
        return;
    
    MobileParty party = MobileParty.MainParty;
    int totalReclaimed = 0;
    
    foreach (var rationElement in _issuedFoodRations)
    {
        ItemObject item = rationElement.EquipmentElement.Item;
        int amountToReclaim = rationElement.Amount;
        
        // Check main inventory
        int inInventory = party.ItemRoster.GetItemNumber(item);
        
        // Check stowage (if implemented)
        int inStowage = GetItemCountInStowage(item);
        
        int totalAvailable = inInventory + inStowage;
        int actuallyReclaimed = Math.Min(amountToReclaim, totalAvailable);
        
        if (actuallyReclaimed > 0)
        {
            // Reclaim from main inventory first
            int fromInventory = Math.Min(actuallyReclaimed, inInventory);
            if (fromInventory > 0)
            {
                party.ItemRoster.AddToCounts(item, -fromInventory);
                totalReclaimed += fromInventory;
                actuallyReclaimed -= fromInventory;
            }
            
            // Then reclaim from stowage if needed
            if (actuallyReclaimed > 0 && inStowage > 0)
            {
                RemoveItemFromStowage(item, actuallyReclaimed);
                totalReclaimed += actuallyReclaimed;
            }
            
            ModLogger.Info("Muster", 
                $"Reclaimed issued ration: {item.Name} x{totalReclaimed} " +
                $"(from inventory: {fromInventory}, from stowage: {actuallyReclaimed})");
        }
        else
        {
            // Ration was consumed or sold - that's fine
            ModLogger.Info("Muster", 
                $"Issued ration consumed/sold before muster: {item.Name}");
        }
    }
    
    // Clear tracking
    _issuedFoodRations.Clear();
}
```

**How This Prevents Abuse:**

1. **Issue:** Player receives 1 grain (tracked)
2. **Player tries to hide it:** Moves grain to stowage
3. **Muster:** System checks BOTH inventory AND stowage, reclaims grain
4. **Result:** Can't accumulate by hiding in storage

**Personal Food is Protected:**

```csharp
Player inventory:
  - 2 grain (bought from town - NOT tracked)
  - 1 butter (issued at muster - tracked in _issuedFoodRations)

At next muster:
  - System reclaims: 1 butter (tracked)
  - System leaves: 2 grain (not tracked)
  - Player keeps their personal food reserves
```

**Key Difference from Simple ID Tracking:**

- **Old approach:** Track item ID string, remove 1 of that item type (couldn't distinguish issued vs personal)
- **New approach:** Track specific ItemRosterElement instances, reclaim exact quantities, scan all storage locations

**This matches the QM equipment tracking system:** Issued items are tracked throughout service, reclaimed when needed, and can't be hidden in storage.

### **Stowage System Integration:**

If your mod has a baggage train/stowage system, the reclamation MUST scan all storage locations:

```csharp
private int GetItemCountInStowage(ItemObject item)
{
    // If stowage system exists
    if (StowageManager != null)
    {
        return StowageManager.GetStoredItemCount(item);
    }
    return 0; // No stowage system
}

private void RemoveItemFromStowage(ItemObject item, int amount)
{
    // If stowage system exists
    if (StowageManager != null)
    {
        StowageManager.RemoveItem(item, amount);
        ModLogger.Info("Muster", 
            $"Reclaimed {amount}x {item.Name} from stowage (cannot hide issued rations)");
    }
}
```

**Storage Locations to Check:**
- Main party inventory (always)
- Baggage train stowage (if implemented)
- Companion inventories (if allowed to transfer)
- Settlement stashes (if player can store there)

**Goal:** Issued rations cannot be hidden anywhere. They are reclaimed at next muster from ALL locations.

---

### **Ensuring Loss Events Only Target Personal Food:**

**Critical:** Personal food loss events must NEVER target the currently issued ration.

```csharp
private ItemObject SelectRandomPersonalFood()
{
    MobileParty party = MobileParty.MainParty;
    List<ItemObject> personalFoodItems = new List<ItemObject>();
    
    // Build list of food items in inventory
    foreach (var element in party.ItemRoster)
    {
        if (!element.EquipmentElement.Item.IsFood) continue;
        
        // Skip if this is a currently issued ration
        bool isIssued = _issuedFoodRations.Any(r => 
            r.EquipmentElement.Item.StringId == element.EquipmentElement.Item.StringId);
        
        if (isIssued)
        {
            // This is the issued ration, skip it
            // But only skip the QUANTITY that's issued (1 item)
            int personalQuantity = element.Amount - 1;
            if (personalQuantity > 0)
            {
                // Player has extras of this food type (e.g., bought 2 grain, 1 is issued)
                personalFoodItems.Add(element.EquipmentElement.Item);
            }
        }
        else
        {
            // This is personal food, can target it
            personalFoodItems.Add(element.EquipmentElement.Item);
        }
    }
    
    // Select random personal food item
    if (personalFoodItems.Count > 0)
    {
        return personalFoodItems[MBRandom.RandomInt(personalFoodItems.Count)];
    }
    
    return null; // No personal food to target
}

private void TriggerFoodLossEvent()
{
    ItemObject targetFood = SelectRandomPersonalFood();
    
    if (targetFood == null)
    {
        // No personal food to lose (only have issued ration)
        ModLogger.Info("FoodLoss", "No personal food to target, event cancelled");
        return;
    }
    
    // Remove 1 of the personal food
    MobileParty.MainParty.ItemRoster.AddToCounts(targetFood, -1);
    
    // Display event
    InformationManager.DisplayMessage(new InformationMessage(
        new TextObject("{=food_loss}You lost some food: {ITEM}")
            .SetTextVariable("ITEM", targetFood.Name).ToString(),
        Colors.Red));
    
    ModLogger.Info("FoodLoss", $"Personal food lost to event: {targetFood.Name}");
}
```

**Key Logic:**
1. List all food items in inventory
2. Check if item is currently issued (in `_issuedFoodRations`)
3. If issued, subtract 1 from available quantity (protect the issued item)
4. Only target the remaining personal food
5. If no personal food available, cancel the event

**Example:**
- Player has: 3 grain (2 personal + 1 issued)
- Event fires: Targets 1 of the 2 personal grain
- Result: 2 grain (1 personal + 1 issued) remain
- Issued ration is protected!

### **Anti-Abuse Example Scenario:**

**Attempt to Exploit:**
1. **Muster 1:** Receive 1 grain (issued, tracked)
2. **Player:** Moves grain to stowage to "hide" it
3. **Muster 2:** Receive 1 butter (issued, tracked)
4. **Player thinks:** "I have 1 grain in stowage + 1 butter in inventory = 2 food!"
5. **Muster 3 Reality:**
   - System reclaims 1 butter from inventory ✓
   - System reclaims 1 grain from stowage ✓
   - Player has: 0 issued rations
   - **Exploit failed!**

**Legitimate Personal Food:**
1. **Player buys:** 2 grain from town (personal, not tracked)
2. **Muster 1:** Receive 1 butter (issued, tracked)
3. **Player moves:** Butter to stowage, keeps 2 grain in inventory
4. **Muster 2:**
   - System reclaims 1 butter from stowage ✓
   - System leaves 2 grain alone ✓ (not tracked = personal)
   - Player receives new ration: 1 meat (issued, tracked)
   - Player has: 2 grain (personal) + 1 meat (issued) = 3 food total
5. **Result:** Player can build personal reserves by buying food, but cannot accumulate issued rations.

**This is intentional:** Buying personal food creates a legitimate buffer. Only military-issued rations are reclaimed.

### **Food Item IDs (Native Game):**

Common food items available in Bannerlord:
- `grain` - Basic grain (10g, 10 weight)
- `meat` - Salted meat (30g, 10 weight)
- `butter` - Butter (40g, varies)
- `cheese` - Cheese (50g, varies)
- `fish` - Fish (similar to meat)
- `olives` - Olives (lower value)
- `dates` - Dates (lower value)

**All provide same nutrition:** 1 food item = 20 days for 1 person

---

## Why This System Works

### **Solves the Accumulation Problem:**
✅ **No hoarding** - Ration exchange prevents endless stockpiling  
✅ **Natural scarcity** - Player can't coast on accumulated rations  
✅ **Forced engagement** - Must actively manage food during campaigns  
✅ **Abuse-proof** - Issued rations tracked and reclaimed from ALL storage locations  

### **Creates Meaningful Gameplay:**
✅ **Company supply matters** - Directly impacts player's daily life  
✅ **QM reputation matters** - Better food quality when available  
✅ **Strategic decisions** - When to buy backup food vs. save gold  
✅ **Tension during sieges** - Low supplies = no rations = must spend gold  
✅ **Personal food risk** - Building reserves is smart but not foolproof (random loss)  
✅ **Interesting events** - Rats, theft, refugees create narrative moments  

### **Realistic Military Logistics:**
✅ **Ration exchange** - Mimics real military supply distribution  
✅ **Scarcity during war** - Campaigns strain logistics  
✅ **Personal vs. issued** - Can supplement with bought/foraged food  
✅ **No exploits** - Tracking system prevents hiding rations in stowage  
✅ **Discharge reclamation** - Issued rations returned when leaving service  

### **Career Progression (T1-T4 → T5+):**
✅ **Natural transition** - Enlisted/NCO gets issued rations, Officers buy provisions  
✅ **Increased responsibility** - Officers manage their own logistics  
✅ **Prepares for leadership** - T5+ system scales to leading troops (future)  
✅ **Reflects pay increase** - Officers earn more, can afford premium QM prices  
✅ **Supply matters more** - Low supply means limited QM stock, must use towns  
✅ **QM rep still relevant** - Better prices with high reputation (150% vs 200%)  

### **Player Experience:**
✅ **T1-T4: Stable in peace** - High supplies = reliable rations  
✅ **T1-T4: Pressure in war** - Low supplies = food becomes a cost (50-100g per campaign)  
✅ **T5+: Convenience premium** - QM shop available in field, but 150-200% of town prices  
✅ **T5+: Strategic choices** - Use QM for convenience or towns for value  
✅ **Agency** - Can always buy food from towns as backup  
✅ **Never surprise starvation** - Issued ration lasts 20 days, only reclaimed at muster  

### **Balancing Economics:**

#### **T1-T4 (Enlisted/NCO) - Issued Rations:**
- **Peacetime cost:** 0-30g per month (rations reliable, occasional loss events)
- **Active campaign cost:** ~60-90g per month (backup food purchases + loss events)
- **Siege/crisis cost:** ~100-150g per month (must buy food regularly + loss events)
- **Personal food loss:** ~1-2 items per month (~30-60g to replace)
- **Reasonable expense** - Food costs are meaningful but not crippling (2-5% of monthly wages)

#### **T5+ (Officer) - QM Provisions:**
- **Peacetime cost (QM only):** ~68g per month (1.5 food × 45g avg from QM)
- **Peacetime cost (smart - use towns):** ~45g per month (1.5 food × 30g avg from towns)
- **Active campaign cost (QM):** ~68-90g per month (QM provisions + loss events)
- **Siege/crisis cost (QM):** ~100-120g per month (limited QM stock + town purchases)
- **Convenience premium:** ~23g per month to buy from QM vs towns
- **Personal food loss:** ~1-2 items per month (~30-60g to replace)

#### **T5+ With Troops (Future Feature):**
- **5 troops monthly cost (QM):** ~338g per month (7.5 food × 45g)
- **5 troops monthly cost (towns):** ~225g per month (7.5 food × 30g)
- **Major expense** - Leading troops significantly increases logistics costs

### **Economic Pressure Examples:**

**Monthly Wages (for comparison):**
- T1 Enlisted: ~300-400g per month
- T2 Enlisted: ~500-600g per month
- T3 NCO: ~800-1000g per month
- **T5 Officer: ~1200-1500g per month**
- **T6+ Senior Officer: ~2000-3000g per month**

**Food as % of Wages:**

**T1-T4 (Issued Rations):**
- Peacetime: 0-5% (negligible)
- Active campaign: 10-15% (noticeable)
- Siege/crisis: 15-20% (significant pressure)

**T5+ Solo Officer (QM Provisions):**
- Peacetime (using QM): 4-6% (minor convenience cost)
- Peacetime (using towns): 3-4% (minimal)
- Active campaign: 7-10% (noticeable)
- Siege/crisis: 8-10% (moderate)

**T5+ With Troops (5 soldiers):**
- Peacetime (using QM): 22-28% (major expense!)
- Peacetime (using towns): 15-19% (significant but manageable)
- Active campaign: 25-35% (heavy logistics burden)

**This creates realistic military economics:**
- **Enlisted:** Food is free unless supplies fail
- **Officers (solo):** Food is a small cost, convenience premium optional
- **Officers (with troops):** Food is a major logistics burden, must actively manage

---

**Status**: Ready for review and implementation planning.

**Next Steps:**

**T1-T4 Issued Rations System:**
1. Review ration availability percentages (currently: 100%/80%/50%/0%)
2. Review QM rep thresholds for food quality
3. Review personal food loss event frequencies (5-15% per day)
4. Implement ration tracking (`_issuedFoodRations` list in save/load)
5. Implement personal food targeting logic (must exclude issued rations)
6. Integrate with company supply system
7. Test scarcity experience during sieges (supplies <50%)
8. Test personal food loss events with various reserve sizes (2, 4, 6 items)
9. Ensure issued rations are NEVER lost to random events

**T5+ Officer Provisioning System:**
10. Review QM food pricing (200% base, 150% max discount)
11. Review supply-dependent inventory quantities (2-6 grain, etc.)
12. Implement T5 promotion transition (reclaim issued rations, unlock shop)
13. Implement QM food shop refresh every muster
14. Implement supply-dependent inventory generation
15. Create QM food shop UI (purchase interface)
16. Test T5 transition ends issued rations cleanly
17. Test QM shop prices are always 150-200% of town prices
18. Test low supply (<30%) limits QM stock to 2 grain only
19. Balance officer food costs vs wages (should be 4-10% of monthly pay)
20. Test that QM-purchased food is treated as personal (loss events can target)

