# ADR-001: Enlistment System Implementation

**Date:** 2025-01-27  
**Status:** Accepted  
**Deciders:** Development Team  

## Context

The Enlisted mod needs to implement a Freelancer-style enlistment system where players can join a lord's army as a soldier through conversation dialogs. The system must:

1. Allow players to request enlistment from lords via conversation
2. Base acceptance on reputation (positive/neutral = accept, negative = decline)
3. Manage equipment storage and restoration
4. Provide leave functionality through conversation
5. Follow modern Bannerlord APIs and the project blueprint

## Decision

We will implement the enlistment system using:

### Architecture Approach
- **CampaignBehavior-based**: Use `EnlistmentBehavior` extending `CampaignBehaviorBase` for proper game integration
- **State Management**: Serializable `EnlistmentState` class for persistence
- **Dialog System**: Modern Bannerlord conversation APIs (`AddPlayerLine`, `AddDialogLine`)
- **Equipment Management**: Store/restore player equipment during enlistment/leave cycles

### Implementation Details

#### Core Components
1. **EnlistmentBehavior** (`src/Features/Enlistment/Application/EnlistmentBehavior.cs`)
   - Handles dialog registration and game events
   - Manages enlistment state transitions
   - Integrates with `CampaignEvents` for proper lifecycle

2. **EnlistmentState** (`src/Features/Enlistment/Domain/EnlistmentState.cs`)
   - Serializable state management
   - Equipment storage and restoration
   - Service time and tier tracking

#### Dialog Flow
```
lord_talk_speak_diplomatic
├── "I would like to join your army as a soldier" (enlistment_start)
├── Lord response based on reputation
│   ├── Accept: "I would be honored to have you serve in my army" (enlistment_accept)
│   └── Decline: "I cannot accept you at this time" (enlistment_decline)
└── Player confirmation (enlistment_confirm)
```

#### Leave Flow
```
lord_talk_speak_diplomatic (while enlisted)
├── "I would like to request leave from your army" (enlistment_ask_leave)
├── Commander response
│   ├── Accept: "You are free to go" (enlistment_leave_accept)
│   └── Decline: "Cannot grant leave" (enlistment_leave_decline)
└── Player confirmation (enlistment_leave_confirm)
```

### Reputation System
- **Acceptance Threshold**: ≥0 relation (neutral or positive)
- **Decline Threshold**: <0 relation (negative)
- **Implementation**: `Hero.MainHero.GetRelation(conversationHero)`

### Equipment Management
- **Storage**: Store all equipment slots and inventory items
- **Restoration**: Restore original equipment on leave
- **Soldier Equipment**: Currently minimal (placeholder for future customization)

## Consequences

### Positive
- ✅ Follows Freelancer mod pattern for familiarity
- ✅ Uses modern Bannerlord APIs (not deprecated)
- ✅ Proper state persistence with save/load support
- ✅ Clean separation of concerns (behavior vs state)
- ✅ Extensible design for future enhancements

### Negative
- ⚠️ Requires reputation management (may need adjustment for different playstyles)
- ⚠️ Equipment system is basic (needs enhancement for full soldier experience)
- ⚠️ No wage/promotion system yet (future enhancement)

### Risks
- **Save Game Compatibility**: State serialization must remain stable
- **Mod Compatibility**: Dialog IDs must be unique to avoid conflicts
- **Performance**: Equipment storage/restoration on frequent paths

## Alternatives Considered

### Alternative 1: Harmony-based Implementation
- **Approach**: Use Harmony patches to intercept conversation flows
- **Rejected**: Unnecessary complexity for public API functionality
- **Reason**: Modern Bannerlord APIs provide sufficient hooks

### Alternative 2: Menu-driven System
- **Approach**: Force custom menus on settlement entry (ServeAsSoldier style)
- **Rejected**: Reduces player agency and conflicts with blueprint's permissive approach
- **Reason**: Prefer conversation-driven approach for better UX

### Alternative 3: Army-based Attachment
- **Approach**: Create actual armies and attach player party
- **Rejected**: Overly complex for simple soldier roleplay
- **Reason**: Reference-based attachment (Freelancer style) is sufficient

## Implementation Notes

### Current Limitations
1. Equipment system needs enhancement for proper soldier gear
2. No wage or promotion mechanics
3. Leave conditions are always accepted (can be made conditional)
4. No battle participation mechanics

### Future Enhancements
1. **Equipment Progression**: Tier-based equipment upgrades
2. **Wage System**: Daily payments based on service tier
3. **Promotion System**: Automatic promotions based on service time
4. **Battle Mechanics**: Enhanced participation for enlisted soldiers
5. **Leave Restrictions**: Minimum service time requirements

### Migration Strategy
- Current implementation is forward-compatible
- New features can be added without breaking existing saves
- Equipment system can be enhanced incrementally

## References

- [Freelancer Reference Document](Freelancer-Reference.md)
- [ServeAsSoldier Reference Document](ServeAsSoldier-Reference.md)
- [Bannerlord API Documentation](https://apidoc.bannerlord.com/v/1.2.12/)
- [Project Blueprint](BLUEPRINT.md) - Package-by-Feature organization
