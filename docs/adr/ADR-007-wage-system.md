# ADR-007: Wage System Design

**Status:** Accepted  
**Date:** 2025-01-12  
**Deciders:** Development Team

## Context

Enlisted soldiers in medieval armies received regular compensation for their service. Mount & Blade II: Bannerlord lacks a wage system for enlisted players, who typically only earn gold through loot and quest rewards.

The enlistment system needed:
1. **Regular Income**: Steady gold income during military service
2. **Configurable Rates**: Adjustable wage amounts for different playstyles
3. **Automatic Payment**: No player micromanagement required
4. **Economic Balance**: Wages that supplement but don't replace other income sources

## Decision

We implement a **Daily Wage System** that provides regular compensation for enlisted military service:

### Domain Model
- **Daily Payment Cycle**: Wages paid every 24 hours of in-game time
- **Fixed Rate System**: Consistent wage amount per day of service
- **Configuration-Driven**: Wage amounts controlled via mod settings
- **Service Requirement**: Only active enlisted players receive wages

### Application Layer
- **WageBehavior**: Campaign integration and daily tick event handling
- **Payment Processing**: Automatic gold transfers using game's economic system
- **Service Validation**: Ensures player is actively enlisted before payment
- **Configuration Integration**: Uses centralized settings for wage amounts

### Infrastructure Layer
- **Gold Transfer**: Uses TaleWorlds.CampaignSystem.Actions.GiveGoldAction
- **Daily Tick Hook**: Integrates with campaign's daily advancement events
- **Settings Integration**: Reads wage rates from ModSettings configuration

## Implementation Details

### Payment Schedule
- **Trigger**: CampaignEvents.DailyTickEvent (once per in-game day)
- **Eligibility**: Player must be actively enlisted with valid commander
- **Amount**: Configurable via settings.xml (default: 10 gold per day)
- **Delivery**: Automatic transfer to player's gold reserves

### Wage Calculation
```csharp
Base Daily Wage = 10 gold (configurable)
Service Requirement = Active enlistment status
Payment Condition = Valid commander + active army
```

### Economic Integration
- **Gold Source**: Wages appear as gold gains (not from specific character)
- **No Deductions**: Full wage amount paid regardless of army expenses
- **Immediate Availability**: Gold usable immediately upon payment
- **Transaction Logging**: Payment events logged for debugging

### Configuration Options
```xml
<DailyWage>10</DailyWage>              <!-- Gold per day -->
<ShowVerboseMessages>false</ShowVerboseMessages> <!-- Payment notifications -->
```

## Consequences

### Positive
- **Steady Income**: Reliable gold source encourages longer enlistments
- **Economic Immersion**: Simulates historical military compensation
- **Progression Support**: Funds equipment upgrades and army expenses
- **Configurable Balance**: Server operators can adjust economic impact

### Negative
- **Economic Inflation**: Regular gold income may unbalance game economy
- **Passive Income**: Players receive money without active effort
- **Configuration Dependency**: Requires careful wage rate balancing

### Neutral
- **Performance Impact**: Single daily calculation per player
- **Save Compatibility**: No state persistence required

## Economic Balance Considerations

### Wage Rate Guidelines
- **Conservative Default**: 10 gold/day (equivalent to 1-2 basic food items)
- **Supplement Not Replace**: Wages should not exceed combat/trade income
- **Army Context**: Wage rates should reflect faction economic status
- **Player Choice**: Optional feature via configuration settings

### Comparison to Game Economy
```
Daily Wage (10g) vs Other Income Sources:
- Village trade mission: 100-500g (one-time)
- Battle loot: 50-200g (per battle)
- Prisoner sales: 20-100g (per batch)
- Workshop income: 50-300g/day (requires investment)
```

## Future Considerations

- **Tier-Based Wages**: Higher promotion tiers receive better pay
- **Faction Wage Differences**: Rich kingdoms pay more than poor ones
- **Performance Bonuses**: Extra pay for battle victories or achievements
- **Deduction System**: Wage penalties for poor performance or misconduct
- **Equipment Allowances**: Automatic equipment provisioning instead of gold
- **Pension System**: Retirement benefits for long-service veterans

## Integration Points

### Dependencies
- **Enlistment System**: Must be actively enlisted to receive wages
- **Configuration System**: Wage rates controlled via centralized settings
- **Campaign Events**: Hooks into daily advancement cycle

### Service Interactions
- **Battle Participation**: No direct interaction (wages independent of combat)
- **Promotion System**: Future enhancement for tier-based wage increases
- **Army Management**: Wages continue regardless of army activities

## Compliance

This decision implements:
- Blueprint Section 2.3: "Economic System Integration"
- Blueprint Section 3.3: "Configuration-Driven Behavior"
- Blueprint Section 4.2: "Automated Background Processing"
