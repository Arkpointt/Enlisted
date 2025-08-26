# ADR-006: Promotion System Design

**Status:** Accepted  
**Date:** 2025-01-12  
**Deciders:** Development Team

## Context

The enlistment system needed a progression mechanism to provide long-term engagement and simulate military advancement. Traditional RPG leveling doesn't fit the military theme, and the base game lacks enlisted rank progression.

Requirements identified:
1. **Merit-Based Advancement**: Promotions earned through combat participation
2. **Tier-Based Ranks**: Clear progression ladder with meaningful milestones
3. **Experience Accumulation**: XP system tied to battle participation
4. **Save Persistence**: Promotion progress maintained across sessions

## Decision

We implement a **Military Promotion System** with experience-based tier advancement:

### Domain Model
- **PromotionState**: Tracks current tier, XP progress, and advancement requirements
- **Tier System**: Numerical ranks (1-N) with increasing XP requirements
- **XP Accumulation**: Battle participation awards experience points
- **Progressive Requirements**: Higher tiers require more XP to achieve

### Application Layer
- **PromotionBehavior**: Campaign integration and battle event handling
- **PromotionRules**: Business logic for XP awards and tier requirements
- **Battle Integration**: Automatic XP grants based on combat participation

### Infrastructure Layer
- **Save Integration**: Persists promotion state via TaleWorlds save system
- **Battle Detection**: Hooks into game's battle participation events
- **XP Calculation**: Determines appropriate XP rewards for different battle types

## Implementation Details

### XP Award System
- **Battle Participation**: Base XP for joining battles while enlisted
- **Battle Outcome**: Bonus XP for victories, reduced for defeats
- **Battle Scale**: Larger battles provide more XP than smaller skirmishes
- **Continuous Service**: No XP decay - progress is permanent

### Tier Progression
```
Tier 1 (Recruit): 0 XP     → 600 XP to advance
Tier 2 (Private): 600 XP   → 1200 XP to advance  
Tier 3 (Corporal): 1800 XP → 1800 XP to advance
Tier 4 (Sergeant): 3600 XP → 2400 XP to advance
[Additional tiers scale exponentially]
```

### Advancement Mechanics
- **Automatic Promotion**: Tier advancement happens automatically when XP threshold reached
- **Surplus XP**: Excess XP carries over to next tier requirement
- **No Demotion**: Players cannot lose tiers once achieved
- **Cross-Enlistment**: Promotion progress persists between different army enlistments

## Consequences

### Positive
- **Long-term Engagement**: Provides progression goals beyond basic enlistment
- **Battle Incentive**: Encourages active participation in military campaigns
- **Achievement Recognition**: Visible progression reflects player dedication
- **Replayability**: Different promotion paths with different armies

### Negative
- **Grind Potential**: Higher tiers may require extensive battle participation
- **Balance Complexity**: XP values need careful tuning to feel rewarding
- **Save Dependency**: Promotion progress lost if save system fails

### Neutral
- **Performance Impact**: Minimal - calculations only during battle events
- **UI Requirements**: Future promotion display needs implementation

## Battle Integration

### XP Award Triggers
- **BattleParticipationPatch**: Hooks into game's battle end events
- **Enlistment Requirement**: Only enlisted players receive military XP
- **Automatic Processing**: No player input required during battles

### XP Calculation Logic
```csharp
Base XP = 50 (per battle)
Victory Bonus = +25 XP
Defeat Penalty = -10 XP  
Large Battle Bonus = +15 XP (>100 troops)
Siege Bonus = +20 XP
```

## Future Considerations

- **Rank Names**: Replace numeric tiers with military rank titles
- **Rank Benefits**: Mechanical advantages for higher tiers (better equipment, wage bonuses)
- **Specialization Tracks**: Different advancement paths (Infantry, Cavalry, Archery)
- **Officer Promotion**: Path from enlisted ranks to commissioned officer status
- **Cross-Faction Recognition**: Promotion progress acknowledged by different armies

## Compliance

This decision implements:
- Blueprint Section 2.2: "Progression System Design"
- Blueprint Section 3.1: "Event-Driven Architecture" 
- Blueprint Section 4.3: "Persistent State Management"
