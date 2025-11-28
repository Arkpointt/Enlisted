# SandBox Reference

## Relevance to Enlisted Mod: HIGH

The SandBox assembly contains the actual implementation of campaign behaviors, mission management, and game components specific to the sandbox/campaign mode. While TaleWorlds.CampaignSystem provides the framework, SandBox provides many concrete implementations.

---

## Key Classes and Systems

### Mission Management

| Class | Purpose | Enlisted Usage |
|-------|---------|----------------|
| `SandBoxMissionManager` | Creates campaign missions | Understanding how missions are started |
| `SandBoxMissions` | Static mission creation methods | Study mission setup patterns |
| `CampaignMissionManager` | Campaign-specific mission handling | Mission lifecycle |
| `MapScene` | Campaign map scene | Map rendering context |

### Campaign Behaviors

| Class | Purpose | Enlisted Usage |
|-------|---------|----------------|
| `DefaultCutscenesCampaignBehavior` | Handles cutscenes/notifications | We patch to suppress kingdom join cutscenes |
| `DefaultNotificationsCampaignBehavior` | Default notifications | Notification patterns |
| `GuardsCampaignBehavior` | Settlement guard spawning | Understanding NPC spawns |
| `RecruitmentAgentSpawnBehavior` | Recruitable NPC spawning | Settlement population |
| `StatisticsCampaignBehavior` | Tracks game statistics | Stat tracking patterns |

### Game Components

| Class | Purpose | Enlisted Usage |
|-------|---------|----------------|
| `SandboxBattleBannerBearersModel` | Banner bearer logic | Understanding battle roles |
| `GameComponents/` folder | Various gameplay calculations | Study for game balance understanding |

### Issues System

| Class | Purpose | Enlisted Usage |
|-------|---------|----------------|
| `Issues/` folder | Quest/issue implementations | Study for quest patterns if we add quests |

### Board Games

| Class | Purpose | Enlisted Usage |
|-------|---------|----------------|
| `BoardGames/` folder | Tavern board games | Not directly relevant |

---

## Important Directories

### `/CampaignBehaviors/`
Concrete behavior implementations. Very useful to study for patterns.

Key behaviors to examine:
- `DefaultCutscenesCampaignBehavior` - We patch this for kingdom notifications
- `HeirSelectionCampaignBehavior` - How special menus are triggered
- `RetirementCampaignBehavior` - End-game handling
- `PrisonBreakCampaignBehavior` - Complex event chains

### `/GameComponents/`
Model implementations for calculations:
- Troop wages
- Party morale
- Battle rewards
- Prisoner management

### `/Missions/`
Mission setup and configuration:
- Arena missions
- Tournament missions
- Settlement missions
- Battle missions

### `/Source/Missions/`
Additional mission logic and agent behaviors.

---

## Critical Files to Study

| File | Why |
|------|-----|
| `SandBoxSubModule.cs` | Module initialization, behavior registration |
| `SandBoxMissions.cs` | How different mission types are created |
| `CampaignMissionManager.cs` | Mission-campaign bridge |
| `SandBoxGameManager.cs` | Game state management |

---

## Behavior Registration Pattern

Study how SandBox registers its behaviors in `SandBoxSubModule`:

```csharp
protected override void OnGameStart(Game game, IGameStarter gameStarter)
{
    if (game.GameType is Campaign)
    {
        CampaignGameStarter campaignStarter = (CampaignGameStarter)gameStarter;
        campaignStarter.AddBehavior(new DefaultCutscenesCampaignBehavior());
        campaignStarter.AddBehavior(new StatisticsCampaignBehavior());
        // ... more behaviors
    }
}
```

---

## Cheat System

The SandBox assembly contains the game's cheat system. While not directly relevant to the mod, it shows patterns for:
- Adding gold
- Spawning troops
- Healing characters
- Unlocking content

Files like `Add1000GoldCheat.cs`, `Give5TroopsToPlayerCheat.cs` show how to modify game state safely.

---

## Conversation Missions

The `/Conversation/` folder shows how conversation missions are set up:
- `ConversationMission.cs` - How dialog scenes work
- `MissionLogics/` - Logic for conversation flow

---

## Agent Navigator

`AgentNavigator.cs` handles NPC navigation in settlements. Useful if we need to understand how NPCs move in town/castle scenes.

---

## Known Patterns

### Mission Creation
```csharp
// SandBoxMissions pattern for starting missions
public static Mission OpenBattleMission(...)
{
    return MissionState.OpenNew("battle", ...);
}
```

### Behavior Event Handling
```csharp
// Most behaviors follow this pattern
public override void RegisterEvents()
{
    CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
    CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, OnDailyTick);
}
```

---

## Not Relevant to Enlisted

- `BoardGames/` - Tavern games, not needed
- `Tournaments/` - Tournament system, not needed for basic enlistment
- Most cheat classes - Development only

