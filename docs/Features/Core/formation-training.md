# Feature Spec: Formation Training System

## Overview
Automatic daily skill XP system based on player's military formation specialization, providing authentic military training progression.

## Purpose
Give players natural skill progression that matches their military role. Infantry soldiers naturally develop melee combat skills, cavalry masters horsemanship, archers perfect ranged combat, creating authentic military career progression.

## Inputs/Outputs

**Inputs:**
- Player's formation type (chosen during T1->T2 proving event)
- JSON configuration from `duties_system.json`
- Daily tick events while actively enlisted

**Outputs:**
- Daily skill XP applied to formation-appropriate skills
- Immersive training descriptions explaining skill development
- Formation display in enlisted status menu

## Behavior

### Daily Training Process
1. System detects player's formation (Infantry, Cavalry, Archer, Horse Archer)
2. Applies configured XP amounts to appropriate skills
3. Uses `Hero.MainHero.AddSkillXp(skill, amount)` API for reliable skill progression
4. Runs only while actively enlisted (training is paused while on leave)

### Formation Skill Mapping
- **Infantry**: Athletics (+5), One-Handed (+5), Two-Handed (+5), Polearm (+5), Throwing (+2)
- **Cavalry**: Riding (+5), One-Handed (+5), Polearm (+5), Athletics (+2), Two-Handed (+2)
- **Horse Archer**: Riding (+5), Bow (+5), Throwing (+5), Athletics (+2), One-Handed (+2)
- **Archer**: Bow (+5), Crossbow (+5), Athletics (+5), One-Handed (+2)

### Formation Assignment
- **T1 (New Recruits)**: Everyone starts as Infantry
- **T1->T2 (Proving Event)**: Formation chosen during "Finding Your Place" event
  - Options: Infantry, Archer, Cavalry, Horse Archer (conditional)
  - Horse Archer only available for Khuzait and Aserai cultures
- **T2+**: Formation locked to choice (cannot change)
- **Persistent**: Formation choice maintained across save/load

## Technical Implementation

**Files:**
- `EnlistedDutiesBehavior.cs` - Core formation training logic and XP application
- `EnlistmentBehavior.cs` - Initial formation (Infantry) for new recruits
- `PromotionBehavior.cs` - Triggers proving event for formation choice
- `LanceLifeEventsStateBehavior.cs` - Applies formation effect from event
- `duties_system.json` - Formation skill XP configuration
- `events_promotion.json` - T1->T2 proving event with formation options

**Key APIs:**
```csharp
// Formation detection (stored from proving event choice)
string GetPlayerFormationType() => _playerFormation?.ToLower() ?? "infantry";

// Apply skill XP using the native game API
Hero.MainHero.AddSkillXp(skill, xpAmount);

// Formation assignment (from proving event effect)
EnlistedDutiesBehavior.Instance?.SetPlayerFormation("cavalry");
```

**Configuration Structure:**
```json
{
  "formation_training": {
    "enabled": true,
    "formations": {
      "infantry": {
        "description": "Foot soldiers - ground combat and conditioning",
        "skills": {
          "Athletics": 5,
          "OneHanded": 5,
          "TwoHanded": 5,
          "Polearm": 5,
          "Throwing": 2
        }
      }
    }
  }
}
```

## Edge Cases

**Configuration Loading Fails:**
- System uses fallback configuration to prevent crashes
- Error logged but formation training disabled safely
- Build process ensures `duties_system.json` copies to game folder

**Formation Not Set:**
- Defaults to "infantry" for safety
- Initial formation set during first enlistment
- Formation persists through save/load cycles

**Unknown Skills in Configuration:**
- Individual skill failures logged but don't crash system
- Other skills continue to receive XP normally
- Uses `MBObjectManager.Instance.GetObject<SkillObject>(skillName)` for resolution

**Leave/Rejoin Scenarios:**
- Training is paused during temporary leave
- Formation is preserved when returning from leave

**Existing Saves (Migration):**
- If player has no formation set, detect from equipped troop or equipment
- `DeriveFormationFromTroop()` checks `IsMounted` and `IsRanged` properties
- `DetectPlayerFormation()` analyzes equipped items as fallback

## Acceptance Criteria

- [x] Daily XP applied to all formation-appropriate skills
- [x] Formation chosen during T1->T2 proving event
- [x] Horse Archer option only for Khuzait/Aserai cultures
- [x] JSON configuration loads successfully
- [x] Formation displays correctly in enlisted menu
- [x] Immersive training descriptions for each formation
- [x] No crashes or assertion errors
- [x] Save/load maintains formation assignment
- [x] Existing saves migrate correctly

## Debugging

**Common Issues:**
- **No XP being applied**: Check if `duties_system.json` exists in game's ModuleData folder
- **Wrong formation detected**: Verify `_playerFormation` field is set during proving event
- **Horse Archer not showing**: Check player's enlisted lord culture (Khuzait/Aserai only)

**Log Categories:**
- "Duties" - Formation training application and XP processing
- "Config" - Configuration loading and validation
- "Promotion" - Proving event and formation assignment

**Configuration Verification:**
- Check `Modules\Enlisted\ModuleData\Enlisted\duties_system.json` exists
- Verify "formation_training.enabled" is true
- Confirm formation names match exactly ("infantry", "cavalry", "archer", "horsearcher")

**Testing:**
- Enlist with any lord, advance to T2
- Complete the "Finding Your Place" proving event
- Verify formation is set and training XP applies daily
- Test Horse Archer availability with Khuzait/Aserai lords
