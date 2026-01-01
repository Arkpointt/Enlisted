# Tactical Enhancements Integration Map

**Purpose:** Maps advanced tactical behaviors from `tactical-formation-behavior-enhancement.md` and `advanced-tactical-behaviors.md` into specific phases of `BATTLE-AI-IMPLEMENTATION-SPEC.md`.

**Status:** Implementation roadmap for tactical enhancement integration.

---

## Enhancement → Phase Mapping

### Phase 1: Foundation & Utilities

**Add to Phase 1:**

| Enhancement | Where in Phase 1 | File Location | Priority |
|-------------|------------------|---------------|----------|
| **Activity Detection Utilities** | New: 1.6 | `TacticalUtilities.cs` | High |
| **Battle Joined Detection (Hysteresis)** | New: 1.7 | `TacticalUtilities.cs` | High |
| **Formation Activity Metrics** | New: 1.8 | `TacticalUtilities.cs` | High |

**New Work Items:**

```
1.6 Activity Detection Utilities
- IsFormationShooting(formation, threshold) - checks LastRangedAttackTime
- FormationFightingInMelee(formation, threshold) - checks LastMeleeAttackTime
- FormationActiveSkirmishersRatio(formation) - percentage actively shooting
Status: ⬜ TODO | Complexity: Low

1.7 Battle Joined Detection with Hysteresis
- HasBattleBeenJoined(mainInfantry, currentlyJoined, joinDistance)
- Uses +5m buffer to prevent flip-flopping (75m to join, 80m to un-join)
- Critical for cavalry timing and battle phase detection
Status: ⬜ TODO | Complexity: Low

1.8 Formation Activity State Tracking
- Track formation states: Idle, Moving, Shooting, InMelee, Retreating
- Used by orchestrator for tactical decisions
Status: ⬜ TODO | Complexity: Medium
```

---

### Phase 2: Agent Combat Enhancements

**Add to Phase 2:**

| Enhancement | Where in Phase 2 | File Location | Priority |
|-------------|------------------|---------------|----------|
| **Skill-Based Property Tuning** | Enhance 2.1-2.3 | `EnlistedAgentAiPatch.cs` | High |
| **Ranged Weapon Differentiation** | Enhance 2.3 | `EnlistedAgentAiPatch.cs` | Medium |
| **Objective-Aware Combat Modifiers** | New: 2.8 | `EnlistedAgentAiPatch.cs` | High |
| **Battle Phase Combat Modifiers** | New: 2.9 | `EnlistedAgentAiPatch.cs` | Medium |

**New/Enhanced Work Items:**

```
2.1 AgentDrivenProperties Tuning (ENHANCED)
Add skill-based calculation:
- AIBlockOnDecideAbility = aiLevel * 2f (clamped 0.3-1.0)
- AIParryOnDecideAbility = aiLevel * 2.2f (clamped 0.05-0.95)
- AiAttackOnDecideChance = aiLevel * 1.2f (clamped 0.1-0.8)
- AiDefendWithShieldDecisionChanceValue = hasShield ? 0.25 : 0.01
Status: ⬜ TODO | Complexity: Medium

2.3 Ranged AI Tuning (ENHANCED)
Add weapon-specific accuracy:
- Bows: AiShooterError = 0.003f * (1 - aiLevel * 0.7)
- Crossbows: AiShooterError = 0.001f * (1 - aiLevel * 0.7)
- Lead error: AiRangerLeadErrorMin/Max based on skill
Status: ⬜ TODO | Complexity: Medium

2.8 Objective-Aware Combat Modifiers (NEW)
Agent behavior adapts to formation objective:
- Attack: +20% aggression, -10% blocking
- Hold: -30% aggression, +20% blocking, +30% shield use
- Screen: -20% aggression, +10% blocking
- Fighting Retreat: -50% aggression, +40% blocking
Status: ⬜ TODO | Complexity: Medium

2.9 Battle Phase Combat Modifiers (NEW)
Agent behavior adapts to battle phase:
- Crisis: +15% aggression, +10% blocking (desperation)
- Rout: -40% aggression, -30% blocking/parry (panic)
- Pursuit: +30% aggression, -15% blocking (confidence)
Status: ⬜ TODO | Complexity: Low
```

---

### Phase 3: Formation Intelligence

**Add to Phase 3:**

| Enhancement | Where in Phase 3 | File Location | Priority |
|-------------|------------------|---------------|----------|
| **Multi-Tier Position Validation** | New: 3.8 | `FormationPositionValidator.cs` | High |
| **Tactical Position Scoring** | New: 3.9 | `FormationPositionValidator.cs` | High |
| **Suppression Detection & Response** | New: 3.10 | `FormationThreatResponse.cs` | High |
| **Arrangement Matching** | Enhance 3.4 | `FormationArrangementSelector.cs` | Medium |
| **Infantry Cavalry Detection** | New: 3.11 | `InfantryThreatResponse.cs` | High |
| **Flank Protection (Plan-Aware)** | New: 3.12 | `FlankProtectionBehavior.cs` | High |

**New/Enhanced Work Items:**

```
3.4 Formation Cohesion Levels (ENHANCED)
Add arrangement matching:
- When pinning enemy, mirror their arrangement (Line ↔ Loose)
- Emergency overrides: Square vs cavalry (80+ units), Shield Wall (shields), Loose (no shields)
- Suppression response: Loose if UnderRangedAttackRatio > 0.2 and distance > 40m
Status: ⬜ TODO | Complexity: Medium

3.8 Multi-Tier Position Validation (NEW)
Formation position validation with fallbacks:
1. Check if position valid (NavMesh + boundaries)
2. If invalid, search nearby positions (expanding circles)
3. Score positions tactically (height, cover, spacing)
4. Notify orchestrator if constraints prevent ideal position
Status: ⬜ TODO | Complexity: High

3.9 Tactical Position Scoring (NEW)
Evaluate position quality based on objective:
- Height advantage (terrain elevation)
- Cover from ranged fire (LOS checks)
- Approach quality (attack objectives)
- Formation spacing (can fit without clipping)
Score 0-2.0, pick best available position
Status: ⬜ TODO | Complexity: High

3.10 Suppression Detection & Response (NEW)
React to ranged fire:
- Detect: UnderRangedAttackRatio > 0.2f
- Response: Go loose (if far), reposition, fast reform
- Notify orchestrator for support/adjustment
Status: ⬜ TODO | Complexity: Medium

3.11 Infantry Cavalry Threat Detection (NEW)
Infantry detects and responds to charging cavalry:
- Detect: cavalry is closest enemy, within 35m, moving toward us (60° cone)
- Response: Stop, face threat, form square/shield wall/loose based on composition
- Request support from orchestrator (cavalry intercept)
- Calculate threat urgency: 10 / timeToImpact * (1.5 if main effort)
Status: ⬜ TODO | Complexity: High

3.12 Plan-Aware Flank Protection (NEW)
Flank protection based on battle plan:
- Left Hook: right flank screens while left attacks
- Hammer & Anvil: infantry holds (anvil) aggressively
- Delay: maximize delay, not fixed positioning
- Calculate intercept positions (60% between main effort and threat)
- Threat analysis: time to target, moving toward, counter-matchup
Status: ⬜ TODO | Complexity: High
```

---

### Phase 5: Tactical Decision Engine

**Add to Phase 5:**

| Enhancement | Where in Phase 5 | File Location | Priority |
|-------------|------------------|---------------|----------|
| **Archer Effectiveness Awareness** | Enhance 5.2 | `ArcherBehaviorDirector.cs` | High |
| **Cavalry Width Matching** | Enhance 5.4 | `CavalryCycleManager.cs` | Medium |
| **Formation Integrity Gates** | Enhance 5.4 | `CavalryCycleManager.cs` | High |
| **Enhanced Cavalry States** | Enhance 5.4 | `CavalryCycleManager.cs` | High |
| **Adaptive Cavalry Timers** | Enhance 5.4 | `AdaptiveTimingManager.cs` | Medium |
| **Plan-Based Cavalry Deployment** | Enhance 5.3 | `CavalryDeploymentDirector.cs` | High |
| **Intelligent Target Selection** | Enhance 5.4 | `CavalryTargetSelector.cs` | High |

**New/Enhanced Work Items:**

```
5.2 Archer Targeting Decisions (ENHANCED)
Add effectiveness awareness system:
- IsFormationHittingTargets: check casualties, LOS, hit ratio
- CheckLineOfSightToTarget: raycast from archer to target (detect terrain blocking)
- MicroAdjustForEffectiveness: if shooting but not hitting, try 5m adjustments
- EvaluateArcherPosition: score by LOS (critical), height, range, cover
- Hold position when effective, only move when ineffective
Status: ⬜ TODO | Complexity: High

5.3 Cavalry Reserve Timing (ENHANCED)
Add plan-based deployment:
- Hammer & Anvil: wait for infantry engaged (anvil set)
- Left/Right Hook: release when main effort 30-50m from enemy
- Delay: only charge if enemy cavalry threatens
- Center Punch: hold until exploitation phase (after breakthrough)
Status: ⬜ TODO | Complexity: High

5.4 Cavalry Cycle Charging (ENHANCED)
Add 9-state machine with integrity gates:
States: Reserve → Positioning → Charging → ChargingPast → Impact → Melee → Disengaging → Rallying → Reforming → Bracing
Integrity gates:
- Only advance when deviation < 5f (very tight)
- Reform until deviation < 12f (good cohesion)
- Rally when deviation > 25f (scattered)
Timers:
- Charge: 15s (adaptive: distance/speed + 5s)
- Melee: 5s (adaptive: +3s if winning, -2s if losing)
- Reform: 12s (adaptive: 8s if tight, 16s if scattered, *0.75 if suppressed)
Width matching: SetFormOrder(FormOrderCustom(target.Width))
Disengagement: when deviation > 25f OR speed < 2f OR casualties > 20%
Status: ⬜ TODO | Complexity: Very High

5.4.1 Cavalry Target Selection (NEW)
Plan-aware target scoring:
- Class priority: Archers (100), Infantry (60), Cavalry (30)
- Distance: optimal 50-80m (+40), too close <30m (-30)
- Plan relevance: Hammer & Anvil + engaged with infantry (+70)
- Vulnerability: suppressed (+30), loose (+25), small (<40 units +40)
- Approach angle: rear/flank approach (+40)
- Threat to team: calculate threat score, prioritize threats
Status: ⬜ TODO | Complexity: High
```

---

### Phase 15: Plan Execution & Anti-Flip-Flop

**Add to Phase 15:**

| Enhancement | Where in Phase 15 | File Location | Priority |
|-------------|------------------|---------------|----------|
| **Hysteresis Pattern (General)** | Enhance 15.2 | Throughout | High |

**Enhanced Work Items:**

```
15.2 Anti-Flip-Flop Rules (ENHANCED)
Apply hysteresis pattern throughout:
- Battle joined: 75m to join, 80m to un-join (+5m buffer)
- State transitions: require buffer before reversing
- Formation objectives: 30s minimum commitment
- Plan changes: 20s cooldown, max 2/60s
- Prevents oscillation in all AI decisions
Status: ⬜ TODO | Complexity: Medium
```

---

### Phase 16: Agent-Level Combat Director

**Add to Phase 16:**

| Enhancement | Where in Phase 16 | File Location | Priority |
|-------------|------------------|---------------|----------|
| **Bounded Agent Autonomy** | New: 16.6 | `AgentPositioningIntelligence.cs` | High |
| **Formation Cohesion Management** | New: 16.7 | `FormationCohesionManager.cs` | High |

**New Work Items:**

```
16.6 Bounded Agent Autonomy (NEW)
Infantry in melee makes micro-tactical decisions:
- Decisions: Attack, BackStep, FindAlly, FlankLeft, FlankRight
- Scoring: based on enemy vulnerability, local numerical advantage, isolation
- Bounded: max 5m deviation from formation position
- Gated: only for infantry, only in melee, only during Attack/Pin, not main effort
Status: ⬜ TODO | Complexity: High

16.7 Formation Cohesion Management (NEW)
Control autonomy radius by objective:
- Main Effort: 2m (very tight)
- Hold/Screen: 3m (tight)
- Attack/Pin: 5m (moderate)
- Fighting Retreat: 8m (loose, survival)
Agent autonomy allowed:
- Infantry in melee: Yes
- Cavalry: No (need mass)
- Archers: No (need positioning)
Status: ⬜ TODO | Complexity: Medium
```

---

## Implementation Priority

### High Priority (Do First)
1. **Phase 1 Utilities** - Foundation for everything else
2. **Phase 3.8-3.12** - Formation intelligence (position validation, threat detection)
3. **Phase 5.4 Enhanced** - Cavalry cycling with integrity gates
4. **Phase 2.8-2.9** - Objective/phase-aware agent combat

### Medium Priority (Do Second)
1. **Phase 5.2 Enhanced** - Archer effectiveness awareness
2. **Phase 5.3 Enhanced** - Plan-based cavalry deployment
3. **Phase 3.4 Enhanced** - Arrangement matching
4. **Phase 16.6-16.7** - Bounded agent autonomy

### Lower Priority (Polish)
1. **Phase 2.3 Enhanced** - Weapon-specific ranged tuning
2. **Phase 5.4.1** - Advanced cavalry target selection
3. **Adaptive timers** - Fine-tuning after base implementation works

---

## File Creation Checklist

**New Files to Create:**

```
src/Features/Combat/
├── TacticalUtilities.cs                    (Phase 1)
├── EnlistedAgentAiPatch.cs                 (Phase 2)
├── FormationPositionValidator.cs           (Phase 3)
├── FormationArrangementSelector.cs         (Phase 3)
├── InfantryThreatResponse.cs               (Phase 3)
├── FlankProtectionBehavior.cs              (Phase 3)
├── ArcherBehaviorDirector.cs               (Phase 5)
├── CavalryCycleManager.cs                  (Phase 5)
├── CavalryDeploymentDirector.cs            (Phase 5)
├── CavalryTargetSelector.cs                (Phase 5)
├── AdaptiveTimingManager.cs                (Phase 5)
├── AgentPositioningIntelligence.cs         (Phase 16)
└── FormationCohesionManager.cs             (Phase 16)
```

---

## Integration with Existing Phases

### How Enhancements Fit

**Phase 3 (Formation Intelligence):**
- BEFORE: Basic threat assessment, cohesion levels
- AFTER: + Position validation, suppression response, cavalry threat detection, plan-aware behaviors

**Phase 5 (Tactical Decision Engine):**
- BEFORE: Basic cavalry cycling, archer targeting
- AFTER: + Effectiveness awareness, integrity gates, adaptive timers, plan-based deployment

**Phase 2 (Agent Combat):**
- BEFORE: Basic agent property tuning
- AFTER: + Skill-based formulas, objective modifiers, phase modifiers

**Phase 16 (Agent-Level Director):**
- BEFORE: Champion duels, dramatic moments
- AFTER: + Bounded autonomy for infantry micro-decisions

---

## Testing Strategy

Each enhancement should be tested:

1. **Unit Tests**
   - Utilities: IsFormationShooting, HasBattleBeenJoined
   - Position scoring: known positions produce expected scores
   - Target selection: scoring produces correct priorities

2. **Integration Tests**
   - Cavalry: full charge cycle completes correctly
   - Archers: detect blocked LOS and reposition
   - Infantry: detect cavalry and form square
   - Agent autonomy: stays within bounds

3. **Battle Tests**
   - Different battle plans (Hammer & Anvil, Left Hook, etc.)
   - Different army compositions (cavalry heavy, archer heavy, etc.)
   - Different terrains (hills, forests, open plains)
   - Different battle phases (opening, crisis, pursuit)

---

## Edge Cases By Enhancement

These edge cases must be handled in all implementations. Use null checks, early returns, and graceful degradation.

### Activity Detection Utilities

| Edge Case | Handling Strategy |
|-----------|-------------------|
| `formation` is null | Return false/0, log warning |
| `formation.CountOfUnits` is 0 | Return false (avoid division by zero) |
| `agent.LastRangedAttackTime` is 0 | Treat as "never fired", not "fired at time 0" |
| `agent.LastMeleeAttackTime` is 0 | Treat as "never attacked" |
| `MBCommon.GetTotalMissionTime()` returns 0 | Early battle, return false for activity checks |
| Agent collection modified during iteration | Use `ApplyActionOnEachUnitViaBackupList` (creates copy) |
| All agents dead in formation | `CountOfUnits` is 0, early exit |

### Battle Joined Detection (Hysteresis)

| Edge Case | Handling Strategy |
|-----------|-------------------|
| `mainInfantry` is null | Return true (assume joined, safer) |
| `mainInfantry.CountOfUnits` is 0 | Return true (no infantry to check) |
| No enemy found | Return true (battle effectively over) |
| Distance is NaN (identical positions) | Return true (definitely joined if overlapping) |
| `currentlyJoined` never initialized | Default to false on first call |

### Position Validation

| Edge Case | Handling Strategy |
|-----------|-------------------|
| `desiredPosition` is invalid (Zero) | Use `formation.CachedMedianPosition` as fallback |
| `GetNavMesh()` returns `UIntPtr.Zero` | Position invalid, search nearby |
| No valid position found in search radius | Return current formation position, log warning |
| `objective` is null | Use default tactical scoring (balanced) |
| `Mission.Current` is null | Early exit, cannot validate |
| `IsPositionInsideBoundaries` fails | Clamp to nearest boundary point |
| All 8 sample positions invalid | Expand search radius (up to 50m), then give up |
| Formation is too large to fit at position | Score position as poor, not invalid |

### Line of Sight Checking

| Edge Case | Handling Strategy |
|-----------|-------------------|
| `Mission.Current.Scene` is null | Return true (assume LOS, safer than stuck) |
| `from` and `to` positions identical | Return true (trivially visible) |
| `from` position underground (negative Z) | Clamp to ground level before raycast |
| Raycast hits friendly unit | Consider as clear (friendly is not obstruction) |
| Raycast returns collision at 0 distance | Position overlaps obstacle, invalid |
| `GetNavMeshZ()` returns invalid height | Use `GetGroundVec3()` instead |
| Archer height offset puts position in terrain | Reduce offset, minimum 0.5m |

### Archer Effectiveness Awareness

| Edge Case | Handling Strategy |
|-----------|-------------------|
| `nearestEnemy` is null | Return to Approaching state |
| Target formation destroyed during shooting | Acquire new target, transition to Approaching |
| No micro-adjustment positions are valid | Stay in current position, log warning |
| `IsFormationShooting` returns true but `IsHitting` unknown | Assume effective if shooting with LOS |
| All candidate positions score 0 | Stay in current position (best available) |
| `MissileRangeAdjusted` is 0 | Use fallback range of 100m |
| Formation has no ranged weapons equipped | Skip archer behavior entirely |
| Height advantage calculation produces NaN | Treat as 0 height difference |

### Suppression Detection & Response

| Edge Case | Handling Strategy |
|-----------|-------------------|
| `UnderRangedAttackRatio` unavailable | Skip suppression response (native handles) |
| Formation already at loosest arrangement | No arrangement change needed |
| Suppression detected but no valid fallback position | Hold position, request support |
| Rapid suppression state changes (flip-flop) | Add 3s cooldown before toggling loose/line |
| Orchestrator is null when notifying | Log warning, continue without notification |

### Infantry Cavalry Threat Detection

| Edge Case | Handling Strategy |
|-----------|-------------------|
| No cavalry in battle | Skip cavalry detection entirely |
| Multiple cavalry formations charging | Respond to closest/highest threat score |
| `formation.CountOfUnits` < 80 for square | Use shield wall instead (if shields) |
| No shields in formation for shield wall | Use loose arrangement |
| Cavalry changes direction mid-detection | Recalculate on next tick |
| Cavalry threat disappears (destroyed/retreated) | Cancel threat response, resume normal behavior |
| Support request sent but orchestrator busy | Queue request, orchestrator handles when able |
| `TimeToImpact` is 0 or negative | Maximum urgency (1.0) |

### Flank Protection (Plan-Aware)

| Edge Case | Handling Strategy |
|-----------|-------------------|
| `plan` is null | Use default screening behavior |
| `MainEffortFormation` is null | Skip intercept calculation, use defensive position |
| No threats detected | Maintain default flank position |
| Threat formation destroyed while intercepting | Acquire new target or return to default |
| All positions blocked by terrain | Stay in current position, notify orchestrator |
| Multiple threats to main effort | Intercept highest-scored threat |
| Main effort formation destroyed | Become main effort or retreat |

### Cavalry State Machine

| Edge Case | Handling Strategy |
|-----------|-------------------|
| Timer is null when checked | Create new timer with default duration |
| `_targetFormation` becomes null during charge | Abort charge, transition to Reforming |
| Formation has 0 units mid-cycle | Cancel cycle, formation considered destroyed |
| `DeviationOfPositionsExcludeFarAgents` returns NaN | Treat as very high (50f), trigger reform |
| All cavalry killed during impact | End cycle, no further transitions |
| Target width is 0 | Use default cavalry width |
| Reform timer expires but still scattered | Force transition anyway, accept imperfect formation |
| Bracing state but no counter-charge opportunity | Stay bracing until orchestrator orders attack |
| Enemy cavalry charges while we're reforming | Interrupt reform, counter-charge |

### Cavalry Target Selection

| Edge Case | Handling Strategy |
|-----------|-------------------|
| No valid targets (all destroyed) | Return null, cavalry enters Reserve |
| All targets score equally (tie) | Prefer closest, then random |
| `_lastTarget` was destroyed | Clear `_lastTarget`, don't penalize |
| Target changes objective mid-scoring | Score based on current state |
| Plan type is null | Use default priority (archers > infantry > cavalry) |
| Distance to all targets > 100m | Select closest despite distance penalty |

### Adaptive Timers

| Edge Case | Handling Strategy |
|-----------|-------------------|
| `speed` is 0 (formation not moving) | Use maximum duration (20s for charge) |
| `killRatio` undefined (no kills yet) | Use base duration (5s for melee) |
| `deviation` is NaN | Use default reform duration (12s) |
| `UnderRangedAttackRatio` query fails | Don't apply suppression multiplier |
| Duration calculation produces negative | Clamp to minimum (3s) |
| Duration calculation produces infinity | Clamp to maximum (20s) |

### Bounded Agent Autonomy

| Edge Case | Handling Strategy |
|-----------|-------------------|
| `agent` is dead | Skip entirely |
| `agent.Formation` is null | Skip (agent not in formation) |
| `formationOrderPosition` is invalid | Use agent's current position |
| No enemies nearby | Return to formation position |
| No allies nearby for FindAlly decision | Fallback to BackStep or Attack |
| Agent is main effort (should have no autonomy) | Early exit, follow formation strictly |
| Deviation calculation produces negative distance | Use absolute value |
| All decision scores are 0 | Default to Attack |
| Agent autonomy disabled mid-fight | Smoothly return to formation position |

### Formation Cohesion Management

| Edge Case | Handling Strategy |
|-----------|-------------------|
| `context` is null | Return default autonomy radius (4m) |
| `objective` enum has unexpected value | Use default (4m) |
| Formation type unknown | Deny autonomy (safer) |
| Agent not in melee but within autonomy distance | Still allow, check range on decision |
| Autonomy radius changed mid-combat | Smoothly enforce new radius |

### General Cross-Cutting Edge Cases

| Edge Case | Handling Strategy |
|-----------|-------------------|
| Orchestrator is null (not enlisted) | All tactical behaviors skip orchestrator calls, use local logic |
| Mission ends during any tick | Check `Mission.Current?.IsEnding` at tick start |
| Formation destroyed mid-tick | Null check before every formation access |
| Team switches mid-battle (surrender) | Re-evaluate orchestrator state |
| Save/load during battle | Reinitialize timers, reset state machines to safe states |
| Performance spike (tick takes too long) | Cap iterations, defer complex calculations |
| Float precision issues (very small values) | Use epsilon comparisons for float equality |
| Harmony patch throws exception | Wrap in try-catch, log, continue with native behavior |

---

## Defensive Coding Patterns

Apply these patterns throughout all implementations:

### 1. Null-Check Everything

```csharp
// BAD
float distance = formation.CachedAveragePosition.Distance(target.CachedAveragePosition);

// GOOD
if (formation == null || target == null) return;
if (formation.CountOfUnits == 0 || target.CountOfUnits == 0) return;
float distance = formation.CachedAveragePosition.Distance(target.CachedAveragePosition);
```

### 2. Guard Division by Zero

```csharp
// BAD
float ratio = shootingCount / formation.CountOfUnits;

// GOOD
float ratio = formation.CountOfUnits > 0 
    ? (float)shootingCount / formation.CountOfUnits 
    : 0f;
```

### 3. Clamp All Calculations

```csharp
// BAD
float duration = timeToTarget + 5f;

// GOOD
float duration = MBMath.ClampFloat(timeToTarget + 5f, 10f, 20f);
```

### 4. Graceful Degradation

```csharp
// If orchestrator unavailable, fall back to local behavior
var orchestrator = Mission.Current?.GetMissionBehavior<EnlistedBattleBehavior>()?.Orchestrator;
if (orchestrator == null)
{
    // Use local-only logic without plan context
    return CalculateReactivePosition();
}
// Full plan-aware logic
return CalculatePlanPosition(orchestrator.CurrentPlan);
```

### 5. Try-Catch Critical Paths

```csharp
[HarmonyPatch(typeof(AgentStatCalculateModel), "SetAiRelatedProperties")]
public class EnlistedAgentAiPatch
{
    private static void Postfix(Agent agent, ref AgentDrivenProperties props)
    {
        try
        {
            ApplyEnlistedModifications(agent, ref props);
        }
        catch (Exception ex)
        {
            EnlistedLogger.LogError($"AgentAI patch failed: {ex.Message}");
            // Native properties remain unchanged (safe fallback)
        }
    }
}
```

### 6. Timer Safety

```csharp
// Always check timer before using
if (_chargeTimer == null)
{
    _chargeTimer = new Timer(Mission.Current.CurrentTime, CHARGE_DURATION, false);
}

if (_chargeTimer.Check(Mission.Current.CurrentTime))
{
    // Timer expired, transition state
}
```

### 7. State Machine Safety

```csharp
// Always have a default case
switch (_state)
{
    case CavalryState.Reserve:
        // ...
        break;
    case CavalryState.Charging:
        // ...
        break;
    // ... other cases ...
    default:
        EnlistedLogger.LogWarning($"Unknown cavalry state: {_state}, resetting to Reserve");
        _state = CavalryState.Reserve;
        break;
}
```

---

**Summary:** Every tactical enhancement from the detailed docs now has a specific home in the implementation spec. Start with Phase 1 utilities, then build up through Phases 2, 3, 5, and 16 as outlined above.
