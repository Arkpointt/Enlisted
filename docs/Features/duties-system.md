# Feature Spec: Military Duties System

## Overview
JSON-configured military role system where players can take on duties like Quartermaster, Scout, Field Medic, etc. with real gameplay benefits.

## Purpose
Add variety and specialization to military service. Different duties provide different benefits (skill bonuses, equipment access, special abilities) and make each playthrough feel different.

## Inputs/Outputs

**Inputs:**
- Player's current formation type (Infantry, Archer, Cavalry, Horse Archer)
- Available duty slots (increases with tier: 1 → 2 → 3)
- Officer role status 
- JSON configuration from `duties_system.json`

**Outputs:**
- Active duty assignments with real benefits
- Skill bonuses applied daily/on events
- Equipment access modifications (officers get broader selection)
- Status display in enlisted menu

## Behavior

**Duty Assignment:**
1. Player accesses duties menu from enlisted status
2. Shows available duties for their formation type  
3. Player selects duties (limited by available slots)
4. Benefits applied immediately and tracked

**Daily Processing:**
- Skill bonuses awarded based on active duties
- Officer role benefits calculated (15% equipment discount, etc.)
- Duty performance tracked for future advancement

**Formation-Based Filtering:**
- Infantry: Runner, Quartermaster, Field Medic, Armorer
- Archer: Scout, Marksman, Lookout
- Cavalry: Messenger, Pathfinder, Shock Trooper  
- Horse Archer: Scout, Messenger, Skirmisher

## Technical Implementation

**Files:**
- `EnlistedDutiesBehavior.cs` - Core duty management and benefit application
- `DutyConfiguration.cs` - JSON loading and validation
- `duties_system.json` - Configuration data

**Configuration Structure:**
```json
{
  "availableDuties": [
    {
      "id": "quartermaster",
      "name": "Quartermaster", 
      "formations": ["infantry", "cavalry"],
      "benefits": {
        "skillGain": { "trade": 2, "steward": 1 },
        "equipmentDiscount": 0.15
      }
    }
  ]
}
```

**Benefit Application:**
- Skills: `Hero.MainHero.AddSkillXp(skill, bonusAmount)` 
- Equipment: Discount multiplier in cost calculation
- Officer roles: Integration with existing party role system

## Edge Cases

**Invalid JSON Configuration:**
- Validation on load prevents crashes from bad config
- Default fallback configuration if file corrupted
- Error logging with specific validation failure details

**Formation Type Changes:**
- Update available duties when player's formation changes
- Remove duties that are no longer valid for new formation
- Notify player of duty changes

**Duty Slot Limits:**
- Enforce maximum duties based on tier (1 at low tier, 3 at high tier)
- Handle tier decrease (rare but possible) by removing excess duties
- Priority system for which duties to keep

**Save/Load Compatibility:**
- Active duties persist through save/load correctly
- Benefits recalculated on load to handle config changes
- Graceful handling of missing duty definitions in saves

## Acceptance Criteria

- ✅ JSON configuration loads and validates correctly
- ✅ Duties filtered appropriately by formation type
- ✅ Skill bonuses applied correctly and consistently  
- ✅ Officer roles provide equipment discounts and enhanced access
- ✅ Duty slots enforced based on tier progression
- ✅ Configuration changes work without recompiling mod
- ✅ Save/load maintains duty assignments correctly

## Debugging

**Common Issues:**
- **Duties not showing**: Check formation type detection and JSON filtering
- **Benefits not applying**: Verify daily tick events are firing correctly
- **Config not loading**: Check JSON syntax and file location

**Log Categories:**
- "Duties" - Duty assignment and benefit application
- "ConfigManager" - JSON loading and validation
- Look in `duties_system.json` for configuration structure
