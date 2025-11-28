# TaleWorlds.CampaignSystem Reference

## Relevance to Enlisted Mod: CRITICAL

This is the most important reference for the Enlisted mod. It contains the entire campaign/strategic layer of the game - everything from hero management, party behavior, kingdom systems, dialogue, encounters, and game menus. Nearly every feature in Enlisted depends on this assembly.

---

## Key Classes and Systems

### Core Campaign Classes

| Class | Purpose | Enlisted Usage |
|-------|---------|----------------|
| `Campaign` | Main campaign singleton, manages all campaign state | Access current game state, time, menu context |
| `CampaignBehaviorBase` | Base class for all campaign behaviors | All our behaviors inherit from this |
| `CampaignEvents` | Static event hooks for campaign events | Subscribe to battle start/end, settlement events, hero events |
| `CampaignGameStarter` | Registers behaviors, menus, dialogs at game start | Register EnlistmentBehavior, menus, conversations |
| `CampaignTime` | Time representation in campaign | Calculate wages, desertion timers, leave duration |

### Party and Movement

| Class | Purpose | Enlisted Usage |
|-------|---------|----------------|
| `MobileParty` | Represents a moving party on the map | Player party management, lord party following |
| `PartyBase` | Base class for all parties | Access MapEvent, SiegeEvent, etc. |
| `Hero` | Represents a character/lord | Track enlisted lord, check alive/prisoner status |
| `Army` | Groups of parties under one leader | Join lord's army for battles |

### Encounters and Battles

| Class | Purpose | Enlisted Usage |
|-------|---------|----------------|
| `EncounterManager` | Creates and manages encounters | Start settlement encounters, battle participation |
| `PlayerEncounter` | Current player encounter state | Check if in encounter, leave/finish encounters |
| `MapEvent` | Represents a battle/siege on map | Detect lord battles, check battle state |
| `MapEventState` | Battle state enum (Wait, Started, etc.) | Detect siege phases to prevent RGL crashes |

### Menu System

| Class | Purpose | Enlisted Usage |
|-------|---------|----------------|
| `GameMenu` | Static methods for menu activation | ActivateGameMenu, SwitchToMenu, ExitToLast |
| `GameMenuManager` | Manages all game menus | RefreshMenuOptions |
| `GameMenuOption` | Individual menu option | Our custom menu options |
| `MenuCallbackArgs` | Callback context for menu options | Access menu context, get party info |

### Dialogue System

| Class | Purpose | Enlisted Usage |
|-------|---------|----------------|
| `DialogFlow` | Conversation flow definition | Define enlistment dialogs |
| `ConversationManager` | Manages active conversations | Start/end conversations |
| `ConversationSentence` | Individual dialog line | Our dialog sentences |

### Kingdom and Clan

| Class | Purpose | Enlisted Usage |
|-------|---------|----------------|
| `Kingdom` | Faction with vassals and policies | Track enlisted kingdom for desertion grace |
| `Clan` | Family/clan grouping | Player clan, lord clans |
| `IFaction` | Interface for factions | Check faction allegiance |

### Settlements

| Class | Purpose | Enlisted Usage |
|-------|---------|----------------|
| `Settlement` | Towns, castles, villages | Visit settlements while enlisted |
| `Town` | Town-specific data | Town access system |
| `Village` | Village-specific data | Village visits |

### Save System Integration

| Class | Purpose | Enlisted Usage |
|-------|---------|----------------|
| `IDataStore` | Save/load interface | Persist enlistment state |
| `SaveableTypeDefiner` | Register saveable types | Register our custom types |

---

## Important Directories

### `/Actions/`
Game actions like `ChangeRelationAction`, `KillCharacterAction`, `GiveGoldAction`. We patch `ChangeRelationAction` to suppress discharge relation penalties.

### `/CampaignBehaviors/`
Native behaviors to study for patterns. See `DefaultEncounterBehaviors`, `PlayerCaptivityBehavior`.

### `/GameMenus/`
Native menu definitions. Study `EncounterGameMenuBehavior` for encounter menu patterns.

### `/Encounters/`
Encounter types. `PlayerEncounter` is critical for understanding battle/siege flows.

### `/MapEvents/`
Battle/siege event system. `MapEvent` states are critical for our siege crash fixes.

### `/Helpers/`
Utility classes like `MobilePartyHelper`, `SettlementHelper`. Useful patterns for our code.

### `/GameComponents/`
Model classes that calculate game values. Study for understanding wage, skill, morale calculations.

---

## Critical API Patterns

### Subscribing to Events
```csharp
CampaignEvents.OnMapEventStarted.AddNonSerializedListener(this, OnMapEventStarted);
CampaignEvents.HeroKilled.AddNonSerializedListener(this, OnHeroKilled);
CampaignEvents.OnSettlementLeftEvent.AddNonSerializedListener(this, OnSettlementLeft);
```

### Menu Registration
```csharp
campaignGameStarter.AddGameMenu("menu_id", "Menu text {VARIABLE}", OnMenuInit);
campaignGameStarter.AddGameMenuOption("menu_id", "option_id", "Option text", OnCondition, OnConsequence);
```

### Saving State
```csharp
public override void SyncData(IDataStore dataStore)
{
    dataStore.SyncData("_enlistedLord", ref _enlistedLord);
    dataStore.SyncData("_enlistmentXP", ref _enlistmentXP);
}
```

---

## Files to Study

| File | Why |
|------|-----|
| `Campaign.cs` | Understand campaign tick system, menu context |
| `CampaignEvents.cs` | All available event hooks |
| `MobileParty.cs` | Party properties, AI, movement |
| `PlayerEncounter.cs` | Encounter state machine |
| `EncounterManager.cs` | How encounters are created |
| `MapEvent.cs` | Battle/siege state tracking |
| `GameMenu.cs` | Menu activation/switching |
| `Hero.cs` | Hero properties, states |

---

## Known Issues / Gotchas

1. **Menu Transitions**: Calling `GameMenu.SwitchToMenu()` during certain states causes RGL assertion crashes
2. **Encounter Timing**: `PlayerEncounter.Finish()` must be called at the right time
3. **Party Visibility**: Setting `IsVisible` during skeleton updates causes crashes
4. **MapEvent State**: Check `MapEventState.Wait` before processing siege events
5. **Army Attachment**: `AttachTo` can cause timing issues during siege transitions

