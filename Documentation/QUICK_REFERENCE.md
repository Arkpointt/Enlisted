# ENLISTED MOD - QUICK REFERENCE
*Fast lookup for key information during development*

---

## ?? QUICK START

### Build & Test
```bash
# Build the mod
dotnet build Enlisted.csproj

# Installation path
Modules/Enlisted/

# Required game version
Mount & Blade II: Bannerlord (Latest)
```

### Key Files
- **Main Logic:** `Behaviors/EnlistmentBehavior.cs`
- **Army Operations:** `Services/ArmyService.cs`  
- **Party Hiding:** `Services/PartyIllusionService.cs`
- **Dialog System:** `Services/DialogService.cs`
- **Settings:** `Utils/Constants.cs`

---

## ?? CORE APIS

### Army Management
```csharp
// Create army
((Kingdom)faction).CreateArmy(leader, settlement, Army.ArmyTypes.Patrolling);

// Join army
party.Army = targetArmy;

// Leave army  
party.Army = null;

// AI escort behavior
SetPartyAiAction.GetActionForEscortingParty(escortParty, targetParty);
```

### Party Illusion
```csharp
// Hide player party
MobileParty.MainParty.IsVisible = false;

// Follow commander's camera
commanderParty.Party.SetAsCameraFollowParty();

// Restore player control
main.Party.SetAsCameraFollowParty();
main.IsVisible = true;
```

### Dialog Integration
```csharp
// Conversation hubs
"lord_talk", "hero_main_options"

// Priority for visibility
110

// Condition pattern
() => !IsEnlisted && ValidCommander(Hero.OneToOneConversationHero)
```

---

## ?? TESTING SCENARIOS

### Basic Flow
1. Start new campaign
2. Find a lord in a town
3. Talk to lord ? should see "I wish to enlist" option
4. Enlist ? party should disappear, camera follows lord
5. Talk to same lord ? should see "I'd like to leave" option
6. Leave ? party reappears, camera returns

### Edge Cases
- Commander dies while enlisted
- Army disbands while enlisted  
- Faction goes to war while enlisted
- Save/load while enlisted
- Multiple army operations

---

## ?? PROJECT STRUCTURE

```
Enlisted/
??? Behaviors/
?   ??? EnlistmentBehavior.cs     # Main orchestrator
?   ??? WageBehavior.cs           # Wage system
??? Services/
?   ??? ArmyService.cs            # Army join/leave logic
?   ??? PartyIllusionService.cs   # Hide/show party
?   ??? DialogService.cs          # Conversation registration
??? Patches/
?   ??? BanditEncounterPatch.cs   # Hostile encounter prevention
?   ??? BattleParticipationPatch.cs # Auto-join battles
?   ??? HidePartyNamePlatePatch.cs # UI nameplate hiding
??? Utils/
?   ??? ReflectionHelpers.cs      # API compatibility
?   ??? Constants.cs              # All constants
?   ??? DebugHelper.cs            # Production utilities
??? Models/
?   ??? EnlistmentState.cs        # Save/load data
??? SubModule.cs                  # Mod entry point
```

---

## ?? CONFIGURATION

### Constants (Utils/Constants.cs)
```csharp
MAIN_HUBS = ["lord_talk", "hero_main_options"]
DIALOG_PRIORITY = 110  
MESSAGE_PREFIX = "[Enlisted]"
```

### Save Fields (Models/EnlistmentState.cs)
```csharp
[SaveableField(1)] bool IsEnlisted
[SaveableField(2)] Hero Commander  
[SaveableField(3)] bool PendingDetach
[SaveableField(4)] bool PlayerPartyWasVisible
```

---

## ?? REFLECTION APIS

### Critical Reflection Calls
```csharp
// SetPartyAiAction.GetActionForEscortingParty
var actionType = typeof(MobileParty).Assembly.GetType("TaleWorlds.CampaignSystem.Actions.SetPartyAiAction");
var method = actionType?.GetMethod("GetActionForEscortingParty", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
method?.Invoke(null, new object[] { escortParty, targetParty });
```

### Fallback Pattern
```csharp
try {
    // Reflection call
    ReflectionHelpers.SetEscortAction(party, target);
} catch {
    // Safe fallback
    party?.Ai?.SetMoveEscortParty(target);
}
```

---

## ? FEATURE STATUS

### ? Complete
- Core enlistment system
- Army integration  
- Party illusion
- Save/load persistence
- Service architecture
- Dialog integration
- Battle participation
- Hostile encounter protection
- UI integration

### ?? Ready for Use
- Complete "Serve as a Soldier" experience
- Seamless lord integration
- Automatic battle participation
- Protection from independent encounters
- Settlement following
- Professional UI hiding

---

## ?? CRITICAL NOTES

### Must Remember
1. **Use Game's APIs:** Always mirror Bannerlord's own patterns
2. **Fail-Safe Design:** Provide fallbacks for every operation
3. **SaveableField:** Only on private fields, not properties
4. **Reflection Safety:** Always try/catch reflection calls
5. **Service Pattern:** Keep behaviors as orchestrators only

### Performance Tips
- Cache reflection results
- Minimize OnTick operations
- Use efficient state checks
- Avoid repeated object creation

---

**Last Updated:** [Current Date]  
**Status:** Production Ready  
**Version:** 1.0