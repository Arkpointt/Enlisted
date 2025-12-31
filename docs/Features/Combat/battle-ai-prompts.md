# Battle AI Implementation Prompts

**Summary:** Copy-paste prompts for each implementation phase of the Battle AI system. Each prompt is designed for a NEW AI chat session with full context recovery.

**Status:** 📋 Reference
**Last Updated:** 2025-12-31
**Related Docs:** [BATTLE-AI-IMPLEMENTATION-SPEC.md](BATTLE-AI-IMPLEMENTATION-SPEC.md), [battle-ai-plan.md](battle-ai-plan.md), [agent-combat-ai.md](agent-combat-ai.md), [BLUEPRINT](../../BLUEPRINT.md)

---

## How to Use These Prompts

Each phase prompt is **self-contained for a fresh AI chat**. Every prompt includes:
1. **Prerequisites** - What must exist before starting
2. **Context Recovery** - How the new AI verifies previous work
3. **Tasks** - What to implement
4. **Edge Cases** - Critical scenarios to handle
5. **Acceptance Criteria** - Definition of done
6. **Handoff Notes Template** - What to capture for the next AI

**Start each new chat by copying the entire prompt block (inside the triple backticks).**

---

## Index

| Phase | Description | Model | Chat Strategy | Status |
|-------|-------------|-------|---------------|--------|
| [Phase 1](#phase-1-foundation) | Foundation (activation gate, hooks) | Opus 4 | 🔒 Standalone | ⬜ TODO |
| [Phase 2](#phase-2-agent-combat-enhancements) | Agent Combat AI tuning | Opus 4 | 🔒 Standalone | ⬜ TODO |
| [Phase 3](#phase-3-formation-intelligence) | Formation-level tactics | Opus 4 | 🔒 Standalone | ⬜ TODO |
| [Phase 4](#phase-4-battle-orchestrator-core) | Orchestrator OODA loop | Opus 4 | 🔒 Standalone | ⬜ TODO |
| [Phase 5](#phase-5-tactical-decision-engine) | Cavalry, reserves, targeting | Opus 4 | 🔒 Standalone | ⬜ TODO |
| [Phase 6](#phase-6-battle-plan-generation) | Plan types, selection | Opus 4 | 🔒 Standalone | ⬜ TODO |
| [Phase 7](#phase-7-reserve--retreat-management) | Reserve & retreat | Sonnet 4 | 🔒 Standalone | ⬜ TODO |
| [Phase 8](#phase-8-player-counter-intelligence-t7) | T7+ counter-AI | Opus 4 | 🔒 Standalone | ⬜ TODO |
| [Phase 9-11](#phases-9-11-polish-combined) | Reinforcements, pacing, terrain | Sonnet 4 | ⚡ COMBINE | ⬜ TODO |
| [Phase 12-14](#phases-12-14-formation-systems-combined) | Formation doctrine, shapes, organization | Opus 4 | ⚡ COMBINE | ⬜ TODO |
| [Phase 15](#phase-15-plan-execution--anti-flip-flop) | State machine, anti-flip-flop | Opus 4 | 🔒 Standalone | ⬜ TODO |
| [Phase 16-18](#phases-16-18-advanced-polish-combined) | Combat director, retreat, reinforcement | Sonnet 4 | ⚡ COMBINE | ⬜ TODO |

---

## Chat Strategy Recommendations

| Strategy | Phases | Rationale |
|----------|--------|-----------|
| **STANDALONE** | 1, 2, 3, 4 | Core foundation; each phase builds on previous |
| **STANDALONE** | 5, 6 | Complex tactical logic; needs focus |
| **STANDALONE** | 7 | Reserve/retreat is self-contained |
| **STANDALONE** | 8 | Player counter-AI is specialized |
| **COMBINE** | 9-11 | Polish phases; natural continuation |
| **COMBINE** | 12-14 | Formation systems; tightly coupled |
| **STANDALONE** | 15 | State machine is complex |
| **COMBINE** | 16-18 | Advanced polish; related systems |

---

## Model Recommendations

| Phase | Complexity | Model | Est. Time | Rationale |
|-------|------------|-------|-----------|-----------|
| Phase 1 - Foundation | Medium | Claude Opus 4 | 2-3 hours | API hookups, game integration |
| Phase 2 - Agent Combat | High | Claude Opus 4 | 3-4 hours | 40+ properties, profiles |
| Phase 3 - Formation Intel | High | Claude Opus 4 | 3-4 hours | Weapon discipline, self-org |
| Phase 4 - Orchestrator | High | Claude Opus 4 | 4-5 hours | OODA loop, strategy modes |
| Phase 5 - Tactical Engine | High | Claude Opus 4 | 4-5 hours | Cavalry cycles, reserves |
| Phase 6 - Battle Plans | High | Claude Opus 4 | 3-4 hours | Plan generation/selection |
| Phase 7 - Reserve/Retreat | Medium | Claude Sonnet 4 | 2-3 hours | Straightforward logic |
| Phase 8 - Counter-AI | High | Claude Opus 4 | 3-4 hours | Player prediction |
| Phases 9-11 | Medium | Claude Sonnet 4 | 3-4 hours | Polish systems |
| Phases 12-14 | High | Claude Opus 4 | 4-5 hours | Formation doctrine |
| Phase 15 | High | Claude Opus 4 | 3-4 hours | State machine |
| Phases 16-18 | Medium | Claude Sonnet 4 | 3-4 hours | Advanced polish |

**Total estimated time:** ~40-50 hours

---

## Quick Reference: Build & Test

```powershell
# Build command (PowerShell)
cd C:\Dev\Enlisted\Enlisted; dotnet build -c "Enlisted RETAIL" /p:Platform=x64

# Logs output to:
C:\Program Files (x86)\Steam\steamapps\common\Mount & Blade II Bannerlord\Modules\Enlisted\Debugging\

# Decompile reference (API verification):
C:\Dev\Enlisted\Decompile\
```

---

## Critical Project Constraints

Include these in EVERY prompt:

```
═══════════════════════════════════════════════════════════════════════════════
CRITICAL PROJECT CONSTRAINTS (from BLUEPRINT.md)
═══════════════════════════════════════════════════════════════════════════════

GAME TARGET:
- Bannerlord v1.3.13 specifically (not latest version)

API VERIFICATION:
- ALWAYS verify against local decompile FIRST: C:\Dev\Enlisted\Decompile\
- Key assemblies: TaleWorlds.MountAndBlade, TaleWorlds.CampaignSystem, SandBox
- Never rely on external AI docs or outdated API references

PROJECT BUILD:
- Old-style .csproj: Must manually add new .cs files with <Compile Include="..."/>
- Build command: dotnet build -c "Enlisted RETAIL" /p:Platform=x64
- Output: <Bannerlord>/Modules/Enlisted/bin/Win64_Shipping_Client/

LOGGING:
- Use ModLogger class (not Console.WriteLine or Debug.Log)
- Categories: Combat, BattleAI, Formation, etc.
- Output: Modules\Enlisted\Debugging\ (in Bannerlord install folder)
- Format: ModLogger.Info("BattleAI", "message"); ModLogger.Debug/Warn/Error

ENLISTED-ONLY ACTIVATION:
- All Battle AI ONLY runs when player is enlisted
- Check: EnlistmentState.IsEnlisted before any AI logic
- If not enlisted, native AI runs unmodified

CODE QUALITY (RESHARPER + QODANA):
- Follow ReSharper/Rider and Qodana recommendations as strictly as possible
- Fix ALL warnings before considering code complete
- Never suppress warnings with pragmas unless truly necessary
- If suppressing is unavoidable, use [SuppressMessage] with clear Justification
- Common issues to proactively fix:
  - Unused using directives → remove them
  - Redundant namespace qualifiers → use using statements
  - Unused variables/parameters/methods → remove them
  - Redundant default parameter values → omit them
  - PossibleNullReferenceException → add null checks
  - Single-line statements without braces → add braces

COMMENT STYLE:
- Describe WHAT the code does NOW, not when it changed
- NO "Phase X added..." or changelog-style comments
- NO "legacy" or "migration" mentions
- Write professionally and naturally

BRACES:
- ALWAYS use braces for if/for/while/foreach, even single lines
- ✅ if (x) { return y; }
- ❌ if (x) return y;

COMMON PITFALLS TO AVOID:
1. Not adding new files to .csproj → files won't compile
2. Using external API docs instead of decompile → wrong APIs
3. Ignoring ReSharper/Qodana warnings → code quality degrades, PR rejected
4. Missing null checks → crashes
5. Using ChangeHeroGold instead of GiveGoldAction → gold UI breaks
6. Iterating equipment with Enum.GetValues → includes invalid count values
7. Leaving unused code → Qodana flags it, wastes maintenance effort
8. Suppressing warnings with #pragma → User prefers fixing over suppressing

SAVE SYSTEM (if adding persistent state):
- Register new classes in EnlistedSaveDefiner
- Use SaveLoadDiagnostics.SafeSyncData() wrapper
- See src/Mod.Core/SaveSystem/ for examples
```

---

## Key Patterns & Managers

New AI chats should understand these existing systems:

```
EXISTING MANAGERS (use these, don't recreate):
- EnlistmentBehavior.Instance - Enlisted state, lord reference, IsEnlisted
- CompanyNeedsManager.Instance - Supplies, Morale, Rest, Readiness
- EscalationManager.Instance - Discipline, Scrutiny, Officer/Lord/Soldier rep
- ConfigurationManager.Instance - JSON config loading

EXISTING UTILITIES:
- ModLogger - Logging (Info, Debug, Warn, Error)
- NextFrameDispatcher.RunNextFrame() - Deferred operations
- CampaignSafetyGuard.SafeMainHero - Null-safe hero access

FOLDER STRUCTURE FOR NEW FILES:
- src/Features/Combat/ - Battle AI files go here
- src/Features/Combat/Models/ - Data models and enums
- Keep related functionality together

FILE NAMING:
- Use PascalCase for files: BattleOrchestrator.cs, CavalryCycleManager.cs
- Models/enums in Models subfolder: Models/StrategyMode.cs

HARMONY PATCHES (if needed):
- Place in src/Mod.GameAdapters/
- Use [HarmonyPatch(typeof(TargetClass), "MethodName")]
- Prefer Postfix over Prefix when possible
- Log patch application for debugging
```

---

# Phase 1: Foundation

**Goal:** Build core infrastructure - activation gate, MissionBehavior hook, basic orchestrator shell, config loading

**Status:** ⬜ TODO

**Prerequisites:** None (this is the first phase)

```
I need you to implement Phase 1 (Foundation) of the Battle AI system for my Bannerlord mod.

═══════════════════════════════════════════════════════════════════════════════
CRITICAL PROJECT CONSTRAINTS (from BLUEPRINT.md - READ CAREFULLY)
═══════════════════════════════════════════════════════════════════════════════

GAME TARGET:
- Bannerlord v1.3.13 specifically (not latest version)

API VERIFICATION:
- ALWAYS verify against local decompile FIRST: C:\Dev\Enlisted\Decompile\
- Key assemblies: TaleWorlds.MountAndBlade, TaleWorlds.CampaignSystem, SandBox
- Never rely on external AI docs or outdated API references

PROJECT BUILD:
- Old-style .csproj: Must manually add new .cs files with <Compile Include="..."/>
- Build command: dotnet build -c "Enlisted RETAIL" /p:Platform=x64
- Output: <Bannerlord>/Modules/Enlisted/bin/Win64_Shipping_Client/

LOGGING:
- Use ModLogger class (NOT Console.WriteLine)
- Format: ModLogger.Info("BattleAI", "message");
- Categories: BattleAI, Combat, Formation
- Output: Modules\Enlisted\Debugging\ (Bannerlord install folder)

ENLISTED-ONLY:
- All Battle AI ONLY runs when player is enlisted
- Check: EnlistmentBehavior.Instance.IsEnlisted before any AI logic
- If not enlisted, native AI runs unmodified

CODE QUALITY (RESHARPER + QODANA - CRITICAL):
- Follow ReSharper/Qodana recommendations as strictly as possible
- Fix ALL warnings before considering code complete (don't suppress with pragmas)
- ALWAYS use braces for if/for/while/foreach, even single lines
- Comments describe WHAT code does NOW (no changelog style)
- Remove: unused usings, unused variables, unused methods, redundant qualifiers
- Add null checks where needed (avoid PossibleNullReferenceException)

EXISTING MANAGERS TO USE:
- EnlistmentBehavior.Instance - Enlisted state, lord reference
- ConfigurationManager.Instance - JSON config loading
- ModLogger - Logging

═══════════════════════════════════════════════════════════════════════════════
DOCUMENTATION TO READ FIRST
═══════════════════════════════════════════════════════════════════════════════

Read these docs BEFORE implementing:

1. docs/Features/Combat/BATTLE-AI-IMPLEMENTATION-SPEC.md
   - Section 1: Architecture Overview (layer hierarchy, dual orchestrator, activation gate)
   - Section 2: Phase 1 items (1.1-1.6)
   - Section 2b: Edge Cases for Phase 1

2. docs/Features/Combat/battle-ai-plan.md
   - Part 6: Modding Entry Points (MissionBehavior, Harmony targets, useful APIs)
   - Part 1: Native AI Analysis (understand what exists)

3. docs/Features/Combat/agent-combat-ai.md
   - AgentStatCalculateModel section (how to override agent stats)

4. docs/BLUEPRINT.md
   - Quick Orientation (project constraints)
   - Common Tasks (adding files, logging)

5. DECOMPILE REFERENCE (verify all APIs exist):
   - C:\Dev\Enlisted\Decompile\TaleWorlds.MountAndBlade\Mission.cs
   - C:\Dev\Enlisted\Decompile\TaleWorlds.MountAndBlade\MissionBehavior.cs
   - C:\Dev\Enlisted\Decompile\TaleWorlds.MountAndBlade\Team.cs
   - C:\Dev\Enlisted\Decompile\TaleWorlds.MountAndBlade\TeamQuerySystem.cs

═══════════════════════════════════════════════════════════════════════════════
PHASE 1 TASKS
═══════════════════════════════════════════════════════════════════════════════

1.1 ENLISTED ACTIVATION GATE
- Create utility method: ShouldActivateBattleAI()
- Check EnlistmentState.IsEnlisted (use existing EnlistmentBehavior.Instance)
- Check Mission.Current is not null
- Check not multiplayer (Mission.Current.IsMultiplayer)
- Check field battle (not siege/arena)
- Return false if ANY check fails

1.2 MISSIONBEHAVIOR ENTRY POINT
- Create src/Features/Combat/BattleOrchestratorBehavior.cs
- Inherit from MissionBehavior
- Override OnBehaviorInitialize() - create orchestrators IF activation gate passes
- Override OnMissionTick(float dt) - tick orchestrators
- Override OnMissionEnded() - cleanup
- Register behavior in SubModule.OnMissionStarted callback

1.3 BASIC ORCHESTRATOR SHELL
- Create src/Features/Combat/BattleOrchestrator.cs
- One orchestrator per team (PlayerTeam + EnemyTeam)
- Store reference to Team
- Stub methods: Observe(), Orient(), Decide(), Act()
- Simple tick method that logs it's running

1.4 BATTLE STATE MODEL
- Create src/Features/Combat/Models/BattleState.cs
- Read-only data from Team.QuerySystem:
  - PowerRatio (ours vs theirs)
  - CavalryRatio
  - InfantryRatio
  - RangedRatio
  - TotalAlive, TotalCasualties
- Create helper: BattleStateReader.CaptureState(Team team)

1.5 AGENT STAT MODEL OVERRIDE
- Create src/Features/Combat/EnlistedAgentStatCalculateModel.cs
- Inherit from SandboxAgentStatCalculateModel
- Override InitializeAgentStats() - call base, then apply enlisted modifiers
- Only apply modifiers if activation gate passes
- Register via GameModelsContext in SubModule.OnGameStart

1.6 CONFIGURATION LOADING
- Add "battle_ai" section to ModuleData/Enlisted/enlisted_config.json
- Include: enabled (bool), log_decisions (bool), decision_interval_sec (float)
- Load via existing ConfigurationManager
- Default: enabled=true, log_decisions=true, decision_interval_sec=1.5

═══════════════════════════════════════════════════════════════════════════════
EDGE CASES TO HANDLE
═══════════════════════════════════════════════════════════════════════════════

From BATTLE-AI-IMPLEMENTATION-SPEC.md Section 2b:

| Edge Case | Handling Strategy |
|-----------|-------------------|
| Mission.Current is null | Guard all entry points, abort initialization |
| Enlisted state changes during battle load | Re-check before orchestrator tick starts |
| Multiple missions loading in sequence | Clean up previous orchestrator on mission end |
| Mission ends before orchestrator initializes | Register cleanup in OnMissionEnded |
| Configuration file missing/corrupt | Use hardcoded defaults, log warning |
| Multiplayer battle detected | Activation gate returns false, skip entirely |
| Save/load during battle | Handle via OnAfterMissionLoad hook |
| Team is null or has no agents | Early exit from orchestrator creation |

═══════════════════════════════════════════════════════════════════════════════
FILES TO CREATE
═══════════════════════════════════════════════════════════════════════════════

1. src/Features/Combat/BattleOrchestratorBehavior.cs
2. src/Features/Combat/BattleOrchestrator.cs
3. src/Features/Combat/Models/BattleState.cs
4. src/Features/Combat/BattleStateReader.cs
5. src/Features/Combat/EnlistedAgentStatCalculateModel.cs

REMEMBER: Add all new .cs files to Enlisted.csproj with <Compile Include="..."/>

═══════════════════════════════════════════════════════════════════════════════
EXISTING FILES TO MODIFY
═══════════════════════════════════════════════════════════════════════════════

1. src/Mod.Entry/SubModule.cs
   - Add BattleOrchestratorBehavior registration in OnMissionStarted
   - Add EnlistedAgentStatCalculateModel registration in OnGameStart

2. ModuleData/Enlisted/enlisted_config.json
   - Add "battle_ai" configuration section

3. src/Mod.Core/Config/ConfigurationManager.cs
   - Add BattleAIConfig class and property

4. Enlisted.csproj
   - Add <Compile Include="..."/> for all new files

═══════════════════════════════════════════════════════════════════════════════
ACCEPTANCE CRITERIA
═══════════════════════════════════════════════════════════════════════════════

[ ] Activation gate returns false when player not enlisted
[ ] Activation gate returns true when player is enlisted in field battle
[ ] BattleOrchestratorBehavior added to mission when enlisted
[ ] BattleOrchestrator created for each team (PlayerTeam, EnemyTeam)
[ ] Orchestrator ticks log every ~1.5 seconds: "[BattleAI] Team:X Tick"
[ ] BattleState correctly reads power ratios from TeamQuerySystem
[ ] EnlistedAgentStatCalculateModel registered and base stats unchanged
[ ] Config loads from JSON, falls back to defaults if missing
[ ] All new files added to Enlisted.csproj
[ ] Build succeeds: dotnet build -c "Enlisted RETAIL" /p:Platform=x64
[ ] No crashes when entering battle while enlisted
[ ] No crashes when entering battle while NOT enlisted (native AI only)

═══════════════════════════════════════════════════════════════════════════════
HANDOFF NOTES (Capture these for Phase 2)
═══════════════════════════════════════════════════════════════════════════════

When complete, provide these handoff notes for the next AI session:

FILES CREATED:
- [ ] src/Features/Combat/BattleOrchestratorBehavior.cs
- [ ] src/Features/Combat/BattleOrchestrator.cs
- [ ] src/Features/Combat/Models/BattleState.cs
- [ ] src/Features/Combat/BattleStateReader.cs
- [ ] src/Features/Combat/EnlistedAgentStatCalculateModel.cs

FILES MODIFIED:
- [ ] src/Mod.Entry/SubModule.cs
- [ ] ModuleData/Enlisted/enlisted_config.json
- [ ] src/Mod.Core/Config/ConfigurationManager.cs
- [ ] Enlisted.csproj

KEY APIS VERIFIED IN DECOMPILE:
- (list which APIs you verified exist)

KEY DECISIONS MADE:
- (list any architectural decisions)

KNOWN ISSUES/TECH DEBT:
- (list any incomplete items)

VERIFICATION PASSED:
[ ] Enlisted player enters battle - orchestrator logs appear
[ ] Non-enlisted player enters battle - no orchestrator logs
[ ] Build succeeds
[ ] No runtime errors
```

---

# Phase 2: Agent Combat Enhancements

**Goal:** Implement context-aware agent property tuning, AI profiles, and coordination signals

**Status:** ⬜ TODO

**Prerequisites:** Phase 1 complete (EnlistedAgentStatCalculateModel exists)

```
I need you to implement Phase 2 (Agent Combat Enhancements) of the Battle AI system for my Bannerlord mod.

═══════════════════════════════════════════════════════════════════════════════
CRITICAL PROJECT CONSTRAINTS (from BLUEPRINT.md)
═══════════════════════════════════════════════════════════════════════════════

GAME TARGET: Bannerlord v1.3.13

API VERIFICATION:
- ALWAYS verify against local decompile: C:\Dev\Enlisted\Decompile\
- Check: TaleWorlds.MountAndBlade\AgentDrivenProperties.cs for actual property names

PROJECT BUILD:
- Old-style .csproj: Manually add new files with <Compile Include="..."/>
- Build: dotnet build -c "Enlisted RETAIL" /p:Platform=x64

LOGGING: Use ModLogger.Info("BattleAI", "message");

ENLISTED-ONLY: Check EnlistmentBehavior.Instance.IsEnlisted before AI logic

CODE QUALITY (RESHARPER + QODANA):
- Follow ReSharper/Qodana - fix ALL warnings, don't suppress
- ALWAYS use braces for if/for/while/foreach
- Comments describe current behavior (no changelog style)
- Remove unused code, add null checks

═══════════════════════════════════════════════════════════════════════════════
CONTEXT RECOVERY (Verify Phase 1 exists before starting)
═══════════════════════════════════════════════════════════════════════════════

Phase 1 is COMPLETE. Before implementing Phase 2, verify these files exist:

Phase 1 Files (MUST exist - read them to understand current implementation):
[ ] src/Features/Combat/BattleOrchestratorBehavior.cs - MissionBehavior with activation gate
[ ] src/Features/Combat/BattleOrchestrator.cs - Per-team orchestrator shell
[ ] src/Features/Combat/Models/BattleState.cs - Read-only battle data
[ ] src/Features/Combat/BattleStateReader.cs - Captures state from TeamQuerySystem
[ ] src/Features/Combat/EnlistedAgentStatCalculateModel.cs - Agent stat override (empty)

Verify Phase 1 is working:
1. Check logs for "[BattleAI] Team:X Tick" entries during battle
2. Verify BattleOrchestrator exists for both teams
3. Verify EnlistedAgentStatCalculateModel is registered

If any Phase 1 files are missing, they need to be created first (see Phase 1 prompt).

═══════════════════════════════════════════════════════════════════════════════
DOCUMENTATION TO READ FIRST
═══════════════════════════════════════════════════════════════════════════════

Read these docs BEFORE implementing:

1. docs/Features/Combat/BATTLE-AI-IMPLEMENTATION-SPEC.md
   - Section 2: Phase 2 items (2.1-2.4)
   - Section 2b: Edge Cases for Phase 2
   - Section 3.2: Agent Combat AI specification

2. docs/Features/Combat/agent-combat-ai.md (CRITICAL - read entire doc)
   - AgentDrivenProperties (40+ properties)
   - AI Level calculation from skills
   - BehaviorValueSet (formation-based aggression)
   - All property ranges and effects

3. DECOMPILE REFERENCE (verify all APIs exist):
   - C:\Dev\Enlisted\Decompile\TaleWorlds.MountAndBlade\AgentDrivenProperties.cs
   - C:\Dev\Enlisted\Decompile\TaleWorlds.MountAndBlade\Agent.cs
   - C:\Dev\Enlisted\Decompile\SandBox\SandboxAgentStatCalculateModel.cs

═══════════════════════════════════════════════════════════════════════════════
PHASE 2 TASKS
═══════════════════════════════════════════════════════════════════════════════

2.1 CONTEXT-AWARE AGENT TUNING
- Modify EnlistedAgentStatCalculateModel.InitializeAgentStats()
- Detect combat context:
  - Formation under archer fire? → Increase shield use priority
  - Formation in melee? → Increase aggression
  - Formation retreating? → Reduce blocking (run away)
  - Agent surrounded? → Defensive stance
- Apply property adjustments based on context

2.2 COORDINATION SIGNALS (Reduce Mob Behavior)
- Track which enemies are being targeted by how many agents
- Agents prefer less-targeted enemies (reduce AITargetingChance for over-targeted)
- Implement targeting weight adjustment
- Goal: Enemies spread across multiple targets, not piling on one

2.3 AI PROFILES
- Create AgentCombatProfile enum: Green, Regular, Veteran, Elite
- Create profile definitions with property sets:

Green Profile (Tier 1-2 troops):
  - AIBlockOnDecideAbility = 0.55f
  - AIParryOnDecideAbility = 0.45f
  - AiRandomizedDefendDirectionChance = 0.5f
  - AIAttackOnDecideChance = 0.2f
  
Veteran Profile (Tier 4-5 troops):
  - AIBlockOnDecideAbility = 0.92f
  - AIParryOnDecideAbility = 0.85f
  - AiRandomizedDefendDirectionChance = 0.15f
  - AIAttackOnDecideChance = 0.4f

Elite Profile (Tier 6+ or heroes):
  - AIBlockOnDecideAbility = 0.98f
  - AIParryOnDecideAbility = 0.92f
  - AiRandomizedDefendDirectionChance = 0.05f
  - AIAttackOnDecideChance = 0.48f

2.4 SKILL-BASED PROPERTY SCALING
- Calculate AI level from agent skills (OneHandedSkill + AthleticsSkill, etc.)
- Scale properties based on skill:
  - Higher skill = better blocking, less random errors
  - Lower skill = more hesitation, more mistakes
- Use exponential smoothing, not linear (elite soldiers MUCH better)

═══════════════════════════════════════════════════════════════════════════════
EDGE CASES TO HANDLE
═══════════════════════════════════════════════════════════════════════════════

From BATTLE-AI-IMPLEMENTATION-SPEC.md Section 2b:

| Edge Case | Handling Strategy |
|-----------|-------------------|
| Agent dies during property calculation | Null-check Agent.State, skip dead agents |
| Agent has no weapon equipped | Use baseline properties, don't error |
| Agent switches weapons mid-combat | Recalculate properties on weapon change event |
| Horse archer dismounted | Transition from mounted to foot profile |
| Agent is player-controlled | Skip AI property overrides for player agent |
| Agent retreating/routing | Apply retreat-specific behavior (no blocking) |
| Agent in water/falling/ragdoll | Skip property updates until agent stable |

═══════════════════════════════════════════════════════════════════════════════
FILES TO CREATE
═══════════════════════════════════════════════════════════════════════════════

1. src/Features/Combat/AgentCombatTuner.cs (context-aware tuning logic)
2. src/Features/Combat/Models/AgentCombatProfile.cs (profile enum and definitions)
3. src/Features/Combat/TargetCoordinator.cs (mob behavior reduction)

REMEMBER: Add all new .cs files to Enlisted.csproj with <Compile Include="..."/>

═══════════════════════════════════════════════════════════════════════════════
FILES TO MODIFY
═══════════════════════════════════════════════════════════════════════════════

1. src/Features/Combat/EnlistedAgentStatCalculateModel.cs
   - Call AgentCombatTuner methods
   - Apply profile-based property sets
   - Apply skill scaling

═══════════════════════════════════════════════════════════════════════════════
ACCEPTANCE CRITERIA
═══════════════════════════════════════════════════════════════════════════════

[ ] Veteran soldiers demonstrably fight better than green troops
[ ] Context-aware adjustments visible (shield use under archer fire)
[ ] Enemies spread across multiple targets (reduced mob behavior)
[ ] AI profiles apply correct property values
[ ] Skill-based scaling works (higher skill = better properties)
[ ] Player agent NOT affected by AI tuning
[ ] Dead/routing agents handled gracefully
[ ] Logs show property adjustments: "[BattleAI] Agent:X Profile:Veteran Context:UnderFire"
[ ] Build succeeds
[ ] No crashes or exceptions

═══════════════════════════════════════════════════════════════════════════════
HANDOFF NOTES (Capture these for Phase 3)
═══════════════════════════════════════════════════════════════════════════════

When complete, provide these handoff notes for the next AI session:

FILES CREATED:
- [ ] src/Features/Combat/AgentCombatTuner.cs
- [ ] src/Features/Combat/Models/AgentCombatProfile.cs
- [ ] src/Features/Combat/TargetCoordinator.cs

FILES MODIFIED:
- [ ] src/Features/Combat/EnlistedAgentStatCalculateModel.cs

PROPERTY RANGES USED:
- (list the property ranges for each profile)

KEY DECISIONS MADE:
- (how did you calculate AI level from skills?)
- (how did you implement targeting coordination?)

KNOWN ISSUES/TECH DEBT:
- (list any incomplete items)

VERIFICATION PASSED:
[ ] Veteran vs Green troops observable difference
[ ] Context adjustments working
[ ] Mob behavior reduced
```

---

# Phase 3: Formation Intelligence

**Goal:** Implement smart charge decisions, weapon discipline, self-organizing ranks

**Status:** ⬜ TODO

**Prerequisites:** Phase 1-2 complete

```
I need you to implement Phase 3 (Formation Intelligence) of the Battle AI system for my Bannerlord mod.

═══════════════════════════════════════════════════════════════════════════════
CRITICAL PROJECT CONSTRAINTS (from BLUEPRINT.md)
═══════════════════════════════════════════════════════════════════════════════

GAME TARGET: Bannerlord v1.3.13

API VERIFICATION: Use local decompile at C:\Dev\Enlisted\Decompile\

PROJECT BUILD:
- Old-style .csproj: Manually add new files with <Compile Include="..."/>
- Build: dotnet build -c "Enlisted RETAIL" /p:Platform=x64

LOGGING: Use ModLogger.Info("BattleAI", "message");

CODE QUALITY (RESHARPER + QODANA):
- Follow ReSharper/Qodana - fix ALL warnings, don't suppress
- ALWAYS use braces for if/for/while/foreach
- Comments describe current behavior (no changelog style)
- Remove unused code, add null checks

═══════════════════════════════════════════════════════════════════════════════
CONTEXT RECOVERY (Verify Phase 1-2 exist before starting)
═══════════════════════════════════════════════════════════════════════════════

Phase 1-2 are COMPLETE. Before implementing Phase 3, verify these files exist:

Phase 1-2 Files (MUST exist):
[ ] src/Features/Combat/BattleOrchestratorBehavior.cs
[ ] src/Features/Combat/BattleOrchestrator.cs
[ ] src/Features/Combat/EnlistedAgentStatCalculateModel.cs
[ ] src/Features/Combat/AgentCombatTuner.cs
[ ] src/Features/Combat/Models/AgentCombatProfile.cs

If any files are missing, create them first (see Phase 1-2 prompts).

═══════════════════════════════════════════════════════════════════════════════
DOCUMENTATION TO READ FIRST
═══════════════════════════════════════════════════════════════════════════════

1. docs/Features/Combat/BATTLE-AI-IMPLEMENTATION-SPEC.md
   - Section 2: Phase 3 items (3.1-3.7)
   - Section 2b: Edge Cases for Phase 3

2. docs/Features/Combat/battle-ai-plan.md
   - Part 5: Tactical Enhancements (Smart Charge Decisions)
   - Part 10: Tactical Decision Engine (Cavalry Cycle Charging)
   - Part 11: Agent Formation Behavior (Pike/Spear Weapon Discipline)
   - Part 12: Intelligent Formation Organization (Self-Organizing Ranks)

3. DECOMPILE REFERENCE:
   - C:\Dev\Enlisted\Decompile\TaleWorlds.MountAndBlade\Formation.cs
   - C:\Dev\Enlisted\Decompile\TaleWorlds.MountAndBlade\FormationQuerySystem.cs
   - C:\Dev\Enlisted\Decompile\TaleWorlds.MountAndBlade\BehaviorTacticalCharge.cs

═══════════════════════════════════════════════════════════════════════════════
PHASE 3 TASKS
═══════════════════════════════════════════════════════════════════════════════

3.1 SMART CHARGE DECISIONS
- Harmony patch BehaviorTacticalCharge or influence via weights
- Check target before charging:
  - Is target formation braced spears? → DON'T CHARGE
  - Is target shield wall facing us? → DON'T CHARGE (flank instead)
  - Is target vulnerable archers? → CHARGE
  - Is target routing? → CHARGE (pursue)
- Minimum charge distance: 60m (need momentum)
- Log charge decisions

3.2 INFANTRY FLANKING
- When own infantry has 1.5x advantage:
  - Split formation if >80 troops
  - Detach flanking element
  - Main force pins, flanking force envelops
- Only if orchestrator decides it's appropriate

3.3 THREAT ASSESSMENT TARGETING
- Score enemy formations before engaging:
  - Low armor = higher priority for archers
  - Low morale = higher priority (break them)
  - Vulnerable position = high priority
- Pass priority to formation's targeting decisions

3.4 FORMATION COHESION LEVELS
- Three levels: Tight, Moderate, Loose
- Tight: Under ranged fire, advancing on enemy
- Moderate: Normal combat, stable engagement
- Loose: Exploiting, pursuing, enemy broken
- Adjust via formation spacing orders

3.5 PIKE/SPEAR WEAPON DISCIPLINE (CRITICAL)
- Pike infantry keep polearms out at 6m+ range
- Switch to sidearm at <1.5m (pike too close)
- Formation breaking up = instant recall, tighten
- Never drop pike for melee unless forced

3.6 MULTI-WEAPON INFANTRY PROGRESSION
- Soldiers with javelins + spear + sword:
  1. Throw javelins at 20m+ (don't waste in melee)
  2. Use spear at 3-15m
  3. Switch to sword at <1.5m
- Conserve throwing weapons if spear available

3.7 SELF-ORGANIZING RANKS
- Heavy/elite troops to front (high armor, shields)
- Light troops to rear (support, ranged)
- Calculate FrontLineScore per agent:
  - +Armor, +Shield, +Tier, +Melee Skill
  - -Ranged primary weapon
- Sort formation by FrontLineScore

═══════════════════════════════════════════════════════════════════════════════
EDGE CASES TO HANDLE
═══════════════════════════════════════════════════════════════════════════════

| Edge Case | Handling Strategy |
|-----------|-------------------|
| Formation has 0 agents | Skip formation entirely |
| Formation has 1 agent | Treat as solo unit, limited tactics |
| Formation disbanded mid-charge | Abort charge, reassign agents |
| All cavalry dead | No cavalry charges possible |
| Pike infantry have no pikes (dropped) | Fall back to secondary weapons |
| Multi-weapon soldier all weapons dropped | Mark as non-combatant |
| Target formation destroyed mid-charge | Acquire new target |
| Self-organizing with identical troops | Random tie-breaking |

═══════════════════════════════════════════════════════════════════════════════
FILES TO CREATE
═══════════════════════════════════════════════════════════════════════════════

1. src/Features/Combat/SmartChargeEvaluator.cs (charge decisions)
2. src/Features/Combat/WeaponDiscipline.cs (pike/spear/multi-weapon logic)
3. src/Features/Combat/FormationOrganizer.cs (self-organizing ranks)
4. src/Features/Combat/ThreatAssessor.cs (target scoring)

═══════════════════════════════════════════════════════════════════════════════
ACCEPTANCE CRITERIA
═══════════════════════════════════════════════════════════════════════════════

[ ] Cavalry doesn't charge braced spear formations
[ ] Cavalry charges from 60m+ with momentum
[ ] Pike infantry keep pikes out at proper range
[ ] Multi-weapon soldiers use proper progression
[ ] Heavy troops end up in front ranks
[ ] Light/ranged troops end up in rear
[ ] Charge decisions logged
[ ] Build succeeds
[ ] No crashes

═══════════════════════════════════════════════════════════════════════════════
HANDOFF NOTES (Capture these for Phase 4)
═══════════════════════════════════════════════════════════════════════════════

FILES CREATED: (list them)
FILES MODIFIED: (list them)
KEY DECISIONS: (how did you hook into charge behavior? Harmony or weights?)
KNOWN ISSUES: (any incomplete items)
```

---

# Phase 4: Battle Orchestrator Core

**Goal:** Implement OODA decision loop, strategy modes, formation roles, battle phases

**Status:** ⬜ TODO

**Prerequisites:** Phase 1-3 complete

```
I need you to implement Phase 4 (Battle Orchestrator Core) of the Battle AI system for my Bannerlord mod.

═══════════════════════════════════════════════════════════════════════════════
CRITICAL PROJECT CONSTRAINTS (from BLUEPRINT.md)
═══════════════════════════════════════════════════════════════════════════════

GAME TARGET: Bannerlord v1.3.13

API VERIFICATION: Use local decompile at C:\Dev\Enlisted\Decompile\

PROJECT BUILD:
- Old-style .csproj: Manually add new files with <Compile Include="..."/>
- Build: dotnet build -c "Enlisted RETAIL" /p:Platform=x64

LOGGING: Use ModLogger.Info("BattleAI", "message");

CODE QUALITY (RESHARPER + QODANA):
- Follow ReSharper/Qodana - fix ALL warnings, don't suppress
- ALWAYS use braces for if/for/while/foreach
- Comments describe current behavior (no changelog style)
- Remove unused code, add null checks

═══════════════════════════════════════════════════════════════════════════════
CONTEXT RECOVERY
═══════════════════════════════════════════════════════════════════════════════

Verify Phase 1-3 files exist:
[ ] src/Features/Combat/BattleOrchestrator.cs (empty OODA methods)
[ ] src/Features/Combat/Models/BattleState.cs
[ ] src/Features/Combat/BattleStateReader.cs
[ ] src/Features/Combat/SmartChargeEvaluator.cs
[ ] src/Features/Combat/WeaponDiscipline.cs
[ ] src/Features/Combat/FormationOrganizer.cs

═══════════════════════════════════════════════════════════════════════════════
DOCUMENTATION TO READ
═══════════════════════════════════════════════════════════════════════════════

1. docs/Features/Combat/BATTLE-AI-IMPLEMENTATION-SPEC.md
   - Section 2: Phase 4 items (4.1-4.5)
   - Section 2b: Edge Cases for Phase 4
   - Section 3.1: BattleOrchestrator specification

2. docs/Features/Combat/battle-ai-plan.md
   - Part 4: Battle Orchestrator Proposal (full detail)
   - Part 9: AI vs AI Battle Intelligence

═══════════════════════════════════════════════════════════════════════════════
PHASE 4 TASKS
═══════════════════════════════════════════════════════════════════════════════

4.1 OODA DECISION LOOP (Every 1-2 seconds)
- OBSERVE: Capture signals from TeamQuerySystem, FormationQuerySystem
  - Power ratios, casualty rates, positions
  - Store in rolling history (last 30 seconds)
- ORIENT: Analyze battlefield state
  - Estimate frontline center
  - Sample local superiority at key points
  - Detect flanking threats
- DECIDE: Choose strategy mode (with hysteresis)
  - Use 1.3x hysteresis to prevent flip-flopping
  - Log decision factors
- ACT: Apply bounded interventions
  - Nudge formation behavior weights
  - Issue reserve commands
  - Respect cooldowns

4.2 STRATEGY MODES
- EngageBalanced: Standard balanced attack
- DelayDefend: Defensive posture, refuse flanks
- Exploit: Aggressive exploitation of weakness
- Withdraw: Fighting retreat, preserve force

Mode transitions:
- PowerRatio < 0.4 → Withdraw
- PowerRatio > 1.5 + flank exposed → Exploit
- PowerRatio 0.4-0.7 → DelayDefend
- Otherwise → EngageBalanced

4.3 FORMATION ROLES
- MainLine: Primary combat formations
- Screen: Skirmishers, forward security
- FlankGuard: Protect exposed flanks
- Reserve: Held back, committed on triggers

Assign roles based on:
- Formation composition (cavalry = potential reserve)
- Current position
- Player command (respect player orders for player's formation)

4.4 BATTLE PHASES
- Forming: Initial deployment
- Advancing: Moving to contact
- Engaged: Active combat
- Retreating: Organized withdrawal

Detect phase from:
- Distance to enemy
- Casualty rates
- Formation states

4.5 TREND ANALYSIS
- Track power ratio over time (rolling 30s window)
- Detect: "Losing steadily for N seconds" → more aggressive action
- Detect: "Winning but slowing" → commit reserves
- Provide trend data to decision logic

═══════════════════════════════════════════════════════════════════════════════
EDGE CASES
═══════════════════════════════════════════════════════════════════════════════

| Edge Case | Handling Strategy |
|-----------|-------------------|
| Battle ends during OODA loop | Check Mission.IsEnding at start of tick |
| All formations destroyed | Early exit, stop orchestrator |
| Power ratio NaN/infinity | Guard division by zero, default to 1.0 |
| Enemy team retreated | Transition to consolidation/pursuit |
| Trend data insufficient (<5 samples) | Use current snapshot, mark unknown |
| Strategy mode indeterminate | Default to EngageBalanced |

═══════════════════════════════════════════════════════════════════════════════
FILES TO CREATE/MODIFY
═══════════════════════════════════════════════════════════════════════════════

Create:
1. src/Features/Combat/Models/StrategyMode.cs (enum)
2. src/Features/Combat/Models/FormationRole.cs (enum)
3. src/Features/Combat/Models/BattlePhase.cs (enum)
4. src/Features/Combat/TrendAnalyzer.cs

Modify:
1. src/Features/Combat/BattleOrchestrator.cs (implement OODA methods)

═══════════════════════════════════════════════════════════════════════════════
ACCEPTANCE CRITERIA
═══════════════════════════════════════════════════════════════════════════════

[ ] OODA loop ticks every 1-2 seconds
[ ] Strategy mode changes with hysteresis (no rapid flipping)
[ ] Formation roles assigned appropriately
[ ] Battle phase detected correctly
[ ] Trend analysis provides useful data
[ ] Logs show: "[BattleAI] Team:X Strategy:EngageBalanced→Exploit (PowerRatio:1.8)"
[ ] Build succeeds
[ ] No crashes

═══════════════════════════════════════════════════════════════════════════════
HANDOFF NOTES
═══════════════════════════════════════════════════════════════════════════════

FILES CREATED: (list)
FILES MODIFIED: (list)
STRATEGY THRESHOLDS: (document what values you used)
KEY DECISIONS: (list)
KNOWN ISSUES: (list)
```

---

# Phase 5: Tactical Decision Engine

**Goal:** Cavalry cycle charging, reserve timing, targeting decisions

**Status:** ⬜ TODO

**Prerequisites:** Phase 1-4 complete

```
I need you to implement Phase 5 (Tactical Decision Engine) of the Battle AI system for my Bannerlord mod.

═══════════════════════════════════════════════════════════════════════════════
CRITICAL PROJECT CONSTRAINTS (from BLUEPRINT.md)
═══════════════════════════════════════════════════════════════════════════════

GAME TARGET: Bannerlord v1.3.13
API VERIFICATION: Use local decompile at C:\Dev\Enlisted\Decompile\
PROJECT BUILD: dotnet build -c "Enlisted RETAIL" /p:Platform=x64
CSPROJ: Manually add new files with <Compile Include="..."/>
LOGGING: Use ModLogger.Info("BattleAI", "message");
CODE QUALITY: Follow ReSharper/Qodana strictly, fix all warnings, use braces, no changelog comments

═══════════════════════════════════════════════════════════════════════════════
CONTEXT RECOVERY
═══════════════════════════════════════════════════════════════════════════════

Verify Phase 1-4 files exist:
[ ] src/Features/Combat/BattleOrchestrator.cs (with OODA loop)
[ ] src/Features/Combat/Models/StrategyMode.cs
[ ] src/Features/Combat/Models/FormationRole.cs
[ ] src/Features/Combat/TrendAnalyzer.cs

═══════════════════════════════════════════════════════════════════════════════
DOCUMENTATION TO READ
═══════════════════════════════════════════════════════════════════════════════

1. docs/Features/Combat/BATTLE-AI-IMPLEMENTATION-SPEC.md
   - Section 2: Phase 5 items (5.1-5.6)
   - Section 2b: Edge Cases for Phase 5

2. docs/Features/Combat/battle-ai-plan.md
   - Part 10: Tactical Decision Engine (all sections)
   - Cavalry Cycle Charging section (CRITICAL)

═══════════════════════════════════════════════════════════════════════════════
PHASE 5 TASKS
═══════════════════════════════════════════════════════════════════════════════

5.1 THREAT VS OPPORTUNITY WEIGHING
- Multi-factor scoring for tactical decisions
- Factors: Enemy vulnerability, own strength, terrain, timing
- Score each potential action, pick highest

5.2 ARCHER TARGETING DECISIONS
- Target priority for ranged units:
  1. Low armor targets (max damage)
  2. Low morale targets (break them)
  3. Enemy ranged (counter-battery)
  4. High-value targets (heroes)
- Pass targeting hints to formations

5.3 CAVALRY RESERVE TIMING
- Hold cavalry until:
  - Enemy flank exposed
  - Enemy archers unprotected
  - Main line needs relief
  - Enemy routing (pursuit)
- Commit criteria logged

5.4 CAVALRY CYCLE CHARGING (CRITICAL)
State machine:
1. HOLD: Waiting for opportunity
2. CHARGE: Moving to contact (60m+ distance)
3. IMPACT: In melee with target
4. DISENGAGE: Breaking contact (10-15 seconds max melee)
5. RALLY: Reforming after disengage
6. REFORM: Preparing for next cycle

Rules:
- Min 60m for charge (momentum)
- Max 15 seconds in melee (don't get bogged down)
- Rally point behind own lines
- 3-6 charges per battle vs native's 1

5.5 RESERVE COMMITMENT LOGIC
- Commit reserve when:
  - Main line casualties > 25%
  - Flank collapsing
  - Enemy reserve committed (match)
  - Clear breakthrough opportunity
- Never commit all reserves at once (keep 30%)

5.6 PURSUIT DECISIONS
- Full pursuit: Enemy broken, we have cavalry
- Cavalry only: Infantry holds, cavalry pursues
- Hold: Enemy may rally, preserve force
- Regroup: High casualties, reform first

═══════════════════════════════════════════════════════════════════════════════
EDGE CASES
═══════════════════════════════════════════════════════════════════════════════

| Edge Case | Handling Strategy |
|-----------|-------------------|
| No archers on team | Skip archer targeting logic |
| No cavalry on team | Skip cavalry reserve/timing logic |
| All enemies same armor/tier | Random target selection |
| Cavalry cannot reach target | Terrain check before commit |
| Reserve already committed | Ignore duplicate commits |
| Pursuit targets despawn | End pursuit, consolidate |
| Cavalry destroyed mid-cycle | Cancel cycle |

═══════════════════════════════════════════════════════════════════════════════
FILES TO CREATE
═══════════════════════════════════════════════════════════════════════════════

1. src/Features/Combat/TacticalDecisionEngine.cs
2. src/Features/Combat/CavalryCycleManager.cs (state machine)
3. src/Features/Combat/ReserveManager.cs
4. src/Features/Combat/Models/CavalryCycleState.cs (enum)

═══════════════════════════════════════════════════════════════════════════════
ACCEPTANCE CRITERIA
═══════════════════════════════════════════════════════════════════════════════

[ ] Cavalry charges from 60m+ with momentum
[ ] Cavalry disengages after 10-15 seconds melee
[ ] Cavalry reforms and charges again (3-6 cycles)
[ ] Reserve commits at appropriate triggers
[ ] Archer targeting prioritizes vulnerable targets
[ ] Pursuit decision made at battle end
[ ] Logs show cycle states: "[BattleAI] Cavalry:Formation3 CHARGE→IMPACT→DISENGAGE"
[ ] Build succeeds

═══════════════════════════════════════════════════════════════════════════════
HANDOFF NOTES
═══════════════════════════════════════════════════════════════════════════════

FILES CREATED: (list)
CYCLE TIMING VALUES: (charge distance, melee duration, etc.)
RESERVE THRESHOLDS: (casualty %, etc.)
KEY DECISIONS: (list)
```

---

# Phase 6: Battle Plan Generation

**Goal:** Plan types, plan selection, main effort designation, adaptation

**Status:** ⬜ TODO

**Prerequisites:** Phase 1-5 complete

```
I need you to implement Phase 6 (Battle Plan Generation) of the Battle AI system for my Bannerlord mod.

═══════════════════════════════════════════════════════════════════════════════
CRITICAL PROJECT CONSTRAINTS (from BLUEPRINT.md)
═══════════════════════════════════════════════════════════════════════════════

GAME TARGET: Bannerlord v1.3.13
API VERIFICATION: Use local decompile at C:\Dev\Enlisted\Decompile\
PROJECT BUILD: dotnet build -c "Enlisted RETAIL" /p:Platform=x64
CSPROJ: Manually add new files with <Compile Include="..."/>
LOGGING: Use ModLogger.Info("BattleAI", "message");
CODE QUALITY: Follow ReSharper/Qodana strictly, fix all warnings, use braces, no changelog comments

═══════════════════════════════════════════════════════════════════════════════
CONTEXT RECOVERY
═══════════════════════════════════════════════════════════════════════════════

Verify Phase 1-5 files exist:
[ ] src/Features/Combat/BattleOrchestrator.cs (OODA loop working)
[ ] src/Features/Combat/TacticalDecisionEngine.cs
[ ] src/Features/Combat/CavalryCycleManager.cs
[ ] src/Features/Combat/ReserveManager.cs

═══════════════════════════════════════════════════════════════════════════════
DOCUMENTATION TO READ
═══════════════════════════════════════════════════════════════════════════════

1. docs/Features/Combat/BATTLE-AI-IMPLEMENTATION-SPEC.md
   - Section 2: Phase 6 items (6.1-6.6)
   - Section 2b: Edge Cases for Phase 6

2. docs/Features/Combat/battle-ai-plan.md
   - Part 14: Battle Plan Generation (all sections)

═══════════════════════════════════════════════════════════════════════════════
PHASE 6 TASKS
═══════════════════════════════════════════════════════════════════════════════

6.1 PLAN TYPES
- LeftHook: Main effort left, pin center/right
- RightHook: Main effort right, pin center/left
- CenterPunch: Concentrated center breakthrough
- DoubleEnvelopment: Both flanks attack, center pins
- HammerAnvil: Infantry pins, cavalry flanks
- Delay: Defensive, trade space for time

6.2 PLAN SELECTION
Score each plan by:
- Composition fit (cavalry for HammerAnvil, etc.)
- Terrain suitability (high ground for defense)
- Power ratio (envelopment when advantaged)
- Enemy disposition (exploit weak flank)

6.3 MAIN EFFORT DESIGNATION
- Assign strongest formations to main effort axis
- Other formations support (pin, screen, reserve)
- Log main effort clearly

6.4 FORMATION OBJECTIVES
- Attack: Advance and destroy
- Pin: Engage and fix enemy
- Screen: Skirmish, delay, protect flank
- Breakthrough: Punch through line
- Flank: Envelop enemy position
- Hold: Defensive position

6.5 SEQUENTIAL OBJECTIVES
- Cavalry: Screen → Wait → Charge → Pursue
- Infantry: Advance → Engage → Exploit
- Phase-based tasking respects timing

6.6 PLAN ADAPTATION
- Detect plan success/failure every 30s
- Shift main effort if original axis failing
- Commit reserves to failing axis OR exploit success
- Log adaptation: "[BattleAI] Plan:LeftHook→CenterPunch (LeftFlankStalled)"

═══════════════════════════════════════════════════════════════════════════════
EDGE CASES
═══════════════════════════════════════════════════════════════════════════════

| Edge Case | Handling Strategy |
|-----------|-------------------|
| Terrain data unavailable | Use flat-terrain default plan |
| No valid plan matches | Fall back to EngageBalanced |
| Main effort formation destroyed | Reassign to next strongest |
| All formations same objective | Spread objectives |
| Plan scores tied | Random tie-breaking |
| Insufficient troops for plan | Scale down complexity |

═══════════════════════════════════════════════════════════════════════════════
FILES TO CREATE
═══════════════════════════════════════════════════════════════════════════════

1. src/Features/Combat/BattlePlanGenerator.cs
2. src/Features/Combat/Models/BattlePlan.cs
3. src/Features/Combat/Models/BattlePlanType.cs (enum)
4. src/Features/Combat/Models/FormationObjective.cs (enum)

═══════════════════════════════════════════════════════════════════════════════
ACCEPTANCE CRITERIA
═══════════════════════════════════════════════════════════════════════════════

[ ] Plans generated based on composition/terrain/ratio
[ ] Main effort designated and logged
[ ] Formation objectives assigned
[ ] Plan adapts when axis failing
[ ] Logs show: "[BattleAI] Plan:HammerAnvil MainEffort:CavalryF3 Objective:Flank"
[ ] Build succeeds

═══════════════════════════════════════════════════════════════════════════════
HANDOFF NOTES
═══════════════════════════════════════════════════════════════════════════════

FILES CREATED: (list)
PLAN SCORING WEIGHTS: (document)
KEY DECISIONS: (list)
```

---

# Phase 7: Reserve & Retreat Management

**Goal:** Reserve management, organized withdrawal, rally points

**Status:** ⬜ TODO

**Prerequisites:** Phase 1-6 complete

```
I need you to implement Phase 7 (Reserve & Retreat Management) of the Battle AI system for my Bannerlord mod.

═══════════════════════════════════════════════════════════════════════════════
CRITICAL PROJECT CONSTRAINTS (from BLUEPRINT.md)
═══════════════════════════════════════════════════════════════════════════════

GAME TARGET: Bannerlord v1.3.13
API VERIFICATION: Use local decompile at C:\Dev\Enlisted\Decompile\
PROJECT BUILD: dotnet build -c "Enlisted RETAIL" /p:Platform=x64
CSPROJ: Manually add new files with <Compile Include="..."/>
LOGGING: Use ModLogger.Info("BattleAI", "message");
CODE QUALITY: Follow ReSharper/Qodana strictly, fix all warnings, use braces, no changelog comments

═══════════════════════════════════════════════════════════════════════════════
CONTEXT RECOVERY (Verify Phase 1-6 exist before starting)
═══════════════════════════════════════════════════════════════════════════════

Verify these key files exist from previous phases:
[ ] src/Features/Combat/BattleOrchestrator.cs (with OODA loop from Phase 4)
[ ] src/Features/Combat/TacticalDecisionEngine.cs (Phase 5)
[ ] src/Features/Combat/BattlePlanGenerator.cs (Phase 6)
[ ] src/Features/Combat/ReserveManager.cs (Phase 5 - you'll extend this)

If missing, complete earlier phases first.

═══════════════════════════════════════════════════════════════════════════════
DOCUMENTATION TO READ
═══════════════════════════════════════════════════════════════════════════════

1. docs/Features/Combat/BATTLE-AI-IMPLEMENTATION-SPEC.md
   - Section 2: Phase 7 items (7.1-7.5)
   - Section 2b: Edge Cases for Phase 7

2. docs/Features/Combat/battle-ai-plan.md
   - Part 18: Coordinated Retreat

═══════════════════════════════════════════════════════════════════════════════
PHASE 7 TASKS
═══════════════════════════════════════════════════════════════════════════════

7.1 RESERVE MANAGER
- Track which formations are reserves
- Hold until commit triggers
- Commit commands: Reinforce, Counterattack, Intercept, Pursue
- Never commit all (keep 30% uncommitted)

7.2 CASUALTY-BASED POSTURE
- Engaged: Normal combat (<20% casualties)
- FightingRetreat: Giving ground (20-40% casualties)
- TacticalWithdrawal: Organized fallback (40-60% casualties)
- LastStand: Fight to end (>60% OR cornered)

7.3 ORGANIZED WITHDRAWAL
- Designate covering force (rearguard)
- Step-by-step fallback (bounds)
- Bound 1: Rearguard holds, main force moves
- Bound 2: Main force covers, rearguard moves
- Continue until rally point reached

7.4 RALLY POINTS & REGROUPING
- Calculate rally point behind own lines
- Reformation after reaching rally
- Morale recovery at rally point
- Resume combat or continue retreat

7.5 STALEMATE PREVENTION
- If no significant change in 45 seconds:
  - Force action (commit reserve, attempt flank)
  - Prevents "staring contests"
- Timer resets on any major action

═══════════════════════════════════════════════════════════════════════════════
EDGE CASES
═══════════════════════════════════════════════════════════════════════════════

| Edge Case | Handling Strategy |
|-----------|-------------------|
| No reserve designated | Mark reserve as empty, skip logic |
| Rally point unreachable | Select nearest accessible point |
| Covering force destroyed | Accept losses, continue retreat |
| Stalemate timer but enemy routing | Cancel stalemate, pursue |
| Last stand triggered but won | Cancel last stand, exploit |

═══════════════════════════════════════════════════════════════════════════════
FILES TO CREATE
═══════════════════════════════════════════════════════════════════════════════

1. src/Features/Combat/RetreatCoordinator.cs
2. src/Features/Combat/RallyPointCalculator.cs
3. src/Features/Combat/Models/CombatPosture.cs (enum)

═══════════════════════════════════════════════════════════════════════════════
ACCEPTANCE CRITERIA
═══════════════════════════════════════════════════════════════════════════════

[ ] Reserve manager tracks and commits reserves
[ ] Casualty thresholds trigger posture changes
[ ] Organized withdrawal with covering force
[ ] Rally points calculated and used
[ ] Stalemate prevention triggers action
[ ] Logs show: "[BattleAI] Posture:Engaged→TacticalWithdrawal (Casualties:42%)"
[ ] Build succeeds
```

---

# Phase 8: Player Counter-Intelligence (T7+)

**Goal:** Tracking player formation, threat projection, counter-composition

**Status:** ⬜ TODO

**Prerequisites:** Phase 1-7 complete

```
I need you to implement Phase 8 (Player Counter-Intelligence) of the Battle AI system for my Bannerlord mod.

═══════════════════════════════════════════════════════════════════════════════
CRITICAL PROJECT CONSTRAINTS (from BLUEPRINT.md)
═══════════════════════════════════════════════════════════════════════════════

GAME TARGET: Bannerlord v1.3.13
API VERIFICATION: Use local decompile at C:\Dev\Enlisted\Decompile\
PROJECT BUILD: dotnet build -c "Enlisted RETAIL" /p:Platform=x64
CSPROJ: Manually add new files with <Compile Include="..."/>
LOGGING: Use ModLogger.Info("BattleAI", "message");
CODE QUALITY: Follow ReSharper/Qodana strictly, fix all warnings, use braces, no changelog comments
T7+ ACTIVATION: Only when player is T7+ (commander with own troops)

═══════════════════════════════════════════════════════════════════════════════
CONTEXT RECOVERY (Verify Phase 1-7 exist before starting)
═══════════════════════════════════════════════════════════════════════════════

Verify these key files exist from previous phases:
[ ] src/Features/Combat/BattleOrchestrator.cs (with OODA loop)
[ ] src/Features/Combat/TacticalDecisionEngine.cs
[ ] src/Features/Combat/ReserveManager.cs
[ ] src/Features/Combat/RetreatCoordinator.cs (Phase 7)

If missing, complete earlier phases first.

═══════════════════════════════════════════════════════════════════════════════
DOCUMENTATION TO READ
═══════════════════════════════════════════════════════════════════════════════

1. docs/Features/Combat/BATTLE-AI-IMPLEMENTATION-SPEC.md
   - Section 2: Phase 8 items (8.1-8.5)
   - Section 2b: Edge Cases for Phase 8

2. docs/Features/Combat/battle-ai-plan.md
   - Part 7: Player Counter-Intelligence (all sections)

═══════════════════════════════════════════════════════════════════════════════
PHASE 8 TASKS
═══════════════════════════════════════════════════════════════════════════════

8.1 PLAYER FORMATION TRACKING
- Identify player's formation
- Track: Position, velocity, facing
- Calculate threat angle to own formations

8.2 THREAT PROJECTION
- Predict player position 10-15 seconds ahead
- Use velocity extrapolation
- Adjust for terrain obstacles
- Log predictions

8.3 FLANK DETECTION
- Multiple signals for flanking:
  - Player angle > 45° from front
  - Player velocity toward flank
  - Player composition (cavalry = fast flank threat)
- Flank threat score: 0-1

8.4 COUNTER-COMPOSITION RESERVE
- Hold units that counter player's composition:
  - Player has cavalry → Hold spear infantry
  - Player has archers → Hold cavalry
  - Player has infantry → Hold ranged
- Only commit counters when player commits

8.5 FLANK RESPONSE DECISION TREE
- FlankThreat < 0.3: Ignore
- FlankThreat 0.3-0.6: Reposition flank guard
- FlankThreat 0.6-0.8: Commit reserve to intercept
- FlankThreat > 0.8: Compact formation, refuse flank

═══════════════════════════════════════════════════════════════════════════════
EDGE CASES
═══════════════════════════════════════════════════════════════════════════════

| Edge Case | Handling Strategy |
|-----------|-------------------|
| Player T7+ but in simulation | Check if player commanding |
| Player changes formation mid-battle | Update tracking |
| Player dismounts/mounts | Recalculate threat (speed change) |
| Player dead but battle continues | Stop tracking player |
| Player far from battle (scouting) | Reduce threat priority |
| Player has no troops (solo) | Treat as single high-value target |
| Player on AI's team | Skip counter-AI entirely |

═══════════════════════════════════════════════════════════════════════════════
FILES TO CREATE
═══════════════════════════════════════════════════════════════════════════════

1. src/Features/Combat/PlayerCounterAI.cs
2. src/Features/Combat/ThreatProjector.cs
3. src/Features/Combat/Models/FlankThreat.cs

═══════════════════════════════════════════════════════════════════════════════
ACCEPTANCE CRITERIA
═══════════════════════════════════════════════════════════════════════════════

[ ] Player formation tracked when T7+
[ ] Threat projection predicts player movement
[ ] Flank detection scores 0-1
[ ] Counter-composition reserves held
[ ] Flank response triggers at thresholds
[ ] Only activates when player is T7+ commander
[ ] Logs show: "[BattleAI] PlayerThreat:0.7 Response:InterceptReserve"
[ ] Build succeeds
```

---

# Phases 9-11: Polish (Combined)

**Goal:** Reinforcement intelligence, battle pacing, terrain/morale exploitation

**Status:** ⬜ TODO

**Prerequisites:** Phase 1-8 complete

```
I need you to implement Phases 9-11 (Polish) of the Battle AI system for my Bannerlord mod.

These are polish phases that can be done together in one chat.

═══════════════════════════════════════════════════════════════════════════════
CRITICAL PROJECT CONSTRAINTS (from BLUEPRINT.md)
═══════════════════════════════════════════════════════════════════════════════

GAME TARGET: Bannerlord v1.3.13
API VERIFICATION: Use local decompile at C:\Dev\Enlisted\Decompile\
PROJECT BUILD: dotnet build -c "Enlisted RETAIL" /p:Platform=x64
CSPROJ: Manually add new files with <Compile Include="..."/>
LOGGING: Use ModLogger.Info("BattleAI", "message");
CODE QUALITY: Follow ReSharper/Qodana strictly, fix all warnings, use braces, no changelog comments

═══════════════════════════════════════════════════════════════════════════════
CONTEXT RECOVERY (Verify Phase 1-8 exist before starting)
═══════════════════════════════════════════════════════════════════════════════

Verify these key files exist from previous phases:
[ ] src/Features/Combat/BattleOrchestrator.cs (core orchestrator)
[ ] src/Features/Combat/BattlePlanGenerator.cs (plan system)
[ ] src/Features/Combat/TacticalDecisionEngine.cs (tactical decisions)
[ ] src/Features/Combat/ReserveManager.cs (reserve system)
[ ] src/Features/Combat/PlayerCounterAI.cs (Phase 8)

If missing, complete earlier phases first.

═══════════════════════════════════════════════════════════════════════════════
DOCUMENTATION TO READ
═══════════════════════════════════════════════════════════════════════════════

1. docs/Features/Combat/BATTLE-AI-IMPLEMENTATION-SPEC.md
   - Section 2: Phases 9, 10, 11 items
   - Section 2b: Edge Cases for Phases 9-11

2. docs/Features/Combat/battle-ai-plan.md
   - Part 20: Reinforcement Intelligence
   - Part 19: Battle Pacing and Cinematics
   - Part 16: Terrain Exploitation
   - Part 17: Morale Exploitation

═══════════════════════════════════════════════════════════════════════════════
PHASE 9: REINFORCEMENT INTELLIGENCE
═══════════════════════════════════════════════════════════════════════════════

9.1 Strategic Wave Timing
- Hold/rush reinforcements based on battle state
- Losing? Rush reinforcements
- Winning? Delay for dramatic arrival

9.2 Formation-Aware Assignment
- Route reinforcements to formations by need
- Understrength formations get priority

9.3 Quality Distribution
- Spread elites across formations
- Don't put all veterans in one formation

9.4 Spawn Point Tactics
- Fight near your spawn for advantage
- Consider spawn point defense

9.5 Reinforcement Integration
- Form up before joining combat
- Brief morale/cohesion boost on arrival

═══════════════════════════════════════════════════════════════════════════════
PHASE 10: BATTLE PACING & CINEMATICS
═══════════════════════════════════════════════════════════════════════════════

10.1 Deliberate Approach Speed
- Don't blob forward immediately
- Form up, then advance as formation

10.2 Skirmish Phase
- Probing attacks before full engagement
- Archers/skirmishers lead

10.3 Dramatic Moments
- Champion duels (heroes engage each other)
- Last stands (few vs many)
- Banner drama (protect banner bearer)

10.4 Morale-Driven Endings
- Rout cascades when morale breaks
- Ordered withdrawal when losing

═══════════════════════════════════════════════════════════════════════════════
PHASE 11: TERRAIN & MORALE EXPLOITATION
═══════════════════════════════════════════════════════════════════════════════

11.1 High Ground Strategy
- Seek and hold elevation
- Bonus for ranged from high ground

11.2 Choke Point Exploitation
- Funnel enemies through narrow terrain
- Deny enemy flanking through chokes

11.3 Forest/Difficult Terrain
- Use forest to negate cavalry

11.4 Terrain-Aware Battle Plans
- Select plans based on terrain features

11.5 Morale Reading
- Target low-morale formations

11.6 Rout Triggering
- Focus fire to break enemy morale

11.7 Protecting Own Morale
- Shield fragile formations

11.8 Strategic Withdrawal Before Collapse
- Disengage before routing

═══════════════════════════════════════════════════════════════════════════════
FILES TO CREATE
═══════════════════════════════════════════════════════════════════════════════

1. src/Features/Combat/ReinforcementManager.cs
2. src/Features/Combat/BattlePacingManager.cs
3. src/Features/Combat/TerrainAnalyzer.cs
4. src/Features/Combat/MoraleExploiter.cs

═══════════════════════════════════════════════════════════════════════════════
ACCEPTANCE CRITERIA
═══════════════════════════════════════════════════════════════════════════════

[ ] Reinforcements routed intelligently
[ ] Deliberate approach, not blobbing
[ ] High ground sought when available
[ ] Low-morale targets prioritized
[ ] Build succeeds
```

---

# Phases 12-14: Formation Systems (Combined)

**Goal:** Formation doctrine, unit shapes, intelligent organization

**Status:** ⬜ TODO

**Prerequisites:** Phase 1-11 complete

```
I need you to implement Phases 12-14 (Formation Systems) of the Battle AI system for my Bannerlord mod.

These are tightly coupled formation systems that should be done together.

═══════════════════════════════════════════════════════════════════════════════
CRITICAL PROJECT CONSTRAINTS (from BLUEPRINT.md)
═══════════════════════════════════════════════════════════════════════════════

GAME TARGET: Bannerlord v1.3.13
API VERIFICATION: Use local decompile at C:\Dev\Enlisted\Decompile\
PROJECT BUILD: dotnet build -c "Enlisted RETAIL" /p:Platform=x64
CSPROJ: Manually add new files with <Compile Include="..."/>
LOGGING: Use ModLogger.Info("BattleAI", "message");
CODE QUALITY: Follow ReSharper/Qodana strictly, fix all warnings, use braces, no changelog comments

═══════════════════════════════════════════════════════════════════════════════
CONTEXT RECOVERY (Verify Phase 1-11 exist before starting)
═══════════════════════════════════════════════════════════════════════════════

Verify these key files exist from previous phases:
[ ] src/Features/Combat/BattleOrchestrator.cs
[ ] src/Features/Combat/BattlePlanGenerator.cs
[ ] src/Features/Combat/FormationOrganizer.cs (Phase 3 - you'll extend)
[ ] src/Features/Combat/ReinforcementManager.cs (Phase 9)
[ ] src/Features/Combat/TerrainAnalyzer.cs (Phase 11)

If missing, complete earlier phases first.

═══════════════════════════════════════════════════════════════════════════════
DOCUMENTATION TO READ
═══════════════════════════════════════════════════════════════════════════════

1. docs/Features/Combat/BATTLE-AI-IMPLEMENTATION-SPEC.md
   - Section 2: Phases 12, 13, 14 items
   - Section 2b: Edge Cases for Phases 12-14
   - Section 3.7: Formation Doctrine System
   - Section 3.8: Unit Type Formations

2. docs/Features/Combat/battle-ai-plan.md
   - Part 13: Formation Doctrine System
   - Part 15: Unit Type Formations
   - Part 12: Intelligent Formation Organization

═══════════════════════════════════════════════════════════════════════════════
PHASE 12: FORMATION DOCTRINE SYSTEM
═══════════════════════════════════════════════════════════════════════════════

12.1 Battle Scale Detection
- Lord Party Battle: 50-200 troops
- Army Battle: 400+ troops

12.2 Formation Count Logic
- Small: 2-3 formations
- Large: 4-6 formations

12.3 Formation Doctrines
- SingleDeepLine: 2-rank line
- ThreeWing: Left, center, right
- ExtendedThin: Wide frontage (outnumbered)
- HammerAnvil: Infantry + cavalry reserve

12.4 Line Depth Decisions
- 2-4 ranks based on numbers/threat

12.5 Counter-Formation Tactics
- Detect enemy doctrine, select counter

12.6 Troop Distribution
- Spread elites across formations

═══════════════════════════════════════════════════════════════════════════════
PHASE 13: UNIT TYPE FORMATIONS
═══════════════════════════════════════════════════════════════════════════════

13.1 Infantry Shapes
- Line, Shield Wall, Loose, Square, Circle

13.2 Archer Shapes
- Line, Loose, Scatter, Staggered

13.3 Cavalry Shapes
- Line, Skein/Wedge, Loose

13.4 Formation Positioning
- Archers 20-40m behind infantry
- Cavalry on flanks

13.5 Dynamic Formation Switching
- Shield Wall under archer fire
- Square when cavalry charges (80+ troops)

13.6 Width and Depth Control
- Match/exceed enemy frontage

13.7 Formation Facing/Movement
- Maintain facing during advance

13.8 Multi-Formation Coordination
- All formations move as coherent army

═══════════════════════════════════════════════════════════════════════════════
PHASE 14: INTELLIGENT FORMATION ORGANIZATION
═══════════════════════════════════════════════════════════════════════════════

14.1 Front-Line Scoring
- Score: +Armor, +Shield, +Tier, +MeleeSkill

14.2 Flank Spillover
- Rear troops work flanks when stable

14.3 Gap Filling Logic
- Middle ranks step up when front falls

14.4 Formation Safeguards
- Max deviation distance
- No chase beyond formation
- Instant recall on order

14.5 Positional Combat Behavior
- Front: aggressive
- Rear: defensive/ranged

═══════════════════════════════════════════════════════════════════════════════
FILES TO CREATE
═══════════════════════════════════════════════════════════════════════════════

1. src/Features/Combat/FormationDoctrineSystem.cs
2. src/Features/Combat/UnitFormationShapes.cs
3. src/Features/Combat/IntelligentFormationOrganizer.cs
4. src/Features/Combat/Models/FormationDoctrine.cs (enum)
5. src/Features/Combat/Models/FormationShape.cs (enum)

═══════════════════════════════════════════════════════════════════════════════
ACCEPTANCE CRITERIA
═══════════════════════════════════════════════════════════════════════════════

[ ] Battle scale correctly detected
[ ] Formation count appropriate for size
[ ] Infantry switches to Shield Wall under fire
[ ] Square formed when cavalry charges (80+ troops)
[ ] Heavy troops in front ranks
[ ] Formations move as coherent army
[ ] Build succeeds
```

---

# Phase 15: Plan Execution & Anti-Flip-Flop

**Goal:** State machine, anti-flip-flop rules, enemy composition recognition

**Status:** ⬜ TODO

**Prerequisites:** Phase 1-14 complete

```
I need you to implement Phase 15 (Plan Execution & Anti-Flip-Flop) of the Battle AI system for my Bannerlord mod.

═══════════════════════════════════════════════════════════════════════════════
CRITICAL PROJECT CONSTRAINTS (from BLUEPRINT.md)
═══════════════════════════════════════════════════════════════════════════════

GAME TARGET: Bannerlord v1.3.13
API VERIFICATION: Use local decompile at C:\Dev\Enlisted\Decompile\
PROJECT BUILD: dotnet build -c "Enlisted RETAIL" /p:Platform=x64
CSPROJ: Manually add new files with <Compile Include="..."/>
LOGGING: Use ModLogger.Info("BattleAI", "message");
CODE QUALITY: Follow ReSharper/Qodana strictly, fix all warnings, use braces, no changelog comments

═══════════════════════════════════════════════════════════════════════════════
CONTEXT RECOVERY (Verify Phase 1-14 exist before starting)
═══════════════════════════════════════════════════════════════════════════════

Verify these key files exist from previous phases:
[ ] src/Features/Combat/BattleOrchestrator.cs
[ ] src/Features/Combat/BattlePlanGenerator.cs (Phase 6 - you'll extend)
[ ] src/Features/Combat/FormationDoctrineSystem.cs (Phase 12)
[ ] src/Features/Combat/UnitFormationShapes.cs (Phase 13)

If missing, complete earlier phases first.

═══════════════════════════════════════════════════════════════════════════════
DOCUMENTATION TO READ
═══════════════════════════════════════════════════════════════════════════════

1. docs/Features/Combat/BATTLE-AI-IMPLEMENTATION-SPEC.md
   - Section 2: Phase 15 items (15.1-15.7)
   - Section 2b: Edge Cases for Phase 15
   - Section 3.9: Plan Execution State Machine

2. docs/Features/Combat/battle-ai-plan.md
   - Part 14: Battle Plan Generation (Plan Execution section)

═══════════════════════════════════════════════════════════════════════════════
PHASE 15 TASKS
═══════════════════════════════════════════════════════════════════════════════

15.1 PLAN EXECUTION STATE MACHINE
States:
- Forming: Initial deployment
- Advancing: Moving to contact
- Engaging: Active combat
- Exploiting: Pushing advantage
- Consolidating: Holding gains
- Pursuing: Chasing routed enemy
- Adapting: Changing plans
- Withdrawing: Organized retreat

Transitions based on:
- Distance to enemy
- Casualty rates
- Enemy state (routing, reinforcing)

15.2 ANTI-FLIP-FLOP RULES (CRITICAL)
- Minimum commitment: 30 seconds on any plan
- Cooldown after change: 20 seconds
- Max changes: 2 per 60 seconds
- Enforce via timestamps and counters

15.3 PHASE-BASED GATING
- Allow plan changes at natural phase transitions
- E.g., Engaging→Exploiting is natural time to adapt

15.4 LEGITIMATE CHANGE DETECTION
Allow plan changes for:
- Catastrophic failure (40%+ casualties in 20s)
- Enemy major shift (reinforcement wave)
- Clear new opportunity (enemy flank collapsed)

15.5 ENEMY COMPOSITION RECOGNITION
Classify enemy:
- Infantry Horde: >70% infantry
- Archer Heavy: >40% archers
- Cavalry Heavy: >40% cavalry
- Horse Archers: >30% mounted ranged
- Balanced: Mixed

15.6 DEFENSIVE COUNTER-FORMATIONS
- vs Infantry Horde: Standard line, archer attrition
- vs Archer Heavy: Shield wall advance, cavalry rush
- vs Cavalry Heavy: Spear screen, protect archers
- vs Horse Archers: Compact, wait for them to close

15.7 COMMITMENT TIMING
- When to engage vs delay
- Consider: terrain, reinforcements, morale

═══════════════════════════════════════════════════════════════════════════════
EDGE CASES
═══════════════════════════════════════════════════════════════════════════════

| Edge Case | Handling Strategy |
|-----------|-------------------|
| Battle starts in Engaging (close spawn) | Skip Forming/Advancing |
| Catastrophic failure false positive | Require sustained failure (10+ seconds) |
| Anti-flip-flop locks needed change | Emergency override for genuine catastrophe |
| Enemy composition changes | Re-evaluate periodically |
| State machine in invalid state | Reset to Engaging, log error |

═══════════════════════════════════════════════════════════════════════════════
FILES TO CREATE
═══════════════════════════════════════════════════════════════════════════════

1. src/Features/Combat/PlanExecutionStateMachine.cs
2. src/Features/Combat/AntiFlipFlopGuard.cs
3. src/Features/Combat/EnemyCompositionAnalyzer.cs
4. src/Features/Combat/Models/PlanPhase.cs (enum)
5. src/Features/Combat/Models/EnemyCompositionType.cs (enum)

═══════════════════════════════════════════════════════════════════════════════
ACCEPTANCE CRITERIA
═══════════════════════════════════════════════════════════════════════════════

[ ] State machine transitions through phases correctly
[ ] Anti-flip-flop prevents rapid plan changes
[ ] Legitimate emergencies bypass anti-flip-flop
[ ] Enemy composition correctly classified
[ ] Counter-formations selected appropriately
[ ] Logs show: "[BattleAI] Phase:Advancing→Engaging (ContactMade)"
[ ] Logs show: "[BattleAI] PlanChange BLOCKED (Cooldown:15s remaining)"
[ ] Build succeeds
```

---

# Phases 16-18: Advanced Polish (Combined)

**Goal:** Agent combat director, coordinated retreat details, reinforcement details

**Status:** ⬜ TODO

**Prerequisites:** Phase 1-15 complete

```
I need you to implement Phases 16-18 (Advanced Polish) of the Battle AI system for my Bannerlord mod.

These are advanced polish phases that complete the system.

═══════════════════════════════════════════════════════════════════════════════
CRITICAL PROJECT CONSTRAINTS (from BLUEPRINT.md)
═══════════════════════════════════════════════════════════════════════════════

GAME TARGET: Bannerlord v1.3.13
API VERIFICATION: Use local decompile at C:\Dev\Enlisted\Decompile\
PROJECT BUILD: dotnet build -c "Enlisted RETAIL" /p:Platform=x64
CSPROJ: Manually add new files with <Compile Include="..."/>
LOGGING: Use ModLogger.Info("BattleAI", "message");
CODE QUALITY: Follow ReSharper/Qodana strictly, fix all warnings, use braces, no changelog comments

═══════════════════════════════════════════════════════════════════════════════
CONTEXT RECOVERY (Verify Phase 1-15 exist before starting)
═══════════════════════════════════════════════════════════════════════════════

Verify these key files exist from previous phases:
[ ] src/Features/Combat/BattleOrchestrator.cs
[ ] src/Features/Combat/RetreatCoordinator.cs (Phase 7 - you'll extend)
[ ] src/Features/Combat/ReinforcementManager.cs (Phase 9 - you'll extend)
[ ] src/Features/Combat/PlanExecutionStateMachine.cs (Phase 15)
[ ] src/Features/Combat/AgentCombatTuner.cs (Phase 2)

If missing, complete earlier phases first.

═══════════════════════════════════════════════════════════════════════════════
DOCUMENTATION TO READ
═══════════════════════════════════════════════════════════════════════════════

1. docs/Features/Combat/BATTLE-AI-IMPLEMENTATION-SPEC.md
   - Section 2: Phases 16, 17, 18 items
   - Section 2b: Edge Cases for Phases 16-18
   - Section 3.10: Agent-Level Combat Director
   - Section 3.11: Coordinated Retreat System

2. docs/Features/Combat/battle-ai-plan.md
   - Part 21: Agent-Level Combat Director
   - Part 18: Coordinated Retreat
   - Part 20: Reinforcement Intelligence (details)

═══════════════════════════════════════════════════════════════════════════════
PHASE 16: AGENT-LEVEL COMBAT DIRECTOR
═══════════════════════════════════════════════════════════════════════════════

16.1 Champion Duel System
- Detect hero vs hero engagement
- Create "duel zone" where other agents don't interfere
- Allow dramatic 1v1 fights

16.2 Last Stand Scenarios
- Few agents remaining + surrounded
- Boost aggression, reduce blocking (go out fighting)
- Dramatic final moments

16.3 Banner Bearer Drama
- Identify banner bearers
- AI prioritizes protecting own banners
- AI prioritizes capturing enemy banners

16.4 Small Squad Actions
- 2-5 man coordinated assaults
- Synchronized attack on single target
- Used for elite squads

16.5 Integration with Formation AI
- Agent drama respects formation orders
- Never override critical tactical orders
- Drama is enhancement, not replacement

═══════════════════════════════════════════════════════════════════════════════
PHASE 17: COORDINATED RETREAT (Details)
═══════════════════════════════════════════════════════════════════════════════

17.1 Retreat Decision Logic
- When to retreat vs fight to death
- Consider: casualties, morale, escape route

17.2 Covering Force (Rearguard)
- Designate units to cover retreat
- Rearguard fights while main force withdraws

17.3 Step-by-Step Withdrawal
- Bounding overwatch: move, cover, move
- Organized fallback, not rout

17.4 Preserving Force
- When battle is lost, minimize casualties
- Don't throw lives away

17.5 Rally Points
- Calculate safe rally locations
- Regroup after retreat

═══════════════════════════════════════════════════════════════════════════════
PHASE 18: REINFORCEMENT DETAILS
═══════════════════════════════════════════════════════════════════════════════

18.1 Big Wave Strategic Impact
- 100+ troops = major battlefield event
- Coordinate timing for maximum impact

18.2 Wave Coordination Between Sides
- Time waves to counter enemy waves

18.3 Desperation Waves
- All-in spawning when losing badly
- Emergency reinforcement

18.4 Spawn Point Defense/Attack
- Protect own spawn
- Harass enemy spawn

18.5 Merge vs Second Line Decision
- Fresh troops join existing formations OR
- Form new second line
- Based on tactical need

═══════════════════════════════════════════════════════════════════════════════
FILES TO CREATE
═══════════════════════════════════════════════════════════════════════════════

1. src/Features/Combat/AgentCombatDirector.cs
2. src/Features/Combat/ChampionDuelManager.cs
3. src/Features/Combat/CoordinatedRetreatManager.cs
4. src/Features/Combat/ReinforcementWaveManager.cs

═══════════════════════════════════════════════════════════════════════════════
ACCEPTANCE CRITERIA
═══════════════════════════════════════════════════════════════════════════════

[ ] Champion duels occur between heroes
[ ] Last stand behavior triggers appropriately
[ ] Coordinated retreat with covering force
[ ] Step-by-step withdrawal working
[ ] Reinforcement waves timed strategically
[ ] Desperation waves trigger when losing
[ ] Build succeeds
```

---

# Quick Reference: Build & Test

```powershell
# Build command (PowerShell - use semicolon, not &&)
cd C:\Dev\Enlisted\Enlisted; dotnet build -c "Enlisted RETAIL" /p:Platform=x64

# Logs output location
C:\Program Files (x86)\Steam\steamapps\common\Mount & Blade II Bannerlord\Modules\Enlisted\Debugging\

# Decompile reference (API verification)
C:\Dev\Enlisted\Decompile\

# Key decompile files for Battle AI:
C:\Dev\Enlisted\Decompile\TaleWorlds.MountAndBlade\Mission.cs
C:\Dev\Enlisted\Decompile\TaleWorlds.MountAndBlade\MissionBehavior.cs
C:\Dev\Enlisted\Decompile\TaleWorlds.MountAndBlade\Team.cs
C:\Dev\Enlisted\Decompile\TaleWorlds.MountAndBlade\TeamQuerySystem.cs
C:\Dev\Enlisted\Decompile\TaleWorlds.MountAndBlade\Formation.cs
C:\Dev\Enlisted\Decompile\TaleWorlds.MountAndBlade\FormationQuerySystem.cs
C:\Dev\Enlisted\Decompile\TaleWorlds.MountAndBlade\Agent.cs
C:\Dev\Enlisted\Decompile\TaleWorlds.MountAndBlade\AgentDrivenProperties.cs
C:\Dev\Enlisted\Decompile\SandBox\SandboxAgentStatCalculateModel.cs
```

---

# Handoff Template

Use this template after completing any phase:

```
═══════════════════════════════════════════════════════════════════════════════
PHASE [X] HANDOFF NOTES
═══════════════════════════════════════════════════════════════════════════════

COMPLETED: [DATE]
BY: [AI MODEL]

FILES CREATED:
- [ ] path/to/file1.cs
- [ ] path/to/file2.cs

FILES MODIFIED:
- [ ] path/to/existing1.cs
- [ ] path/to/existing2.cs

ADDED TO ENLISTED.CSPROJ:
- [ ] <Compile Include="path\to\file1.cs"/>
- [ ] <Compile Include="path\to\file2.cs"/>

KEY APIs VERIFIED IN DECOMPILE:
- API1: exists, works as expected
- API2: exists, slight difference from docs

KEY DECISIONS MADE:
- Decision 1: chose X over Y because Z
- Decision 2: implemented using pattern A

THRESHOLDS/VALUES USED:
- Threshold 1: value
- Threshold 2: value

KNOWN ISSUES/TECH DEBT:
- Issue 1: description, future fix needed
- Issue 2: description

VERIFICATION PASSED:
[ ] Build succeeds
[ ] Logs appear correctly
[ ] No runtime errors
[ ] Feature works as expected

READY FOR PHASE [X+1]: YES/NO
```

---

**Last Updated:** 2025-12-31  
**Status:** Prompts Ready (Implementation Not Started)
