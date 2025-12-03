# Feature Spec: Formation Training System

## Overview
Automatic daily skill XP system based on player's military formation specialization, providing authentic military training progression.

## Purpose
Give players natural skill progression that matches their military role. Infantry soldiers naturally develop melee combat skills, cavalry masters horsemanship, archers perfect ranged combat, creating authentic military career progression.

## Inputs/Outputs

**Inputs:**
- Player's formation type (set from chosen troop during promotion or initial enlistment)
- JSON configuration from `duties_system.json`
- Daily tick events while enlisted or on leave

**Outputs:**
- Daily skill XP applied to formation-appropriate skills
- Immersive training descriptions explaining skill development
- Formation display in enlisted status menu

## Behavior

**Daily Training Process:**
1. System detects player's formation (Infantry, Cavalry, Archer, Horse Archer)
2. Applies configured XP amounts to appropriate skills
3. Uses `Hero.MainHero.AddSkillXp(skill, amount)` API for reliable skill progression
4. Continues during temporary leave (training doesn't stop)

**Formation Skill Mapping:**
- **Infantry**: Athletics (+50), One-Handed (+50), Two-Handed (+50), Polearm (+50), Throwing (+25)
- **Cavalry**: Riding (+50), One-Handed (+50), Polearm (+50), Athletics (+25), Two-Handed (+25)
- **Horse Archer**: Riding (+50), Bow (+50), Throwing (+50), Athletics (+25), One-Handed (+25)
- **Archer**: Bow (+50), Crossbow (+50), Athletics (+50), One-Handed (+25)

**Formation Assignment:**
- **New Recruits**: Set to "infantry" based on lord's BasicTroop
- **Promoted Players**: Updated when selecting new troop type
- **Persistent**: Formation choice maintained across save/load

## Technical Implementation

**Files:**
- `EnlistedDutiesBehavior.cs` - Core formation training logic and XP application
- `EnlistmentBehavior.cs` - Initial formation assignment for new recruits
- `TroopSelectionManager.cs` - Formation update during troop selection
- `EnlistedMenuBehavior.cs` - Formation-specific training descriptions
- `duties_system.json` - Formation skill XP configuration

**Key APIs:**
```csharp
// Formation detection (stored from troop choice)
string GetPlayerFormationType() => _playerFormation?.ToLower() ?? "infantry";

// Apply skill XP using the native game API
Hero.MainHero.AddSkillXp(skill, xpAmount);

// Formation assignment (during enlistment/promotion)
EnlistedDutiesBehavior.Instance?.SetPlayerFormation("infantry");
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
          "Athletics": 50,
          "OneHanded": 50,
          "TwoHanded": 50,
          "Polearm": 50,
          "Throwing": 25
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
- Training continues during temporary leave
- Formation preserved when returning from leave
- No interruption in skill progression

## Acceptance Criteria

- ✅ Daily XP applied to all formation-appropriate skills
- ✅ Formation detection based on chosen troop type
- ✅ Training continues during temporary leave
- ✅ JSON configuration loads successfully
- ✅ Formation displays correctly in enlisted menu
- ✅ Immersive training descriptions for each formation
- ✅ No crashes or assertion errors
- ✅ Save/load maintains formation assignment
- ✅ Build process copies configuration files properly

## Debugging

**Common Issues:**
- **No XP being applied**: Check if `duties_system.json` exists in game's ModuleData folder
- **Wrong formation detected**: Verify `_playerFormation` field is set during enlistment
- **Assertion errors**: Remove any special text formatting that Bannerlord can't parse

**Log Categories:**
- "Duties" - Formation training application and XP processing
- "Config" - Configuration loading and validation
- "Enlistment" - Initial formation assignment during enlistment

**Configuration Verification:**
- Check `Modules\Enlisted\ModuleData\Enlisted\duties_system.json` exists
- Verify "formation_training.enabled" is true
- Confirm formation names match exactly ("infantry", "cavalry", "archer", "horsearcher")

**Testing:**
- Enlist with any lord, advance time one day
- Check skills menu for XP progression
- Verify log messages confirm XP application
- Test with different formations by promoting and selecting different troop types
