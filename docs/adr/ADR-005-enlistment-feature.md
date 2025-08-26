# ADR-005: Enlistment Feature Design

**Status:** Accepted  
**Date:** 2025-01-12  
**Deciders:** Development Team

## Context

Mount & Blade II: Bannerlord lacks a formal military enlistment system that allows players to serve as regular soldiers under army commanders. Players can only join armies as independent mercenaries or vassals, which doesn't provide the authentic enlisted soldier experience.

The mod needs to provide:
1. **Voluntary Enlistment**: Players can choose to enlist with army commanders
2. **Contract Enforcement**: Prevent players from easily abandoning their military service
3. **Authentic Experience**: Hide player party identity to simulate being a regular soldier
4. **Save Persistence**: Maintain enlistment state across game sessions

## Decision

We implement an **Enlistment System** with the following core components:

### Domain Model
- **EnlistmentState**: Tracks current enlistment status, commander, and contract lifecycle
- **Contract Lifecycle**: Enlist → Active Service → Leave Request → Discharge
- **Commander Validation**: Continuous checking that commander is alive and has an active party

### Application Layer
- **EnlistmentBehavior**: Campaign integration and game event handling
- **IEnlistmentService**: Public interface for other features to check enlistment status
- **Dialog Integration**: Conversation system for enlistment/discharge requests

### Infrastructure Layer
- **ArmyIntegrationService**: Handles joining/leaving army parties via TaleWorlds API
- **PartyIllusionService**: Manages player party visibility and name plate hiding
- **DialogService**: Implements conversation trees for enlistment interactions

### Game Adapter Integration
- **SuppressArmyMenuPatch**: Prevents conflicting "leave army" options during enlistment
- **HidePartyNamePlatePatch**: Hides player party nameplate for immersion
- **Save Integration**: Persists enlistment state via TaleWorlds save system

## Consequences

### Positive
- **Immersive Gameplay**: Players experience authentic military service
- **Narrative Consistency**: Enlisted players cannot abandon service easily
- **Save Compatibility**: State persists across game sessions and updates
- **Extensible Design**: Other features can query enlistment status via interface

### Negative
- **Game State Complexity**: Must track and validate commander state continuously
- **Save Game Dependency**: Relies on TaleWorlds save system stability
- **Potential Soft Locks**: Players could get stuck if commander dies unexpectedly

### Neutral
- **Performance Impact**: Minimal - only checks during specific game events
- **Compatibility**: Should work with most other mods unless they modify army systems

## Implementation Details

### Enlistment Flow
1. Player approaches army commander
2. Dialog option to "Request Enlistment" appears
3. Player party becomes hidden and joins commander's army
4. Player receives confirmation and enters active service state

### Discharge Flow
1. Player uses dialog to "Request Discharge"
2. System marks discharge as pending
3. Player party becomes visible again
4. Enlistment state resets to civilian

### Error Handling
- **Commander Death**: Automatic discharge with notification
- **Army Dissolution**: Emergency service termination
- **Save Corruption**: Graceful fallback to civilian state

## Compliance

This decision implements:
- Blueprint Section 2.1: "Domain-Driven Feature Design"
- Blueprint Section 3.2: "Infrastructure Isolation"
- Blueprint Section 4.1: "Fail-Safe State Management"

## Future Considerations

- **Multiple Enlistment Types**: Different service branches (infantry, cavalry, archers)
- **Contract Terms**: Time-limited enlistments vs. indefinite service
- **Desertion Penalties**: Reputation or bounty consequences for abandoning service
- **Officer Track**: Advanced enlistment path leading to command positions
