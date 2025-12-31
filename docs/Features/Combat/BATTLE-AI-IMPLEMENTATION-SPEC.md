# Battle AI & Agent Combat: Master Implementation Spec

**Purpose:** Consolidated implementation specification for the Battle AI Upgrade and Agent Combat systems. This document serves as the master checklist for development, tracking all systems, their dependencies, entry points, and implementation status.

**Scope:** Field battles only. Siege/naval AI is out of scope.

**Core Principle:** Make AI smarter through better coordination and decision-making, not through cheating.

---

## Table of Contents

1. [Architecture Overview](#1-architecture-overview)
2. [Implementation Phases](#2-implementation-phases)
   - 2b. [Edge Cases By Phase](#2b-edge-cases-by-phase)
3. [System Specifications](#3-system-specifications)
4. [API Reference](#4-api-reference)
5. [Configuration & Tuning](#5-configuration--tuning)
6. [Testing & Validation](#6-testing--validation)

---

# 1. Architecture Overview

## 1.1 Layer Hierarchy

```
┌─────────────────────────────────────────────────────────────────────┐
│  BATTLE ORCHESTRATOR (New - Per Team)                               │
│  Coordinates all formations, manages reserves, executes battle plans│
│  Decision Interval: 1-2 seconds                                     │
└─────────────────────────┬───────────────────────────────────────────┘
                          │
┌─────────────────────────▼───────────────────────────────────────────┐
│  TeamAIComponent (Native - Strategy Layer)                          │
│  Selects TacticComponent every ~5 seconds                           │
│  Enhanced via behavior weight nudges from Orchestrator              │
└─────────────────────────┬───────────────────────────────────────────┘
                          │
┌─────────────────────────▼───────────────────────────────────────────┐
│  FormationAI (Native - Behavior Layer)                              │
│  Selects BehaviorComponent per tick                                 │
│  Enhanced via context-aware behavior weights                        │
└─────────────────────────┬───────────────────────────────────────────┘
                          │
┌─────────────────────────▼───────────────────────────────────────────┐
│  HumanAIComponent / AgentDrivenProperties (Native - Agent Layer)    │
│  Individual combat decisions, reactions, targeting                   │
│  Enhanced via context-aware property tuning                          │
└─────────────────────────────────────────────────────────────────────┘
```

## 1.2 Dual Orchestrator Model

Both sides in battle receive independent orchestrators running identical logic. This creates adversarial intelligence where both sides actively try to win.

```
┌─────────────────────┐         ┌─────────────────────┐
│  PLAYER TEAM        │   VS    │  ENEMY TEAM         │
│  ORCHESTRATOR       │         │  ORCHESTRATOR       │
│                     │◄───────►│                     │
│  Same code          │ Observe │  Same code          │
│  Different side     │ React   │  Different side     │
└─────────────────────┘         └─────────────────────┘
```

## 1.3 Activation Gate: Enlisted Only

**CRITICAL:** The Battle AI system ONLY runs when the player is actively enlisted in the mod. If the player is not enlisted (playing as a regular lord, before starting enlisted career, or after discharge), the system is completely disabled and native AI runs unmodified.

### Activation Check
```csharp
bool ShouldActivateBattleAI()
{
    // Only run if player is currently enlisted
    if (!EnlistmentState.IsEnlisted)
        return false;
    
    // Only run in field battles (not siege/naval)
    if (!IsFieldBattle())
        return false;
    
    // Don't run in multiplayer
    if (Mission.Current?.IsMultiplayer == true)
        return false;
    
    return true;
}
```

### Enlisted State Check Points
| Check Point | Action if NOT Enlisted |
|-------------|------------------------|
| `OnMissionBehaviorInitialize` | Skip adding `BattleOrchestratorBehavior` |
| `OnBattleStarted` | Return early, no orchestrator creation |
| Every Orchestrator tick | Early exit if enlisted state changed mid-battle |

### Player Rank Tiers (When Enlisted)

| Rank | Player Role | AI Behavior |
|------|-------------|-------------|
| **T1-T6** | Soldier in NPC formation | Standard AI, player is just another soldier |
| **T7-T9** | Commander with own troops | Player Counter-AI activates |

### Why This Matters
- Players may start a campaign, play as a regular lord, then enlist later
- Players may be discharged and return to regular gameplay
- The Battle AI should NOT affect normal Bannerlord gameplay outside the enlisted experience
- This ensures mod compatibility and allows players to opt-in/opt-out

---

# 2. Implementation Phases

## Phase 1: Foundation (Priority: Critical)

| ID | System | Description | Status | Complexity |
|----|--------|-------------|--------|------------|
| 1.1 | **Enlisted Activation Gate** | Check `EnlistmentState.IsEnlisted` before any AI runs | ⬜ TODO | Low |
| 1.2 | MissionBehavior Entry | `BattleOrchestratorBehavior` hook into mission (if enlisted) | ⬜ TODO | Low |
| 1.3 | Basic Orchestrator Shell | Per-team orchestrator instantiation | ⬜ TODO | Low |
| 1.4 | Battle State Model | Read-only signals from TeamQuerySystem/FormationQuerySystem | ⬜ TODO | Medium |
| 1.5 | Agent Stat Model Override | `EnlistedAgentStatCalculateModel` registration (if enlisted) | ⬜ TODO | Low |
| 1.6 | Configuration Loading | Tunable values from JSON config | ⬜ TODO | Low |

## Phase 2: Agent Combat Enhancements (Priority: High)

| ID | System | Description | Status | Complexity |
|----|--------|-------------|--------|------------|
| 2.1 | Context-Aware Agent Tuning | Adjust AgentDrivenProperties by situation | ⬜ TODO | Medium |
| 2.2 | Coordination Signals | Reduce mob behavior (don't pile on one target) | ⬜ TODO | Medium |
| 2.3 | AI Profiles | Veteran, Elite Guard, Green profiles | ⬜ TODO | Low |
| 2.4 | Skill-Based Property Scaling | Better soldiers fight smarter | ⬜ TODO | Medium |

## Phase 3: Formation Intelligence (Priority: High)

| ID | System | Description | Status | Complexity |
|----|--------|-------------|--------|------------|
| 3.1 | Smart Charge Decisions | Patch BehaviorTacticalCharge to assess targets | ⬜ TODO | Medium |
| 3.2 | Infantry Flanking | Split infantry for envelopment when 1.5x advantage | ⬜ TODO | Medium |
| 3.3 | Threat Assessment Targeting | Score enemies before engaging | ⬜ TODO | Medium |
| 3.4 | Formation Cohesion Levels | Tight/Moderate/Loose based on enemy state | ⬜ TODO | Low |
| 3.5 | Pike/Spear Weapon Discipline | Keep polearms out at proper range | ⬜ TODO | High |
| 3.6 | Multi-Weapon Infantry | Throwing → Melee Polearm → Sidearm progression | ⬜ TODO | High |
| 3.7 | Self-Organizing Ranks | Heavy/elite to front, light to rear | ⬜ TODO | Medium |

## Phase 4: Battle Orchestrator Core (Priority: High)

| ID | System | Description | Status | Complexity |
|----|--------|-------------|--------|------------|
| 4.1 | OODA Decision Loop | Observe → Orient → Decide → Act (1-2 sec) | ⬜ TODO | Medium |
| 4.2 | Strategy Modes | EngageBalanced, DelayDefend, Exploit, Withdraw | ⬜ TODO | Medium |
| 4.3 | Formation Roles | MainLine, Screen, FlankGuard, Reserve assignment | ⬜ TODO | Medium |
| 4.4 | Battle Phases | Forming → Advancing → Engaged → Retreating | ⬜ TODO | Medium |
| 4.5 | Trend Analysis | Track power ratio over time, detect "losing for N seconds" | ⬜ TODO | Medium |

## Phase 5: Tactical Decision Engine (Priority: High)

| ID | System | Description | Status | Complexity |
|----|--------|-------------|--------|------------|
| 5.1 | Threat vs Opportunity Weighing | Multi-factor action scoring | ⬜ TODO | High |
| 5.2 | Archer Targeting Decisions | Target selection by armor/tier/skill | ⬜ TODO | Medium |
| 5.3 | Cavalry Reserve Timing | When to hold, when to commit | ⬜ TODO | High |
| 5.4 | Cavalry Cycle Charging | Charge → Impact → Disengage → Rally → Reform | ⬜ TODO | High |
| 5.5 | Reserve Commitment Logic | Flank collapse, main line support | ⬜ TODO | High |
| 5.6 | Pursuit Decisions | Full pursuit, cavalry only, hold, regroup | ⬜ TODO | Medium |

## Phase 6: Battle Plan Generation (Priority: Medium)

| ID | System | Description | Status | Complexity |
|----|--------|-------------|--------|------------|
| 6.1 | Plan Types | Left Hook, Right Hook, Center Punch, Double Envelopment, Hammer Anvil, Delay | ⬜ TODO | High |
| 6.2 | Plan Selection | Score plans by composition, terrain, power ratio | ⬜ TODO | High |
| 6.3 | Main Effort Designation | Concentrate best troops on primary axis | ⬜ TODO | Medium |
| 6.4 | Formation Objectives | Attack, Pin, Screen, Breakthrough, Flank, Hold | ⬜ TODO | Medium |
| 6.5 | Sequential Objectives | Phase-based cavalry tasking | ⬜ TODO | High |
| 6.6 | Plan Adaptation | Detect success/failure, shift effort | ⬜ TODO | High |

## Phase 7: Reserve & Retreat Management (Priority: Medium)

| ID | System | Description | Status | Complexity |
|----|--------|-------------|--------|------------|
| 7.1 | Reserve Manager | Hold reserves, commit on triggers | ⬜ TODO | Medium |
| 7.2 | Casualty-Based Posture | Engaged → FightingRetreat → TacticalWithdrawal → LastStand | ⬜ TODO | Medium |
| 7.3 | Organized Withdrawal | Covering force, step-by-step fallback | ⬜ TODO | High |
| 7.4 | Rally Points & Regrouping | Post-retreat reformation | ⬜ TODO | Medium |
| 7.5 | Stalemate Prevention | 45-second timer forces action | ⬜ TODO | Low |

## Phase 8: Player Counter-Intelligence (T7+) (Priority: Medium)

| ID | System | Description | Status | Complexity |
|----|--------|-------------|--------|------------|
| 8.1 | Player Formation Tracking | Position, velocity, threat angle | ⬜ TODO | Medium |
| 8.2 | Threat Projection | Predict player position 10-15 seconds ahead | ⬜ TODO | Medium |
| 8.3 | Flank Detection | Multiple signals for flanking threat | ⬜ TODO | Medium |
| 8.4 | Counter-Composition Reserve | Hold counters for player's composition | ⬜ TODO | High |
| 8.5 | Flank Response Tree | Intercept, delay, refuse flank, compact | ⬜ TODO | High |

## Phase 9: Reinforcement Intelligence (Priority: Medium)

| ID | System | Description | Status | Complexity |
|----|--------|-------------|--------|------------|
| 9.1 | Strategic Wave Timing | Hold/rush reinforcements based on context | ⬜ TODO | High |
| 9.2 | Formation-Aware Assignment | Route to formations by tactical need | ⬜ TODO | Medium |
| 9.3 | Quality Distribution | Spread elites/veterans/regulars | ⬜ TODO | Medium |
| 9.4 | Spawn Point Tactics | Fight near your spawn for advantage | ⬜ TODO | Medium |
| 9.5 | Reinforcement Integration | Form up, morale boost, cohesion | ⬜ TODO | Medium |
| 9.6 | Cinematic Staging | Staging formations, skirmish phase | ⬜ TODO | High |

## Phase 10: Battle Pacing & Cinematics (Priority: Low)

| ID | System | Description | Status | Complexity |
|----|--------|-------------|--------|------------|
| 10.1 | Deliberate Approach Speed | Form up before blobbing forward | ⬜ TODO | Low |
| 10.2 | Skirmish Phase | Probing attacks before full engagement | ⬜ TODO | Medium |
| 10.3 | Dramatic Moments | Champion duels, last stands, banner drama | ⬜ TODO | High |
| 10.4 | Morale-Driven Endings | Rout cascades, ordered withdrawal | ⬜ TODO | Medium |

## Phase 11: Terrain & Morale Exploitation (Priority: Low)

| ID | System | Description | Status | Complexity |
|----|--------|-------------|--------|------------|
| 11.1 | High Ground Strategy | Seek and hold elevation | ⬜ TODO | Medium |
| 11.2 | Choke Point Exploitation | Funnel enemies through narrow terrain | ⬜ TODO | Medium |
| 11.3 | Forest/Difficult Terrain | Use terrain to negate cavalry | ⬜ TODO | Medium |
| 11.4 | Terrain-Aware Battle Plans | Select plans based on available terrain | ⬜ TODO | High |
| 11.5 | Morale Reading | Target low-morale formations | ⬜ TODO | Low |
| 11.6 | Rout Triggering | Focus fire to break enemy morale | ⬜ TODO | Medium |
| 11.7 | Protecting Own Morale | Shield fragile formations | ⬜ TODO | Medium |
| 11.8 | Strategic Withdrawal Before Collapse | Disengage before rout | ⬜ TODO | Medium |

## Phase 12: Formation Doctrine System (Priority: Medium)

| ID | System | Description | Status | Complexity |
|----|--------|-------------|--------|------------|
| 12.1 | Battle Scale Detection | Lord Party (50-200) vs Army Battle (400+) | ⬜ TODO | Low |
| 12.2 | Formation Count Logic | 2-3 for small, 4-6 for large battles | ⬜ TODO | Medium |
| 12.3 | Formation Doctrines | Single Deep Line, Three-Wing, Extended Thin, Hammer-Anvil | ⬜ TODO | High |
| 12.4 | Line Depth Decisions | 2-4 ranks based on numbers/threat | ⬜ TODO | Medium |
| 12.5 | Counter-Formation Tactics | Detect enemy doctrine, select counter | ⬜ TODO | High |
| 12.6 | Troop Distribution | Spread elites, quality balance across formations | ⬜ TODO | Medium |
| 12.7 | Spawn Point Strategy | Fight near your spawn for reinforcement advantage | ⬜ TODO | Medium |

## Phase 13: Unit Type Formations (Priority: Medium)

| ID | System | Description | Status | Complexity |
|----|--------|-------------|--------|------------|
| 13.1 | Infantry Formation Shapes | Line, Shield Wall, Loose, Square, Circle | ⬜ TODO | Medium |
| 13.2 | Archer Formation Shapes | Line, Loose, Scatter, Staggered behind infantry | ⬜ TODO | Medium |
| 13.3 | Cavalry Formation Shapes | Line, Skein/Wedge, Loose | ⬜ TODO | Medium |
| 13.4 | Formation Positioning | Combined arms: archers behind, cavalry on flanks | ⬜ TODO | Medium |
| 13.5 | Dynamic Formation Switching | Auto-switch based on threat (square vs cavalry) | ⬜ TODO | High |
| 13.6 | Width and Depth Control | Adjust frontage to match/exceed enemy | ⬜ TODO | Medium |
| 13.7 | Formation Facing/Movement | Maintain facing during advance | ⬜ TODO | Low |
| 13.8 | Multi-Formation Coordination | All formations move as coherent army | ⬜ TODO | High |

## Phase 14: Intelligent Formation Organization (Priority: Medium)

| ID | System | Description | Status | Complexity |
|----|--------|-------------|--------|------------|
| 14.1 | Front-Line Scoring | Calculate suitability for front rank (armor, shield, tier) | ⬜ TODO | Medium |
| 14.2 | Flank Spillover | Rear troops work flanks when line stable | ⬜ TODO | Medium |
| 14.3 | Gap Filling Logic | Middle ranks step up when front-line falls | ⬜ TODO | Medium |
| 14.4 | Formation Safeguards | Max deviation distance, no chase, instant recall | ⬜ TODO | Medium |
| 14.5 | Positional Combat Behavior | Front aggressive, rear defensive/ranged | ⬜ TODO | Medium |

## Phase 15: Plan Execution & Anti-Flip-Flop (Priority: High)

| ID | System | Description | Status | Complexity |
|----|--------|-------------|--------|------------|
| 15.1 | Plan Execution State Machine | Forming → Advancing → Engaging → Exploiting → Pursuing | ⬜ TODO | High |
| 15.2 | Anti-Flip-Flop Rules | 30s minimum commitment, 20s cooldown, max 2 changes/60s | ⬜ TODO | High |
| 15.3 | Phase-Based Gating | Allow changes at natural phase transitions | ⬜ TODO | Medium |
| 15.4 | Legitimate Change Detection | Catastrophic failure, enemy major shift, new opportunity | ⬜ TODO | High |
| 15.5 | Enemy Composition Recognition | Infantry Horde, Archer Heavy, Cavalry Heavy, Horse Archers, etc. | ⬜ TODO | High |
| 15.6 | Defensive Counter-Formations | Auto-select counters based on enemy composition | ⬜ TODO | High |
| 15.7 | Commitment Timing | When to engage vs when to delay | ⬜ TODO | Medium |

## Phase 16: Agent-Level Combat Director (Priority: Low)

| ID | System | Description | Status | Complexity |
|----|--------|-------------|--------|------------|
| 16.1 | Champion Duel System | Hero vs hero dramatic fights | ⬜ TODO | High |
| 16.2 | Last Stand Scenarios | Dramatic final moments for surrounded units | ⬜ TODO | Medium |
| 16.3 | Banner Bearer Drama | Protect/capture banner events | ⬜ TODO | Medium |
| 16.4 | Small Squad Actions | 2-5 man coordinated assaults | ⬜ TODO | Medium |
| 16.5 | Integration with Formation AI | Agent drama respects formation orders | ⬜ TODO | Medium |

## Phase 17: Coordinated Retreat (Priority: Medium)

| ID | System | Description | Status | Complexity |
|----|--------|-------------|--------|------------|
| 17.1 | Retreat Decision Logic | When to retreat vs fight to death | ⬜ TODO | Medium |
| 17.2 | Covering Force (Rearguard) | Designate units to cover retreat | ⬜ TODO | High |
| 17.3 | Step-by-Step Withdrawal | Bounds: move, cover, move | ⬜ TODO | High |
| 17.4 | Preserving Force | Minimize casualties when battle is lost | ⬜ TODO | Medium |
| 17.5 | Rally Points | Regroup after retreat | ⬜ TODO | Medium |

## Phase 18: Reinforcement Details (Priority: Low)

| ID | System | Description | Status | Complexity |
|----|--------|-------------|--------|------------|
| 18.1 | Big Wave Strategic Impact | 100+ troops as major battlefield event | ⬜ TODO | Medium |
| 18.2 | Wave Coordination Between Sides | Time waves to counter enemy waves | ⬜ TODO | High |
| 18.3 | Desperation Waves | All-in spawning when losing badly | ⬜ TODO | Medium |
| 18.4 | Spawn Point Defense/Attack | Protect your spawn, harass theirs | ⬜ TODO | Medium |
| 18.5 | Merge vs Second Line Decision | Fresh troops join or form new line | ⬜ TODO | High |

---

# 2b. Edge Cases By Phase

Each phase must handle these edge cases. Implementations should include null checks, early returns, and graceful degradation.

## Phase 1: Foundation

| Edge Case | Handling Strategy |
|-----------|-------------------|
| `Mission.Current` is null | Guard all entry points, abort initialization |
| Enlisted state changes during battle load | Re-check before orchestrator tick starts |
| Multiple missions loading in sequence | Clean up previous orchestrator on mission end |
| Mission ends before orchestrator initializes | Register cleanup in `OnMissionEnded` |
| Configuration file missing/corrupt | Use hardcoded defaults, log warning |
| Multiplayer battle detected | Activation gate returns false, skip entirely |
| Save/load during battle | Handle via `OnAfterMissionLoad` hook |
| Team is null or has no agents | Early exit from orchestrator creation |

## Phase 2: Agent Combat Enhancements

| Edge Case | Handling Strategy |
|-----------|-------------------|
| Agent dies during property calculation | Null-check `Agent.State`, skip dead agents |
| Agent has no weapon equipped | Use baseline properties, don't error |
| Agent switches weapons mid-combat | Recalculate properties on weapon change event |
| Horse archer dismounted | Transition from mounted to foot profile |
| Agent is player-controlled | Skip AI property overrides for player agent |
| Agent retreating/routing | Apply retreat-specific behavior (no blocking) |
| Agent in water/falling/ragdoll | Skip property updates until agent stable |
| All agents same skill level | System still works, no special handling needed |

## Phase 3: Formation Intelligence

| Edge Case | Handling Strategy |
|-----------|-------------------|
| Formation has 0 agents | Skip formation entirely, mark as invalid |
| Formation has 1 agent | Treat as solo unit, limited tactics available |
| Formation disbanded mid-charge | Abort charge, reassign agents to other formations |
| All cavalry dead | No cavalry charges possible, remove from options |
| Mixed formation (infantry + cavalry) | Use dominant type for formation behavior |
| Formation leader dies | Native handles succession, orchestrator continues |
| Pike infantry have no pikes (dropped/broken) | Fall back to secondary weapons, skip pike logic |
| Multi-weapon soldier all weapons dropped | Agent becomes non-combatant, low priority |
| Target formation destroyed mid-charge | Abort charge, acquire new target |
| Self-organizing with identical troops | Random tie-breaking, any order valid |

## Phase 4: Battle Orchestrator Core

| Edge Case | Handling Strategy |
|-----------|-------------------|
| Battle ends during OODA loop | Check `Mission.IsEnding` at start of each tick |
| All formations destroyed | Team eliminated, stop orchestrator |
| Team has no agents remaining | Early exit, no decisions to make |
| Power ratio is NaN/infinity | Guard division by zero, default to 1.0 |
| Enemy team retreated (no targets) | Transition to consolidation/pursuit |
| Trend data insufficient (<5 samples) | Use current snapshot, mark trend as unknown |
| Strategy mode indeterminate | Default to EngageBalanced |
| Enemy orchestrator data unavailable | Act on observable enemy behavior only |

## Phase 5: Tactical Decision Engine

| Edge Case | Handling Strategy |
|-----------|-------------------|
| No archers on team | Skip archer-specific targeting logic |
| No cavalry on team | Skip cavalry reserve/timing logic |
| All enemies same armor/tier | Random target selection, equal priority |
| Cavalry cannot reach target (terrain) | Terrain check before charge commit |
| Reserve already committed, commit called again | Ignore duplicate commit commands |
| Pursuit targets teleport/despawn | End pursuit, transition to consolidation |
| Cycle charging cavalry destroyed mid-cycle | Cancel cycle, no further cycles |
| No valid charge targets | Hold cavalry, wait for opportunity |
| Reserve commit threshold never reached | Continue holding until battle outcome clear |

## Phase 6: Battle Plan Generation

| Edge Case | Handling Strategy |
|-----------|-------------------|
| Terrain data unavailable | Use flat-terrain default plan |
| No valid plan matches conditions | Fall back to balanced engagement |
| Plan becomes invalid mid-execution | Trigger plan adaptation early |
| Main effort formation destroyed | Reassign main effort to next strongest |
| All formations assigned same objective | Spread objectives to avoid overlap |
| Sequential phase formation killed | Skip phase, continue to next |
| Plan scored identically (tie) | Random tie-breaking |
| Insufficient troops for selected plan | Scale down plan complexity |
| Enemy composition unknown | Use balanced counter-plan |

## Phase 7: Reserve & Retreat Management

| Edge Case | Handling Strategy |
|-----------|-------------------|
| No reserve designated (all committed) | Mark reserve as empty, skip reserve logic |
| Rally point unreachable (terrain) | Select nearest accessible point |
| Covering force destroyed while covering | Main force continues retreat, accept losses |
| Stalemate timer triggers but enemy routing | Cancel stalemate, transition to pursuit |
| Last stand triggered but battle won | Cancel last stand, transition to exploitation |
| Retreat ordered with only 1 formation | That formation becomes rearguard AND retreating |
| All formations below retreat threshold | Team-wide retreat, no covering force |
| Rally point occupied by enemies | Select alternate rally point |

## Phase 8: Player Counter-Intelligence (T7+)

| Edge Case | Handling Strategy |
|-----------|-------------------|
| Player T7+ but in simulation battle | Check if player is actually commanding |
| Player changes formation mid-battle | Update tracking on formation change |
| Player dismounts/mounts | Recalculate threat projection (speed change) |
| Player dead but battle continues | Stop tracking player, normal AI behavior |
| Player far from main battle (scouting) | Reduce threat priority, focus on main battle |
| Player has no troops (solo) | Treat as single high-value target |
| Player controlled by AI (auto-battle) | May not trigger T7+ logic at all |
| Cannot determine player formation | Fall back to tracking player agent directly |
| Player is on AI's team | Skip counter-AI entirely (same team) |

## Phase 9: Reinforcement Intelligence

| Edge Case | Handling Strategy |
|-----------|-------------------|
| No more reinforcements available | Mark reinforcement system complete |
| Spawn point occupied by enemies | Delay spawn or force spawn with protection |
| Reinforcements spawn into melee | Immediate engagement mode, skip forming up |
| All formations at capacity | Create new formation for reinforcements |
| Elite distribution when no elites remain | Distribute available quality fairly |
| Wave coordination with no enemy waves | Use own tactical timing only |
| Spawn blocked by terrain | Engine handles, no mod action needed |
| Reinforcement wave during retreat | Consider delaying wave or emergency spawn |

## Phase 10: Battle Pacing & Cinematics

| Edge Case | Handling Strategy |
|-----------|-------------------|
| Battle ends during skirmish phase | Allow natural conclusion, no forced drama |
| No heroes for champion duels | Skip duel system for this battle |
| All banners already captured | Skip banner drama |
| Dramatic moment interrupted by rout | Let rout take priority, skip drama |
| Very short battle (<30 seconds) | Skip pacing system, natural conclusion |
| No suitable last stand candidates | Skip last stand scenarios |
| Champion killed instantly (one-shot) | No duel established, continue normally |
| Dramatic moment would affect outcome | Never let drama override tactical reality |

## Phase 11: Terrain & Morale Exploitation

| Edge Case | Handling Strategy |
|-----------|-------------------|
| No terrain features on map | Use flat-terrain behaviors |
| Completely flat battlefield | Skip terrain-seeking logic |
| Morale system disabled (mod conflict) | Check for morale values, skip if unavailable |
| All enemies max morale | Don't rely on morale breaks, use attrition |
| Terrain data stale after battle moves | Periodic terrain re-query (every 10s) |
| High ground occupied by enemy | Contest or find alternative position |
| Choke point benefits enemy more | Avoid that choke point |
| Forest terrain crashes pathfinding | Use simpler pathfinding, avoid dense areas |

## Phase 12: Formation Doctrine System

| Edge Case | Handling Strategy |
|-----------|-------------------|
| Extreme imbalance (10 vs 500) | Use guerrilla/skirmish doctrine |
| All troops same type | Simplified doctrine, no combined arms |
| Reinforcements change battle scale | Re-evaluate doctrine on major scale change |
| Counter-doctrine for unknown doctrine | Use balanced counter |
| Formation count exceeds troops | Reduce formation count to match |
| Single-formation army | Skip multi-formation doctrine |
| Doctrine requires cavalry but none available | Fall back to infantry-only doctrine |
| Battle scale ambiguous (250 troops) | Use medium-scale rules |

## Phase 13: Unit Type Formations

| Edge Case | Handling Strategy |
|-----------|-------------------|
| Infantry have no shields | Cannot use Shield Wall, use Line/Loose |
| Fewer than 80 troops | Cannot form Square, use tighter formations |
| Mixed cavalry/infantry in formation | Native handles, respect native assignment |
| Formation switching during charge | Delay switch until charge complete |
| Multi-formation coordination with destroyed formations | Remove from coordination, continue with survivors |
| All archers killed | Skip archer positioning logic |
| All cavalry killed | Skip cavalry flank positioning |
| Formation shape not supported (modded) | Fall back to Line formation |

## Phase 14: Intelligent Formation Organization

| Edge Case | Handling Strategy |
|-----------|-------------------|
| All troops identical equipment score | Random front-line assignment |
| Formation has only ranged troops | Use loose formation, all treated as ranged |
| Front line all killed, only ranged remain | Ranged become front line by necessity |
| Gap filling when no reserves available | Accept gaps, maintain formation as-is |
| Tiny formation (3 or fewer troops) | Skip organization logic, all front-line |
| Score calculation returns same value | Tie-break by agent index |
| Equipment data unavailable | Use troop tier as fallback score |

## Phase 15: Plan Execution & Anti-Flip-Flop

| Edge Case | Handling Strategy |
|-----------|-------------------|
| Battle starts in Engaging phase (close spawn) | Skip Forming/Advancing phases |
| Catastrophic failure false positive | Require sustained failure (10+ seconds) |
| Anti-flip-flop locks needed change | Emergency override for genuine catastrophe |
| Enemy composition changes (reinforcements) | Re-evaluate composition periodically |
| Plan execution with missing formations | Reassign objectives to remaining formations |
| State machine in invalid state | Reset to Engaging, log error |
| Phase transition during plan change cooldown | Allow changes at transitions |
| All phases complete but battle continues | Stay in Pursuing/Consolidating |

## Phase 16: Agent-Level Combat Director

| Edge Case | Handling Strategy |
|-----------|-------------------|
| Hero killed during duel | End duel, other hero returns to formation |
| No suitable duel opponent | Skip duel system |
| Last stand with only 1 agent | Treat as heroic last stand (solo) |
| Banner bearer killed before drama | Skip banner drama for this bearer |
| Small squad all killed mid-action | End action, no completion callback |
| Drama conflicts with formation orders | Formation orders take priority |
| Multiple duels requested | Only one active duel per side at a time |
| Champion is player agent | Skip AI champion behavior, player controls |

## Phase 17: Coordinated Retreat

| Edge Case | Handling Strategy |
|-----------|-------------------|
| Rearguard destroyed while covering | Main force accepts losses, continues retreat |
| Rally point in enemy territory | Find nearest safe rally point |
| Retreat blocked by terrain | Find alternate retreat path |
| Step-by-step with only one formation | Single formation does fight-retreat-fight |
| Retreat decision but battle instantly ends | No action needed, battle over |
| Covering force refuses to hold | Force hold behavior, override morale |
| Rally point changes during retreat | Notify all formations of new rally |
| Enemy doesn't pursue | Stop retreat, transition to consolidation |

## Phase 18: Reinforcement Details

| Edge Case | Handling Strategy |
|-----------|-------------------|
| Desperation wave when already winning | Skip desperation, use normal timing |
| Spawn point captured by enemy | Force spawn with immediate combat flag |
| Merge with incompatible formation types | Create new formation instead |
| Wave coordination with no enemy waves | Self-paced wave timing |
| Big wave arrives but battle over | Wave doesn't spawn, battle ends |
| Multiple desperation triggers | Only one desperation wave per battle |
| Merge decision when formations destroyed | Create new formation for wave |
| All spawn points blocked | Engine handles, accept spawn location |

---

# 3. System Specifications

## 3.1 BattleOrchestrator

**Purpose:** Commander-layer AI that coordinates all formations for a team.

### Files
- `src/Features/Combat/BattleOrchestrator.cs`
- `src/Features/Combat/BattleOrchestratorBehavior.cs`

### Inputs (Read-Only)
| Source | Data |
|--------|------|
| `Team.QuerySystem` | Power ratios, composition, positions |
| `Formation.QuerySystem` | Local power, casualties, suppression |
| `TacticalPosition/Region` | Terrain features (if present) |

### Outputs (Bounded Interventions)
| Output | Description |
|--------|-------------|
| Strategy Mode | EngageBalanced, DelayDefend, Exploit, Withdraw |
| Formation Roles | MainLine, Screen, FlankGuard, Reserve |
| Behavior Weight Nudges | Adjust formation behavior weights |
| Reserve Commands | Commit, hold, intercept |

### Decision Loop (OODA)
```
Every 1-2 seconds:
1. OBSERVE: Capture signals, update rolling trend history
2. ORIENT: Estimate frontline, sample local superiority, detect threats
3. DECIDE: Choose strategy mode (with hysteresis), pick main effort
4. ACT: Apply bounded interventions with cooldowns
```

### Key Thresholds
| Threshold | Value | Usage |
|-----------|-------|-------|
| Decision Interval | 1.5 sec | OODA loop tick |
| Strategy Hysteresis | 1.3x | Prevent flip-flopping |
| Retreat Threshold | 0.4 | RemainingPowerRatio |
| Reserve Commit (Casualties) | 25% | Main line casualties |
| Reserve Commit (Opportunity) | Exposed flank | Enemy flank open |

---

## 3.2 Agent Combat AI

**Purpose:** Individual soldier combat behavior tuning via AgentDrivenProperties.

### Files
- `src/Features/Combat/EnlistedAgentStatCalculateModel.cs`
- `src/Features/Combat/AgentCombatTuner.cs`

### Core Properties (40+ total, key ones listed)

#### Melee Combat
| Property | Range | Effect |
|----------|-------|--------|
| `AIBlockOnDecideAbility` | 0.5–0.99 | Blocking decision quality |
| `AIParryOnDecideAbility` | 0.5–0.95 | Parry decision quality |
| `AiRandomizedDefendDirectionChance` | 0.0–1.0 | Wrong block direction % |
| `AIAttackOnDecideChance` | 0.05–0.48 | Attack decision likelihood |
| `AIAttackOnParryChance` | 0.0–0.08 | Counter-attack after parry |

#### Ranged Combat
| Property | Range | Effect |
|----------|-------|--------|
| `AiShooterError` | ~0.008 | Base aiming error |
| `AiRangerLeadErrorMin/Max` | -0.35–0.2 | Lead target error |
| `AiShootFreq` | 0.3–1.0 | Shooting frequency |

#### Reactions
| Property | Range | Effect |
|----------|-------|--------|
| `AiCheckApplyMovementInterval` | 0.05–0.1 | Movement update rate |
| `AiMovementDelayFactor` | 1.0–1.33 | Reaction delay |

### AI Profiles

#### Veteran Profile
```csharp
props.AIBlockOnDecideAbility = 0.92f;
props.AIParryOnDecideAbility = 0.85f;
props.AiRandomizedDefendDirectionChance = 0.15f;
props.AIAttackOnDecideChance = 0.4f;
props.AiShooterError = 0.005f;
props.AiShootFreq = 0.9f;
```

#### Elite Guard Profile
```csharp
props.AIBlockOnDecideAbility = 0.98f;
props.AIParryOnDecideAbility = 0.95f;
props.AiRandomizedDefendDirectionChance = 0.05f;
props.AIAttackOnParryChance = 0.2f;
props.AiCheckApplyMovementInterval = 0.03f;
```

### Context-Aware Tuning Triggers
| Situation | Property Changes |
|-----------|------------------|
| Under ranged attack | Shield probability ↑ |
| Locally outnumbered | Block ↑, Attack ↓ |
| Winning overall | Counter-attack chance ↑ |
| Formation charging | Aggression ↑ |

---

## 3.3 Cavalry Cycle Manager

**Purpose:** Implement proper lance doctrine with charge → impact → disengage → reform cycles.

### Files
- `src/Features/Combat/CavalryCycleManager.cs`

### State Machine
| State | Duration | Formation | Purpose |
|-------|----------|-----------|---------|
| Reserve | Variable | Line | Wait for orders |
| Positioning | 10-20s | Line | Get 60-80m from target |
| Charging | 5-10s | Wedge | Build speed, lances ready |
| Impact | 1-3s | Wedge | Lance damage window |
| Melee | 8-12s MAX | Loose | Finish immediate threats |
| Disengaging | 5-10s | Loose | Break contact |
| Rallying | 10-15s | Loose | Move to rally point |
| Reforming | 8-12s | Line | Prepare for next charge |

### Key Parameters
| Parameter | Value | Rationale |
|-----------|-------|-----------|
| MaxMeleeTime | 12 sec | Don't get bogged down |
| MinChargeDistance | 60m | Need speed for lance impact |
| ReformDistance | 80m | Rally far enough from enemy |
| ReformDuration | 8 sec | Time to tighten formation |

### Weapon-Aware Adjustments
| Lance Ratio | MaxMeleeTime | MinChargeDistance |
|-------------|--------------|-------------------|
| > 60% | 10s | 70m |
| 30-60% | 15s | 60m |
| < 30% | 25s | 40m |

---

## 3.4 Formation Weapon Discipline

**Purpose:** Ensure soldiers use optimal weapons for range (pikes, spears, multi-weapon loadouts).

### Files
- `src/Features/Combat/PikeInfantryManager.cs`
- `src/Features/Combat/MultiWeaponInfantryManager.cs`

### Pike/Spear Distance Rules
| Distance | Weapon | Action |
|----------|--------|--------|
| 6m+ | Pike out | Advance in formation |
| 3-6m | Pike out | HOLD, thrust attacks |
| 1.5-3m | Pike out | Contested range |
| < 1.5m | Switch sidearm | Enemy inside reach |
| > 3m again | Switch back | Re-establish reach |

### Multi-Weapon Priority (Javelin + Spear + Sword)
| Distance | Weapon | Rationale |
|----------|--------|-----------|
| 20m+ | Throwing | Ranged damage |
| 15-20m | Prepare melee | Switch before contact |
| 3-15m | Melee polearm | **Reach advantage zone** |
| 1.5-3m | Keep polearm | Still effective |
| < 1.5m | Sidearm | Close combat |

---

## 3.5 Reserve Manager

**Purpose:** Hold reserves until optimal commit moment.

### Files
- `src/Features/Combat/ReserveManager.cs`

### Commit Triggers
| Trigger | Response |
|---------|----------|
| Main line > 25% casualties | Commit to reinforce |
| Enemy flank exposed | Commit to exploit |
| Enemy reserves engaged (not winning) | Commit to match |
| Battle time > 30s | Allow commitment |

### Reserve Sizing by Situation
| Situation | Reserve Size |
|-----------|--------------|
| Counter already deployed | 0-10% (minimal) |
| Counter not deployed, we have it | 20-30% |
| Player significantly stronger | 30%+ |

---

## 3.6 Battle Plan System

**Purpose:** Generate and execute coordinated battle plans.

### Files
- `src/Features/Combat/BattlePlanSelector.cs`
- `src/Features/Combat/FormationObjectiveAssigner.cs`

### Plan Types
| Plan | Best When | Main Effort |
|------|-----------|-------------|
| Left/Right Hook | Cavalry advantage, flank exposed | Flank axis |
| Center Punch | Infantry advantage, thin center | Center axis |
| Double Envelopment | 1.4x+ numerical advantage | Both flanks |
| Hammer & Anvil | 25%+ cavalry | Infantry pins, cavalry rear |
| Delay | Outnumbered, defensive objective | Screen all |
| Refused Flank | Terrain advantage on one side | Strong side |

### Formation Objectives
| Objective | Behavior | Engagement |
|-----------|----------|------------|
| Attack | Advance aggressively | Full |
| Pin | Engage to hold | Moderate |
| Screen | Delay, don't get destroyed | Light |
| Hold | Defend position | Full, no advance |
| Flank | Maneuver to side | When in position |
| Pursue | Chase fleeing | After rout |

---

## 3.7 Formation Doctrine System

**Purpose:** Organize armies into appropriate formations based on battle scale and composition.

### Files
- `src/Features/Combat/FormationDoctrineManager.cs`
- `src/Features/Combat/BattleScaleDetector.cs`

### Battle Scales
| Scale | Troops on Field | Formations | Characteristics |
|-------|-----------------|------------|-----------------|
| **Lord Party** | 50-200 | 2-3 | Everyone fits, no waves, quick |
| **Small Army** | 200-400 | 4 | Can split infantry, add reserve |
| **Large Army** | 400-500 | 5-6 | Three-wing or classical with reserve |

### Formation Doctrines
| Doctrine | Best For | Structure |
|----------|----------|-----------|
| **Single Deep Line + Reserve** | Medium armies, balanced, defending | 3-4 rank infantry, archers behind, reserve behind |
| **Three-Wing Line** | Large armies, envelopment | Left/Center/Right infantry wings, cavalry flanks |
| **Extended Thin Line** | Outnumbered | Match frontage with thin (2 rank) line |
| **Hammer and Anvil** | Cavalry-heavy (30%+) | Infantry holds, cavalry rear attack |

### Formation Count by Size
```csharp
if (troops < 100) return 2-3;       // Infantry + maybe Archers/Cavalry
if (troops < 200) return 3;         // Infantry + Archers + Cavalry  
if (troops < 400) return 4;         // + Reserve or Flank formation
if (troops >= 400) return 5-6;      // Three-wing or classical
```

---

## 3.8 Unit Type Formations

**Purpose:** Configure infantry/archer/cavalry formation shapes for different situations.

### Files
- `src/Features/Combat/InfantryFormationSelector.cs`
- `src/Features/Combat/ArcherPositioner.cs`
- `src/Features/Combat/CavalryFormationManager.cs`

### Infantry Formations
| Formation | Use Case | Speed | Weakness |
|-----------|----------|-------|----------|
| **Line** | Default combat | 80% | None |
| **Shield Wall** | Advancing into archers, holding | 30% | Flanks, slow to turn |
| **Loose** | Under archer fire (no shields) | 90% | Weak in melee |
| **Square** | Cavalry charging you (80+ troops) | 30% | Can't advance, arrows hurt |
| **Circle** | Surrounded, last stand | 0% | Thin everywhere |

### Infantry Formation Selection
```
1. Surrounded? → Circle
2. Cavalry charging? → Square
3. Under archer fire + have shields? → Shield Wall
4. Under archer fire + no shields? → Loose
5. Default → Line
```

### Archer Positioning
| Position | Distance from Infantry | Notes |
|----------|------------------------|-------|
| Behind infantry | 20-40m | Standard, protected |
| Inside circle | 0m (center) | When surrounded |
| On flank | 30m offset | Independent fire |

### Cavalry Formations by Task
| Task | Formation | Arrangement |
|------|-----------|-------------|
| Reserve/Waiting | Line | 2 ranks, facing enemy |
| Screening | Loose | Spread for coverage |
| Charging infantry | Skein (Wedge) | Concentrated impact |
| Charging archers | Skein or Line | Soft target, either works |
| Pursuit | Loose | Cover ground |
| Counter-cavalry | Line | Width for collision |

---

## 3.9 Plan Execution State Machine

**Purpose:** Execute battle plans through phases with anti-flip-flop protection.

### Files
- `src/Features/Combat/PlanExecutor.cs`
- `src/Features/Combat/FlipFlopPrevention.cs`

### Plan Phases
| Phase | Description | Plan Changes? |
|-------|-------------|---------------|
| **Forming** | Deploy into formation | ✅ Free to pick |
| **Positioning** | Move to engagement position | ❌ Locked |
| **Approach** | Closing distance | ⚠️ If enemy shifts |
| **Contact** | Skirmishing begins | ⚠️ If needed |
| **MainEngagement** | Lines engaged | ❌ Locked (unless catastrophic) |
| **Exploiting** | Breakthrough achieved | ✅ Commit reserves |
| **Pursuing** | Enemy routing | ✅ Pursuit mode |
| **Withdrawing** | We're retreating | ✅ Survival mode |

### Anti-Flip-Flop Rules
| Rule | Value | Purpose |
|------|-------|---------|
| Minimum commitment | 30 sec | Give plan time to work |
| Change cooldown | 20 sec | Prevent rapid switching |
| Max changes in window | 2 per 60s | Detect flip-flopping |
| Enemy change cooldown | 30 sec | Don't respond immediately to enemy change |

### Legitimate Change Triggers
- Phase transition (natural progression)
- Catastrophic failure (40%+ casualties, encircled)
- Major enemy tactical shift (committed reserve, stance change)
- New major opportunity (gap in enemy line, commander killed)

---

## 3.10 Agent-Level Combat Director

**Purpose:** Create dramatic agent-level moments (duels, last stands, banner drama).

### Files
- `src/Features/Combat/AgentCombatDirector.cs`
- `src/Features/Combat/ChampionDuelManager.cs`

### Champion Duel System
| Trigger | Behavior |
|---------|----------|
| Two heroes in proximity | Clear space, focus on each other |
| One hero heavily wounded | Dramatic final clash |
| Hero kills hero | Morale cascade effect |

### Last Stand Scenarios
| Trigger | Behavior |
|---------|----------|
| Formation < 10 troops, surrounded | Form tight circle, fight to death |
| Single hero remaining | Dramatic solo against many |
| Banner bearer alone | Protect banner at all costs |

### Banner Bearer Drama
| Event | Effect |
|-------|--------|
| Banner planted | Morale boost to nearby allies |
| Banner carrier killed | Nearby troop scrambles to recover |
| Banner captured | Major morale hit to owning side |

---

## 3.11 Coordinated Retreat System

**Purpose:** Execute organized withdrawal instead of piecemeal panic routs.

### Files
- `src/Features/Combat/WithdrawalManager.cs`
- `src/Features/Combat/RearguardManager.cs`

### Withdrawal Phases
| Phase | Behavior |
|-------|----------|
| **Covering** | Ranged/cavalry skirmish, infantry begins fallback |
| **Disengaging** | Main body breaks contact |
| **Retreating** | Full retreat, covering force follows |
| **Rallying** | Reform at rally point |

### Rearguard Selection
| Priority | Unit Type | Rationale |
|----------|-----------|-----------|
| 1 | Cavalry | Mobile, can disengage easily |
| 2 | Ranged | Harassment while retreating |
| 3 | Heavy infantry | Can hold briefly if needed |

### Step-by-Step Withdrawal
```
1. Rearguard holds position, main body moves 50m back
2. Main body stops, faces enemy
3. Rearguard breaks contact, moves through main body
4. Rearguard becomes new rear, main body moves 50m back
5. Repeat until safe distance or rally point
```

---

# 4. API Reference

## 4.1 Harmony Patch Targets

| Target | Purpose | Priority |
|--------|---------|----------|
| `TeamAIComponent.MakeDecision()` | Override tactic selection | High |
| `FormationAI.FindBestBehavior()` | Override behavior selection | High |
| `TacticComponent.GetTacticWeight()` | Adjust tactic weights | Medium |
| `BehaviorComponent.GetAiWeight()` | Adjust behavior weights | Medium |
| `BehaviorTacticalCharge.GetAiWeight()` | Smart charge decisions | High |
| `AgentStatCalculateModel.SetAiRelatedProperties()` | Agent tuning | High |

## 4.2 Useful Native APIs

| API | Purpose |
|-----|---------|
| `Team.QuerySystem` | Team-level power, composition |
| `Formation.QuerySystem` | Formation-level data |
| `Formation.AI.SetBehaviorWeight<T>(weight)` | Adjust behaviors |
| `Formation.SetMovementOrder()` | Issue movement orders |
| `Formation.TransferUnits()` | Split formations |
| `Formation.SwitchUnitLocations()` | Reorganize ranks |
| `Agent.AgentDrivenProperties` | Combat AI properties |
| `Agent.UpdateAgentProperties()` | Apply property changes |
| `Mission.GetFleePositionsForSide()` | Retreat destinations |

## 4.3 Query System Properties

### TeamQuerySystem
| Property | Description |
|----------|-------------|
| `MemberCount` / `EnemyUnitCount` | Unit counts |
| `InfantryRatio` / `RangedRatio` / `CavalryRatio` | Composition |
| `TeamPower` / `TotalPowerRatio` | Power metrics |
| `RemainingPowerRatio` | Current/Initial power |
| `GetLocalAllyPower(Vec2)` / `GetLocalEnemyPower(Vec2)` | Local power |

### FormationQuerySystem
| Property | Description |
|----------|-------------|
| `FormationPower` | Formation combat power |
| `LocalPowerRatio` | Local superiority |
| `ClosestEnemyFormation` | Nearest enemy |
| `UnderRangedAttackRatio` | Suppression level |
| `CasualtyRatio` | Losses percentage |

---

# 5. Configuration & Tuning

## 5.1 Config File

**Path:** `ModuleData/Enlisted/battle_ai_config.json`

```json
{
  "orchestrator": {
    "decisionIntervalSec": 1.5,
    "strategyHysteresis": 1.3,
    "retreatThreshold": 0.4
  },
  "reserves": {
    "minBattleTimeBeforeCommit": 30,
    "casualtyCommitThreshold": 0.25,
    "defaultReserveRatio": 0.2
  },
  "cavalry": {
    "maxMeleeTime": 12,
    "minChargeDistance": 60,
    "reformDistance": 80,
    "reformDuration": 8
  },
  "agentProfiles": {
    "veteran": {
      "blockAbility": 0.92,
      "parryAbility": 0.85,
      "wrongBlockChance": 0.15
    },
    "elite": {
      "blockAbility": 0.98,
      "parryAbility": 0.95,
      "wrongBlockChance": 0.05
    }
  },
  "weaponDiscipline": {
    "pikeKeepDistance": 3.0,
    "pikeSwitchDistance": 1.5,
    "throwingStopDistance": 15
  },
  "stalemate": {
    "maxDurationSec": 45,
    "noActivityThresholdSec": 20
  }
}
```

## 5.2 Difficulty Scaling

| Difficulty | AI Level Cap | Property Modifier |
|------------|--------------|-------------------|
| Very Easy | 10% | Base × 0.7 |
| Easy | 21% | Base × 0.85 |
| Normal | 32% | Base × 1.0 |
| Hard | 64% | Base × 1.15 |
| Very Hard | 96% | Base × 1.3 |

---

# 6. Testing & Validation

## 6.1 Acceptance Criteria

### Enlisted-Only Activation (Critical)
- [ ] System completely disabled when player NOT enlisted
- [ ] No BattleOrchestratorBehavior added in non-enlisted battles
- [ ] No AgentStatCalculateModel override in non-enlisted battles
- [ ] Native AI unchanged when player is regular lord (pre-enlistment)
- [ ] Native AI unchanged when player is discharged (post-enlistment)
- [ ] Battles work correctly if player enlists/discharges mid-campaign

### Orchestrator
- [ ] Outnumbered → hold/refuse instead of piecemeal charges
- [ ] Maintains reserve and commits appropriately
- [ ] Exploits local superiority (concentrates force)
- [ ] Withdraws in coordinated fashion
- [ ] Stable intent transitions with logged factors
- [ ] No flip-flopping (respects commitment windows)

### Agent Combat
- [ ] Veteran soldiers demonstrably more effective
- [ ] Context-aware adjustments visible (shield use under fire)
- [ ] Reduced mob behavior (enemies spread across multiple targets)
- [ ] Positional combat (front aggressive, rear defensive)

### Cavalry
- [ ] Lance charges execute with proper distance (60m+)
- [ ] Disengages after 10-15 seconds of melee
- [ ] Reforms before next charge
- [ ] 3-6 effective charges per battle vs native's 1
- [ ] Avoids charging braced spears/shield walls

### Weapon Discipline
- [ ] Pikes kept out until enemy inside 1.5m
- [ ] Multi-weapon soldiers use proper progression
- [ ] Throwing weapons not used in melee range
- [ ] Javelins conserved for melee if no other polearm

### Formation Doctrine
- [ ] Battle scale correctly detected (small/large)
- [ ] Formation count appropriate for army size
- [ ] Doctrines selected based on composition/terrain
- [ ] Troop quality balanced across formations

### Unit Formations
- [ ] Infantry switches to Shield Wall under archer fire
- [ ] Infantry forms Square when cavalry charges (80+ troops)
- [ ] Archers positioned correctly behind infantry
- [ ] Combined arms positioning maintained during advance

### Plan Execution
- [ ] Plans execute through proper phases
- [ ] No flip-flopping between offensive/defensive
- [ ] Catastrophic failures trigger emergency changes
- [ ] Enemy composition correctly recognized and countered

### Coordinated Retreat
- [ ] Organized withdrawal instead of piecemeal rout
- [ ] Rearguard designated and covers retreat
- [ ] Step-by-step bounds executed
- [ ] Rally point reached and formation reformed

### Performance
- [ ] No frame-time spikes in 500+ troop battles
- [ ] < 1ms per tick for orchestrator decisions
- [ ] Formation organization doesn't cause stuttering

## 6.2 Test Scenarios

### Enlisted Activation (Critical)
| Scenario | Expected Behavior |
|----------|-------------------|
| Battle before player enlists | Native AI only, no orchestrator |
| Battle while player enlisted (T1-T6) | Full Battle AI active |
| Battle while player is commander (T7+) | Full Battle AI active + Counter-AI |
| Battle after player discharged | Native AI only, no orchestrator |
| Player enlists mid-campaign | Battle AI activates from next battle |
| Player discharges mid-campaign | Battle AI deactivates from next battle |

### Core Orchestrator
| Scenario | Expected Behavior |
|----------|-------------------|
| 2:1 numerical advantage | Envelopment, cavalry flank |
| 1:2 numerical disadvantage | Refused flanks, fighting retreat |
| Cavalry vs exposed archers | Charge, destroy, reform |
| Cavalry vs spear screen | Hold, wait for opening |
| Player T7+ flanking | AI tracks, intercepts with reserve |
| High casualties (60%+) | Tactical withdrawal or last stand |
| Large battle reinforcements | Staged arrival, tactical integration |

### Formation Doctrine
| Scenario | Expected Behavior |
|----------|-------------------|
| 50 troops vs 50 | 2-3 formations (simple) |
| 400 troops vs 400 | 5-6 formations (three-wing or reserve) |
| Outnumbered 2:1 | Extended thin line to prevent flanking |
| 40%+ cavalry | Hammer-anvil doctrine selected |

### Plan Execution
| Scenario | Expected Behavior |
|----------|-------------------|
| Plan fails after 30s | Adapt (reinforce or shift axis) |
| Enemy changes stance | Wait 30s before responding |
| Rapid plan changes | Flip-flop detection locks changes |
| Phase transition | Plan changes allowed |
| Catastrophic failure | Emergency plan change allowed |

### Unit Type Formations
| Scenario | Expected Behavior |
|----------|-------------------|
| Infantry under archer fire | Shield Wall (if shields) or Loose |
| Cavalry charging infantry | Square formation (if 80+ troops) |
| Surrounded | Circle formation |
| Combined arms positioning | Archers 20-40m behind, cavalry on flanks |

### Weapon Discipline
| Scenario | Expected Behavior |
|----------|-------------------|
| Pike infantry vs approaching enemy | Pike out at 6m+, switch at <1.5m |
| Javelin + spear troops | Throw javelins 20m+, melee spear 3-15m, sword <1.5m |
| Pike formation breaking up | Immediate recall, tighten formation |

### Enemy Composition Recognition
| Enemy Type | Counter Response |
|------------|------------------|
| Infantry Horde | Standard line, let archers attrit |
| Archer Heavy | Shield wall advance, cavalry rush archers |
| Cavalry Heavy | Square/spear screen, protect archers |
| Horse Archers | Compact formation, wait for them to close |
| Pike Wall | Flank with cavalry, don't charge front |
| Shield Wall | Flank, or archer fire to soften |

### Coordinated Retreat
| Scenario | Expected Behavior |
|----------|-------------------|
| 40%+ casualties, can disengage | Tactical withdrawal toward spawn |
| 60%+ casualties, surrounded | Last stand formation |
| Retreat ordered | Rearguard covers, step-by-step fallback |
| Reached rally point | Reform and reassess |

## 6.3 Logging Requirements

All orchestrator decisions must be logged:
```
[BattleAI] Team:Defender Strategy:DelayDefend→Exploit (PowerRatio:1.2, EnemyFlankExposed:true)
[BattleAI] Cavalry:Formation3 State:Charging→Impact Target:EnemyArchers
[BattleAI] Reserve:Formation5 Commit:MainLineCollapsing (Casualties:0.27)
```

---

# Implementation Dependency Graph

```
Phase 1: Foundation
    ├── 1.1 ENLISTED ACTIVATION GATE (blocks all if not enlisted)
    │       │
    ├── 1.2 MissionBehavior Entry (if enlisted)
    └── 1.3 Orchestrator Shell
            │
            ├── 1.4 Battle State Model ─────────────────┐
            └── 1.5 Agent Stat Model ─┐                 │
                                      │                 │
Phase 2: Agent Combat ◄───────────────┘                 │
    ├── 2.1 Context-Aware Tuning                        │
    ├── 2.2 Coordination Signals                        │
    └── 2.3/2.4 Profiles & Scaling                      │
                                                        │
Phase 3: Formation Intel ◄──────────────────────────────┤
    ├── 3.1 Smart Charge                                │
    ├── 3.5 Pike Discipline                             │
    └── 3.7 Self-Organizing Ranks                       │
                                                        │
Phase 4: Orchestrator Core ◄────────────────────────────┘
    ├── 4.1 OODA Loop
    ├── 4.2 Strategy Modes
    └── 4.4 Battle Phases
            │
            ├──────────────────────┐
            │                      │
Phase 5: Tactical Engine           Phase 6: Battle Plans ◄── Phase 15: Plan Execution
    ├── 5.3 Cavalry Timing         ├── 6.1 Plan Types         ├── 15.1 State Machine
    ├── 5.4 Cycle Charging         └── 6.3 Main Effort        └── 15.2 Anti-Flip-Flop
    └── 5.5 Reserve Logic                  │
            │                              │
            └──────────────┬───────────────┘
                           │
Phase 7: Reserve & Retreat ◄── Phase 17: Coordinated Retreat
    ├── 7.1 Reserve Manager      ├── 17.2 Rearguard
    └── 7.3 Organized Withdrawal └── 17.3 Step-by-Step
            │
Phase 8: Player Counter-AI (T7+ only) ◄
    ├── 8.1 Player Tracking
    └── 8.4 Counter-Composition
            │
            ├──────────────────────────────────────┐
            │                                      │
Phase 12: Formation Doctrine                Phase 13: Unit Type Formations
    ├── 12.1 Battle Scale                       ├── 13.1-13.3 Shapes
    ├── 12.3 Doctrines                          └── 13.5 Dynamic Switching
    └── 12.5 Counter-Formation                         │
            │                                          │
            └──────────────────────────────────────────┘
                           │
Phase 14: Formation Organization ◄
    ├── 14.1 Front-Line Scoring
    ├── 14.3 Gap Filling
    └── 14.5 Positional Combat
            │
Phase 9-11: Core Polish ◄
    ├── 9: Reinforcement Intelligence
    ├── 10: Battle Pacing & Cinematics
    └── 11: Terrain & Morale Exploitation
            │
Phase 16-18: Advanced Polish ◄
    ├── 16: Agent-Level Combat Director (Duels, Drama)
    └── 18: Reinforcement Details (Waves, Desperation)
```

---

# Implementation Summary

| Category | Phases | Work Items | Priority |
|----------|--------|------------|----------|
| **Foundation** | 1 | 6 | Critical |
| **Core Combat** | 2-5 | 24 | High |
| **Battle Planning** | 6, 15 | 13 | High |
| **Reserve/Retreat** | 7, 17 | 10 | Medium |
| **Counter-AI** | 8 | 5 | Medium |
| **Formation Systems** | 12-14 | 19 | Medium |
| **Unit Formations** | 13 | 8 | Medium |
| **Reinforcements** | 9, 18 | 11 | Medium |
| **Polish/Drama** | 10-11, 16 | 13 | Low |
| **TOTAL** | **18 Phases** | **~109 Items** | — |

---

# Source Documents

- `docs/Features/Combat/battle-ai-plan.md` - Full design document (13,000+ lines, 21 parts)
- `docs/Features/Combat/agent-combat-ai.md` - Agent-level AI reference (486 lines)
- `docs/Features/Combat/formation-assignment.md` - Formation placement for enlisted soldiers
- `docs/Features/Combat/training-system.md` - Training and XP progression

---

# Coverage Matrix

This spec covers all 21 parts of the battle-ai-plan.md:

| Part | Topic | Phase(s) |
|------|-------|----------|
| 1 | Native AI Analysis | Architecture Overview, API Reference |
| 2 | Identified Gaps | (Context for design, not implementation) |
| 3 | Modern AI Techniques | (Techniques inform implementation) |
| 4 | Battle Orchestrator Proposal | Phase 4 |
| 5 | Tactical Enhancements | Phases 2, 3, 5 |
| 6 | Modding Entry Points | API Reference |
| 7 | Player Counter-Intelligence | Phase 8 |
| 8 | Dual Orchestrator Architecture | Architecture 1.2 |
| 9 | AI vs AI Battle Intelligence | Phases 4, 5 |
| 10 | Tactical Decision Engine | Phase 5 |
| 11 | Agent Formation Behavior | Phases 3, 7, 14 |
| 12 | Intelligent Formation Organization | Phase 14 |
| 13 | Formation Doctrine System | Phase 12 |
| 14 | Battle Plan Generation | Phases 6, 15 |
| 15 | Unit Type Formations | Phase 13 |
| 16 | Terrain Exploitation | Phase 11 |
| 17 | Morale Exploitation | Phase 11 |
| 18 | Coordinated Retreat | Phases 7, 17 |
| 19 | Battle Pacing and Cinematics | Phase 10 |
| 20 | Reinforcement Intelligence | Phases 9, 18 |
| 21 | Agent-Level Combat Director | Phase 16 |

---

**Last Updated:** 2025-12-31  
**Status:** Specification Complete (108 items across 18 phases), Implementation Not Started
