# Configuration Files

JSON files controlling the Enlisted mod. Edit and restart campaign to apply changes.

**Current Mod Version**: 0.5.4  
**Compatible Game Version**: 1.3.9

## Files

| File | Purpose |
|------|---------|
| settings.json | Logging levels, encounter settings, mod behavior flags |
| enlisted_config.json | Tiers, wages, retirement, grace periods, formations, equipment pricing |
| duties_system.json | Duty definitions and officer roles |
| progression_config.json | XP thresholds, XP sources, wage formulas, promotion benefits |
| equipment_pricing.json | Formation/culture cost multipliers |
| equipment_kits.json | Culture-specific loadouts |
| menu_config.json | Menu text and options |

## Key Settings

### Retirement (enlisted_config.json)
```json
"retirement": {
  "first_term_days": 252,
  "renewal_term_days": 84,
  "cooldown_days": 42,
  "first_term_gold": 10000,
  "first_term_reenlist_bonus": 20000,
  "renewal_discharge_gold": 5000,
  "renewal_continue_bonus": 5000,
  "lord_relation_bonus": 30,
  "faction_reputation_bonus": 30,
  "other_lords_relation_bonus": 15,
  "other_lords_min_relation": 50
}
```

### Tier Requirements (enlisted_config.json)
```json
"enlistment": {
  "tier_requirements": [0, 800, 3000, 6000, 11000, 19000],
  "tier_names": [
    "Levy",
    "Footman",
    "Serjeant",
    "Man-at-Arms",
    "Banner Serjeant",
    "Household Guard"
  ],
  "daily_base_xp": 25,
  "assignment_wage_multipliers": {
    "grunt_work": 0.8,
    "guard_duty": 0.9,
    "cook": 0.9,
    "foraging": 1.0,
    "surgeon": 1.3,
    "engineer": 1.4,
    "quartermaster": 1.2,
    "scout": 1.1,
    "sergeant": 1.5,
    "strategist": 1.6
  }
}
```

### Finance Settings (enlisted_config.json)
```json
"finance": {
  "show_in_clan_tooltip": true,
  "tooltip_label": "{=enlisted_wage_income}Enlistment Wages",
  "wage_formula": {
    "base_wage": 10,
    "level_multiplier": 1,
    "tier_multiplier": 5,
    "xp_divisor": 200,
    "army_bonus_multiplier": 1.2
  }
}
```

### XP Sources (progression_config.json)
```json
"xp_sources": {
  "daily_base": 25,
  "battle_participation": 25,
  "xp_per_kill": 2
}
```

### Wage Formula (progression_config.json)
```json
"wage_system": {
  "base_formula": {
    "daily_base": 10,
    "tier_bonus_per_level": 5,
    "hero_level_multiplier": 1,
    "xp_bonus_divisor": 200,
    "maximum_base_wage": 150
  },
  "assignment_multipliers": {
    "sergeant": 1.5,
    "strategist": 1.6
  },
  "army_bonuses": {
    "in_army": 1.2
  }
}
```

### Formation Selection (progression_config.json)
```json
"formation_selection": {
  "trigger_tier": 2,
  "allow_multiple_changes": true,
  "change_cooldown_days": 7,
  "free_changes": 1
}
```

### Grace Period (enlisted_config.json)
```json
"gameplay": {
  "desertion_grace_period_days": 14,
  "leave_max_days": 14
}
```

### Log Levels (settings.json)
```json
"LogLevels": {
  "Default": "Info",
  "Battle": "Info",
  "Siege": "Info",
  "Combat": "Info",
  "Equipment": "Info",
  "Gold": "Info",
  "XP": "Info",
  "Menu": "Warn",
  "Encounter": "Warn",
  "Promotion": "Info",
  "Duties": "Info",
  "TroopSelection": "Info",
  "KillTracker": "Info",
  "Enlistment": "Info",
  "Patch": "Warn",
  "Interface": "Warn",
  "Bootstrap": "Info",
  "Session": "Info",
  "Config": "Info"
}
```
Valid levels: Off, Error, Warn, Info, Debug, Trace

### Encounter Settings (settings.json)
```json
"Encounter": {
  "AttachWhenClose": true,
  "AttachRange": 0.6,
  "TrailDistance": 1.2,
  "SuppressPlayerEncounter": true
}
```

### Formation Pricing (equipment_pricing.json)
```json
"equipment_pricing": {
  "base_cost_per_tier": 75,
  "formation_multipliers": {
    "infantry": 1.0,
    "archer": 1.3,
    "cavalry": 2.0,
    "horsearcher": 2.5
  },
  "culture_modifiers": {
    "empire": 1.0,
    "aserai": 0.9,
    "khuzait": 0.8,
    "vlandia": 1.2,
    "sturgia": 0.9,
    "battania": 0.8,
    "nord": 0.9,
    "darshi": 0.9
  },
  "elite_multiplier": 1.5,
  "personal_equipment_restore_cost": 0
}
```

### Formation Display Names (enlisted_config.json)
Customize how formations are displayed per culture:
```json
"formations": {
  "infantry": {
    "display_names": {
      "empire": "Legionary",
      "aserai": "Footman",
      "khuzait": "Spearman",
      "vlandia": "Man-at-Arms",
      "sturgia": "Warrior",
      "battania": "Clansman",
      "nord": "Huskarl",
      "darshi": "Desert Warrior"
    }
  }
}
```

## Supported Cultures
empire, aserai, sturgia, vlandia, khuzait, battania, nord, darshi

## Enlisted settlement behavior
- When enlisted and your lord/army reaches a town or castle, the enlisted menu remains active and now shows a Visit option. It only hides if you are already inside a native town/castle menu or an actual battle/siege is active.
- Settlement encounters (peaceful entry) are allowed; only live battles or sieges block the enlisted menu.

## Notes

- Changes to config files require restarting your campaign to take effect
- Always back up your saves before modifying configuration files
- The `schemaVersion` field should not be modified manually
- Invalid JSON syntax will cause the mod to fail loading - validate your JSON before saving
