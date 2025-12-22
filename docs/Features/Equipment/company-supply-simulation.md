# Company Supply Simulation System

**Summary:** The company supply system tracks logistical health of the military unit including equipment maintenance materials, ammunition/consumables, and fodder/animal care supplies. Supply levels are calculated from inventory, consumed through time and combat, and replenished through quartermaster purchases, creating realistic military logistics gameplay.

**Status:** ✅ Current  
**Last Updated:** 2025-12-22  
**Related Docs:** [Quartermaster System](quartermaster-system.md), [Provisions & Rations](provisions-rations-system.md)

---

## Overview

**Company Supply** represents the overall logistical health of your military unit, tracking:
- Equipment maintenance materials (repair supplies, oil, leather, whetstone)
- Ammunition and consumables (arrows, bolts, bandages, medicine)
- Fodder and animal care supplies (for horses and pack animals)
- Camp supplies (tents, rope, tools, firewood)
- **Note:** Food is tracked separately by vanilla Bannerlord's native food system

**Tracked Value:** `CompanyNeedsState.Supplies` (0-100%) — backed by `CompanySupplyManager`

**Impact:**
- < 30% = Cannot access equipment changes (quartermaster menu blocked)
- < 50% = Warning messages, morale penalties
- < 20% = Critical - Scrutiny increases, troops desert

---

## CRITICAL: Vanilla Food System Integration

### **DO NOT Modify Native Food:**

Bannerlord's AI relies heavily on the native food system:
- `MobileParty.Food` (tracked in `ItemRoster.TotalFood`)
- `MobileParty.FoodChange` (daily consumption rate)
- `MobileParty.GetNumDaysForFoodToLast()` (AI decision making)
- `PartyBase.IsStarving` (when `RemainingFoodPercentage < 0`)

**Key Native Behaviors:**
1. **FoodConsumptionBehavior**: Consumes food daily based on party size + perks
2. **PartiesBuyFoodCampaignBehavior**: AI auto-buys food when entering settlements
3. **AI Decision Making**: Lords check food supply before raids/sieges
4. **Army Food Sharing**: Armies automatically share food between parties

**Our Approach:**
- **Observe** lord's food levels to influence our Supply %
- **Simulate** non-food logistics (ammo, repairs, camp supplies)
- **Gate** equipment access based on our Supply metric
- **Never** modify `MobileParty.Food`, `FoodChange`, or food items directly

### **CRITICAL: Company Scope (Not Army Scope)**

Company Supply is intentionally scoped to the **Company you are enlisted in** (i.e., the **enlisted lord’s party**), not to the full army.

- If the enlisted lord is attached to an army, **do not switch** the observation target to the army leader or any aggregate/army-wide value.
- We may still *observe* vanilla behavior that happens because armies share food (that will naturally change the enlisted lord party’s food days), but we do not treat “being in an army” as a different supply model.

---

## Supply Consumption (Daily)

### **Hybrid System: Observation + Simulation**

Our supply system has two components:

#### **1. Food Component (Observed - 40% of total supply)**

We **observe** the lord's party food situation and map it to our supply percentage:

```csharp
public int CalculateFoodSupplyContribution()
{
    MobileParty lordParty = GetLordParty();
    if (lordParty == null) return 40; // Full food component if not attached
    
    int daysOfFood = lordParty.GetNumDaysForFoodToLast();
    
    // Map food days to supply contribution (0-40%)
    if (daysOfFood >= 10) return 40;      // 10+ days = full
    if (daysOfFood >= 7) return 35;       // 7-9 days = good
    if (daysOfFood >= 5) return 30;       // 5-6 days = adequate
    if (daysOfFood >= 3) return 20;       // 3-4 days = low
    if (daysOfFood >= 1) return 10;       // 1-2 days = critical
    if (lordParty.Party.IsStarving) return 0; // Starving = no food
    
    return 5; // <1 day but not starving yet
}
```

**This observes vanilla without modifying it.**

#### **2. Non-Food Component (Simulated - 60% of total supply)**

We **simulate** consumption of:
- Ammunition (arrows, bolts)
- Maintenance supplies (oil, whetstone, leather)
- Medical supplies (bandages, medicine)
- Camp gear (rope, tools, tents)
- Animal supplies (horseshoes, fodder supplements)

**Base Consumption Rate:** 1.5% per day (non-food supplies)

**Company Size Modifier:**
```csharp
int companySize = GetLordPartySize(); // Lord's total party size
float sizeMultiplier = companySize / 100.0f;

// Example:
// 50 troops = 0.5x consumption (0.75% per day)
// 100 troops = 1.0x consumption (1.5% per day)
// 200 troops = 2.0x consumption (3% per day)
```

**Activity Multiplier (Non-Food):**

| Activity | Multiplier | Daily Non-Food Consumption (100 troops) |
|----------|------------|--------------------------------|
| **Resting in settlement** | 0.3x | 0.45% per day |
| **Traveling (normal)** | 1.0x | 1.5% per day |
| **Patrolling** | 1.2x | 1.8% per day |
| **Raiding** | 1.5x | 2.25% per day |
| **Besieging** | 2.5x | 3.75% per day *(siege equipment use)* |
| **Being besieged** | 1.8x | 2.7% per day |
| **Battle** | Special | See "Combat Losses" section |

**Terrain Multiplier (Non-Food):**

| Terrain | Multiplier | Reason |
|---------|------------|--------|
| **Plains/Forest** | 1.0x | Standard wear |
| **Desert** | 1.2x | Sand damages equipment |
| **Mountains** | 1.3x | Rough terrain, rockslides |
| **Snow** | 1.4x | Fuel for fires, equipment brittleness |
| **Sea (naval)** | 1.1x | Salt corrosion |

**Total Supply Formula:**
```
Total Supply % = Food Component (0-40%) + Non-Food Component (0-60%)

Non-Food Component reduces by:
  (Base Rate 1.5% × Size Mult × Activity Mult × Terrain Mult) per day
  
Food Component updates based on lord's food days remaining
```

---

## Supply Losses (Events)

### **Combat Losses:**

**After Battle:**
```csharp
public int CalculateBattleSupplyLoss(int troopsLost, bool playerVictory)
{
    // Base loss from casualties
    int casualtyLoss = troopsLost / 10; // 10 casualties = -1% supply
    
    // Defeat penalty (abandoned supplies)
    int defeatPenalty = playerVictory ? 0 : 5;
    
    // Siege equipment usage
    int siegeLoss = IsSiegeBattle() ? 3 : 0;
    
    return casualtyLoss + defeatPenalty + siegeLoss;
}
```

**Examples:**
- Victory with 10 casualties: -1% supply
- Victory with 30 casualties: -3% supply
- Defeat with 20 casualties: -7% supply (2% + 5% abandoned)
- Siege assault victory: -8% supply (5% casualties + 3% siege equipment)

### **Special Events:**

| Event | Supply Loss | Description |
|-------|-------------|-------------|
| **Supply wagon raided** | -15% | Enemy raiders hit your supply train |
| **Spoilage (summer)** | -5% | Heat spoils/ruins stored non-food supplies (bandages, medicines, oils, etc.) |
| **Flooded supplies** | -10% | River crossing gone wrong |
| **Fire in camp** | -8% | Supplies burned |
| **Deserters steal supplies** | -3% per deserter | Internal theft |
| **Lord requisitions supplies** | -10% | Lord takes supplies for main army |

---

## Supply Gains (Replenishment)

### **1. Automatic Resupply (Company Context - Non-Food Only)**

When the player is with the enlisted lord and the Company is in a settlement:

```csharp
public void DailyNonFoodResupply()
{
    // Important: resupply is computed for the Company (enlisted lord’s party),
    // regardless of whether the lord is currently attached to an army.
    if (IsWithEnlistedLordPartyInSettlement())
    {
        // Settlement resupply: +3% non-food supplies per day in town/castle
        int nonFoodResupply = 3;
        
        // Wealthy settlements give more (better workshops/smiths)
        if (CurrentSettlement.Prosperity > 5000)
            nonFoodResupply += 1;
        
        // Modifier based on settlement relation
        if (CurrentSettlement.OwnerClan == PlayerLord.Clan)
            nonFoodResupply += 1; // Friendly territory has better access
        
        // Only replenish non-food component (max 60%)
        int currentNonFood = GetNonFoodSupplyComponent();
        if (currentNonFood < 60)
        {
            AddNonFoodSupplies(nonFoodResupply);
        }
    }
    
    // Food component updates separately by observing the enlisted lord party food
    UpdateFoodComponentFromLordParty();
}
```

**Settlement Resupply (Non-Food):**
- In friendly town/castle: +3% per day
- In wealthy settlement: +4% per day  
- In owned settlement: +5% per day
- **Food:** Automatically updates based on lord's party food status (vanilla system handles buying)

### **2. Quartermaster Duties (Player Actions)**

When player performs supply-related duties:

| Duty/Activity | Non-Food Gain | Food Impact | Notes |
|---------------|---------------|-------------|-------|
| **Foraging Duty** | +2 to +5% | Indirect | Helps lord's party forage (vanilla handles food) |
| **Inventory Management** | +2% | None | Organize existing supplies, reduce waste |
| **Equipment Maintenance** | +3% | None | Repair/salvage gear, reclaim materials |
| **Negotiate with Merchants** | +5 to +10% | Indirect | Buy supplies (costs gold, helps lord buy food) |
| **Hunt Wildlife** | +1% | Indirect | Supplement food (adds food items to lord's party) |

**Note:** Food-related duties (foraging, hunting) add food items to the lord's party's inventory via vanilla systems. This indirectly improves our Food Component by increasing the lord's days of food remaining.

### **3. Supply Wagon Events**

**Supply Caravan Arrival:**
```
Every 7-14 days (random), if lord is with army:
  Supply wagon arrives: +20% supply
  
  BUT: 10% chance wagon is raided en route
    If raided: +0% supply
```

### **4. Post-Battle Looting (Non-Food Supplies)**

```csharp
public int CalculateNonFoodSupplyLoot(int enemyTroopsKilled, string enemyFaction)
{
    // Base loot: arrows, damaged gear, materials
    int baseLoot = enemyTroopsKilled / 25; // 25 kills = +1% non-food supply
    
    // Faction modifier (equipment quality affects salvage)
    float factionMultiplier = enemyFaction switch
    {
        "empire" => 1.3f,     // Professional army, good equipment
        "vlandia" => 1.2f,    // Knights, quality gear
        "sturgia" => 1.0f,    // Standard equipment
        "battania" => 0.9f,   // Woodland gear, less metal
        "khuzait" => 0.7f,    // Light cavalry, minimal armor
        "aserai" => 1.0f,     // Desert troops, standard gear
        "looters" => 0.5f,    // Peasants, junk
        "bandits" => 0.6f,    // Poorly equipped
        _ => 1.0f
    };
    
    return (int)(baseLoot * factionMultiplier);
}
```

**Examples:**
- Defeat 100 Empire troops: +5% non-food supply (4% base × 1.3)
- Defeat 75 Khuzait troops: +2% non-food supply (3% base × 0.7)
- Defeat bandit hideout (50 bandits): +1% non-food supply (2% base × 0.6)

**Food Looting:** Vanilla system handles food items automatically (loot screen)

### **5. Player Purchases**

```
Visit town merchant:
  [Buy Supplies] → Opens purchase menu
  
  Supply Packages:
    Small Pack (+ 5% supply): 150g
    Medium Pack (+10% supply): 280g
    Large Pack (+20% supply): 500g
    
  Price varies by:
    - Town prosperity (higher = cheaper)
    - Player Trade skill (higher = cheaper)
    - War/peace (war = 50% markup)
```

---

## Supply Management Strategies

### **Peacetime (Easy):**
```
Daily consumption: -2%
Settlement resupply: +5%
Net: +3% per day (stable, plenty of supply)

Player rarely needs to worry about supply.
```

### **Active Campaign (Moderate):**
```
Daily consumption: -2.4% (patrolling)
Settlement resupply: +5% (every 3-4 days in town)
Battle losses: -2% per battle
Net: Slowly declining, need occasional foraging

Player must do foraging duties every 10-15 days.
```

### **Siege/Long Campaign (Hard):**
```
Daily consumption: -4% (siege)
No settlement resupply (in field)
Battle losses: -8% (siege assault)
Net: Rapidly declining

Player must:
  - Do foraging duties frequently (+8%)
  - Buy supplies from nearby towns (+20% for 500g)
  - Win battles to loot enemy supplies (+3-6%)
  - Risk starvation if siege lasts >20 days
```

---

## Supply Crisis Events

> **Canonical Event Definitions**: See `docs/StoryBlocks/content-index.md` → "Food & Supply Events" section for event IDs and skill checks.

| Threshold | Event ID | Skill Check |
|-----------|----------|-------------|
| 50% | `evt_supply_warning` | — |
| 30% | `evt_supply_critical_gate` | — (Equipment blocked) |
| 20% | `evt_supply_critical` | Scouting (forage) |
| 10% | `evt_supply_catastrophic` | — (Automatic) |

### **Low Supply Warnings:**

**50% Supply:**
```
QM: "Supplies are running low. We should restock soon."
Effect: Yellow warning on main menu
```

**30% Supply (CRITICAL):**
```
QM: "We're nearly out of supplies! Equipment changes blocked."
Effect:
  - Cannot access Master at Arms (equipment menu blocked)
  - -5 Morale per day
  - +1 Scrutiny per day
  - Chance of desertion increases
```

**20% Supply (CRISIS):**
```
Event fires:
  "Starvation in Camp"
  
  The company is out of food. Men are hungry and desperate.
  
  Options:
    [Emergency Foraging] → High risk mission (+10% supply or disaster)
    [Requisition from Villages] → Raid nearby village (-30 relation, +15% supply)
    [Cut Rations] → -10 Morale, slower consumption (1% per day)
    [Desert] → Leave service to avoid catastrophe
```

**10% Supply (CATASTROPHIC):**
```
Automatic effects:
  - 1d6 troops desert per day
  - +5 Scrutiny per day
  - -20 Morale per day
  - Lord may dismiss you for incompetence
```

---

## Technical Implementation: Hybrid Supply System

### **Core Architecture:**

```csharp
public class CompanySupplyManager
{
    // Stored components (0-100 scale for easier math)
    private float _nonFoodSupply = 60.0f; // We manage this
    private float _lastFoodComponent = 40.0f; // Cached from last check
    
    // Public property returns total (0-100%)
    public int TotalSupply
    {
        get
        {
            float food = CalculateFoodComponent();
            float nonFood = _nonFoodSupply;
            return (int)Math.Min(100, food + nonFood);
        }
    }
    
    /// <summary>
    /// Observes lord's party food and maps to 0-40% component
    /// DOES NOT MODIFY vanilla food system
    /// </summary>
    private float CalculateFoodComponent()
    {
        MobileParty lordParty = GetPlayerLordParty();
        if (lordParty == null) return 40.0f; // Full if not enlisted
        
        int daysOfFood = lordParty.GetNumDaysForFoodToLast();
        
        // Map vanilla food days to our 0-40% food component
        if (lordParty.Party.IsStarving) return 0.0f;
        if (daysOfFood >= 10) return 40.0f;
        if (daysOfFood >= 7) return 35.0f;
        if (daysOfFood >= 5) return 30.0f;
        if (daysOfFood >= 3) return 20.0f;
        if (daysOfFood >= 1) return 10.0f;
        
        return 5.0f; // <1 day
    }
    
    /// <summary>
    /// Updates non-food supplies based on activity
    /// Called on daily tick
    /// </summary>
    public void DailyNonFoodUpdate()
    {
        float consumption = CalculateNonFoodConsumption();
        float resupply = CalculateNonFoodResupply();
        
        _nonFoodSupply = Math.Max(0, Math.Min(60, _nonFoodSupply - consumption + resupply));
        
        // Cache food component for comparison
        _lastFoodComponent = CalculateFoodComponent();
    }
    
    /// <summary>
    /// Simulates non-food supply consumption
    /// </summary>
    private float CalculateNonFoodConsumption()
    {
        MobileParty lordParty = GetPlayerLordParty();
        if (lordParty == null) return 0;
        
        // Base rate: 1.5% per day
        float baseRate = 1.5f;
        
        // Size modifier
        float sizeMultiplier = lordParty.Party.NumberOfAllMembers / 100.0f;
        
        // Activity modifier
        float activityMult = GetActivityMultiplier(lordParty);
        
        // Terrain modifier
        float terrainMult = GetTerrainMultiplier(lordParty);
        
        return baseRate * sizeMultiplier * activityMult * terrainMult;
    }
    
    /// <summary>
    /// Calculates non-food resupply (settlement only)
    /// </summary>
    private float CalculateNonFoodResupply()
    {
        MobileParty lordParty = GetPlayerLordParty();
        if (lordParty == null || lordParty.CurrentSettlement == null) return 0;
        
        Settlement settlement = lordParty.CurrentSettlement;
        if (!settlement.IsTown && !settlement.IsCastle) return 0;
        
        float resupply = 3.0f; // Base rate
        
        if (settlement.Prosperity > 5000) resupply += 1.0f;
        if (settlement.OwnerClan == lordParty.LeaderHero?.Clan) resupply += 1.0f;
        
        return resupply;
    }
    
    /// <summary>
    /// Player action: Manual non-food supply gain
    /// </summary>
    public void AddNonFoodSupplies(float amount)
    {
        _nonFoodSupply = Math.Min(60, _nonFoodSupply + amount);
    }
    
    /// <summary>
    /// Player action: Add food to lord's party (helps food component)
    /// This USES vanilla system to add items
    /// </summary>
    public void AddFoodToLordParty(ItemObject foodItem, int quantity)
    {
        MobileParty lordParty = GetPlayerLordParty();
        if (lordParty == null) return;
        
        // Use vanilla system to add food item
        lordParty.ItemRoster.AddToCounts(foodItem, quantity);
        
        // Food component will update automatically on next check
        // via CalculateFoodComponent() reading lordParty.GetNumDaysForFoodToLast()
    }
}
```

### **Key Principles:**

1. **Never Modify `MobileParty.Food` directly** - It's a calculated property
2. **Never Modify `MobileParty.FoodChange`** - AI relies on this
3. **Read-Only Access:** Use `GetNumDaysForFoodToLast()`, `Party.IsStarving`
4. **Add Items, Not Stats:** Use `lordParty.ItemRoster.AddToCounts()` for food
5. **Separate Concerns:** We own non-food (60%), observe food (40%)

---

## Integration with Existing Systems

### **1. Lance Needs System**

Supply is already tracked in `LanceNeedsState.Supplies` (0-100):

```csharp
public class LanceNeedsState
{
    public int Supplies 
    { 
        get => _companySupplyManager.TotalSupply; 
    }
    
    private CompanySupplyManager _companySupplyManager = new CompanySupplyManager();
    
    // Hook into daily tick
    public void OnDailyTick()
    {
        _companySupplyManager.DailyNonFoodUpdate();
        
        // Check thresholds and fire warnings/events
        if (Supplies < 30)
        {
            // Block quartermaster menu
            // Fire low supply events
        }
    }
    
    // Player actions call manager directly
    public void OnForagingDutyComplete(int foodItemsGained, float nonFoodGained)
    {
        // Add food items to lord's party (vanilla system)
        _companySupplyManager.AddFoodToLordParty(GetFoodItem(), foodItemsGained);
        
        // Add non-food supplies (our system)
        _companySupplyManager.AddNonFoodSupplies(nonFoodGained);
    }
}
```

### **2. Schedule Activities**

Certain scheduled activities affect supply:

| Activity | Supply Effect |
|----------|---------------|
| **Foraging Duty** | +5% on success |
| **Quartermaster Inventory** | Prevents -1% waste |
| **Work Detail** | +1% (organize supplies) |
| **Rest** | -0.5% (lower consumption) |

### **3. Quartermaster Relationship**

QM relationship affects supply management:

| QM Rep | Supply Bonus |
|--------|--------------|
| **65+** | -10% consumption (efficient management) |
| **35-65** | -5% consumption |
| **0-35** | Normal consumption |
| **<0** | +10% consumption (wasteful, sabotage) |

### **4. Duty System**

Quartermaster duty holders get supply bonuses:

```
If player has "quartermaster" duty:
  - Daily consumption: -20%
  - Foraging gains: +50%
  - Can negotiate better supply prices (-20% cost)
```

---

## Implementation Checklist

### **Core System (Implemented Dec 2025)**

- [x] Add daily supply consumption calculation (CompanySupplyManager.CalculateNonFoodConsumption)
- [x] Hook into daily tick for time-based updates (EnlistmentBehavior.OnDailyTick)
- [x] Apply activity and terrain multipliers (GetActivityMultiplier, GetTerrainMultiplier)
- [x] Implement settlement auto-resupply (+3-5% per day in towns/castles)
- [x] Add post-battle supply looting (+1% per 25 kills, capped at 6%)
- [x] Implement battle supply losses (casualties, defeat penalty, siege penalty)
- [x] Add low supply warnings via logging (50%, 30% thresholds)
- [x] Block equipment menu at <30% supply (existing CompanyNeedsState logic)
- [x] Save/load non-food supply value (SerializeCompanyNeeds)
- [x] Handle edge cases (lord captured, on leave, grace period re-enlistment)

### **Future Enhancements (Not Yet Implemented)**

- [ ] Display supply % in main menu UI
- [ ] Add foraging duty supply gains (+3-8%)
- [ ] Add supply purchase from merchants
- [ ] Add special loss events (raid, fire, spoilage)
- [ ] Track deserter supply theft
- [ ] Implement critical supply events (<20%)
- [ ] Add automatic desertion at <10%
- [ ] Connect to QM rep system (efficiency bonus)
- [ ] Connect to quartermaster duty (consumption reduction)
- [ ] Add supply caravan events (every 7-14 days)

---

## Example Supply Lifecycle

### **Week 1: Peacetime**
```
Day 1: Start at 100% supply (Food: 40%, Non-Food: 60%)
  - Lord has 12 days of food (Food Component: 40%)
  - Full camp supplies (Non-Food Component: 60%)
  
Day 2: In settlement
  - Non-Food: 60% - 0.45% consumption + 3% resupply = 62.55% (capped at 60%)
  - Food: 40% (lord still has 11 days, vanilla auto-bought food)
  - Total: 100%
  
Day 3-7: Stay at 100% (resupply > consumption, lord maintains food)

Result: No supply concerns, vanilla system keeps lord fed
```

### **Week 2: Campaign Begins**
```
Day 8: Leave settlement, begin patrol
  - Lord has 10 days of food → Food Component: 40%
  - Non-Food: 60% - 1.8% (patrol rate) = 58.2%
  - Total: 98.2%
  
Day 9: Battle against 80 Empire troops
  - Lord has 9 days of food → Food Component: 40%
  - Non-Food: 58.2% - 1.8% travel - 4% battle loss + 4% loot = 56.4%
  - Total: 96.4%
  
Day 10-11: Travel (no food buying opportunities, vanilla consuming food)
  - Lord has 7 days of food → Food Component: 35%
  - Non-Food: 56.4% - 3.6% = 52.8%
  - Total: 87.8%
  
Day 12: Foraging duty
  - Player adds 3 food items to lord's party → Lord now has 9 days
  - Food Component: 40%
  - Non-Food: 52.8% + 3% = 55.8%
  - Total: 95.8%
  
Day 13-14: Return to settlement
  - Lord buys food (vanilla) → 12 days → Food Component: 40%
  - Non-Food: 55.8% + 6% (two days resupply) = 60% (capped)
  - Total: 100%

Result: Hybrid system works! Vanilla handles food, we manage other logistics
```

### **Week 3: Siege (Hard Mode)**
```
Day 15: Begin siege (lord has 10 days of food)
  - Food Component: 40%
  - Non-Food: 60% - 3.75% (siege rate, 100 troops) = 56.25%
  - Total: 96.25%
  
Day 16-18: Siege continues (vanilla consuming food, can't resupply)
  - Lord now has 7 days → Food Component: 35%
  - Non-Food: 56.25% - 11.25% = 45%
  - Total: 80% (WARNING at 50%)
  
Day 19: Assault battle (casualties, lord at 5 days food)
  - Food Component: 30%
  - Non-Food: 45% - 3.75% daily - 6% battle loss = 35.25%
  - Total: 65.25%
  
Day 20: Player does equipment maintenance duty
  - Food Component: 30% (lord at 4 days)
  - Non-Food: 35.25% - 3.75% + 3% maintenance = 34.5%
  - Total: 64.5%
  
Day 21-23: Continue siege (CRITICAL - lord at 2 days food)
  - Food Component: 10% (LOW)
  - Non-Food: 34.5% - 11.25% = 23.25%
  - Total: 33.25% (CRITICAL - equipment menu blocked!)
  
Day 24: Player negotiates with nearby town merchant (+500g spent)
  - Lord buys 5 food (+2 days) → Food Component: 20%
  - Non-Food: 23.25% - 3.75% + 8% purchase = 27.5%
  - Total: 47.5% (Still critical)
  
Day 25: Siege victory! Loot supplies, enter town
  - Lord forages from captured town (vanilla) → 10 days → Food Component: 40%
  - Non-Food: 27.5% + 5% loot + 5% town resupply = 37.5%
  - Total: 77.5%
  
Day 26-30: Recover in town
  - Food Component: 40% (lord maintains via vanilla auto-buy)
  - Non-Food: 37.5% + 15% (3 days × 5%) = 52.5%
  - Total: 92.5%

Result: Intense siege pressure, realistic logistics, required active management
```

---

## Summary: Why This Hybrid Approach Works

### **Respects Vanilla AI:**
✅ Lord's AI continues to buy food automatically via `PartiesBuyFoodCampaignBehavior`  
✅ AI decision-making (raids, sieges) still uses `GetNumDaysForFoodToLast()`  
✅ Army food sharing continues to work naturally  
✅ Starvation mechanics remain intact  

### **Adds Depth Without Conflicts:**
✅ Non-food logistics (60% of supply) are fully simulated by us  
✅ Food status (40% of supply) is observed, not modified  
✅ Player can still influence food via adding items (vanilla-compatible)  
✅ Equipment gate is based on our hybrid supply metric  

### **Mod Compatibility:**
✅ No modifications to native `MobileParty` properties  
✅ No interference with other mods' food/party systems  
✅ All changes are additive (our own systems)  
✅ Read-only observation of vanilla state  

### **Gameplay Benefits:**
✅ Realistic siege pressure (food + non-food both deplete)  
✅ Player agency (manage non-food via duties)  
✅ Automatic food handling (lord's AI prevents micro-management)  
✅ Meaningful equipment gate (tied to overall logistics)  

### **Implementation Simplicity:**
✅ One manager class (`CompanySupplyManager`)  
✅ Two independent components (food observed, non-food simulated)  
✅ Clean separation of concerns  
✅ Easy to debug and balance  

---

**Status**: Core implementation complete (December 2025).

**Completed:**
1. ✅ Implemented `CompanySupplyManager` class with hybrid 40/60 model
2. ✅ Hooked into `CompanyNeedsState.Supplies` property
3. ✅ Added daily tick handler for non-food consumption/resupply
4. ✅ Added battle supply changes (losses and loot)
5. ✅ Added save/load support for non-food supply value
6. ✅ Edge cases handled (lord captured, grace period, leave)

**Next Steps (Future Enhancements):**
1. Update duty completion handlers to add non-food supplies (foraging duty)
2. Add supply purchase option from quartermaster
3. Test with various lord behaviors (siege, travel, peace)
4. Balance consumption/resupply rates based on playtesting
5. Add visual UI indicator for supply status

