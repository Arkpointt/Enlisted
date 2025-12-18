# Lance Life JSON Schemas

This document defines the JSON schemas for all Lance Life data files. These schemas are the source of truth for data structure validation.

---

## Table of Contents

1. [Schema Version](#schema-version)
2. [Event Schema](#event-schema)
3. [Story Pack Schema](#story-pack-schema)
4. [Escalation Track Schema](#escalation-track-schema)
5. [Lance Schema](#lance-schema)
6. [Promotion Schema](#promotion-schema)
7. [Condition Schema](#condition-schema)
8. [Common Types](#common-types)
9. [File Organization](#file-organization)

---

## Schema Version

All data files must include a schema version for migration support:

```json
{
  "schemaVersion": 1,
  "...": "..."
}
```

> **Note:** Schema version uses numeric type (not string) for consistency across all data files.

---

## Event Schema

Events are the core content unit. All story content uses this schema.

### Full Event Definition

```json
{
  "id": "string (required, unique)",
  "category": "duty | training | general | onboarding | escalation | promotion",
  
  "metadata": {
    "tier_range": {
      "min": 1,
      "max": 6
    },
    "content_doc": "string (source document reference)"
  },
  
  "delivery": {
    "method": "automatic | player_initiated",
    "menu": "string | null",
    "menu_section": "training | tasks | social | null"
  },
  
  "triggers": {
    "all": ["string (conditions that must ALL be true)"],
    "any": ["string (conditions where ANY can be true)"],
    "time_of_day": ["dawn | morning | afternoon | evening | dusk | night | late_night"],
    "escalation_requirements": {
      "heat": { "min": 0, "max": 10 },
      "discipline": { "min": 0, "max": 10 },
      "lance_reputation": { "min": -50, "max": 50 },
      "medical_risk": { "min": 0, "max": 5 }
    }
  },
  
  "requirements": {
    "duty": "string | null",
    "formation": "infantry | cavalry | archer | naval | any",
    "tier": { "min": 1, "max": 6 },
    "min_fatigue_capacity": 0,
    "no_conditions": ["injury_severe", "illness_severe"],
    "flags_required": ["string"],
    "flags_forbidden": ["string"]
  },
  
  "timing": {
    "cooldown_days": 0,
    "priority": "normal | high | critical",
    "one_time": false,
    "rate_limit": {
      "max_per_week": 0,
      "category_cooldown_days": 0
    }
  },
  
  "content": {
    "title": "string (localization key or raw text)",
    "setup": "string (localization key or raw text, supports placeholders)",
    "options": [
      {
        "id": "string (unique within event)",
        "text": "string (localization key or raw text)",
        "tooltip": "string | null",
        "condition": "string | null (condition for option to appear)",
        "risk": "safe | risky | corrupt",
        "risk_chance": 50,
        
        "costs": {
          "fatigue": 0,
          "gold": 0,
          "time_hours": 0
        },
        
        "rewards": {
          "xp": {
            "skill_name": 0
          },
          "gold": 0,
          "relation": {
            "target": 0
          },
          "items": ["item_id"]
        },
        
        "effects": {
          "heat": 0,
          "discipline": 0,
          "lance_reputation": 0,
          "medical_risk": 0,
          "fatigue_relief": 0,
          
          "formation": "infantry | archer | cavalry | horsearcher (Phase 7)",
          "starter_duty": "string | null (Phase 7 - auto-assign duty)",
          "promotes": false,
          "character_tag": "string | null (Phase 7 - narrative tag)",
          "loyalty_tag": "string | null (Phase 7 - loyalty faction)"
        },
        
        "flags_set": ["string"],
        "flags_clear": ["string"],
        
        "outcome": "string (localization key or raw text)",
        "outcome_failure": "string | null (for risky options)",
        
        "injury_risk": {
          "chance": 0,
          "severity": "minor | moderate | severe",
          "type": "wound | strain | illness"
        },
        
        "triggers_event": "string | null (chain to another event)",
        "advances_onboarding": false
      }
    ]
  },
  
  "variants": {
    "variant_key": {
      "setup": "string (override setup for this variant)",
      "options": []
    }
  }
}
```

### Minimal Event (Automatic, Simple)

```json
{
  "id": "gen_dawn_muster",
  "category": "general",
  
  "delivery": {
    "method": "automatic",
    "menu": null
  },
  
  "triggers": {
    "all": ["is_enlisted", "ai_safe"],
    "time_of_day": ["dawn"]
  },
  
  "timing": {
    "cooldown_days": 3,
    "priority": "normal"
  },
  
  "content": {
    "title": "Morning Muster",
    "setup": "{LANCE_LEADER_SHORT}'s voice shatters the dawn quiet. \"On your feet, {LANCE_NAME}! Muster in five!\"",
    "options": [
      {
        "id": "fall_in_first",
        "text": "Fall in first, look sharp",
        "risk": "safe",
        "costs": { "fatigue": 1 },
        "rewards": { "xp": { "leadership": 15 } },
        "effects": { "lance_reputation": 2 },
        "outcome": "You're first in line. {LANCE_LEADER_SHORT} gives you an approving nod."
      },
      {
        "id": "fall_in_normal",
        "text": "Fall in with the rest",
        "risk": "safe",
        "outcome": "You join the formation without incident."
      }
    ]
  }
}
```

### Player-Initiated Event (Training)

```json
{
  "id": "inf_train_shield_wall",
  "category": "training",
  
  "metadata": {
    "tier_range": { "min": 1, "max": 6 }
  },
  
  "delivery": {
    "method": "player_initiated",
    "menu": "enlisted_activities",
    "menu_section": "training"
  },
  
  "triggers": {
    "time_of_day": ["morning", "afternoon"]
  },
  
  "requirements": {
    "formation": "infantry",
    "min_fatigue_capacity": 2,
    "no_conditions": ["injury_moderate", "injury_severe"]
  },
  
  "timing": {
    "cooldown_days": 2,
    "priority": "normal"
  },
  
  "content": {
    "title": "Shield Wall Drill",
    "setup": "{SERGEANT_NAME} forms up {LANCE_NAME}. \"Shield wall! Tight formation!\"",
    "options": [
      {
        "id": "standard",
        "text": "Hold the line, focus on form",
        "risk": "safe",
        "costs": { "fatigue": 2 },
        "rewards": { "xp": { "polearm": 25, "one_handed": 20 } },
        "outcome": "Solid drill. Your shield work is improving."
      },
      {
        "id": "push_front",
        "text": "Push to the front, take more hits",
        "risk": "risky",
        "risk_chance": 70,
        "costs": { "fatigue": 3 },
        "rewards": { "xp": { "polearm": 35, "one_handed": 25, "athletics": 20 } },
        "outcome": "You take the brunt of the practice blows. It hurts, but you learn fast.",
        "outcome_failure": "A practice sword catches you wrong. You'll feel that tomorrow.",
        "injury_risk": {
          "chance": 10,
          "severity": "minor",
          "type": "wound"
        }
      }
    ]
  }
}
```

### Escalation Threshold Event

```json
{
  "id": "heat_warning",
  "category": "escalation",
  
  "metadata": {
    "track": "heat",
    "threshold": 3,
    "threshold_name": "watched",
    "threshold_direction": "positive"
  },
  
  "delivery": {
    "method": "automatic",
    "menu": null
  },
  
  "triggers": {
    "all": ["is_enlisted", "ai_safe"],
    "escalation_requirements": {
      "heat": { "min": 3 }
    }
  },
  
  "timing": {
    "cooldown_days": 7,
    "priority": "high"
  },
  
  "content": {
    "title": "The Warning",
    "setup": "{LANCE_LEADER_SHORT} pulls you aside after evening meal...",
    "options": [
      {
        "id": "deny",
        "text": "\"I don't know what you're talking about.\"",
        "risk": "safe",
        "outcome": "{LANCE_LEADER_SHORT} stares at you. \"Right. You don't.\" They walk away."
      },
      {
        "id": "acknowledge",
        "text": "\"I hear you. I'll be more careful.\"",
        "risk": "safe",
        "effects": { "heat": -1 },
        "outcome": "A curt nod. \"See that you are.\""
      }
    ]
  }
}
```

### Onboarding Event with Variants

```json
{
  "id": "enlisted_onboard_01_meet_lance",
  "category": "onboarding",
  
  "metadata": {
    "track": "enlisted",
    "stage": 1,
    "tier_range": { "min": 1, "max": 4 }
  },
  
  "delivery": {
    "method": "automatic",
    "menu": null
  },
  
  "triggers": {
    "all": ["is_enlisted", "ai_safe", "onboarding_stage_1"],
    "escalation_requirements": {
      "days_since_enlistment": { "max": 1 }
    }
  },
  
  "timing": {
    "cooldown_days": 0,
    "priority": "high",
    "one_time": true
  },
  
  "content": {
    "title": "Meeting the Lance",
    "setup": "DEFAULT - overridden by variant",
    "options": [
      {
        "id": "respectful",
        "text": "\"Reporting for duty.\"",
        "risk": "safe",
        "effects": { "lance_reputation": 3 },
        "outcome": "Professional. They appreciate that.",
        "advances_onboarding": true
      }
    ]
  },
  
  "variants": {
    "first_time": {
      "setup": "A scarred veteran blocks your path. \"Fresh meat?\" They look you up and down. \"I'm {LANCE_LEADER_RANK} {LANCE_LEADER_NAME}. This is {LANCE_NAME}. Try not to die.\""
    },
    "transfer": {
      "setup": "{LANCE_LEADER_SHORT} looks at your transfer papers. \"Served under {PREVIOUS_LORD}, eh? Different army, same job. Fall in.\""
    },
    "return": {
      "setup": "{LANCE_LEADER_SHORT} raises an eyebrow. \"Back again? Thought you'd had enough.\" A hint of a smile. \"Well, you know the drill.\""
    }
  }
}
```

---

## Story Pack Schema

Story packs are collections of related events.

```json
{
  "schemaVersion": 1,
  "packId": "string (unique identifier)",
  "packName": "string (display name)",
  "description": "string",
  "category": "duty | training | general | onboarding | escalation | promotion",
  "tier_range": {
    "min": 1,
    "max": 6
  },
  "enabled": true,
  "events": [
    { "... event schema ...": "..." }
  ]
}
```

### Example: Duty Pack

```json
{
  "schemaVersion": 1,
  "packId": "duty_quartermaster",
  "packName": "Quartermaster Duty Events",
  "description": "Events for soldiers assigned quartermaster duty",
  "category": "duty",
  "tier_range": { "min": 1, "max": 6 },
  "enabled": true,
  "events": [
    { "id": "qm_supply_inventory", "...": "..." },
    { "id": "qm_merchant_negotiation", "...": "..." },
    { "id": "qm_spoiled_supplies", "...": "..." },
    { "id": "qm_requisition_request", "...": "..." },
    { "id": "qm_accounting_discrepancy", "...": "..." }
  ]
}
```

---

## Escalation Track Schema

Defines an escalation track and its thresholds.

```json
{
  "schemaVersion": 1,
  "trackId": "string (heat | discipline | lance_reputation | medical_risk)",
  "trackName": "string (display name)",
  "description": "string",
  
  "range": {
    "min": 0,
    "max": 10
  },
  
  "default_value": 0,
  
  "decay": {
    "enabled": true,
    "rate": -1,
    "interval_days": 7,
    "requires_no_infractions": true,
    "decay_toward": 0
  },
  
  "thresholds": [
    {
      "value": 3,
      "name": "watched",
      "direction": "positive | negative",
      "event_id": "heat_warning",
      "description": "People are starting to notice"
    },
    {
      "value": 5,
      "name": "shakedown",
      "direction": "positive",
      "event_id": "heat_shakedown",
      "description": "Formal inspection triggered"
    }
  ],
  
  "ui": {
    "display_name": "Heat",
    "icon": "heat_icon",
    "color_low": "#00FF00",
    "color_high": "#FF0000",
    "show_in_status": true
  }
}
```

### Example: Heat Track

```json
{
  "schemaVersion": 1,
  "trackId": "heat",
  "trackName": "Corruption Attention",
  "description": "How much attention your corrupt activities have drawn",
  
  "range": { "min": 0, "max": 10 },
  "default_value": 0,
  
  "decay": {
    "enabled": true,
    "rate": -1,
    "interval_days": 7,
    "requires_no_infractions": true,
    "decay_toward": 0
  },
  
  "thresholds": [
    { "value": 3, "name": "watched", "direction": "positive", "event_id": "heat_warning" },
    { "value": 5, "name": "shakedown", "direction": "positive", "event_id": "heat_shakedown" },
    { "value": 7, "name": "audit", "direction": "positive", "event_id": "heat_audit" },
    { "value": 10, "name": "exposed", "direction": "positive", "event_id": "heat_exposed" }
  ],
  
  "ui": {
    "display_name": "Heat",
    "show_in_status": true
  }
}
```

### Example: Lance Reputation Track

```json
{
  "schemaVersion": 1,
  "trackId": "lance_reputation",
  "trackName": "Lance Reputation",
  "description": "How your lance mates view you",
  
  "range": { "min": -50, "max": 50 },
  "default_value": 0,
  
  "decay": {
    "enabled": true,
    "rate": 1,
    "interval_days": 14,
    "decay_toward": 0
  },
  
  "thresholds": [
    { "value": 20, "name": "trusted", "direction": "positive", "event_id": "lance_trusted" },
    { "value": 40, "name": "bonded", "direction": "positive", "event_id": "lance_bonded" },
    { "value": -20, "name": "disliked", "direction": "negative", "event_id": "lance_isolated" },
    { "value": -40, "name": "hostile", "direction": "negative", "event_id": "lance_sabotage" }
  ],
  
  "ui": {
    "display_name": "Lance Standing",
    "show_in_status": true
  }
}
```

---

## Lance Schema

Defines a lance (squad) structure.

```json
{
  "schemaVersion": 1,
  
  "lance": {
    "id": "string (generated)",
    "name": "string",
    "lord_id": "string (reference to lord)",
    "formation": "infantry | cavalry | archer | naval",
    "battles_survived": 0,
    "fallen_count": 0,
    
    "slots": [
      {
        "position": 1,
        "role": "leader",
        "npc_id": "string | null",
        "is_player": false
      },
      {
        "position": 2,
        "role": "second",
        "npc_id": "string | null",
        "is_player": false
      },
      {
        "position": 3,
        "role": "veteran",
        "npc_id": "string | null",
        "is_player": false
      },
      {
        "position": 4,
        "role": "veteran",
        "npc_id": "string | null",
        "is_player": false
      },
      {
        "position": 5,
        "role": "soldier",
        "npc_id": "string | null",
        "is_player": false
      },
      {
        "position": 6,
        "role": "soldier",
        "npc_id": "string | null",
        "is_player": false
      },
      {
        "position": 7,
        "role": "soldier",
        "npc_id": "string | null",
        "is_player": false
      },
      {
        "position": 8,
        "role": "soldier",
        "npc_id": "string | null",
        "is_player": false
      },
      {
        "position": 9,
        "role": "recruit",
        "npc_id": "string | null",
        "is_player": false
      },
      {
        "position": 10,
        "role": "recruit | soldier",
        "npc_id": "string | null",
        "is_player": true
      }
    ]
  }
}
```

### Lance NPC Schema

```json
{
  "schemaVersion": 1,
  
  "npc": {
    "id": "string (generated)",
    "name_first": "string",
    "name_short": "string",
    "rank": "string",
    "role": "leader | second | veteran | soldier | recruit",
    "trait": "string (personality descriptor)",
    
    "state": {
      "is_alive": true,
      "is_wounded": false,
      "wound_recovery_days": 0
    },
    
    "relationship_with_player": 0
  }
}
```

---

## Promotion Schema

Defines promotion requirements, proving events, and career-shaping choices.

### Promotion Definition

```json
{
  "schemaVersion": "1.0",
  "promotionId": "string",
  
  "from_tier": 1,
  "to_tier": 2,
  
  "eligibility": {
    "xp_required": 800,
    "min_days_in_rank": 7,
    "min_events_completed": 3,
    "min_battles_survived": 1,
    "min_lance_reputation": 0,
    "max_discipline": 7,
    "min_lance_leader_relation": 0,
    "min_lord_relation": 0,
    "required_flags": [],
    "forbidden_flags": []
  },
  
  "proving_event": {
    "event_id": "promotion_t1_t2_finding_your_place",
    "can_fail": false,
    "retry_days_on_fail": 7,
    "failure_followup_event": "string | null"
  },
  
  "rewards": {
    "relation_lord": 0,
    "relation_lance_leader": 5,
    "relation_lance": 5,
    "gold_bonus": 0,
    "unlocks": ["formation_selection", "advanced_duties"]
  },
  
  "ceremony": {
    "title_key": "promo_title_2",
    "message_key": "promo_msg_2",
    "chat_key": "promo_chat_2"
  }
}
```

### Full Promotion Config (All Tiers)

```json
{
  "schemaVersion": 1,
  "promotions": [
    {
      "promotionId": "promotion_t1_t2",
      "from_tier": 1,
      "to_tier": 2,
      "eligibility": {
        "xp_required": 800,
        "min_days_in_rank": 7,
        "min_events_completed": 3,
        "min_battles_survived": 1,
        "min_lance_reputation": 0,
        "max_discipline": 7,
        "min_lance_leader_relation": 0
      },
      "proving_event": {
        "event_id": "promotion_t1_t2_finding_your_place",
        "can_fail": false
      },
      "rewards": {
        "relation_lance_leader": 5,
        "unlocks": ["formation_selection"]
      }
    },
    {
      "promotionId": "promotion_t2_t3",
      "from_tier": 2,
      "to_tier": 3,
      "eligibility": {
        "xp_required": 3000,
        "min_days_in_rank": 14,
        "min_events_completed": 5,
        "min_battles_survived": 3,
        "min_lance_reputation": 10,
        "max_discipline": 7,
        "min_lance_leader_relation": 10
      },
      "proving_event": {
        "event_id": "promotion_t2_t3_sergeants_test",
        "can_fail": true,
        "retry_days_on_fail": 7,
        "failure_followup_event": "promotion_t2_t3_learning_from_failure"
      },
      "rewards": {
        "relation_lance_leader": 5,
        "unlocks": ["advanced_duties"]
      }
    },
    {
      "promotionId": "promotion_t3_t4",
      "from_tier": 3,
      "to_tier": 4,
      "eligibility": {
        "xp_required": 6000,
        "min_days_in_rank": 21,
        "min_events_completed": 8,
        "min_battles_survived": 5,
        "min_lance_reputation": 20,
        "max_discipline": 5,
        "min_lance_leader_relation": 20
      },
      "proving_event": {
        "event_id": "promotion_t3_t4_crisis_reveals",
        "can_fail": true,
        "retry_days_on_fail": 14,
        "failure_followup_event": "promotion_t3_t4_passed_over"
      },
      "rewards": {
        "relation_lord": 5,
        "unlocks": []
      },
      "sets_character_tag": true
    },
    {
      "promotionId": "promotion_t4_t5",
      "from_tier": 4,
      "to_tier": 5,
      "eligibility": {
        "xp_required": 11000,
        "min_days_in_rank": 30,
        "min_events_completed": 12,
        "min_battles_survived": 8,
        "min_lance_reputation": 30,
        "max_discipline": 5,
        "min_lance_leader_relation": 30
      },
      "proving_event": {
        "event_id": "promotion_t4_t5_those_who_follow",
        "can_fail": true,
        "retry_days_on_fail": 30,
        "failure_followup_event": "promotion_t4_t5_under_rival"
      },
      "rewards": {
        "relation_lance": 10,
        "unlocks": ["lance_leader_role"]
      },
      "creates_rival_on_fail": true
    },
    {
      "promotionId": "promotion_t5_t6",
      "from_tier": 5,
      "to_tier": 6,
      "eligibility": {
        "xp_required": 19000,
        "min_days_in_rank": 45,
        "min_events_completed": 15,
        "min_battles_survived": 12,
        "min_lance_reputation": 40,
        "max_discipline": 3,
        "min_lord_relation": 10
      },
      "proving_event": {
        "event_id": "promotion_t5_t6_lords_eye",
        "can_fail": true,
        "retry_days_on_fail": 30,
        "failure_followup_event": "promotion_t5_t6_prove_your_worth"
      },
      "rewards": {
        "relation_lord": 15,
        "unlocks": ["household_guard_role", "command_staff_access"]
      },
      "sets_loyalty_tag": true
    }
  ]
}
```

### Character Tags

Set during T3->T4 promotion event based on player choice:

```json
{
  "character_tags": [
    {
      "id": "steady",
      "name": "Steady",
      "description": "Calm under pressure, methodical approach",
      "set_by_event": "promotion_t3_t4_crisis_reveals",
      "set_by_option": "take_command_calm",
      "affects_events": true,
      "event_modifier": "Offers calm/methodical options in leadership events"
    },
    {
      "id": "fierce",
      "name": "Fierce",
      "description": "Intense, aggressive, leads by energy",
      "set_by_event": "promotion_t3_t4_crisis_reveals",
      "set_by_option": "take_command_fierce",
      "affects_events": true,
      "event_modifier": "Offers bold/aggressive options in leadership events"
    },
    {
      "id": "supportive",
      "name": "Supportive",
      "description": "Quiet competence, empowers others",
      "set_by_event": "promotion_t3_t4_crisis_reveals",
      "set_by_option": "support_others",
      "affects_events": true,
      "event_modifier": "Offers team-focused options in leadership events"
    }
  ]
}
```

### Loyalty Tags

Set during T5->T6 promotion event based on player choice:

```json
{
  "loyalty_tags": [
    {
      "id": "absolute",
      "name": "Absolute Loyalty",
      "description": "Will obey without question",
      "set_by_event": "promotion_t5_t6_lords_eye",
      "set_by_option": "absolute_loyalty",
      "affects_events": true,
      "event_modifier": "Lord trusts you with morally gray orders"
    },
    {
      "id": "counselor",
      "name": "Trusted Counselor",
      "description": "Voices concerns, then obeys",
      "set_by_event": "promotion_t5_t6_lords_eye",
      "set_by_option": "voice_then_obey",
      "affects_events": true,
      "event_modifier": "Lord asks your opinion before decisions"
    },
    {
      "id": "principled",
      "name": "Principled Soldier",
      "description": "Has moral limits",
      "set_by_event": "promotion_t5_t6_lords_eye",
      "set_by_option": "moral_limits",
      "affects_events": true,
      "event_modifier": "Lord respects you but may exclude you from certain orders"
    }
  ]
}
```

### Command Style Tags (T7->T8)

Set during T7->T8 promotion event based on player choice:

```json
{
  "command_style_tags": [
    {
      "id": "dutiful",
      "name": "Dutiful Commander",
      "description": "Accepts losses as necessary cost of war",
      "set_by_event": "promotion_t7_t8_test_of_command",
      "set_by_option": "yes_duty",
      "affects_events": true,
      "event_modifier": "Neutral soldier loyalty, lord appreciates pragmatism"
    },
    {
      "id": "reluctant",
      "name": "Reluctant Commander",
      "description": "Hates sending soldiers to die but does it anyway",
      "set_by_event": "promotion_t7_t8_test_of_command",
      "set_by_option": "yes_but_hate",
      "affects_events": true,
      "event_modifier": "+5 soldier loyalty, events reflect internal conflict"
    },
    {
      "id": "protective",
      "name": "Protective Commander",
      "description": "Never wastes lives, spends them only when necessary",
      "set_by_event": "promotion_t7_t8_test_of_command",
      "set_by_option": "protect_my_soldiers",
      "affects_events": true,
      "event_modifier": "+10 soldier loyalty, may conflict with lord orders"
    }
  ]
}
```

### Strategic Style Tags (T8->T9)

Set during T8->T9 promotion event based on player choice:

```json
{
  "strategic_style_tags": [
    {
      "id": "bold",
      "name": "Bold Strategist",
      "description": "Favors aggressive, high-risk strategies",
      "set_by_event": "promotion_t8_t9_council_of_war",
      "set_by_option": "strategic_bold",
      "affects_events": true,
      "event_modifier": "Higher risk/reward in command events, may clash with cautious lords"
    },
    {
      "id": "cautious",
      "name": "Cautious Strategist",
      "description": "Favors defensive, low-risk strategies",
      "set_by_event": "promotion_t8_t9_council_of_war",
      "set_by_option": "strategic_cautious",
      "affects_events": true,
      "event_modifier": "Lower casualties, slower campaign progress, favored by risk-averse lords"
    }
  ]
}
```

### Rival Tracking

Created on T4->T5 failure:

```json
{
  "rival": {
    "exists": false,
    "npc_id": "string | null",
    "name": "string | null",
    "relationship": 0,
    "is_lance_leader": false,
    "created_at_event": "string | null",
    "resolution_state": "active | reconciled | defeated | transferred"
  }
}
```

### Promotion State (Runtime)

```json
{
  "promotion_state": {
    "current_tier": 1,
    "days_in_current_tier": 0,
    "events_completed_this_term": 0,
    "battles_survived_this_term": 0,
    
    "eligibility_status": {
      "next_tier": 2,
      "is_eligible": false,
      "blocking_reasons": ["min_battles_survived: 0/2", "min_events_completed: 2/5"],
      "met_requirements": ["xp_required", "min_days_in_rank"]
    },
    
    "retry_cooldowns": {
      "promotion_t2_t3": "CampaignTime | null",
      "promotion_t3_t4": "CampaignTime | null",
      "promotion_t4_t5": "CampaignTime | null",
      "promotion_t5_t6": "CampaignTime | null",
      "promotion_t6_t7": "CampaignTime | null",
      "promotion_t7_t8": "CampaignTime | null",
      "promotion_t8_t9": "CampaignTime | null"
    },
    
    "character_tag": "steady | fierce | supportive | null",
    "loyalty_tag": "absolute | counselor | principled | null",
    "command_style_tag": "dutiful | reluctant | protective | null",
    "strategic_style_tag": "bold | cautious | null",
    
    "formation": "infantry | archer | cavalry | horse_archer | null",
    "formation_chosen_at": "CampaignTime | null",
    
    "declined_commission": false
  }
}
```

### Commander Tracking (T7+ Only)

```json
{
  "commander_stats": {
    "battles_commanded": 0,
    "soldiers_recruited_total": 0,
    "soldiers_lost_total": 0,
    "soldiers_wounded_total": 0,
    "soldier_survival_rate": 100,
    "retinue_average_veterancy": 0,
    "campaign_victories": 0,
    "campaign_defeats": 0
  }
}
```

---

### Event Option Effects (Promotion-Specific)

Extended option effects for promotion events:

```json
{
  "effects": {
    "heat": 0,
    "discipline": 0,
    "lance_reputation": 0,
    "medical_risk": 0,
    "fatigue_relief": 0,
    
    "formation": "infantry | archer | cavalry | horse_archer | null",
    "character_tag": "steady | fierce | supportive | null",
    "loyalty_tag": "absolute | counselor | principled | null",
    
    "promotes": true,
    "promotion_fails": false,
    "retry_modifier_days": 0,
    
    "creates_rival": false,
    "rival_relation": 0
  }
}
```

### Trigger Conditions (Promotion-Specific)

Added trigger conditions for promotion events:

| Condition | Description |
|-----------|-------------|
| `promotion_eligible_t2` | Meets all T1->T2 requirements |
| `promotion_eligible_t3` | Meets all T2->T3 requirements |
| `promotion_eligible_t4` | Meets all T3->T4 requirements |
| `promotion_eligible_t5` | Meets all T4->T5 requirements |
| `promotion_eligible_t6` | Meets all T5->T6 requirements |
| `promotion_retry_ready:{tier}` | Retry cooldown has expired |
| `has_character_tag:{tag}` | Player has specific character tag |
| `has_loyalty_tag:{tag}` | Player has specific loyalty tag |
| `has_rival` | Player has an active rival |
| `rival_is_lance_leader` | Rival is currently lance leader |
| `faction_has_horse_archers` | Faction is Aserai or Khuzait |

---

## Condition Schema

Defines player conditions (injuries, illnesses).

```json
{
  "schemaVersion": "1.0",
  
  "condition": {
    "id": "string",
    "type": "injury | illness | exhaustion",
    "name": "string",
    "description": "string",
    
    "severity": "minor | moderate | severe",
    
    "effects": {
      "health_drain_per_day": 0,
      "fatigue_pool_modifier": 1.0,
      "xp_modifier": 1.0,
      "blocks_training": false,
      "blocks_combat": false
    },
    
    "recovery": {
      "base_days": 5,
      "treated_days": 3,
      "can_worsen": false,
      "worsens_to": "string | null"
    },
    
    "ui": {
      "icon": "string",
      "color": "#FF0000"
    }
  }
}
```

---

## Camp Status Schema

Defines the camp state that affects event availability and triggers.

```json
{
  "schemaVersion": 1,
  
  "camp_status": {
    "supplies": {
      "current_level": "abundant | adequate | low | critical",
      "days_until_critical": 0,
      "logistics_strain_percent": 0
    },
    
    "morale": {
      "current_level": "high | steady | low | breaking",
      "value": 0,
      "recent_events": ["victory", "defeat", "long_march", "paid", "unpaid"]
    },
    
    "pay": {
      "status": "current | delayed | overdue | crisis",
      "days_since_pay": 0,
      "pending_amount": 0,
      "tension_level": 0
    },
    
    "wounded": {
      "count": 0,
      "severe_count": 0,
      "lance_mates_wounded": []
    },
    
    "camp_state": {
      "is_established": false,
      "days_in_camp": 0,
      "in_settlement": false,
      "settlement_type": "town | castle | village | none",
      "days_from_settlement": 0,
      "is_besieging": false,
      "is_besieged": false
    },
    
    "weather": {
      "current": "clear | rain | snow | storm",
      "temperature": "hot | warm | mild | cold | freezing"
    },
    
    "army": {
      "is_in_army": false,
      "army_size": 0,
      "army_morale": 0,
      "lord_is_leading": false
    }
  }
}
```

### Camp Status Trigger Conditions

These conditions can be used in event triggers:

| Condition | Description |
|-----------|-------------|
| `supplies_abundant` | Supplies at abundant level |
| `supplies_adequate` | Supplies at adequate level |
| `supplies_low` | Supplies at low level |
| `supplies_critical` | Supplies at critical level |
| `logistics_strain_min:{n}` | Logistics strain >= n% |
| `logistics_strain_max:{n}` | Logistics strain <= n% |
| `morale_high` | Army/camp morale high |
| `morale_steady` | Army/camp morale steady |
| `morale_low` | Army/camp morale low |
| `morale_breaking` | Army/camp morale breaking |
| `pay_current` | Pay is current |
| `pay_delayed` | Pay is delayed |
| `pay_overdue` | Pay is overdue |
| `pay_crisis` | Pay crisis (mutiny risk) |
| `wounded_in_camp` | Wounded soldiers present |
| `lance_mate_wounded` | A lance mate is wounded |
| `camp_established` | Army has made camp |
| `in_settlement` | In a settlement |
| `not_in_settlement` | In the field |
| `days_from_settlement_min:{n}` | Days from settlement >= n |
| `is_besieging` | Army is besieging |
| `is_besieged` | Army is under siege |
| `weather_cold` | Cold or freezing weather |
| `weather_hot` | Hot weather |
| `weather_storm` | Storm conditions |

---

## Camp Menu Schema

Defines the structure of camp activity menus.

```json
{
  "schemaVersion": 1,
  
  "menu": {
    "id": "enlisted_activities",
    "title": "Camp Activities",
    "description": "Training, tasks, and social activities",
    
    "header": {
      "show_time_of_day": true,
      "show_fatigue": true,
      "show_condition_warning": true
    },
    
    "sections": [
      {
        "id": "training",
        "title": "Training ({FORMATION})",
        "description": "Formation-specific drills and practice",
        "icon": "training_icon",
        "sort_order": 1,
        
        "visibility": {
          "requires": ["is_enlisted"],
          "time_of_day": ["morning", "afternoon"],
          "hide_if": ["injury_severe", "exhaustion_severe"]
        },
        
        "events_filter": {
          "category": "training",
          "menu_section": "training"
        }
      },
      {
        "id": "tasks",
        "title": "Camp Tasks",
        "description": "Contribute to camp operations",
        "icon": "tasks_icon",
        "sort_order": 2,
        
        "visibility": {
          "requires": ["is_enlisted", "camp_established"]
        },
        
        "events_filter": {
          "category": "training",
          "menu_section": "tasks"
        }
      },
      {
        "id": "social",
        "title": "Social",
        "description": "Spend time with your lance",
        "icon": "social_icon",
        "sort_order": 3,
        
        "visibility": {
          "requires": ["is_enlisted"],
          "time_of_day": ["evening", "dusk", "night"]
        },
        
        "events_filter": {
          "category": "training",
          "menu_section": "social"
        }
      },
      {
        "id": "rest",
        "title": "Rest",
        "description": "Skip activities, recover fatigue",
        "icon": "rest_icon",
        "sort_order": 4,
        
        "visibility": {
          "requires": ["is_enlisted"],
          "show_if_fatigue_above": 0
        },
        
        "action": {
          "type": "rest",
          "fatigue_relief": 2,
          "message": "You rest instead of training."
        }
      }
    ]
  }
}
```

### Camp Menu Option Schema

Individual menu options generated from events:

```json
{
  "option": {
    "id": "string (from event id)",
    "text": "string (event title + cost/reward summary)",
    "tooltip": "string (full description)",
    "icon": "string | null",
    
    "display": {
      "show_fatigue_cost": true,
      "show_xp_reward": true,
      "show_cooldown": true
    },
    
    "state": {
      "enabled": true,
      "disabled_reason": "string | null",
      "cooldown_remaining_days": 0,
      "blocked_by_condition": "string | null",
      "blocked_by_fatigue": false,
      "blocked_by_time": false
    }
  }
}
```

---

## Retinue Schema

Defines the player's personal soldiers at Tier 7+ (Commander tier).

**Note:** Retinue is a T7-T9 (Commander) feature only. T1-6 players follow orders and do not command personal troops. Companions do not count toward retinue cap.

```json
{
  "schemaVersion": 1,
  
  "retinue": {
    "enabled": false,
    "unlocked_at_tier": 7,
    
    "capacity": {
      "tier_7": 15,
      "tier_8": 25,
      "tier_9": 35
    },
    
    "companions_count_toward_cap": false,
    
    "current": {
      "count": 0,
      "max": 5,
      "soldiers": [
        {
          "id": "string (generated)",
          "troop_id": "string (native troop type)",
          "name": "string",
          "tier": 0,
          "is_wounded": false,
          "wound_recovery_days": 0,
          "battles_survived": 0,
          "kills": 0
        }
      ]
    },
    
    "recruitment": {
      "sources": ["battlefield_pickup", "camp_assignment", "promotion"],
      "costs": {
        "gold_per_soldier": 0,
        "requires_vacancy": true
      }
    },
    
    "maintenance": {
      "wage_per_soldier_per_day": 0,
      "paid_from": "player_gold | muster_ledger"
    },
    
    "battle": {
      "spawn_with_player": true,
      "assigned_to_player_formation": true,
      "can_die_permanently": true,
      "wound_chance_on_death": 50
    }
  }
}
```

### Retinue Soldier Schema

```json
{
  "soldier": {
    "id": "string",
    "troop_id": "string (references native troop type)",
    "custom_name": "string | null",
    "generated_name": "string",
    
    "stats": {
      "tier": 0,
      "battles_survived": 0,
      "kills": 0,
      "days_served": 0
    },
    
    "state": {
      "is_alive": true,
      "is_wounded": false,
      "wound_severity": "none | minor | moderate | severe",
      "wound_recovery_days": 0,
      "is_available": true
    },
    
    "equipment": {
      "is_customized": false,
      "custom_equipment": []
    },
    
    "history": {
      "recruited_date": "CampaignTime",
      "recruited_source": "battlefield_pickup | camp_assignment | promotion",
      "notable_battles": []
    }
  }
}
```

### Retinue Events

Events related to retinue management (T7-9 only):

| Event ID | Trigger | Description |
|----------|---------|-------------|
| `retinue_recruit_battlefield` | After battle, player tier 7+ | Offer to take on a surviving soldier |
| `retinue_soldier_wounded` | After battle | One of your soldiers was wounded |
| `retinue_soldier_died` | After battle | One of your soldiers fell |
| `retinue_soldier_promoted` | Soldier hits kills threshold | Soldier earned recognition |
| `retinue_capacity_increased` | Player promoted to T8/T9 | You can now command more soldiers |

---

## Camp Follower Schema (Optional)

Defines camp followers the player can interact with.

```json
{
  "schemaVersion": 1,
  
  "camp_follower": {
    "id": "string",
    "type": "merchant | healer | smith | entertainer | fence",
    "name": "string",
    
    "availability": {
      "in_settlement_only": false,
      "requires_army_size": 0,
      "chance_present": 100
    },
    
    "services": [
      {
        "id": "string",
        "name": "string",
        "description": "string",
        "cost_gold": 0,
        "cost_heat": 0,
        "effect": {
          "type": "buy_item | sell_item | heal | repair | reduce_heat | information",
          "value": 0
        }
      }
    ],
    
    "relationship": {
      "track_relationship": false,
      "can_provide_quests": false
    }
  }
}
```

### Standard Camp Followers

```json
{
  "camp_followers": [
    {
      "id": "camp_merchant",
      "type": "merchant",
      "name": "Traveling Merchant",
      "availability": { "in_settlement_only": false, "chance_present": 80 },
      "services": [
        { "id": "buy_supplies", "name": "Buy Supplies", "cost_gold": 20 },
        { "id": "buy_medicine", "name": "Buy Herbal Remedy", "cost_gold": 50 }
      ]
    },
    {
      "id": "camp_healer",
      "type": "healer",
      "name": "Camp Healer",
      "availability": { "in_settlement_only": false, "requires_army_size": 50 },
      "services": [
        { "id": "treat_wound", "name": "Treat Wound", "cost_gold": 30, "effect": { "type": "heal", "value": 50 } }
      ]
    },
    {
      "id": "camp_fence",
      "type": "fence",
      "name": "Shady Dealer",
      "availability": { "in_settlement_only": false, "chance_present": 40 },
      "services": [
        { "id": "sell_loot", "name": "Sell 'Found' Goods", "cost_heat": 1 },
        { "id": "buy_contraband", "name": "Buy Contraband", "cost_gold": 100, "cost_heat": 1 }
      ]
    }
  ]
}
```

---

## Common Types

### Formation Types

```json
{
  "formations": [
    {
      "id": "infantry",
      "name": "Infantry",
      "description": "Shield wall, front line fighters",
      "available_all_cultures": true
    },
    {
      "id": "archer",
      "name": "Archer",
      "description": "Ranged fighters with bow or crossbow",
      "available_all_cultures": true
    },
    {
      "id": "cavalry",
      "name": "Cavalry",
      "description": "Mounted fighters",
      "available_all_cultures": true
    },
    {
      "id": "horse_archer",
      "name": "Horse Archer",
      "description": "Mounted ranged fighters",
      "available_all_cultures": false,
      "cultures": ["aserai", "khuzait"]
    },
    {
      "id": "naval",
      "name": "Naval",
      "description": "Ship-based fighters (War Sails only)",
      "available_all_cultures": false,
      "requires_expansion": "war_sails"
    }
  ]
}
```

### Culture Formation Availability

| Culture | Infantry | Archer | Cavalry | Horse Archer |
|---------|----------|--------|---------|--------------|
| `vlandia` | [x] | [x] | [x] | X |
| `sturgia` | [x] | [x] | [x] | X |
| `empire` | [x] | [x] | [x] | X |
| `battania` | [x] | [x] | [x] | X |
| `aserai` | [x] | [x] | [x] | [x] |
| `khuzait` | [x] | [x] | [x] | [x] |

---

### Trigger Conditions

Valid trigger condition strings:

| Condition | Description |
|-----------|-------------|
| `is_enlisted` | Player is currently enlisted |
| `ai_safe` | Not in battle, encounter, or prisoner |
| `has_duty:{duty_id}` | Player has specific duty assigned |
| `in_settlement` | In a settlement |
| `not_in_settlement` | In the field |
| `before_battle` | Battle imminent (within 24h) |
| `after_battle` | Within 3 days of battle |
| `at_sea` | On a ship |
| `camp_established` | Army has camped |
| `wounded_in_camp` | Wounded soldiers in camp |
| `enemy_nearby` | Enemies in proximity |
| `has_flag:{flag_id}` | Player has specific flag set |
| `onboarding_stage_{1,2,3}` | Onboarding at specific stage |
| `onboarding_complete` | Onboarding finished |
| `tier_min:{n}` | Player tier >= n |
| `tier_max:{n}` | Player tier <= n |
| `days_since_enlistment_min:{n}` | Days since enlistment >= n |
| `days_since_enlistment_max:{n}` | Days since enlistment <= n |
| `lance_leader_relation_min:{n}` | Lance leader relation >= n |
| `promotion_eligible_t{n}` | Meets all requirements for tier n |
| `promotion_retry_ready:{n}` | Retry cooldown expired for tier n |
| `has_character_tag:{tag}` | Player has character tag (steady/fierce/supportive) |
| `has_loyalty_tag:{tag}` | Player has loyalty tag (absolute/counselor/principled) |
| `has_rival` | Player has an active rival |
| `rival_is_lance_leader` | Rival is currently lance leader |
| `faction_has_horse_archers` | Faction is Aserai or Khuzait |
| `faction_is:{culture}` | Player's faction matches culture |
| `supplies_low` | Camp supplies at low level |
| `supplies_critical` | Camp supplies at critical level |
| `morale_low` | Camp/army morale low |
| `pay_overdue` | Pay is overdue |
| `logistics_strain_min:{n}` | Logistics strain >= n% |
| `lance_mate_wounded` | A lance mate is wounded |

### Time of Day Values

| Value | Hours |
|-------|-------|
| `dawn` | 5:00 - 7:00 |
| `morning` | 7:00 - 12:00 |
| `afternoon` | 12:00 - 17:00 |
| `evening` | 17:00 - 20:00 |
| `dusk` | 20:00 - 21:00 |
| `night` | 21:00 - 1:00 |
| `late_night` | 1:00 - 5:00 |

### Skill Names

Valid skill names for XP rewards:

```
one_handed, two_handed, polearm, bow, crossbow, throwing,
riding, athletics, smithing, scouting, tactics, roguery,
charm, leadership, trade, steward, medicine, engineering,
mariner, shipmaster
```

### Placeholders

Standard placeholders for text:

| Placeholder | Description |
|-------------|-------------|
| `{PLAYER_NAME}` | Player's name |
| `{PLAYER_RANK}` | Player's rank title |
| `{LORD_NAME}` | Enlisted lord's name |
| `{LORD_TITLE}` | Enlisted lord's title |
| `{FACTION_NAME}` | Faction name |
| `{LANCE_NAME}` | Lance name |
| `{LANCE_LEADER_NAME}` | Lance leader's full name |
| `{LANCE_LEADER_SHORT}` | Lance leader's short name |
| `{LANCE_LEADER_RANK}` | Lance leader's rank |
| `{SECOND_NAME}` | Second-in-command name |
| `{SECOND_RANK}` | Second-in-command rank |
| `{VETERAN_1_NAME}` | First veteran's name |
| `{SOLDIER_NAME}` | Generic soldier name (context) |
| `{RECRUIT_NAME}` | Generic recruit name (context) |
| `{LANCE_MATE_NAME}` | Lance mate name (context) |
| `{PREVIOUS_LORD}` | Previous lord (for transfers) |
| `{FORMATION}` | Player's formation name |
| `{CURRENT_DUTY}` | Current duty name |

---

## File Organization

### Recommended Directory Structure

```
ModuleData/Enlisted/
├── enlisted_config.json
├── duties_system.json
├── Activities/
│   └── activities.json
├── Conditions/
│   └── condition_defs.json
├── LancePersonas/
│   └── name_pools.json
├── StoryPacks/
│   └── LanceLife/
│       ├── training.json
│       ├── logistics.json
│       ├── morale.json
│       ├── corruption.json
│       ├── medical.json
│       └── escalation_thresholds.json
└── Events/
    ├── schema_version.json
    ├── events_duty_quartermaster.json
    ├── events_duty_scout.json
    ├── events_duty_field_medic.json
    ├── events_duty_messenger.json
    ├── events_duty_armorer.json
    ├── events_duty_runner.json
    ├── events_duty_lookout.json
    ├── events_duty_engineer.json
    ├── events_duty_boatswain.json
    ├── events_duty_navigator.json
    ├── events_training.json
    ├── events_general.json
    ├── events_onboarding.json
    └── events_escalation_thresholds.json

ModuleData/Languages/
└── enlisted_strings.xml
```

### schema_version.json

```json
{
  "schemaVersion": 1,
  "notes": "Schema marker for Lance Life Events packs under ModuleData/Enlisted/Events/."
}
```

### enlisted_config.json (relevant excerpt)

```json
{
  "lance_life_events": {
    "enabled": true,
    "events_folder": "Events",
    "automatic": { "enabled": true },
    "player_initiated": { "enabled": true },
    "onboarding": { "enabled": true, "skip_for_veterans": true, "stage_count": 3 },
    "incident_channel": { "enabled": true }
  },
  "player_conditions": { "enabled": true, "exhaustion_enabled": true }
  }
}
```

---

## Validation Rules

### Event Validation

1. `id` must be unique across all loaded events
2. `category` must be one of: `duty`, `training`, `general`, `onboarding`, `escalation`, `promotion`
3. `options` array must have 2-4 items
4. Each option must have unique `id` within the event
5. `risk_chance` must be 0-100 if specified
6. All referenced `event_id` in `triggers_event` must exist
7. Tier ranges must satisfy `min <= max`

### Escalation Track Validation

1. `trackId` must be unique
2. `range.min < range.max`
3. `default_value` must be within range
4. Threshold values must be within range
5. Each threshold `event_id` must reference an existing event

### Localization Validation

1. All `_key` suffixed strings should have corresponding entries in `enlisted_strings.xml`
2. Missing localization logs a warning but doesn't fail load (uses raw key as fallback)

---

*Schema Version: 1*
*For use with: Lance Life Event System, Enlisted Mod*
