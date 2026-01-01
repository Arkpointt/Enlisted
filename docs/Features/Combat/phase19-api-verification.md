# Phase 19: Battlefield Realism Enhancements - API Verification Report

**Date:** 2025-12-31  
**Purpose:** Verify native API support for all Phase 19 systems before implementation

---

## ✅ **VERIFIED FEASIBLE** (8 Systems)

### 1. **Ammunition Tracking & Behavior** ✅
**API Support:**
- `MissionEquipment.GetAmmoAmount(EquipmentIndex)` - Returns current ammo count
- `MissionEquipment.GetMaxAmmo(EquipmentIndex)` - Returns max ammo capacity
- `MissionWeapon.Amount` - Current amount property

**Implementation Path:**
- Read ammo during AgentCombatDirector tick
- Modify behavior when ammo < 30% (seek resupply, retreat to reserve)
- Detect when ammo == 0 (switch to melee, reposition)

**Files:**
- `TaleWorlds.MountAndBlade\MissionEquipment.cs:288-312`

---

### 2. **Line Relief & Rotation** ✅
**API Support:**
- `IFormationUnit.FormationRankIndex` - Already using in Phase 14
- `Formation.GetUnitPositionWithIndex()` - Get unit position in formation
- Agent movement orders - Can reposition agents

**Implementation Path:**
- Detect front rank casualties > 40%
- Swap front rank (0) with rank 1 agents
- Move fatigued agents to rank 2+ (reserve)
- Use existing formation organization from Phase 14

**Notes:**
- This integrates with existing Phase 14 self-organizing ranks
- Already have positional scoring system

---

### 3. **Morale Contagion** ✅
**API Support:**
- `CommonAIComponent.Morale` - **HAS SETTER** (line 42)
  ```csharp
  public float Morale
  {
      get => this._morale;
      set => this._morale = MBMath.ClampFloat(value, 0.0f, 100f);
  }
  ```
- `Agent.GetComponent<CommonAIComponent>()` - Access morale component

**Implementation Path:**
- Sample nearby allies within 10m
- Calculate average morale
- Apply contagion: `newMorale = currentMorale + (avgMorale - currentMorale) * 0.15f * dt`
- Cap changes to ±2 per second

**Files:**
- `TaleWorlds.MountAndBlade\CommonAIComponent.cs:39-43`

---

### 4. **Commander Death Response** ✅
**API Support:**
- `Formation.Captain` - Public getter/setter (line 422)
- `Mission.FormationCaptainChanged` - Event for captain changes
- `Mission.OnFormationCaptainChanged(Formation)` - Event handler

**Implementation Path:**
- Subscribe to `FormationCaptainChanged` event
- On captain death/change, apply temporary morale penalty (-10)
- Apply combat effectiveness reduction (15% for 30 seconds)
- Optional: trigger formation reorganization

**Files:**
- `TaleWorlds.MountAndBlade\Formation.cs:422-426`
- `TaleWorlds.MountAndBlade\Mission.cs:1240, 5612`

---

### 5. **Breaking Point Detection** ✅
**API Support:**
- `Formation.CountOfUnits` - Current unit count
- `Formation.QuerySystem.CasualtyRatio` - Existing casualty tracking
- `CommonAIComponent.Morale` - Read morale state

**Implementation Path:**
- Check formation state each tick
- Detect: casualties > 60% AND morale < 20
- Trigger: mass rout, abandon formation, flee
- Persist for realism (no instant recovery)

**Notes:**
- Uses existing data, no new APIs needed

---

### 6. **Feint Maneuvers** ✅
**API Support:**
- `Formation.SetMovementOrder()` - Issue movement orders
- `Formation.OrderPosition` - Current order position
- `Timer` - Timing for feint duration

**Implementation Path:**
- Strategic Orchestrator detects opportunity (enemy flank exposed)
- Issue false charge order (advance 20m)
- After 3-5 seconds, withdraw
- Enemy AI reacts (repositions)
- Resume actual attack on different vector

**Notes:**
- Uses existing formation movement APIs
- Timing and coordination done at Orchestrator level

---

### 7. **Pursuit Depth Control** ✅
**API Support:**
- `Agent.Position` - Agent world position
- `Formation.OrderPosition` - Formation anchor point
- `Agent.GetRetreatPos()` - Already exists for retreating

**Implementation Path:**
- Track distance from formation anchor
- If pursuing agent > maxPursuitDepth (25m-40m), recall
- Apply recall via agent behavior override
- Prevents overextension and flanking

**Notes:**
- Uses existing positional data
- Integrates with Phase 16 Agent Combat Director

---

### 8. **Banner Rallying Points** ✅
**API Support:**
- `Agent._formationBanner` - Banner field (line 95)
- `BattleBannerBearersModel` - Native model exists
- `BannerBearerLogic` - Native logic exists
- `BannerBearerLogic.OnItemPickup()` - Detects banner pickup

**Implementation Path:**
- Detect banner bearer via `Agent._formationBanner != null`
- Low morale agents seek banner bearer within 15m
- Apply morale boost: +15 morale over 5 seconds when near banner
- Banner bearer gets +10% defense

**Files:**
- `TaleWorlds.MountAndBlade\Agent.cs:95`
- `TaleWorlds.MountAndBlade\ComponentInterfaces\BattleBannerBearersModel.cs`
- `TaleWorlds.MountAndBlade\BannerBearerLogic.cs:158`

---

## ⚠️ **PARTIAL SUPPORT** (2 Systems)

### 9. **Terrain Micro-Exploitation** ⚠️
**API Support:**
- `Scene.GetTerrainHeightData()` - Get terrain height
- `WorldPosition.GetNavMeshZ()` - Get navigation mesh height
- ❓ No direct "cover detection" API found
- ❓ No obvious "reverse slope" calculation

**Implementation Path (Limited):**
- Can detect height differences via `GetTerrainHeightData()`
- Can prefer high ground by comparing agent Z vs enemy Z
- **CANNOT** detect cover/concealment directly
- **CANNOT** easily detect reverse slopes

**Recommendation:**
- **SIMPLIFY** to "High Ground Preference" only
- Agents prefer positions where `agent.Z > enemy.Z + 2m`
- Skip cover detection (no API support)
- Skip reverse slope (too complex without native support)

**Files:**
- `TaleWorlds.Engine\Scene.cs:147-178` (terrain data)

---

### 10. **Opportunistic Missile Resupply** ⚠️
**API Support:**
- `MissionEquipment.GetAmmoAmount()` - ✅ Can READ ammo
- `Agent.OnItemPickup()` - ✅ Can pick up items
- `SpawnedItemEntity` - ✅ Dropped weapons exist
- ❓ **NO API** to create ammo bundles at runtime
- ❓ Ammo barrels (`AmmoBarrelBase`) are scene objects, not spawnable

**Implementation Path (Hacky):**
- Detect dropped ranged weapons via `SpawnedItemEntity`
- Agent picks up via `Agent.OnItemPickup(SpawnedItemEntity, slot, out removeWeapon)`
- **PROBLEM:** Can't spawn new ammo, only scavenge existing drops
- **PROBLEM:** May conflict with equipment slots

**Recommendation:**
- **DEFER** this system - API support is insufficient
- Would require spawning `SpawnedItemEntity` at runtime
- No clean way to "transfer" ammo without replacing weapon
- High complexity, low return

**Files:**
- `TaleWorlds.MountAndBlade\Agent.cs:2412` (OnItemPickup)
- `TaleWorlds.MountAndBlade\Objects\Usables\AmmoBarrelBase.cs` (scene-only)

---

## ❌ **NO API SUPPORT** (1 System)

### 11. **Sound-Based Awareness** ❌
**API Support:**
- `SoundPlayer` class exists
- ❌ No spatial audio detection APIs
- ❌ No "GetSoundsAtPosition" method
- ❌ No combat sound event system accessible to mods

**Implementation Path:**
- **NOT FEASIBLE** without native audio query APIs

**Recommendation:**
- **CUT** this system entirely
- Alternative: Use visual proximity instead (already doing this)
- Native doesn't expose sound data to modders

---

## **Final Recommendations**

### ✅ **IMPLEMENT (8 Systems):**
1. Ammunition Tracking & Behavior
2. Line Relief & Rotation
3. Morale Contagion
4. Commander Death Response
5. Breaking Point Detection
6. Feint Maneuvers
7. Pursuit Depth Control
8. Banner Rallying Points

### 📝 **SIMPLIFY (1 System):**
9. **Terrain Micro-Exploitation** → Rename to "High Ground Preference"
   - Only implement height-based positioning
   - Remove cover detection
   - Remove reverse slope

### ❌ **CUT (2 Systems):**
10. **Opportunistic Missile Resupply** - Insufficient API support
11. **Sound-Based Awareness** - No API exists

---

## **Revised Phase 19 Scope**

**Total Systems: 9** (down from 11)

### **Adjusted Development Time:**
- Original: 5-7 hours (11 systems)
- Revised: 4-6 hours (9 systems, 1 simplified)

### **Total Project Time:**
- Original: 50-64 hours
- Revised: 49-63 hours

---

## **API Verification Checklist**

| System | API Verified | Feasible | Notes |
|--------|-------------|----------|-------|
| Ammunition Tracking | ✅ | ✅ | GetAmmoAmount, GetMaxAmmo |
| Line Relief | ✅ | ✅ | FormationRankIndex, existing |
| Morale Contagion | ✅ | ✅ | Morale setter exists |
| Commander Death | ✅ | ✅ | Formation.Captain, event |
| Breaking Point | ✅ | ✅ | Uses existing data |
| Feints | ✅ | ✅ | Formation movement orders |
| Pursuit Control | ✅ | ✅ | Position tracking |
| Banners | ✅ | ✅ | BannerBearerLogic exists |
| Terrain Exploit | ⚠️ | ⚠️ | Simplify to high ground only |
| Missile Resupply | ⚠️ | ❌ | Can't spawn ammo cleanly |
| Sound Awareness | ❌ | ❌ | No spatial audio API |

---

**Verification Method:**
- Direct inspection of decompiled Bannerlord source at `C:\Dev\Enlisted\Decompile`
- Confirmed existence of methods, properties, and events
- Tested for read/write access where applicable
- No assumptions made without API confirmation
