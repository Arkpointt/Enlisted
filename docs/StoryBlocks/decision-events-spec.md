# decision events spec

crusader kings-style decision system for a living, breathing company. the player has **always-available decisions** they can initiate, plus **invitations and events** that come to them from the lord, lance leader, and the situation.

---

## index

- [overview](#overview)
- [the living company](#the-living-company)

## overview

Decision Events come in **two forms**, just like Crusader Kings:

### 1. Player-Initiated Decisions (Always Available)

Like CK3's **Decisions tab** — the player can always access these from the Main Menu when conditions are met.

| Aspect | Description |
|--------|-------------|
| **Delivery** | `player_initiated` via menu |
| **Access** | Main Menu → Decisions section |
| **Availability** | Shows when requirements met (tier, gold, cooldown) |
| **Examples** | Throw a party, organize a hunt, challenge someone to spar |

### 2. Pushed Decisions (Invitations & Events)

Like CK3's **event popups** — the Lord invites you to hunt, a lance mate asks for help, something happens.

| Aspect | Description |
|--------|-------------|
| **Delivery** | `automatic` via inquiry popup |
| **Access** | Pushed to player when triggers fire |
| **Availability** | Based on game state, time, relationships |
| **Examples** | Lord's hunting invitation, lance mate's request, camp fever |

---

## The Living Company

The lance is a living, breathing unit. Events and decisions reflect this:

| Source | Player-Initiated | Pushed Events |
|--------|------------------|---------------|
| **The Lord** | Request audience, petition for favor | Lord invites you to hunt, rewards your service |
| **Lance Leader** | Report for duty, request transfer | Sends you on assignment, offers advice |
| **Lance Mates** | Offer to train together, share rations | Ask for loan, invite to dice game, need cover |
| **Self** | Practice skills, maintain equipment | Get sick, have nightmare, receive letter |
| **Situation** | — | Equipment breaks, opportunity arises |

### Placeholder Variables

| Variable | Description |
|----------|-------------|
| `{LORD_NAME}` | The enlisted lord's name |
| `{LANCE_LEADER}` | Lance leader persona name and rank |
| `{LANCE_MATE}` | Random lance mate name |
| `{SERGEANT}` | Sergeant/officer persona |
| `{PLAYER_RANK}` | Player's current rank title |

---

## Delivery Methods

### Player-Initiated (Menu-Based)

```json
{
  "id": "decision_throw_party",
  "category": "decision",
  "delivery": {
    "method": "player_initiated",
    "channel": "menu",
    "menu": "enlisted_main_menu",
    "menu_section": "decisions"
  }
}
```

The player sees these in the Main Menu's Decisions section. They're always there (when conditions met), not random.

### Pushed (Automatic Popup)

```json
{
  "id": "decision_lord_hunt_invitation",
  "category": "decision",
  "delivery": {
    "method": "automatic",
    "channel": "inquiry"
  }
}
```

These pop up when the Lord/AI decides to present them. The player doesn't go looking for them.

---

## Event Structure

Decision Events use the existing Lance Life Events schema with these characteristics:

### Example: Lance Leader Sends You Hunting

```json
{
  "id": "decision_hunting_party",
  "category": "decision",
  "delivery": {
    "method": "automatic",
    "channel": "inquiry"
  },
  "triggers": {
    "all": ["is_enlisted"],
    "any": ["Day", "Dawn"]
  },
  "timing": {
    "cooldown_days": 7,
    "priority": 50
  },
  "content": {
    "title": "The Hunt",
    "setup": "{LANCE_LEADER} stops you after muster. 'Taking a hunting party out. Bring back game, you'll get a share of the bounty. You in?'",
    "options": [
      {
        "id": "join",
        "text": "Aye, I'll go.",
        "risk": "risky",
        "risk_chance": 0.15,
        "costs": { "fatigue": 3 },
        "rewards": { 
          "gold": { "min": 10, "max": 25 },
          "xp": { "Scouting": 25, "Athletics": 10 }
        },
        "injury": { "chance": 0.10, "types": ["sprain", "cut"] },
        "outcome": "A good hunt. The quartermaster pays {GOLD_EARNED} denars for your share of the game.",
        "outcome_failure": "The hunt goes poorly. You twist your ankle on rough ground.",
        "chains_to": "decision_hunting_return"
      },
      {
        "id": "train_instead",
        "text": "No thanks, I'll train.",
        "risk": "safe",
        "costs": { "fatigue": 2 },
        "rewards": { "xp": { "OneHanded": 20 } },
        "outcome": "You spend the morning at the training posts while others hunt."
      },
      {
        "id": "rest",
        "text": "I need rest today.",
        "risk": "safe",
        "rewards": { "fatigueRelief": 2 },
        "outcome": "You take it easy. Sometimes that's the wisest choice."
      }
    ]
  }
}
```

### Example: Lord Invites You to Hunt

```json
{
  "id": "decision_lord_hunt",
  "category": "decision",
  "triggers": {
    "all": ["is_enlisted", "Day"],
    "any": []
  },
  "requirements": {
    "tier_min": 4,
    "lord_relation_min": 5
  },
  "timing": {
    "cooldown_days": 21,
    "priority": 80
  },
  "content": {
    "title": "A Lord's Invitation",
    "setup": "A messenger finds you. '{LORD_NAME} is riding out to hunt and asks for {PLAYER_RANK} to attend.' This is an honor — and an opportunity.",
    "options": [
      {
        "id": "accept",
        "text": "I'd be honored.",
        "costs": { "fatigue": 2 },
        "rewards": { 
          "xp": { "Riding": 20, "Bow": 15 },
          "relation": 3
        },
        "outcome": "You ride alongside {LORD_NAME}. The lord seems to enjoy your company.",
        "chains_to": "decision_hunt_with_lord_chat"
      },
      {
        "id": "decline_politely",
        "text": "I'm honored, but I have duties.",
        "rewards": {},
        "effects": { "relation": -1 },
        "outcome": "The messenger nods and leaves. You hope the lord understands."
      }
    ]
  }
}
```

### Key Fields for Decisions

| Field | Purpose |
|-------|---------|
| `category: "decision"` | Identifies as a Decision Event for pacing/limits |
| `chains_to` | Optional follow-up event ID (for multi-part stories) |
| `rewards.gold` | Direct gold payment (fixed or min/max range) |
| `rewards.xp` | Skill XP by skill name |
| `rewards.relation` | Change to lord relation |
| `effects.lance_reputation` | Change to lance reputation |
| `loot.pool` | (Optional) Reference to a loot table for item rewards |

---

## Event Chains (CK3 Style)

A single decision can spawn 5-10 follow-up events based on outcomes. This creates narrative depth without front-loading complexity.

### Example: The Hunting Party Chain

```
[Decision: Lance Leader Sends You Hunting]
    ↓
Player chooses "I'll go"
    ↓
Roll for success (risk_chance: 0.15 failure)
    │
    ├─ SUCCESS → [Follow-up: Hunting Return]
    │     ↓
    │     "The hunt went well. You've earned 15 denars."
    │     "A lance mate asks to borrow some coin..."
    │     ├─ Help them out (gold cost, +Lance Rep)
    │     ├─ Decline politely
    │     └─ Spend it on drinks for the lads (+Charm, +Lance Rep)
    │
    └─ FAILURE → [Follow-up: Hunting Mishap]
          ↓
          "You've twisted your ankle. Miles from camp."
          ├─ Push through (worsens injury)
          ├─ Ask for help (lance mate helps, +Lance Rep)
          └─ Rest and limp back (uses more fatigue, safe)
```

### Example: Lord Hunt Chain

```
[Decision: Lord Invites You to Hunt]
    ↓
Player chooses "I'd be honored"
    ↓
[Follow-up: Hunt with Lord Conversation]
    ↓
"As you ride, {LORD_NAME} asks about the recent battle..."
    ├─ Share your observations (+Tactics XP, +Relation)
    ├─ Praise the lord's strategy (+Charm XP, +Relation)
    └─ Keep quiet and listen (safe, small +Tactics XP)
```

### Chain Event Schema

```json
{
  "id": "decision_hunting_return",
  "category": "decision_chain",
  "delivery": {
    "method": "automatic",
    "channel": "inquiry",
    "trigger_source": "chain"
  },
  "timing": {
    "delay_hours": 4,
    "one_time": true
  },
  "content": {
    "title": "Back from the Hunt",
    "setup": "The hunt went well. You've earned {GOLD_EARNED} denars for your share. As you pocket the coin, {LANCE_MATE} approaches with a sheepish look. 'Could I borrow a few denars? I'm short until payday.'",
    "options": [
      {
        "id": "help_out",
        "text": "Here, take five.",
        "costs": { "gold": 5 },
        "effects": { "lance_reputation": 3 },
        "flags_set": ["lance_mate_owes_you"],
        "outcome": "{LANCE_MATE} grins. 'I'll pay you back, I swear it.'"
      },
      {
        "id": "decline",
        "text": "Sorry, I need it myself.",
        "outcome": "They nod and walk off. No hard feelings — everyone's short these days."
      },
      {
        "id": "buy_drinks",
        "text": "Tell you what, drinks are on me tonight.",
        "costs": { "gold": 10 },
        "rewards": { "xp": { "Charm": 15 } },
        "effects": { "lance_reputation": 5 },
        "outcome": "That night at the fire circle, the lads toast your generosity."
      }
    ]
  }
}
```

---

## Reward Types

Decision Events can award any combination of rewards. Loot is just one option — most events will award gold, XP, or reputation.

### All Reward Types

| Reward | Field | Description | Example |
|--------|-------|-------------|---------|
| **Gold** | `gold` | Direct payment | Hunting bounty, gambling winnings, lord's favor |
| **Skill XP** | `xp` | Skill experience | Training, learning from veterans, practical experience |
| **Lord Relation** | `relation` | Relation with enlisted lord | Pleasing the lord, personal missions |
| **Lance Reputation** | `lance_reputation` (effect) | Standing with lance mates | Helping them, covering for them, sharing |
| **Fatigue Relief** | `fatigueRelief` | Energy restored | Rest, relaxation, good meal |
| **Items** | `loot` | Equipment, supplies, trade goods | Battlefield salvage, merchant deals |
| **Flags** | `flags_set` | Story state for future events | Triggers follow-up events |

### Reward Examples by Event Type

**Hunting Party (sent by Lance Leader):**
```json
"rewards": {
  "gold": 15,
  "xp": { "Scouting": 20, "Athletics": 10 }
}
```

**Hunt with the Lord (Lord's invitation):**
```json
"rewards": {
  "xp": { "Riding": 25, "Bow": 15 },
  "relation": 3
}
```

**Help a Lance Mate:**
```json
"rewards": {
  "xp": { "Charm": 15 }
},
"effects": {
  "lance_reputation": 5
}
```

**Dice Game Winnings:**
```json
"rewards": {
  "gold": { "min": 30, "max": 80 }
}
```

**Veteran's Teaching:**
```json
"rewards": {
  "xp": { "Tactics": 30, "Leadership": 15 },
  "fatigueRelief": 1
}
```

**Battlefield Salvage (risky):**
```json
"rewards": {
  "loot": {
    "pool": "battlefield_salvage",
    "tier_filter": true,
    "chance": 0.70
  }
},
"effects": {
  "heat": 2
}
```

---

## Loot System (Optional Reward Type)

Most Decision Events won't involve loot — they'll award gold, XP, or reputation. But when loot is appropriate (battlefield salvage, merchant deals, captured equipment), the loot system provides tier and formation-appropriate items.

### Loot Pools

Loot pools define what items can drop and are filtered by the player's tier and formation.

**Location:** `ModuleData/Enlisted/Loot/loot_pools.json`

```json
{
  "schemaVersion": 1,
  "pools": [
    {
      "id": "hunting_rewards",
      "description": "Rewards from a successful hunt",
      "items": [
        { "type": "food", "id": "deer_meat", "weight": 50 },
        { "type": "food", "id": "rabbit_meat", "weight": 30 },
        { "type": "trade", "id": "deer_hide", "weight": 15 },
        { "type": "trade", "id": "antlers", "weight": 5 }
      ]
    },
    {
      "id": "battlefield_salvage",
      "description": "Equipment found after battle",
      "tier_filter": true,
      "formation_filter": true,
      "items": [
        { "type": "weapon", "category": "sword", "weight": 30 },
        { "type": "weapon", "category": "axe", "weight": 20 },
        { "type": "weapon", "category": "bow", "formation": ["archer", "horsearcher"], "weight": 25 },
        { "type": "weapon", "category": "polearm", "formation": ["cavalry"], "weight": 25 },
        { "type": "armor", "category": "body", "weight": 15 },
        { "type": "armor", "category": "head", "weight": 10 },
        { "type": "horse", "formation": ["cavalry", "horsearcher"], "weight": 5 }
      ]
    },
    {
      "id": "dice_game_winnings",
      "description": "What you might win gambling",
      "items": [
        { "type": "gold", "min": 20, "max": 100, "weight": 60 },
        { "type": "weapon", "category": "dagger", "weight": 20 },
        { "type": "trade", "id": "silver_ring", "weight": 15 },
        { "type": "trade", "id": "gold_ring", "weight": 5 }
      ]
    }
  ]
}
```

### Tier Filtering

When `tier_filter: true`, the loot system selects items appropriate to the player's tier:

| Tier | Item Value Range | Quality Weights |
|------|------------------|-----------------|
| T1-T2 | 50-500 denars | Mostly Poor/Normal |
| T3-T4 | 200-2000 denars | Normal/Fine mix |
| T5-T6 | 500-5000 denars | Fine/Masterwork mix |
| T7-T9 | 2000-15000 denars | Masterwork/Lordly |

### Formation Filtering

When `formation_filter: true`, weapon/armor drops match the player's formation:

| Formation | Preferred Weapons | Preferred Armor |
|-----------|-------------------|-----------------|
| Infantry | Swords, Axes, Shields, Polearms | Heavy body, helmets |
| Cavalry | Lances, Swords, Polearms | Medium body, horse gear |
| Archer | Bows, Crossbows, Daggers | Light body, bracers |
| Horse Archer | Bows, Swords | Light/Medium, horse gear |

---

## Item Quality Modifiers

Bannerlord items can have quality modifiers that affect stats and value. Decision Events can award items with specific quality or roll randomly.

### Quality Tiers

| Quality | Effect | Value Modifier | Drop Weight by Tier |
|---------|--------|----------------|---------------------|
| **Rusty** | -15% damage, -20% speed | 0.3x | T1-T2: 30%, T3+: 5% |
| **Chipped** | -10% damage | 0.5x | T1-T2: 25%, T3-T4: 15% |
| **Bent** | -10% speed, -5% damage | 0.4x | T1-T2: 20%, T3: 10% |
| **Crude** | -5% all stats | 0.6x | T1-T3: 20% |
| **Normal** | Base stats | 1.0x | All tiers: baseline |
| **Balanced** | +5% speed | 1.3x | T3+: 15% |
| **Sharpened** | +10% damage | 1.5x | T3+: 10%, T5+: 20% |
| **Fine** | +10% all stats | 2.0x | T4+: 10%, T6+: 25% |
| **Masterwork** | +15% all stats | 3.0x | T5+: 5%, T7+: 15% |
| **Lordly** | +20% all stats | 5.0x | T7+: 5% |

### Armor Quality

| Quality | Effect | Value Modifier |
|---------|--------|----------------|
| **Tattered** | -20% armor | 0.3x |
| **Worn** | -10% armor | 0.5x |
| **Battered** | -15% armor, -10% weight | 0.4x |
| **Normal** | Base stats | 1.0x |
| **Reinforced** | +10% armor, +5% weight | 1.5x |
| **Thick** | +15% armor, +10% weight | 1.8x |
| **Hardened** | +15% armor | 2.0x |
| **Lordly** | +20% armor, -5% weight | 4.0x |

### Horse Quality

| Quality | Effect | Value Modifier |
|---------|--------|----------------|
| **Lame** | -30% speed, -20% maneuver | 0.2x |
| **Swaybacked** | -15% speed, -10% charge | 0.4x |
| **Stubborn** | -10% maneuver | 0.6x |
| **Normal** | Base stats | 1.0x |
| **Spirited** | +10% speed | 1.5x |
| **Heavy** | +15% charge, +10% HP | 1.8x |
| **Champion** | +15% all stats | 3.0x |

---

## Loot in Event Options

### Direct Item Award

```json
{
  "id": "take_sword",
  "text": "Take the sword.",
  "rewards": {
    "item": {
      "id": "vlandian_sword",
      "quality": "sharpened",
      "count": 1
    }
  }
}
```

### Loot Pool Roll

```json
{
  "id": "search_bodies",
  "text": "Search the fallen.",
  "risk": "risky",
  "rewards": {
    "loot": {
      "pool": "battlefield_salvage",
      "rolls": 2,
      "chance_per_roll": 0.50
    }
  },
  "effects": { "heat": 2 }
}
```

### Tiered/Formation-Aware

```json
{
  "id": "claim_share",
  "text": "Claim your share of the spoils.",
  "rewards": {
    "loot": {
      "pool": "battlefield_salvage",
      "tier_filter": true,
      "formation_filter": true,
      "quality_bias": "normal_or_better",
      "rolls": 1,
      "chance_per_roll": 0.70
    }
  }
}
```

---

## Decision Events Catalog

### Player-Initiated Decisions (Always Available in Menu)

These are always visible in the Main Menu's Decisions section when requirements are met. The player initiates them.

| Event ID | Requirements | Description | Rewards |
|----------|--------------|-------------|---------|
| `decision_throw_party` | Has 50+ gold, morale not already high | Organize a party for the lance | Gold cost, +Lance Rep, +Morale |
| `decision_organize_hunt` | Day, not in town, tier 2+ | Get permission to lead a hunting party | +Gold (share), +Scouting XP |
| `decision_challenge_spar` | Day, not injured | Challenge a lance mate to spar | +Weapon XP, injury risk |
| `decision_petition_lord` | Tier 4+, lord relation > 0 | Request audience with the lord | Variable outcomes |
| `decision_share_rations` | Has rations, lance mate hungry | Give food to a struggling comrade | +Lance Rep |
| `decision_volunteer_duty` | Not on duty | Volunteer for extra duty | +Discipline, +fatigue |
| `decision_practice_solo` | Any time | Train alone | +Weapon XP, fatigue |
| `decision_write_home` | In town, has paper | Write a letter home | +Charm XP |

### Pushed by the Lord (Invitations)

The Lord notices you and extends an opportunity. These pop up based on your tier, relation, and situation.

| Event ID | Trigger | Description | Rewards |
|----------|---------|-------------|---------|
| `decision_lord_hunt_invitation` | Day, tier 4+, lord relation > 5 | Lord invites you to ride with him | +Riding/Bow XP, +relation |
| `decision_lord_errand` | In town, tier 3+, relation > 0 | Lord asks you to run an errand | +Gold, +relation |
| `decision_lord_counsel` | Dusk, tier 5+, relation > 10 | Lord asks your opinion on a matter | +Leadership XP, +relation |
| `decision_lord_reward` | After victory, distinguished self | Lord rewards your service publicly | +Gold or item, +relation |
| `decision_lord_personal_guard` | Battle, tier 5+, relation > 15 | Lord wants you by his side | +Combat XP, high risk, +relation |

### Pushed by Lance Leader (Assignments)

The Lance Leader gives orders, offers opportunities, or shares advice.

| Event ID | Trigger | Description | Rewards |
|----------|---------|-------------|---------|
| `decision_sent_hunting` | Day/Dawn, not in town | Sent out with hunting party | +Gold (bounty), +Scouting XP |
| `decision_foraging_detail` | Day, logistics strained | Assigned to find supplies | +Steward XP, helps camp |
| `decision_escort_duty` | Near town, tier 3+ | Escort supplies or person | +Gold, +Athletics XP |
| `decision_lance_leader_advice` | Random, tier 2-4 | Career advice, training tip | +XP in chosen skill |
| `decision_cover_shift` | Night, lance mate sick | Asked to cover someone's watch | +Discipline relief, +fatigue |

### Pushed by Lance Mates (Requests)

Your comrades ask for help, invite you to join them, or share something.

| Event ID | Trigger | Description | Rewards |
|----------|---------|-------------|---------|
| `decision_dice_game_invite` | Dusk/Night, pay_tension > 30 | Invited to the dice game | Win/lose gold |
| `decision_cover_for_mate` | Random, lance_rep > 0 | They're in trouble, need cover | +Lance Rep, discipline risk |
| `decision_drink_invitation` | Evening, lance mate has coin | Invited for drinks | +Charm XP, +fatigue relief |
| `decision_loan_request` | Random, lance mate in need | They need to borrow coin | Gold cost, +Lance Rep |
| `decision_train_together` | Day, lance mate training | Invitation to spar | +Weapon XP for both |
| `decision_share_story` | Night, fire circle | Lance mate wants to hear your tale | +Charm XP, +Lance Rep |

### Pushed by Situation (Things That Happen)

Events that occur based on chance, time, or consequences of earlier choices.

| Event ID | Trigger | Description | Outcomes |
|----------|---------|-------------|----------|
| `decision_camp_fever` | Random (5%/day) | You wake feeling sick | Rest, push through, see surgeon |
| `decision_equipment_broke` | Post-battle (10%) | Gear damaged in fight | Repair (gold), improvise, replace |
| `decision_letter_arrives` | In town, random | Letter from home | Read, reply, share with lance |
| `decision_found_coin` | Random (rare) | Found a lost coin pouch | Keep (+Gold, +Heat), turn in |
| `decision_nightmare` | Night, after hard battle | Bad dreams | Talk to mate, endure alone |
| `decision_merchant_deal` | Entered town (20%) | Good prices available | Buy, pass |
| `decision_battlefield_salvage` | Leaving battle, tier 3+ | Fallen have usable gear | Take (risk), pass |

### Chain Events (Follow-ups)

Events that trigger as consequences of earlier decisions.

| Event ID | Chains From | Description |
|----------|-------------|-------------|
| `decision_hunting_return` | decision_organize_hunt | Your party returns with game |
| `decision_lord_hunt_conversation` | decision_lord_hunt_invitation | Lord makes conversation while riding |
| `decision_dice_winning_streak` | decision_dice_game_invite (win) | You're on a roll — keep playing? |
| `decision_dice_losing_streak` | decision_dice_game_invite (loss) | Down on luck — try to win it back? |
| `decision_fever_worsens` | decision_camp_fever (push through) | Condition gets worse |
| `decision_loan_repaid` | decision_loan_request (gave loan) | Days later, they have coin |
| `decision_party_aftermath` | decision_throw_party | The morning after |

---

## Pacing and Protection Systems

CK3 uses multiple layers to prevent event spam and keep things diverse. Here's how we implement the same protections:

---

### Layer 1: Individual Event Cooldowns

Each event has its own cooldown preventing it from firing again too soon.

```json
{
  "id": "decision_lord_hunt_invitation",
  "timing": {
    "cooldown_days": 21
  }
}
```

| Event Type | Typical Cooldown |
|------------|------------------|
| Lord invitations | 14-30 days |
| Lance Leader assignments | 5-10 days |
| Lance mate requests | 7-14 days |
| Situation events | 10-21 days |
| Chain follow-ups | 0-2 days (fast) |

---

### Layer 2: Category Cooldowns

Even if individual events are ready, the **category** has limits. This prevents "3 lance mate requests in a row."

```json
"timing": {
  "category_cooldown_days": 3
}
```

| Category | Cooldown Between Any Event in Category |
|----------|----------------------------------------|
| `decision_lord` | 7 days |
| `decision_lance_leader` | 3 days |
| `decision_lance_mate` | 2 days |
| `decision_situation` | 1 day |
| `decision_chain` | 0 (chains fire immediately) |

---

### Layer 3: Global Limits

Maximum pushed events per time period, regardless of category.

```json
{
  "lance_life_events": {
    "decision_events": {
      "max_per_day": 2,
      "max_per_week": 8,
      "min_hours_between": 4
    }
  }
}
```

| Setting | Value | Purpose |
|---------|-------|---------|
| `max_per_day` | 2 | No more than 2 pushed decisions per day |
| `max_per_week` | 8 | Spread out over the week |
| `min_hours_between` | 4 | At least 4 game hours between popups |

**Exception:** Chain events bypass `max_per_day` (player started the chain, it should conclude).

---

### Layer 4: Weight System (Diversity)

Not all eligible events have equal chance. Weights make some more likely than others.

```json
{
  "id": "decision_lord_hunt_invitation",
  "timing": {
    "base_weight": 100,
    "weight_modifiers": [
      { "condition": "lord_relation > 20", "add": 50 },
      { "condition": "tier >= 5", "add": 30 },
      { "condition": "just_won_battle", "add": 100 },
      { "condition": "pay_tension > 50", "add": -50 }
    ]
  }
}
```

**How it works:**
1. All eligible events are collected
2. Each event's weight is calculated (base + modifiers)
3. One is selected randomly, weighted by final score
4. Higher weight = more likely to be picked

**Example pool:**
| Event | Base Weight | Modifiers | Final Weight | Chance |
|-------|-------------|-----------|--------------|--------|
| Lord Hunt | 100 | +50 (relation) | 150 | 30% |
| Lance Mate Loan | 100 | — | 100 | 20% |
| Dice Game Invite | 100 | +50 (pay tension) | 150 | 30% |
| Equipment Broke | 100 | — | 100 | 20% |
| **Total** | | | **500** | 100% |

---

### Layer 5: Priority System

When multiple events are eligible, priority determines which fires first.

```json
{
  "timing": {
    "priority": 80
  }
}
```

| Priority | Value | Event Types |
|----------|-------|-------------|
| **Critical** | 100 | Chain follow-ups, severe consequences |
| **High** | 80 | Lord invitations, urgent situations |
| **Normal** | 50 | Standard decisions |
| **Low** | 20 | Minor social events |
| **Background** | 10 | Flavor events |

**Processing order:**
1. Sort eligible events by priority (descending)
2. Check global limits (max_per_day, etc.)
3. Apply weight selection among top-priority tier
4. Fire selected event

**Chain events always win:** Priority 100, bypass limits.

---

### Layer 6: One-Time and Limited Events

Some events should only fire once ever, or limited times per term.

```json
{
  "timing": {
    "one_time": true
  }
}
```

```json
{
  "timing": {
    "max_per_term": 2
  }
}
```

| Setting | Use Case |
|---------|----------|
| `one_time: true` | First time meeting the lord, major story moments |
| `max_per_term: 1` | Big events that shouldn't repeat often |
| `max_per_term: 3` | Events that can happen a few times per service term |

---

### Layer 7: Mutual Exclusion

Some events shouldn't fire if another related event fired recently.

```json
{
  "timing": {
    "excludes": ["decision_dice_game_invite", "decision_share_drink"]
  }
}
```

If `decision_dice_game_invite` fired today, `decision_share_drink` won't fire (they're both "evening social" events).

---

### Layer 8: Narrative State Blocking

Events can be blocked by story flags from previous choices.

```json
{
  "triggers": {
    "all": ["is_enlisted"],
    "none": ["rejected_lord_invitation_recently"]
  }
}
```

If the player recently rejected the lord's invitation, the lord won't invite them again for a while.

---

### Config Settings (Full Example)

```json
{
  "lance_life_events": {
    "decision_events": {
      "enabled": true,
      
      "global_limits": {
        "max_per_day": 2,
        "max_per_week": 8,
        "min_hours_between": 4,
        "chain_bypass_daily_limit": true
      },
      
      "category_cooldowns": {
        "decision_lord": 7,
        "decision_lance_leader": 3,
        "decision_lance_mate": 2,
        "decision_situation": 1,
        "decision_chain": 0
      },
      
      "weight_defaults": {
        "base_weight": 100,
        "lord_relation_bonus_per_10": 10,
        "tier_bonus_per_level": 5
      },
      
      "priority_thresholds": {
        "critical": 100,
        "high": 80,
        "normal": 50,
        "low": 20
      }
    }
  }
}
```

---

### The "Later" Option

For non-urgent decisions, include a dismiss option that doesn't penalize the player:

```json
{
  "id": "later",
  "text": "I'll think about it.",
  "tooltip": "Dismiss for now. May come up again.",
  "risk": "safe",
  "flags_set": ["dismissed_lord_hunt"],
  "cooldown_override": 7,
  "outcome": "You politely decline for now. Perhaps another time."
}
```

- `cooldown_override` sets a shorter cooldown than normal (event can come back sooner)
- `flags_set` can trigger different dialogue next time ("You turned me down before...")

---

### Summary: The Full Protection Stack

```
EVENT ELIGIBLE?
     ↓
[1] Individual cooldown passed? → No → Skip
     ↓ Yes
[2] Category cooldown passed? → No → Skip
     ↓ Yes
[3] Global daily limit not reached? → No → Queue for tomorrow
     ↓ Yes
[4] Min hours since last popup? → No → Queue for later
     ↓ Yes
[5] One-time already fired? → Yes → Skip forever
     ↓ No
[6] Max-per-term reached? → Yes → Skip this term
     ↓ No
[7] Excluded by recent event? → Yes → Skip
     ↓ No
[8] Blocked by story flags? → Yes → Skip
     ↓ No
ADD TO ELIGIBLE POOL
     ↓
CALCULATE WEIGHT (base + modifiers)
     ↓
SORT BY PRIORITY
     ↓
WEIGHTED RANDOM SELECTION FROM TOP TIER
     ↓
FIRE EVENT
```

---

### Diversity Tips (Content Design)

Beyond the mechanical protections, diversity comes from content design:

| Principle | Implementation |
|-----------|----------------|
| **Vary the source** | Mix Lord, Lance Leader, Lance Mate, and Situation events |
| **Vary the time** | Some events are morning-only, some night-only |
| **Vary the location** | Some only in camp, some only in town, some on march |
| **Vary the stakes** | Mix high-stakes decisions with low-stakes social moments |
| **Use chains sparingly** | Chains are memorable; don't chain everything |
| **Let players breathe** | Some days should have zero pushed decisions |

---

## The "Quiet Day" Philosophy

**Not every day should have a decision.** The player is a soldier in a company — most days are routine. When something *does* happen, it should feel notable.

### Target Pacing

| Time Period | Pushed Decisions | Player Feel |
|-------------|------------------|-------------|
| Quiet week (marching) | 2-3 total | "Just another week on campaign" |
| Active week (battles, towns) | 5-6 total | "Things are happening" |
| Dramatic week (pay crisis, siege) | 8-10 total | "Everything is coming to a head" |

### What Makes Events Feel Special

1. **Rarity of Lord events** — The Lord only invites you occasionally
2. **Contrast with routine** — After 3 quiet days, a dice game invite feels welcome
3. **Contextual timing** — A party makes sense after a victory, not randomly
4. **Player anticipation** — They check the menu thinking "anything happening today?"

### Quiet Day Calculation

The system should actively produce quiet days:

```
Base chance of any pushed event per day: 60%
→ 40% of days have NO pushed decisions naturally

After an event fires:
→ min_hours_between: 4 (can't spam)
→ If event fired in morning, evening event is still possible
→ If event fired in evening, next event is tomorrow at earliest
```

### Event Density by Game State

| Lord's Activity | Event Frequency Modifier |
|-----------------|--------------------------|
| Marching (traveling) | 0.7x (fewer events) |
| Camped (waiting) | 1.0x (normal) |
| In town | 1.3x (more opportunities) |
| Pre-battle | 0.5x (everyone focused) |
| Post-battle | 1.5x (lots happening) |
| Siege (attacker) | 0.8x (routine siege work) |
| Siege (defender) | 1.2x (tension, supplies) |

---

## Activity-Aware Events

**Current Gap:** Events fire based on time/location/escalation, but NOT based on what the player is actually doing (their scheduled activity or duty).

For a living CK3 feel, events should be contextual to the player's current activity. When you're training, you get training events. When you're on patrol, you get patrol events.

---

### The Problem

The Schedule system and Events system currently run as separate pipelines:

```
SCHEDULE SYSTEM                    EVENTS SYSTEM
─────────────────                  ──────────────
ScheduleBehavior                   LanceLifeEventsAutomaticBehavior
     │                                      │
     ▼                                      ▼
ScheduleExecutor                   LanceLifeEventTriggerEvaluator
     │                                      │
     ▼                                      ▼
"Training block starts"            Checks: time, location, escalation
     │                                      │
     ▼                                      ▼
[Placeholder popup]                [Random eligible event]
                                   (NOT aware of activity)
```

**Result:** Player is doing "Training", but gets a random event about dice games or letters from home. Events don't match what the player is actually doing.

---

### The Solution: Activity Context

Connect the two systems so events know what activity the player is currently doing.

#### Step 1: Add Activity Tokens to Trigger Evaluator

New trigger tokens in `CampaignTriggerTokens`:

| Token | Description |
|-------|-------------|
| `current_activity:training` | Player is in a Training activity block |
| `current_activity:rest` | Player is in a Rest block |
| `current_activity:patrol` | Player is in a Patrol Duty block |
| `current_activity:guard` | Player is in a Guard Duty block |
| `current_activity:work_detail` | Player is in a Work Detail block |
| `current_activity:free_time` | Player is in a Free Time block |
| `on_duty:quartermaster` | Player's assigned duty is Quartermaster |
| `on_duty:scout` | Player's assigned duty is Scout |
| `on_duty:field_medic` | Player's assigned duty is Field Medic |

**Implementation in `LanceLifeEventTriggerEvaluator.cs`:**

```csharp
// Activity context tokens
case string s when s.StartsWith("current_activity:"):
{
    var activityType = s.Substring("current_activity:".Length);
    var currentBlock = ScheduleBehavior.Instance?.GetCurrentActiveBlock();
    if (currentBlock == null) return false;
    
    return activityType.ToLowerInvariant() switch
    {
        "training" => currentBlock.BlockType == "TrainingDrill" 
                   || currentBlock.BlockType == "SkillTraining",
        "rest" => currentBlock.BlockType == "Rest",
        "patrol" => currentBlock.BlockType == "PatrolDuty",
        "guard" => currentBlock.BlockType == "GuardDuty",
        "work_detail" => currentBlock.BlockType == "WorkDetail",
        "free_time" => currentBlock.BlockType == "FreeTime" 
                    || currentBlock.BlockType == "PersonalTime",
        _ => currentBlock.BlockType.ToLowerInvariant().Contains(activityType)
    };
}

// Duty tokens
case string s when s.StartsWith("on_duty:"):
{
    var dutyType = s.Substring("on_duty:".Length);
    var currentDuty = EnlistmentBehavior.Instance?.CurrentDuty;
    if (string.IsNullOrEmpty(currentDuty)) return false;
    
    return currentDuty.Equals(dutyType, StringComparison.OrdinalIgnoreCase);
}
```

#### Step 2: Schedule Executor Notifies Event System

When a schedule block starts, tell the event system about the activity context:

```csharp
// In ScheduleExecutor.ExecuteScheduleBlock()
public void ExecuteScheduleBlock(ScheduledBlock block)
{
    // Set activity context for event filtering
    _currentActivityContext = block.BlockType;
    
    // Notify event system (optional: boost matching events)
    LanceLifeEventsAutomaticBehavior.Instance?.OnActivityContextChanged(block.BlockType);
    
    // ... existing execution logic ...
}
```

#### Step 3: Event Weight Boost for Matching Activity

Events tagged with the current activity get a weight boost:

```csharp
// In DecisionEventEvaluator.CalculateWeight()
private float CalculateWeight(LanceLifeEventDefinition evt)
{
    float weight = evt.Timing?.Weight ?? 1.0f;
    
    // Boost events matching current activity
    var currentActivity = ScheduleBehavior.Instance?.GetCurrentActivityContext();
    if (!string.IsNullOrEmpty(currentActivity) && EventMatchesActivity(evt, currentActivity))
    {
        weight *= 2.0f;  // Double weight for matching events
    }
    
    // ... other weight modifiers ...
    
    return weight;
}
```

---

### Event JSON Schema Update

Events can now specify activity requirements:

```json
{
  "id": "sparring_accident",
  "category": "training",
  "delivery": {
    "method": "automatic",
    "channel": "inquiry"
  },
  "triggers": {
    "all": ["is_enlisted", "current_activity:training"],
    "any": []
  },
  "timing": {
    "cooldown_days": 5,
    "weight": 1.0,
    "activity_context": "TrainingDrill"
  },
  "content": {
    "title": "Training Mishap",
    "setup": "During morning drill, your sparring partner's blade slips past your guard. The edge catches your arm — not deep, but it stings.",
    "options": [
      {
        "id": "shake_it_off",
        "text": "It's just a scratch. Keep training.",
        "effects": { "medical_risk": 1 },
        "outcome": "You bind the cut and get back to work. The sergeant nods approvingly."
      },
      {
        "id": "see_medic",
        "text": "Better get this looked at.",
        "effects": { "fatigue": 1 },
        "outcome": "The field medic cleans and wraps the wound. 'You'll live,' he says."
      }
    ]
  }
}
```

---

### Activity → Event Pool Mapping

Which events should fire during which activities:

| Activity Block | Event Categories | Example Events |
|----------------|------------------|----------------|
| **TrainingDrill** | training, injury, skill | Sparring accident, instructor tip, rivalry challenge |
| **PatrolDuty** | patrol, encounter, discovery | Bandit sighting, lost traveler, abandoned camp |
| **GuardDuty** | guard, camp, vigilance | Suspicious noise, deserter spotted, boring shift |
| **WorkDetail** | work, equipment, quartermaster | Broken tool, QM favor, supply counting |
| **Rest** | rest, social, personal | Dreams, gossip, letter from home, lance bonding |
| **FreeTime** | social, gambling, leisure | Dice game, drinking, wrestling match |
| **SkillTraining** | training, learning, mentor | Mentor advice, breakthrough, practice injury |

---

### Duty-Specific Events

Player's assigned duty also affects events:

| Duty | Unique Event Pool |
|------|-------------------|
| **Quartermaster** | Supply shortages, inventory theft, merchant dealings |
| **Scout** | Intelligence reports, getting lost, enemy sightings |
| **Field Medic** | Patient care, disease outbreak, supply requests |
| **Runner** | Message delivery, overhearing secrets, exhaustion |
| **Standard Bearer** | Banner maintenance, morale events, visibility in battle |

**Example:**

```json
{
  "id": "quartermaster_inventory_short",
  "triggers": {
    "all": ["is_enlisted", "on_duty:quartermaster"],
    "any": []
  },
  "content": {
    "setup": "Counting supplies, you notice the numbers don't add up. Someone's been helping themselves to the stores.",
    "options": [
      { "id": "report", "text": "Report to the sergeant.", "effects": { "heat": -1, "lance_reputation": -5 } },
      { "id": "investigate", "text": "Find out who first.", "effects": {} },
      { "id": "ignore", "text": "Not my problem.", "effects": { "heat": 2 } }
    ]
  }
}
```

---

### Fallback: Random Events Still Fire

Not ALL events require activity matching. Some events are universal:

```json
{
  "id": "letter_from_home",
  "triggers": {
    "all": ["is_enlisted"],
    "any": []
  },
  "timing": {
    "activity_context": null,  // No activity requirement
    "weight": 0.5  // Lower weight, fires less often
  }
}
```

**Event Selection Priority:**

1. **Activity-matching events** (2x weight boost)
2. **Duty-matching events** (1.5x weight boost)
3. **Universal events** (normal weight)

This ensures variety while keeping events contextual.

---

### Main Menu Status Display

The Main Menu shows current activity, connecting player awareness to events:

```
┌─────────────────────────────────────────┐
│ ENLISTED MAIN MENU                      │
├─────────────────────────────────────────┤
│ ► Current Status                        │
│   Activity: Training (Morning)          │  ← Player knows what they're doing
│   Duty: Scout                           │  ← Player knows their role
│   Fatigue: 12/30                        │
│                                         │
│ ► Pending Decisions [1]                 │
│   • Challenge from a lance mate         │  ← Training-related!
└─────────────────────────────────────────┘
```

When an event fires, the player thinks "Ah, this is happening because I'm training" — not "random popup from nowhere."

---

### Camp Activities Integration

Camp Activities (the repeatable options in `activities.json`) also set activity context:

1. Player selects "Spar with lance mate" from Camp Activities menu
2. System sets activity context to "Training"
3. Event system now prioritizes training-related events
4. If event fires during this activity, it feels connected

**Flow:**

```
Player selects activity    →    Activity context set    →    Matching events prioritized
     (Camp Menu)                   (ScheduleBehavior)            (Event Evaluator)
```

---

## Implementation Guide

### Data Structures

#### DecisionEventState (Persisted)

```csharp
public class DecisionEventState
{
    // Per-event tracking
    public Dictionary<string, CampaignTime> EventLastFired { get; set; }
    public HashSet<string> OneTimeFired { get; set; }
    public Dictionary<string, int> FiredThisTerm { get; set; }
    
    // Per-category tracking
    public Dictionary<string, CampaignTime> CategoryLastFired { get; set; }
    
    // Global tracking
    public int FiredToday { get; set; }
    public int FiredThisWeek { get; set; }
    public CampaignTime LastEventFired { get; set; }
    
    // Story flags
    public HashSet<string> ActiveFlags { get; set; }
    public Dictionary<string, CampaignTime> FlagExpiry { get; set; }
}
```

#### DecisionEventDefinition (From JSON)

```csharp
public class DecisionEventDefinition
{
    public string Id { get; set; }
    public string Category { get; set; }
    
    public DecisionDelivery Delivery { get; set; }
    public DecisionTriggers Triggers { get; set; }
    public DecisionTiming Timing { get; set; }
    public DecisionContent Content { get; set; }
}

public class DecisionTiming
{
    public int CooldownDays { get; set; }
    public int CategoryCooldownDays { get; set; }
    public int Priority { get; set; }
    public int BaseWeight { get; set; }
    public List<WeightModifier> WeightModifiers { get; set; }
    public bool OneTime { get; set; }
    public int MaxPerTerm { get; set; }
    public List<string> Excludes { get; set; }
}
```

### Evaluation Algorithm

```csharp
public class DecisionEventEvaluator
{
    public DecisionEventDefinition SelectEvent(
        List<DecisionEventDefinition> allEvents,
        DecisionEventState state,
        GameContext context)
    {
        // Step 1: Filter to eligible events
        var eligible = allEvents
            .Where(e => PassesAllChecks(e, state, context))
            .ToList();
        
        if (!eligible.Any())
            return null;
        
        // Step 2: Check global limits
        if (state.FiredToday >= Config.MaxPerDay)
            return null;
        
        if (HoursSince(state.LastEventFired) < Config.MinHoursBetween)
            return null;
        
        // Step 3: Group by priority tier
        var topPriority = eligible.Max(e => e.Timing.Priority);
        var topTier = eligible
            .Where(e => e.Timing.Priority >= topPriority - 10)
            .ToList();
        
        // Step 4: Calculate weights
        var weighted = topTier
            .Select(e => new {
                Event = e,
                Weight = CalculateWeight(e, context)
            })
            .ToList();
        
        // Step 5: Weighted random selection
        return WeightedRandom(weighted);
    }
    
    private bool PassesAllChecks(
        DecisionEventDefinition evt,
        DecisionEventState state,
        GameContext context)
    {
        // [1] Individual cooldown
        if (state.EventLastFired.TryGetValue(evt.Id, out var lastFired))
        {
            if (DaysSince(lastFired) < evt.Timing.CooldownDays)
                return false;
        }
        
        // [2] Category cooldown
        if (state.CategoryLastFired.TryGetValue(evt.Category, out var catLast))
        {
            if (DaysSince(catLast) < evt.Timing.CategoryCooldownDays)
                return false;
        }
        
        // [3-4] Global limits checked later (after eligibility)
        
        // [5] One-time check
        if (evt.Timing.OneTime && state.OneTimeFired.Contains(evt.Id))
            return false;
        
        // [6] Max-per-term check
        if (evt.Timing.MaxPerTerm > 0)
        {
            var firedCount = state.FiredThisTerm.GetValueOrDefault(evt.Id, 0);
            if (firedCount >= evt.Timing.MaxPerTerm)
                return false;
        }
        
        // [7] Mutual exclusion
        if (evt.Timing.Excludes != null)
        {
            foreach (var excluded in evt.Timing.Excludes)
            {
                if (state.EventLastFired.TryGetValue(excluded, out var exLast))
                {
                    if (DaysSince(exLast) < 1) // Fired today
                        return false;
                }
            }
        }
        
        // [8] Story flag blocking
        if (evt.Triggers.None != null)
        {
            foreach (var blockedFlag in evt.Triggers.None)
            {
                if (state.ActiveFlags.Contains(blockedFlag))
                    return false;
            }
        }
        
        // Standard trigger evaluation
        return EvaluateTriggers(evt.Triggers, context);
    }
    
    private int CalculateWeight(
        DecisionEventDefinition evt,
        GameContext context)
    {
        int weight = evt.Timing.BaseWeight;
        
        if (evt.Timing.WeightModifiers != null)
        {
            foreach (var mod in evt.Timing.WeightModifiers)
            {
                if (EvaluateCondition(mod.Condition, context))
                {
                    weight += mod.Add;
                }
            }
        }
        
        // Apply game state modifier (quiet march vs active battle)
        weight = (int)(weight * GetActivityModifier(context));
        
        return Math.Max(weight, 1); // Minimum weight of 1
    }
    
    private float GetActivityModifier(GameContext context)
    {
        return context.LordActivity switch
        {
            LordActivity.Traveling => 0.7f,
            LordActivity.Camped => 1.0f,
            LordActivity.InTown => 1.3f,
            LordActivity.PreBattle => 0.5f,
            LordActivity.PostBattle => 1.5f,
            LordActivity.BesiegingCastle => 0.8f,
            LordActivity.DefendingSiege => 1.2f,
            _ => 1.0f
        };
    }
}
```

### Hourly Tick Integration

```csharp
public class DecisionEventBehavior : CampaignBehaviorBase
{
    private DecisionEventEvaluator _evaluator;
    private DecisionEventState _state;
    
    public override void RegisterEvents()
    {
        CampaignEvents.HourlyTickEvent.AddNonSerializedListener(this, OnHourlyTick);
        CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, OnDailyTick);
    }
    
    private void OnHourlyTick()
    {
        if (!IsEnlisted || !IsSafeToShowEvent())
            return;
        
        // Only evaluate at certain hours (morning, evening)
        var hour = CampaignTime.Now.CurrentHourInDay;
        if (!IsEventHour(hour))
            return;
        
        // Try to select and fire an event
        var allEvents = DecisionEventCatalog.GetAll();
        var context = BuildContext();
        
        var selected = _evaluator.SelectEvent(allEvents, _state, context);
        
        if (selected != null)
        {
            FireEvent(selected);
            UpdateState(selected);
        }
    }
    
    private void OnDailyTick()
    {
        // Reset daily counter
        _state.FiredToday = 0;
        
        // Decay weekly counter
        // (or track actual days and filter)
        
        // Expire old flags
        ExpireFlags();
    }
    
    private bool IsEventHour(int hour)
    {
        // Events can fire at: 7am, 12pm, 6pm, 9pm
        return hour == 7 || hour == 12 || hour == 18 || hour == 21;
    }
    
    private void UpdateState(DecisionEventDefinition evt)
    {
        var now = CampaignTime.Now;
        
        _state.EventLastFired[evt.Id] = now;
        _state.CategoryLastFired[evt.Category] = now;
        _state.LastEventFired = now;
        _state.FiredToday++;
        _state.FiredThisWeek++;
        
        if (evt.Timing.OneTime)
            _state.OneTimeFired.Add(evt.Id);
        
        if (evt.Timing.MaxPerTerm > 0)
        {
            var count = _state.FiredThisTerm.GetValueOrDefault(evt.Id, 0);
            _state.FiredThisTerm[evt.Id] = count + 1;
        }
    }
}
```

### Persistence

```csharp
public override void SyncData(IDataStore dataStore)
{
    // Event cooldowns
    dataStore.SyncData("decision_event_last_fired", ref _eventLastFired);
    dataStore.SyncData("decision_one_time_fired", ref _oneTimeFired);
    dataStore.SyncData("decision_fired_this_term", ref _firedThisTerm);
    
    // Category cooldowns
    dataStore.SyncData("decision_category_last_fired", ref _categoryLastFired);
    
    // Global counters (reset daily, but persist for save/load mid-day)
    dataStore.SyncData("decision_fired_today", ref _firedToday);
    dataStore.SyncData("decision_last_event_fired", ref _lastEventFired);
    
    // Story flags
    dataStore.SyncData("decision_active_flags", ref _activeFlags);
    dataStore.SyncData("decision_flag_expiry", ref _flagExpiry);
}
```

---

## Testing the Pacing

### Debug Commands

Add console commands to test pacing:

| Command | Effect |
|---------|--------|
| `decision.list_eligible` | Show all currently eligible events with weights |
| `decision.force [id]` | Force-fire a specific event |
| `decision.reset_cooldowns` | Clear all cooldowns |
| `decision.skip_days [n]` | Advance time, show what would have fired |
| `decision.stats` | Show fired counts, quiet days, etc. |

### Logging

Log decision evaluations for tuning:

```
[Decision] Hourly eval at 12:00
[Decision] Eligible: 5 events
[Decision]   - decision_dice_game (weight: 150, priority: 50)
[Decision]   - decision_lance_mate_loan (weight: 100, priority: 50)
[Decision]   - decision_lord_hunt (weight: 0, BLOCKED: category cooldown)
[Decision] Selected: decision_dice_game (rolled 127/250)
[Decision] Fired today: 1/2
```

### Acceptance Criteria

| Metric | Target | How to Measure |
|--------|--------|----------------|
| Quiet days per week | 2-3 | Count days with zero pushed decisions |
| Lord events per month | 2-4 | Count `decision_lord_*` fires |
| Same-event repeat gap | 7+ days | Minimum time between same event |
| Same-category repeat gap | 2+ days | Minimum time between same category |
| Player doesn't feel spammed | Subjective | Playtest feedback |
| Player doesn't feel ignored | Subjective | Playtest feedback |

---

## Loot Result Display

When loot is awarded, show it in the outcome text:

### Single Item
> "You claim a **Sharpened Vlandian Sword** from the fallen knight."

### Multiple Items
> "Searching the camp, you find:
> - **Bent Imperial Spatha**
> - **12 Denars**
> - **Worn Leather Boots**"

### Quality Descriptions

| Quality | Description Style |
|---------|-------------------|
| Rusty | "a rusty [item], neglected but serviceable" |
| Chipped | "a chipped [item], showing signs of hard use" |
| Normal | "a [item]" |
| Sharpened | "a well-sharpened [item]" |
| Fine | "a fine [item], clearly well-crafted" |
| Masterwork | "a masterwork [item], exceptional quality" |
| Lordly | "a lordly [item], fit for a noble's hand" |

---

## Player Actions with Loot

After receiving loot, the player can:

| Action | Where | Effect |
|--------|-------|--------|
| **Keep** | Automatic | Item goes to player inventory |
| **Sell** | Next town visit | Gold based on value × merchant mood |
| **Give to Lance** | Decision option or Camp Screen | +Lance Rep, item gone |
| **Equip** | Camp Screen (Quartermaster) | Replaces current gear |

### "Give to Lance" as Decision Option

```json
{
  "id": "give_to_lance",
  "text": "Give it to {LANCE_MATE} — they need it more.",
  "tooltip": "Boost your standing with the lance.",
  "rewards": {},
  "effects": { "lance_reputation": 5 },
  "outcome": "{LANCE_MATE} grins and claps you on the shoulder. 'I owe you one.'"
}
```

---

## Full Examples

### Example 1: Gold & Reputation (No Loot)

```json
{
  "id": "decision_lance_mate_loan",
  "category": "decision",
  "delivery": {
    "method": "automatic",
    "channel": "inquiry"
  },
  "triggers": {
    "all": ["is_enlisted"],
    "any": ["Day", "Dusk"]
  },
  "escalation_requirements": {
    "pay_tension_min": 20
  },
  "requirements": {
    "tier_min": 2
  },
  "timing": {
    "cooldown_days": 14,
    "priority": 40
  },
  "content": {
    "titleId": "ll_decision_loan_title",
    "title": "A Favor Asked",
    "setupId": "ll_decision_loan_setup",
    "setup": "{LANCE_MATE} pulls you aside, looking uncomfortable. 'I hate to ask, but... could you lend me 20 denars? I've got a debt and they're not patient men. I'll pay you back when we get paid.'",
    "options": [
      {
        "id": "lend_full",
        "textId": "ll_decision_loan_opt_full",
        "text": "Here's twenty. Don't worry about it.",
        "tooltip": "Help them out. They'll remember this.",
        "costs": { "gold": 20 },
        "effects": { "lance_reputation": 8 },
        "flags_set": ["lance_mate_owes_20"],
        "outcomeId": "ll_decision_loan_outcome_full",
        "outcome": "{LANCE_MATE} looks relieved. 'I won't forget this. You're a good one.'"
      },
      {
        "id": "lend_half",
        "textId": "ll_decision_loan_opt_half",
        "text": "I can spare ten.",
        "tooltip": "Partial help. Better than nothing.",
        "costs": { "gold": 10 },
        "effects": { "lance_reputation": 4 },
        "flags_set": ["lance_mate_owes_10"],
        "outcomeId": "ll_decision_loan_outcome_half",
        "outcome": "'It's something. Thanks.' They pocket the coin with a grateful nod."
      },
      {
        "id": "decline",
        "textId": "ll_decision_loan_opt_decline",
        "text": "Sorry, I can't. I'm short too.",
        "tooltip": "Turn them down. No hard feelings.",
        "effects": { "lance_reputation": -1 },
        "outcomeId": "ll_decision_loan_outcome_decline",
        "outcome": "They shrug. 'Can't blame a man for asking.' They walk off to find someone else."
      },
      {
        "id": "advice",
        "textId": "ll_decision_loan_opt_advice",
        "text": "Talk to the sergeant about a pay advance.",
        "tooltip": "Point them to official channels.",
        "rewards": { "xp": { "Charm": 10 } },
        "outcomeId": "ll_decision_loan_outcome_advice",
        "outcome": "They consider it. 'Maybe. Thanks for the idea.' It's not money, but it's something."
      }
    ]
  }
}
```

### Example 2: Loot & Risk (Battlefield Salvage)

```json
{
  "id": "decision_battlefield_salvage",
  "category": "decision",
  "delivery": {
    "method": "automatic",
    "channel": "inquiry"
  },
  "triggers": {
    "all": ["is_enlisted", "LeavingBattle"],
    "any": []
  },
  "escalation_requirements": {
    "heat_max": 6
  },
  "requirements": {
    "tier_min": 3
  },
  "timing": {
    "cooldown_days": 5,
    "priority": 60
  },
  "content": {
    "titleId": "ll_decision_salvage_title",
    "title": "The Fallen's Gear",
    "setupId": "ll_decision_salvage_setup",
    "setup": "The battle is won, but the field is littered with the dead. You notice a fallen enemy officer nearby — his gear looks better than yours. {LANCE_LEADER} isn't watching.",
    "options": [
      {
        "id": "take_weapon",
        "textId": "ll_decision_salvage_opt_weapon",
        "text": "Claim his weapon.",
        "tooltip": "Take the officer's sidearm. Some heat risk.",
        "risk": "risky",
        "risk_chance": 0.20,
        "rewards": {
          "loot": {
            "pool": "officer_weapon",
            "tier_filter": true,
            "quality_bias": "fine_or_better",
            "rolls": 1,
            "chance_per_roll": 1.0
          }
        },
        "effects": { "heat": 1 },
        "outcomeId": "ll_decision_salvage_outcome_weapon",
        "outcome": "You slip the blade from his belt — a {LOOT_ITEM}. No one saw.",
        "outcome_failureId": "ll_decision_salvage_fail_weapon",
        "outcome_failure": "As you reach for it, a sergeant spots you. 'That goes to the quartermaster, soldier.' You back off, marked."
      },
      {
        "id": "take_armor",
        "textId": "ll_decision_salvage_opt_armor",
        "text": "Strip his armor.",
        "tooltip": "Take the officer's armor. More heat risk — harder to hide.",
        "risk": "risky",
        "risk_chance": 0.35,
        "rewards": {
          "loot": {
            "pool": "officer_armor",
            "tier_filter": true,
            "formation_filter": true,
            "quality_bias": "normal_or_better",
            "rolls": 1,
            "chance_per_roll": 1.0
          }
        },
        "effects": { "heat": 2 },
        "outcomeId": "ll_decision_salvage_outcome_armor",
        "outcome": "You work fast, claiming {LOOT_ITEM} before anyone notices.",
        "outcome_failureId": "ll_decision_salvage_fail_armor",
        "outcome_failure": "The quartermaster's men arrive before you're done. 'Official salvage,' they say, pushing you aside. Your name goes on a list."
      },
      {
        "id": "search_all",
        "textId": "ll_decision_salvage_opt_all",
        "text": "Search everything. Take what you can.",
        "tooltip": "Maximum loot. Maximum risk.",
        "risk": "risky",
        "risk_chance": 0.50,
        "costs": { "fatigue": 1 },
        "rewards": {
          "loot": {
            "pool": "battlefield_salvage",
            "tier_filter": true,
            "formation_filter": true,
            "rolls": 3,
            "chance_per_roll": 0.60
          }
        },
        "effects": { "heat": 4 },
        "outcomeId": "ll_decision_salvage_outcome_all",
        "outcome": "You work the field quickly, pocketing what you can:\n{LOOT_LIST}",
        "outcome_failureId": "ll_decision_salvage_fail_all",
        "outcome_failure": "You're caught red-handed by the provost. 'Looting? That's a flogging offense.' You're marched away in front of the lance."
      },
      {
        "id": "leave_it",
        "textId": "ll_decision_salvage_opt_leave",
        "text": "Leave it. Not worth the risk.",
        "tooltip": "Walk away clean.",
        "risk": "safe",
        "effects": { "heat": -1 },
        "outcomeId": "ll_decision_salvage_outcome_leave",
        "outcome": "You walk on. The dead have nothing you need badly enough to risk your neck."
      }
    ]
  }
}
```

---

## Loot Pools: Formation-Specific Examples

### Infantry Loot Pool

```json
{
  "id": "infantry_battlefield_salvage",
  "formation": "infantry",
  "items": [
    { "category": "one_handed_sword", "weight": 25 },
    { "category": "one_handed_axe", "weight": 20 },
    { "category": "mace", "weight": 15 },
    { "category": "shield", "weight": 20 },
    { "category": "body_armor", "slot": "body", "weight": 10 },
    { "category": "helmet", "slot": "head", "weight": 10 }
  ]
}
```

### Cavalry Loot Pool

```json
{
  "id": "cavalry_battlefield_salvage",
  "formation": "cavalry",
  "items": [
    { "category": "lance", "weight": 25 },
    { "category": "one_handed_sword", "weight": 20 },
    { "category": "polearm", "weight": 15 },
    { "category": "shield", "weight": 10 },
    { "category": "body_armor", "slot": "body", "weight": 10 },
    { "category": "horse", "weight": 10 },
    { "category": "horse_harness", "weight": 10 }
  ]
}
```

### Archer Loot Pool

```json
{
  "id": "archer_battlefield_salvage",
  "formation": "archer",
  "items": [
    { "category": "bow", "weight": 30 },
    { "category": "crossbow", "weight": 20 },
    { "category": "arrows", "weight": 20 },
    { "category": "dagger", "weight": 15 },
    { "category": "body_armor", "slot": "body", "armor_class": "light", "weight": 10 },
    { "category": "gloves", "weight": 5 }
  ]
}
```

---

## UI & Menu Integration

This section documents how Decision Events integrate with the game's menu systems to provide a seamless CK3-style experience.

### Menu Architecture Overview

The Enlisted mod has two primary UI surfaces for player interaction:

```
┌─────────────────────────────────────────────────────────────────┐
│                    ENLISTED MAIN MENU                           │
│              (enlisted_status game menu)                        │
│                                                                 │
│  • Always accessible when enlisted                              │
│  • Lightweight, quick access                                    │
│  • Uses native Bannerlord game menu system                      │
│  • Options: Quartermaster, Talk to Lord, Duties, etc.           │
│                                                                 │
│  ➤ DECISIONS GO HERE (player-initiated, quick access)          │
└─────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────┐
│                    CAMP MANAGEMENT SCREEN                       │
│              (CampManagementScreen Gauntlet UI)                 │
│                                                                 │
│  • Deep management screen                                       │
│  • Visited every ~12 days during Muster                         │
│  • Full Gauntlet UI with tabs                                   │
│  • Tabs: Lance, Orders, Activities, Reports, Army               │
│                                                                 │
│  ➤ Schedule/Activities configured here (not decisions)         │
└─────────────────────────────────────────────────────────────────┘
```

### Where Decisions Are Surfaced

| Decision Type | UI Location | Rationale |
|---------------|-------------|-----------|
| **Player-Initiated** | Enlisted Main Menu → "Pending Decisions" submenu | Quick access, always available |
| **Pushed/Automatic** | `LanceLifeEventScreen` popup | Comes to player, narrative immersion |

### Recommended Implementation: Phase 4

#### Option A: Native Game Submenu (Recommended for Phase 4)

Add a new menu option to `enlisted_status` that opens a decisions submenu:

```
enlisted_status (existing menu)
├── Master at Arms
├── Visit Quartermaster
├── My Lord...
├── Report for Duty
├── Pending Decisions (3)  ← NEW
│   ├── Request Training Leave     [Ready]
│   ├── Organize Dice Game         [3 days cooldown]
│   ├── Challenge Lance Mate       [Requires Tier 3+]
│   └── Back to status
├── Ask for Leave
└── Desert the Army
```

**Implementation Details:**

| Component | File | Changes |
|-----------|------|---------|
| Menu registration | `EnlistedMenuBehavior.cs` | Add `enlisted_decisions` submenu |
| Menu options | `EnlistedMenuBehavior.cs` | Dynamic options from `GetAvailablePlayerDecisions()` |
| Option handler | `EnlistedMenuBehavior.cs` | Call `DecisionEventBehavior.FirePlayerDecision()` |
| Decision count | `EnlistedMenuBehavior.cs` | Show count in menu text |

**Pros:**
- Fast to implement (~2 hours)
- Matches existing patterns (duty selection, settlement access)
- No new Gauntlet screens needed
- Player selects decision → opens `LanceLifeEventScreen`

**Cons:**
- Limited visual richness (text only, no icons)
- Cooldowns/requirements shown as tooltip only

#### Option B: Custom Gauntlet Screen (Future Enhancement)

Create a dedicated `DecisionsScreen` similar to `LanceLifeEventScreen`:

```
┌─────────────────────────────────────────────────────────────────┐
│                      PENDING DECISIONS                          │
│                                                                 │
├─────────────────────────────────────────────────────────────────┤
│  ┌─────────────────────────────────────────────────────────┐    │
│  │ 🎯 Request Training Leave                               │    │
│  │ Ask the Lance Leader for extra training time.           │    │
│  │ ▸ Requirements: Tier 2+                                 │    │
│  │ ▸ Status: Ready                                         │    │
│  │                                              [SELECT]   │    │
│  └─────────────────────────────────────────────────────────┘    │
│                                                                 │
│  ┌─────────────────────────────────────────────────────────┐    │
│  │ 🎲 Organize Dice Game                                   │    │
│  │ Gather the lads for a night of gambling.                │    │
│  │ ▸ Cost: 10 gold                                         │    │
│  │ ▸ Cooldown: 3 days remaining                            │    │
│  │                                              [LOCKED]   │    │
│  └─────────────────────────────────────────────────────────┘    │
│                                                                 │
├─────────────────────────────────────────────────────────────────┤
│                           [Close]                               │
└─────────────────────────────────────────────────────────────────┘
```

**Pros:**
- Rich visual presentation
- Inline requirements, cooldowns, costs
- Better player experience
- Can show locked decisions (greyed out with reason)

**Cons:**
- More development time (~4-6 hours)
- New XML + ViewModel to maintain
- Can be added as Phase 5 enhancement

### Player Feedback Flow

When a player makes a decision (from menu or pushed event):

```
Player selects option
        │
        ▼
┌───────────────────────────────────────────────────────────────┐
│ 1. EFFECT APPLICATION                                         │
│    LanceLifeEventEffectsApplier.Apply()                       │
│    ├─ Gold changes                                            │
│    ├─ Skill XP                                                │
│    ├─ Fatigue costs/recovery                                  │
│    ├─ Escalation effects (Heat, Discipline, Rep)              │
│    └─ Injury/Illness rolls                                    │
└───────────────────────────────────────────────────────────────┘
        │
        ▼
┌───────────────────────────────────────────────────────────────┐
│ 2. COMBAT LOG FEEDBACK (bottom-left messages)                 │
│    ShowEffectFeedback()                                       │
│    ├─ "Received 100 gold"                    (Yellow)         │
│    ├─ "+25 OneHanded experience"             (Cyan)           │
│    ├─ "Heat increased (+2)"                  (Red)            │
│    ├─ "Lance reputation improved (+3)"       (Cyan)           │
│    └─ etc.                                                    │
└───────────────────────────────────────────────────────────────┘
        │
        ▼
┌───────────────────────────────────────────────────────────────┐
│ 3. CHAIN EVENT HANDLING (Phase 3)                             │
│    ApplyDecisionEventEffects()                                │
│    ├─ Queue chain events (chains_to)                          │
│    ├─ Set story flags (set_flags)                             │
│    └─ Clear story flags (clear_flags)                         │
└───────────────────────────────────────────────────────────────┘
        │
        ▼
┌───────────────────────────────────────────────────────────────┐
│ 4. VISUAL UPDATES                                             │
│    UpdateEscalationTracks()                                   │
│    ├─ Heat bar updates                                        │
│    ├─ Discipline bar updates                                  │
│    └─ Lance Rep bar updates                                   │
└───────────────────────────────────────────────────────────────┘
        │
        ▼
┌───────────────────────────────────────────────────────────────┐
│ 5. NARRATIVE OUTCOME                                          │
│    ShowOutcomePopup()                                         │
│    ├─ Display outcomeTextFallback                             │
│    └─ "Continue" button closes event screen                   │
└───────────────────────────────────────────────────────────────┘
```

### Existing UI Components (Reuse These)

| Component | Location | Purpose |
|-----------|----------|---------|
| `LanceLifeEventScreen` | `src/Features/Lances/UI/` | Beautiful event popup with story, choices, bars |
| `LanceLifeEventVM` | `src/Features/Lances/UI/` | ViewModel for event screen |
| `EventChoiceVM` | `src/Features/Lances/UI/` | Individual choice button VM |
| `LanceLifeEventScreen.xml` | `GUI/Prefabs/Events/` | Gauntlet XML for event popup |
| `EventChoiceButton.xml` | `GUI/Prefabs/Events/` | Choice button XML |
| `ModernEventPresenter` | `src/Features/Lances/UI/` | Entry point to show events |

### Key Files to Modify for Phase 4

| File | Changes |
|------|---------|
| `EnlistedMenuBehavior.cs` | Add `enlisted_decisions` menu and options |
| `DecisionEventBehavior.cs` | Already has `GetAvailablePlayerDecisions()` and `FirePlayerDecision()` |
| `DecisionEventEvaluator.cs` | Already has `GetAvailablePlayerDecisions()` |

### Menu Option Availability Logic

For each player-initiated decision in the submenu:

```csharp
// Pseudo-code for menu option availability
bool IsDecisionAvailable(LanceLifeEventDefinition decision)
{
    // Check if on cooldown
    if (state.GetDaysSinceEventFired(decision.Id) < decision.Timing.CooldownDays)
        return false; // Show greyed out with "X days remaining"
    
    // Check tier requirement
    if (enlistment.Tier < decision.Requirements.Tier.Min)
        return false; // Show greyed out with "Requires Tier X"
    
    // Check gold requirement (from first option's costs)
    var goldCost = decision.Options.FirstOrDefault()?.Costs?.Gold ?? 0;
    if (Hero.MainHero.Gold < goldCost)
        return false; // Show greyed out with "Need X gold"
    
    // Check one-time
    if (decision.Timing.OneTime && state.HasFiredOneTime(decision.Id))
        return false; // Don't show at all
    
    // Check story flag blocking
    if (decision.Triggers.None?.Any(f => state.HasActiveFlag(f)) == true)
        return false; // Don't show at all
    
    return true; // Show as available
}
```

### Future Enhancements (Phase 5+)

| Enhancement | Description | Priority |
|-------------|-------------|----------|
| **Custom DecisionsScreen** | Rich Gauntlet UI for decisions list | Medium |
| **Decision categories** | Group by type (Social, Training, etc.) | Low |
| **Decision urgency** | Flash notification for expiring opportunities | Medium |
| **Quick preview** | Show costs/rewards before opening full event | Low |
| **Sound effects** | Audio feedback for effects | Low |
| **Bar animations** | Animate escalation bar changes | Low |

---

## Implementation Checklist

### Phase 1: Core Infrastructure (Week 1) ✅ COMPLETE

- [x] **DecisionEventState** — Persistence class for cooldowns, flags, counters
  - `src/Features/Lances/Events/Decisions/DecisionEventState.cs`
- [x] **DecisionEventEvaluator** — Selection algorithm with all 8 protection layers
  - `src/Features/Lances/Events/Decisions/DecisionEventEvaluator.cs`
- [x] **DecisionEventBehavior** — Hourly tick integration, event firing
  - `src/Features/Lances/Events/Decisions/DecisionEventBehavior.cs`
- [x] **Config loading** — Read pacing settings from `enlisted_config.json`
  - `src/Features/Lances/Events/Decisions/DecisionEventConfig.cs`
  - Added `decision_events` section to `enlisted_config.json`
- [x] **Persistence** — Save/load state via `IDataStore.SyncData`
- [x] **Registration** — `DecisionEventBehavior` registered in `SubModule.cs`

### Phase 1.5: Activity-Aware Events (Week 1-2) ✅ COMPLETE

**Connect events to the schedule/activity system so events match what the player is doing.**

- [x] Add `current_activity:X` tokens to `CampaignTriggerTokens`
  - `CurrentActivityPrefix = "current_activity:"`
  - `current_activity:training`, `current_activity:rest`, `current_activity:patrol`, etc.
- [x] Add `on_duty:X` tokens to `CampaignTriggerTokens`
  - `OnDutyPrefix = "on_duty:"`
  - `on_duty:quartermaster`, `on_duty:scout`, `on_duty:field_medic`, etc.
- [x] Implement token evaluation in `LanceLifeEventTriggerEvaluator.cs`
  - Query `ScheduleBehavior.Instance.GetCurrentActiveBlock()` for activity
  - Query `EnlistmentBehavior.Instance.SelectedDuty` for duty
- [x] Activity weight boost in event selection (2x for matching activity, 1.5x for duty)
- [x] Created sample decision events: `ModuleData/Enlisted/Events/events_decisions.json`
  - 6 example events: Lord hunt invitation, dice game, training offer, scout assignment, medic emergency, QM deal

### Phase 2: Pacing System (Week 2) ✅ COMPLETE (built into Phase 1)

- [x] Individual event cooldowns
- [x] Category cooldowns
- [x] Global limits (`max_per_day`, `max_per_week`, `min_hours_between`)
- [x] Weight calculation with modifiers (activity boost, priority, decay)
- [x] Priority sorting
- [x] One-time and max-per-term tracking
- [x] Mutual exclusion checking
- [x] Story flag blocking

### Phase 3: Event Chains (Week 3) ✅ COMPLETE

- [x] `chains_to` field support in schema
  - Added `chains_to`, `chain_delay_hours` to `LanceLifeEventOptionDefinition`
- [x] `set_flags` and `clear_flags` for story state management
  - Added `set_flags`, `clear_flags`, `flag_duration_days` to options
- [x] `triggers.none` for story flag blocking
  - Added `none` list to `LanceLifeEventTriggers`
- [x] `timing.excludes` for mutual exclusion
  - Added `excludes` list to `LanceLifeEventTiming`
- [x] `timing.max_per_term` for term-based limits
  - Added `max_per_term` to `LanceLifeEventTiming`
- [x] Chain event queueing (fire after delay)
  - `DecisionEventState` already had `QueueChainEvent` infrastructure
  - `LanceLifeEventVM.ApplyDecisionEventEffects` queues chain events
- [x] Chain events bypass daily limits (via priority sorting)
- [x] Story flags integration in UI (set/clear on option selection)
- [x] Sample chain events created: Lance Mate Favor → Repayment chain

### Phase 4: Player-Initiated Decisions Menu (Week 3) ✅ COMPLETE

**Add "Pending Decisions" submenu to the Enlisted Main Menu.**

- [x] **Create `enlisted_decisions` submenu** in `EnlistedMenuBehavior.cs`
  - Added menu via `starter.AddWaitGameMenu("enlisted_decisions", ...)`
  - Added "Back to status" option
  - Added background initialization handler
- [x] **Add "Pending Decisions" option** to `enlisted_status` menu
  - Shows count of available decisions: "Pending Decisions (3)"
  - Opens `enlisted_decisions` submenu
- [x] **Dynamic decision options** in submenu
  - Queries `DecisionEventBehavior.GetAvailablePlayerDecisions()`
  - Creates menu option for each decision (up to 10)
  - Shows cooldown/requirement status in tooltip
- [x] **Decision option handler**
  - On select → calls `DecisionEventBehavior.FirePlayerDecision(eventId)`
  - Opens `LanceLifeEventScreen` with full event experience
- [x] **Availability gating**
  - Decisions filtered by evaluator (cooldown, requirements, flags)
  - Tooltip shows setup text, cooldown status, costs
- [x] **Create sample player-initiated decisions**
  - `events_player_decisions.json` with 5 decisions:
    - Organize Dice Game (social)
    - Request Extra Training (skill improvement)
    - Visit the Wounded (lance reputation)
    - Petition the Lord (Tier 3+)
    - Write a Letter Home (fatigue relief)
- [x] **Public State accessor** for tooltips
  - Added `DecisionEventBehavior.State` property

**Files modified:**
- `src/Features/Interface/Behaviors/EnlistedMenuBehavior.cs`
- `src/Features/Lances/Events/Decisions/DecisionEventBehavior.cs`
- `ModuleData/Enlisted/Events/events_player_decisions.json` (new)

### Phase 5: Loot System (Optional, Week 4)

- [ ] Create `loot_pools.json` schema and loader
- [ ] Tier filtering logic
- [ ] Formation filtering logic
- [ ] Quality modifier selection
- [ ] `{LOOT_ITEM}` and `{LOOT_LIST}` placeholders

### Phase 6: Tier-Based Narrative Access (Week 5) ✅ COMPLETE

**Ensure roleplay authenticity: a T1 peasant won't hunt with the lord.**

Events and decisions must respect the player's tier/rank for narrative believability.
A Tier 1 recruit receives orders from the Lance Leader, not invitations from the Lord.
Noble activities (hunts, feasts, councils) require sufficient rank to participate.

#### Tier Access Matrix

| Tier Range | Role | Narrative Sources | Content Access |
|------------|------|-------------------|----------------|
| **T1-T2** | Recruit/Soldier | Lance Leader, Lance Mates | Camp duties, training, menial tasks |
| **T3-T4** | Veteran/Corporal | Lance Leader, occasional Lord recognition | Small party command, responsibility events |
| **T5-T6** | Sergeant/Officer | Lance Leader + direct Lord interaction | Hunts, feasts, strategic discussions |
| **T7+** | Retinue/Commander | Full noble access | All events, command decisions |

#### Implementation Tasks

- [x] **Add `narrative_source` field to events**
  - `lord`, `lance_leader`, `lance_mate`, `situation`
  - Added to `LanceLifeEventDefinition` in `LanceLifeEventCatalog.cs`
- [x] **Create tier gate lookup table** in config
  - Added `tier_gates` section to `enlisted_config.json`
  - `DecisionTierGatesConfig` class with `GetMinTierForSource()` method
- [x] **Update evaluator with narrative source check**
  - Added `PassesNarrativeSourceCheck()` as Protection Layer 9
  - Logs blocked events for debugging
- [x] **Review all existing events for tier appropriateness**
  - Hunt invitations: T5+ (narrative_source: lord)
  - Scout assignments: T2+ (narrative_source: lance_leader)
  - Quartermaster deals: T3+ (narrative_source: situation)
  - Dice games: T1+ (narrative_source: lance_mate)
  - Training offers: T1+ (narrative_source: lance_leader)
  - Petition Lord: T3+ (narrative_source: lord_direct)
  - Letter writing: T1+ (narrative_source: situation)

**Files modified:**
- `src/Features/Lances/Events/LanceLifeEventCatalog.cs` - Added `NarrativeSource` property
- `src/Features/Lances/Events/Decisions/DecisionEventConfig.cs` - Added `DecisionTierGatesConfig`
- `src/Features/Lances/Events/Decisions/DecisionEventEvaluator.cs` - Added `PassesNarrativeSourceCheck()`
- `ModuleData/Enlisted/enlisted_config.json` - Added `tier_gates` section
- `ModuleData/Enlisted/Events/events_decisions.json` - All events tagged with `narrative_source`
- `ModuleData/Enlisted/Events/events_player_decisions.json` - All events tagged
  - Petitioning lord: T3+
- [ ] **Create tier-appropriate alternative events**
  - T1-2 alternative to "hunt with lord" → "help with horse lines"
  - T1-2 alternative to "feast invitation" → "serve at feast" (kitchen duty)
- [ ] **Narrative source tokens for triggers**
  - `tier_allows:lord_invitation`, `tier_allows:noble_events`
  - Evaluator checks against player tier

#### Content Guidelines by Tier

**T1-T2 (Recruit/Soldier):**
- Takes orders from Lance Leader
- Interacts mostly with lance mates
- Camp chores, training, guard duty
- "The sergeant tells you..." not "The Lord summons you..."

**T3-T4 (Veteran/Corporal):**
- Can receive praise/criticism from Lord (brief audience)
- May lead small work parties
- Some access to officer-level information
- "Word reaches you that the Lord noticed your performance..."

**T5-T6 (Sergeant/Officer):**
- Direct Lord interactions become normal
- Invited to hunts, feasts, councils
- Makes decisions affecting others
- "Lord {NAME} invites you to join the hunting party..."

**T7+ (Retinue/Commander):**
- Full noble-tier access
- Strategic discussions
- Command authority
- "As one of the Lord's trusted commanders..."

#### Example Events

**T5+ Only: Lord's Hunt Invitation**
```json
{
  "id": "decision_lord_hunt",
  "narrative_source": "lord",
  "requirements": {
    "tier": { "min": 5 }
  }
}
```

**T1-4 Alternative: Horse Line Duty (same time slot)**
```json
{
  "id": "decision_horse_duty",
  "narrative_source": "lance_leader",
  "requirements": {
    "tier": { "min": 1, "max": 4 }
  },
  "triggers": {
    "none": ["decision_lord_hunt"]
  }
}
```

#### Acceptance Criteria

- [ ] No events with `narrative_source: lord` fire for T1-T2 players
- [ ] Noble activities (hunt, feast) require T5+
- [ ] Every "lord invitation" has a tier-appropriate alternative for lower tiers
- [ ] Narrative text matches player's social position
- [ ] Tooltips show tier requirement when player is below threshold

### Phase 7: Content Creation (Ongoing, Weeks 2-6)

**Player-Initiated Decisions:**
- [ ] Create 8-10 player-initiated decisions for Main Menu

**Activity-Specific Events:**
- [ ] Training events (5-8): sparring, injury, skill tips, rivalry
- [ ] Patrol events (5-8): encounters, discoveries, getting lost
- [ ] Guard events (5-8): suspicious sounds, deserters, boredom
- [ ] Rest events (5-8): dreams, gossip, letters, bonding
- [ ] Free time events (5-8): dice, drinking, wrestling
- [ ] Work detail events (3-5): equipment, QM interactions

**Duty-Specific Events:**
- [ ] Quartermaster events (3-5): inventory, theft, merchants
- [ ] Scout events (3-5): intelligence, enemy sightings
- [ ] Field Medic events (3-5): patients, disease, supplies

**Chain Events:**
- [ ] Create 10-15 chain follow-up events

**Balance:**
- [ ] Balance weights and cooldowns through playtesting

### Phase 8: News System Integration (Week 5)

**Integrate Decision Events with the existing News System so players can see a history of their choices.**

The News System (`EnlistedNewsBehavior`, `CampBulletinIntegration`) already tracks:
- Kingdom events (battles, sieges, prisoners, diplomacy)
- Personal events (army formation, battle participation)
- Schedule events (daily orders, block completion)

**Missing Integration:**
- Decision Events (when player makes a choice)
- Lance Life Events (injuries, deaths, lance mate interactions)
- Escalation System changes (heat/discipline thresholds crossed)

#### Implementation Tasks

- [ ] **Add news posting to `DecisionEventBehavior`**
  - When a decision event fires: "An opportunity arises: {EVENT_TITLE}"
  - When player makes a choice: "You decided to {OPTION_TEXT}"
  - Post to Personal feed category: "decision"

- [ ] **Add news posting to `LanceLifeEventEffectsApplier`**
  - When effects are applied: summarize outcome
  - Post significant outcomes: gold changes, injuries, reputation changes
  - Category: "lance"

- [ ] **Create `CampNewsGenerator.GenerateDecisionNews()`**
  ```csharp
  public static CampBulletinNewsItemVM GenerateDecisionNews(
      string eventTitle, 
      string choiceText, 
      Dictionary<string, int> effects)
  {
      // Format: "Organized Dice Game: Won 25 gold, +2 Lance Reputation"
  }
  ```

- [ ] **Add news for escalation threshold crossings**
  - When heat crosses warning threshold: "Tensions rising - the Lord takes notice"
  - When discipline drops: "Discipline is slipping"
  - When lance reputation improves: "Your standing in the lance improves"
  - Query from `LanceSimulationEventBroadcaster` or hook into threshold events

- [ ] **News categories for routing**
  | Category | Feed | Description |
  |----------|------|-------------|
  | `decision` | Lance/Personal | Player choices |
  | `lance_life` | Lance | Lance member events |
  | `escalation` | Lance | Threshold crossings |

- [ ] **Update `CampBulletinIntegration.PostNews()`**
  - Handle new categories
  - Route appropriately

#### Player Experience

When the player opens the Camp Bulletin, they should see:
- Recent decision headlines: "Organized Dice Game - Won 25 gold"
- Lance member news: "Recruit Hakon recovered from injury"
- Escalation warnings: "Discipline improved after training week"
- Kingdom events: "Lord Derthert defeated Count Aldric near Marunath"

This creates a "personal journal" feel where the player can review their choices and their impact.

#### Acceptance Criteria

- [ ] Every fired decision event creates a news item
- [ ] Every choice in a decision creates a follow-up news item with effects summary
- [ ] News items appear in Camp Bulletin under correct categories
- [ ] Escalation threshold crossings generate news
- [ ] News persists across save/load

### Phase 9: Testing & Debug (Week 6)

- [ ] `decision.list_eligible` console command
- [ ] `decision.force [id]` console command
- [ ] `decision.reset_cooldowns` console command
- [ ] `decision.stats` console command
- [ ] `decision.activity_context` console command (show current activity)
- [ ] Pacing log output for tuning
- [ ] Activity matching verification

### Acceptance Criteria

| Metric | Target |
|--------|--------|
| Quiet days per week | 2-3 days with zero pushed decisions |
| Lord events per month | 2-4 total |
| Same-event repeat gap | 7+ days minimum |
| Same-category repeat gap | 2+ days minimum |
| Activity match rate | 70%+ of events match current activity |
| Player feedback | "Events feel connected to what I'm doing" |

---

---

## Technical Debt: Two Overlapping Systems

There are currently **two separate story content systems** with overlapping content that should be consolidated.

### The Problem

| System | Location | Array Name | Schema |
|--------|----------|------------|--------|
| **Story Packs** | `StoryPacks/LanceLife/*.json` | `stories` | Simpler, trigger-based |
| **Lance Life Events** | `Events/*.json` | `events` | Full schema with delivery methods |

**Duplicates found:**
- `escalation_thresholds` exists in both locations
- `training` content exists in both (different content, same theme)

### Why This Is a Problem

1. **Confusion** — Which system is canonical?
2. **Maintenance burden** — Changes might need to be made in two places
3. **Different schemas** — Story Packs use `stories[]`, Events use `events[]`
4. **Feature gaps** — Story Packs don't support `player_initiated` delivery

### Recommendation: Consolidate to Events System

The **Events system** (`Events/*.json`) should be canonical because:
- Supports both `automatic` and `player_initiated` delivery
- Supports multiple channels: `menu`, `inquiry`, `incident`
- Has richer schema for requirements, timing, effects
- Already has more content

### Migration Plan

#### Phase 1: Audit
- [ ] List all content in `StoryPacks/LanceLife/*.json`
- [ ] Identify which are duplicated in `Events/*.json`
- [ ] Identify which are unique to Story Packs

#### Phase 2: Migrate Unique Content
- [ ] Convert unique Story Pack content to Events schema
- [ ] Add to appropriate `Events/*.json` files
- [ ] Test that events fire correctly

#### Phase 3: Deprecate Story Packs
- [ ] Add deprecation warning in Story Pack loader
- [ ] Update documentation to point to Events only
- [ ] Remove Story Pack files after testing period

#### Phase 4: Cleanup
- [ ] Remove Story Pack loader code (or keep as legacy fallback)
- [ ] update `docs/StoryBlocks/story-systems-master.md` references (ensure no legacy storyblock index links remain)
- [ ] Remove `StoryPacks/LanceLife/` folder

### Files to Migrate

| Story Pack File | Status | Notes |
|-----------------|--------|-------|
| `corruption.json` | Migrate | Unique content |
| `discipline.json` | Migrate | Unique content |
| `escalation_thresholds.json` | **Duplicate** | Events version is canonical |
| `logistics.json` | Migrate | Unique content |
| `medical.json` | Migrate | Unique content |
| `morale.json` | Migrate | Unique content |
| `training.json` | **Duplicate** | Different content, merge carefully |

### Schema Conversion Example

**Story Pack format:**
```json
{
  "id": "corruption.ledger_skim_v1",
  "category": "corruption",
  "triggers": { "all": ["entered_town"], "any": [] },
  "cooldownDays": 8,
  "options": [...]
}
```

**Events format:**
```json
{
  "id": "corruption.ledger_skim_v1",
  "category": "corruption",
  "delivery": {
    "method": "automatic",
    "channel": "inquiry"
  },
  "triggers": {
    "all": ["is_enlisted", "entered_town"],
    "any": []
  },
  "timing": {
    "cooldown_days": 8,
    "priority": 50
  },
  "content": {
    "options": [...]
  }
}
```

---

## Systems Enhancement: Making Escalation Systems Engaging

The escalation systems (Heat, Discipline, Lance Rep, Pay Tension, Medical Risk, Fatigue) exist but currently operate in silos. This section documents how to make them interconnected and engaging like CK3.

### Design Constraints

**What we CAN affect:**
- Player's health pool
- Equipment/gear access (what quartermaster offers)
- Gold (wages, costs, rewards)
- Event availability (unlock/block events based on state)
- Narrative consequences (text, story outcomes)
- Promotion eligibility
- Duty assignments

**What we CANNOT affect:**
- Player's combat skills directly (only XP)
- World state (politics, wars)
- Other characters' behavior (simulated via events)
- Map movement

---

### System Interactions Matrix

How each system should affect others:

```
HEAT ──────────────► QUARTERMASTER TRUST ──────► EQUIPMENT ACCESS
  │                                                     │
  │ (high heat = suspicious)                           │
  │                                                     ▼
  └──────────────────────────────────────────► PROMOTION ELIGIBILITY
                                                        ▲
DISCIPLINE ─────────► DUTY ACCESS ──────────────────────┘
  │                        │
  │ (high discipline =     │ (bad duty = less XP, 
  │  stuck on bad duty)    │  less prestige)
  │                        │
  └──────────────────────► LORD RELATION ◄─────── CLEAN BEHAVIOR BONUS
                                 │
                                 │ (relation gates officer track)
                                 ▼
                           TIER 5+ ACCESS

LANCE REP ─────────► PROTECTION FROM CONSEQUENCES
  │                        │
  │ (high rep = lance      │ (they warn you, cover for you,
  │  covers for you)       │  vouch during investigations)
  │                        │
  └──────────────────────► EVENT OPTIONS UNLOCKED

FATIGUE ───────────► MEDICAL RISK
  │                        │
  │ (exhaustion causes     │ (untreated = worsening)
  │  illness)              │
  │                        ▼
  └──────────────────► ACTIVITY AVAILABILITY
```

---

### Enhancement 1: Heat → Quartermaster Trust

**Current State:** Heat only triggers punishment events at thresholds (3, 5, 7, 10).

**Enhancement:** Heat affects what gear the quartermaster will offer.

| Heat Level | State | Quartermaster Effect |
|------------|-------|---------------------|
| 0 for 14+ days | "Trusted" | First pick of new gear, 10% discount, offered quality items |
| 0 | "Clean" | Normal access |
| 1-2 | "Watched" | Normal access, warning text |
| 3-4 | "Suspected" | No discount, may refuse quality items |
| 5-6 | "Known trouble" | Only standard gear, 10% markup |
| 7+ | "Marked thief" | Basic gear only, 20% markup, may refuse service |

**New Events:**

| Event ID | Trigger | Type | Description |
|----------|---------|------|-------------|
| `quartermaster_trusts_you` | Heat 0 for 14+ days | Pushed | QM offers first pick of new shipment |
| `quartermaster_good_deal` | Heat 0, in town | Pushed | QM found quality gear, offers discount |
| `quartermaster_suspicious` | Heat 4+, buying gear | Pushed | QM questions your coin source |
| `quartermaster_refuses` | Heat 6+, trying to buy | Pushed | QM won't sell you the good stuff |

**Player-Initiated:**

| Decision | Requirements | Effect |
|----------|--------------|--------|
| "Build trust with quartermaster" | Heat 0, 20 gold, cooldown 14 days | Gift/bribe, +future discount |

---

### Enhancement 2: Discipline → Duty Access

**Current State:** High discipline blocks promotion.

**Enhancement:** Discipline affects which duties you can hold and request.

| Discipline | State | Duty Effect |
|------------|-------|-------------|
| 0 for 14+ days | "Model" | Can request any duty, priority for good assignments |
| 0 | "Clean" | Normal access |
| 1-2 | "Minor marks" | Normal access |
| 3-4 | "Troubled" | Cannot request duty change |
| 5-6 | "Problem soldier" | Demoted to low-wage duty (Runner, Lookout) |
| 7+ | "Punishment detail" | Forced onto extra duty, no pay bonus |

**New Events:**

| Event ID | Trigger | Type | Description |
|----------|---------|------|-------------|
| `duty_promotion_offered` | Discipline 0, tier 3+, good performance | Pushed | Lance Leader offers better duty |
| `duty_stuck` | Discipline 3+, tries to change | Pushed | Request denied due to record |
| `duty_demotion` | Discipline 5+ | Pushed | Demoted to grunt work |
| `discipline_redemption_duty` | Discipline 3+, volunteer | Player-Init | Volunteer for hard duty to clear record |

**Player-Initiated:**

| Decision | Requirements | Effect |
|----------|--------------|--------|
| "Volunteer for dangerous duty" | Discipline 1+, not in combat | -2 Discipline, +fatigue, risk |
| "Request duty change" | Discipline < 3, cooldown | Submit request (may be denied) |

---

### Enhancement 3: Lance Rep → Protection from Consequences

**Current State:** Lance Rep has threshold events but limited gameplay impact.

**Enhancement:** High Lance Rep means the lance protects you from Heat/Discipline consequences.

| Lance Rep | State | Protection Effect |
|-----------|-------|-------------------|
| +40 to +50 | "Bonded" | Lance hides evidence, takes blame, warns of inspections |
| +20 to +39 | "Trusted" | Lance vouches for you (-Heat on investigations), shares info |
| +5 to +19 | "Accepted" | Minor help (tips about officers) |
| -4 to +4 | "Neutral" | No help, no harm |
| -5 to -19 | "Disliked" | No help offered |
| -20 to -39 | "Outcast" | Blamed first (+Heat/+Discipline when things go wrong) |
| -40 to -50 | "Hated" | Actively sabotaged, set up to fail |

**New Events:**

| Event ID | Trigger | Type | Description |
|----------|---------|------|-------------|
| `lance_warns_inspection` | Lance Rep 20+, Heat 3+, inspection coming | Pushed | Lance mate warns you, chance to hide contraband |
| `lance_vouches_for_you` | Lance Rep 30+, heat_shakedown | Pushed | They speak up for you, -Heat |
| `lance_takes_blame` | Lance Rep 40+, Discipline event | Pushed | Lance mate takes fall for you |
| `lance_blames_you` | Lance Rep -20, something goes wrong | Pushed | They point finger at you, +Heat or +Discipline |
| `lance_sabotages_gear` | Lance Rep -40 | Pushed | Your equipment "goes missing" |

**Player-Initiated:**

| Decision | Requirements | Effect |
|----------|--------------|--------|
| "Throw a party for the lance" | 50 gold, cooldown 21 days | +10 Lance Rep, gold spent |
| "Share your rations" | Has food, lance mate hungry | +3 Lance Rep |
| "Cover for a lance mate" | Lance mate in trouble | +5 Lance Rep, +Discipline risk |

---

### Enhancement 4: Lord Relation → Officer Track Gate

**Current State:** Lord relation exists but doesn't gate progression.

**Enhancement:** Officer track (T5+) requires lord's favor.

| Tier | Track | Lord Relation Requirement |
|------|-------|---------------------------|
| T1-T4 | Enlisted | None |
| T5 | Officer | Relation ≥ 5 |
| T6 | Officer | Relation ≥ 15 |
| T7 | Commander | Relation ≥ 25 |
| T8-T9 | Commander | Relation ≥ 35 |

**How to Build Lord Relation:**

| Source | Relation Gain |
|--------|---------------|
| Accept lord's invitation (hunt, errand) | +2 to +5 |
| Distinguished in battle | +1 to +3 |
| Complete lord's personal mission | +5 to +10 |
| "Model Soldier" status (see below) | +1 per week |
| Reported corruption (betrayed lance) | +3, but -Lance Rep |

**New Events:**

| Event ID | Trigger | Type | Description |
|----------|---------|------|-------------|
| `lord_notices_soldier` | Clean state, battle, tier 3+ | Pushed | Lord singles you out for praise |
| `lord_hunting_invitation` | Relation 5+, tier 4+, day | Pushed | Lord invites you to ride |
| `lord_personal_errand` | Relation 10+, in town | Pushed | Lord trusts you with personal task |
| `lord_officer_consideration` | Relation req met, XP met, tier 4 | Pushed | Lord considering you for officer |
| `lord_disappointed` | Failed lord's task, or high Heat exposed | Pushed | Lord's trust damaged |

**Player-Initiated:**

| Decision | Requirements | Effect |
|----------|--------------|--------|
| "Request audience with Lord" | Tier 4+, cooldown 14 days | Attempt to speak with lord |
| "Petition for officer consideration" | Tier 4, XP ready, Relation 5+ | Formally request promotion |

---

### Enhancement 5: Fatigue → Medical Risk Link

**Current State:** Fatigue is just a cost limiter with no consequences.

**Enhancement:** Exhaustion causes health problems.

| Fatigue Level | State | Effect |
|---------------|-------|--------|
| 0-5 | "Well Rested" | +10% XP bonus on training activities |
| 6-15 | "Normal" | No effect |
| 16-22 | "Tired" | Cannot do demanding activities (3+ fatigue cost) |
| 23-27 | "Exhausted" | +1 Medical Risk per day, event warning |
| 28+ | "Breaking" | Collapse event (forced rest, injury risk) |

**New Events:**

| Event ID | Trigger | Type | Description |
|----------|---------|------|-------------|
| `exhaustion_warning` | Fatigue 23+ | Pushed | Lance Leader tells you to rest |
| `exhaustion_collapse` | Fatigue 28+ | Pushed | You collapse, forced rest, injury roll |
| `well_rested_bonus` | Fatigue < 5, training | Modifier | Bonus XP on next training |
| `fatigue_illness` | Fatigue 25+, daily roll | Pushed | Exhaustion causes camp fever |

**Player-Initiated:**

| Decision | Requirements | Effect |
|----------|--------------|--------|
| "Take a rest day" | Any time | Skip activities, -5 Fatigue |
| "See the surgeon for fatigue" | 10 gold, in camp | -8 Fatigue, medical attention |

---

### Enhancement 6: "Model Soldier" Composite State

When multiple systems are clean simultaneously, special benefits unlock.

**Requirements:**
- Heat = 0 for 7+ days
- Discipline = 0 for 7+ days
- Lance Rep ≥ +10

**Benefits:**

| Benefit | Effect |
|---------|--------|
| Lord notices | +1 Lord Relation per week while maintained |
| First pick of gear | Quartermaster offers best items first |
| Duty priority | Can request any duty, likely approved |
| Promotion fast-track | Reduced XP requirement for next tier |
| Lance respect | +2 Lance Rep per week (reputation begets reputation) |

**Events:**

| Event ID | Trigger | Type | Description |
|----------|---------|------|-------------|
| `model_soldier_recognized` | Achieve Model Soldier status | Pushed | Lance Leader commends you publicly |
| `model_soldier_lord_notice` | Model Soldier, week passed | Pushed | Lord hears about your record |
| `model_soldier_lost` | Heat or Discipline > 0 | Pushed | You've tarnished your record |

---

### Enhancement 7: Positive Threshold Events

**Current Gap:** Systems only have punishment thresholds. Add reward thresholds.

**Heat — Clean Rewards:**

| Condition | Event | Effect |
|-----------|-------|--------|
| Heat 0 for 7 days | `heat_reputation_clean` | "Word is you're straight. That's rare." |
| Heat 0 for 21 days | `heat_trusted_with_gold` | QM trusts you with valuable delivery (+gold if honest) |
| Heat 0 for 30 days | `heat_unblemished_record` | Lord notes your clean record (+relation) |

**Discipline — Clean Rewards:**

| Condition | Event | Effect |
|-----------|-------|--------|
| Discipline 0 for 7 days | `discipline_good_standing` | Extra rest time granted |
| Discipline 0 for 21 days | `discipline_commendation` | Lance Leader commends you (+XP) |
| Discipline 0 for 30 days | `discipline_promotion_priority` | First in line for promotion |

**Lance Rep — High Rewards:**

| Condition | Event | Effect |
|-----------|-------|--------|
| Lance Rep reaches +20 | `lance_trusted_secret` | Lance mate shares a secret/opportunity |
| Lance Rep reaches +35 | `lance_gift` | Lance mate gives you something useful |
| Lance Rep +40 for 14 days | `lance_brotherhood` | You're family now — they'd die for you |

---

### Enhancement 8: Active Recovery Decisions

**Current Gap:** Recovery is mostly passive (decay over time). Add active recovery choices.

**Player-Initiated Recovery:**

| Decision | Requirements | Cost | Effect |
|----------|--------------|------|--------|
| "Come clean to the Sergeant" | Heat 2+ | -5 Lance Rep | -3 Heat |
| "Volunteer for night watch" | Discipline 1+ | +3 Fatigue | -2 Discipline |
| "Help struggling lance mate" | Lance Rep < 20 | +2 Fatigue | +3 Lance Rep |
| "Buy drinks for the lance" | 15 gold | Gold | +5 Lance Rep |
| "See the surgeon" | Medical Risk 1+ | 10 gold | -2 Medical Risk |
| "Rest and recover" | Any | 1 schedule slot | -3 Fatigue |
| "Report corruption" | Heat 3+, witnessed corruption | -20 Lance Rep | -5 Heat, +2 Lord Relation |

---

### Enhancement 9: Cross-System Event Conditions

Events should check multiple systems for richer outcomes.

**Examples:**

```json
{
  "id": "heat_investigation_lance_helps",
  "triggers": {
    "all": ["heat_5"],
    "escalation_requirements": {
      "lance_reputation_min": 25
    }
  },
  "content": {
    "setup": "Inspection coming. But {LANCE_MATE} pulls you aside. 'I'll hide it. You owe me.'",
    "options": [
      { "id": "accept_help", "effects": { "heat": -2, "lance_reputation": 3 } },
      { "id": "refuse", "text": "No, I'll face it myself." }
    ]
  }
}
```

```json
{
  "id": "lord_invitation_blocked_by_heat",
  "triggers": {
    "all": ["lord_relation_10"],
    "escalation_requirements": {
      "heat_min": 5
    }
  },
  "content": {
    "setup": "The lord was going to invite you to ride, but word of your... reputation reached him first.",
    "options": [
      { "id": "understand", "text": "I understand. I'll earn his trust back." }
    ]
  }
}
```

```json
{
  "id": "model_soldier_officer_offer",
  "triggers": {
    "all": ["tier_4", "xp_ready"],
    "escalation_requirements": {
      "heat_max": 0,
      "discipline_max": 0,
      "lance_reputation_min": 15,
      "lord_relation_min": 10
    }
  },
  "content": {
    "setup": "{LORD_NAME} summons you. 'Your record is spotless. Your lance respects you. I'm promoting you to {NEXT_RANK}.'",
    "options": [
      { "id": "accept", "effects": { "promotes": true } },
      { "id": "decline", "text": "I'm not ready, my lord." }
    ]
  }
}
```

---

### Implementation Checklist: Systems Enhancement

**Phase 1: Positive Thresholds**
- [ ] Add Heat clean events (7, 21, 30 days)
- [ ] Add Discipline clean events (7, 21, 30 days)
- [ ] Add Lance Rep high events (+20, +35, +40)
- [ ] Add "Model Soldier" composite state detection

**Phase 2: System Interactions**
- [ ] Quartermaster trust based on Heat level
- [ ] Duty access based on Discipline level
- [ ] Lance protection based on Lance Rep
- [ ] Officer track gating by Lord Relation
- [ ] Fatigue → Medical Risk at high levels

**Phase 3: Active Recovery Decisions**
- [ ] Add recovery decisions to player-initiated menu
- [ ] Implement decision effects on escalation values
- [ ] Add cooldowns for recovery actions

**Phase 4: Cross-System Events**
- [ ] Update event evaluator to check multiple escalation requirements
- [ ] Create events with compound conditions
- [ ] Test interaction chains

**Phase 5: Config**
- [ ] Add positive threshold timing to `enlisted_config.json`
- [ ] Add quartermaster trust levels
- [ ] Add duty access rules
- [ ] Add officer track relation requirements

---

## References

- `docs/Features/Core/lance-life-events.md` — Event system architecture
- `docs/StoryBlocks/story-systems-master.md` — content index + system effects
- `ModuleData/Enlisted/Events/*.json` — Event pack definitions
- `ModuleData/Enlisted/enlisted_config.json` — Event pacing config

---

## Version History

**v1.9** (December 17, 2025) — PHASE 6 IMPLEMENTED (Tier-Based Narrative Access)
- ✅ Implemented Phase 6: Tier-Based Narrative Access
  - Added `narrative_source` field to `LanceLifeEventDefinition`
  - Added `tier_gates` config with `GetMinTierForSource()` lookup
  - Added `PassesNarrativeSourceCheck()` as Protection Layer 9
  - Tagged all decision events with appropriate `narrative_source`
  - Lord hunt: T5+ only, Lance mate dice: T1+, Petition Lord: T3+

**v1.8** (December 17, 2025) — NEWS INTEGRATION PHASE DOCUMENTED
- Added Phase 8: News System Integration
  - Decision events post news when fired and when player chooses
  - Lance life events post news for significant outcomes
  - Escalation threshold crossings generate news
  - `CampNewsGenerator.GenerateDecisionNews()` method spec
  - News categories: `decision`, `lance_life`, `escalation`
  - Creates "personal journal" feel in Camp Bulletin
- Fixed phase numbering (duplicate Phase 6 → now correctly numbered 6-9)

**v1.7** (December 17, 2025) — PHASE 6 DOCUMENTED (Tier-Based Narrative Access)
- Added Phase 6: Tier-Based Narrative Access
  - Tier access matrix (T1-2 → Lance Leader, T5+ → Lord interactions)
  - `narrative_source` field concept for events
  - Content guidelines by tier range
  - Tier-appropriate alternative event pattern
  - Example events showing tier gating
- Fixed SaveDefiner ID collision (enum IDs 100-103 to avoid class ID overlap)
- Fixed dual serialization issue (`[SaveableField]` + `SyncData` conflict)

**v1.6** (December 16, 2025) — PHASE 4 IMPLEMENTED
- ✅ Implemented Phase 4: Player-Initiated Decisions Menu
  - Added `enlisted_decisions` submenu to `EnlistedMenuBehavior.cs`
  - Added "Pending Decisions" option with decision count
  - Dynamic decision slots (up to 10)
  - Tooltips with setup text, cooldown, costs
  - Selection opens `LanceLifeEventScreen`
  - Created 5 sample player-initiated decisions
- Added `State` public accessor to `DecisionEventBehavior`

**v1.5** (December 16, 2025) — UI/MENU INTEGRATION DOCUMENTED
- Added "UI & Menu Integration" section
  - Documented menu architecture (Enlisted Main Menu vs Camp Management)
  - Recommended Option A (Native Submenu) for Phase 4
  - Documented Option B (Custom Gauntlet Screen) for future enhancement
  - Added complete player feedback flow diagram
  - Listed existing UI components to reuse
  - Detailed Phase 4 implementation plan for `EnlistedMenuBehavior.cs`
- Updated Phase 4 checklist with specific implementation tasks

**v1.4** (December 16, 2025) — PHASE 3 IMPLEMENTED
- ✅ Implemented Phase 3: Event Chains
  - Added `chains_to`, `chain_delay_hours` to option schema
  - Added `set_flags`, `clear_flags`, `flag_duration_days` for story state
  - Added `triggers.none` for story flag blocking
  - Added `timing.excludes` for mutual exclusion
  - Added `timing.max_per_term` for term-based limits
  - Chain event queueing integrated into UI option selection
  - Sample chain: Lance Mate Favor → Repayment (3 events)
- ✅ Updated `events_decisions.json` with 9 total events

**v1.3** (December 16, 2025) — PHASE 1 + 1.5 IMPLEMENTED
- ✅ Implemented Phase 1: Core Infrastructure
  - `DecisionEventState` persistence class
  - `DecisionEventConfig` with JSON config loading
  - `DecisionEventEvaluator` with 8 protection layers
  - `DecisionEventBehavior` with hourly tick and event firing
  - Registered in `SubModule.cs`
- ✅ Implemented Phase 1.5: Activity-Aware Events
  - Added `current_activity:X` and `on_duty:X` tokens to `CampaignTriggerTokens`
  - Implemented token evaluation in `LanceLifeEventTriggerEvaluator`
  - Activity weight boost (2x for activity match, 1.5x for duty match)
- ✅ Created sample events: `events_decisions.json` (6 events)
- ✅ Phase 2 (Pacing) built into Phase 1 evaluator

**v1.2** (December 16, 2025)
- Added Activity-Aware Events section
- Documented `current_activity:X` and `on_duty:X` trigger tokens
- Added activity context integration with Schedule system
- Added activity weight boost for matching events
- Updated Implementation Checklist with Phase 1.5 (Activity Integration)
- Added activity-specific and duty-specific event content plans
- Added acceptance criteria for activity match rate (70%+)

**v1.1** (December 16, 2025)
- Added Systems Enhancement section
- Documented system interactions (Heat→QM, Discipline→Duty, etc.)
- Added positive threshold events
- Added active recovery decisions
- Added cross-system event examples
- Added Model Soldier composite state

**v1.0** (December 16, 2025)
- Initial spec for Decision Events
- Loot pools with tier/formation filtering
- Item quality modifiers
- Event chaining system
- Pacing guidelines

