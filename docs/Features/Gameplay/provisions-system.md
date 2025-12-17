# Provisions & Food System

**Status**: [x] Implemented (v3.0)  
**Category**: Gameplay Feature  
**Dependencies**: Quartermaster Hero, Retinue System, Fatigue System

---

## Overview

The **Provisions System** allows players to purchase rations from the quartermaster to boost morale and restore fatigue. At Commander ranks (T7+), players can also provision their personal retinue to maintain soldier morale and prevent desertions.

---

## Personal Rations

Purchase food for yourself to gain temporary benefits:

### Rations Tiers

| Tier | Cost | Duration | Morale Bonus | Fatigue Recovery |
|------|------|----------|--------------|------------------|
| **Basic Rations** | 25 denars | 3 days | +2 morale | None |
| **Good Fare** | 50 denars | 3 days | +4 morale | -1 fatigue/day |
| **Officer's Table** | 100 denars | 3 days | +6 morale | -2 fatigue/day |

### How It Works

1. **Purchase**: Visit quartermaster -> "I need better provisions"
2. **Activation**: Rations immediately take effect
3. **Duration**: Benefits last for specified days
4. **Stacking**: Only one rations tier active at a time (purchasing new tier replaces old)
5. **Expiration**: Benefits cease when timer expires

### Morale Impact
- Passive morale boost while active
- Stacks with other morale sources
- Visible in status displays

### Fatigue Restoration
- **Good Fare**: Restores 1 fatigue point per day
- **Officer's Table**: Restores 2 fatigue points per day
- Applied at daily tick
- Helps offset fatigue costs from activities

### Strategic Use
- **Before Long Campaigns**: Stock up on Officer's Table for extended operations
- **Low Fatigue**: Use Good Fare or Officer's Table for daily recovery
- **Budget Option**: Basic Rations provide morale without premium cost
- **PayTension**: Higher morale helps offset pay delay penalties

---

## Retinue Provisioning (T7-T9)

At Commander ranks, players gain a personal retinue that requires feeding:

### Provisioning Tiers

| Tier | Cost per Soldier | Duration | Morale Effect |
|------|------------------|----------|---------------|
| **Bare Minimum** | 1 denar | 7 days | -2 morale |
| **Standard** | 2 denars | 7 days | No modifier |
| **Good Fare** | 4 denars | 7 days | +2 morale |
| **Officer Quality** | 6 denars | 7 days | +4 morale |

### Retinue Sizes by Tier
- **T7 (Commander I)**: 15 soldiers
- **T8 (Commander II)**: 25 soldiers  
- **T9 (Commander III)**: 35 soldiers

### Example Costs

**T7 Commander with 15 soldiers:**
- Bare Minimum: 15g for 7 days (2.1g/day)
- Standard: 30g for 7 days (4.3g/day)
- Good Fare: 60g for 7 days (8.6g/day)
- Officer Quality: 90g for 7 days (12.9g/day)

**T9 Commander with 35 soldiers:**
- Bare Minimum: 35g for 7 days (5g/day)
- Standard: 70g for 7 days (10g/day)
- Good Fare: 140g for 7 days (20g/day)
- Officer Quality: 210g for 7 days (30g/day)

### Warning System

**2 Days Before Expiration:**
- Warning message displayed: "Your retinue provisions will expire in 2 days!"
- Reminder to restock before starvation

**At Expiration (Starvation):**
- **Severe morale penalty** applied to retinue
- **Risk of desertions** if not addressed
- Warning: "Your retinue has run out of provisions! Morale is plummeting."

### Strategic Considerations

**Balancing Cost vs Morale:**
- **Standard**: Neutral option, no morale modifier
- **Good Fare**: Best balance of cost and morale boost
- **Officer Quality**: Premium option for critical operations
- **Bare Minimum**: Emergency option when broke (morale penalty)

**Timing Purchases:**
- Restock 2 days before expiration to avoid warnings
- Plan major expenses (equipment, rations) around pay musters
- Consider provisioning tier based on upcoming battles

**Morale Management:**
- High morale retinues fight better
- Offsets other morale penalties (pay tension, camp conditions)
- Critical for maintaining discipline during long campaigns

---

## Accessing Provisions

### Menu Navigation

1. **Enlisted Status** -> **Visit Quartermaster**
2. Open dialogue with quartermaster
3. Choose: **"I need better provisions"**
4. Quartermaster responds with archetype-appropriate line
5. Opens **Quartermaster Rations** menu

### Rations Menu Structure

```
QUARTERMASTER RATIONS
--------------------
[Current Status]
- Your Rations: Good Fare (2 days remaining)
- Retinue Provisions: Standard (5 days remaining, 15 soldiers)

PERSONAL RATIONS
- Basic Rations (25g) - 3 days, +2 morale
- Good Fare (50g) - 3 days, +4 morale, -1 fatigue/day
- Officer's Table (100g) - 3 days, +6 morale, -2 fatigue/day

RETINUE PROVISIONING (T7+)
- Bare Minimum (1g/soldier) - 7 days, -2 morale
- Standard (2g/soldier) - 7 days, neutral
- Good Fare (4g/soldier) - 7 days, +2 morale
- Officer Quality (6g/soldier) - 7 days, +4 morale

< Back to Equipment Menu
```

### Quick Access Tips
- **Spacebar**: Fast-forward time while in menu (wait menu functionality)
- **ESC**: Close menu quickly
- **Direct Purchase**: Click option to buy immediately

---

## Integration with Other Systems

### Fatigue System
- Good Fare and Officer's Table restore fatigue daily
- Helps offset costs of camp activities
- Useful for high-activity playstyles

### Morale System
- All rations tiers boost morale
- Retinue morale affects combat effectiveness
- Stacks with other morale sources

### Pay System
- Provisions are optional purchases
- No penalties for not buying (except retinue starvation)
- Budget accordingly around pay musters

### Quartermaster Relationship
- Purchasing provisions grants +1 relationship
- Higher relationship = discounts on all purchases
- At 80+ relationship (Battle Brother): 15% discount

### Camp Life Simulation
- Camp conditions don't affect provision availability
- Provisions help offset morale penalties from poor camp conditions

---

## Technical Details

### Core Files
- **Purchase Logic**: `QuartermasterManager.cs` (AddRationsMenuOptions)
- **Rations State**: `EnlistmentBehavior.cs` (Food/Rations section)
- **Retinue Provisions**: `EnlistmentBehavior.cs` (Retinue Provisioning section)
- **Daily Tick**: `EnlistmentBehavior.cs` (OnDailyTick)

### Persistence
- Current rations tier and expiration date saved
- Retinue provisioning tier and expiration saved
- Warning flag (prevents spam) saved

### Localization
All strings localized in `enlisted_strings.xml`:
- Menu headers and options
- Warning messages
- Status displays

---

## Design Philosophy

### Optional System
- **No Forced Purchases**: Provisions are entirely optional
- **No Penalties**: Except retinue starvation (which has clear warnings)
- **Player Agency**: Choose when and what to buy

### Risk vs Reward
- **Investment**: Spend gold now for future benefits
- **Planning**: Anticipate future needs
- **Budgeting**: Balance provisions with equipment and other expenses

### Immersive Detail
- Reinforces military service theme
- Quartermaster as provider of necessities
- Reflects real army logistics

### Commander Responsibility
- Retinue provisioning at T7+ reflects command authority
- Managing soldier welfare part of officer duties
- Clear consequences (starvation) for neglect

---

## Player Strategies

### Early Game (T1-T6)
- **Basic Rations**: Cheap morale boost before battles
- **Good Fare**: If fatigue is an issue
- **Officer's Table**: For critical missions only
- **Skip**: If gold is tight and morale is fine

### Commander Ranks (T7-T9)
- **Personal**: Maintain Officer's Table for fatigue management
- **Retinue**: Standard or Good Fare for baseline morale
- **Pre-Battle**: Upgrade to Officer Quality before major battles
- **Budget**: Drop to Bare Minimum temporarily if desperate

### PayTension Management
- Keep morale high to offset pay tension penalties
- Provisions help maintain loyalty during pay delays
- Officer Quality for critical retention periods

---

## Future Expansion Opportunities

- **Culture-Specific Rations**: Different foods per culture
- **Quartermaster Archetype Effects**: Scoundrel offers "interesting" food
- **Feast Events**: Special occasions with unique provisions
- **Supply Lines**: Availability affected by distance from towns
- **Black Market Food**: Higher quality, higher risk
- **Retinue Preferences**: Soldiers request better food

---

## Related Systems

- [Quartermaster Hero](../Core/quartermaster-hero-system.md)
- [Retinue System](../Core/retinue-system.md)
- [Fatigue System](../Core/camp-fatigue.md)
- [Pay System](../Core/pay-system.md)
