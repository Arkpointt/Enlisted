# Provisions & Rations System

**Summary:** The provisions system manages player food requirements across two distinct phases of military service: T1-T6 enlisted soldiers and NCOs receive issued rations every 12 days (ration exchange system), while T7+ officers must purchase provisions from the quartermaster at premium prices. The system creates meaningful food scarcity, integrates with company supply levels, and uses quartermaster reputation to determine ration quality and pricing.

**Status:** ✅ Current  
**Last Updated:** 2025-12-22  
**Related Docs:** [Quartermaster System](quartermaster-system.md), [Company Supply](company-supply-simulation.md)

---

## Index

1. [Overview](#overview)
2. [Rank-Based Transition](#rank-based-transition)
3. [T1-T6: Issued Rations](#t1-t6-issued-rations)
4. [T7+: Officer Provisioning](#t7-officer-provisioning)
5. [Food Consumption Mechanics](#food-consumption-mechanics)
6. [Personal Food Management](#personal-food-management)
7. [Food Loss Events](#food-loss-events)
8. [Food Shortage Events](#food-shortage-events)
9. [Integration & Implementation](#integration--implementation)

---

## Overview

The provisions system reflects the player's progression from enlisted soldier to officer by changing how food is acquired and managed:

**Tier Structure:**
- **T1-T4**: Enlisted (regular soldiers)
- **T5-T6**: NCO (Non-Commissioned Officers)
- **T7-T9**: Officers

### T1-T6 (Enlisted/NCO): Issued Rations
- **Ration exchange at muster** (every 12 days)
- **Quality based on quartermaster reputation** (grain → butter → cheese → meat)
- **Availability based on company supply** (100% → 0% as supply drops)
- **Cannot accumulate** - old rations reclaimed when new ones issued
- **Must supplement** with personal food during shortages

### T7+ (Officer): Self-Provisioning
- **No more issued rations** - transition at T7 promotion (first officer rank)
- **Quartermaster provisions shop** - premium pricing (150-200% of town markets)
- **Supply-dependent inventory** - stock refreshes every muster
- **Reputation affects pricing** - best discount: 150% (never cheaper than towns)
- **Officer responsibility** - prepares for leading troops with food requirements

**Core Principle:** Food becomes a strategic resource that creates scarcity pressure during campaigns, forcing active management rather than passive accumulation.

---

## Rank-Based Transition

### T1-T6: Enlisted/NCO Life
```
Muster Day → Quartermaster Exchange
↓
1. Reclaim old issued ration (if issuing new one)
2. Check company supply availability
3. Issue new ration (if available) based on QM rep
4. Player keeps personal food (bought/foraged)
```

### T7 Promotion: The Transition
```
Quartermaster: "Congratulations on your promotion, Lieutenant.
    
As an officer, you're no longer issued rations. You're expected to 
manage your own provisions.
    
I have a shop available - prices are higher than town markets, but 
it's convenient when you're in the field. Stock refreshes every muster."

[Reclaims any remaining issued rations]

Your ration days are over. Welcome to the officer corps.
```

### T7+: Officer Life
```
Muster Day → Provisions Shop Refresh
↓
1. Quartermaster inventory refreshes
2. Stock quantity based on company supply
3. Prices based on QM reputation (150-200%)
4. Officer buys what they need
5. Purchased food is personal (not reclaimed)
```

---

## T1-T6: Issued Rations

### Ration Exchange System

The ration exchange system prevents accumulation while maintaining military logistics realism:

**At Each Muster (12 Days):**

1. **If issuing new ration:**
   - Reclaim previous issued ration (if exists in inventory/stowage)
   - Issue new ration based on supply/reputation
   
2. **If NOT issuing (low supply):**
   - Nothing reclaimed (player keeps what they have)
   - Player must buy/forage food to survive

**Why This Works:**
- Prevents endless stockpiling
- Creates natural scarcity during campaigns
- Forces engagement with food management
- Mirrors real military supply distribution

### Ration Availability

Ration availability depends on company supply levels:

| Company Supply | Ration Chance | Experience |
|----------------|---------------|------------|
| **70-100%** | 100% | Always receive rations |
| **50-69%** | 80% | 1 in 5 musters: no ration |
| **30-49%** | 50% | Coin flip each muster |
| **< 30%** | 0% | No rations ever (equipment also blocked) |

**Scarcity Scenarios:**

**High Supply (70%+):**
- Predictable: Always get ration
- Strategy: No action needed
- Cost: 0g (rations provided)

**Medium Supply (50-69%):**
- Risky: 20% chance no ration
- Strategy: Keep 1 personal food as backup
- Cost: ~30g every 2-3 musters

**Low Supply (30-49%):**
- Crisis: 50% chance no ration
- Strategy: Actively buy/forage food
- Cost: 50-75g per muster cycle

**Critical Supply (<30%):**
- Emergency: No rations ever
- Strategy: Buy from towns or forage duties
- Cost: ~40g every 12 days + crisis management

### Ration Quality

Ration quality is determined by quartermaster reputation:

| QM Reputation | Food Item | Value | Lasts (Solo) | Flavor |
|---------------|-----------|-------|--------------|--------|
| **80 to 100** | Meat | 30g | 20 days | "The quartermaster favors you." |
| **50 to 79** | Cheese | 50g | 20 days | "The quartermaster is being generous." |
| **20 to 49** | Butter | 40g | 20 days | "Better than grain, at least." |
| **-9 to 19** | Grain | 10g | 20 days | "Standard rations." |
| **-50 to -10** | Grain | 10g | 20 days | "It's moldy and barely edible." |

**Key Points:**
- All food items provide same nutrition (1 food = 20 days for 1 person)
- Difference is value/flavor, not survival duration
- Reputation affects quality when rations are available
- Supply affects whether rations are issued at all

### Anti-Abuse: Tracking System

Issued rations are tracked to prevent hiding in stowage:

```csharp
// Save/load this list
private List<ItemRosterElement> _issuedFoodRations = new List<ItemRosterElement>();

// At muster: reclaim from ALL storage locations
private void ReclaimIssuedRations()
{
    foreach (var rationElement in _issuedFoodRations)
    {
        // Check main inventory
        int inInventory = party.ItemRoster.GetItemNumber(item);
        
        // Check stowage (prevents hiding)
        int inStowage = GetItemCountInStowage(item);
        
        // Reclaim from both locations
        // ...
    }
    
    _issuedFoodRations.Clear();
}
```

**Protection:**
- Issued rations tracked in persistent registry
- Reclamation scans inventory AND baggage stash
- Personal food (bought/foraged) never touched
- Consumed/sold rations: no penalty

**Example Exploit Prevention:**
```
Attempt:
1. Muster 1: Get 1 grain (issued)
2. Player: Hide grain in stowage
3. Muster 2: Get 1 butter (issued)
4. Player thinks: "I have 2 food now!"

Reality:
5. Muster 3: System reclaims both grain (from stowage) and butter (from inventory)
6. Result: Only new ration remains
```

---

## T7+ Officer Provisioning

### Officer Transition

**At T7 Promotion (First Officer Rank):**
```csharp
private void OnPromotedToOfficer()
{
    // Reclaim any remaining issued rations
    if (_issuedFoodRations.Count > 0)
    {
        ReclaimIssuedRations();
        
        InformationManager.DisplayMessage(
            "As an officer, you are no longer issued rations. " +
            "You must purchase provisions from the Quartermaster or town markets.");
    }
    
    // Unlock QM provisions shop
    _qmFoodShopUnlocked = true;
}
```

### Provisions Shop Pricing

Officers pay a premium for the convenience of field provisioning:

| QM Reputation | Price Multiplier | Example (Grain: 10g) | Example (Meat: 30g) |
|---------------|------------------|---------------------|---------------------|
| **61 to 100** | **1.5x (150%)** | **15g** | **45g** |
| **31 to 60** | 1.75x (175%) | 17.5g | 52.5g |
| **1 to 30** | 1.9x (190%) | 19g | 57g |
| **-50 to 0** | 2.0x (200%) | 20g | 60g |

**Design Principle:**
- Even at maximum reputation: **never cheaper than 150% of town markets**
- Premium reflects: field convenience, no travel required, guaranteed availability
- Officers earn more wages, can afford the premium
- Smart officers use towns when possible, QM during campaigns

### Inventory by Supply Level

Quartermaster shop inventory refreshes every muster (12 days), with stock determined by company supply:

#### High Supply (70-100%): Full Stock
```
=== QUARTERMASTER PROVISIONS ===

Company Supply: 75% (Good stock)
Your Reputation: +45 (Friendly - 175% of market prices)

Available Provisions (refreshes in 8 days):
  [Grain] - 6 available - 17g each (market: 10g)
  [Butter] - 4 available - 70g each (market: 40g)
  [Cheese] - 3 available - 87g each (market: 50g)
  [Meat] - 3 available - 52g each (market: 30g)
  [Fish] - 2 available - 61g each (market: 35g)

Total Available: ~18 food items (360 days worth)
```

#### Medium Supply (50-69%): Limited Stock
```
Available Provisions:
  [Grain] - 4 available - 17g each
  [Butter] - 2 available - 70g each
  [Meat] - 2 available - 52g each

Total Available: ~8 food items (160 days worth)
```

#### Low Supply (30-49%): Minimal Stock
```
Available Provisions:
  [Grain] - 2 available - 17g each
  [Butter] - 1 available - 70g each

Total Available: ~3 food items (60 days worth)
NOT ENOUGH for full muster cycle!
```

#### Critical Supply (<30%): Bare Minimum
```
QM: "I've only got grain right now. Two bags. That's it.
    
We're in crisis mode. I'd recommend hitting the town markets 
until our supply situation improves."

Available Provisions:
  [Grain] - 2 available - 17g each

Total Available: ~2 food items (40 days worth)
CRITICAL SHORTAGE - must use towns!
```

### Officer Food Economics

**Solo Officer (1 Party Member):**

Monthly Consumption: 1.5 food items (30 days ÷ 20 days per food)

| Source | Cost | Notes |
|--------|------|-------|
| **Town Markets** | ~45g/month | Cheaper but requires travel |
| **QM (max discount)** | ~68g/month | +23g premium for convenience |
| **QM (low rep)** | ~90g/month | +45g premium, expensive |

**Convenience Premium:** ~23g/month to buy from QM vs towns at max rep

**When to Use QM:**
- Active campaign (no time for towns)
- Siege/war (towns inaccessible)
- Emergency (ran out suddenly)
- High wages (premium doesn't matter)

**Officer with Small Detachment (5 Troops - Future):**

Monthly Consumption: 7.5 food items (5 members × 30 days ÷ 20)

| Source | Cost | Impact |
|--------|------|--------|
| **Towns** | ~225g/month | Manageable with officer wages |
| **QM (max)** | ~338g/month | Major expense, +113g premium |

**Strategic Implication:** Leading troops makes food a significant logistics burden

---

## Food Consumption Mechanics

### Native Bannerlord Food Math

From `DefaultMobilePartyFoodConsumptionModel.cs`:

```csharp
public override int NumberOfMenOnMapToEatOneFood => 20;

public override ExplainedNumber CalculateDailyBaseFoodConsumptionf(MobileParty party)
{
    int num = party.Party.NumberOfAllMembers + party.Party.NumberOfPrisoners / 2;
    return new ExplainedNumber(-(float)num / 20.0f);
}
```

**Key Formula:**
```
Daily Food Consumption = (Party Members + Prisoners/2) / 20
```

**For Solo Player:**
```
Daily Consumption = 1 / 20 = 0.05 food per day
12-Day Cycle = 0.05 × 12 = 0.6 food needed
```

**Food Item Duration:**
```
1 food item = 20 days for 1 person
```

**This means:**
- Issued ration (1 food) lasts 20 days
- Muster cycle is 12 days
- Player has ~8 days buffer after each muster
- **BUT** ration is reclaimed at next muster if new one issued

### Scarcity Math Example

**Stable Cycle (Supply 70%+):**
```
Day 0: Muster - Get 1 grain (issued)
Day 12: Consumed 0.6 food, have 0.4 remaining (8 days worth)
Day 12: Muster - Reclaim 0.4 grain, issue 1 new butter
Day 24: Consumed 0.6 food, have 0.4 remaining
[Stable cycle continues]
```

**Shortage Cycle (Supply 50%):**
```
Day 0: Muster - Get 1 grain
Day 12: Muster - Coin flip... NO RATION (bad luck)
Day 12: Old ration NOT reclaimed, still have 0.4 grain (8 days)
Day 16: Personal food runs out (4 days later)
Day 16-24: Player MUST buy food from town (~30g)
Day 24: Muster - Coin flip... GET RATION (lucky!)
[Cost: 30g for backup food]
```

**Crisis Cycle (Supply 30%):**
```
Day 0: Muster - Get 1 grain
Day 12: Muster - NO RATION (supply too low)
Day 16: Buy 1 grain from town (30g)
Day 24: Muster - NO RATION again
Day 28: Buy 1 grain from town (30g)
Day 36: Muster - Still NO RATION
[Cost: 60-90g per month during crisis]
```

---

## Personal Food Management

### Personal vs. Issued Food

| Type | Source | Tracked? | Reclaimed? | Loss Events? |
|------|--------|----------|------------|--------------|
| **Issued Rations (T1-T6)** | Quartermaster at muster | Yes | Yes (at next muster) | No (immune) |
| **Personal Food** | Bought from towns, foraged, hunted | No | Never | Yes (rats, spoilage, theft) |
| **Officer Provisions (T7+)** | Bought from QM shop | No | Never | Yes (treated as personal) |

### Building Food Reserves

Players can supplement issued rations with personal food:

**Sources of Personal Food:**

| Source | Method | Cost | Notes |
|--------|--------|------|-------|
| **Town Markets** | Visit marketplace | 20-50g per food | Most reliable |
| **Foraging Duty** | Complete duty assignment | Free | +3-8% company supply, +1 grain |
| **Hunting Wildlife** | Event (Cunning 30+) | Free | Random encounter, +1 meat |
| **Ask Comrades** | Emergency dialogue | -5 Soldier Rep | One-time option |

**Strategic Reserve Management:**

**Conservative (Safe):**
- Keep 2 personal food at all times
- Cost: ~60g initial + 30-60g/month replacement
- Never risk starvation
- Slight loss to random events

**Risky (Economical):**
- Only buy when ration not issued
- Cost: 0-60g/month depending on supply
- Risk: Sudden shortages
- Must have town access

**Example Smart Player:**
```
Day 1: Buy 2 grain (60g) - personal reserve
Day 12: Muster - Get 1 butter (issued)
  Total: 2 grain (personal) + 1 butter (issued) = 3 food
Day 18: Rats event → Lose 1 grain (personal)
  Total: 1 grain (personal) + 1 butter (issued) = 2 food
Day 24: Muster - Reclaim butter, issue 1 meat
  Total: 1 grain (personal) + 1 meat (issued) = 2 food
Day 36: Buy 2 more grain (60g)
  
Cost: 120g over 36 days - never starved, stable reserves
```

---

## Food Loss Events

### Purpose

Personal food loss events prevent infinite reserve accumulation and create ongoing food pressure.

**Design Principle:**
- Only affect **personal food** (bought/foraged)
- **Never** affect issued rations (immune)
- Only trigger when player has **2+ personal food items**
- Frequency increases with larger reserves (hoarding penalty)

### Event Trigger Logic

```csharp
private void CheckPersonalFoodLossEvents()
{
    // Count personal food (total food minus issued rations)
    int totalFood = party.ItemRoster.TotalFood;
    int issuedFood = CountIssuedRations(); // 0 or 1
    int personalFood = totalFood - issuedFood;
    
    // Only trigger if 2+ personal food
    if (personalFood < 2) return;
    
    // Base chance: 5% per day with 2-3 personal food
    float chance = 0.05f;
    if (personalFood >= 4) chance = 0.10f; // Hoarding penalty
    if (personalFood >= 6) chance = 0.15f; // Severe hoarding penalty
    
    if (MBRandom.RandomFloat < chance)
    {
        TriggerPersonalFoodLossEvent();
    }
}
```

### Event Types

#### 1. Food Spoilage (Summer/Heat)
```
Event: "Food Spoilage"

The summer heat has spoiled some of your personal food stores. 
Flies buzz around the rotten provisions.

Effect: Lose 1 personal food item

Conditions: Summer season, not in settlement, 5-10% daily chance
```

#### 2. Rats in Camp
```
Event: "Rats in Camp"

You discover rats have gotten into your personal food supplies.

Options:
  [Accept Loss] → Lose 1 food
  [Hunt the Rats] (Cunning 50+) → 50% chance: No loss + 5 XP
                                   50% chance: Still lose food
  [Set Traps] (Costs 5g) → Lose food but gain 1 fur (worth 10g)

Conditions: In camp, 5% daily chance
```

#### 3. Theft by Comrade
```
Event: "Missing Provisions"

You notice some of your personal food supplies have gone missing. 
A fellow soldier eyes you guiltily.

Options:
  [Confront Him] → Lose 1 food, gain "Comrade Debt" event
                   (He owes you; +10 rep when repaid later)
  [Let It Slide] → Lose 1 food, +5 Soldier Rep (generous)
  [Report to NCO] → Lose 1 food, thief punished, -10 Soldier Rep

Conditions: Enlisted, 3+ personal food, Soldier Rep < 70, 3% daily chance
```

#### 4. Battle Damage
```
Event: "Battle Damage"

Your pack was damaged in the recent battle. Some of your 
personal provisions are ruined or scattered.

Effect: 
  - Lose 1 personal food
  - If 4+ personal food, lose 2 instead

Conditions: After any battle, 20% chance, scales with battle intensity
```

#### 5. Desperate Refugee
```
Event: "Desperate Refugee"

A starving refugee approaches your camp, begging for food. 
She has a small child with her.

Options:
  [Give Food] → Lose 1 food, +5 Honor, +10 Charm XP
  [Refuse] → No loss, -3 Honor
  [Sell Food] → Lose 1 food, +20 gold, -5 Honor, -5 Soldier Rep

Conditions: War zone, recently raided area, 3% daily chance
```

#### 6. Checkpoint Shakedown
```
Event: "Checkpoint Shakedown"

Guards at a checkpoint demand "tribute" - they eye your provisions hungrily.

Options:
  [Pay Bribe] → Pay 30g, keep food
  [Give Food] → Lose 1 food, no gold cost
  [Refuse] (Roguery 40+) → 60% success: No loss
                           40% failure: Lose 1 food + 10g "fine"

Conditions: Non-friendly territory, 2% daily chance
```

---

## Food Shortage Events

### Low Food Warning

Triggers when player has < 3 days of food remaining:

```
Warning: "Running Low on Food"

You're running low on rations. You should buy food from a town 
or forage before you starve.

Days of food remaining: 2

[Message auto-appears, no action required]
```

### Starvation Event

Triggers when `Party.IsStarving == true`:

```
Event: "Starving"

You have no food left. Your stomach growls painfully.

Options:
  [Buy Food from Town] → Costs 20-50g depending on settlement
  [Forage] (Cunning 30+) → Gain 1 food (grain), +5 Cunning XP
  [Ask Quartermaster] (QM Rep 30+) → Gain 1 food, -10 QM Rep
  [Endure] → -10 HP, -5 Morale
```

**Conditions:**
- `MobileParty.Party.IsStarving == true`
- Player has < 1 day of food

---

## Integration & Implementation

### Muster Event Integration

```csharp
private void OnMusterDay()
{
    int playerTier = GetPlayerTier();
    
    if (playerTier < 7)
    {
        // T1-T6: Process ration exchange (Enlisted and NCOs)
        ProcessMusterFoodRation();
    }
    else
    {
        // T7+: Refresh QM provisions shop (Officers)
        RefreshQuartermasterFoodInventory();
    }
    
    // Existing: Pay wages
    PayWages();
    
    // Reset muster tracking
    _nextPayday = CampaignTime.DaysFromNow(12);
}
```

### Daily Tick Integration

```csharp
private void OnDailyTick()
{
    if (IsEnlisted && !IsOnLeave)
    {
        // Check for personal food loss events
        CheckPersonalFoodLossEvents();
        
        // Check for food shortage warnings
        CheckFoodShortageEvents();
    }
}
```

### Post-Battle Integration

```csharp
private void OnBattleEnd()
{
    // Check for battle damage to food
    if (HasPersonalFood(2) && MBRandom.RandomFloat < 0.20f)
    {
        TriggerBattleFoodDamageEvent();
    }
}
```

### Discharge Integration

```csharp
private void OnServiceEnded()
{
    // Reclaim issued rations (T1-T6 only)
    ReclaimIssuedRations();
    
    // Personal food is retained
    
    // No penalty for consumed rations
}
```

### Implementation Checklist

**Phase 1: T1-T6 Ration System (Enlisted/NCO)**
- [ ] Add `_issuedFoodRations` list to save/load
- [ ] Implement `ProcessMusterFoodRation()` with exchange logic
- [ ] Implement `ReclaimIssuedRations()` (scan inventory + stowage)
- [ ] Integrate with company supply for availability checks
- [ ] Test ration exchange cycle

**Phase 2: T7+ Officer Provisions**
- [ ] Add `_qmFoodShop` inventory to save/load
- [ ] Implement `RefreshQuartermasterFoodInventory()` with supply tiers
- [ ] Implement `CalculateOfficerFoodPrice()` with rep multipliers
- [ ] Create provisions shop UI
- [ ] Implement T7 transition (reclaim rations, unlock shop)

**Phase 3: Personal Food Loss Events**
- [ ] Implement `CheckPersonalFoodLossEvents()` daily check
- [ ] Create event definitions in JSON
- [ ] Implement targeting logic (exclude issued rations)
- [ ] Test frequency balancing

**Phase 4: Integration**
- [ ] Hook into muster event
- [ ] Hook into daily tick
- [ ] Hook into post-battle
- [ ] Hook into discharge
- [ ] Test full lifecycle

### Configuration

Location: `ModuleData/Enlisted/retirement_config.json` (or new `provisions_config.json`)

```json
{
  "rationAvailability": {
    "highSupply": 100,
    "mediumSupply": 80,
    "lowSupply": 50,
    "criticalSupply": 0
  },
  "officerPricing": {
    "maxRepMultiplier": 1.5,
    "lowRepMultiplier": 2.0
  },
  "lossEventChances": {
    "baseChance": 0.05,
    "hoardingChance": 0.10,
    "severeHoardingChance": 0.15
  }
}
```

### Related Source Files

| File | Purpose |
|------|---------|
| `src/Features/Enlistment/Behaviors/EnlistmentBehavior.cs` | Muster event, ration exchange |
| `src/Features/Equipment/Behaviors/QuartermasterManager.cs` | Provisions shop logic |
| `src/Features/Content/EventDeliveryManager.cs` | Food loss events |
| `ModuleData/Enlisted/Events/events_food_loss.json` | Food event definitions |

---

**End of Document**

