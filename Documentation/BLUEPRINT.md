# ENLISTED MOD BLUEPRINT
*Complete documentation of features, architecture, and implementation*

---

## ?? TABLE OF CONTENTS
1. [Mod Overview](#mod-overview)
2. [Current Features](#current-features)
3. [Technical Architecture](#technical-architecture)
4. [Implementation Status](#implementation-status)
5. [Development Roadmap](#development-roadmap)
6. [Technical Notes](#technical-notes)
7. [Testing Checklist](#testing-checklist)

---

## ?? MOD OVERVIEW

**Name:** Enlisted - Serve as a Soldier  
**Version:** 1.0.0  
**Target Game:** Mount & Blade II: Bannerlord  
**Framework:** .NET Framework 4.7.2, C# 13.0  

**Core Concept:** Allow players to enlist in a lord's army as a subordinate, creating an immersive "serve as a soldier" experience where the player follows and fights alongside their commander rather than leading independently.

**Key Philosophy:** Seamless integration with existing game mechanics using the same APIs as Bannerlord's built-in systems.

---

## ? CURRENT FEATURES

### ?? Core Enlistment System
- **Dialog Integration**: Seamlessly integrated into existing lord conversation hubs
  - "I wish to enlist in your army" option
  - "I'd like to leave your service" option
  - Appears in `lord_talk` and `hero_main_options` hubs
  - Priority 110 to appear prominently

### ?? Army Management Integration
- **True Army Joining**: Uses exact same APIs as game's Army Management screen
  - Creates army for commander if they don't have one
  - Adds player to commander's army via `Kingdom.CreateArmy()`
  - Sets proper AI escort behavior via `SetPartyAiAction.GetActionForEscortingParty()`
  - Leaves army cleanly via `main.Army = null`

### ??? Party Illusion System
- **Visual Merger**: Creates convincing illusion of merged parties
  - Hides player party from map (`main.IsVisible = false`)
  - Transfers camera control to commander (`commanderParty.Party.SetAsCameraFollowParty()`)
  - Maintains escort behavior when not in formal army
  - Restores player control on discharge

### ?? Wage System
- **Daily Compensation**: Integrated wage payment system
  - Configurable daily wage amount
  - Automatic payment while enlisted
  - Settings persistence across game sessions

### ??? Settlement Integration
- **Seamless Town Entry**: Follow commander into settlements
  - Automatic entry when commander enters towns
  - Maintains enlisted status inside settlements
  - Can talk to commander while in settlements

### ?? Battle Participation
- **Automatic Battle Joining**: Join commander's battles automatically
  - Monitors when commander enters combat
  - Uses encounter system to join battles on correct side
  - Provides battle notifications to player

### ??? Hostile Encounter Protection
- **Protection While Enlisted**: Prevents independent hostile encounters
  - Blocks bandit attacks while serving
  - Prevents enemy faction encounters
  - Smart detection of hostile vs. friendly parties

### ?? UI Integration
- **Clean Visual Experience**: Professional UI modifications
  - Hides party nameplate when enlisted
  - Removes visual indicators of independent party
  - Seamless restoration when leaving service

---

## ??? TECHNICAL ARCHITECTURE

### ?? Folder Structure
```
Enlisted/
??? Behaviors/           # Main orchestrators
?   ??? EnlistmentBehavior.cs    # Core enlistment orchestrator
?   ??? WageBehavior.cs          # Wage payment system
??? Services/            # Business logic layer
?   ??? ArmyService.cs           # Army creation/joining/leaving
?   ??? PartyIllusionService.cs  # Party hiding & camera control
?   ??? DialogService.cs         # Dialog registration & text
??? Patches/             # Harmony patches
?   ??? BanditEncounterPatch.cs  # Hostile encounter prevention
?   ??? BattleParticipationPatch.cs # Auto-join battles
?   ??? HidePartyNamePlatePatch.cs # UI nameplate hiding
??? Utils/               # Utilities and helpers
?   ??? ReflectionHelpers.cs     # Reflection-based API calls
?   ??? Constants.cs             # All constants & messages
?   ??? DebugHelper.cs           # Production utilities
??? Models/              # Data structures
?   ??? EnlistmentState.cs       # Save/load state management
??? SubModule.cs         # Mod entry point
```

### ?? Design Patterns Used
- **Service Layer Pattern**: Clean separation of concerns
- **Orchestrator Pattern**: Main behavior coordinates services
- **Reflection Pattern**: API compatibility across game versions
- **State Management**: Clean save/load system
- **Harmony Patching**: Non-intrusive game modification

### ?? Key Design Principles
1. **Single Responsibility**: Each class has one clear purpose
2. **Dependency Injection**: Services are injected, not instantiated
3. **Fail-Safe Fallbacks**: Multiple layers of fallback behavior
4. **Game API Compatibility**: Uses exact same patterns as base game

---

## ?? IMPLEMENTATION STATUS

### ? COMPLETED FEATURES
- [x] Core enlistment dialog system
- [x] Army creation and joining using game APIs
- [x] Party illusion system (hiding/camera)
- [x] Clean army leaving system
- [x] Save/load state persistence
- [x] Wage system integration
- [x] Settlement following behavior
- [x] Service-based architecture
- [x] Reflection-based API compatibility
- [x] Battle participation system
- [x] Hostile encounter protection
- [x] UI integration and nameplate hiding
- [x] Professional code cleanup

### ?? READY FOR PRODUCTION
- Complete "Serve as a Soldier" experience
- Seamless lord integration
- Automatic battle participation
- Protection from independent encounters
- Settlement following
- Professional UI hiding

---

## ??? DEVELOPMENT ROADMAP

### ??? Phase 1: Military Career System
- [ ] Rank progression system
- [ ] Merit-based promotions
- [ ] Specialized military roles
- [ ] Equipment standardization by rank

### ?? Phase 2: Advanced Battle Features
- [ ] Formation positioning within army
- [ ] Command chain simulation
- [ ] Battle reward distribution
- [ ] Tactical role assignments

### ?? Phase 3: Faction Integration
- [ ] Multiple army types support
- [ ] Faction-specific military traditions
- [ ] Training and skill development
- [ ] Military mission system

---

## ?? TECHNICAL NOTES

### ?? Game Integration Points
1. **ArmyManagementVM Pattern**: Uses identical army creation logic
2. **CampaignBehaviorBase**: Proper game event registration
3. **SaveableField**: Bannerlord-compatible save system
4. **ConversationSentence**: Native dialog integration
5. **Harmony Patches**: Non-intrusive game modification

### ?? Reflection API Usage
- **SetPartyAiAction.GetActionForEscortingParty**: AI escort behavior
- **Kingdom.CreateArmy**: Army creation using game's method
- **Party.SetAsCameraFollowParty**: Camera control transfer

### ?? Save Data Structure
```csharp
EnlistmentState {
    bool IsEnlisted              // Core enlistment status
    Hero Commander              // Current commander reference
    bool PendingDetach          // Cleanup flag
    bool PlayerPartyWasVisible  // Original visibility state
}
```

### ?? Configuration Constants
- Dialog Priority: 110 (high visibility)
- Message Prefix: "[Enlisted]"
- Main Dialog Hubs: "lord_talk", "hero_main_options"
- Army Type: Army.ArmyTypes.Patrolling

---

## ? TESTING CHECKLIST

### ?? Core Functionality
- [x] Can enlist with various lord types
- [x] Dialog appears in correct conversation hubs
- [x] Army creation works when commander has no army
- [x] Army joining works when commander has existing army
- [x] Party becomes invisible on enlistment
- [x] Camera follows commander party
- [x] Can discharge from service
- [x] Party visibility restores on discharge
- [x] Camera control returns to player

### ??? Settlement Integration
- [x] Follows commander into towns
- [x] Can talk to commander inside settlements
- [x] Maintains enlisted status in settlements
- [x] Proper exit behavior from settlements

### ?? Save/Load System
- [x] Enlistment state persists across saves
- [x] Commander reference survives save/load
- [x] Party visibility state preserved
- [x] No corruption or crashes on load

### ?? Battle Integration
- [x] Participates in battles as army member
- [x] Automatic battle joining
- [x] Proper side selection
- [x] Battle notifications

### ??? Protection System
- [x] Blocks bandit encounters while enlisted
- [x] Blocks enemy faction encounters
- [x] Smart hostile party detection
- [x] Proper encounter cancellation

### ?? UI Integration
- [x] Nameplate hiding when enlisted
- [x] Nameplate restoration when leaving
- [x] Clean visual experience

---

## ?? DEVELOPMENT NOTES

### ?? Success Metrics
- Seamless integration feeling (no "modded" feel)
- No crashes or save corruption
- Intuitive user experience
- Performance equivalent to base game

### ?? Key Learnings
1. **Use Game APIs**: Bannerlord's own systems are best practice
2. **Reflection for Compatibility**: Future-proofs against API changes
3. **Service Architecture**: Maintainable and testable code structure
4. **Fail-Safe Design**: Multiple fallback layers prevent crashes
5. **Clean Production Code**: Professional standards throughout

### ?? Player Experience Goals
- Feel like truly serving in an army
- Maintain player agency while following orders
- Provide meaningful military experience
- Integrate seamlessly with existing gameplay

---

**Last Updated:** [Current Date]  
**Status:** Production Ready  
**Version:** 1.0.0