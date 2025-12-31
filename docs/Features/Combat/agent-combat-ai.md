# Agent-Level Combat AI Reference

This document covers how individual AI soldiers fight, react, and make combat decisions. This is separate from formation/team-level AI (see [battle-ai-plan.md](battle-ai-plan.md)).

---

## Table of Contents

- [1. Overview](#1-overview)
- [2. Core Systems](#2-core-systems)
  - [2.1 AgentDrivenProperties](#21-agentdrivenproperties)
  - [2.2 AgentStatCalculateModel](#22-agentstatcalculatemodel)
  - [2.3 HumanAIComponent](#23-humanaicomponent)
- [3. Combat AI Properties (All 40+ AI Properties)](#3-combat-ai-properties)
  - [3.1 Melee Combat](#31-melee-combat)
  - [3.2 Blocking & Parrying](#32-blocking--parrying)
  - [3.3 Ranged Combat](#33-ranged-combat)
  - [3.4 Shield Usage](#34-shield-usage)
  - [3.5 Reaction Times & Decision Intervals](#35-reaction-times--decision-intervals)
  - [3.6 Movement & Positioning](#36-movement--positioning)
  - [3.7 Weapon Preferences](#37-weapon-preferences)
- [4. How AI Level Is Calculated](#4-how-ai-level-is-calculated)
- [5. BehaviorValueSet (Formation Context)](#5-behaviorvalueset-formation-context)
- [6. Modding Entry Points](#6-modding-entry-points)
  - [6.1 Custom AgentStatCalculateModel](#61-custom-agentstatcalculatemodel)
  - [6.2 Harmony Patches](#62-harmony-patches)
  - [6.3 Direct Agent Modification](#63-direct-agent-modification)
  - [6.4 Global AI Level Multiplier](#64-global-ai-level-multiplier)
- [7. Tuning Recommendations](#7-tuning-recommendations)
- [8. Key Thresholds & Defaults](#8-key-thresholds--defaults)

---

## 1. Overview

Every AI soldier in Bannerlord has **97 driven properties** that control their combat behavior. These determine:
- How accurately they attack/defend
- How quickly they react
- How well they aim ranged weapons
- When they decide to attack vs defend
- How they use shields
- Their movement patterns in combat

The key insight: **AI difficulty comes from these properties, not from damage/health cheats**.

---

## 2. Core Systems

### 2.1 AgentDrivenProperties

`AgentDrivenProperties` is a container of 97 float values that drive agent behavior. Each property is accessed via `DrivenProperty` enum.

**Location**: `TaleWorlds.MountAndBlade.AgentDrivenProperties`

```csharp
public class AgentDrivenProperties
{
    private readonly float[] _statValues = new float[97];
    
    public float GetStat(DrivenProperty propertyEnum);
    public void SetStat(DrivenProperty propertyEnum, float value);
    
    // Convenience properties like:
    public float AIBlockOnDecideAbility { get; set; }
    public float AiShooterError { get; set; }
    // ... 95 more
}
```

### 2.2 AgentStatCalculateModel

`AgentStatCalculateModel` is the abstract base class that calculates all agent stats. The game provides implementations like `SandboxAgentStatCalculateModel`.

**Location**: `TaleWorlds.MountAndBlade.AgentStatCalculateModel`

**Key Methods**:
```csharp
public abstract void InitializeAgentStats(Agent agent, Equipment spawnEquipment, 
    AgentDrivenProperties agentDrivenProperties, AgentBuildData agentBuildData);

public abstract void UpdateAgentStats(Agent agent, AgentDrivenProperties agentDrivenProperties);

protected void SetAiRelatedProperties(Agent agent, AgentDrivenProperties props, 
    WeaponComponentData equippedItem, WeaponComponentData secondaryItem);

protected float CalculateAILevel(Agent agent, int relevantSkillLevel);
```

### 2.3 HumanAIComponent

`HumanAIComponent` handles soldier-level behavior within formations:
- Formation integrity (staying in position)
- Item pickup (shields, arrows, banners)
- Mount finding (for dismounted cavalry)
- `BehaviorValueSet` for context-specific aggression

**Location**: `TaleWorlds.MountAndBlade.HumanAIComponent`

---

## 3. Combat AI Properties

### 3.1 Melee Combat

| Property | Type | Description | Range |
|----------|------|-------------|-------|
| `AIDecideOnAttackChance` | float | Base chance to decide to attack | 0.0–1.0 |
| `AIAttackOnDecideChance` | float | Attack decision likelihood | 0.05–0.48 |
| `AIAttackOnParryChance` | float | Counter-attack after successful parry | 0.0–0.08 |
| `AiAttackOnParryTiming` | float | How fast to counter-attack | -0.2–0.1 |
| `AiTryChamberAttackOnDecide` | float | Attempt chamber (attack-into-attack) | 0.0–0.085 |
| `AiKick` | float | Kick/bash ability | 0.0–0.3 |
| `AiAttackCalculationMaxTimeFactor` | float | Time spent calculating attack | 0.0–1.0 |
| `AiDecideOnAttackWhenReceiveHitTiming` | float | Recovery timing after being hit | -0.25–0.0 |
| `AiDecideOnAttackContinueAction` | float | Continue attack decision | -0.5–0.0 |
| `AiDecideOnAttackingContinue` | float | Keep attacking | 0.0–0.1 |
| `AIHoldingReadyMaxDuration` | float | Max time holding attack ready | 0.0–0.25 |
| `AIHoldingReadyVariationPercentage` | float | Variation in ready duration | 0.0–1.0 |

### 3.2 Blocking & Parrying

| Property | Type | Description | Range |
|----------|------|-------------|-------|
| `AIBlockOnDecideAbility` | float | Ability to decide to block correctly | 0.5–0.99 |
| `AIParryOnDecideAbility` | float | Ability to decide to parry correctly | 0.5–0.95 |
| `AIParryOnAttackAbility` | float | Parry while in attack animation | 0.0–1.0 |
| `AIParryOnAttackingContinueAbility` | float | Parry during attack continuation | 0.5–0.95 |
| `AiParryDecisionChangeValue` | float | Flexibility in parry decisions | 0.05–0.75 |
| `AiRandomizedDefendDirectionChance` | float | Chance to block WRONG direction | 0.0–1.0 |
| `AIRealizeBlockingFromIncorrectSideAbility` | float | Realize blocking wrong & correct | 0.0–1.0 |
| `AIDecideOnRealizeEnemyBlockingAttackAbility` | float | React to enemy blocking | 0.0–1.0 |
| `AISetNoAttackTimerAfterBeingHitAbility` | float | Recovery after being hit | 0.33–1.0 |
| `AISetNoAttackTimerAfterBeingParriedAbility` | float | Recovery after being parried | 0.2–1.0 |
| `AISetNoDefendTimerAfterHittingAbility` | float | Defend after landing hit | 0.1–0.99 |
| `AISetNoDefendTimerAfterParryingAbility` | float | Defend after parrying | 0.15–1.0 |
| `AIEstimateStunDurationPrecision` | float | Precision judging stun duration | 0.0–0.8 |

### 3.3 Ranged Combat

| Property | Type | Description | Range |
|----------|------|-------------|-------|
| `AiShooterError` | float | Base aiming error | ~0.008 |
| `AiShooterErrorWoRangeUpdate` | float | Error without range update | 0.0 |
| `AiRangerLeadErrorMin` | float | Min lead target error (negative = behind) | -0.35–0.0 |
| `AiRangerLeadErrorMax` | float | Max lead target error | 0.0–0.2 |
| `AiRangerVerticalErrorMultiplier` | float | Vertical aim error multiplier | 0.0–0.1 |
| `AiRangerHorizontalErrorMultiplier` | float | Horizontal aim error (radians) | 0.0–0.035 |
| `AiShootFreq` | float | How often they shoot | 0.3–1.0 |
| `AiWaitBeforeShootFactor` | float | Patience before shooting | 0.0–1.0 |
| `AiRangedHorsebackMissileRange` | float | Range for mounted archery | 0.3–0.7 |
| `WeaponInaccuracy` | float | Weapon-specific inaccuracy | 0.0–varies |

### 3.4 Shield Usage

| Property | Type | Description | Range |
|----------|------|-------------|-------|
| `AiDefendWithShieldDecisionChanceValue` | float | Chance to use shield | 0.5–2.0 |
| `AiAttackingShieldDefenseChance` | float | Use shield while attacking | 0.2–0.5 |
| `AiAttackingShieldDefenseTimer` | float | Duration of shield use | -0.3–0.0 |
| `AiRaiseShieldDelayTimeBase` | float | Delay before raising shield | -0.75–-0.25 |
| `AiUseShieldAgainstEnemyMissileProbability` | float | Probability to shield against arrows | 0.1–0.9 |

### 3.5 Reaction Times & Decision Intervals

| Property | Type | Description | Default |
|----------|------|-------------|---------|
| `AiCheckApplyMovementInterval` | float | How often movement is applied | ~0.05–0.1 |
| `AiCheckCalculateMovementInterval` | float | How often movement is calculated | 0.25–0.5 |
| `AiCheckDecideSimpleBehaviorInterval` | float | How often simple behaviors are decided | 0.3–1.5 |
| `AiCheckDoSimpleBehaviorInterval` | float | How often simple behaviors execute | 1.0–2.0 |
| `AiMovementDelayFactor` | float | Movement reaction delay | ~1.0–1.33 |

### 3.6 Movement & Positioning

| Property | Type | Description | Range |
|----------|------|-------------|-------|
| `AiMoveEnemySideTimeValue` | float | Strafing around enemy | -2.5–-2.0 |
| `AiMinimumDistanceToContinueFactor` | float | Min distance to continue attack | 2.3–2.9 |
| `AiChargeHorsebackTargetDistFactor` | float | Horseback charge distance | 3.0–4.5 |
| `AiFacingMissileWatch` | float | Awareness of incoming missiles | -0.96–-0.9 |
| `AiFlyingMissileCheckRadius` | float | Radius to check for missiles | 2.0–8.0 |

### 3.7 Weapon Preferences

| Property | Type | Description | Default |
|----------|------|-------------|---------|
| `AiWeaponFavorMultiplierMelee` | float | Preference for melee weapons | 1.0 |
| `AiWeaponFavorMultiplierRanged` | float | Preference for ranged weapons | 1.0 |
| `AiWeaponFavorMultiplierPolearm` | float | Preference for polearms | 1.0 |

---

## 4. How AI Level Is Calculated

The native `AgentStatCalculateModel` calculates AI properties from skills:

```csharp
protected float CalculateAILevel(Agent agent, int relevantSkillLevel)
{
    float difficultyModifier = GetDifficultyModifier();
    float skillFactor = relevantSkillLevel / 300f;
    
    // Difficulty affects the maximum AI level achievable
    float maxLevel = difficultyModifier <= 0.0 ? 0.1f 
                   : difficultyModifier <= 0.5 ? 0.32f 
                   : 0.96f;
    
    return Clamp(skillFactor * maxLevel, 0f, 1f);
}
```

**Difficulty Modifiers**:
- Easy: 0.0 → max AI level 0.1
- Normal: 0.5 → max AI level 0.32
- Hard: 1.0 → max AI level 0.96

Then properties are derived from this AI level:

```csharp
protected void SetAiRelatedProperties(Agent agent, AgentDrivenProperties props, ...)
{
    float meleeAI = CalculateAILevel(agent, meleeSkill);
    float rangedAI = CalculateAILevel(agent, rangedSkill);
    
    // Blocking: 0.5 at worst → 0.99 at best
    props.AIBlockOnDecideAbility = Lerp(0.5f, 0.99f, Pow(meleeAI, 0.5f));
    
    // Wrong block direction: 100% at worst → 0% at best
    props.AiRandomizedDefendDirectionChance = 1f - Pow(meleeAI, 3f);
    
    // Shooting frequency: 30% at worst → 100% at best
    props.AiShootFreq = 0.3f + 0.7f * rangedAI;
    
    // Lead error: high at worst → near zero at best
    props.AiRangerLeadErrorMin = -(1f - rangedAI) * 0.35f;
    props.AiRangerLeadErrorMax = (1f - rangedAI) * 0.2f;
    
    // ... 35+ more properties calculated similarly
}
```

---

## 5. BehaviorValueSet (Formation Context)

`HumanAIComponent` uses `BehaviorValueSet` to adjust individual aggression based on formation state:

| Set | When Used | Effect |
|-----|-----------|--------|
| `Default` | Normal combat | Balanced attack/defend |
| `DefensiveArrangementMove` | ShieldWall/Square/Circle | Prioritize position over combat |
| `Follow` | Following leader | Reduced aggression |
| `DefaultMove` | Moving in formation | Standard movement focus |
| `Charge` | Charging | Maximum aggression |
| `DefaultDetached` | Detached from formation | Independent combat |

Each set configures 7 behavior weights:
- `GoToPos` - Move to position priority
- `Melee` - Melee combat priority
- `Ranged` - Ranged combat priority
- `ChargeHorseback` - Mounted charge priority
- `RangedHorseback` - Mounted archery priority
- `AttackEntityMelee` - Attack objects (gates, etc.)
- `AttackEntityRanged` - Shoot objects

---

## 6. Modding Entry Points

### 6.1 Custom AgentStatCalculateModel

**Best approach for comprehensive changes.**

```csharp
public class EnlistedAgentStatCalculateModel : SandboxAgentStatCalculateModel
{
    public override void InitializeAgentStats(Agent agent, Equipment spawnEquipment,
        AgentDrivenProperties props, AgentBuildData data)
    {
        base.InitializeAgentStats(agent, spawnEquipment, props, data);
        
        // Boost AI after base calculation
        EnhanceAiProperties(agent, props);
    }
    
    public override void UpdateAgentStats(Agent agent, AgentDrivenProperties props)
    {
        base.UpdateAgentStats(agent, props);
        EnhanceAiProperties(agent, props);
    }
    
    private void EnhanceAiProperties(Agent agent, AgentDrivenProperties props)
    {
        // Make blocking more reliable
        props.AIBlockOnDecideAbility = Math.Min(0.99f, props.AIBlockOnDecideAbility * 1.2f);
        
        // Reduce wrong block direction
        props.AiRandomizedDefendDirectionChance *= 0.5f;
        
        // Improve aim
        props.AiShooterError *= 0.7f;
        props.AiRangerLeadErrorMin *= 0.7f;
        props.AiRangerLeadErrorMax *= 0.7f;
        
        // Faster reactions
        props.AiCheckApplyMovementInterval *= 0.8f;
        props.AiMovementDelayFactor *= 0.9f;
    }
}

// Register in SubModule:
protected override void OnGameStart(Game game, IGameStarter starter)
{
    if (starter is CampaignGameStarter campaignStarter)
    {
        // Replace the model
        campaignStarter.AddModel(new EnlistedAgentStatCalculateModel());
    }
}
```

### 6.2 Harmony Patches

**For targeted modifications without replacing the whole model.**

```csharp
[HarmonyPatch(typeof(AgentStatCalculateModel), "SetAiRelatedProperties")]
public class AiPropertiesPatch
{
    [HarmonyPostfix]
    public static void Postfix(Agent agent, AgentDrivenProperties agentDrivenProperties)
    {
        // Only boost enemy AI
        if (agent.Team?.IsPlayerAlly == false)
        {
            agentDrivenProperties.AIBlockOnDecideAbility *= 1.3f;
            agentDrivenProperties.AiShooterError *= 0.6f;
        }
    }
}
```

### 6.3 Direct Agent Modification

**For per-agent changes at runtime.**

```csharp
// In a MissionBehavior:
public override void OnAgentBuild(Agent agent, Banner banner)
{
    if (!agent.IsHuman || agent.IsPlayerControlled)
        return;
    
    // Elite soldiers get better AI
    if (IsEliteSoldier(agent))
    {
        agent.AgentDrivenProperties.AIBlockOnDecideAbility = 0.95f;
        agent.AgentDrivenProperties.AiRandomizedDefendDirectionChance = 0.1f;
        agent.AgentDrivenProperties.AiShooterError = 0.004f;
        agent.UpdateAgentProperties();  // Apply changes
    }
}
```

### 6.4 Global AI Level Multiplier

**Simplest approach for uniform difficulty adjustment.**

```csharp
// At mission start:
MissionGameModels.Current.AgentStatCalculateModel.SetAILevelMultiplier(1.5f);

// Reset when done:
MissionGameModels.Current.AgentStatCalculateModel.ResetAILevelMultiplier();
```

This multiplies the AI level calculated from skills, affecting all derived properties.

---

## 7. Tuning Recommendations

### For More Realistic/Difficult Combat

| Goal | Properties to Modify |
|------|---------------------|
| **Better blocking** | `AIBlockOnDecideAbility` ↑, `AiRandomizedDefendDirectionChance` ↓ |
| **More accurate aim** | `AiShooterError` ↓, `AiRangerLeadErrorMin/Max` → 0 |
| **Faster reactions** | `AiCheckApplyMovementInterval` ↓, `AiMovementDelayFactor` ↓ |
| **More aggressive** | `AIAttackOnDecideChance` ↑, `AIDecideOnAttackChance` ↑ |
| **Better counter-attacks** | `AIAttackOnParryChance` ↑, `AiAttackOnParryTiming` → 0 |
| **Smarter shield use** | `AiUseShieldAgainstEnemyMissileProbability` ↑, `AiDefendWithShieldDecisionChanceValue` ↑ |

### Example: "Veteran" AI Profile

```csharp
void ApplyVeteranProfile(AgentDrivenProperties props)
{
    // Combat awareness
    props.AIBlockOnDecideAbility = 0.92f;
    props.AIParryOnDecideAbility = 0.85f;
    props.AiRandomizedDefendDirectionChance = 0.15f;
    
    // Attack skill
    props.AIAttackOnDecideChance = 0.4f;
    props.AIAttackOnParryChance = 0.12f;
    props.AiTryChamberAttackOnDecide = 0.08f;
    
    // Reactions
    props.AISetNoAttackTimerAfterBeingHitAbility = 0.9f;
    props.AISetNoDefendTimerAfterHittingAbility = 0.85f;
    
    // Ranged
    props.AiShooterError = 0.005f;
    props.AiShootFreq = 0.9f;
    props.AiRangerLeadErrorMin = -0.08f;
    props.AiRangerLeadErrorMax = 0.05f;
}
```

### Example: "Elite Guard" AI Profile

```csharp
void ApplyEliteGuardProfile(AgentDrivenProperties props)
{
    // Near-perfect blocking
    props.AIBlockOnDecideAbility = 0.98f;
    props.AIParryOnDecideAbility = 0.95f;
    props.AiRandomizedDefendDirectionChance = 0.05f;
    props.AIRealizeBlockingFromIncorrectSideAbility = 0.95f;
    
    // Expert timing
    props.AISetNoAttackTimerAfterBeingHitAbility = 0.98f;
    props.AISetNoDefendTimerAfterHittingAbility = 0.95f;
    props.AIEstimateStunDurationPrecision = 0.05f;
    
    // Aggressive when opportunity
    props.AIAttackOnParryChance = 0.2f;
    props.AiAttackOnParryTiming = 0.1f;
    props.AIDecideOnRealizeEnemyBlockingAttackAbility = 0.9f;
    
    // Fast reactions
    props.AiCheckApplyMovementInterval = 0.03f;
    props.AiMovementDelayFactor = 0.8f;
}
```

---

## 8. Key Thresholds & Defaults

| Property | Worst AI | Average AI | Best AI |
|----------|----------|------------|---------|
| `AIBlockOnDecideAbility` | 0.50 | 0.75 | 0.99 |
| `AIParryOnDecideAbility` | 0.50 | 0.72 | 0.95 |
| `AiRandomizedDefendDirectionChance` | 1.00 | 0.50 | 0.00 |
| `AiShooterError` | 0.008 | 0.008 | 0.008 |
| `AiShootFreq` | 0.30 | 0.65 | 1.00 |
| `AiRangerLeadErrorMax` | 0.20 | 0.10 | 0.00 |
| `AiCheckApplyMovementInterval` | 0.10 | 0.07 | 0.05 |

### Difficulty Modifiers

| Difficulty | Modifier | Max AI Level |
|------------|----------|--------------|
| Very Easy | 0.0 | 10% |
| Easy | 0.25 | 21% |
| Normal | 0.5 | 32% |
| Hard | 0.75 | 64% |
| Very Hard | 1.0 | 96% |

---

## Next Steps

This document provides the foundation for upgrading agent-level combat AI. Combine with:
- [battle-ai-plan.md](battle-ai-plan.md) - Formation/team-level AI
- Implementation plan (to be created)

Potential upgrade approaches:
1. **Skill-based profiles** - Better soldiers fight smarter
2. **Context-aware adjustments** - Defensive when outnumbered
3. **Fatigue integration** - Tired soldiers have worse reactions
4. **Experience system** - Soldiers improve over battles
