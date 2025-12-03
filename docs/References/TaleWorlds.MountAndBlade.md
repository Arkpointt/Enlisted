# TaleWorlds.MountAndBlade Reference

## Relevance to Enlisted Mod: HIGH

This assembly handles all 3D mission/battle mechanics - the actual combat you experience when fighting. It includes agent (character) management, formations, AI behaviors, siege weapons, and mission flow. Essential for our kill tracking, formation assignment, and battle behavior features.

---

## Key Classes and Systems

### Mission and Agents

| Class | Purpose | Enlisted Usage |
|-------|---------|----------------|
| `Mission` | Active 3D battle instance | Check Mission.Current for active battles |
| `Agent` | Individual character in mission | Track player kills, detect deaths |
| `AgentComponent` | Extensible agent behavior | Could add custom agent components |
| `Team` | Battle team (player/enemy/ally) | Formation assignments |

### Formation System

| Class | Purpose | Enlisted Usage |
|-------|---------|----------------|
| `Formation` | Group of agents in formation | Player's assigned formation |
| `FormationAI` | AI controlling formation behavior | Understand formation orders |
| `FormationClass` | Infantry, Ranged, Cavalry, etc. | Assign player to appropriate formation |
| `ArrangementOrder` | Formation shape orders | Line, column, circle, etc. |

### Battle Behaviors

| Class | Purpose | Enlisted Usage |
|-------|---------|----------------|
| `BehaviorComponent` | Base for formation AI behaviors | Understanding formation behaviors |
| `BehaviorCharge` | Charge attack behavior | Understanding AI orders |
| `BehaviorDefend` | Defensive behavior | Understanding defensive stances |
| `BehaviorRetreat` | Retreat behavior | Understanding withdrawal |

### Orders System

| Class | Purpose | Enlisted Usage |
|-------|---------|----------------|
| `OrderController` | Issues orders to formations | Battle command system |
| `MovementOrder` | Movement-type orders | Advance, retreat, hold |
| `FacingOrder` | Direction facing orders | Formation orientation |
| `FiringOrder` | Ranged attack orders | Fire at will, hold fire |

### Mission Logic

| Class | Purpose | Enlisted Usage |
|-------|---------|----------------|
| `MissionBehavior` | Base for mission behaviors | CombatStatsMissionBehavior |
| `MissionLogic` | Core mission logic base | Mission flow control |
| `BattleEndLogic` | Handles battle ending | Detect battle completion |
| `CasualtyHandler` | Tracks deaths/wounds | Kill tracking integration |

### Siege Systems

| Class | Purpose | Enlisted Usage |
|-------|---------|----------------|
| `SiegeLadder` | Siege ladder object | Siege assault mechanics |
| `SiegeTower` | Siege tower object | Understanding siege flow |
| `BatteringRam` | Ram siege weapon | Gate assault mechanics |

---

## Important Directories

### `/AI/`
AI decision making. Study `AgentHumanAILogic`, `TacticComponent` for understanding how AI soldiers behave.

### `/ComponentInterfaces/`
Model interfaces for combat calculations. `AgentStatCalculateModel`, `StrikeMagnitudeModel`, etc.

### `/GameKeyCategory/`
Input key categories for combat controls.

---

## Critical Patterns

### Checking Active Mission
```csharp
if (Mission.Current != null)
{
    // We're in a 3D battle
    var playerAgent = Mission.Current.MainAgent;
}
```

### Mission Behavior Registration
```csharp
public class CombatStatsMissionBehavior : MissionBehavior
{
    public override MissionBehaviorType BehaviorType => MissionBehaviorType.Other;
    
    public override void OnAgentRemoved(Agent affectedAgent, Agent affectorAgent, 
        AgentState agentState, KillingBlow blow)
    {
        // Track kills here
    }
}
```

### Formation Assignment Check
```csharp
var playerFormation = Mission.Current?.MainAgent?.Formation;
var formationType = playerFormation?.FormationIndex; // Infantry, Ranged, etc.
```

### Battle Side Detection
```csharp
var playerTeam = Mission.Current?.PlayerTeam;
var isDefender = playerTeam?.Side == BattleSideEnum.Defender;
```

---

## Files to Study

| File | Why |
|------|-----|
| `Mission.cs` | Core mission class, all battle state |
| `Agent.cs` | Character properties, health, weapons |
| `Formation.cs` | Formation properties and behavior |
| `BehaviorComponent.cs` | Base for AI behaviors - we patch this |
| `Team.cs` | Team management, formations collection |
| `OrderController.cs` | How orders are issued |
| `CasualtyHandler.cs` | Death/wound tracking |
| `BattleEndLogic.cs` | Battle completion detection |

---

## Event Hooks

### Agent Events
- `OnAgentRemoved` - Agent dies or retreats
- `OnAgentBuild` - Agent spawns
- `OnAgentHit` - Agent takes damage

### Mission Events
- `OnMissionTick` - Every frame during mission
- `AfterStart` - Mission initialized
- `OnMissionResultReady` - Battle outcome determined

---

## Kill Tracking Implementation

For tracking player kills, override `OnAgentRemoved`:

```csharp
public override void OnAgentRemoved(Agent affectedAgent, Agent affectorAgent, 
    AgentState agentState, KillingBlow blow)
{
    // affectorAgent is who killed them
    // Check if affectorAgent is player
    if (affectorAgent != null && affectorAgent.IsMainAgent)
    {
        // Player killed affectedAgent
        if (affectedAgent.IsEnemyOf(affectorAgent))
        {
            // Valid enemy kill
            _playerKills++;
        }
    }
}
```

---

## Known Issues / Gotchas

1. **Mission.Current Timing**: Only valid during active 3D battles, null on campaign map
2. **Agent Lifecycle**: Agents can be removed before mission ends
3. **Formation Changes**: Formations can be reorganized mid-battle
4. **Team Assignment**: Player team assignment happens after mission start
5. **Order Authority**: Only commanders can issue certain orders

