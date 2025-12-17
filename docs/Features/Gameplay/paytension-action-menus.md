# PayTension Action Menus

**Status**: ✅ Implemented (v3.0)  
**Category**: Gameplay Feature  
**Dependencies**: Pay System, Camp, Quartermaster Hero

---

## Overview

When pay is severely delayed (PayTension 40+), two special action menus appear in **Camp**: **Desperate Measures** (corruption path) and **Help the Lord** (loyalty path). These menus offer ways to earn gold or reduce PayTension through morally gray or honorable actions.

---

## Accessing Action Menus

### Prerequisites
- **Enlisted** status active
- **PayTension 40+** (pay delayed/disrupted)
- Access to **Camp** (from Enlisted Status)

### Menu Location
1. **Enlisted Status** → **Camp**
2. Camp menu opens
3. Two new options appear (if PayTension 40+):
   - 🔴 **"Desperate Measures..."** (HostileAction icon)
   - 🟢 **"Help the Lord with Finances"** (Mission icon)

### Visibility
- **Below 40 tension**: Menus completely hidden
- **40+ tension**: Both menus visible
- **Higher tension**: More options unlock within each menu

---

## Desperate Measures (Corruption Path)

**Theme**: Dark path - earn gold now, face consequences later  
**Icon**: HostileAction (red skull)  
**Access**: PayTension 40+

### Menu Introduction
"Desperate Measures

When legitimate channels fail, some turn to darker paths. Choose carefully - your reputation and honor are at stake.

Current PayTension: {tension}/100"

---

### Option 1: Bribe Paymaster's Clerk
**Unlocks at**: PayTension 40+  
**Cost**: 50 denars  
**Risk**: 30% chance of getting caught

**Success (70%):**
- Gain 70 denars (20 net profit)
- Message: "The clerk adjusts the records in your favor."

**Failure (30%):**
- Lose 50 denars
- -10 Quartermaster relationship
- Message: "The clerk takes your money... then reports you."

**Strategy**: Low-risk gambling with small profit potential

---

### Option 2: Skim Supplies
**Unlocks at**: PayTension 40+  
**Requirement**: Quartermaster OR Armorer duty  
**Gain**: 30 denars

**Effects:**
- Gain 30 denars immediately
- If QM relationship < 40: -5 relationship + suspicious message

**Strategy**: Duty-locked option for supply personnel

---

### Option 3: Find Black Market
**Unlocks at**: PayTension 50+  
**Effect**: Information only (no immediate gameplay effect)

**Message**: "You make contact with some... entrepreneurial traders. They'll be around the camp from time to time."

**Future Potential**: Foundation for black market trading system

---

### Option 4: Sell Issued Equipment
**Unlocks at**: PayTension 60+  
**Gain**: Tier × 25 denars (25-225 depending on rank)

**Effects:**
- Immediate gold based on rank
- No immediate penalties
- Risk: Need to replace equipment eventually

**Strategy**: Emergency cash when desperate

---

### Option 5: Listen to Desertion Talk
**Unlocks at**: PayTension 70+  
**Special**: Triggers free desertion at PayTension 60+

**If Free Desertion Available (60+ tension):**
- Shows inquiry: "Will you desert with them?"
- Option to leave service immediately
- Minimal penalties (-5 lord relation only)

**If Below 60 Tension:**
- Flavor text only: "You listen but decide the risk isn't worth it... yet."

**Strategy**: Endgame option when situation is critical

---

## Help the Lord (Loyalty Path)

**Theme**: Honorable path - help lord recover finances, reduce tension  
**Icon**: Mission (quest marker)  
**Access**: PayTension 40+

### Menu Introduction
"Help the Lord

The lord's coffers are running low, but loyal soldiers can help. Volunteer for missions that bring coin to the treasury.

Current PayTension: {tension}/100
Lord {lordName} needs your help to restore the treasury."

---

### Mission 1: Collect Debts
**Unlocks at**: PayTension 40+  
**Skill Check**: Charm (50% base + Charm/5)  
**Success**: -10 PayTension  
**Fatigue Cost**: None

**Success:**
- PayTension reduced by 10
- Message: "You successfully collect the debts. The lord is pleased."

**Failure:**
- No effect
- Message: "The merchants refuse to pay. Perhaps they need more... persuasion."

**Strategy**: Low-risk, skill-based option

---

### Mission 2: Escort Merchant
**Unlocks at**: PayTension 50+  
**Cost**: 4 fatigue  
**Effect**: -15 PayTension (always succeeds)

**Effects:**
- PayTension reduced by 15
- Costs 4 fatigue
- Message: "You escort the merchant safely. The lord's coffers grow."

**Strategy**: Guaranteed success but costs fatigue

---

### Mission 3: Negotiate Loan
**Unlocks at**: PayTension 60+  
**Requirement**: Trade skill 50+  
**Skill Check**: Trade skill percentage  
**Success**: -20 PayTension

**Success:**
- PayTension reduced by 20
- Message: "You secure a favorable loan for the lord. The treasury is replenished."

**Failure:**
- No effect
- Message: "The bankers aren't interested. Perhaps another approach..."

**If Trade < 50:**
- Option grayed out
- Tooltip: "Requires Trade skill 50+. Your skill: {skill}"

**Strategy**: High-skill, high-reward option

---

### Mission 4: Volunteer for Raid
**Unlocks at**: PayTension 70+  
**Requirement**: Lord at war  
**Risk**: Combat injury chance  
**Effect**: -25 PayTension + 50 denars

**Success (based on combat skills):**
- PayTension reduced by 25
- Gain 50 denars
- Message: "The raid is a complete success! Valuable loot is captured."

**Injury (10-50% chance based on combat skill):**
- PayTension still reduced by 25
- Gain 50 denars
- Lose 20-50 HP
- Message: "The raid succeeds but you're wounded! (-{damage} HP)"

**If Not at War:**
- Option grayed out
- Tooltip: "Only available when the lord is at war."

**Injury Chance Formula:**
```
combatSkill = OneHanded + TwoHanded
injuryChance = Max(10%, 50% - (combatSkill / 10))
```

**Strategy**: High-risk, high-reward endgame mission

---

## PayTension Reduction

### Stabilization Threshold
When PayTension drops **below 40** (from ≥40):
- Special message: "The lord's financial situation has stabilized."
- Desperate Measures and Help the Lord menus disappear
- Normal operations resume

### Reduction Sources
**Loyalty Missions:**
- Collect Debts: -10 tension
- Escort Merchant: -15 tension
- Negotiate Loan: -20 tension
- Volunteer for Raid: -25 tension

**Automatic:**
- Pay muster success: tension reduced significantly
- Time passage: gradual decay if lord's situation improves

---

## Strategic Decision Making

### Corruption vs Loyalty

**Desperate Measures (Corruption):**
- **Pro**: Immediate gold, no fatigue cost
- **Con**: Risk of getting caught, reputation damage, moral compromise
- **Best For**: Players who need gold NOW

**Help the Lord (Loyalty):**
- **Pro**: Reduces tension, builds lord relations, honorable
- **Con**: Costs fatigue, skill-gated, indirect benefit
- **Best For**: Players focused on long-term service

### Optimal Strategies

**Early Tension (40-59):**
- Start with Collect Debts or Bribe Clerk (low risk)
- Skim Supplies if you have the right duty
- Save fatigue for emergencies

**Moderate Tension (60-79):**
- Escalate to Escort Merchant or Negotiate Loan
- Sell Equipment if you need immediate cash
- Consider black market contact

**Critical Tension (80-100):**
- Volunteer for Raid (if lord at war)
- Listen to Desertion Talk (free desertion option)
- All-or-nothing: save the lord or leave

---

## Integration with Other Systems

### Quartermaster Hero
- Quartermaster offers archetype-specific advice about these options
- Relationship bonuses for exploring PayTension dialogue
- Scoundrel hints at Desperate Measures
- Believer encourages Help the Lord

### Fatigue System
- Escort Merchant costs 4 fatigue
- Balance fatigue budget with mission requirements
- Good Fare/Officer's Table can help restore fatigue

### Free Desertion
- Listen to Desertion Talk triggers free desertion system
- At 60+ tension: leave with minimal penalties
- Alternative to helping lord or corruption

### Skills
- Charm affects Collect Debts success
- Trade skill gates Negotiate Loan
- Combat skills affect Raid injury chance

---

## Technical Implementation

### Core Files
- **Menu Creation**: `CampMenuHandler.cs` (AddDesperateMeasuresMenu, AddHelpTheLordMenu)
- **Tension Tracking**: `EnlistmentBehavior.cs` (_payTension field)
- **Tension Reduction**: `EnlistmentBehavior.cs` (ReducePayTension method)
- **Free Desertion**: `EnlistmentBehavior.cs` (ProcessFreeDesertion)

### Menu Visibility Logic
```csharp
// In Command Tent menu option condition:
var tension = EnlistmentBehavior.Instance?.PayTension ?? 0;
if (tension < 40) return false; // Hide completely
return true; // Show at 40+
```

### PayTension Reduction
```csharp
public void ReducePayTension(int amount)
{
    var oldTension = _payTension;
    _payTension = Math.Max(0, _payTension - amount);
    
    if (_payTension < 40 && oldTension >= 40)
    {
        // Stabilization message
    }
}
```

---

## Design Philosophy

### Moral Ambiguity
- No "correct" path - both have valid reasons
- Corruption offers immediate relief but long-term risk
- Loyalty offers honor but requires sacrifice
- Player choice reflects their character

### Escalation
- Options unlock progressively as situation worsens
- Higher tension = more extreme options
- Mirrors desperation of unpaid soldiers

### Consequences
- Corruption path: immediate benefit, potential reputation cost
- Loyalty path: helps lord, reduces tension, builds relations
- Free desertion: emergency exit when situation is hopeless

### Player Agency
- All options are optional
- Can ignore menus entirely
- Multiple paths to same goal (reduce tension)

---

## Future Expansion Opportunities

- **Reputation System**: Track corruption vs loyalty choices
- **Lord Reactions**: Lord discovers your corruption or praises loyalty
- **Companion Opinions**: Companions approve/disapprove of choices
- **Black Market System**: Fully implemented trading via Desperate Measures
- **Raid Missions**: Full tactical raid gameplay
- **Debt Collection**: Actual NPC interactions vs abstract skill check
- **Loan Negotiations**: Dialogue with bankers

---

## Related Systems

- [Pay System](../Core/pay-system.md)
- [Quartermaster Hero](../Core/quartermaster-hero-system.md)
- [Camp Fatigue](../Core/camp-fatigue.md)
- [Free Desertion](../Core/enlistment.md#free-desertion)
