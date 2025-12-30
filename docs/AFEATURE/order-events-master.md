# Order Events Master Reference

**Summary:** Single source of truth for all order events. These are events that fire during order execution (at slot phases). AI reads this to generate JSON. This is content specification, not architecture. For architecture, see Order Progression System and Content Orchestrator.

**Status:** 📋 Specification  
**Last Updated:** 2025-12-30  
**Related Docs:** [Order Progression System](order-progression-system.md), [Content Orchestrator](content-orchestrator-plan.md), [Orders Content](orders-content.md), [Event System Schemas](../Features/Content/event-system-schemas.md) (placeholder variables, JSON schema)

---

## Quick Reference

### How Order Events Work

```
1. Player accepts order (e.g., order_guard_post)
2. Order runs through phases (4 per day: Dawn, Midday, Dusk, Night)
3. At "slot" phases, OrderProgressionBehavior rolls for event:
   - slot = 15% base chance
   - slot! = 35% base chance
   - Modified by activity level (×0.3 quiet → ×1.5 intense)
4. If event fires, select from order's event pool
5. Player makes choice, consequences apply
6. Order continues to next phase
```

**Important:** Event text uses placeholder variables (e.g., `{SERGEANT}`, `{LORD_NAME}`, `{PLAYER_RANK}`) for culture-awareness and immersion. See [Text Placeholder Variables](#text-placeholder-variables) section.

### Where This Fits

```
┌─────────────────────────────────────────────────────────────────┐
│                    CONTENT ORCHESTRATOR                          │
│         (Provides world state, coordinates timing)               │
└─────────────────────────────────────────────────────────────────┘
                              │
              ┌───────────────┴───────────────┐
              ▼                               ▼
┌─────────────────────────┐     ┌─────────────────────────┐
│   ORDER EVENTS          │     │   CAMP LIFE EVENTS      │
│   (This document)       │     │   (Separate system)     │
│   Fire during duty      │     │   Fire when off-duty    │
│   6-8 per order pool    │     │   Orchestrator-driven   │
└─────────────────────────┘     └─────────────────────────┘
```

---

## INDEX

### Quick Navigation

| Section | Description |
|---------|-------------|
| [Architecture Integration](#architecture-integration) | How this integrates with Order Progression System |
| [Consequence Codes](#consequence-codes) | All valid effect codes for JSON |
| [Context Mapping](#context-mapping) | Document codes → world_state values |
| [Order Event Pools](#order-event-pools) | Which events go to which order |
| [Event Tables](#event-tables) | Full event definitions |
| [Chain Registry](#chain-registry) | Multi-event story chains |

### Event Count by Order

| Order | Events | Tier | Primary Skill | Notes |
|-------|--------|------|---------------|-------|
| `order_guard_post` | 6 | T1-T3 | Perception | Stationary watch |
| `order_sentry_duty` | 5 | T1-T3 | Perception | Forward position |
| `order_camp_patrol` | 6 | T1-T3 | Athletics | Camp perimeter |
| `order_firewood_detail` | 5 | T1-T3 | Athletics | Wood gathering |
| `order_latrine_duty` | 3 | T1-T3 | None | Punishment duty |
| `order_equipment_cleaning` | 4 | T1-T3 | Crafting | Gear maintenance |
| `order_muster_inspection` | 3 | T1-T3 | None | Formal inspection |
| `order_march_formation` | 5 | T1-T3 | Athletics | Army movement |
| `order_scout_route` | 8 | T4+ | Scouting | Long-range recon |
| `order_escort_duty` | 6 | T4+ | Athletics/Tactics | Cargo/VIP protection |
| `order_lead_patrol` | 8 | T5+ | Tactics/Leadership | Command patrol |
| `order_treat_wounded` | 6 | T4+ | Medicine | Field medical care |
| `order_repair_equipment` | 5 | T4+ | Crafting | Skilled repair work |
| `order_forage_supplies` | 6 | T4+ | Scouting | Supply gathering |
| `order_train_recruits` | 5 | T5+ | Leadership | Drill new soldiers |
| `order_inspect_defenses` | 4 | T4+ | Engineering | Fortification check |
| **TOTAL** | **85** | | | |

**Summary by Tier:**
- T1-T3 Basic Orders: 8 orders, 37 events
- T4-T6 Specialist Orders: 8 orders, 48 events

---

## Architecture Integration

### Trigger System (from Order Progression System)

Order events fire during slot phases of active orders:

| Phase Type | Base Chance | Description |
|------------|-------------|-------------|
| `routine` | 0% | Status text only, no event possible |
| `slot` | 15% | May fire event, modified by activity |
| `slot!` | 35% | Likely fires event, modified by activity |
| `checkpoint` | 0% | Mini-reward, no event |
| `resolve` | N/A | Order completion |

### Activity Level Modifiers

World state affects event trigger probability:

| Activity Level | Multiplier | Example Situations |
|----------------|------------|-------------------|
| Quiet | ×0.3 | Garrison peacetime |
| Routine | ×0.6 | Peacetime recruiting |
| Active | ×1.0 | War campaign |
| Intense | ×1.5 | Siege operations |

**Example: Guard Post during siege vs garrison**

Guard Post has phases: `[routine] [routine] [slot] [slot!] [slot] [routine] [checkpoint] [resolve]`

- **Garrison (Quiet):** slot = 4.5%, slot! = 10.5% → ~0.2 events expected
- **Siege (Intense):** slot = 22.5%, slot! = 52.5% → ~1.0 events expected

### Event Selection Flow

```csharp
// In OrderProgressionBehavior.ProcessSlotPhase()

1. Get current WorldSituation from orchestrator
2. Apply activity modifier to base chance
3. Roll for event trigger
4. If triggered:
   a. Get order's event pool (e.g., order_guard_post events)
   b. Filter by world_state requirements
   c. Exclude recently fired (per-event cooldown)
   d. Select weighted random
   e. Queue via EventDeliveryManager
```

---

## Consequence Codes

Use these codes in the Consequences column. Maps to `effects` in JSON schema.

### Player Effects

| Code | JSON Field | Description | Example |
|------|------------|-------------|---------|
| `HP` | `hpChange` | Player health | HP -20 |
| `MED` | `medicalRisk` | Medical escalation (0-5) | MED +2 |
| `G` | `gold` | Player gold | G +50, G -30 |
| `FAT` | `fatigue` | Fatigue change | FAT +5 |
| `EQ` | — | Equipment damage/loss | EQ -1 (weapon) |

### Reputation Effects

| Code | JSON Field | Description | Example |
|------|------------|-------------|---------|
| `REP.S` | `soldierRep` | Soldier reputation (-50 to +50) | REP.S +3 |
| `REP.O` | `officerRep` | Officer reputation (0-100) | REP.O -5 |
| `REP.L` | `lordRep` | Lord reputation (0-100) | REP.L +5 |

### Escalation Tracks (0-100 Scale)

| Code | JSON Field | Scale | Description |
|------|------------|-------|-------------|
| `DISC` | `discipline` | 0-100 | Discipline record (infractions) |
| `SUSP` | `scrutiny` | 0-100 | Suspicion/scrutiny (crime) |
| `MED` | `medicalRisk` | 0-5 | Medical emergency (keep small) |

**Discipline Thresholds:**

| Level | Threshold | Event |
|-------|-----------|-------|
| Notice | 15 | Verbal warning |
| Concern | 30 | Written up |
| Problem | 50 | Restricted duty |
| Serious | 70 | Probation |
| Critical | 85 | Facing discharge |
| Max | 100 | Discharged |

**Scrutiny Thresholds:**

| Level | Threshold | Event |
|-------|-----------|-------|
| Watched | 15 | Someone notices |
| Questioned | 30 | Officer inquires |
| Investigated | 50 | Formal investigation |
| Pressured | 70 | They're squeezing you |
| Exposed | 85 | Evidence mounting |
| Max | 100 | Arrested |

**Escalation Value Guide:**

| Severity | DISC | SUSP | Examples |
|----------|------|------|----------|
| Trivial | +3-5 | +3-5 | Late, minor rule-bending |
| Minor | +5-10 | +5-10 | Gambling, small bribes, drunk |
| Moderate | +10-15 | +10-15 | Disobeying orders, theft |
| Serious | +15-25 | +15-25 | Assault, smuggling, falsifying |
| Major | +25-40 | +25-40 | Desertion attempt, major theft |
| Extreme | +40+ | +40+ | Treason, selling intel |

### Company Effects

| Code | JSON Field | Description | Example |
|------|------------|-------------|---------|
| `C.MEN` | `troopLoss` | Soldiers killed | C.MEN -3 |
| `C.WND` | `troopWounded` | Soldiers wounded | C.WND -2 |
| `C.FOOD` | `foodLoss` | Food supplies | C.FOOD -10 |
| `C.GOLD` | — | Company gold | C.GOLD -50 |
| `C.MOR` | `companyNeeds.Morale` | Company morale | C.MOR -5 |
| `C.EQ` | `companyNeeds.Equipment` | Company equipment | C.EQ -1 |

### Skill XP

| Code | JSON Field | Description | Example |
|------|------------|-------------|---------|
| `XP.*` | `skillXp` | Skill experience | XP.Scouting +15 |

### Flags & Chains

| Code | JSON Field | Description | Example |
|------|------------|-------------|---------|
| `FLAG` | `setFlags` | Sets a flag | FLAG:spy_contact |
| `→` | `chainsTo` | Triggers chain | →spy_chain_2 |

---

## Context Mapping

### Document Codes → world_state Values

The event tables use short context codes. Map to `world_state` requirements in JSON:

| Doc Code | Full Name | Your `world_state` Value | When Detected |
|----------|-----------|--------------------------|---------------|
| `GAR` | Garrison | `"peacetime_garrison"` | Lord at friendly settlement |
| `FLD-P` | Field (Peace) | `"peacetime_recruiting"` | Lord moving, no war |
| `FLD-W` | Field (War) | `"war_marching"`, `"war_active_campaign"` | Lord in active campaign |
| `S-A` | Siege-Attack | `"siege_attacking"` | Lord besieging settlement |
| `S-D` | Siege-Defend | `"siege_defending"` | Lord defending settlement |

**Note:** Post-Battle (AFT) events trigger via MapIncidentManager, not order slots. They're not in this document.

### JSON Example

```json
{
  "id": "guard_drunk_soldier",
  "order_type": "order_guard_post",
  "requirements": {
    "world_state": ["peacetime_garrison", "war_active_campaign"]
  }
}
```

---

## Reward Calibration

**Career Duration:** 3+ years minimum (252+ game days) for T1-T6.

**Target:** ~60 order events over 3 years. Slow progression. Most events give +1 to +3 rep.

| Impact Level | REP.O | REP.S | REP.L | XP | Gold | Frequency |
|--------------|-------|-------|-------|-----|------|-----------|
| **Trivial** | ±1 | ±1 | — | +3-5 | ±5-10 | 60% of events |
| **Minor** | ±2 | ±2 | ±1 | +5-8 | ±10-15 | 25% of events |
| **Standard** | ±3-4 | ±3-4 | ±2 | +8-12 | ±15-25 | 10% of events |
| **Significant** | ±5-6 | ±5-6 | ±3-4 | +12-20 | ±25-40 | 4% of events |
| **Major** | ±8 | ±8 | ±5 | +25 | ±50 | 1% of events |

**Notes:**
- Most choices should give +1 to +3 rep
- Anything above +5 is exceptional
- Lord Rep is hardest to gain (less contact with lord)
- Negative outcomes can be harsher than positive gains
- HP damage ranges: Minor -3 to -8, Standard -10 to -15, Serious -18 to -22

---

## Order Event Pools

This section defines which events belong to each order's pool.

### T1-T3 Basic Soldier Orders

#### order_guard_post

**Description:** Stand watch at your assigned position.  
**Tier:** T1-T3  
**Duration:** 2 days  
**Primary Skill:** Perception

**Event Pool (6 events):**

| ID | World State | Summary | Choices | Consequences | Check |
|----|-------------|---------|---------|--------------|-------|
| `guard_drunk_soldier` | GAR, FLD-W | Drunk soldier wants through | Let pass / Turn away / Report | REP.O -1/+1/+2, REP.S +1/-1/-2 | — |
| `guard_strange_noise` | FLD-W, S-D | Strange noise in darkness | Investigate / Hold post / Raise alarm | HP -5 (fail)/0/0, REP.O +2/+1/-1 | Perception 35 |
| `guard_officer_inspection` | GAR, FLD-W | Officer testing you | Challenge / Let pass / Hesitate | REP.O +4/-3/-1 | — |
| `guard_noble_demands` | GAR | Noble demands entry without papers | Obey / Challenge / Fetch officer | REP.O -1/+2/+1, DISC 0/+5/0 | Charm 30 |
| `guard_relieved_early` | ANY | Relief came early | Accept / Offer to stay | FAT -5/+3, REP.S 0/+2 | — |
| `guard_double_shift` | ANY | Relief never came | Stay / Go find them / Leave post | FAT +8/+3/-2, REP.O +2/0/-10 | — |

---

#### order_sentry_duty

**Description:** Guard a specific entrance or forward position.  
**Tier:** T1-T3  
**Duration:** 1 day  
**Primary Skill:** Perception

**Event Pool (5 events):**

| ID | World State | Summary | Choices | Consequences | Check |
|----|-------------|---------|---------|--------------|-------|
| `sentry_movement_spotted` | FLD-W, S-D | Movement in the dark | Raise alarm / Watch / Challenge | REP.O +1/+2/+2, HP 0/0/-8 (fail) | Perception 40 |
| `sentry_fell_asleep` | ANY | Caught yourself dozing | Shake it off / Report self / Hope | FAT +3/0/0, REP.O 0/+1/-5 (if caught) | — |
| `sentry_challenged_approach` | FLD-W | Figure approaching | Challenge / Wait / Shoot | REP.O +1/0/+2 or -5 (friendly) | Perception 40 |
| `sentry_infiltrator` | S-D | Climber on the wall | Raise alarm / Challenge / Shoot | REP.O +1/+2/+2, HP 0/-8 (fail)/0 | Perception 35 |
| `sentry_relief_late` | ANY | Your relief is late | Stay / Complain / Leave | FAT +5/+3/0, REP.O +2/-1/-8, DISC 0/+5/+15 | — |

---

#### order_camp_patrol

**Description:** Walk the camp perimeter, maintain order.  
**Tier:** T1-T3  
**Duration:** 2 days  
**Primary Skill:** Athletics

**Event Pool (6 events):**

| ID | World State | Summary | Choices | Consequences | Check |
|----|-------------|---------|---------|--------------|-------|
| `patrol_soldier_fight` | GAR, FLD-W | Two soldiers fighting | Break it up / Get sergeant / Let them fight | REP.S +1/-1/0, REP.O +1/+2/-1 | Athletics 35 |
| `patrol_gambling_ring` | GAR | Gambling behind tents | Break it up / Join / Ignore | REP.O +1/-2/0, G 0/±15/0, REP.S -1/+1/0 | — |
| `patrol_suspicious_activity` | FLD-W | Something's not right | Investigate / Report / Ignore | REP.O +2/+1/-1, HP -5 (fail)/0/0 | Perception 35 |
| `patrol_twisted_ankle` | FLD-W, S-A | Rough ground | Push through / Rest / Report | FAT +5/+2/0, HP -5/0/0, REP.O +1/0/-1 | Athletics 30 |
| `patrol_lost_item` | GAR | Someone lost something | Help find / Ignore / Take it | REP.S +2/-1/0, G 0/0/+10, DISC 0/0/+8 | Perception 35 |
| `patrol_shortcut` | ANY | Shortcut would skip checkpoints | Take it / Do full route | REP.O -3 (if caught)/+1, FAT -2/+2 | — |

---

#### order_firewood_detail

**Description:** Gather and chop wood for camp fires.  
**Tier:** T1-T3  
**Duration:** 1 day  
**Primary Skill:** Athletics

**Event Pool (5 events):**

| ID | World State | Summary | Choices | Consequences | Check |
|----|-------------|---------|---------|--------------|-------|
| `firewood_axe_slip` | ANY | Tool slipped | Quick fix / Report / Push through | HP -8/-3/-15, MED +1/0/+2 | Athletics 35 |
| `firewood_found_something` | FLD-W | Found something while digging | Report / Keep / Investigate | REP.O +2/-2/+3, G 0/+15/+20 | — |
| `firewood_wildlife` | FLD-W | Dangerous animal nearby | Warn others / Kill it / Avoid | REP.S +2/+3/0, HP 0/-10 (fail)/0 | Athletics 40 |
| `firewood_work_song` | ANY | Work song starts | Join / Stay quiet / Lead different song | REP.S +2/0/+3, C.MOR +2/0/+5 | — |
| `firewood_competition` | GAR | Work competition offered | Accept / Decline / Raise stakes | REP.S +3/0/+5, FAT +5/0/+10, G 0/0/±20 | Athletics 35 |

---

#### order_latrine_duty

**Description:** Maintain the camp latrines. Punishment duty.  
**Tier:** T1-T3  
**Duration:** 1 day  
**Primary Skill:** None

**Event Pool (3 events):**

| ID | World State | Summary | Choices | Consequences | Check |
|----|-------------|---------|---------|--------------|-------|
| `latrine_overheard_rumor` | GAR | You hear officers talking | Listen carefully / Ignore / Cough loudly | XP.Scouting +5/0/0, SUSP +5/0/0 | — |
| `latrine_officer_complains` | GAR | Officer unhappy with your work | Redo it / Argue / Apologize | REP.O +2/-5/0, FAT +5/0/0 | — |
| `latrine_soldier_gratitude` | ANY | Soldier thanks you | Accept / Brush off | REP.S +2/0 | — |

**Notes:** Low-event order. Often assigned as punishment. Minimal rewards.

---

#### order_equipment_cleaning

**Description:** Clean and maintain company gear.  
**Tier:** T1-T3  
**Duration:** 1 day  
**Primary Skill:** Crafting

**Event Pool (4 events):**

| ID | World State | Summary | Choices | Consequences | Check |
|----|-------------|---------|---------|--------------|-------|
| `cleaning_found_damage` | ANY | Discovered damaged equipment | Report / Fix quietly / Ignore | REP.O +2/+3/-2, C.EQ 0/+1/0 | Crafting 35 |
| `cleaning_helped_comrade` | GAR | Fellow soldier struggling | Help / Ignore / Mock | REP.S +3/-1/-3, FAT +3/0/0 | — |
| `cleaning_officer_inspection` | GAR | Officer inspects your work | (Automatic check) | REP.O +3/-2 | Crafting 40 |
| `cleaning_contraband_found` | GAR | Found something hidden | Report / Ignore / Take it | REP.O +3/0/-3, G 0/0/+15, SUSP 0/0/+10 | — |

---

#### order_muster_inspection

**Description:** Fall in for formal inspection.  
**Tier:** T1-T3  
**Duration:** 1 day (4 phases)  
**Primary Skill:** None

**Event Pool (3 events):**

| ID | World State | Summary | Choices | Consequences | Check |
|----|-------------|---------|---------|--------------|-------|
| `muster_kit_problem` | GAR | Something's wrong with your kit | Borrow quick / Face consequences / Bluff | REP.O 0/-5/+2 (if pass), REP.S -1/0/0 | Charm 40 |
| `muster_praised` | GAR | Officer singles you out for praise | Accept humbly / Puff up | REP.O +3/+2, REP.S +1/-2 | — |
| `muster_singled_out` | GAR | Officer singles you out suspiciously | Stand firm / Look nervous | SUSP +8/+15, REP.O +1/-1 | — |

---

#### order_march_formation

**Description:** Maintain position during army movement.  
**Tier:** T1-T3  
**Duration:** Variable  
**Primary Skill:** Athletics

**Event Pool (5 events):**

| ID | World State | Summary | Choices | Consequences | Check |
|----|-------------|---------|---------|--------------|-------|
| `march_fell_behind` | FLD-W | Struggling to keep pace | Push harder / Ask for help / Fall back | FAT +8/+3/+2, REP.O +2/0/-3 | Athletics 35 |
| `march_helped_comrade` | FLD-W | Soldier beside you struggling | Help / Ignore / Report | REP.S +3/-1/-2, FAT +5/0/0 | — |
| `march_equipment_problem` | FLD-W | Something broke | Quick fix / Report / Keep going | REP.O +2/+1/-2, EQ 0/0/-1 | Crafting 35 |
| `march_terrain_hazard` | FLD-W | Dangerous ground ahead | Warn others / Navigate carefully / Rush through | REP.S +2/+1/0, HP 0/0/-8 | Athletics 35 |
| `march_foraging_opportunity` | FLD-P | Spotted food while marching | Grab it / Report location / Ignore | C.FOOD +3/+5/0, DISC +5/0/0 | Scouting 30 |

---

### T4-T6 Specialist Orders

#### order_scout_route

**Description:** Reconnoiter ahead of the army.  
**Tier:** T4+  
**Duration:** 3 days  
**Primary Skill:** Scouting

**Event Pool (8 events):**

| ID | World State | Summary | Choices | Consequences | Check |
|----|-------------|---------|---------|--------------|-------|
| `scout_tracks_found` | FLD-W | Fresh tracks, many riders | Follow / Note and continue / Report now | REP.O +3/+2/+1, HP -12 (ambush)/0/0 | Scouting 50 |
| `scout_enemy_patrol` | FLD-W, S-A | Enemy patrol ahead | Go to ground / Shadow them / Withdraw | REP.O +1/+4/+1, XP.Scouting +5/+15/+3 | Scouting 45 |
| `scout_terrain_obstacle` | FLD-W | Landmarks don't match | Backtrack / Push on / Climb for view | FAT +5/+8/+5, REP.O +2 (success)/-3 (fail)/+3 | Scouting 45 |
| `scout_enemy_camp` | FLD-W, S-A | Found enemy camp | Get closer / Count from here / Report now | REP.L +5/+2/+1, HP -18 (caught)/0/0 | Scouting 55 |
| `scout_ambush_sprung` | FLD-W | Arrows incoming! | Flee / Fight / Surrender | HP -12/-22/0, EQ -1/-1/-all, REP.O 0/+2/-8 | Riding 50 |
| `scout_weather_closing` | FLD-W | Storm coming fast | Shelter / Push through / Turn back | FAT +3/+8/+5, HP 0/-8/0, REP.O 0/+2/-1 | — |
| `scout_opportunity` | FLD-W | Lone enemy rider, unaware | Capture / Observe / Leave | REP.O +4/+2/0, HP -12 (fail)/0/0, G +25 (ransom)/0/0 | Riding 50 |
| `scout_shortcut_found` | FLD-W | Discovered faster route | Mark it / Report immediately / Keep going | REP.L +3/+4/+1, XP.Scouting +10/+5/+3 | Scouting 40 |

---

#### order_escort_duty

**Description:** Protect cargo or VIP during transport.  
**Tier:** T4+  
**Duration:** 3 days  
**Primary Skill:** Athletics, Tactics

**Event Pool (6 events):**

| ID | World State | Summary | Choices | Consequences | Check |
|----|-------------|---------|---------|--------------|-------|
| `escort_bandits_spotted` | FLD-W | Bandits watching from ridge | Speed up / Defensive stance / Parley | FAT +5/+8/0, HP 0/-12 (attack)/0, G 0/0/-15 | Tactics 40 |
| `escort_wheel_stuck` | FLD-W | Wagon wheel stuck in mud | Push through / Lighten load / Find route | FAT +8/+5/+5, C.FOOD 0/-5/0, REP.O +2/0/+3 | Athletics 40 |
| `escort_ambush` | FLD-W | Ambush sprung on convoy | Circle defense / Charge through / Sacrifice rear | C.MEN -1/-1/-2, REP.O +3/+4/+2, HP -15/-18/-8 | Tactics 55 |
| `escort_prisoner_deal` | FLD-W | Prisoner offers bribe | Refuse / Accept / Report | REP.O +2/-5/+3, G 0/+30/0, DISC 0/+15/0 | — |
| `escort_vip_demands` | FLD-W | VIP making demands | Comply / Explain / Refuse | REP.L +1/-2/+2, REP.O -1/+2/+3 | Charm 50 |
| `escort_shortcut_option` | FLD-W | Shorter route, more risk | Take it / Stay safe | REP.O +3/+1, HP -15 (ambush)/0 | Tactics 50 |

---

#### order_lead_patrol

**Description:** Command a small patrol group.  
**Tier:** T5+  
**Duration:** 3 days  
**Primary Skill:** Tactics, Leadership

**Event Pool (8 events):**

| ID | World State | Summary | Choices | Consequences | Check |
|----|-------------|---------|---------|--------------|-------|
| `lead_soldier_behind` | FLD-W | One of your men falling behind | Wait for him / Send someone / Leave him | REP.S +3/-1/-5, FAT +3/+2/0 | Leadership 45 |
| `lead_route_dispute` | FLD-W | Your scout disagrees on direction | Trust him / Overrule / Compromise | REP.O +2/+3/+1, REP.S +2/-1/+1 | Tactics 50 |
| `lead_enemy_contact` | FLD-W, S-A | Enemy spotted ahead | Engage / Evade / Set ambush | C.MEN -1/-1/0, REP.O +3/+1/+5, HP -15/-8/0 | Tactics 55 |
| `lead_morale_drop` | FLD-W | Men are grumbling | Inspiring speech / Threaten / Listen to them | C.MOR +5/+2/+3, REP.S +3/-3/+2 | Leadership 50 |
| `lead_terrain_hazard` | FLD-W | Dangerous crossing ahead | Find safe path / Push through / Send scout | FAT +5/+8/+3, HP 0/-10 (fail)/0, C.MEN 0/-1 (fail)/0 | Scouting 45 |
| `lead_man_injured` | FLD-W | One of your men hurt | Field treatment / Send him back / Push on | MED +1/0/+2, REP.S +2/+1/-2 | Medicine 40 |
| `lead_ambush_sprung` | FLD-W | Walked into ambush | Rally defense / Fighting retreat / Charge through | C.MEN -1/-1/-2, REP.O +4/+3/+5, HP -15/-12/-20 | Tactics 55 |
| `lead_good_find` | FLD-W | Your patrol found something valuable | Secure it / Report location / Investigate | C.FOOD +5/0/+8, REP.L +2/+3/+4, HP 0/0/-8 (trap) | Perception 40 |

---

#### order_treat_wounded

**Description:** Care for injured soldiers.  
**Tier:** T4+  
**Duration:** 2 days  
**Primary Skill:** Medicine

**Event Pool (6 events):**

| ID | World State | Summary | Choices | Consequences | Check |
|----|-------------|---------|---------|--------------|-------|
| `treat_supply_shortage` | ANY | Running low on bandages | Improvise / Request more / Ration carefully | REP.S +2/+1/0, REP.O +2/+2/-1 | Medicine 45 |
| `treat_difficult_case` | ANY | This one might not make it | All effort / Triage others / Comfort him | REP.S +4/-1/+2, FAT +8/+3/+5 | Medicine 55 |
| `treat_infection_risk` | GAR, FLD-W | Wound looks infected | Aggressive treatment / Watch and wait / Seek help | HP -5 (to patient)/0/0, REP.S +3/+1/+2 | Medicine 50 |
| `treat_grateful_soldier` | ANY | Patient wants to thank you | Accept graciously / Brush it off | REP.S +3/+1 | — |
| `treat_officer_wounded` | FLD-W, S-A | Officer needs treatment | Prioritize him / Treat in order / Quick stabilize | REP.L +3/-2/+1, REP.O +3/+2/+2, REP.S -2/+2/0 | Medicine 50 |
| `treat_contagion_risk` | GAR | Sick soldiers, might spread | Isolate / Treat together / Report to captain | MED +2/+1/0, REP.S +1/+2/0, REP.O 0/0/+2 | Medicine 45 |

---

#### order_repair_equipment

**Description:** Fix and maintain company gear.  
**Tier:** T4+  
**Duration:** 2 days  
**Primary Skill:** Crafting

**Event Pool (5 events):**

| ID | World State | Summary | Choices | Consequences | Check |
|----|-------------|---------|---------|--------------|-------|
| `repair_missing_parts` | ANY | Critical part is broken, none in stock | Improvise / Cannibalize other gear / Report shortage | C.EQ +2/+1/0, REP.O +3/+1/+2 | Crafting 50 |
| `repair_sabotage_discovered` | GAR, S-D | This damage wasn't accidental | Investigate quietly / Report immediately / Confront suspect | SUSP +10/0/+15, REP.O +3/+2/+4, HP 0/0/-8 (fight) | Perception 45 |
| `repair_helped_smith` | GAR | Camp smith needs extra hands | Help willingly / Help grudgingly / Refuse | REP.S +3/+1/-2, FAT +5/+3/0, XP.Crafting +10/+5/0 | — |
| `repair_rush_job` | FLD-W, S-A | Captain needs this done NOW | Work fast / Explain properly / Cut corners | REP.O +3/+2/-2, C.EQ +1/+2/-1 | Crafting 55 |
| `repair_quality_work` | GAR | Your work is exceptional today | Show the officer / Stay humble | REP.O +4/+2, REP.S -1/+1 | — |

---

#### order_forage_supplies

**Description:** Gather food and materials from the land.  
**Tier:** T4+  
**Duration:** 2 days  
**Primary Skill:** Scouting

**Event Pool (6 events):**

| ID | World State | Summary | Choices | Consequences | Check |
|----|-------------|---------|---------|--------------|-------|
| `forage_rich_find` | FLD-W | Found a well-stocked barn | Take what's needed / Take everything / Leave payment | C.FOOD +8/+15/+5, REP.O +1/-2/+2, DISC 0/+10/0 | — |
| `forage_farmer_encounter` | FLD-P, FLD-W | Farmer with pitchfork | Pay fair price / Intimidate / Leave empty | C.FOOD +5/+8/0, G -15/0/0, REP.S +1/-2/+1 | Charm 40 |
| `forage_enemy_territory` | FLD-W, S-A | Deep in hostile land | Move carefully / Speed priority / Find cover | HP -10 (spotted)/0/0, FAT +5/+8/+3, C.FOOD +3/+5/+5 | Scouting 50 |
| `forage_wildlife_threat` | FLD-W | Dangerous animal ahead | Hunt it / Avoid it / Scare it off | C.FOOD +8/0/0, HP -12 (fail)/0/-5 | Athletics 45 |
| `forage_spoiled_cache` | FLD-W | Found supplies but they're rotting | Take what's good / Leave it / Mark for others | C.FOOD +3/0/+5, MED +1/0/0 | — |
| `forage_separated` | FLD-W | Lost sight of your group | Call out / Stay put / Find your way | REP.S -1/0/+2, FAT +5/+3/+8, HP 0/0/-8 (ambush) | Scouting 40 |

---

#### order_train_recruits

**Description:** Drill new soldiers in basics.  
**Tier:** T5+  
**Duration:** 2 days  
**Primary Skill:** Leadership

**Event Pool (5 events):**

| ID | World State | Summary | Choices | Consequences | Check |
|----|-------------|---------|---------|--------------|-------|
| `train_difficult_recruit` | GAR | One recruit won't listen | Patience / Discipline / Make example | REP.S +2/-1/-3, REP.O +1/+2/+3, XP.Leadership +8/+5/+3 | Leadership 45 |
| `train_injury_during` | GAR | Recruit hurt during drill | Stop and treat / Continue carefully / Push through | MED +1/0/+2, REP.S +2/+1/-2, FAT +3/+5/+8 | Medicine 35 |
| `train_impressive_performance` | GAR | One recruit shows real promise | Praise publicly / Note quietly / Extra training | REP.S +2/+1/+3, REP.O +1/+2/+2 | — |
| `train_officer_observes` | GAR | Captain watching your methods | Stick to basics / Show off / Ask for guidance | REP.O +2/+4/-1, REP.L 0/+2/+1 | Leadership 50 |
| `train_recruit_question` | GAR | Recruit asks about killing | Answer honestly / Deflect / Harsh truth | REP.S +3/+1/+2, XP.Leadership +5/+3/+8 | — |

---

#### order_inspect_defenses

**Description:** Check fortifications and defensive positions.  
**Tier:** T4+  
**Duration:** 2 days  
**Primary Skill:** Engineering

**Event Pool (4 events):**

| ID | World State | Summary | Choices | Consequences | Check |
|----|-------------|---------|---------|--------------|-------|
| `inspect_found_weakness` | GAR, S-D | Discovered structural flaw | Report to captain / Fix it yourself / Delegate repair | REP.L +3/+2/+1, REP.O +2/+4/+2, C.EQ 0/+3/+2 | Engineering 45 |
| `inspect_officer_dispute` | GAR | Two officers disagree on priority | Support senior / Support junior / Offer solution | REP.O +1/0/+3, REP.L +2/-1/+3 | Charm 45 |
| `inspect_sabotage_signs` | S-D | Something's been tampered with | Investigate / Report immediately / Set watch | SUSP +15/0/+5, REP.O +4/+2/+3, HP -10 (trap)/0/0 | Perception 50 |
| `inspect_improvement_idea` | GAR, S-D | You see how to make it better | Propose formally / Suggest quietly / Keep silent | REP.L +4/+2/0, REP.O +2/+3/0, XP.Engineering +15/+10/0 | Engineering 50 |

---

## Chain Registry

Multi-event story chains that span orders and contexts. Currently 4 chains designed.

| Chain ID | Events | Summary | Trigger Events |
|----------|--------|---------|----------------|
| `spy_chain` | 3 | Infiltrator → contact → mission | sentry_infiltrator, patrol_suspicious |
| `deserter_chain` | 3 | Spot deserter → returns → resolution | patrol_suspicious, guard_strange_noise |
| `mentor_chain` | 3 | Veteran interest → training → bond | firewood_helped, patrol_helped_comrade |
| `prisoner_chain` | 4 | Captured → interrogation → escape → resolution | scout_ambush_sprung (surrender) |

**Future Chains (not yet designed):**
- `rival_chain` - Conflict with soldier escalates
- `theft_chain` - Witness theft, choose sides
- `romance_chain` - Relationship develops
- `corruption_chain` - Officer bribery scheme

### Chain: spy_chain

**Summary:** Discover infiltrator during siege defense, make contact, choose allegiance.

**Flow:**
```
1. sentry_infiltrator (SENTRY, S-D, T1-T6)
   └─ Choice "Challenge" with Perception success
   └─ Sets FLAG:spy_spotted
   
2. patrol_spy_contact (PATROL, S-D, T3-T6)
   └─ Requires: FLAG:spy_spotted
   └─ "The man you caught finds you. He has a proposal."
   └─ Choices: Report immediately / Hear him out / Demand payment
   └─ Report: +REP.O +3, chain ends
   └─ Hear out: Sets FLAG:spy_listening, continues
   └─ Payment: +G +20, SUSP +20, chain ends

3. scout_spy_mission (SCOUT, S-D, T4-T6)
   └─ Requires: FLAG:spy_listening
   └─ "He needs something from inside. Information. Or sabotage."
   └─ Choices: Do it / Double-cross / Refuse
   └─ Do it: +G +50, DISC +35, SUSP +40
   └─ Double-cross: +REP.L +5, HP -15 (fight)
   └─ Refuse: +REP.O +1, chain ends
```

### Chain: mentor_chain

**Summary:** Veteran takes interest, offers training, bond forms.

**Flow:**
```
1. firewood_helped OR patrol_helped_comrade
   └─ Trigger: Choose to help struggling soldier
   └─ Sets FLAG:helped_veteran

2. patrol_mentor_offer (PATROL, GAR, T1-T3)
   └─ Requires: FLAG:helped_veteran
   └─ "Old Tormund pulls you aside. 'You've got instincts. Let me show you.'"
   └─ Choices: Accept / Decline / Ask what's in it
   └─ Accept: Sets FLAG:mentor_training, +XP.OneHanded +10
   └─ Decline: Chain ends
   └─ Suspicious: -REP.S -2, chain ends

3. guard_mentor_test (GUARD, GAR, T1-T3)
   └─ Requires: FLAG:mentor_training
   └─ "Tormund set this up. He wants to see if you learned."
   └─ Skill check: OneHanded 40
   └─ Success: +REP.S +3, +XP.OneHanded +15, Tormund ally flag
   └─ Fail: +XP.OneHanded +8, "More training needed"
```

---

## JSON Event Template

When generating JSON from this document, use this structure:

```json
{
  "id": "guard_drunk_soldier",
  "order_type": "order_guard_post",
  
  "titleId": "order_evt_guard_drunk_title",
  "title": "Drunk Soldier",
  
  "setupId": "order_evt_guard_drunk_setup",
  "setup": "{SOLDIER_NAME} approaches your post, weaving slightly. He reeks of drink. 'Just let me through, {PLAYER_RANK}.'",
  
  "requirements": {
    "world_state": ["peacetime_garrison", "war_active_campaign"]
  },
  
  "options": [
    {
      "id": "let_pass",
      "textId": "order_evt_guard_drunk_opt_let",
      "text": "Step aside. Not worth the trouble.",
      "tooltip": "Avoid confrontation. -1 Officer Rep, +1 Soldier Rep.",
      "effects": {
        "officerRep": -1,
        "soldierRep": 1
      }
    },
    {
      "id": "turn_away",
      "textId": "order_evt_guard_drunk_opt_turn",
      "text": "You're not getting through like this.",
      "tooltip": "+1 Officer Rep, -1 Soldier Rep.",
      "effects": {
        "officerRep": 1,
        "soldierRep": -1
      }
    },
    {
      "id": "report",
      "textId": "order_evt_guard_drunk_opt_report",
      "text": "Call {SERGEANT}.",
      "tooltip": "+2 Officer Rep, -2 Soldier Rep.",
      "resultText": "{SERGEANT} arrives and hauls {SOLDIER_NAME} away by the collar.",
      "effects": {
        "officerRep": 2,
        "soldierRep": -2
      }
    }
  ]
}
```

---

## Text Placeholder Variables

All text fields (`setup`, `text`, `resultText`) should use placeholder variables for immersion and culture-awareness. These are replaced at runtime with actual game data.

### Common Placeholders

| Category | Variables | Notes |
|----------|-----------|-------|
| **Player** | `{PLAYER_NAME}`, `{PLAYER_RANK}` | Player's name and culture-specific rank title |
| **NCO/Officers** | `{SERGEANT}`, `{NCO_NAME}`, `{OFFICER_NAME}`, `{CAPTAIN_NAME}` | Culture-aware NCO/officer titles |
| **Soldiers** | `{SOLDIER_NAME}`, `{COMRADE_NAME}`, `{VETERAN_NAME}`, `{RECRUIT_NAME}` | Fellow soldiers in your unit |
| **Lord/Faction** | `{LORD_NAME}`, `{LORD_TITLE}`, `{FACTION_NAME}` | Your enlisted lord and faction |
| **Location** | `{SETTLEMENT_NAME}`, `{COMPANY_NAME}` | Current settlement and party name |

### Culture-Aware Rank Examples

The `{SERGEANT}` placeholder resolves to culture-specific NCO titles:

| Culture | {SERGEANT} Resolves To |
|---------|------------------------|
| Empire | Optio |
| Vlandia | Sergeant |
| Sturgia | Huskarl |
| Battania | Oathsworn |
| Khuzait | Veteran |
| Aserai | Faaris |

**Example:**
```json
{
  "setup": "{SERGEANT} pulls you aside. '{PLAYER_NAME}, {LORD_NAME} wants the perimeter checked before nightfall.'",
  "text": "Tell {SERGEANT} you'll handle it.",
  "resultText": "You report back to {SERGEANT}. 'Well done,' he says. {LORD_NAME} will hear of this."
}
```

**Note:** The event tables in this document use shorthand (e.g., "Sergeant says...") for readability. When generating JSON, convert to placeholders for culture-awareness.

**Full placeholder reference:** See [Event System Schemas - Text Placeholder Variables](../Features/Content/event-system-schemas.md#text-placeholder-variables)

---

## Implementation Notes

### Result Display (NOT Popups)

**Event outcomes display in Recent Activities / YOU section, NOT as popups.**

| What | Display Method |
|------|----------------|
| Event setup + options | Popup (player makes choice) |
| `resultText` after choice | Recent Activities queue |
| `failure_resultText` | Recent Activities queue |
| Chain events (next phase) | New popup (it's a new event) |
| `reward_choices` sub-options | Popup (still interactive) |

**Flow:**
1. Event fires → Popup shows setup + options
2. Player picks option → Popup closes
3. `resultText` appears in Recent Activities / YOU section
4. If `chains_to` is set → New popup fires after delay

This reduces UI interruption while keeping narrative feedback visible.

**Full news system integration:** See [News & Reporting System](../Features/UI/news-reporting-system.md) for:
- `AddEventOutcome()` API for queueing results
- `EventOutcomeRecord` data structure
- Queue processing on daily tick
- Display format in Recent Activities

**Integration point:** When `OrderProgressionBehavior` fires an order event and player makes a choice, call:
```csharp
EnlistedNewsBehavior.Instance.AddEventOutcome(
    eventTitle: event.Title,
    resultNarrative: selectedOption.ResultText,  // or random from array
    severity: event.Severity,
    dayNumber: (int)CampaignTime.Now.ToDays
);
```

### File Structure

```
ModuleData/Enlisted/Orders/order_events/
├─ guard_events.json       (order_guard_post)
├─ sentry_events.json      (order_sentry_duty)
├─ patrol_events.json      (order_camp_patrol)
├─ patrol_lead_events.json (order_lead_patrol)
├─ scout_events.json       (order_scout_route)
├─ escort_events.json      (order_escort_duty)
├─ firewood_events.json    (order_firewood_detail)
├─ latrine_events.json     (order_latrine_duty)
├─ cleaning_events.json    (order_equipment_cleaning)
├─ muster_events.json      (order_muster_inspection)
├─ march_events.json       (order_march_formation)
├─ medical_events.json     (order_treat_wounded)
├─ repair_events.json      (order_repair_equipment)
├─ forage_events.json      (order_forage_supplies)
├─ training_events.json    (order_train_recruits)
└─ defenses_events.json    (order_inspect_defenses)
```

### Localization

All text fields have corresponding ID fields for XML localization:
- `titleId` / `title`
- `setupId` / `setup`
- `textId` / `text` (per option)

Add strings to `ModuleData/Languages/enlisted_strings.xml`.

---

## Content Variety Systems

These systems add replayability and immersion to events.

### 1. Result Text Variants

Instead of one `resultText`, provide an array. System picks randomly:

```json
{
  "id": "guard_strange_noise_investigate",
  "resultText": [
    "A stray dog, hunting rats. You return to your post.",
    "Just the wind. You feel foolish but alert.",
    "A drunk soldier, relieving himself. He stumbles off.",
    "Nothing. Shadows and nerves. The night is long."
  ]
}
```

**Parser behavior:** If `resultText` is array, pick random. If string, use directly.

### 2. Callback System (Flag-Based Recognition)

When player helps someone, set a flag. Later events check for it:

**Event 1 - Helping:**
```json
{
  "id": "firewood_helped_veteran",
  "options": [
    {
      "id": "help",
      "text": "Help him carry the load.",
      "effects": { "soldierRep": 2, "fatigue": 5 },
      "set_flags": ["helped_veteran_tormund"]
    }
  ]
}
```

**Event 2 - Callback (days/weeks later):**
```json
{
  "id": "patrol_veteran_returns_favor",
  "requirements": {
    "flags": ["helped_veteran_tormund"]
  },
  "setup": "The old veteran you helped with firewood falls in beside you. 'Heard you're on patrol. I'll walk with you a while. Watch your back.'",
  "options": [
    {
      "id": "accept",
      "text": "Appreciate it.",
      "effects": { "soldierRep": 3 },
      "resultText": "He spots trouble before you do. Good man to have around."
    }
  ]
}
```

**Common callback patterns:**
- `helped_*` → Ally appears later
- `reported_*` → Enemy remembers
- `spared_*` → Gratitude or resentment
- `witnessed_*` → Secret knowledge used later

### 3. Tier-Appropriate Language

Events should address player differently based on tier. Use tier requirements + different text:

**T1-T2 (Disposable grunt):**
```json
{
  "setup": "{SERGEANT} barely looks at you. 'You. New one. Get over there and don't touch anything.'",
  "requirements": { "tier": { "min": 1, "max": 2 } }
}
```

**T3-T4 (Known soldier):**
```json
{
  "setup": "{SERGEANT} nods as you approach. 'Good, someone who knows what they're doing. Take the west section.'",
  "requirements": { "tier": { "min": 3, "max": 4 } }
}
```

**T5-T6 (Respected veteran):**
```json
{
  "setup": "{SERGEANT} straightens slightly. 'Glad you're here, {PLAYER_RANK}. The new ones could learn from watching you.'",
  "requirements": { "tier": { "min": 5, "max": 6 } }
}
```

**Implementation:** Create tier variants of key events, or use conditional text blocks.

### 4. Comedic Failures

Not every failure is disaster. Some are just embarrassing:

**Minor Failures (Low stakes, high humor):**
```json
{
  "id": "patrol_shortcut_fail",
  "failure_resultText": [
    "You step in something. The smell follows you for hours. -3 Soldier Rep.",
    "The 'shortcut' leads to a dead end. Everyone sighs. You pretend it was intentional.",
    "You trip on a root. {COMRADE_NAME} snorts. 'Graceful.'",
    "A chicken attacks you. A CHICKEN. The men will never let you forget this."
  ],
  "effects": { "soldierRep": -2 }
}
```

**Serious Failures (Reserved for high-stakes events):**
```json
{
  "id": "scout_ambush_fail",
  "failure_resultText": "Arrows everywhere. You take one in the shoulder. Blood runs hot. You have to get out.",
  "effects": { "hpChange": -20, "medicalRisk": 2 }
}
```

**Guidelines:**
- T1-T3 basic orders: More comedic failures allowed
- T4-T6 specialist orders: Failures have real weight
- Combat/ambush events: Always serious
- Social events: Mix of embarrassing and serious

### 5. Tone Consistency Table

| Event Type | Success Tone | Failure Tone |
|------------|--------------|--------------|
| Guard/Sentry | Professional satisfaction | Mild embarrassment or real danger |
| Patrol | Camaraderie | Comedy or injury |
| Labor | Work done, tired | Accident or mockery |
| Scout | Intel gained, mission pride | Real danger, potential capture |
| Medical | Life saved, gratitude | Guilt, patient worsens |
| Leadership | Men respect you | Lost respect, morale hit |

---

**End of Document**
