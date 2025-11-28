# Configuration Files

JSON files controlling the Enlisted mod. Edit and restart campaign to apply changes.

## Files

| File | Purpose |
|------|---------|
| settings.json | Logging levels, encounter settings |
| enlisted_config.json | Tiers, wages, retirement, grace periods |
| duties_system.json | Duty definitions and officer roles |
| progression_config.json | XP thresholds, XP sources |
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
  "lord_relation_bonus": 30
}
```

### XP Sources (progression_config.json)
```json
"xp_sources": {
  "daily_base": 25,
  "battle_participation": 25,
  "xp_per_kill": 1
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
  "Battle": "Debug",
  "Equipment": "Warn"
}
```
Valid levels: Off, Error, Warn, Info, Debug, Trace

### Formation Pricing (equipment_pricing.json)
```json
"formation_multipliers": {
  "infantry": 1.0,
  "archer": 1.3,
  "cavalry": 2.0,
  "horsearcher": 2.5
}
```

## Supported Cultures
empire, aserai, sturgia, vlandia, khuzait, battania, nord, darshi
