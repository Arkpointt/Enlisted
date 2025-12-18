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

## Native Skill XP Systems

Bannerlord includes several native systems that award skill XP independently of the mod's Formation Training. These run alongside Formation Training and affect skill progression:

### Track Detection (Footprints)
- **Trigger**: Every 15 minutes if you have an effective scout with Scouting > 0
- **XP Formula**: Base 0.2 × (1 + hours elapsed) × (1 + 0.02 × max(0, 100 - party size))
- **Enemy Multiplier**: Lord (10x), Bandit (4x), Caravan (3x), Other (2x)
- **Typical Range**: 2-30 XP for most tracks, 50-200+ XP for enemy lord tracks
- **Detection Difficulty**: Based on track age, distance, party size, time of day (night is 10x harder)
- **Code**: `MapTracksCampaignBehavior`, `DefaultMapTrackModel.GetSkillFromTrackDetected()`
- **Visual**: Colored arrows on map (no text notification by default)

### Finding Items on Map
- **Trigger**: Daily tick for each party
- **Requirement**: Beast Whisperer perk (Scouting 275)
- **Chance**: 3% per day
- **Terrain**: Plains or Steppe only
- **Reward**: Random mountable animal added to inventory
- **Notification**: "{COUNT} {ANIMAL_NAME} is added to your party"
- **Code**: `FindingItemOnMapBehavior`

### Travel-Based Skill XP
- **Scouting**: Passive XP while traveling, based on terrain type
- **Athletics**: XP when on foot without horse equipped
- **Riding**: XP when mounted or horse equipped
- **Navigation**: XP when at sea (Naval DLC)
- **Frequency**: Hourly tick, movement skills checked every 4 hours
- **Rate**: Based on party movement speed
- **Code**: `MobilePartyTrainingBehavior.WorkSkills()`

### Trade Profit XP
- **Trigger**: When selling items you previously purchased
- **XP Amount**: Equal to profit margin (sell price - buy price)
- **Notification**: Silent (XP bar fills without message)
- **Note**: Not relevant for T1-T3 soldiers (no trade access)
- **Code**: `TradeSkillCampaignBehavior`

### Hideout Spotting
- **XP Reward**: 100 Scouting XP to scout
- **Trigger**: When your party spots a bandit hideout on the map
- **Code**: `SkillLevelingManager.OnHideoutSpotted()`

### Daily Party Training
- **Trigger**: Daily tick for all non-hero troops
- **Calculation**: `PartyTrainingModel.GetEffectiveDailyExperience()`
- **Modified By**: Party training perks
- **Special**: Bow Trainer perk (Bow 225) gives XP to lowest Bow skill hero daily
- **Code**: `MobilePartyTrainingBehavior.OnDailyTickParty()`

### Interaction with Formation Training

**Current Behavior:**
- Native systems run independently and stack with Formation Training
- Both systems award XP simultaneously
- No conflicts or redundancy issues

**Design Notes:**
- Formation Training is intentionally additive (doesn't replace native systems)
- Native travel XP provides baseline passive progression
- Formation Training provides role-specific military training on top
- Track detection rewards active scouting and situational awareness
- Combined effect: Faster, more focused skill progression for enlisted soldiers

**Future Considerations:**
- Could add event notifications for track detection ("You found enemy tracks! +12 Scouting XP")
- Could tie track detection to lance roles/duties (scout duty bonus)
- Could trigger lance life events when finding significant tracks (enemy army)
- Could optionally scale down native travel XP if progression feels too fast

---

## Debugging

**Common Issues:**
- **No XP being applied**: Check if `duties_system.json` exists in game's ModuleData folder
- **Wrong formation detected**: Verify `_playerFormation` field is set during proving event
- **Horse Archer not showing**: Check player's enlisted lord culture (Khuzait/Aserai only)
- **Unexpected XP gains**: Check native systems (tracks, travel, trade) - not all XP is from Formation Training

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
- Monitor for native XP sources (check for tracks on map, travel on different terrains)
