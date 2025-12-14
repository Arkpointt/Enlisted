# Lord Financial Crisis System — Implementation Guide

**Status:** Research & Design Phase  
**System:** Financial Crisis Tracker (Expanded Lord Wealth Status)  
**Target:** EnlistmentBehavior.cs Pay System Extension  
**Dependencies:** Native Bannerlord finance systems, Pay Tension events

---

## Table of Contents

1. [Executive Summary](#executive-summary)
2. [Native Code Research](#native-code-research)
3. [Current System Analysis](#current-system-analysis)
4. [Proposed System Design](#proposed-system-design)
5. [Implementation Phases](#implementation-phases)
6. [Integration Points](#integration-points)
7. [Event Schema Extensions](#event-schema-extensions)
8. [Testing Strategy](#testing-strategy)
9. [Rollout Plan](#rollout-plan)

---

## Executive Summary

### Purpose

Expand the existing **Lord Wealth Status** (simple gold check) into a **Lord Financial Crisis Tracker** that monitors multiple native game systems to create richer storytelling around lords in financial distress.

### Why This Matters

**Current State:**
- Lord Wealth Status only checks `lord.Gold`
- Binary outcome: lord can pay OR lord can't pay
- Limited narrative context

**Enhanced State:**
- Track 5 financial indicators from native systems
- Multi-factor crisis severity (Stable → Strained → Critical → Collapse)
- Rich narrative context for pay tension events

**Example:**
```
CURRENT: "Lord can't pay you (broke)"
ENHANCED: "Lord is in debt to the kingdom (5000 gold), his troops haven't been paid in days 
           (desertion imminent), and the party has 2 days of food remaining with no money to 
           buy more. He can't pay you because the entire operation is collapsing."
```

### Key Finding from Native Code

**Lords DO have financial problems in vanilla Bannerlord:**
- Native parties track unpaid wages (`MobileParty.HasUnpaidWages`)
- Clans accumulate kingdom debt (`Clan.DebtToKingdom`)
- AI parties can't buy food if they lack gold
- Native desertion occurs when wages aren't paid
- Kingdom bailout exists but is limited (500-2000 gold, only triggers below 30k)

**This system leverages existing native mechanics rather than inventing new ones.**

---

## Native Code Research

### Critical Native Systems Discovered

#### 1. Party Wage Payment System
**Location:** `TaleWorlds.CampaignSystem.GameComponents.DefaultClanFinanceModel`

**Key Thresholds:**
```csharp
private const int payGarrisonWagesTreshold = 8000;
private const int payClanPartiesTreshold = 4000;
private const int payLeaderPartyWageTreshold = 2000;
```

**Behavior:**
- Clan gold < 2000 → Leader party gets $0 wage budget
- Clan gold < 4000 → Other parties get minimal/zero wages  
- Clan gold < 8000 → Garrisons get reduced wages

**Code Reference:**
```csharp
// From AddPartyExpense() in DefaultClanFinanceModel
int a1 = clan.Gold + (int) goldChange.ResultNumber;
int num1 = a1;
if (a1 < (party.IsGarrison ? 8000 : 4000) & applyWithdrawals && clan != Clan.PlayerClan)
    num1 = party.LeaderHero == null || party.PartyTradeGold >= 500 ? 0 : MathF.Min(a1, 250);
```

#### 2. Unpaid Wages Tracking
**Location:** `MobileParty.HasUnpaidWages` (float, 0.0 to 1.0)

**Morale Impact:**
```csharp
// From ApplyMoraleEffect() in DefaultClanFinanceModel
if (paymentAmount < wage && wage > 0)
{
    float num = (float) (1.0 - (double) paymentAmount / (double) wage);
    float moraleHit = (float) Campaign.Current.Models.PartyMoraleModel
        .GetDailyNoWageMoralePenalty(mobileParty) * num;
    mobileParty.RecentEventsMorale += moraleHit;
    mobileParty.HasUnpaidWages = num;
}
```

**Desertion Impact:**
```csharp
// From GetTroopsToDesertDueToWageAndPartySize() in DefaultPartyDesertionModel
if (mobileParty.IsGarrison && (double) mobileParty.HasUnpaidWages > 0.0)
    desertionCount += MathF.Min(mobileParty.Party.NumberOfHealthyMembers, 5);
```

#### 3. Kingdom Debt System
**Location:** `Clan.DebtToKingdom` (int)

**How Debt Accumulates:**
```csharp
// When clan can't afford kingdom expenses (tributes, mercenaries, call-to-war)
if (applyWithdrawals && goldRemaining - expenseShare < 5000)
{
    expenseShare = MathF.Max(0, goldRemaining - 5000);
    clan.DebtToKingdom += (originalExpense - expenseShare);
}
```

**Debt Payment:**
```csharp
// From AddPaymentForDebts()
int debtPayment = MathF.Min(clan.DebtToKingdom, clan.Gold + goldChange);
clan.DebtToKingdom -= debtPayment;
goldChange.Add((float) -debtPayment);
```

#### 4. Food Purchase System
**Location:** `PartiesBuyFoodCampaignBehavior.cs`

**Critical Check:**
```csharp
// Lords only buy food if they can afford it
if ((double) itemPrice <= (double) mobileParty.PartyTradeGold)
    SellItemsAction.Apply(settlement.Party, mobileParty.Party, itemRosterElement, 1);
```

**Food Days Calculation:**
```csharp
// From MobileParty
public int GetNumDaysForFoodToLast()
{
    int totalFood = this.ItemRoster.TotalFood * 100;
    return (int) ((double) totalFood / (100.0 * -(double) this.FoodChange));
}
```

#### 5. Kingdom Bailout System
**Location:** `AddIncomeFromKingdomBudget()` in DefaultClanFinanceModel

**Bailout Conditions:**
```csharp
// Only triggers for AI lords in kingdoms
if (clan.Gold < 30000 && clan.Kingdom != null && 
    clan.Leader != Hero.MainHero && !clan.IsUnderMercenaryService)
{
    int bailout = (clan.Gold < 5000 ? 2000 : 
                   clan.Gold < 10000 ? 1500 : 
                   clan.Gold < 20000 ? 1000 : 500);
    // Limited by kingdom budget availability
    int amount = MathF.Min(clan.Kingdom.KingdomBudgetWallet, bailout);
    goldChange.Add((float) amount);
}
```

**Key Insight:** Bailout is modest and only delays crisis, doesn't prevent it.

---

## Current System Analysis

### Existing Implementation

**Location:** `EnlistmentBehavior.cs` lines 4653-4807

**Current Lord Wealth Status:**
```csharp
internal enum LordWealthStatus { Wealthy, Comfortable, Struggling, Poor, Broke }

private LordWealthStatus GetLordWealthStatus()
{
    var gold = _enlistedLord?.Gold ?? 0;
    if (gold > 50000) return LordWealthStatus.Wealthy;
    if (gold > 20000) return LordWealthStatus.Comfortable;
    if (gold > 5000) return LordWealthStatus.Struggling;
    if (gold > 1000) return LordWealthStatus.Poor;
    return LordWealthStatus.Broke;
}
```

**Pay Modifier Usage:**
```csharp
private float GetLordWealthPayModifier()
{
    var status = GetLordWealthStatus();
    return status switch
    {
        LordWealthStatus.Wealthy => 1.1f,      // +10% pay
        LordWealthStatus.Comfortable => 1.0f,   // Standard pay
        LordWealthStatus.Struggling => 0.9f,    // -10% pay
        LordWealthStatus.Poor => 0.75f,         // -25% pay
        LordWealthStatus.Broke => 0.5f,         // -50% pay
        _ => 1.0f
    };
}
```

**Payment Resolution:**
```csharp
// Line 3877-3899 in daily tick pay muster resolution
var canPayFull = CanLordAffordPay(totalWithBackpay);
var canPayPartial = !canPayFull && CanLordAffordPartialPay(totalWithBackpay);
var wealthStatus = GetLordWealthStatus();

if (canPayFull && totalWithBackpay > 0)
    ProcessFullPayment(totalWithBackpay);
else if (canPayPartial && totalWithBackpay > 0)
    ProcessPartialPayment(payout, totalWithBackpay);
else if (payout > 0)
    ProcessPayDelay(payout);  // Accumulates backpay, increases PayTension
```

### Strengths of Current System

✅ **Simple and reliable** - Gold check never fails  
✅ **Pay modifier working** - Wealthy lords pay +10%, broke lords pay -50%  
✅ **Pay delay tracking** - Backpay and tension accumulate correctly  
✅ **Clean integration** - Works with existing pay tension events

### Limitations of Current System

❌ **Narrow context** - Only sees gold, not broader financial distress  
❌ **Binary outcome** - Lord can pay OR lord can't pay (no nuance)  
❌ **Missed storytelling** - Can't tell rich stories about *why* lord is broke  
❌ **Ignores native systems** - Doesn't leverage native unpaid wages, debt, food crises

### Integration Points Discovered

**1. Daily Tick Execution (Line 3803-3843)**
```csharp
private void ProcessEnlistedDailyService()
{
    // ... existing wage accrual ...
    
    // INTEGRATION POINT: Add lord financial state check here
    // CheckLordFinancialCrisis();
    
    // Phase 5: Check for NPC soldier desertion when pay tension is high
    CheckNpcDesertionFromPayTension();
}
```

**2. Pay Tension Event Triggers**
- Events use `pay_tension_min` in trigger requirements
- Crisis level could be exposed as additional trigger tokens
- Example: `lord_crisis:critical` or `lord_debt:high`

**3. Lance Life Events System**
- Uses `LanceLifeEventTriggerEvaluator.cs` for condition checks
- Can add custom trigger tokens for crisis indicators
- Automatic event firing when thresholds crossed

---

## Proposed System Design

### Architecture Overview

**Core Principle:** Expand existing Lord Wealth Status with additional checks while maintaining backward compatibility.

```
┌─────────────────────────────────────────────────────────┐
│ CURRENT: Lord Wealth Status (Simple)                    │
│  └─> Checks: lord.Gold                                  │
│      └─> Returns: Wealthy/Comfortable/Struggling/Poor/Broke
└─────────────────────────────────────────────────────────┘
                          ⬇ EXPAND TO ⬇
┌─────────────────────────────────────────────────────────┐
│ ENHANCED: Lord Financial Crisis Tracker                 │
│  ├─> Check 1: lord.Gold                                 │
│  ├─> Check 2: lordParty.HasUnpaidWages (native)        │
│  ├─> Check 3: clan.DebtToKingdom (native)              │
│  ├─> Check 4: Food crisis (days + gold)                │
│  └─> Check 5: Recent party desertion                    │
│      └─> Returns: CrisisLevel (Stable/Strained/Critical/Collapse)
└─────────────────────────────────────────────────────────┘
```

### Data Structure

**Add new struct/class:**
```csharp
/// <summary>
/// Multi-factor financial crisis state for enlisted lord.
/// Tracks native game indicators to provide rich narrative context.
/// </summary>
internal class LordFinancialCrisisState
{
    // Primary indicator (existing)
    public int LordGold { get; set; }
    public LordWealthStatus WealthStatus { get; set; }  // Keep for backward compat
    
    // Native system indicators (NEW)
    public bool HasUnpaidNativeTroops { get; set; }
    public float UnpaidWagesRatio { get; set; }  // 0.0 to 1.0
    public int KingdomDebt { get; set; }
    public int DaysOfFoodRemaining { get; set; }
    public int PartyTradeGold { get; set; }
    public bool CanAffordFood { get; set; }
    
    // Derived indicators
    public bool RecentDesertion { get; set; }  // Lost troops in last 7 days
    public int CrisisScore { get; set; }  // 0-10 composite score
    public LordFinancialCrisisLevel CrisisLevel { get; set; }
    
    // Timestamp
    public CampaignTime LastCalculated { get; set; }
}

/// <summary>
/// Lord's overall financial crisis severity level.
/// </summary>
internal enum LordFinancialCrisisLevel
{
    Stable,      // No problems, operating normally
    Strained,    // One issue (low gold OR small debt OR food tight)
    Critical,    // Multiple problems compounding
    Collapse     // Everything failing simultaneously
}
```

### Crisis Score Calculation

**Composite scoring (0-10 points):**

```csharp
private LordFinancialCrisisState CalculateLordFinancialCrisis()
{
    var state = new LordFinancialCrisisState();
    var clan = _enlistedLord?.Clan;
    var lordParty = _enlistedLord?.PartyBelongedTo;
    
    if (clan == null || lordParty == null)
        return state;  // Default safe state
    
    int crisisScore = 0;
    
    // Factor 1: Gold vs native thresholds (0-3 points)
    state.LordGold = _enlistedLord.Gold;
    state.WealthStatus = GetLordWealthStatus();  // Keep existing
    
    if (state.LordGold < 2000)
        crisisScore += 3;  // Can't pay leader party
    else if (state.LordGold < 4000)
        crisisScore += 2;  // Can't pay other parties
    else if (state.LordGold < 8000)
        crisisScore += 1;  // Can't pay garrisons
    
    // Factor 2: Native unpaid wages (0-2 points)
    state.HasUnpaidNativeTroops = lordParty.HasUnpaidWages > 0.0f;
    state.UnpaidWagesRatio = lordParty.HasUnpaidWages;
    
    if (state.UnpaidWagesRatio > 0.5f)
        crisisScore += 2;  // Severely underpaid
    else if (state.UnpaidWagesRatio > 0.0f)
        crisisScore += 1;  // Some wages missed
    
    // Factor 3: Kingdom debt (0-3 points)
    state.KingdomDebt = clan.DebtToKingdom;
    
    if (state.KingdomDebt > 5000)
        crisisScore += 3;  // Crushing debt
    else if (state.KingdomDebt > 1000)
        crisisScore += 2;  // Significant debt
    else if (state.KingdomDebt > 0)
        crisisScore += 1;  // Minor debt
    
    // Factor 4: Food crisis (0-2 points)
    state.DaysOfFoodRemaining = lordParty.GetNumDaysForFoodToLast();
    state.PartyTradeGold = lordParty.PartyTradeGold;
    state.CanAffordFood = state.PartyTradeGold >= 500;  // Rough threshold
    
    if (state.DaysOfFoodRemaining < 2 && !state.CanAffordFood)
        crisisScore += 2;  // Starvation imminent
    else if (state.DaysOfFoodRemaining < 5 && !state.CanAffordFood)
        crisisScore += 1;  // Food becoming critical
    
    // Factor 5: Recent desertion (0-1 point)
    // Note: Requires tracking previous party size
    // state.RecentDesertion = DetectRecentDesertion(lordParty);
    // if (state.RecentDesertion) crisisScore += 1;
    
    // Compute final score and level
    state.CrisisScore = Math.Min(10, crisisScore);
    state.CrisisLevel = state.CrisisScore switch
    {
        0 => LordFinancialCrisisLevel.Stable,
        1 or 2 => LordFinancialCrisisLevel.Strained,
        3 or 4 or 5 => LordFinancialCrisisLevel.Critical,
        _ => LordFinancialCrisisLevel.Collapse  // 6+
    };
    
    state.LastCalculated = CampaignTime.Now;
    return state;
}
```

### Impact on Pay Tension

**Accelerate PayTension based on crisis level:**

```csharp
// In ProcessEnlistedDailyService() after wage accrual
private void ApplyFinancialCrisisTension()
{
    var crisisState = CalculateLordFinancialCrisis();
    
    // Base tension increase when lord can't pay (existing system)
    // PLUS additional tension from crisis severity
    int bonusTension = crisisState.CrisisLevel switch
    {
        LordFinancialCrisisLevel.Stable => 0,
        LordFinancialCrisisLevel.Strained => 1,    // +1 tension/day
        LordFinancialCrisisLevel.Critical => 3,    // +3 tension/day
        LordFinancialCrisisLevel.Collapse => 5,    // +5 tension/day
        _ => 0
    };
    
    if (bonusTension > 0 && _payTension < 100)
    {
        _payTension = Math.Min(100, _payTension + bonusTension);
        ModLogger.Info("Pay", $"Crisis tension applied: +{bonusTension} " +
            $"(Level={crisisState.CrisisLevel}, Score={crisisState.CrisisScore})");
    }
}
```

### Backward Compatibility

**Keep existing APIs unchanged:**

```csharp
// EXISTING: Keep for backward compatibility
private LordWealthStatus GetLordWealthStatus()
{
    // Original implementation unchanged
}

private float GetLordWealthPayModifier()
{
    // Original implementation unchanged
}

// NEW: Enhanced API with crisis context
internal LordFinancialCrisisState GetLordFinancialCrisisState()
{
    return CalculateLordFinancialCrisis();
}

// NEW: Readable status for UI/logging
internal string GetCrisisStatusText()
{
    var state = GetLordFinancialCrisisState();
    return state.CrisisLevel switch
    {
        LordFinancialCrisisLevel.Stable => "Stable",
        LordFinancialCrisisLevel.Strained => "Strained",
        LordFinancialCrisisLevel.Critical => "Critical",
        LordFinancialCrisisLevel.Collapse => "Collapse",
        _ => "Unknown"
    };
}
```

---

## Implementation Phases

### Phase 0: Preparation (Research Complete) ✅

**Status:** Complete  
**Deliverables:**
- ✅ Native code analysis complete
- ✅ Integration points identified
- ✅ Architecture designed
- ✅ This document created

**Next:** Code implementation begins

---

### Phase 1: Infrastructure (Foundation)

**Goal:** Add crisis state tracking without changing behavior

**Tasks:**

1. **Add data structures** (EnlistmentBehavior.cs ~line 4653)
   ```csharp
   // Add after LordWealthStatus enum
   internal class LordFinancialCrisisState { /* ... */ }
   internal enum LordFinancialCrisisLevel { /* ... */ }
   ```

2. **Implement calculation method** (~line 4807)
   ```csharp
   private LordFinancialCrisisState CalculateLordFinancialCrisis()
   {
       // Implementation as designed above
   }
   ```

3. **Add daily tracking** (in ProcessEnlistedDailyService ~line 3835)
   ```csharp
   // After CheckNpcDesertionFromPayTension();
   
   // Track lord's financial crisis state
   if (_enlistedLord != null)
   {
       var crisisState = CalculateLordFinancialCrisis();
       
       // Log for Phase 1 observation
       if (crisisState.CrisisLevel >= LordFinancialCrisisLevel.Critical)
       {
           ModLogger.Info("LordCrisis", 
               $"Lord {_enlistedLord.Name} financial crisis detected: " +
               $"Level={crisisState.CrisisLevel}, Score={crisisState.CrisisScore}, " +
               $"Gold={crisisState.LordGold}, Debt={crisisState.KingdomDebt}, " +
               $"UnpaidWages={crisisState.UnpaidWagesRatio:F2}, " +
               $"FoodDays={crisisState.DaysOfFoodRemaining}");
       }
   }
   ```

4. **Add public accessors**
   ```csharp
   // Public API for other systems (events, UI)
   public LordFinancialCrisisLevel LordCrisisLevel => 
       GetLordFinancialCrisisState()?.CrisisLevel ?? LordFinancialCrisisLevel.Stable;
   
   public int LordCrisisScore => 
       GetLordFinancialCrisisState()?.CrisisScore ?? 0;
   ```

**Testing:**
- Enlist with various lords (rich/poor)
- Monitor logs for crisis state changes
- Verify native values read correctly
- Confirm no behavior changes (observation only)

**Success Criteria:**
- ✅ Crisis state calculates without errors
- ✅ Native values read correctly (gold, debt, unpaid wages, food)
- ✅ Logging shows crisis detection working
- ✅ No impact on existing pay system behavior
- ✅ No crashes or save corruption

**Duration:** 2-4 hours

---

### Phase 2: Crisis Tension (Behavior Integration)

**Goal:** Make crisis level affect pay tension escalation

**Tasks:**

1. **Implement crisis tension modifier** (~line 3836)
   ```csharp
   private void ApplyFinancialCrisisTension()
   {
       // Implementation as designed above
       // Adds 0/1/3/5 tension per day based on crisis level
   }
   ```

2. **Call in daily tick** (ProcessEnlistedDailyService)
   ```csharp
   // After existing pay tension processing
   ApplyFinancialCrisisTension();
   ```

3. **Update pay resolution logging** (~line 3883)
   ```csharp
   var crisisState = GetLordFinancialCrisisState();
   ModLogger.Info("Pay", $"Resolving pay muster: PendingPay={payout}, " +
       $"Backpay={_owedBackpay}, CanPayFull={canPayFull}, " +
       $"LordWealth={wealthStatus}, CrisisLevel={crisisState.CrisisLevel}");
   ```

4. **Add player notification** (when crisis escalates)
   ```csharp
   // When crisis level increases to Critical/Collapse
   if (crisisState.CrisisLevel >= LordFinancialCrisisLevel.Critical)
   {
       InformationManager.DisplayMessage(new InformationMessage(
           $"Lord {_enlistedLord.Name}'s financial situation is deteriorating.",
           Colors.Yellow));
   }
   ```

**Testing:**
- Find/create broke lord scenario (console commands or save editing)
- Verify tension increases faster during crisis
- Monitor pay delays and backpay accumulation
- Check existing pay tension events still fire correctly

**Success Criteria:**
- ✅ Tension increases faster when lord in crisis
- ✅ Crisis levels logged correctly
- ✅ Existing pay system still works
- ✅ Player sees crisis notifications
- ✅ No crashes

**Duration:** 2-3 hours

---

### Phase 3: Event Triggers (Content Hook)

**Goal:** Expose crisis state to Lance Life Events system

**Tasks:**

1. **Add trigger token support** (LanceLifeEventTriggerEvaluator.cs)
   ```csharp
   // Add to EvaluateCustomToken() method
   
   case CampaignTriggerTokens.LordCrisis:
       var enlistment = EnlistmentBehavior.Instance;
       if (enlistment?.IsEnlisted != true) return false;
       
       var crisisLevel = enlistment.LordCrisisLevel;
       
       // Format: lord_crisis:critical or lord_crisis:collapse
       if (token.Contains(":"))
       {
           var parts = token.Split(':');
           var requiredLevel = parts[1].ToLowerInvariant();
           
           return requiredLevel switch
           {
               "strained" => crisisLevel >= LordFinancialCrisisLevel.Strained,
               "critical" => crisisLevel >= LordFinancialCrisisLevel.Critical,
               "collapse" => crisisLevel >= LordFinancialCrisisLevel.Collapse,
               _ => false
           };
       }
       
       return crisisLevel >= LordFinancialCrisisLevel.Critical;
   ```

2. **Add trigger token constant** (CampaignTriggerTokens.cs)
   ```csharp
   public const string LordCrisis = "lord_crisis";
   public const string LordDebt = "lord_debt";  // Optional: debt-specific
   public const string LordStarving = "lord_starving";  // Optional: food-specific
   ```

3. **Document trigger usage** (Update event_metadata_index.md)
   ```markdown
   ### Financial Crisis Triggers
   
   | Token | Description | Example |
   |-------|-------------|---------|
   | `lord_crisis` | Lord in any crisis state | General crisis check |
   | `lord_crisis:strained` | Lord in Strained or worse | Minor problems |
   | `lord_crisis:critical` | Lord in Critical or worse | Serious problems |
   | `lord_crisis:collapse` | Lord in Collapse state | Everything failing |
   ```

**Testing:**
- Create test event with `lord_crisis:critical` trigger
- Verify event fires when crisis reaches Critical
- Verify event doesn't fire when Stable/Strained
- Check cooldowns and timing

**Success Criteria:**
- ✅ Trigger tokens evaluate correctly
- ✅ Events can check crisis state
- ✅ Documentation updated
- ✅ Integration test passes

**Duration:** 3-4 hours

---

### Phase 4: Content Creation (Optional - Stretch Goal)

**Goal:** Create 5 new pay tension events leveraging crisis context

**Quick Reference - 5 Complete Events Ready for Implementation:**

| Event ID | Title | Trigger | Tension | Description |
|----------|-------|---------|---------|-------------|
| `pay_crisis_troops_deserting` | Desertion in the Ranks | `lord_crisis:critical` | 40+ | Lord's native troops desert due to unpaid wages |
| `pay_crisis_debt_collectors` | Crown Collectors | `lord_crisis:critical` + settlement | 50+ | Kingdom debt collectors arrive demanding payment |
| `pay_crisis_rations` | Empty Larder | `lord_crisis:strained` + evening | 45+ | Food crisis - no supplies and no money to buy |
| `pay_crisis_collapse` | Everything Falls Apart | `lord_crisis:collapse` | 70+ | Complete systemic failure - compound crisis |
| `pay_crisis_confession` | Private Audience | `lord_crisis:critical` + night | 55+ | Lord confesses financial ruin privately (T3+) |

All events include:
- ✅ Complete JSON matching your schema
- ✅ 4-5 options per event with varied outcomes
- ✅ Placeholder names ({LANCE_LEADER_SHORT}, {VETERAN_1_NAME}, etc.)
- ✅ Risk/reward balance
- ✅ Multiple paths (loyal/corrupt/leave)
- ✅ Atmospheric military writing style

**Scroll down to see complete event definitions.**

---

**Event Examples:**

**Event 1: "Troops Deserting"**
- **Trigger:** `lord_crisis:critical` + `pay_tension_min: 40`
- **Context:** Lord's native troops are deserting due to unpaid wages
- **Options:**
  - Sympathize with deserters (LanceRep +5)
  - Criticize deserters (Discipline -5)
  - Help lord restore order (Lord relation +10, Fatigue -5)

**Event 2: "Kingdom Debt Collectors"**
- **Trigger:** `lord_debt:high` (debt > 5000) + `pay_tension_min: 50`
- **Context:** Kingdom officials arrive to collect lord's debt
- **Options:**
  - Loan lord money (Gold -1000, Lord relation +15)
  - Offer to work off debt (Duty mission unlocked)
  - Stay out of it (No effect)

**Event 3: "Ration Crisis"**
- **Trigger:** `lord_starving` (food < 3 days, gold < 500) + `pay_tension_min: 60`
- **Context:** Party is out of food and lord can't buy more
- **Options:**
  - Share your personal supplies (Gold -200, Lord relation +10)
  - Forage for the party (Time -4 hours, Food +5)
  - Demand pay first (PayTension +10, confrontation)

**Event 4: "Compound Crisis"**
- **Trigger:** `lord_crisis:collapse` + `pay_tension_min: 70`
- **Context:** Everything is failing - no food, no pay, troops deserting, debt mounting
- **Options:**
  - Loyal stand ("We'll get through this") - Lord relation +20
  - Free desertion option unlocked
  - Mutiny option (if enough lance support)

**Event 5: "Kingdom Intervention"**
- **Trigger:** `lord_crisis:collapse` + kingdom not at war
- **Context:** Kingdom threatens to strip lord's fiefs due to financial mismanagement
- **Options:**
  - Defend lord to kingdom officials (Diplomacy check)
  - Suggest transferring to another lord (Transfer system)
  - Remain silent (No effect)

**Complete Event Definitions:**

Below are 5 complete events ready for implementation in `events_pay_crisis.json`:

---

### Event 1: Troops Deserting

```json
{
  "id": "pay_crisis_troops_deserting",
  "category": "pay",
  "metadata": {
    "tier_range": { "min": 1, "max": 9 },
    "content_doc": "docs/research/lord_financial_crisis_implementation.md"
  },
  "delivery": {
    "method": "automatic",
    "channel": "inquiry",
    "incident_trigger": "DailyTick",
    "menu": null,
    "menu_section": null
  },
  "triggers": {
    "all": ["is_enlisted", "lord_crisis:critical"],
    "any": [],
    "time_of_day": [],
    "escalation_requirements": {
      "pay_tension_min": 40
    }
  },
  "requirements": {
    "duty": "any",
    "formation": "any",
    "tier": { "min": 1, "max": 9 }
  },
  "timing": {
    "cooldown_days": 14,
    "priority": "high",
    "one_time": false,
    "rate_limit": { "max_per_week": 1, "category_cooldown_days": 0 }
  },
  "content": {
    "titleId": "ll_evt_crisis_deserting_title",
    "setupId": "ll_evt_crisis_deserting_setup",
    "title": "Desertion in the Ranks",
    "setup": "Word spreads through camp: three men from the second lance gone during the night. Not dead. Just... gone.\n\n{VETERAN_1_NAME} spits. \"Can't blame them. {LORD_NAME} hasn't paid his own troops in weeks. They're starving.\"\n\n{LANCE_LEADER_SHORT} looks grim. \"It'll spread. Desperate men make desperate choices.\"\n\nThe lord's party is falling apart.",
    "options": [
      {
        "id": "sympathize",
        "textId": "ll_evt_crisis_deserting_opt_sympathize",
        "text": "\"Can't blame them\"",
        "tooltip": "Show understanding",
        "condition": null,
        "risk": "safe",
        "risk_chance": null,
        "costs": { "fatigue": 0, "gold": 0, "time_hours": 0 },
        "rewards": { "xp": {}, "gold": 0, "relation": {}, "items": [] },
        "effects": { "heat": 0, "discipline": 0, "lance_reputation": 5, "medical_risk": 0, "fatigue_relief": 0 },
        "flags_set": [],
        "flags_clear": [],
        "resultTextId": "ll_evt_crisis_deserting_opt_sympathize_outcome",
        "outcome": "Heads nod around the fire. Everyone's thinking the same thing. How long until we're next?"
      },
      {
        "id": "criticize",
        "textId": "ll_evt_crisis_deserting_opt_criticize",
        "text": "\"They broke their oath\"",
        "tooltip": "Condemn deserters",
        "condition": null,
        "risk": "safe",
        "risk_chance": null,
        "costs": { "fatigue": 0, "gold": 0, "time_hours": 0 },
        "rewards": { "xp": {}, "gold": 0, "relation": { "lord": 5 }, "items": [] },
        "effects": { "heat": 0, "discipline": 0, "lance_reputation": -8, "medical_risk": 0, "fatigue_relief": 0 },
        "flags_set": [],
        "flags_clear": [],
        "resultTextId": "ll_evt_crisis_deserting_opt_criticize_outcome",
        "outcome": "\"Easy to say,\" {VETERAN_1_NAME} mutters, \"when you're not the one starving.\"\n\nThe fire crackles. Nobody meets your eyes."
      },
      {
        "id": "help_lord",
        "textId": "ll_evt_crisis_deserting_opt_help",
        "text": "\"I'll talk to {LORD_NAME}\"",
        "tooltip": "Try to help stabilize the situation",
        "condition": null,
        "risk": "risky",
        "risk_chance": 0.40,
        "costs": { "fatigue": 2, "gold": 0, "time_hours": 2 },
        "rewards": { "xp": { "leadership": 20 }, "gold": 0, "relation": { "lord": 15 }, "items": [] },
        "effects": { "heat": 0, "discipline": 0, "lance_reputation": 0, "medical_risk": 0, "fatigue_relief": 0 },
        "flags_set": ["crisis_tried_to_help"],
        "flags_clear": [],
        "resultTextId": "ll_evt_crisis_deserting_opt_help_outcome",
        "outcome": "{LORD_NAME} looks haggard. \"I know. I'm trying. The kingdom's taking everything. Tributes, levies, debts... there's nothing left.\"\n\nYou see the weight he carries. It doesn't make the hunger easier.",
        "outcome_failure": "{LORD_NAME} won't see you. His steward bars the tent.\n\n\"The lord has... more pressing matters.\" Translation: you're not important enough to bother."
      },
      {
        "id": "plan_own_leave",
        "textId": "ll_evt_crisis_deserting_opt_plan",
        "text": "Consider your own options",
        "tooltip": "Start thinking about leaving",
        "condition": null,
        "risk": "safe",
        "risk_chance": null,
        "costs": { "fatigue": 0, "gold": 0, "time_hours": 0 },
        "rewards": { "xp": {}, "gold": 0, "relation": {}, "items": [] },
        "effects": { "heat": 0, "discipline": 0, "lance_reputation": 0, "medical_risk": 0, "fatigue_relief": 0 },
        "flags_set": ["considering_desertion"],
        "flags_clear": [],
        "resultTextId": "ll_evt_crisis_deserting_opt_plan_outcome",
        "outcome": "Three men gone. How many more before the whole thing collapses?\n\nYou start planning your own exit."
      }
    ]
  }
}
```

---

### Event 2: Kingdom Debt Collectors

```json
{
  "id": "pay_crisis_debt_collectors",
  "category": "pay",
  "metadata": {
    "tier_range": { "min": 1, "max": 9 },
    "content_doc": "docs/research/lord_financial_crisis_implementation.md"
  },
  "delivery": {
    "method": "automatic",
    "channel": "inquiry",
    "incident_trigger": "Settlement",
    "menu": null,
    "menu_section": null
  },
  "triggers": {
    "all": ["is_enlisted", "lord_crisis:critical", "in_settlement"],
    "any": [],
    "time_of_day": [],
    "escalation_requirements": {
      "pay_tension_min": 50
    }
  },
  "requirements": {
    "duty": "any",
    "formation": "any",
    "tier": { "min": 1, "max": 9 }
  },
  "timing": {
    "cooldown_days": 21,
    "priority": "high",
    "one_time": false,
    "rate_limit": { "max_per_week": 1, "category_cooldown_days": 0 }
  },
  "content": {
    "titleId": "ll_evt_crisis_debt_title",
    "setupId": "ll_evt_crisis_debt_setup",
    "title": "Crown Collectors",
    "setup": "Officials in royal livery arrive at the keep. Not tax collectors — debt collectors.\n\n{LANCE_LEADER_SHORT} watches from the yard. \"Kingdom's calling in what {LORD_NAME} owes. Five thousand denars. Maybe more.\"\n\n{VETERAN_1_NAME} laughs, bitter. \"He can't pay us. How's he going to pay them?\"\n\nVoices rise from the keep. Angry. Desperate.",
    "options": [
      {
        "id": "loan_lord",
        "textId": "ll_evt_crisis_debt_opt_loan",
        "text": "Loan {LORD_NAME} the money",
        "tooltip": "You'll probably never see it again",
        "condition": "gold_min:1000",
        "risk": "safe",
        "risk_chance": null,
        "costs": { "fatigue": 0, "gold": 1000, "time_hours": 0 },
        "rewards": { "xp": {}, "gold": 0, "relation": { "lord": 25 }, "items": [] },
        "effects": { "heat": 0, "discipline": 0, "lance_reputation": 0, "medical_risk": 0, "fatigue_relief": 0 },
        "flags_set": ["loaned_lord_money"],
        "flags_clear": [],
        "resultTextId": "ll_evt_crisis_debt_opt_loan_outcome",
        "outcome": "{LORD_NAME} takes your coin purse with trembling hands.\n\n\"I won't forget this.\" He might not. But you'll probably never see that gold again."
      },
      {
        "id": "work_off_debt",
        "textId": "ll_evt_crisis_debt_opt_work",
        "text": "\"I'll work off part of the debt\"",
        "tooltip": "Offer your service to reduce the burden",
        "condition": null,
        "risk": "safe",
        "risk_chance": null,
        "costs": { "fatigue": 3, "gold": 0, "time_hours": 8 },
        "rewards": { "xp": { "trade": 20, "charm": 15 }, "gold": 0, "relation": { "lord": 15 }, "items": [] },
        "effects": { "heat": 0, "discipline": 0, "lance_reputation": 5, "medical_risk": 0, "fatigue_relief": 0 },
        "flags_set": ["helped_with_debt"],
        "flags_clear": [],
        "resultTextId": "ll_evt_crisis_debt_opt_work_outcome",
        "outcome": "You spend the day running errands, negotiating, promising future payments. It buys {LORD_NAME} some time.\n\nNot much. But time enough."
      },
      {
        "id": "watch_quietly",
        "textId": "ll_evt_crisis_debt_opt_watch",
        "text": "Stay out of it",
        "tooltip": "Not your problem",
        "condition": null,
        "risk": "safe",
        "risk_chance": null,
        "costs": { "fatigue": 0, "gold": 0, "time_hours": 0 },
        "rewards": { "xp": {}, "gold": 0, "relation": {}, "items": [] },
        "effects": { "heat": 0, "discipline": 0, "lance_reputation": 0, "medical_risk": 0, "fatigue_relief": 0 },
        "flags_set": [],
        "flags_clear": [],
        "resultTextId": "ll_evt_crisis_debt_opt_watch_outcome",
        "outcome": "The collectors leave with promises of payment. Everyone knows the promises are empty.\n\nThey'll be back. With soldiers next time."
      },
      {
        "id": "suggest_transfer",
        "textId": "ll_evt_crisis_debt_opt_transfer",
        "text": "Privately consider switching lords",
        "tooltip": "This ship is sinking",
        "condition": null,
        "risk": "safe",
        "risk_chance": null,
        "costs": { "fatigue": 0, "gold": 0, "time_hours": 0 },
        "rewards": { "xp": {}, "gold": 0, "relation": {}, "items": [] },
        "effects": { "heat": 0, "discipline": 0, "lance_reputation": 0, "medical_risk": 0, "fatigue_relief": 0 },
        "flags_set": ["considering_transfer"],
        "flags_clear": [],
        "resultTextId": "ll_evt_crisis_debt_opt_transfer_outcome",
        "outcome": "You can't shake the thought: there are other lords. Richer lords. Lords who pay.\n\nThis one's drowning. Do you stay and drown with him?"
      }
    ]
  }
}
```

---

### Event 3: Ration Crisis

```json
{
  "id": "pay_crisis_rations",
  "category": "pay",
  "metadata": {
    "tier_range": { "min": 1, "max": 9 },
    "content_doc": "docs/research/lord_financial_crisis_implementation.md"
  },
  "delivery": {
    "method": "automatic",
    "channel": "inquiry",
    "incident_trigger": "DailyTick",
    "menu": null,
    "menu_section": null
  },
  "triggers": {
    "all": ["is_enlisted", "lord_crisis:strained"],
    "any": [],
    "time_of_day": ["evening"],
    "escalation_requirements": {
      "pay_tension_min": 45
    }
  },
  "requirements": {
    "duty": "any",
    "formation": "any",
    "tier": { "min": 1, "max": 9 }
  },
  "timing": {
    "cooldown_days": 10,
    "priority": "normal",
    "one_time": false,
    "rate_limit": { "max_per_week": 1, "category_cooldown_days": 0 }
  },
  "content": {
    "titleId": "ll_evt_crisis_rations_title",
    "setupId": "ll_evt_crisis_rations_setup",
    "title": "Empty Larder",
    "setup": "The quartermaster stands at the supply wagon. Empty hands. Empty promises.\n\n\"Half rations tonight. Maybe nothing tomorrow.\" He won't meet anyone's eyes. \"Lord's got no coin for food. We're three days from the nearest town.\"\n\n{LANCE_MATE_NAME} throws down their bowl. \"So we starve?\"\n\nHungry soldiers make dangerous decisions.",
    "options": [
      {
        "id": "share_supplies",
        "textId": "ll_evt_crisis_rations_opt_share",
        "text": "Share your personal supplies",
        "tooltip": "Cost you gold, but it helps",
        "condition": "gold_min:150",
        "risk": "safe",
        "risk_chance": null,
        "costs": { "fatigue": 0, "gold": 150, "time_hours": 0 },
        "rewards": { "xp": {}, "gold": 0, "relation": { "lord": 8 }, "items": [] },
        "effects": { "heat": 0, "discipline": 0, "lance_reputation": 15, "medical_risk": 0, "fatigue_relief": 0 },
        "flags_set": [],
        "flags_clear": [],
        "resultTextId": "ll_evt_crisis_rations_opt_share_outcome",
        "outcome": "You break out your emergency rations. Dried meat. Hard bread. Not much, but it's something.\n\n{VETERAN_1_NAME} nods. \"You're one of us.\""
      },
      {
        "id": "forage",
        "textId": "ll_evt_crisis_rations_opt_forage",
        "text": "Organize a foraging party",
        "tooltip": "Supplement supplies from the land",
        "condition": null,
        "risk": "risky",
        "risk_chance": 0.25,
        "costs": { "fatigue": 2, "gold": 0, "time_hours": 4 },
        "rewards": { "xp": { "scouting": 20 }, "gold": 0, "relation": {}, "items": ["food_grain"] },
        "effects": { "heat": 0, "discipline": 0, "lance_reputation": 10, "medical_risk": 0, "fatigue_relief": 0 },
        "flags_set": [],
        "flags_clear": [],
        "resultTextId": "ll_evt_crisis_rations_opt_forage_outcome",
        "outcome": "You lead a party into the hills. Mushrooms, wild onions, a rabbit or two.\n\nNot much. But it keeps bellies from growling through the night.",
        "outcome_failure": "Hours searching yields nothing. The land's as empty as the supply wagon.\n\nYou return exhausted, still hungry."
      },
      {
        "id": "demand_pay_first",
        "textId": "ll_evt_crisis_rations_opt_demand",
        "text": "\"Pay us first, then we talk about loyalty\"",
        "tooltip": "Confrontational",
        "condition": null,
        "risk": "safe",
        "risk_chance": null,
        "costs": { "fatigue": 0, "gold": 0, "time_hours": 0 },
        "rewards": { "xp": {}, "gold": 0, "relation": { "lord": -10 }, "items": [] },
        "effects": { "heat": 0, "discipline": 2, "lance_reputation": 5, "medical_risk": 0, "fatigue_relief": 0 },
        "flags_set": ["crisis_demanded_payment"],
        "flags_clear": [],
        "resultTextId": "ll_evt_crisis_rations_opt_demand_outcome",
        "outcome": "The words hang in the cold air. Others nod. Someone had to say it.\n\n{LANCE_LEADER_SHORT} looks uncomfortable. \"Careful. That sounds like mutiny talk.\""
      },
      {
        "id": "endure",
        "textId": "ll_evt_crisis_rations_opt_endure",
        "text": "\"We've been hungry before\"",
        "tooltip": "Soldier on",
        "condition": null,
        "risk": "safe",
        "risk_chance": null,
        "costs": { "fatigue": 1, "gold": 0, "time_hours": 0 },
        "rewards": { "xp": {}, "gold": 0, "relation": {}, "items": [] },
        "effects": { "heat": 0, "discipline": 0, "lance_reputation": 0, "medical_risk": 1, "fatigue_relief": 0 },
        "flags_set": [],
        "flags_clear": [],
        "resultTextId": "ll_evt_crisis_rations_opt_endure_outcome",
        "outcome": "You tighten your belt another notch. You've marched on empty before. You'll manage.\n\nFor a few more days, at least."
      }
    ]
  }
}
```

---

### Event 4: Compound Crisis

```json
{
  "id": "pay_crisis_collapse",
  "category": "pay",
  "metadata": {
    "tier_range": { "min": 1, "max": 9 },
    "content_doc": "docs/research/lord_financial_crisis_implementation.md"
  },
  "delivery": {
    "method": "automatic",
    "channel": "inquiry",
    "incident_trigger": "DailyTick",
    "menu": null,
    "menu_section": null
  },
  "triggers": {
    "all": ["is_enlisted", "lord_crisis:collapse"],
    "any": [],
    "time_of_day": [],
    "escalation_requirements": {
      "pay_tension_min": 70
    }
  },
  "requirements": {
    "duty": "any",
    "formation": "any",
    "tier": { "min": 1, "max": 9 }
  },
  "timing": {
    "cooldown_days": 21,
    "priority": "urgent",
    "one_time": false,
    "rate_limit": { "max_per_week": 1, "category_cooldown_days": 0 }
  },
  "content": {
    "titleId": "ll_evt_crisis_collapse_title",
    "setupId": "ll_evt_crisis_collapse_setup",
    "title": "Everything Falls Apart",
    "setup": "It's all coming down at once.\n\nNo pay. No food. Kingdom debt collectors threatening seizure of {LORD_NAME}'s lands. More desertions every night — seven men gone this week alone.\n\n{LANCE_LEADER_SHORT} gathers the lance. \"The party's falling apart. Kingdom's about to strip his title. We're unpaid, unfed, and the enemy knows we're weak.\"\n\n{VETERAN_1_NAME} meets your eyes. \"Time to choose. Stay and go down with the ship, or get out while we can?\"",
    "options": [
      {
        "id": "loyal_stand",
        "textId": "ll_evt_crisis_collapse_opt_loyal",
        "text": "\"We don't abandon our own\"",
        "tooltip": "Stand by {LORD_NAME}",
        "condition": null,
        "risk": "safe",
        "risk_chance": null,
        "costs": { "fatigue": 0, "gold": 0, "time_hours": 0 },
        "rewards": { "xp": { "leadership": 25 }, "gold": 0, "relation": { "lord": 30 }, "items": [] },
        "effects": { "heat": 0, "discipline": 0, "lance_reputation": 20, "medical_risk": 0, "fatigue_relief": 0 },
        "flags_set": ["crisis_stood_loyal"],
        "flags_clear": [],
        "resultTextId": "ll_evt_crisis_collapse_opt_loyal_outcome",
        "outcome": "\"We took an oath,\" you say. \"Doesn't matter if the pay's late. Doesn't matter if the larder's empty. We're soldiers.\"\n\n{VETERAN_1_NAME} nods slowly. \"Alright. We stay. But {LORD_NAME} better remember this.\""
      },
      {
        "id": "free_desertion",
        "textId": "ll_evt_crisis_collapse_opt_leave",
        "text": "Leave without penalty",
        "tooltip": "Clean break — no one blames you",
        "condition": null,
        "risk": "safe",
        "risk_chance": null,
        "costs": { "fatigue": 0, "gold": 0, "time_hours": 0 },
        "rewards": { "xp": {}, "gold": 0, "relation": { "lord": -5 }, "items": [] },
        "effects": { "heat": 0, "discipline": 0, "lance_reputation": 0, "medical_risk": 0, "fatigue_relief": 0 },
        "flags_set": ["left_during_crisis"],
        "flags_clear": [],
        "resultTextId": "ll_evt_crisis_collapse_opt_leave_outcome",
        "outcome": "You pack your kit. {LORD_NAME} sees you go, nods once. He understands.\n\nNo anger. No punishment. Just the acknowledgment that you're doing what you have to do.\n\nYou walk away from the dying camp. Others will follow."
      },
      {
        "id": "organize_mutiny",
        "textId": "ll_evt_crisis_collapse_opt_mutiny",
        "text": "\"Time for new leadership\"",
        "tooltip": "Dangerous. Revolutionary.",
        "condition": "lance_reputation_min:30",
        "risk": "risky",
        "risk_chance": 0.50,
        "costs": { "fatigue": 3, "gold": 0, "time_hours": 0 },
        "rewards": { "xp": { "leadership": 30 }, "gold": 0, "relation": { "lord": -50 }, "items": [] },
        "effects": { "heat": 5, "discipline": 5, "lance_reputation": 30, "medical_risk": 0, "fatigue_relief": 0 },
        "flags_set": ["attempted_mutiny"],
        "flags_clear": [],
        "resultTextId": "ll_evt_crisis_collapse_opt_mutiny_outcome",
        "outcome": "The words ignite the tinder. {VETERAN_1_NAME} grabs a torch. {LANCE_MATE_NAME} draws steel.\n\n\"We're taking what we're owed!\" someone shouts.\n\nBy dawn, {LORD_NAME} is under guard and the army is yours. For better or worse.",
        "outcome_failure": "{LANCE_LEADER_SHORT} draws steel on you first.\n\n\"You want mutiny? Try me.\" The moment breaks. The others stand down. You've lost them."
      },
      {
        "id": "seek_transfer",
        "textId": "ll_evt_crisis_collapse_opt_transfer",
        "text": "Request transfer to another lord",
        "tooltip": "Official route",
        "condition": null,
        "risk": "safe",
        "risk_chance": null,
        "costs": { "fatigue": 1, "gold": 0, "time_hours": 2 },
        "rewards": { "xp": { "charm": 15 }, "gold": 0, "relation": { "lord": -10 }, "items": [] },
        "effects": { "heat": 0, "discipline": 0, "lance_reputation": -5, "medical_risk": 0, "fatigue_relief": 0 },
        "flags_set": ["requested_transfer"],
        "flags_clear": [],
        "resultTextId": "ll_evt_crisis_collapse_opt_transfer_outcome",
        "outcome": "{LORD_NAME} signs the release. His hand shakes.\n\n\"I understand. Find a lord who can pay you. I... I can't anymore.\"\n\nHe's already broken. This just makes it official."
      }
    ]
  }
}
```

---

### Event 5: Lord's Confession

```json
{
  "id": "pay_crisis_confession",
  "category": "pay",
  "metadata": {
    "tier_range": { "min": 3, "max": 9 },
    "content_doc": "docs/research/lord_financial_crisis_implementation.md"
  },
  "delivery": {
    "method": "automatic",
    "channel": "inquiry",
    "incident_trigger": "Settlement",
    "menu": null,
    "menu_section": null
  },
  "triggers": {
    "all": ["is_enlisted", "lord_crisis:critical", "in_settlement"],
    "any": [],
    "time_of_day": ["night"],
    "escalation_requirements": {
      "pay_tension_min": 55
    }
  },
  "requirements": {
    "duty": "any",
    "formation": "any",
    "tier": { "min": 3, "max": 9 }
  },
  "timing": {
    "cooldown_days": 28,
    "priority": "normal",
    "one_time": false,
    "rate_limit": { "max_per_week": 1, "category_cooldown_days": 0 }
  },
  "content": {
    "titleId": "ll_evt_crisis_confession_title",
    "setupId": "ll_evt_crisis_confession_setup",
    "title": "Private Audience",
    "setup": "{LORD_NAME} calls you to his quarters. Late. Private.\n\nHe looks older than you remember. Haggard. The wine cup trembles in his hand.\n\n\"You're a veteran. You understand numbers.\" He slides a ledger across the table. Red ink everywhere. \"The kingdom takes forty percent in tribute. Mercenaries cost another thirty. Debt payments... I'm underwater. Drowning.\"\n\nHe meets your eyes. \"I can't pay you. Can't pay anyone. But I need soldiers who'll stay anyway.\"",
    "options": [
      {
        "id": "promise_loyalty",
        "textId": "ll_evt_crisis_confession_opt_promise",
        "text": "\"I'll stay\"",
        "tooltip": "Commit to the sinking ship",
        "condition": null,
        "risk": "safe",
        "risk_chance": null,
        "costs": { "fatigue": 0, "gold": 0, "time_hours": 0 },
        "rewards": { "xp": {}, "gold": 0, "relation": { "lord": 35 }, "items": [] },
        "effects": { "heat": 0, "discipline": 0, "lance_reputation": 0, "medical_risk": 0, "fatigue_relief": 0 },
        "flags_set": ["promised_crisis_loyalty"],
        "flags_clear": [],
        "resultTextId": "ll_evt_crisis_confession_opt_promise_outcome",
        "outcome": "{LORD_NAME} grips your shoulder. His eyes shine.\n\n\"I won't forget this. When things turn around — and they will — you'll be rewarded.\"\n\nIf things turn around."
      },
      {
        "id": "conditional_stay",
        "textId": "ll_evt_crisis_confession_opt_conditional",
        "text": "\"Show me a plan\"",
        "tooltip": "Stay if there's hope",
        "condition": null,
        "risk": "safe",
        "risk_chance": null,
        "costs": { "fatigue": 1, "gold": 0, "time_hours": 2 },
        "rewards": { "xp": { "trade": 20, "steward": 15 }, "gold": 0, "relation": { "lord": 15 }, "items": [] },
        "effects": { "heat": 0, "discipline": 0, "lance_reputation": 0, "medical_risk": 0, "fatigue_relief": 0 },
        "flags_set": ["demanded_plan"],
        "flags_clear": [],
        "resultTextId": "ll_evt_crisis_confession_opt_conditional_outcome",
        "outcome": "You spend hours going through the books. Cutting costs. Finding income.\n\nIt's not enough. But it's something. Maybe {LORD_NAME} can claw his way out of this hole. Maybe."
      },
      {
        "id": "honest_answer",
        "textId": "ll_evt_crisis_confession_opt_honest",
        "text": "\"I need to think about it\"",
        "tooltip": "Don't commit yet",
        "condition": null,
        "risk": "safe",
        "risk_chance": null,
        "costs": { "fatigue": 0, "gold": 0, "time_hours": 0 },
        "rewards": { "xp": {}, "gold": 0, "relation": {}, "items": [] },
        "effects": { "heat": 0, "discipline": 0, "lance_reputation": 0, "medical_risk": 0, "fatigue_relief": 0 },
        "flags_set": ["considering_options"],
        "flags_clear": [],
        "resultTextId": "ll_evt_crisis_confession_opt_honest_outcome",
        "outcome": "{LORD_NAME} nods. Disappointment, but understanding.\n\n\"Think fast. Time's running out.\"\n\nFor him. For all of you."
      },
      {
        "id": "ask_release",
        "textId": "ll_evt_crisis_confession_opt_release",
        "text": "\"Release me from my contract\"",
        "tooltip": "Honest, direct",
        "condition": null,
        "risk": "safe",
        "risk_chance": null,
        "costs": { "fatigue": 0, "gold": 0, "time_hours": 0 },
        "rewards": { "xp": {}, "gold": 0, "relation": { "lord": -15 }, "items": [] },
        "effects": { "heat": 0, "discipline": 0, "lance_reputation": 0, "medical_risk": 0, "fatigue_relief": 0 },
        "flags_set": ["requested_release"],
        "flags_clear": [],
        "resultTextId": "ll_evt_crisis_confession_opt_release_outcome",
        "outcome": "He's quiet for a long moment. Then reaches for the seal.\n\n\"Granted. I can't hold you to an oath I can't honor.\"\n\nHe signs your discharge. No anger. Just resignation."
      }
    ]
  }
}
```

**Content Tasks:**
1. ✅ Write 5 event scripts (Complete - see Event Definitions above)
2. Create JSON file `ModuleData/Enlisted/Events/events_pay_crisis.json`
3. Add localization strings to `enlisted_strings.xml` (IDs provided in events)
4. Playtest events in various crisis scenarios
5. Balance costs/rewards

**Complete File Structure:**

Create new file: `ModuleData/Enlisted/Events/events_pay_crisis.json`

```json
{
  "schemaVersion": 1,
  "packId": "pay_crisis",
  "category": "pay",
  "events": [
    // Copy Event 1: Troops Deserting here
    // Copy Event 2: Kingdom Debt Collectors here
    // Copy Event 3: Ration Crisis here
    // Copy Event 4: Compound Crisis here
    // Copy Event 5: Lord's Confession here
  ]
}
```

All 5 complete event definitions are provided in Phase 4 above with full schema compliance.

**Success Criteria:**
- ✅ Events fire at appropriate crisis levels
- ✅ Narrative context matches crisis state
- ✅ Options feel meaningful and distinct
- ✅ No bugs or crashes
- ✅ Player feedback positive

**Duration:** 8-12 hours (writing + testing)

**Localization Strings Required:**

Add to `ModuleData/Languages/enlisted_strings.xml`:

```xml
<!-- Lord Financial Crisis Events -->

<!-- Event 1: Troops Deserting -->
<string id="ll_evt_crisis_deserting_title" text="Desertion in the Ranks" />
<string id="ll_evt_crisis_deserting_setup" text="[Provided in JSON]" />
<string id="ll_evt_crisis_deserting_opt_sympathize" text="&quot;Can't blame them&quot;" />
<string id="ll_evt_crisis_deserting_opt_sympathize_outcome" text="[Provided in JSON]" />
<string id="ll_evt_crisis_deserting_opt_criticize" text="&quot;They broke their oath&quot;" />
<string id="ll_evt_crisis_deserting_opt_criticize_outcome" text="[Provided in JSON]" />
<string id="ll_evt_crisis_deserting_opt_help" text="&quot;I'll talk to {LORD_NAME}&quot;" />
<string id="ll_evt_crisis_deserting_opt_help_outcome" text="[Provided in JSON]" />
<string id="ll_evt_crisis_deserting_opt_plan" text="Consider your own options" />
<string id="ll_evt_crisis_deserting_opt_plan_outcome" text="[Provided in JSON]" />

<!-- Event 2: Kingdom Debt Collectors -->
<string id="ll_evt_crisis_debt_title" text="Crown Collectors" />
<string id="ll_evt_crisis_debt_setup" text="[Provided in JSON]" />
<!-- ... etc for all options ... -->

<!-- Event 3: Ration Crisis -->
<string id="ll_evt_crisis_rations_title" text="Empty Larder" />
<string id="ll_evt_crisis_rations_setup" text="[Provided in JSON]" />
<!-- ... etc for all options ... -->

<!-- Event 4: Compound Crisis -->
<string id="ll_evt_crisis_collapse_title" text="Everything Falls Apart" />
<string id="ll_evt_crisis_collapse_setup" text="[Provided in JSON]" />
<!-- ... etc for all options ... -->

<!-- Event 5: Lord's Confession -->
<string id="ll_evt_crisis_confession_title" text="Private Audience" />
<string id="ll_evt_crisis_confession_setup" text="[Provided in JSON]" />
<!-- ... etc for all options ... -->
```

**Note:** All text content is provided inline in the JSON for these events (using `text` field). The `textId` fields are included for future localization support but are optional in Phase 4. The events will work with just the inline text.

**Note:** Phase 4 is optional. System is fully functional without new events - existing pay tension events will naturally occur more frequently during crises.

---

### Phase 5: Polish & Documentation

**Goal:** Finalize system with UI indicators and documentation

**Tasks:**

1. **Add crisis indicator to Enlisted Status UI**
   ```csharp
   // In EnlistedMenuBehavior status display
   if (enlistment.LordCrisisLevel >= LordFinancialCrisisLevel.Critical)
   {
       statusText += $"\n[Lord Financial Status]: {enlistment.GetCrisisStatusText()}";
   }
   ```

2. **Update pay-system.md documentation**
   - Add Lord Financial Crisis section
   - Document crisis levels and effects
   - Add troubleshooting guide

3. **Create player-facing documentation**
   - Update README with crisis system overview
   - Add to feature list

4. **Performance check**
   - Profile daily tick impact (should be negligible)
   - Verify no memory leaks

**Testing:**
- Full playthrough (20+ in-game days)
- Multiple lords (rich and poor)
- Various crisis scenarios
- Save/load stability

**Success Criteria:**
- ✅ UI shows crisis status
- ✅ Documentation complete
- ✅ Performance acceptable (<1ms per daily tick)
- ✅ No save corruption
- ✅ Ready for release

**Duration:** 3-4 hours

---

## Integration Points

### 1. EnlistmentBehavior.cs

**Primary integration location.**

**Add fields (around line 370):**
```csharp
// Lord financial crisis tracking (Phase 2 financial system)
private LordFinancialCrisisState _lordCrisisState;
private int _lordCrisisCheckCooldownDays = 1;  // Check daily
```

**Add enums/classes (around line 4653):**
```csharp
internal enum LordFinancialCrisisLevel { Stable, Strained, Critical, Collapse }
internal class LordFinancialCrisisState { /* ... */ }
```

**Add methods (around line 4807):**
```csharp
private LordFinancialCrisisState CalculateLordFinancialCrisis() { /* ... */ }
internal LordFinancialCrisisState GetLordFinancialCrisisState() { /* ... */ }
internal string GetCrisisStatusText() { /* ... */ }
private void ApplyFinancialCrisisTension() { /* ... */ }
```

**Modify daily tick (line 3835):**
```csharp
// After CheckNpcDesertionFromPayTension();
ApplyFinancialCrisisTension();
```

### 2. LanceLifeEventTriggerEvaluator.cs

**Add trigger token support.**

**Location:** `EvaluateCustomToken()` method (around line 350)

**Add cases:**
```csharp
case CampaignTriggerTokens.LordCrisis:
    return EvaluateLordCrisisTrigger(token);

case CampaignTriggerTokens.LordDebt:
    return EvaluateLordDebtTrigger(token);

case CampaignTriggerTokens.LordStarving:
    return EvaluateLordStarvingTrigger(token);
```

**Add helper methods:**
```csharp
private bool EvaluateLordCrisisTrigger(string token)
{
    var enlistment = EnlistmentBehavior.Instance;
    if (enlistment?.IsEnlisted != true) return false;
    
    var crisisLevel = enlistment.LordCrisisLevel;
    
    // Parse "lord_crisis:critical" format
    if (token.Contains(":"))
    {
        var parts = token.Split(':');
        var requiredLevel = parts[1].ToLowerInvariant();
        
        return requiredLevel switch
        {
            "strained" => crisisLevel >= LordFinancialCrisisLevel.Strained,
            "critical" => crisisLevel >= LordFinancialCrisisLevel.Critical,
            "collapse" => crisisLevel >= LordFinancialCrisisLevel.Collapse,
            _ => false
        };
    }
    
    // Default: any crisis state
    return crisisLevel >= LordFinancialCrisisLevel.Critical;
}
```

### 3. CampaignTriggerTokens.cs

**Add token constants.**

**Location:** Add to existing constants

```csharp
// Lord financial crisis tokens (Phase 2)
public const string LordCrisis = "lord_crisis";
public const string LordDebt = "lord_debt";
public const string LordStarving = "lord_starving";
```

### 4. Event JSON Files (Optional - Phase 4)

**Create new file or add to existing:**
`ModuleData/Enlisted/Events/events_pay_crisis.json`

**Schema additions:**
```json
{
  "triggers": {
    "all": ["is_enlisted", "lord_crisis:critical"],
    "escalation_requirements": {
      "pay_tension_min": 40
    }
  }
}
```

---

## Event Schema Extensions

### New Trigger Requirements

**Add to event JSON schema:**

```json
{
  "triggers": {
    "all": ["is_enlisted"],
    "lord_financial_state": {
      "crisis_level_min": "critical",    // NEW: stable/strained/critical/collapse
      "debt_min": 5000,                  // NEW: Kingdom debt threshold
      "food_days_max": 3,                // NEW: Food scarcity
      "unpaid_wages": true               // NEW: Native troops unpaid
    },
    "escalation_requirements": {
      "pay_tension_min": 40              // Existing
    }
  }
}
```

### Example Events Using Crisis Triggers

**Example 1: Simple Crisis Check**
```json
{
  "id": "pay_crisis_simple",
  "triggers": {
    "all": ["is_enlisted", "lord_crisis:critical"],
    "escalation_requirements": {
      "pay_tension_min": 40
    }
  }
}
```

**Example 2: Specific Debt Check**
```json
{
  "id": "pay_crisis_debt_collectors",
  "triggers": {
    "all": ["is_enlisted", "lord_debt:high"],
    "lord_financial_state": {
      "debt_min": 5000
    }
  }
}
```

**Example 3: Compound Crisis**
```json
{
  "id": "pay_crisis_collapse",
  "triggers": {
    "all": ["is_enlisted", "lord_crisis:collapse"],
    "lord_financial_state": {
      "crisis_level_min": "collapse",
      "unpaid_wages": true,
      "food_days_max": 2
    },
    "escalation_requirements": {
      "pay_tension_min": 70
    }
  }
}
```

### Schema Validator Updates

**Add validation rules** (if schema validation exists):

```csharp
// Validate lord_financial_state block
if (eventDef.Triggers.LordFinancialState != null)
{
    var fs = eventDef.Triggers.LordFinancialState;
    
    if (!string.IsNullOrEmpty(fs.CrisisLevelMin))
    {
        var validLevels = new[] { "stable", "strained", "critical", "collapse" };
        if (!validLevels.Contains(fs.CrisisLevelMin.ToLowerInvariant()))
        {
            errors.Add($"Invalid crisis_level_min: {fs.CrisisLevelMin}");
        }
    }
    
    if (fs.DebtMin < 0)
        errors.Add("debt_min cannot be negative");
    
    if (fs.FoodDaysMax < 0)
        errors.Add("food_days_max cannot be negative");
}
```

---

## Testing Strategy

### Unit Tests (Code Level)

**Test 1: Crisis Calculation**
```csharp
[Test]
public void TestCrisisCalculation_Stable()
{
    // Setup: Lord with 50k gold, no debt, plenty of food
    var lord = CreateTestLord(gold: 50000);
    var state = CalculateLordFinancialCrisis(lord);
    
    Assert.AreEqual(LordFinancialCrisisLevel.Stable, state.CrisisLevel);
    Assert.AreEqual(0, state.CrisisScore);
}

[Test]
public void TestCrisisCalculation_Critical()
{
    // Setup: Lord with 3k gold, 5k debt, unpaid troops
    var lord = CreateTestLord(gold: 3000, debt: 5000, unpaidWages: 0.8f);
    var state = CalculateLordFinancialCrisis(lord);
    
    Assert.AreEqual(LordFinancialCrisisLevel.Critical, state.CrisisLevel);
    Assert.IsTrue(state.CrisisScore >= 5);
}

[Test]
public void TestCrisisCalculation_Collapse()
{
    // Setup: Everything failing
    var lord = CreateTestLord(gold: 500, debt: 8000, unpaidWages: 1.0f, foodDays: 1);
    var state = CalculateLordFinancialCrisis(lord);
    
    Assert.AreEqual(LordFinancialCrisisLevel.Collapse, state.CrisisLevel);
    Assert.IsTrue(state.CrisisScore >= 6);
}
```

**Test 2: Native Value Reading**
```csharp
[Test]
public void TestNativeValueAccess()
{
    // Verify we can safely read native properties without crashes
    var lord = GetRandomLordFromCampaign();
    var party = lord?.PartyBelongedTo;
    var clan = lord?.Clan;
    
    Assert.DoesNotThrow(() =>
    {
        var gold = lord.Gold;
        var debt = clan.DebtToKingdom;
        var unpaidWages = party.HasUnpaidWages;
        var foodDays = party.GetNumDaysForFoodToLast();
        var partyGold = party.PartyTradeGold;
    });
}
```

**Test 3: Tension Application**
```csharp
[Test]
public void TestCrisisTensionModifier()
{
    // Setup: Lord in Critical state
    var enlistment = SetupEnlistmentWithCrisis(LordFinancialCrisisLevel.Critical);
    var initialTension = enlistment.PayTension;
    
    // Execute: Run daily tick
    enlistment.ProcessEnlistedDailyService();
    
    // Verify: Tension increased by 3 (Critical level)
    Assert.AreEqual(initialTension + 3, enlistment.PayTension);
}
```

### Integration Tests (In-Game)

**Test Scenario 1: Rich Lord → Poor Lord**
1. Start new campaign
2. Find wealthy lord (>50k gold)
3. Enlist
4. Use console commands to drain lord's gold: `campaign.change_hero_gold -45000 [lord_id]`
5. Monitor crisis state in logs
6. Verify crisis detected and tension increases

**Test Scenario 2: Compound Crisis**
1. Enlist with any lord
2. Use console to create crisis:
   - Reduce gold to 1000
   - Add kingdom debt (if possible via console)
   - Remove food from party
3. Verify crisis reaches Collapse
4. Check pay tension escalates rapidly
5. Verify events fire (if Phase 4 implemented)

**Test Scenario 3: Long-Term Stability**
1. Enlist with stable lord
2. Play for 50+ in-game days
3. Monitor crisis state remains Stable
4. Verify no spurious crisis detections
5. Check performance (FPS, memory)

**Test Scenario 4: Save/Load**
1. Create crisis scenario
2. Save game
3. Load game
4. Verify crisis state persists correctly
5. Verify no corruption

### Stress Tests

**Test 1: Rapid Lord Changes**
- Enlist, discharge, re-enlist 10 times rapidly
- Verify no crashes
- Check memory leaks

**Test 2: Multiple Simultaneous Crises**
- Find campaign where multiple lords are broke
- Switch between them
- Verify calculations don't interfere

**Test 3: Extreme Values**
- Test with lord at max gold (999999999)
- Test with lord at 0 gold
- Test with max debt
- Verify no overflow/underflow

### User Acceptance Tests

**UAT 1: Narrative Clarity**
- Player understands WHY lord can't pay
- Crisis indicators make sense
- Events feel appropriate to crisis level

**UAT 2: Gameplay Feel**
- Crisis progression feels fair
- Not too punishing for player
- Provides interesting choices

**UAT 3: Performance**
- No noticeable lag
- Smooth gameplay
- Fast save/load times

---

## Rollout Plan

### Pre-Release Checklist

**Code Quality:**
- [ ] All unit tests pass
- [ ] Integration tests pass
- [ ] No compiler warnings
- [ ] Code reviewed
- [ ] Commented and documented

**Functionality:**
- [ ] Crisis calculation works
- [ ] Native values read correctly
- [ ] Tension modifiers apply
- [ ] Trigger tokens functional
- [ ] Events fire (if Phase 4 done)

**Stability:**
- [ ] No crashes
- [ ] Save/load works
- [ ] No save corruption
- [ ] Memory leaks checked
- [ ] Performance acceptable

**Documentation:**
- [ ] pay-system.md updated
- [ ] This implementation doc complete
- [ ] Event schema documented
- [ ] Changelog updated
- [ ] README updated

### Release Strategy

**Option 1: Phased Rollout (Recommended)**
- **Release 1:** Phase 1 only (infrastructure, logging)
  - Low risk, pure observation
  - Gather data on crisis frequency
  - No behavior changes
- **Release 2:** Phase 2 (crisis tension)
  - Enable behavior changes
  - Monitor player feedback
  - Adjust tension rates if needed
- **Release 3:** Phase 3 + 4 (events)
  - Add new content
  - Full feature complete

**Option 2: All-at-Once**
- Complete Phases 1-3 before release
- More testing required
- Higher risk but faster delivery

**Option 3: Minimal Viable Feature**
- Release Phase 1 + 2 only
- Skip Phase 4 (new events)
- Use existing pay tension events
- Lower content burden

### Recommended Approach

**Start with Option 3 (Minimal Viable Feature):**
1. Implement Phases 1-2 (infrastructure + behavior)
2. Release with existing pay tension events
3. Observe for 1-2 weeks
4. Gather player feedback
5. Add Phase 4 (new events) if feedback positive

**Benefits:**
- Lower risk (incremental changes)
- Faster time to value
- Can validate design before investing in content
- Easier to debug if issues arise

---

## Risk Analysis

### Technical Risks

**Risk 1: Native API Changes**
- **Impact:** High
- **Probability:** Low
- **Mitigation:** 
  - Wrap native calls in try-catch
  - Graceful degradation if values unavailable
  - Version detection

**Risk 2: Performance Impact**
- **Impact:** Medium
- **Probability:** Low
- **Mitigation:**
  - Profile before/after
  - Cache crisis state (daily calculation)
  - Early exit if not enlisted

**Risk 3: Save Corruption**
- **Impact:** Critical
- **Probability:** Very Low
- **Mitigation:**
  - Don't persist crisis state (calculate on demand)
  - Thorough save/load testing
  - Beta testing with community

### Design Risks

**Risk 1: Overcomplicated**
- **Impact:** Medium
- **Probability:** Medium
- **Mitigation:**
  - Start simple (Phase 1-2 only)
  - Validate before expanding
  - Player feedback

**Risk 2: Balance Issues**
- **Impact:** Medium
- **Probability:** Medium
- **Mitigation:**
  - Conservative tension rates
  - Easy to tune via config
  - Monitor player complaints

**Risk 3: Unclear to Players**
- **Impact:** Low
- **Probability:** Medium
- **Mitigation:**
  - Clear UI indicators
  - Tooltip explanations
  - Good event writing

### Mitigation Summary

**Always Safe:**
- Phase 1 is pure observation, zero risk
- Can be released without behavior changes
- Easy to remove if problematic

**Progressive Enhancement:**
- Each phase builds on previous
- Can stop at any phase if needed
- Backward compatible

**Fail-Safe Design:**
- Defaults to stable state if errors
- Never crashes campaign
- Logs errors for debugging

---

## Appendix A: Console Commands (Testing)

### Useful Console Commands for Testing

**Create Crisis Scenarios:**
```
# Drain lord's gold
campaign.change_hero_gold -45000 [lord_id]

# Check lord's current gold
campaign.print_hero_gold [lord_id]

# Remove party food
campaign.add_item_to_hero_party [item_id] -100

# Check party info
campaign.print_party_info [party_id]
```

**Find Lord IDs:**
```
# List all lords
campaign.list_heroes

# Find lord by name
campaign.find_hero [name_fragment]
```

**Time Manipulation:**
```
# Fast forward time
campaign.fast_forward [hours]

# Set time of day
campaign.set_time_of_day [hour]
```

**Note:** Exact console commands depend on campaign system. These are examples - refer to game documentation for actual syntax.

---

## Appendix B: Native Code References

### Key Native Files Analyzed

1. **DefaultClanFinanceModel.cs**
   - Path: `TaleWorlds.CampaignSystem.GameComponents`
   - Key Methods:
     - `CalculateClanGoldChange()`
     - `AddExpensesFromPartiesAndGarrisons()`
     - `ApplyMoraleEffect()`

2. **DefaultPartyDesertionModel.cs**
   - Path: `TaleWorlds.CampaignSystem.GameComponents`
   - Key Methods:
     - `GetTroopsToDesert()`
     - `GetTroopsToDesertDueToWageAndPartySize()`

3. **MobileParty.cs**
   - Path: `TaleWorlds.CampaignSystem.Party`
   - Key Properties:
     - `HasUnpaidWages`
     - `PartyTradeGold`
     - `TotalWage`
     - `GetNumDaysForFoodToLast()`

4. **Clan.cs**
   - Path: `TaleWorlds.CampaignSystem`
   - Key Properties:
     - `Gold`
     - `DebtToKingdom`

5. **PartiesBuyFoodCampaignBehavior.cs**
   - Path: `TaleWorlds.CampaignSystem.CampaignBehaviors`
   - Key Methods:
     - `TryBuyingFood()`
     - `BuyFoodInternal()`

### Native Constants

```csharp
// From DefaultClanFinanceModel
private const int PartyGoldIncomeThreshold = 10000;
private const int payGarrisonWagesTreshold = 8000;
private const int payClanPartiesTreshold = 4000;
private const int payLeaderPartyWageTreshold = 2000;

// Kingdom bailout threshold
if (clan.Gold < 30000 && clan.Kingdom != null)
    AddIncomeFromKingdomBudget(...);
```

---

## Appendix C: Configuration Examples

### Optional: Config-Driven Thresholds

**If you want to make crisis thresholds configurable:**

`enlisted_config.json` additions:
```json
{
  "finance": {
    "lord_crisis": {
      "enabled": true,
      "gold_thresholds": {
        "critical": 2000,
        "poor": 4000,
        "struggling": 8000
      },
      "debt_thresholds": {
        "minor": 1000,
        "significant": 5000,
        "crushing": 10000
      },
      "food_crisis": {
        "days_critical": 2,
        "days_warning": 5,
        "min_gold_to_buy": 500
      },
      "tension_modifiers": {
        "stable": 0,
        "strained": 1,
        "critical": 3,
        "collapse": 5
      },
      "check_interval_days": 1
    }
  }
}
```

**Load in code:**
```csharp
private LordCrisisConfig LoadCrisisConfig()
{
    var config = EnlistedConfig.LoadFinanceConfig();
    return config?.LordCrisis ?? GetDefaultCrisisConfig();
}

private LordCrisisConfig GetDefaultCrisisConfig()
{
    return new LordCrisisConfig
    {
        Enabled = true,
        GoldThresholds = new() { Critical = 2000, Poor = 4000, Struggling = 8000 },
        DebtThresholds = new() { Minor = 1000, Significant = 5000, Crushing = 10000 },
        // ... etc
    };
}
```

**Benefits:**
- Easy tuning without recompiling
- Players can adjust difficulty
- Mod-friendly

**Drawbacks:**
- More complexity
- Config validation needed
- Harder to reason about defaults

**Recommendation:** Start hardcoded, add config if needed later.

---

## Appendix D: Future Enhancements

### Post-Release Improvements (Not in Scope)

**Enhancement 1: Desertion Tracking**
- Track lord's party size daily
- Detect when troops disappear (desertion vs. battle)
- Add desertion events specific to lord's crisis

**Enhancement 2: Lord Behavior Changes**
- Lords in crisis become more aggressive (need loot)
- Lords avoid expensive battles
- Lords sell equipment/prisoners more desperately

**Enhancement 3: Kingdom Response**
- Kingdom intervenes in severe crises
- Other lords comment on financial troubles
- Political consequences for bankrupt lords

**Enhancement 4: Player Agency**
- Loan gold to lord (interest system)
- Help lord with trade missions
- Negotiate debt forgiveness

**Enhancement 5: Crisis Storytelling**
- Story packs specifically about lord financial collapse
- Multi-event chains following crisis arc
- Different outcomes based on player choices

**Enhancement 6: Companion Reactions**
- Retinue members comment on lord's finances
- Lance mates discuss desertion options
- Loyalty tests during crises

**Enhancement 7: Historical Tracking**
- Track lord's financial history
- "Always broke" lords vs. "temporarily struggling"
- Reputation impacts

**Enhancement 8: UI Dashboard**
- Detailed financial breakdown UI
- Charts showing crisis trends
- Warning indicators

---

## Document History

| Version | Date | Author | Changes |
|---------|------|--------|---------|
| 1.0 | 2025-12-13 | AI Assistant | Initial implementation guide created |

---

## Notes & Warnings

### Critical Warnings

⚠️ **DO NOT persist crisis state in save files**
- Calculate on-demand each check
- Avoids save compatibility issues
- Allows tuning without breaking saves

⚠️ **ALWAYS null-check native properties**
- Lords can die, parties can disband
- Native values can be null
- Graceful degradation required

⚠️ **TEST save/load thoroughly**
- Save corruption is worst-case scenario
- Test across multiple game versions
- Beta test with community

⚠️ **START SMALL**
- Phase 1 is observation only (safe)
- Validate before changing behavior
- Easy to roll back if needed

### Development Notes

📝 **Keep existing systems working**
- Backward compatibility mandatory
- Don't break existing pay tension events
- Old Lord Wealth Status still works

📝 **Log everything in Phase 1**
- Observation phase needs data
- Log crisis state changes
- Log native value readings

📝 **Performance matters**
- Daily tick runs every game day
- Keep calculations lightweight
- Profile if adding expensive operations

📝 **Player communication**
- Clear changelog entries
- Explain new system in patch notes
- Provide feedback channels

---

## Contact & Support

**For Questions:**
- Review this document first
- Check native code references (Appendix B)
- Consult event schema documentation

**For Issues:**
- Check risk analysis section
- Review rollback procedures
- Log errors with context

**For Feedback:**
- Document what works/doesn't work
- Include save files if relevant
- Describe expected vs. actual behavior

---

**END OF DOCUMENT**
