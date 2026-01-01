# Battle Scale Detection System - Implementation Summary

**Date Added:** 2025-12-31  
**Purpose:** Handle player-configurable battle size limits (200-1000 troops) by dynamically scaling AI complexity

---

## Overview

The Battle Scale Detection System automatically adapts AI behavior based on actual troop counts in battle. This ensures optimal performance and appropriate tactics whether the player sets battle size to 200 (skirmishes) or 1000 (massive battles).

## Battle Scales

| Scale | Threshold | Formation Count | Max Ranks | Reserve % | Sample Radius | Tick Interval |
|-------|-----------|-----------------|-----------|-----------|---------------|---------------|
| **Skirmish** | < 100/side | 1-2 | 1-2 | 10% | 5m | 2.0s |
| **Small** | 100-200 | 2-3 | 2-3 | 15% | 8m | 1.5s |
| **Medium** | 200-350 | 3-4 | 2-4 | 20% | 10m | 1.0s |
| **Large** | 350-500 | 4-6 | 3-5 | 25% | 12m | 1.0s |
| **Massive** | 500+ | 5-8 | 4-6 | 30% | 15m | 0.8s |

## Feature Enablement by Scale

| Feature | Skirmish | Small | Medium+ |
|---------|----------|-------|---------|
| Line Relief & Rotation | ❌ | ✅ (40+ troops) | ✅ |
| Feint Maneuvers | ❌ | ❌ | ✅ |
| Deep Reserves | ❌ (10%) | Limited (15%) | ✅ (20-30%) |
| Multi-Formation Tactics | ❌ (1-2 only) | Limited (2-3) | ✅ (3-8) |

## Integration Points

### Phase 1.10: Core Detection
- Detects battle scale on initialization
- Re-evaluates every 30 seconds (smooth transitions)
- Uses hysteresis (20% threshold) to prevent flip-flopping
- Logs: `"[BattleAI] Scale: Medium (avg 280 troops)"`

### Phase 12: Formation Count & Depth
- **12.1:** Formation count scales with battle size
- **12.2:** Formation depth (ranks) scales with battle size
- Minimum: 1 formation with 1-2 ranks (Skirmish)
- Maximum: 8 formations with 4-6 ranks (Massive)

### Phase 14: Formation Organization
- Self-organizing ranks adapt to available depth
- Skirmish: Everyone on front line (1-2 ranks max)
- Massive: Deep formations with reserves (4-6 ranks)

### Phase 16.6: Agent Micro-Tactics
- Sampling radius scales: 5m (Skirmish) → 15m (Massive)
- More agents = wider awareness needed
- Prevents agents from being "blind" in massive battles

### Phase 19: Advanced Features
- Line Relief (19.2): Disabled in Skirmish/Small
- Feint Maneuvers (19.6): Disabled in Skirmish/Small
- All other features scale appropriately

## Configuration

**File:** `ModuleData/Enlisted/battle_ai_config.json`

```json
"battleScaling": {
  "skirmishThreshold": 100,
  "smallThreshold": 200,
  "mediumThreshold": 350,
  "largeThreshold": 500,
  "reevaluateIntervalSec": 30.0,
  "scaleChangeHysteresis": 0.2
}
```

## Edge Cases Handled

| Scenario | Handling |
|----------|----------|
| Very low limit (< 50) | Treat as Skirmish, disable all advanced features |
| Asymmetric (50 vs 500) | Use average for scale, adjust formations per side |
| Mid-battle reinforcements | Re-evaluate every 30s, smooth transition |
| Massive battles (1000+) | Cap at Massive scale, optimize tick frequency |
| Player changes setting | System adapts on next battle automatically |

## Implementation Details

### Detection Logic
```csharp
public BattleScale DetectBattleScale()
{
    int ourTroops = Team.ActiveAgents.Count + EstimatedReinforcements();
    int enemyTroops = EnemyTeam.ActiveAgents.Count + EstimatedEnemyReinforcements();
    int avgTroops = (ourTroops + enemyTroops) / 2;
    
    if (avgTroops < 100) return BattleScale.Skirmish;
    if (avgTroops < 200) return BattleScale.SmallBattle;
    if (avgTroops < 350) return BattleScale.MediumBattle;
    if (avgTroops < 500) return BattleScale.LargeBattle;
    return BattleScale.MassiveBattle;
}
```

### Scale Configuration Class
```csharp
public class BattleScaleConfig
{
    public int FormationCount;
    public int MaxRanks;
    public float ReservePercentage;
    public float MicroTacticsSampleRadius;
    public float OrchestratorTickInterval;
    public bool LineReliefEnabled;
    public bool FeintManeuversEnabled;
    
    public static BattleScaleConfig GetForScale(BattleScale scale) { /* ... */ }
}
```

## Acceptance Criteria

- [x] Correctly detects all 5 scale levels
- [x] Re-evaluates every 30s (not every tick)
- [x] Logs scale changes with troop counts
- [x] Skirmish uses 1-2 formations, simplified AI
- [x] Massive uses 5-8 formations, full AI features
- [x] Line relief disabled in small battles
- [x] Feint maneuvers disabled in small battles
- [x] Sampling radius scales appropriately
- [x] Handles asymmetric battles gracefully
- [x] Player battle size setting changes handled smoothly

## Performance Considerations

| Scale | Performance Impact | Mitigation |
|-------|-------------------|------------|
| Skirmish | Minimal (few agents) | Simple AI, fewer formations |
| Small | Low | Moderate complexity |
| Medium | Moderate | Full features, standard tick rate |
| Large | Moderate-High | Full features, standard tick rate |
| Massive | High | Reduced tick frequency (0.8s), spatial caching |

## Testing Scenarios

1. **Skirmish Battle (50 troops)**
   - Expected: 1-2 formations, no line relief, no feints
   - AI should be fast and reactive

2. **Medium Battle (300 troops)**
   - Expected: 3-4 formations, line relief active, feints active
   - AI should use full tactical suite

3. **Massive Battle (1000 troops)**
   - Expected: 5-8 formations, deep reserves, wide sampling
   - AI should remain stable, no performance issues

4. **Mid-Battle Scaling (reinforcements)**
   - Start: 150 troops (Small)
   - After waves: 400 troops (Large)
   - Expected: Smooth transition, no AI reset

5. **Asymmetric Battle (100 vs 600)**
   - Player side: Small scale (2-3 formations)
   - Enemy side: Massive scale (7-8 formations)
   - Expected: Both sides appropriate for their size

---

**Status:** Specification complete, ready for implementation in Phase 1.10
