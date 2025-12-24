# Orders System (Chain of Command)

**Summary:** The Orders system delivers military directives from your superiors (Sergeants, Captains, or your Lord) every 3-5 days. You must accept or decline each order, with success building reputation and failure damaging it. This replaced the legacy passive duties system with explicit, mission-driven tasks that integrate with skills, traits, strategic context, and progression.

**Status:** ✅ Current  
**Last Updated:** 2025-12-23  
**Related Docs:** [Core Gameplay](core-gameplay.md), [Content System Architecture](../Content/content-system-architecture.md), [Event Catalog](../../Content/event-catalog-by-system.md)

---

## Index

- [Overview](#overview)
- [How Orders Work](#how-orders-work)
- [Order Types by Tier](#order-types-by-tier)
- [Success & Failure](#success--failure)
- [Strategic Context Filtering](#strategic-context-filtering)
- [Declining Orders](#declining-orders)
- [Implementation](#implementation)
- [Creating New Orders](#creating-new-orders)

---

## Overview

The Orders system is one of the three core pillars of the Enlisted mod. Instead of passive duty assignments, you receive explicit military directives from the chain of command.

**Key Features:**
- **17 orders** across 3 tier groups (T1-T3, T4-T6, T7-T9)
- **Every 3-5 days** (configurable via `enlisted_config.json`)
- **Accept or Decline** - meaningful choice with consequences
- **Skill-based success** - relevant skills improve your odds
- **Strategic filtering** - orders match current campaign context
- **Chain of command** - issuer depends on your rank

**Design Philosophy:**
Orders are the primary gameplay driver. They provide structure, create meaningful choices, and drive character progression. Success builds reputation; failure damages it. Declining orders repeatedly leads to discharge.

---

## How Orders Work

### 1. Order Assignment

Orders are assigned every 3-5 days based on:
- **Your tier** (T1-T9)
- **Current strategic context** (e.g., Grand Campaign, Siege Works, Winter Camp)
- **Cooldowns** (each order has a cooldown period)
- **Random selection** from eligible orders

**Issuer Hierarchy:**
| Tier | Issuer | Example Orders |
|------|--------|----------------|
| T1-T3 | Sergeant | Guard watch, camp patrol, firewood collection |
| T4-T6 | Captain | Scout route, lead patrol, inspect defenses |
| T7-T9 | Lord | Command squad, strategic planning, inspect readiness |

### 2. Player Response

When an order is issued, you must respond:
- **Accept** - Attempt the order (success/failure determined by skills)
- **Decline** - Refuse the order (reputation penalty + discipline increase)

**Declining Consequences:**
- Officer Rep: -8 to -15
- Discipline: +1
- **5+ declines in a row** → Risk of dishonorable discharge

### 3. Outcome Resolution

If you accept, the outcome is determined by:
```
Success Chance = Base 60% + (Relevant Skill / 3)
```

**Example:** Scout Route order requires Scouting 40
- Player with Scouting 30: 60% + 10% = 70% success
- Player with Scouting 60: 60% + 20% = 80% success
- Player with Scouting 120: 60% + 40% = 100% success (guaranteed + bonus)

---

## Order Types by Tier

### T1-T3: Basic Soldier Orders (6 total)

| Order ID | Name | Skills | Rewards |
|----------|------|--------|---------|
| `order_guard_duty` | Guard Duty | Athletics, Perception | +8 Officer Rep, +6 Readiness |
| `order_camp_patrol` | Camp Patrol | Athletics, Scouting | +7 Officer Rep, +5 Soldier Rep |
| `order_firewood` | Firewood Collection | Athletics | +6 Soldier Rep, +5 Morale |
| `order_equipment_check` | Equipment Inspection | Crafting | +7 Officer Rep, +5 Readiness |
| `order_muster` | Muster Inspection | — | +6 Officer Rep, +4 Lord Rep |
| `order_sentry` | Sentry Post | Athletics, Perception | +7 Officer Rep, +6 Readiness |

**Focus:** Basic tasks, following orders, physical fitness, unit cohesion.

### T4-T6: Specialist Orders (6 total)

| Order ID | Name | Skill Req | Rewards |
|----------|------|-----------|---------|
| `order_scout_route` | Scout the Route | Scouting 40 | +Lord Rep, +Officer Rep, +50g |
| `order_treat_wounded` | Treat the Wounded | Medicine 40 | +Officer Rep, +Soldier Rep, +Morale |
| `order_repair_equipment` | Equipment Repair | Crafting 50 | +Officer Rep, +Equipment |
| `order_forage` | Forage Supplies | Scouting 30 | +Officer Rep, +Supplies |
| `order_lead_patrol` | Lead a Patrol | Tactics 50 | +Lord Rep, +75g |
| `order_inspect_defenses` | Inspect Defenses | Engineering 40 | +Lord Rep, +Readiness |

**Focus:** Specialized skills, leadership opportunities, higher stakes, gold rewards.

**Critical Failure Risks:**
- `order_scout_route`: Ambush (1-2 troop loss, player HP loss)
- `order_lead_patrol`: Patrol lost (2-4 troop loss, morale crash)
- `order_forage`: Spoiled food (food loss, medical risk)

### T7-T9: Leadership Orders (5 total)

| Order ID | Name | Skill Req | Rewards |
|----------|------|-----------|---------|
| `order_command_squad` | Command a Squad | Leadership 80, Tactics 70 | +All Reps, +150g, +5 Renown |
| `order_strategic_planning` | Strategic Planning | Tactics 100 | +Lord Rep, +200g, +10 Renown |
| `order_coordinate_supply` | Coordinate Supply | Steward 80 | +Lord Rep, +Supplies, +120g |
| `order_interrogate` | Interrogate Prisoner | Charm 60 | +Lord Rep, +100g |
| `order_inspect_readiness` | Inspect Company Readiness | Leadership 100 | +All Reps, +Readiness, +Morale |

**Focus:** Command authority, strategic thinking, high-value rewards, renown gain.

**Critical Failure Risks:**
- `order_command_squad`: Squad routed (3-6 troop loss, lord rep crash)
- `order_coordinate_supply`: Supply theft (food loss, scrutiny on player)

---

## Success & Failure

### Success Outcomes

**Rewards:**
- **Reputation:** Officer Rep (+5 to +10), Lord Rep (+4 to +10), Soldier Rep (+5 to +10)
- **Company Needs:** Readiness, Morale, Supplies, Equipment (+5 to +10)
- **Skill XP:** 20-40 XP in relevant skills
- **Trait XP:** 8-15 XP in relevant traits
- **Gold:** 50-200g (T4+ only)
- **Renown:** +5 to +10 (T7+ only)

**Bonus Outcomes (Skill 120+):**
- Double XP rewards
- Bonus reputation
- Special recognition from lord

### Failure Outcomes

**Penalties:**
- **Reputation:** Officer Rep (-5 to -15)
- **Company Needs:** Negative impact (-4 to -10)
- **Critical Failures:** Troop loss, food loss, HP loss, medical risk

**Failure isn't catastrophic.** You tried and it didn't work—this is different from refusing to serve (decline).

### Outcome Text Examples

**Success:**
> "A quiet night. The sergeant commends your vigilance as dawn breaks."

**Failure:**
> "You dozed off at your post. A kicked bucket woke the camp. The shame burns."

**Decline:**
> "'Too tired for watch duty?' The sergeant's voice drips with contempt."

---

## Strategic Context Filtering

Orders are filtered by the current **Strategic Context** to ensure they match the campaign situation.

### Strategic Contexts

| Context | When Active | Order Focus |
|---------|-------------|-------------|
| **Grand Campaign** | Allied hosts gathering for offensive | Scout routes, strategic planning |
| **Last Stand** | Realm bleeding, desperate defense | Defense inspection, readiness checks |
| **Harrying the Lands** | Raiding enemy territory | Patrols, foraging |
| **Siege Works** | Active siege operations | Defense inspection, supply coordination |
| **Riding the Marches** | Peacetime patrol | Guard posts, camp patrol |
| **Garrison Duty** | Stationed in settlement | Muster inspection, equipment checks |
| **Mustering for War** | Recruitment and preparation | Equipment repair, readiness inspection |
| **Winter Camp** | Seasonal rest and training | Firewood collection, maintenance |

### Strategic Tags (JSON)

Each order has strategic tags that determine when it can be issued:

```json
{
  "id": "order_scout_route",
  "strategic_tags": ["patrol", "offensive", "scout"],
  "tags": ["specialist", "outdoor", "dangerous"]
}
```

**Orders System checks:**
1. Player tier matches order requirements
2. Strategic context matches order tags
3. Order not on cooldown
4. No order currently active

---

## Declining Orders

### Penalties

**Immediate:**
- Officer Reputation: -8 to -15
- Discipline: +1

**Repeated Declines:**
- **5+ declines in a row** → Dishonorable discharge risk
- **10+ declines total** → Permanent discharge block

### When to Decline

Valid tactical reasons:
- **Low HP** - Medical condition makes order dangerous
- **Low Fatigue** - Need rest before attempting risky order
- **Low Skill** - Order requires skills you don't have (check tooltip)
- **Strategic** - Decline low-value order to save fatigue for high-value one

**Note:** The game tracks your decline rate. Occasional declines for tactical reasons are acceptable, but habitual refusal leads to discharge.

---

## Implementation

### Code Files

| File | Purpose |
|------|---------|
| `src/Features/Orders/Behaviors/OrderManager.cs` | Core order system, pacing, assignment, tracking |
| `src/Features/Orders/OrderCatalog.cs` | JSON loader, order registry |
| `src/Features/Orders/OrderDefinition.cs` | Data structures for orders |
| `src/Features/Interface/Behaviors/EnlistedMenuBehavior.cs` | Order display in Camp Hub |

### Data Files

| File | Contents |
|------|----------|
| `ModuleData/Enlisted/Orders/orders_t1_t3.json` | 6 basic orders (T1-T3) |
| `ModuleData/Enlisted/Orders/orders_t4_t6.json` | 6 specialist orders (T4-T6) |
| `ModuleData/Enlisted/Orders/orders_t7_t9.json` | 5 leadership orders (T7-T9) |
| `ModuleData/Languages/enlisted_strings.xml` | Localized order text |

### Configuration

All timing is configurable via `ModuleData/Enlisted/enlisted_config.json`:

```json
{
  "decision_events": {
    "pacing": {
      "event_window_min_days": 3,
      "event_window_max_days": 5,
      "evaluation_hours": [8, 14, 20]
    }
  }
}
```

**Settings:**
- `event_window_min_days`: Minimum days between orders (default: 3)
- `event_window_max_days`: Maximum days between orders (default: 5)
- `evaluation_hours`: Times when orders can be issued (default: 8am, 2pm, 8pm)

---

## Creating New Orders

### 1. Define Order in JSON

Add to appropriate tier file (`orders_t1_t3.json`, etc.):

```json
{
  "id": "order_example",
  "title": "Example Order",
  "description": "A brief description of the order that appears in the UI.",
  "issuer": "Sergeant",
  "tags": ["soldier", "camp", "routine"],
  "strategic_tags": ["camp_routine", "preparation"],
  "requirements": {
    "tier_min": 1,
    "tier_max": 3,
    "min_skills": { "Athletics": 30 }
  },
  "consequences": {
    "success": {
      "reputation": { "officer": 8 },
      "company_needs": { "Readiness": 6 },
      "trait_xp": { "Vigor": 12 },
      "skill_xp": { "Athletics": 25 },
      "denars": 50,
      "renown": 5,
      "text": "Success outcome text."
    },
    "failure": {
      "reputation": { "officer": -10 },
      "company_needs": { "Readiness": -8 },
      "hp_loss": 15,
      "text": "Failure outcome text."
    },
    "critical_failure": {
      "reputation": { "officer": -15, "lord": -10 },
      "company_needs": { "Readiness": -15 },
      "troop_loss": { "min": 1, "max": 2 },
      "hp_loss": 25,
      "text": "Critical failure outcome text."
    },
    "decline": {
      "reputation": { "officer": -12 },
      "text": "Decline outcome text."
    }
  }
}
```

**Complete Schema Reference:**

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `id` | string | Yes | Unique order identifier (order_* prefix) |
| `title` | string | Yes | Display name |
| `description` | string | Yes | Order details shown to player |
| `issuer` | string | Yes | Who gives the order: "Sergeant", "Lieutenant", "Captain", "Lord" |
| `tags` | array | Yes | Tactical tags for filtering |
| `strategic_tags` | array | Yes | Strategic context tags |
| `requirements.tier_min` | int | Yes | Minimum tier (1-9) |
| `requirements.tier_max` | int | Yes | Maximum tier (1-9) |
| `requirements.min_skills` | object | Optional | Minimum skill requirements {skill: level} |

**Consequences Schema:**

**Success:**
- `reputation` - Officer/Lord/Soldier rep changes
- `company_needs` - Readiness/Morale/Supplies/Equipment changes
- `trait_xp` - Trait XP awards (Vigor, Discipline, etc.)
- `skill_xp` - Skill XP awards (Athletics, Scouting, etc.)
- `denars` - Gold reward (T4+ orders)
- `renown` - Renown gain (T7+ orders)
- `text` - Success outcome text

**Failure:**
- `reputation` - Negative rep changes
- `company_needs` - Negative need changes
- `hp_loss` - Player HP loss (optional)
- `text` - Failure outcome text

**Critical Failure (T4+ only):**
- `reputation` - Severe negative rep changes
- `company_needs` - Severe negative need changes
- `troop_loss` - {min, max} troops lost from retinue/company
- `hp_loss` - Player HP loss
- `text` - Critical failure outcome text

**Decline:**
- `reputation` - Officer/Lord rep penalty
- `text` - Decline outcome text

### 2. Add Localization Strings

Add to `ModuleData/Languages/enlisted_strings.xml`:

```xml
<string id="order_example_title" text="Example Order" />
<string id="order_example_desc" text="A brief description." />
<string id="order_example_success" text="Success outcome text." />
<string id="order_example_failure" text="Failure outcome text." />
<string id="order_example_decline" text="Decline outcome text." />
```

### 3. Strategic Tags

Use strategic tags to control when the order appears:

**Common Tags:**
- `defense` - Defensive operations
- `offensive` - Offensive campaigns
- `scout` - Reconnaissance focus
- `camp_routine` - Camp maintenance
- `preparation` - Pre-battle preparation
- `patrol` - Patrol operations
- `supply` - Supply management

### 4. Testing

1. Build the mod
2. Enlist at the appropriate tier
3. Wait for order assignment window (3-5 days)
4. Verify order appears and functions correctly
5. Test all three outcomes (success, failure, decline)

---

## Acceptance Criteria

- [x] 17 orders implemented across 3 tier groups
- [x] Strategic context filtering works correctly
- [x] Success/failure resolution uses skill checks
- [x] Reputation and company needs integrate properly
- [x] Declining orders tracks and enforces consequences
- [x] All orders have localized text
- [x] Orders respect cooldowns and pacing config
- [x] Orders integrate with progression system (XP, traits)

---

**End of Document**

