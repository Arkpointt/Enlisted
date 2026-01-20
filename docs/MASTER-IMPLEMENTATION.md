# Master Implementation Plan: Order Prompt Model

**Summary:** Eliminate random event interrupts. Orders auto-assign, 85% of
phases are routine (nothing happens), 15% fire a mini-prompt asking player
if they want to engage. Both choices are gambles - investigate risks danger,
ignore risks getting caught. CK3-style event chains with consequences.

**Status:** 📋 Specification
**Created:** 2026-01-14
**Last Updated:** 2026-01-19
**Target Version:** Bannerlord v1.3.13

**Related Docs:**

- [CK3 Feast Analysis](ANEWFEATURE/ck3-feast-chain-analysis.md)
- [Writing Style Guide](Features/Content/writing-style-guide.md)
- [Event System Schemas](Features/Content/event-system-schemas.md)
- [Content Effects Reference](ANEWFEATURE/content-effects-reference.md)

---

## Table of Contents

- [Critical Implementation Requirements](#critical-implementation-requirements)
- [Core Vision](#core-vision)
- [Current Problems](#current-problems)
- [New Architecture](#new-architecture-the-order-prompt-model)
- [Duty System (Simplified)](#duty-system-simplified)
- [Decisions System](#decisions-system)
- [Dual-Risk Choice Framework](#dual-risk-choice-framework)
- [Event Chain System](#event-chain-system)
- [Implementation: Order Prompt System](#implementation-order-prompt-system)
- [Systems to Modify](#systems-to-modify)
- [Content Conversion Plan](#content-conversion-plan)
- [Testing Checklist](#testing-checklist)
- [New Files Checklist](#new-files-checklist)
- [Background Simulation Systems](#background-simulation-systems-autosim)
- [Orchestrator Removal Plan](#orchestrator-removal-plan)
- [JSON/XML File Cleanup](#jsonxml-file-cleanup)
- [Implementation Priority](#implementation-priority)
- [Map Incident Weighting](#map-incident-weighting-phase-4)
- [Campaign Context Tracking](#campaign-context-tracking-phase-4)
- [Data Structures](#data-structures)
- [Error Code Reference](#error-code-reference)
- [Appendix: CK3 Research Summary](#appendix-ck3-research-summary)

---

## Critical Implementation Requirements

**Before writing ANY code, you MUST:**

1. **Verify all APIs** against `Decompile/` in workspace root (NEVER online docs)
2. **Add new C# files** to `Enlisted.csproj` manually via `<Compile Include="..."/>`
3. **Register save types** in `EnlistedSaveDefiner` for any persisted classes/enums
4. **Use ModLogger** with error codes (format: `E-PROMPT-001`)
5. **Run validation** before committing: `python Tools/Validation/validate_content.py`
6. **Include tooltips** on all event options (cannot be null)
7. **Use `GiveGoldAction`** for gold changes (not `ChangeHeroGold`)

### APIs to Verify in Decompile/

Before implementing, verify these APIs exist in v1.3.13:

| API | Expected Location | Usage |
|:----|:------------------|:------|
| `MBRandom.RandomFloat` | TaleWorlds.Core | Probability rolls |
| `MBRandom.RandomFloatRanged` | TaleWorlds.Core | Delay range (1-4 hours) |
| `CampaignTime.Hours()` | TaleWorlds.CampaignSystem | Delayed consequences |
| `InformationManager.ShowInquiry` | TaleWorlds.Core | Prompt display |
| `InquiryData` | TaleWorlds.Core | Prompt structure |
| `Hero.MainHero.AddSkillXp` | TaleWorlds.CampaignSystem | Skill rewards |

### Save System Registration

Add to `EnlistedSaveDefiner.cs`:

```csharp
// New types for Order Prompt system
DefineEnumType(typeof(PromptOutcome));
DefineEnumType(typeof(DutyType));
DefineEnumType(typeof(CampaignContext));
DefineClassType(typeof(PendingConsequence));
DefineClassType(typeof(EventChainState));
DefineContainerType(typeof(List<PendingConsequence>));
DefineContainerType(typeof(List<EventChainState>));
```

---

## Core Vision

### The Problem We're Solving

Random event interrupts feel jarring at 2x speed. Events "happen to" the
player instead of the player choosing to engage.

### The Solution: Order Prompt Model

**How it works:**

```text
Order auto-assigned: "Guard Duty - 2 days"
  ↓
Phase progresses (Dawn/Midday/Dusk/Night)
  ↓
85% of phases: Routine - nothing happens
  "The watch continues uneventfully."
  ↓
15% of phases: PROMPT fires
  "You hear rustling in the bushes."
  [Investigate] [Stay Focused]
  ↓
Player clicks [Investigate]: Outcome fires (pre-rolled)
  → 50% nothing / 30% reward / 15% chain / 5% danger
  ↓
Player clicks [Stay Focused]: ALSO a gamble!
  → 85% nothing / 15% "caught slacking" follow-up
  → Fined, lord rep loss, scrutiny
```

**Key principles:**

1. **Orders are auto-assigned** - realistic for a soldier
2. **Most duty is routine** - 85% nothing happens (like real military life)
3. **Player chooses to engage** - prompts ask permission before events fire
4. **Both choices are gambles** - investigate risks danger, ignore risks getting caught
5. **Consequences follow choices** - CK3-style chains with outcomes
6. **No random interrupts** - events only fire when player engages

### CK3 Research Insight

CK3 fires very few random events during normal play:

- **Yearly events:** 25% chance (`chance_to_happen = 25`)
- **Event pools:** 500-1000 weight for "nothing" vs 50-100 for events
- **Result:** 3-5 random events per YEAR for players

The drama comes from **activities** (feasts, hunts) where events ARE
frequent because the player initiated them.

**Your orders = CK3 activities.** Make them event-rich via prompts, but
keep idle time quiet.

---

## Current Problems

### Five Overlapping Content Systems

The mod currently has multiple content delivery systems that overlap and
cause confusion:

| System | File | Behavior | Prob |
| :--- | :--- | :--- | :--- |
| Decisions | Manager.cs | Hub choices | OK |
| Order Evt | Behavior.cs | Slot phases | Needs prompt |
| Narrative | Manager.cs | Random spam | **PROBLEM** |
| Map Inc | Incident.cs | Battle/Set | Contextual |
| Camp Opp | Orchestrator.cs | Pre-sched | Complex |

### The Core Problem: EventPacingManager.TryFireEvent()

```csharp
// EventPacingManager.cs line 133-176
private void TryFireEvent(EscalationState escalationState)
{
    // This fires random narrative events every 3-5 days
    // Player has NO CHOICE - event just interrupts them
    var selectedEvent = EventSelector.SelectEvent(worldSituation);
    deliveryManager.QueueEvent(selectedEvent); // POPUP!
}
```

This is the random event spam. It needs to be **deleted**.

### What Players Experience Now

1. Playing game at 2x speed (1 day = ~1 minute)
2. Random popup: "A soldier approaches you..." (didn't ask for this)
3. Another random popup: "You overhear rumors..." (interruption)
4. Order event fires: "Strange noise during guard duty" (OK, contextual)
5. More random popups...

**Result:** Player feels bombarded by content they didn't choose.

---

## New Architecture: The Order Prompt Model

### Design Philosophy

Inspired by CK3's feast event chains:

- **Player chooses to engage** (via prompts during orders)
- **Outcomes are pre-rolled** (hidden setup before player sees choices)
- **Chains create narrative arcs** (not isolated random events)
- **85% routine, 15% interesting** (CK3's weighted "nothing" approach)

### The Flow

```text
Order auto-assigned → Player works through phases →
85% nothing / 15% prompt fires

[Prompt Example]
┌─────────────────────────────────────┐
│ SOMETHING STIRS                     │
│                                     │
│ You hear rustling in the bushes     │
│ nearby. Could be nothing. Could be  │
│ trouble.                            │
│                                     │
│ [Investigate]  [Stay Focused]       │
└─────────────────────────────────────┘

Player clicks [Investigate] → Event chain fires (outcome was pre-rolled)
  → 50% nothing / 30% reward / 15% chain / 5% danger

Player clicks [Stay Focused] → ALSO a gamble (order-type dependent)
  → Guard/Patrol: 20% "caught slacking" follow-up
  → Foraging: 5% follow-up (nobody expects you to chase noises)
  → Escort/March: 0% (staying in formation is correct)
```

---

## Duty System (Simplified)

### 4 Core Duties

The previous 17-order system was overcomplicated. Consolidating to 4 duties:

| Duty | Description | Tier Availability |
|:-----|:------------|:------------------|
| **Patrol** | Walk the perimeter, check routes, sweep areas | T3-9 |
| **Guard** | Stand watch at a post, gates, or camp entrance | T1-9 |
| **Sentry** | Observation post - hilltop, masthead, tower | T2-9 |
| **Camp Labor** | Firewood, latrine, equipment, errands | T1-4 |

### Design Principles

1. **Duties are assigned** - The army tells you what to do
2. **Events happen within duties** - Prompts fire during duty phases
3. **Same duties, different events by tier** - A T1 on Guard sees different prompts than a T7
4. **Sea variants built-in** - Each duty has land/sea context variants

### Tier Gating Rationale

- **Camp Labor (T1-4):** Grunt work. Officers don't haul firewood.
- **Sentry (T2-9):** Requires some trust. Brand new recruits don't get lookout posts.
- **Patrol (T3-9):** Requires initiative. You need to know the army before you walk its perimeter.
- **Guard (T1-9):** Universal. Everyone stands watch.

### Sea Context Variants

| Duty | Land | Sea |
|:-----|:-----|:----|
| Patrol | Camp Patrol | Hull Inspection |
| Guard | Guard Duty | Deck Watch |
| Sentry | Sentry Post | Masthead Watch |
| Camp Labor | Firewood/Latrine | Deck Scrubbing/Cargo |

### Event Scaling by Tier

Same duty, different events based on rank:

**Guard Duty - T1 (Recruit):**
> "You hear footsteps behind the supply tent."
> [Investigate] [Stay at Post]

**Guard Duty - T7 (Officer):**
> "The corporal reports suspicious movement near the north picket."
> [Go Check It Yourself] [Send a Patrol]

The duty is the same. The player's role in the event changes.

---

## Decisions System

### Free Time Activities

When not on duty, the player chooses how to spend their time. These are
**Decisions** - player-initiated activities with their own event chains.

| Decision | Cost | Reward | Risk |
|:---------|:-----|:-------|:-----|
| **Hunt** | Leave camp | Food, skill XP | Injury, getting lost |
| **Gamble** | Gold stake | Win gold | Lose gold, fights |
| **Drink** | Gold | Social events, rumors | Trouble |
| **Train** | Time | Skill XP | None |

### Key Distinction: Duties vs Decisions

- **Duties** = Assigned by the army. You don't choose.
- **Decisions** = Your choice during free time. You pick the activity.

### Decision Chains

Each decision has its own chain file with text-based events:

```
ModuleData/Enlisted/Chains/
├── hunt_chains.json          # Tracking, wildlife, getting lost
├── gamble_chains.json        # Card games, dice, accusations
├── drink_chains.json         # Tavern events, rumors, fights
├── train_chains.json         # Practice, sparring, lessons
```

### Example: Hunt Decision

```json
// Chains/hunt_chains.json
{
  "schemaVersion": 1,
  "decision": "hunt",
  "chains": [
    {
      "id": "hunt_deer_tracks",
      "name": "Deer Tracks",
      "contexts": ["land"],

      "trigger": {
        "title": "Fresh Tracks",
        "description": "You spot deer tracks leading into a thicket.",
        "option_a": "Follow Quietly",
        "option_b": "Try a Different Area"
      },

      "branches": {
        "follow": {
          "skill_check": {
            "skill": "Athletics",
            "threshold": 30,
            "pass_branch": "successful_hunt",
            "fail_branch": "deer_escapes"
          }
        }
      },

      "outcomes": {
        "successful_hunt": {
          "text": "You bring down a young buck. Meat for the camp tonight.",
          "effects": {
            "food_item": "venison",
            "skill_xp": { "Athletics": 20 },
            "lord_reputation": 1
          }
        },
        "deer_escapes": {
          "text": "A branch snaps underfoot. The deer bolts. Empty-handed.",
          "effects": {
            "skill_xp": { "Athletics": 5 }
          }
        }
      }
    }
  ]
}
```

### Example: Gamble Decision

```json
{
  "id": "gamble_dice_game",
  "decision": "gamble",
  "trigger": {
    "title": "Dice Game",
    "description": "Three soldiers wave you over. 'Got coin? We're playing bones.'",
    "option_a": "Join (Stake 20 gold)",
    "option_b": "Watch"
  },
  "outcomes": {
    "win_big": {
      "weight": 20,
      "text": "Lady luck smiles. You walk away with heavy pockets.",
      "effects": { "gold": 50 }
    },
    "win_small": {
      "weight": 25,
      "text": "A modest win. Better than nothing.",
      "effects": { "gold": 15 }
    },
    "lose": {
      "weight": 40,
      "text": "The dice hate you tonight. Your stake is gone.",
      "effects": { "gold": -20 }
    },
    "accused_cheating": {
      "weight": 15,
      "text": "A soldier grabs your wrist. 'Those dice feel wrong to me.'",
      "effects": { "gold": -20 },
      "follow_up_branch": "cheating_accusation"
    }
  }
}
```

---

## Dual-Risk Choice Framework

### The Core Insight: Neither Choice is Safe

The original spec made [Ignore] a "safe" option with no downside. This removes
tension. Instead, **both choices should be gambles**:

| Choice | Upside | Downside |
|:--|:--|:--|
| Investigate | Gold, XP, items, story | Injury, sickness, ambush, fatigue |
| Ignore | Avoid personal danger | Caught slacking: fined, lord rep loss, scrutiny |

**The player weighs:** "Do I risk getting hurt, or risk getting caught?"

### Investigate Outcomes (Pre-Rolled)

When player clicks [Investigate], outcome was already determined:

| Outcome | Weight | Examples |
|:--|:--|:--|
| Nothing | 50% | "Just the wind." / "A rabbit bolts." |
| Small Reward | 30% | Gold (25-75), supplies, skill XP |
| Event Chain | 15% | Deserter encounter, wounded soldier |
| Danger | 5% | Ambush, injury, sickness |

### Ignore Outcomes (Duty-Type Dependent)

When player clicks [Ignore], a **follow-up check** runs based on duty type:

| Duty | Catch Chance | Rationale |
|:--|:--|:--|
| **Guard** | 20% | Investigating IS your job |
| **Sentry** | 20% | You're meant to spot AND report things |
| **Patrol** | 15% | You should check things, but you're also covering ground |
| **Camp Labor** | 0% | Not your job to chase noises while hauling firewood |
| **Off Duty** | 0% | You're literally off duty - no expectations |

### "Caught Slacking" Follow-Up Events

If the ignore check fails, a follow-up event fires (delayed 1-4 hours):

```json
// ignore_consequences.json
{
  "schemaVersion": 1,
  "category": "ignore_consequence",
  "events": [
    {
      "id": "caught_slacking_tracks",
      "titleId": "ignore_tracks_title",
      "title": "Tracks Found",
      "setupId": "ignore_tracks_setup",
      "setup": "The sergeant finds footprints near your post. 'You didn't think to check these?' He's not happy.",
      "tooltip": "Fined 15 gold. -2 Lord rep. +1 Scrutiny.",
      "valid_duty_types": ["guard", "sentry", "patrol"],
      "effects": {
        "gold": -15,
        "lord_reputation": -2,
        "scrutiny": 1
      }
    },
    {
      "id": "caught_slacking_theft",
      "titleId": "ignore_theft_title",
      "title": "Missing Supplies",
      "setupId": "ignore_theft_setup",
      "setup": "Supplies went missing during your watch. Whether you saw something or not, it's on you.",
      "tooltip": "Fined 25 gold. -3 Lord rep.",
      "valid_duty_types": ["guard", "sentry", "patrol"],
      "effects": {
        "gold": -25,
        "lord_reputation": -3
      }
    },
    {
      "id": "caught_slacking_ambush",
      "titleId": "ignore_ambush_title",
      "title": "Preventable Ambush",
      "setupId": "ignore_ambush_setup",
      "setup": "Bandits hit the rear column. Some say you ignored warning signs. Two men dead.",
      "tooltip": "-5 Lord rep. 2 party casualties. +2 Scrutiny.",
      "valid_duty_types": ["guard", "sentry", "patrol"],
      "effects": {
        "lord_reputation": -5,
        "party_casualties": 2,
        "scrutiny": 2
      }
    },
    {
      "id": "caught_slacking_minor",
      "titleId": "ignore_minor_title",
      "title": "Noted",
      "setupId": "ignore_minor_setup",
      "setup": "The corporal gives you a look. He saw you ignore something. Nothing said, but he'll remember.",
      "tooltip": "+1 Scrutiny.",
      "valid_duty_types": ["guard", "sentry", "patrol"],
      "effects": {
        "scrutiny": 1
      }
    }
  ]
}
```

### Actual Stakes in Enlisted

**Available positive outcomes:**

- Gold (via `GiveGoldAction.ApplyBetweenCharacters`)
- Skill XP (thematic to order type)
- Lord reputation
- Items/equipment (rare)
- Narrative satisfaction

**Available negative outcomes:**

- Gold loss (fines)
- Lord reputation loss
- Party loses men (`party_casualties`)
- Player injury/sickness
- Scrutiny increase

---

## Event Chain System

### Organization: Context-Based (CK3 Pattern)

CK3 organizes events by **what you're doing**, not by theme:
- "You're at a feast" → feast chains available
- "You're on a hunt" → hunt chains available

Enlisted follows the same pattern with duties and decisions:

```
ModuleData/Enlisted/Chains/
# Duty chains (assigned work)
├── patrol_chains.json        # Caves, abandoned camps, travelers, wildlife
├── guard_chains.json         # Gate incidents, drunk soldiers, thieves
├── sentry_chains.json        # Distant signals, approaching parties
├── camp_labor_chains.json    # Found items, accidents, shortcuts
# Decision chains (free time activities)
├── hunt_chains.json          # Tracking, wildlife, getting lost
├── gamble_chains.json        # Card games, dice, accusations
├── drink_chains.json         # Tavern events, rumors, fights
├── train_chains.json         # Practice, sparring, lessons
```

Duty chains live in files for their **primary duty** but can specify multiple
valid duties via the `duty_types` array. Decision chains are tied to a single
decision type.

### Chain Structure

Every chain has three layers:

| Layer | Purpose | Example |
|:------|:--------|:--------|
| **Trigger** | Initial prompt during duty | "You spot a cave entrance" |
| **Branch** | What happens based on choice + skill | "You find a deserter" or "You fall into a trap" |
| **Outcome** | Final resolution with rewards/penalties | +50 gold, +2 lord rep |

```
PATROL (duty assignment)
  └─ "You spot a cave entrance" (TRIGGER)
       ├─ [Investigate]
       │    └─ Skill check (Scouting 40)
       │         ├─ PASS → "You find a deserter hiding" (BRANCH)
       │         │    ├─ [Turn him in] → +gold, +lord rep (OUTCOME)
       │         │    └─ [Let him go] → -scrutiny (OUTCOME)
       │         └─ FAIL → "You stumble into a trap" (BRANCH)
       │              └─ HP loss, limp back (OUTCOME)
       └─ [Ignore]
           └─ 15% catch check → "Sergeant finds tracks" (CONSEQUENCE)
```

### Reward Types

Outcomes can grant any combination of:

| Reward | Examples | Notes |
|:-------|:---------|:------|
| **Gold** | 25-150 denars | Scaled by tier |
| **Renown** | 1-5 points | Rare, significant finds only |
| **Lord Reputation** | 1-5 points | Reporting valuable intel |
| **Settlement Relations** | +2-5 with nearby village/town | Helping locals, returning lost goods |
| **Skill XP** | 10-50 XP to relevant skill | Scouting, Medicine, Leadership, etc. |
| **Food Item** | Grain, meat, fish, etc. | Foraging, hunting finds |
| **Equipment** | Weapon, armor, trade goods | Rare - abandoned camps, dead travelers |
| **Company Supplies** | +1-5 supply units | Found caches, successful foraging |

### Penalty Types

Danger outcomes and consequences can inflict:

| Penalty | Examples | Notes |
|:-------|:---------|:------|
| **HP Loss** | 10-40 HP | Traps, ambushes, falls |
| **Gold Loss** | 15-50 denars | Fines for negligence |
| **Reputation Loss** | -1 to -5 | Failed duties, caught slacking |
| **Scrutiny** | +1-3 points | Drawing officer attention |
| **Injury** | Sprained ankle, cut, bruise | Temporary debuff |
| **Sickness** | Food poisoning, fever | From bad foraging |
| **Party Casualties** | 1-3 troops | Severe negligence only |

### JSON Schema: Event Chain

```json
// Chains/patrol_chains.json
{
  "schemaVersion": 1,
  "chains": [
    {
      "id": "cave_deserter",
      "name": "The Cave Deserter",
      "duty_types": ["patrol"],
      "contexts": ["land"],
      "tier_range": [3, 9],

      "trigger": {
        "title": "Cave Entrance",
        "description": "You spot a dark opening in the hillside. Fresh footprints lead inside.",
        "investigate_text": "Enter the Cave",
        "ignore_text": "Mark It and Move On"
      },

      "investigate": {
        "skill_check": {
          "skill": "Scouting",
          "threshold": 40,
          "pass_branch": "deserter_found",
          "fail_branch": "trap_sprung"
        }
      },

      "branches": {
        "deserter_found": {
          "title": "A Desperate Man",
          "description": "A ragged soldier cowers in the darkness. A deserter. He begs you not to turn him in.",
          "choices": [
            {
              "text": "Turn him in to the officers",
              "outcome": "turned_in"
            },
            {
              "text": "Let him go",
              "outcome": "released"
            },
            {
              "text": "Give him food and directions",
              "requirements": { "trait": "Mercy", "min": 20 },
              "outcome": "helped"
            }
          ]
        },
        "trap_sprung": {
          "title": "Ambush!",
          "description": "You step inside and the ground gives way. A pit trap.",
          "auto_outcome": "trap_injury"
        }
      },

      "outcomes": {
        "turned_in": {
          "text": "The officers thank you. The deserter is dragged away screaming.",
          "effects": {
            "gold": 30,
            "lord_reputation": 3,
            "skill_xp": { "Leadership": 15 }
          }
        },
        "released": {
          "text": "He vanishes into the hills. You wonder if he'll make it.",
          "effects": {
            "scrutiny": 1,
            "skill_xp": { "Charm": 10 }
          }
        },
        "helped": {
          "text": "You share your rations and point him toward the border.",
          "effects": {
            "trait_xp": { "Mercy": 15 },
            "skill_xp": { "Charm": 20 }
          },
          "follow_up": {
            "chance": 0.25,
            "delay_days": [14, 30],
            "chain_id": "deserter_returns"
          }
        },
        "trap_injury": {
          "text": "You haul yourself out, ankle throbbing. Stupid mistake.",
          "effects": {
            "hp_loss": 25,
            "injury": "sprained_ankle"
          }
        }
      }
    }
  ]
}
```

### MVP: 3 Chains Per File

Start with 3 chains per file to test the system. Expand after validation.

**patrol_chains.json (3 chains):**
1. Cave Entrance - deserter hiding, pit trap, empty cave
2. Abandoned Campsite - loot, ambush, bodies with clues
3. Tracks in the Mud - follow to merchant, bandit, or nothing

**guard_chains.json (3 chains):**
1. Drunk Soldier - let him pass, report him, help him to bed
2. Suspicious Visitor - challenge, escort to captain, let pass
3. Noise Behind Tents - thief, animal, nothing

**sentry_chains.json (3 chains):**
1. Smoke on Horizon - report it, investigate, ignore
2. Approaching Riders - friendly scouts, merchants, enemy patrol
3. Strange Light - signal fire, campfire, will-o-wisp (nothing)

**camp_labor_chains.json (3 chains):**
1. Found Coins - keep them, turn them in, split with buddy
2. Shortcut Option - risky path (injury chance), safe path (slower)
3. Buried Cache - food supplies, rusted weapons, empty hole

### Decision Chains (MVP: 3 each)

**hunt_chains.json (3 chains):**
1. Deer Tracks - skill check to bring down game
2. Lost in Woods - find way back or wander further
3. Wolf Encounter - fight, flee, or scare off

**gamble_chains.json (3 chains):**
1. Dice Game - win/lose/accused of cheating
2. Card Game - high stakes variant
3. Arm Wrestling - strength check, side bets

**drink_chains.json (3 chains):**
1. Tavern Rumors - hear useful intel or gossip
2. Drinking Contest - impress soldiers or embarrass yourself
3. Bar Fight - intervene, join in, slip away

**train_chains.json (3 chains):**
1. Sparring Partner - improve combat skills
2. Veteran's Lesson - learn specific technique
3. Target Practice - bow/crossbow skill XP

### Shared Chains

Some triggers make sense across multiple duties. Use `duty_types` array:

```json
{
  "id": "suspicious_sound",
  "duty_types": ["guard", "sentry", "patrol"],
  "contexts": ["land"],
  "trigger": {
    "title": "Something Stirs",
    "description": "You hear rustling nearby. Could be nothing.",
    "investigate_text": "Investigate",
    "ignore_text": "Stay Alert"
  }
  // ... branches and outcomes
}
```

This chain lives in one file but fires for guard, sentry, or patrol duties.

---

### Skill-Gated Information (Optional Enhancement)

Higher skills can reveal information in tooltips, helping informed decisions:

```
"You hear rustling in the bushes."

[Investigate]
  - Low Scouting: "Could be anything."
  - Mid Scouting (25+): "Sounds like a single person."
  - High Scouting (50+): "Wounded man, alone, no weapons visible."

[Stay at Post]
  - Shows: "Risk: Sergeant might check your area later."
```

This rewards skill investment without gating content.

### Implementation: Ignore Check Logic

```csharp
private void HandleIgnoreChoice(Order order)
{
    // Get catch chance based on order type
    float catchChance = GetIgnoreCatchChance(order.Type);

    if (catchChance <= 0f)
    {
        // No risk for this order type (escort, march, training)
        return;
    }

    // Roll for "caught slacking"
    if (MBRandom.RandomFloat < catchChance)
    {
        // Schedule follow-up event (1-4 hours later)
        float delayHours = MBRandom.RandomFloatRanged(1f, 4f);
        ScheduleIgnoreConsequence(order, delayHours);

        ModLogger.Debug(LogCategory,
            $"[E-PROMPT-010] Ignore consequence scheduled for {order.Type} in {delayHours:F1}h");
    }
}

private float GetIgnoreCatchChance(DutyType dutyType)
{
    return dutyType switch
    {
        DutyType.Guard => 0.20f,
        DutyType.Sentry => 0.20f,
        DutyType.Patrol => 0.15f,
        DutyType.CampLabor => 0f,
        DutyType.OffDuty => 0f,
        _ => 0.10f
    };
}

private void ScheduleIgnoreConsequence(Order order, float delayHours)
{
    var consequence = IgnoreConsequenceCatalog.GetRandomConsequence(order.Type);

    // Use CampaignTime for delayed event
    var fireTime = CampaignTime.Now + CampaignTime.Hours(delayHours);

    // Queue the consequence event
    _pendingIgnoreConsequences.Add(new PendingConsequence
    {
        FireTime = fireTime,
        ConsequenceId = consequence.Id,
        OrderType = order.Type
    });
}
```

### Data Structure: Ignore Consequences

```csharp
// src/Features/Orders/Models/IgnoreConsequence.cs
public class IgnoreConsequence
{
    public string Id { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public List<OrderType> ValidOrderTypes { get; set; } = new();
    public Dictionary<string, object> Effects { get; set; } = new();
}
```

### Data Structure: Pending Consequence

```csharp
// src/Features/Orders/Models/PendingConsequence.cs
public class PendingConsequence
{
    [SaveableField(1)]
    public CampaignTime FireTime { get; set; }

    [SaveableField(2)]
    public string ConsequenceId { get; set; }

    [SaveableField(3)]
    public DutyType DutyType { get; set; }
}
```

### What Gets Removed

| System | File | Change |
| :--- | :--- | :--- |
| OrderProg | Behavior.cs | Phase logic |
| EventPac | Manager.cs | Remove spam |
| Config | config.json | Remove pacing |
| Data | orders.json | Add prompts |

### What Gets Kept

| System | File | Notes |
| :--- | :--- | :--- |
| Orders | orders.json | Auto-assigned |
| Decisions | decisions.json | Camp Hub menu |
| Map Inc | incidents.json | Battle/Set |
| Threshold | events.json | Supply surge |

---

## Implementation: Order Prompt System

### Core Logic (OrderProgressionBehavior.cs)

```csharp
// During each slot phase transition
private void ProcessSlotPhase(Order currentOrder)
{
    // 85% of the time: routine, nothing happens
    var roll = MBRandom.RandomFloat;
    if (roll >= 0.15f)
    {
        // Silent phase - order progresses normally
        return;
    }

    // 15% of the time: show a prompt
    ShowOrderPrompt(currentOrder);
}

private void ShowOrderPrompt(Order order)
{
    // PRE-ROLL the outcome (CK3 pattern)
    var outcome = RollPromptOutcome(order);

    // Get contextual prompt text based on order type AND travel context
    // (land/sea)
    var isAtSea = IsPartyAtSea();
    var prompt = GetPromptForOrder(order, isAtSea);

    InquiryData inquiry = new InquiryData(
        prompt.Title,           // "Something Stirs"
        prompt.Description,     // "You hear rustling in the bushes..."
                                // (land) or "Strange shadow in the
                                // water..." (sea)
        true, true,
        prompt.InvestigateText, // "Investigate" / "Check It Out" / etc.
        prompt.IgnoreText,      // "Stay Focused" / "Ignore It" / etc.
        () => FireEventChain(order, outcome),  // Player engages
        () => { /* Nothing - order continues */ }  // Player ignores
    );

    InformationManager.ShowInquiry(inquiry);
}

private OrderPrompt GetPromptForOrder(Order order, bool isAtSea)
{
    // Get prompts for this order type
    var prompts = PromptCatalog.GetPromptsForOrderType(order.Id);

    // Filter by travel context - only show contextually appropriate prompts
    var contextualPrompts = prompts.Where(p =>
        p.Contexts.Contains("any") ||
        (isAtSea && p.Contexts.Contains("sea")) ||
        (!isAtSea && p.Contexts.Contains("land"))
    ).ToList();

    // Pick random contextual prompt
    return contextualPrompts[MBRandom.RandomInt(contextualPrompts.Count)];
}

private PromptOutcome RollPromptOutcome(Order order)
{
    // CK3-style weighted random list
    // Outcome is determined BEFORE player sees prompt
    var roll = MBRandom.RandomFloat;

    // Example weights (varies by order type):
    // 50% = nothing interesting
    // 30% = small reward (gold, item, reputation)
    // 15% = interesting event chain
    // 5%  = danger (ambush, injury)

    if (roll < 0.50f) return PromptOutcome.Nothing;
    if (roll < 0.80f) return PromptOutcome.SmallReward;
    if (roll < 0.95f) return PromptOutcome.EventChain;
    return PromptOutcome.Danger;
}
```

### Prompt Templates by Order Type

**IMPORTANT: Context Filtering**
Prompts must be contextually appropriate. "Rustling in bushes"
makes no sense at sea. Use `contexts` field to filter:

- `["land"]` = Only fires on land (bushes, treeline, campsite)
- `["sea"]` = Only fires at sea (hull, rigging, waves, hold)
- `["any"]` = Fires in both contexts (universal prompts)

**Skill Rewards by Order Type:**
Outcomes should award skills thematically appropriate to each order:

- **Treat Wounded** → Medicine
- **Lead Patrol** → Leadership, Tactics
- **Training Recruits** → Leadership, combat skills (OneHanded, Bow, etc.)

```json
// order_prompts.json (NEW FILE)
{
  "schemaVersion": 1,
  "prompts": [
    {
      "duty_types": ["guard", "sentry", "patrol"],
      "prompts": [
        {
          "title": "Something Stirs",
          "description": "You hear rustling in the bushes nearby. Could be nothing.
            Could be trouble.",
          "investigate_text": "Investigate",
          "ignore_text": "Stay at Post",
          "contexts": ["land"]
        },
        {
          "title": "Distant Noise",
          "description": "A sound carries from the treeline. Too faint to
            identify.",
          "investigate_text": "Check It Out",
          "ignore_text": "Ignore It",
          "contexts": ["land"]
        },
        {
          "title": "Strange Light",
          "description": "A brief flicker of light in the darkness. Probably a
            firefly. Probably.",
          "investigate_text": "Move Closer",
          "ignore_text": "Keep Watch",
          "contexts": ["land"]
        },
        {
          "title": "Shadow Below",
          "description": "Something large moves beneath the hull. Too big to be
            a fish.",
          "investigate_text": "Look Closer",
          "ignore_text": "Keep Watch",
          "contexts": ["sea"]
        },
        {
          "title": "Loose Rope",
          "description": "A rope swings free in the rigging. Cut loose, or just
            poorly tied?",
          "investigate_text": "Investigate",
          "ignore_text": "Report It Later",
          "contexts": ["sea"]
        },
        {
          "title": "Strange Sound",
          "description": "Unusual creaking from the hold below. Could be cargo
            shifting. Could be something else.",
          "investigate_text": "Check Below",
          "ignore_text": "Stay at Post",
          "contexts": ["sea"]
        }
      ]
    },
    {
      "duty_types": ["patrol", "camp_labor"],
      "prompts": [
        {
          "title": "Off the Path",
          "description": "You spot what might be an old campsite through the
            brush. Worth investigating?",
          "investigate_text": "Search the Area",
          "ignore_text": "Stay on Task",
          "contexts": ["land"]
        },
        {
          "title": "Bodies Ahead",
          "description": "You come across bodies in the field. Recent, from the
            look of them.",
          "investigate_text": "Check Them",
          "ignore_text": "Move On",
          "contexts": ["land"]
        },
        {
          "title": "Floating Debris",
          "description": "Wreckage floats nearby. Could be from a merchant ship.
            Could be bait.",
          "investigate_text": "Investigate",
          "ignore_text": "Sail On",
          "contexts": ["sea"]
        }
      ]
    },
    {
      "duty_types": ["patrol", "sentry"],
      "prompts": [
        {
          "title": "Smoke on the Horizon",
          "description": "A thin column of smoke rises in the distance. Not on
            your planned route.",
          "investigate_text": "Investigate",
          "ignore_text": "Continue Mission",
          "contexts": ["land", "sea"]
        },
        {
          "title": "Fresh Tracks",
          "description": "Horse tracks, headed away from the main road. Could be
            nothing.",
          "investigate_text": "Follow Them",
          "ignore_text": "Mark and Report",
          "contexts": ["land"]
        },
        {
          "title": "Distant Sail",
          "description": "A sail appears on the horizon. Flying colors you don't
            recognize.",
          "investigate_text": "Close Distance",
          "ignore_text": "Note and Continue",
          "contexts": ["sea"]
        }
      ]
    }
  ]
}
```

### Event Chain Outcomes

```json
// prompt_outcomes.json (NEW FILE)
{
  "schemaVersion": 1,
  "outcomes": {
    "nothing": [
      {
        "text": "Just the wind. Nothing there.",
        "effects": {}
      },
      {
        "text": "A rabbit bolts from the underbrush. False alarm.",
        "effects": {}
      },
      {
        "text": "You find nothing of interest. Time wasted, but at least you
          were thorough.",
        "effects": { "fatigue": 2, "skillXp": { "Perception": 5 } }
      }
    ],
    "small_reward": [
      {
        "text": "You find a coin purse dropped by a careless traveler.",
        "effects": { "gold": "25-75", "skillXp": { "Perception": 12 } }
      },
      {
        "text": "An abandoned pack contains useful supplies.",
        "effects": { "company_supplies": 3, "skillXp": { "Perception": 10 } }
      },
      {
        "text": "Your lord notices your diligence. Word gets around.",
        "effects": { "lord_reputation": 2, "skillXp": { "Perception": 8 } }
      }
    ],
    "event_chain": [
      {
        "chain_id": "deserter_encounter",
        "text": "You find a deserter from another company, hiding in the
          brush...",
        "trigger_event": "evt_deserter_chain_start"
      },
      {
        "chain_id": "wounded_soldier",
        "text": "A wounded soldier lies hidden in the grass, barely alive...",
        "trigger_event": "evt_wounded_soldier_start"
      }
    ],
    "danger": [
      {
        "text": "Ambush! Bandits were waiting in the bushes!",
        "effects": { "trigger_combat": "bandit_ambush_small",
                     "skillXp": { "OneHanded": 10 } }
      },
      {
        "text": "You step into a concealed pit. Your ankle twists painfully.",
        "effects": { "injury": "sprained_ankle", "fatigue": 15 }
      }
    ]
  }
}
```

**Skill XP Integration:**
Outcomes should reward contextually appropriate skills using thematic aliases:

- **Guard/Patrol orders** → `"Perception"` (maps to Scouting → Cunning)
- **Combat outcomes** → `"OneHanded"`, `"TwoHanded"`, `"Polearm"`
    (maps to Vigor skills)
- **Foraging/Supply** → `"Perception"` for finding things (Scouting)
- **Equipment repair** → `"Smithing"` (maps to Crafting → Endurance)
- **Mounted patrol** → `"Riding"` (Endurance)
- **Leading situations** → `"Leadership"` (Social)
- **Medical events** → `"Medicine"` (Intelligence)

See [Content Effects Reference](ANEWFEATURE/content-effects-reference.md) for
skill XP effect format and [Native Skill XP](ANEWFEATURE/native-skill-xp.md)
as a reference for skill names and mapping. Use thematic aliases
like "Perception" to reward specific skills.

### Event Chain Design (CK3 Pattern)

```json
// event_chains.json (NEW FILE)
{
  "chains": [
    {
      "id": "deserter_encounter",
      "name": "The Deserter",
      "phases": [
        {
          "phase": 1,
          "event_id": "evt_deserter_chain_start",
          "title": "A Desperate Man",
          "description": "The man begs you not to turn him in. He says the
            officers beat him, that he couldn't take another day. He's thin,
            exhausted, terrified.",
          "choices": [
            {
              "text": "Let him go",
              "next_phase": 2,
              "flag": "showed_mercy"
            },
            {
              "text": "Take him to the officers",
              "next_phase": 3,
              "flag": "turned_in"
            },
            {
              "text": "Give him food and directions away from camp",
              "next_phase": 2,
              "flag": "helped_escape",
              "requirements": { "lord_reputation": 20 }
            }
          ]
        },
        {
          "phase": 2,
          "event_id": "evt_deserter_mercy",
          "condition": "showed_mercy OR helped_escape",
          "title": "Gone",
          "description": "He vanishes into the night. You wonder if you'll ever
            see him again.",
          "delay_days": "7-14",
          "follow_up": {
            "chance": 0.3,
            "event_id": "evt_deserter_returns",
            "condition": "helped_escape"
          },
          "immediate_effects": {
            "scrutiny": 1,
            "skillXp": { "Charm": 10 }
          }
        },
        {
          "phase": 3,
          "event_id": "evt_deserter_turned_in",
          "condition": "turned_in",
          "title": "Justice",
          "description": "Your lord thanks you for your diligence. The
            deserter is hauled away. You try not to hear him screaming.",
          "immediate_effects": {
            "lord_reputation": 5,
            "gold": 15,
            "skillXp": { "Leadership": 8 }
          }
        }
      ]
    }
  ]
}
```

### CK3 Probability Model Applied

From CK3's feast events analysis:

- `random_list { 500 = { nothing } 50 = { actual_event } }` common pattern
  - Result: Events feel rare and special, not spammy
- Hidden setup events pre-roll outcomes
- Delayed follow-ups create anticipation

**Applied to Enlisted:**

```text
Phase Transition Check:
├── 85% → Routine (silent, order continues)
└── 15% → Prompt fires
        ├── Player clicks [Ignore] → Nothing
        └── Player clicks [Investigate] → Pre-rolled outcome
                ├── 50% → Nothing interesting
                ├── 30% → Small reward
                ├── 15% → Event chain starts
                └── 5%  → Danger
```

**Net result:** ~2-3% of phases lead to actual interesting content.
With 4 phases/day, that's roughly 1 interesting event every 8-12 days.
Much closer to CK3's pacing than current spam.

---

## Systems to Modify

### OrderProgressionBehavior.cs Changes

**Current (lines ~89-120):**

```csharp
private void ProcessSlotPhase(Order order)
{
    // Fires order events randomly
    if (ShouldFireOrderEvent(order))
    {
        var evt = SelectOrderEvent(order);
        deliveryManager.QueueEvent(evt);
    }
}
```

**New:**

```csharp
private void ProcessSlotPhase(Order order)
{
    // 85% routine - no prompt
    if (MBRandom.RandomFloat >= 0.15f)
        return;
    
    // 15% - show player a prompt
    ShowOrderPrompt(order);
}
```

### EventPacingManager.cs Changes

#### DELETE entire TryFireEvent() method (lines 133-176)

This is the random event spam. Remove it entirely.

### enlisted_config.json Changes

#### Remove

```json
"decision_events": {
  "pacing": {
    "event_window_min_days": 3,
    "event_window_max_days": 5
  }
}
```

**Add:**

```json
"order_prompts": {
  "prompt_chance": 0.15,
  "outcome_weights": {
    "nothing": 50,
    "small_reward": 30,
    "event_chain": 15,
    "danger": 5
  }
}
```

---

## Content Conversion Plan

### Converting 65 Context Events → Chain Outcomes

The 65 "Context Events" should be converted:

| Category | Count | Conversion |
| :--- | :--- | :--- |
| Social | 18 | → Camp Decisions |
| Disc | 12 | → Outcomes |
| Danger | 15 | → Outcomes |
| Story | 20 | → Event chains |

**Preserve skill XP:** Existing events that award skill XP should retain
those rewards in their converted form.

### Order Events (84) → Prompt System

The 84 existing order events become:

- Prompt templates (contextual text per order type)
- Outcome pools (what happens when player investigates)
- Event chains (multi-phase narratives)

**Add skill XP:** Many existing order events lack skill XP rewards. During
conversion, add thematically appropriate skill XP to outcomes (see
[Content Effects Reference](ANEWFEATURE/content-effects-reference.md) for
effect format).

---

## Testing Checklist

### Prompt System

- [ ] 85% of phases pass silently
- [ ] 15% of phases show prompt
- [ ] Prompts are contextual to order type
- [ ] Prompts filter by travel context (land/sea)
- [ ] [Investigate] triggers pre-rolled outcome
- [ ] [Ignore] triggers order-type-dependent catch check

### Investigate Outcome Distribution

- [ ] 50% of investigations yield nothing
- [ ] 30% yield small rewards (gold, supplies, lord rep)
- [ ] 15% trigger event chains
- [ ] 5% trigger danger (injury, ambush, sickness)

### Ignore Consequence System

- [ ] Guard/Sentry duties: 20% catch chance on ignore
- [ ] Patrol duty: 15% catch chance on ignore
- [ ] Camp Labor/Off Duty: 0% catch chance (no expectations)
- [ ] "Caught slacking" events fire 1-4 hours after ignore
- [ ] Consequences apply: gold loss, lord rep loss, scrutiny, party casualties
- [ ] Ignore consequence state persists across saves

### Skill Progression

- [ ] Outcomes award appropriate skill XP for order type
- [ ] Thematic aliases work ("Perception" awards Scouting XP)
- [ ] Skill XP integrates with native progression system
- [ ] Different order types reward different skills (coverage balance)

### Event Chains

- [ ] Chains progress through phases
- [ ] Player choices set flags
- [ ] Flags affect later phases
- [ ] Delayed follow-ups fire correctly
- [ ] Chain state persists across saves

### Stakes Balance

- [ ] Investigate risk feels meaningful (injury/danger outcomes matter)
- [ ] Ignore risk feels meaningful (getting caught has real cost)
- [ ] Neither choice feels "always correct"
- [ ] Order type clearly affects optimal strategy
- [ ] Lord rep changes are noticeable to player
- [ ] Party casualty events feel impactful

### Pacing

- [ ] Average 1 interesting event per 8-12 days
- [ ] No more random popup spam
- [ ] Threshold events (supply crisis, etc.) still fire
- [ ] Decisions still available in Camp Hub
- [ ] Map incidents still fire on context

---

## New Files Checklist

### C# Files to Create

Add each to `Enlisted.csproj` after creation:

```xml
<!-- Models -->
<Compile Include="src\Features\Orders\Models\OrderPrompt.cs"/>
<Compile Include="src\Features\Orders\Models\PromptOutcome.cs"/>
<Compile Include="src\Features\Orders\Models\PromptOutcomeData.cs"/>
<Compile Include="src\Features\Orders\Models\IgnoreConsequence.cs"/>
<Compile Include="src\Features\Orders\Models\PendingConsequence.cs"/>

<!-- Catalogs -->
<Compile Include="src\Features\Orders\PromptCatalog.cs"/>
<Compile Include="src\Features\Orders\IgnoreConsequenceCatalog.cs"/>

<!-- Managers -->
<Compile Include="src\Features\Content\EventChainManager.cs"/>
<Compile Include="src\Features\Content\CampaignContextTracker.cs"/>

<!-- Enums (if separate files) -->
<Compile Include="src\Features\Content\CampaignContext.cs"/>
```

### JSON Data Files to Create

```text
ModuleData/Enlisted/
├── Prompts/
│   ├── order_prompts.json          # Prompt templates by order type
│   ├── prompt_outcomes.json        # Outcome pools (nothing/reward/chain/danger)
│   └── ignore_consequences.json    # "Caught slacking" events
└── Chains/
    └── event_chains.json           # Multi-phase event definitions
```

### Validation Updates

After adding JSON files, update `Tools/Validation/validate_content.py` to:

1. Validate new JSON schemas (prompts, outcomes, chains)
2. Check prompt `contexts` field has valid values ("land", "sea", "any")
3. Verify all ignore consequences have `tooltip` fields
4. Ensure outcome `effects` use valid effect keys

---

## Background Simulation Systems (Autosim)

These systems run automatically without player input. They are **separate from
the orchestrator** and will continue working after orchestrator removal.

### Core Simulation Systems (MUST KEEP)

| System | Trigger | What It Does |
|:-------|:--------|:-------------|
| **CompanySimulationBehavior** | Daily | Sickness, injuries, desertion, incidents, crisis events |
| **CampRoutineProcessor** | Phase boundary | XP gains, gold/supply changes, conditions |
| **CampLifeBehavior** | Daily | Quartermaster mood, logistics strain, pay tension |
| **CompanyNeedsManager** | Daily | Readiness degradation (2 base, +5 on march) |
| **RetinueTrickleSystem** | Daily | Free soldier recruitment for T7+ commanders |
| **RetinueCasualtyTracker** | After battles | Veteran emergence, casualty tracking |
| **OrderProgressionBehavior** | Hourly | Event injection during duty phases |
| **EscalationManager** | Daily | Scrutiny/discipline tracking |
| **PlayerConditionBehavior** | Daily | Injury/illness duration tracking |
| **BaggageTrainManager** | Hourly/Daily | Baggage delays, raids, access windows |

### CompanySimulationBehavior - Daily Phases

The main autosim runs these phases every day:

1. **Consumption** - Resource depletion (via CompanyNeedsManager)
2. **Recovery** - Wounded heal, sick recover or die
3. **New Conditions** - Generate sickness (0-2%), injuries, desertion attempts
4. **Incidents** - Roll 0-2 random camp incidents, apply effects
5. **Pulse Evaluation** - Track pressure thresholds (low supplies, high sickness)
6. **Pressure Arc Events** - Fire narrative events at day 3, 5, 7 of crisis
7. **News Generation** - Push results to news system (max 5/day)

**Crisis Triggers:**
- Supply crisis: 3+ days at critical supply level
- Epidemic: High sickness rate
- Desertion waves: Multiple desertions in short period

### CampRoutineProcessor - Phase Outcomes

At phase boundaries (6am, 12pm, 6pm, midnight), rolls outcome quality:

| Outcome | Chance | Effect |
|:--------|:-------|:-------|
| Excellent | 10% | High XP, bonus gold |
| Good | 25% | Good XP, some gold |
| Normal | 40% | Standard XP |
| Poor | 20% | Low XP |
| Mishap | 5% | Gold loss, possible condition |

### RetinueTrickleSystem - Soldier Recruitment

T7+ commanders get free soldiers based on context:

| Context | Interval |
|:--------|:---------|
| Recent victory | 1 soldier per 2 days |
| Peace (5+ days) | 1 per 2 days |
| Friendly territory | 1 per 3 days |
| Campaign (default) | 1 per 4-5 days |
| Recent defeat | BLOCKED |

### BaggageTrainManager - World-State Probabilities

Baggage events use world state for probability modifiers:

| Factor | Modifier |
|:-------|:---------|
| Activity: Intense | Higher delay/raid chance |
| Lord: Defeated | Frozen (no changes) |
| Terrain: Mountain | +10% delay |
| Terrain: Snow | +15% delay |
| War stance: Desperate | Higher raid chance |

### Autosim Independence from Orchestrator

These systems **do not require ContentOrchestrator**. They only use:
- `WorldStateAnalyzer.AnalyzeSituation()` - Direct call works
- `EventDeliveryManager.QueueEvent()` - For crisis events

After orchestrator removal, replace:
```csharp
// OLD
ContentOrchestrator.Instance.GetCurrentWorldSituation()
ContentOrchestrator.Instance.QueueCrisisEvent(eventId)

// NEW
WorldStateAnalyzer.AnalyzeSituation()
EventDeliveryManager.Instance.QueueEvent(EventCatalog.GetEvent(eventId))
```

### Removed Systems (2026-01-11)

- **Morale System** - Affected desertions, incidents, company mood
- **Rest/Fatigue Budget** - 0-24 hours, affected readiness/conditions
- **Company Rest Activity** - Restored rest budget

These are gone. Save loading handles migration.

---

## Orchestrator Removal Plan

The ContentOrchestrator (2000+ lines) was designed for the OLD flat event system.
With the new chain-based system, it's no longer needed. This section documents
safe removal.

### What ContentOrchestrator Did (OLD System)

- Pre-scheduled "opportunities" 24 hours ahead
- Tracked day phase transitions (Dawn/Midday/Dusk/Night)
- Managed opportunity locking/commitment/consumption
- Provided world state snapshots to other systems
- Queued crisis events when company needs hit thresholds
- Medical pressure tracking and illness triggers

### Why It's No Longer Needed (NEW System)

| OLD System | NEW System |
|:-----------|:-----------|
| Orchestrator picks events to fire | **Duties** assign chains contextually |
| Player commits to future opportunities | Player picks **Decisions** immediately |
| Complex 24h scheduling | Chains fire when duty/decision starts |
| World state affects event selection | Chain `tier_range` and `contexts` filter |
| Opportunity locking | No locking needed - chains are immediate |

### Systems to PRESERVE

**Enums (saved in games - MUST KEEP):**
- `DayPhase` - Dawn/Midday/Dusk/Night (useful for duty timing)
- `LordSituation` - What lord is doing (could affect chain availability)
- `ActivityLevel` - Quiet/Routine/Active/Intense (could affect chain frequency)
- `LifePhase`, `WarStance` - Saved in games

**Classes to KEEP:**
- `WorldStateAnalyzer` - Useful for detecting context (land/sea, war/peace)
- `WorldSituation` - Snapshot struct used by other systems
- `OrchestratorEnums.cs` - Contains the enums above

**News System (KEEP FUNCTIONAL):**
- `EnlistedNewsBehavior` - Can work with `WorldStateAnalyzer` directly
- `MainMenuNewsCache` - Remove phase callback, use time-based refresh
- `ForecastGenerator` - Keep for UI status display

### Files to DELETE

**C# Files:**
```
src/Features/Content/ContentOrchestrator.cs     # 2000+ lines - DELETE
src/Features/Content/OrchestratorOverride.cs    # Override system - DELETE
```

**JSON/Config Files:**
```
ModuleData/Enlisted/Config/orchestrator_overrides.json  # DELETE
```

### Files to MODIFY

**Remove ContentOrchestrator references from:**

| File | Changes |
|:-----|:--------|
| `SubModule.cs` | Remove behavior registration |
| `EnlistedMenuBehavior.cs` | Remove 7 orchestrator calls (opportunity system goes away) |
| `EnlistedNewsBehavior.cs` | Replace 3 calls with `WorldStateAnalyzer` |
| `MainMenuNewsCache.cs` | Remove `OnPhaseChanged` callback |
| `OrderProgressionBehavior.cs` | Replace 2 calls with `WorldStateAnalyzer` |
| `CompanySimulationBehavior.cs` | Move crisis event logic inline or to new system |
| `CampScheduleManager.cs` | Remove override checking |
| `CampRoutineProcessor.cs` | Remove override clearing |
| `BaggageTrainManager.cs` | Replace with `WorldStateAnalyzer` |
| `EventPacingManager.cs` | Replace with `WorldStateAnalyzer` |
| `Enlisted.csproj` | Remove file references |

### News System Preservation

The news system shows players what's happening around the map. It should work
without the orchestrator.

**Current flow:**
```
ContentOrchestrator.GetCurrentWorldSituation()
    └─ EnlistedNewsBehavior uses for context
        └─ MainMenuNewsCache caches results
            └─ UI displays news
```

**New flow:**
```
WorldStateAnalyzer.AnalyzeSituation()  # Direct call
    └─ EnlistedNewsBehavior uses for context
        └─ MainMenuNewsCache caches results (time-based refresh)
            └─ UI displays news
```

**Changes needed:**
1. Replace `ContentOrchestrator.Instance.GetCurrentWorldSituation()` with
   `WorldStateAnalyzer.AnalyzeSituation()` (3 places in EnlistedNewsBehavior)
2. Remove `OnPhaseChanged(DayPhase)` callback from MainMenuNewsCache
3. Add time-based refresh (every 2 game hours) instead of phase-based

### Crisis Event Handling

Crisis events (supply shortage, epidemic, desertion wave) currently go through
`ContentOrchestrator.QueueCrisisEvent()`. Move this logic to:

**Option A: Inline in CompanySimulationBehavior**
- When supply/morale hits threshold, directly queue event via EventDeliveryManager
- Simple, no new systems

**Option B: New CrisisEventManager (if needed later)**
- Separate class for crisis logic
- Only if crisis events become complex

Recommend **Option A** for simplicity.

### Camp Opportunity System Removal

The old camp "opportunities" system (commit to future activities) is replaced by
**Decisions**. Players now pick Hunt/Gamble/Drink/Train directly from a menu,
and chains fire immediately.

**Remove from EnlistedMenuBehavior:**
- `GetCurrentPhaseOpportunities()` calls
- `GetAllTodaysOpportunities()` calls
- `CommitToOpportunity()` calls
- `ConsumeOpportunity()` calls
- Opportunity locking UI

**Replace with:**
- Simple Decision menu: [Hunt] [Gamble] [Drink] [Train]
- Each triggers a random chain from that decision's chain file

### Removal Order (Safe Sequence)

1. **Create WorldStateAnalyzer fallback** - Ensure it works standalone
2. **Update news system** - Replace orchestrator calls with WorldStateAnalyzer
3. **Update order system** - Replace orchestrator calls
4. **Inline crisis events** - Move to CompanySimulationBehavior
5. **Remove camp opportunities** - Replace with Decision menu
6. **Delete ContentOrchestrator.cs** - Remove registration from SubModule
7. **Delete orchestrator_overrides.json** - Config no longer needed
8. **Delete OrchestratorOverride.cs** - Override system removed
9. **Update Enlisted.csproj** - Remove file references
10. **Test thoroughly** - News, menus, orders, decisions all work

---

## JSON/XML File Cleanup

With the new chain-based system, many old content files are obsolete. This section
categorizes all content files by fate.

### Files to DELETE (Old Event System)

**Order Events (16 files) - Replaced by duty chains:**
```
ModuleData/Enlisted/Orders/order_events/
├── camp_patrol_events.json         # → patrol_chains.json
├── equipment_cleaning_events.json  # → camp_labor_chains.json
├── escort_duty_events.json         # DELETE (no escort duty)
├── firewood_detail_events.json     # → camp_labor_chains.json
├── forage_supplies_events.json     # → patrol_chains.json or hunt
├── guard_post_events.json          # → guard_chains.json
├── inspect_defenses_events.json    # DELETE (no inspect duty)
├── latrine_duty_events.json        # → camp_labor_chains.json
├── lead_patrol_events.json         # → patrol_chains.json
├── march_formation_events.json     # DELETE (no march duty)
├── muster_inspection_events.json   # DELETE (no muster duty)
├── repair_equipment_events.json    # → camp_labor_chains.json
├── scout_route_events.json         # → patrol_chains.json
├── sentry_duty_events.json         # → sentry_chains.json
├── train_recruits_events.json      # → train_chains.json
└── treat_wounded_events.json       # DELETE (no medical duty)
```

**Old Decision/Opportunity Files:**
```
ModuleData/Enlisted/Decisions/
├── camp_opportunities.json    # DELETE - replaced by Decisions menu
├── camp_decisions.json        # REVIEW - may have useful content
├── decisions.json             # REVIEW - may have useful content
└── medical_decisions.json     # KEEP for now - medical system separate
```

**Orchestrator Config:**
```
ModuleData/Enlisted/Config/
└── orchestrator_overrides.json    # DELETE
```

### Files to KEEP (Still Used)

**Event Files (threshold/crisis events):**
```
ModuleData/Enlisted/Events/
├── events_escalation_thresholds.json  # KEEP - scrutiny/discipline events
├── events_pay_tension.json            # KEEP - pay delay events
├── events_pay_loyal.json              # KEEP - loyalty events
├── events_pay_mutiny.json             # KEEP - mutiny events
├── events_promotion.json              # KEEP - promotion events
├── events_baggage_stowage.json        # KEEP - baggage system
├── events_retinue.json                # KEEP - retinue events
├── illness_onset.json                 # KEEP - medical system
├── pressure_arc_events.json           # KEEP - company pressure
├── incidents_*.json                   # REVIEW - may migrate to chains
└── schema_version.json                # KEEP - version tracking
```

**Order Definitions (needed for duty assignment):**
```
ModuleData/Enlisted/Orders/
├── orders_t1_t3.json    # REVIEW - simplify to 4 duties
├── orders_t4_t6.json    # REVIEW - simplify to 4 duties
└── orders_t7_t9.json    # REVIEW - simplify to 4 duties
```

**Config Files:**
```
ModuleData/Enlisted/Config/
├── enlisted_config.json         # KEEP - core config
├── simulation_config.json       # KEEP - company simulation
├── progression_config.json      # KEEP - tier progression
├── camp_schedule.json           # REVIEW - may simplify
├── routine_outcomes.json        # REVIEW - may migrate to chains
├── equipment_pricing.json       # KEEP - quartermaster
├── retinue_config.json          # KEEP - retinue system
├── baggage_config.json          # KEEP - baggage system
├── strategic_context_config.json # REVIEW - may not need
└── settings.json                # KEEP - user settings
```

### Files to CREATE (New Chain System)

```
ModuleData/Enlisted/Prompts/
├── order_prompts.json       # NEW - prompt templates by duty type
├── prompt_outcomes.json     # NEW - outcome pools (nothing/reward/chain/danger)
└── ignore_consequences.json # NEW - "caught slacking" events

ModuleData/Enlisted/Chains/
├── patrol_chains.json       # NEW - 3 chains MVP
├── guard_chains.json        # NEW - 3 chains MVP
├── sentry_chains.json       # NEW - 3 chains MVP
├── camp_labor_chains.json   # NEW - 3 chains MVP
├── hunt_chains.json         # NEW - 3 chains MVP
├── gamble_chains.json       # NEW - 3 chains MVP
├── drink_chains.json        # NEW - 3 chains MVP
└── train_chains.json        # NEW - 3 chains MVP
```

### Migration Strategy

**Phase 1: Create new chain files (empty structure)**
- Create 8 chain files with schema and 0 chains
- Validate schema works

**Phase 2: Migrate best content from old files**
- Extract good events from order_events/*.json
- Convert flat events to chain format (trigger → branch → outcome)
- Target: 3 chains per file

**Phase 3: Delete old files**
- Remove order_events/ directory
- Remove camp_opportunities.json
- Remove orchestrator_overrides.json

**Phase 4: Simplify order definitions**
- Consolidate orders_t1_t3.json, orders_t4_t6.json, orders_t7_t9.json
- Into single duties.json with 4 duties (Patrol, Guard, Sentry, Camp Labor)

---

## Implementation Priority

### Phase 0: Orchestrator Removal (Before Other Phases)

1. Update `EnlistedNewsBehavior` to use `WorldStateAnalyzer` directly
2. Update `OrderProgressionBehavior` to use `WorldStateAnalyzer` directly
3. Move crisis event queuing inline to `CompanySimulationBehavior`
4. Remove opportunity system from `EnlistedMenuBehavior`
5. Delete `ContentOrchestrator.cs` and `OrchestratorOverride.cs`
6. Delete `orchestrator_overrides.json`
7. Remove from `SubModule.cs` and `Enlisted.csproj`
8. **Run validation:** `python Tools/Validation/validate_content.py`
9. **Build and test:** `dotnet build -c "Enlisted RETAIL" /p:Platform=x64`

### Phase 1: Remove Random Events

1. Delete `EventPacingManager.TryFireEvent()` method
2. Remove pacing config from `enlisted_config.json`
3. **Run validation:** `python Tools/Validation/validate_content.py`
4. **Build and test:** `dotnet build -c "Enlisted RETAIL" /p:Platform=x64`
5. Test that random events no longer fire
6. Verify threshold events still work (supply crisis, illness arcs)

### Phase 2: Add Prompt System (3-5 days)

1. Create data structures (see Data Structures below)
2. **Add new .cs files to `Enlisted.csproj`** (validator will check)
3. **Register new types in `EnlistedSaveDefiner`** if they need persistence
4. Add prompt check to `OrderProgressionBehavior.ProcessSlotPhase()`
5. Create `ModuleData/Enlisted/Prompts/order_prompts.json` with templates
6. Create `ModuleData/Enlisted/Prompts/prompt_outcomes.json` with outcomes
7. **Add skill XP to all outcomes** - use thematic aliases ("Perception",
    "Smithing", etc.)
8. Create `PromptCatalog` to load and manage prompts
9. Implement pre-roll outcome logic with ModLogger
10. **Verify skill XP integration** - ensure `ApplyPromptEffects` handles
    `skillXp` field
11. **Run validation:** `python Tools/Validation/validate_content.py`
12. **Build and test:** `dotnet build -c "Enlisted RETAIL" /p:Platform=x64`

### Phase 3: Event Chains (5-7 days)

1. Create `EventChainManager` class (see Data Structures below)
2. **Add new .cs files to `Enlisted.csproj`**
3. **Register chain state in `EnlistedSaveDefiner`**
4. Create `ModuleData/Enlisted/Chains/event_chains.json` schema
5. Convert 20 story hook events to chains
6. Implement flag system for chain branching (use existing save system)
7. Add delayed follow-up scheduling (integrate with CampaignTime)
8. **Run validation:** `python Tools/Validation/validate_content.py`
9. Test chain branching with different choices

### Phase 4: Campaign Context Tracking (3-4 days)

1. Create `CampaignContextTracker` class in `src/Features/Content/`
2. **Add to `Enlisted.csproj`**
3. Hook into battle end events to record battles
4. Integrate with `WorldStateAnalyzer.DetermineActivityLevel()`
5. Add `valid_contexts` field to prompts and decisions
6. Filter content by campaign context
7. **Run validation:** `python Tools/Validation/validate_content.py`

### Phase 5: Map Incident Weighting (2-3 days)

1. Add CK3-style weighting to `MapIncidentManager`
2. Integrate with `WorldStateAnalyzer.AnalyzeSituation()` (orchestrator already removed in Phase 0)
3. Make incident chances respect global activity levels
4. Test that incidents feel appropriately rare
5. **Run validation:** `python Tools/Validation/validate_content.py`

### Phase 6: Content Conversion (Ongoing)

1. Convert context events to appropriate categories
2. Write new prompt templates per order type (follow
    [Writing Style Guide](Features/Content/writing-style-guide.md))
3. Create outcome pools with variety
4. **Add skill XP to outcomes** - reference
    [Content Effects Reference](ANEWFEATURE/content-effects-reference.md) for:
    - Skill XP effect format and thematic aliases
    - Appropriate skills per order type (see Skill Rewards by Order Type
        section)
    - Attribute coverage balance (ensure Vigor/Control/Intelligence get
        coverage)
5. Write 5-10 event chains with branches
6. **Run validation after each content addition:**
    `python Tools/Validation/validate_content.py`
7. **Sync localization strings:** `python Tools/Validation/sync_event_strings.py`
8. Test in-game with different order types and contexts
9. **Verify skill progression** - test that outcomes award correct skill XP

---

## Appendix: CK3 Research Summary

### Research: CK3 Feast Chains

Location: `C:\Program Files (x86)\Steam\steamapps\common\Crusader Kings III\game\events\activities\feast_activity\`

1. **Heavy "nothing" weighting:**
    - `random_list { 500 = { nothing } 50 = { actual_event } }` common pattern
    - Result: Events feel rare and special, not spammy

2. **Hidden setup events:**
    - `feast_event_setup_0001` fires invisibly, pre-rolls outcome
    - Player sees result event, not the dice roll

3. **Flag-based branching:**
    - `set_variable { name = feast_outcome_generous }` stores choice
    - Later phases check: `has_variable = feast_outcome_generous`

4. **Delayed follow-ups:**
    - `trigger_event = { id = feast_follow_up days = { 7 14 } }`
    - Creates anticipation between chain phases

5. **Yearly event frequency:**
    - `yearly_on_actions.txt` shows `chance_to_happen = 25`
    - Most random events: 500-1000 weight "nothing" vs 50-100 "event"
    - Net result: ~3-5 random events per year

### Research Date

January 14, 2026

See also: `docs/ANEWFEATURE/ck3-feast-chain-analysis.md` for detailed analysis

---

## Map Incident Weighting (Phase 4)

### Problem: Interruption Fatigue

MapIncidentManager currently fires incidents at **100% rate** when eligible:

- Every battle end → Event fires
- Every settlement entry → Event fires
- Every settlement exit → Event fires

Only siege/waiting have chance rolls (10%/15% per hour).

At 2x speed, this creates **interrupt fatigue** even with cooldowns.

### Solution: CK3-Style Weighting + Orchestrator Integration

Make map incidents respect `ContentOrchestrator` activity levels:

```csharp
// src/Features/Content/MapIncidentManager.cs

private bool ShouldFireIncident(string context)
{
    // Get world situation (orchestrator removed - use WorldStateAnalyzer directly)
    var worldSituation = WorldStateAnalyzer.AnalyzeSituation();
    var activityLevel = worldSituation?.ExpectedActivity ?? ActivityLevel.Routine;
    
    // Base chance by activity level (CK3-style: most contexts are quiet)
    float baseChance = activityLevel switch
    {
        ActivityLevel.Quiet => 0.10f,    // 10% - garrison/recovery, less dramatic
        ActivityLevel.Routine => 0.20f,  // 20% - peacetime, normal pacing
        ActivityLevel.Active => 0.30f,   // 30% - campaign, elevated activity
        ActivityLevel.Intense => 0.40f,  // 40% - siege/battle, very eventful
        _ => 0.20f
    };
    
    // Context modifier (battles more dramatic than leaving settlements)
    float contextModifier = context switch
    {
        "leaving_battle" => 1.5f,      // Battles are dramatic
        "entering_town" => 1.0f,       // Towns normal
        "entering_village" => 0.8f,    // Villages quieter
        "leaving_settlement" => 0.75f, // Leaving less interesting
        "during_siege" => 1.2f,        // Siege events important
        "waiting_in_settlement" => 1.0f,
        _ => 1.0f
    };
    
    float finalChance = baseChance * contextModifier;
    
    ModLogger.Debug(LogCategory, 
        $"Incident chance for {context}: {finalChance * 100:F1}% (activity={activityLevel})");
    
    return MBRandom.RandomFloat < finalChance;
}
```

### Updated Incident Flow

**Before (Current):**

```text
Battle ends → Check cooldown → TryDeliverIncident → Event fires (if eligible)
```

**After (CK3-Style):

```text
Battle ends → Check cooldown → ShouldFireIncident? (30% at Active)
  ├─ Yes → TryDeliverIncident → Event fires
  └─ No → Nothing (routine)
```

### Integration Points

**OnBattleEnd:**

```csharp
private void OnBattleEnd(MapEvent mapEvent)
{
    // ... existing checks ...
    
    if (!ShouldFireIncident("leaving_battle"))
    {
        ModLogger.Debug(LogCategory, "Battle incident roll failed (routine)");
        return;
    }
    
    if (TryDeliverIncident("leaving_battle"))
    {
        _lastBattleIncidentTime = CampaignTime.Now;
    }
}
```

**OnSettlementEntered:**

```csharp
private void OnSettlementEntered(MobileParty party, Settlement settlement, Hero hero)
{
    // ... existing checks ...
    
    var context = settlement.IsTown || settlement.IsCastle 
        ? "entering_town" 
        : "entering_village";
    
    if (!ShouldFireIncident(context))
    {
        ModLogger.Debug(LogCategory, $"{context} incident roll failed (routine)");
        return;
    }
    
    if (TryDeliverIncident(context))
    {
        _lastSettlementIncidentTime = CampaignTime.Now;
    }
}
```

**OnSettlementLeft:**

```csharp
private void OnSettlementLeft(MobileParty party, Settlement settlement)
{
    // ... existing checks ...
    
    if (!ShouldFireIncident("leaving_settlement"))
    {
        ModLogger.Debug(LogCategory, "Settlement exit incident roll failed (routine)");
        return;
    }
    
    if (TryDeliverIncident("leaving_settlement"))
    {
        _lastSettlementIncidentTime = CampaignTime.Now;
    }
}
```

### Expected Results

**Garrison (Quiet - 10% base):**

- Entering town: 10% chance
- Leaving settlement: 7.5% chance
- Battle end: 15% chance

**Peacetime Campaign (Routine - 20% base):**

- Entering town: 20% chance
- Leaving settlement: 15% chance
- Battle end: 30% chance

**Active Campaign (Active - 30% base):**

- Entering town: 30% chance
- Leaving settlement: 22.5% chance
- Battle end: 45% chance

**Siege (Intense - 40% base):**

- During siege (hourly): 48% chance (10% base *1.2* 4 checks/day)
- Battle end: 60% chance

### Benefits

1. **Respects global pacing** - Orchestrator controls overall activity
2. **Context-aware** - Battles more eventful than garrison
3. **Dynamic** - Changes with campaign situation
4. **CK3-aligned** - Most triggers are routine, events feel special
5. **Player-friendly** - No surprise spam at 2x speed

### Testing: Incident Weighting

- [ ] Garrison: Very few incidents (feels quiet)
- [ ] Campaign: Moderate incidents (feels active)
- [ ] Siege: Frequent incidents (feels intense)
- [ ] No interrupt fatigue at 2x speed
- [ ] Events feel special, not routine

---

## Campaign Context Tracking (Phase 4)

### Problem: Static Activity Levels

Current `WorldStateAnalyzer` only checks "where is lord now" - but lords are
ALWAYS moving. Result: `ActivityLevel.Routine` 90% of the time, defeating
dynamic pacing.

Also: No way to filter events by recent history. Card games shouldn't fire
right after battle.

### Solution: Track Campaign History

Track **what has happened recently** to determine:

1. **Activity Level** - Based on recent battles, not current location
2. **Campaign Context** - Filter events to contextually appropriate ones

### Campaign Contexts

```csharp
public enum CampaignContext
{
    PostBattle,         // 0-24 hours since battle (rare events)
    RecentEngagement,   // 1-3 days since battle
    NormalCampaign,     // 3+ days since battle
    Garrison,           // In settlement, no recent battles (7+ days)
    PreBattle           // Enemy army nearby (future enhancement)
}
```

### Time Windows

| Context | Time | Level | Events |
| :--- | :--- | :--- | :--- |
| PostBattle | 0-24h | Intense | Loot, medicine, rep |
| RecentEngagement | 1-3d | Active | War stories, smith |
| NormalCampaign | 3-7d | Routine | Patrol, train, gamble |
| Garrison | 7d+ | Quiet | Relax, town, downtime |

### CampaignContextTracker Class

```csharp
// src/Features/Content/CampaignContextTracker.cs
public class CampaignContextTracker : CampaignBehaviorBase
{
    private const string LogCategory = "CampaignContext";
    
    public static CampaignContextTracker Instance { get; private set; }
    
    // Battle tracking
    private CampaignTime _lastBattleTime = CampaignTime.Never;
    private int _lastBattleCasualties;
    private bool _wasVictorious;
    private int _battlesLastSevenDays;
    
    public CampaignContextTracker()
    {
        Instance = this;
    }
    
    public override void RegisterEvents()
    {
        CampaignEvents.OnPlayerBattleEndEvent.AddNonSerializedListener(this, OnBattleEnd);
    }
    
    public override void SyncData(IDataStore dataStore)
    {
        dataStore.SyncData("ctx_lastBattleTime", ref _lastBattleTime);
        dataStore.SyncData("ctx_lastBattleCasualties", ref _lastBattleCasualties);
        dataStore.SyncData("ctx_wasVictorious", ref _wasVictorious);
        dataStore.SyncData("ctx_battlesLastSevenDays", ref _battlesLastSevenDays);
    }
    
    private void OnBattleEnd(MapEvent mapEvent)
    {
        if (mapEvent == null || !mapEvent.IsPlayerMapEvent)
        {
            return;
        }
        
        _lastBattleTime = CampaignTime.Now;
        _wasVictorious = mapEvent.BattleState == BattleState.AttackerVictory; // Adjust based on player side
        _battlesLastSevenDays++;
        
        ModLogger.Info(LogCategory, 
            $"Battle recorded: Victory={_wasVictorious}, Context now PostBattle");
    }
    
    /// <summary>
    /// Gets current campaign context based on battle history.
    /// </summary>
    public CampaignContext GetCurrentContext()
    {
        if (_lastBattleTime == CampaignTime.Never)
        {
            return CampaignContext.Garrison;
        }
        
        var hoursSinceBattle = (float)(CampaignTime.Now - _lastBattleTime).ToHours;
        
        // PostBattle: 0-24 hours (rare events window)
        if (hoursSinceBattle < 24)
        {
            return CampaignContext.PostBattle;
        }
        
        // RecentEngagement: 1-3 days
        if (hoursSinceBattle < 72)
        {
            return CampaignContext.RecentEngagement;
        }
        
        // Garrison: 7+ days without battle
        if (hoursSinceBattle >= 168)
        {
            return CampaignContext.Garrison;
        }
        
        // NormalCampaign: 3-7 days
        return CampaignContext.NormalCampaign;
    }
    
    /// <summary>
    /// Gets activity level based on recent battle history.
    /// More accurate than location-based detection.
    /// </summary>
    public ActivityLevel GetActivityLevelFromHistory()
    {
        var context = GetCurrentContext();
        
        return context switch
        {
            CampaignContext.PostBattle => ActivityLevel.Intense,
            CampaignContext.RecentEngagement => ActivityLevel.Active,
            CampaignContext.NormalCampaign => ActivityLevel.Routine,
            CampaignContext.Garrison => ActivityLevel.Quiet,
            _ => ActivityLevel.Routine
        };
    }
    
    /// <summary>
    /// Hours since last battle. Used for decision/event filtering.
    /// </summary>
    public float GetHoursSinceLastBattle()
    {
        if (_lastBattleTime == CampaignTime.Never)
        {
            return float.MaxValue;
        }
        return (float)(CampaignTime.Now - _lastBattleTime).ToHours;
    }
    
    /// <summary>
    /// Was last battle a victory? Affects post-battle event tone.
    /// </summary>
    public bool WasLastBattleVictory() => _wasVictorious;
}
```

### Integration with WorldStateAnalyzer

```csharp
// Update WorldStateAnalyzer.DetermineActivityLevel()
private static ActivityLevel DetermineActivityLevel(LifePhase lifePhase, LordSituation lordSituation)
{
    // Crisis/Siege override history-based detection
    if (lifePhase == LifePhase.Crisis) return ActivityLevel.Intense;
    if (lifePhase == LifePhase.Siege) return ActivityLevel.Active;
    if (lifePhase == LifePhase.Recovery) return ActivityLevel.Quiet;
    
    // Use history-based detection for Campaign/Peacetime
    var contextTracker = CampaignContextTracker.Instance;
    if (contextTracker != null)
    {
        return contextTracker.GetActivityLevelFromHistory();
    }
    
    // Fallback to existing logic
    return lifePhase switch
    {
        LifePhase.Campaign => ActivityLevel.Routine,
        LifePhase.Peacetime => ActivityLevel.Quiet,
        _ => ActivityLevel.Routine
    };
}
```

### Event/Prompt Context Filtering

Add `valid_contexts` field to prompts:

```json
{
  "prompts": [
    {
      "title": "Loot the Fallen",
      "description": "Bodies lie scattered. Some may have coin.",
      "valid_contexts": ["PostBattle"],
      "contexts": ["land"]
    },
    {
      "title": "Help the Wounded",
      "description": "A soldier groans nearby, clutching his side.",
      "valid_contexts": ["PostBattle", "RecentEngagement"],
      "contexts": ["land", "sea"]
    },
    {
      "title": "War Stories",
      "description": "Veterans gather to share tales of past battles.",
      "valid_contexts": ["RecentEngagement", "NormalCampaign"],
      "contexts": ["land", "sea"]
    },
    {
      "title": "Card Game",
      "description": "Some soldiers are dealing cards by the fire.",
      "valid_contexts": ["NormalCampaign", "Garrison"],
      "contexts": ["land", "sea"]
    },
    {
      "title": "Something Stirs",
      "description": "You hear rustling in the bushes nearby.",
      "valid_contexts": ["NormalCampaign", "Garrison"],
      "contexts": ["land"]
    }
  ]
}
```

### Decision Availability Windows

Post-battle decisions appear in Camp Hub only during valid window:

```json
{
  "decisions": [
    {
      "id": "dec_loot_fallen",
      "title": "Loot the Fallen",
      "valid_contexts": ["PostBattle"],
      "max_hours_since_battle": 24,
      "effects": { "gold": "30-100", "scrutiny": 2 }
    },
    {
      "id": "dec_help_wounded",
      "title": "Help the Wounded",
      "valid_contexts": ["PostBattle", "RecentEngagement"],
      "max_hours_since_battle": 48,
      "effects": { "medicine_xp": 20, "lord_reputation": 3 }
    },
    {
      "id": "dec_report_casualties",
      "title": "Report to Your Lord",
      "valid_contexts": ["PostBattle"],
      "max_hours_since_battle": 12,
      "effects": { "lord_reputation": 3 }
    }
  ]
}
```

### PostBattle Event Rarity

**IMPORTANT:** PostBattle context (0-24 hours) should have **rare** events.
Player needs time for something to actually happen.

```csharp
// In prompt selection logic
float promptChance = context switch
{
    CampaignContext.PostBattle => 0.08f,      // 8% per phase - rare!
    CampaignContext.RecentEngagement => 0.12f, // 12% per phase
    CampaignContext.NormalCampaign => 0.15f,   // 15% per phase (standard)
    CampaignContext.Garrison => 0.10f,         // 10% per phase
    _ => 0.15f
};
```

With 4 phases per day at 8%, that's ~32% daily chance for a PostBattle event.
Gives player ~3 chances for something meaningful to happen in the 24-hour window.

### Testing: Campaign Context

- [ ] Battle ends → Context switches to PostBattle
- [ ] PostBattle decisions appear in Camp Hub
- [ ] PostBattle decisions disappear after 24 hours
- [ ] Events filter to context-appropriate content
- [ ] PostBattle events are rare (player has time)
- [ ] Activity level reflects recent battle history
- [ ] Context persists across save/load
- [ ] Card games don't fire during PostBattle

---

## Data Structures

### OrderPrompt Model

```csharp
// src/Features/Orders/Models/OrderPrompt.cs
public class OrderPrompt
{
    public string Title { get; set; }
    public string Description { get; set; }
    public string InvestigateText { get; set; }
    public string IgnoreText { get; set; }
    public List<string> Contexts { get; set; } = new(); // "land", "sea", "any"
}
```

### PromptOutcome Enum

```csharp
// src/Features/Orders/Models/PromptOutcome.cs
public enum PromptOutcome
{
    Nothing,       // 50% - false alarm, waste of time
    SmallReward,   // 30% - gold, supplies, reputation
    EventChain,    // 15% - trigger multi-phase event
    Danger         // 5%  - ambush, injury, combat
}
```

### DutyType Enum

```csharp
// src/Features/Orders/Models/DutyType.cs
public enum DutyType
{
    Patrol,      // Walk perimeter, check routes (T3-9)
    Guard,       // Stand watch at post (T1-9)
    Sentry,      // Observation post (T2-9)
    CampLabor,   // Firewood, latrine, errands (T1-4)
    OffDuty      // Free time - no duties assigned
}
```

### PromptCatalog Class

```csharp
// src/Features/Orders/PromptCatalog.cs
public static class PromptCatalog
{
    private const string LogCategory = "PromptCatalog";
    private static Dictionary<string, List<OrderPrompt>> _promptsByOrderType;
    private static Dictionary<PromptOutcome, List<PromptOutcomeData>> _outcomes;
    
    public static void LoadPrompts()
    {
        try
        {
            // Load from ModuleData/Enlisted/Prompts/order_prompts.json
            // Parse JSON and populate _promptsByOrderType
            ModLogger.Info(LogCategory, "[E-PROMPT-001] Loaded prompt templates");
        }
        catch (Exception ex)
        {
            ModLogger.Error(LogCategory, "[E-PROMPT-002] Failed to load prompt templates", ex);
        }
    }

    public static List<OrderPrompt> GetPromptsForOrderType(string orderType)
    {
        if (_promptsByOrderType == null)
        {
            ModLogger.Warn(LogCategory, "[E-PROMPT-003] Prompts not loaded yet");
            return new List<OrderPrompt>();
        }
        
        return _promptsByOrderType.TryGetValue(orderType, out var prompts) 
            ? prompts 
            : new List<OrderPrompt>();
    }
    
    public static PromptOutcomeData GetOutcomeData(PromptOutcome outcome)
    {
        // Select random outcome from pool for this outcome type
        var pool = _outcomes[outcome];
        return pool[MBRandom.RandomInt(pool.Count)];
    }
}
```

### PromptOutcomeData Model

```csharp
// src/Features/Orders/Models/PromptOutcomeData.cs
public class PromptOutcomeData
{
    public string Text { get; set; }
    public Dictionary<string, object> Effects { get; set; } = new();
    public string ChainId { get; set; } // For EventChain outcomes
    public string TriggerEvent { get; set; } // Event ID to fire
}
```

### Integration with Existing Systems

**IsPartyAtSea() - Use Existing Code:**

```csharp
private bool IsPartyAtSea()
{
    // Reuse logic from OrderManager.cs lines 118-121
    var enlistment = EnlistmentBehavior.Instance;
    var party = enlistment?.CurrentLord?.PartyBelongedTo;
    return party != null && 
           party.CurrentSettlement == null && 
           party.BesiegedSettlement == null && 
           party.IsCurrentlyAtSea;
}
```

**FireEventChain() - Connect to Event System:**

```csharp
private void FireEventChain(Order order, PromptOutcome outcome)
{
    const string LogCategory = "OrderPrompts";
    
    try
    {
        var outcomeData = PromptCatalog.GetOutcomeData(outcome);
        
        if (outcomeData == null)
        {
            ModLogger.Warn(LogCategory, $"[E-PROMPT-004] No outcome data for {outcome}");
            return;
        }

        ModLogger.Info(LogCategory, $"[E-PROMPT-005] Prompt outcome: {outcome} during order {order.Id}");
        
        // Show outcome text
        if (!string.IsNullOrEmpty(outcomeData.Text))
        {
            InformationManager.DisplayMessage(new InformationMessage(outcomeData.Text, Colors.Yellow));
        }
        
        // Apply effects
        ApplyPromptEffects(outcomeData.Effects);
        
        // Trigger event chain if specified
        if (!string.IsNullOrEmpty(outcomeData.TriggerEvent))
        {
            var evt = EventCatalog.GetEvent(outcomeData.TriggerEvent);
            if (evt != null)
            {
                EventDeliveryManager.Instance?.QueueEvent(evt);
                ModLogger.Debug(LogCategory, $"[E-PROMPT-006] Queued event chain: {outcomeData.TriggerEvent}");
            }
            else
            {
                ModLogger.Warn(LogCategory, $"[E-PROMPT-007] Event not found: {outcomeData.TriggerEvent}");
            }
        }
    }
    catch (Exception ex)
    {
        ModLogger.Error(LogCategory, "[E-PROMPT-008] Failed to fire event chain", ex);
    }
}

private void ApplyPromptEffects(Dictionary<string, object> effects)
{
    foreach (var effect in effects)
    {
        switch (effect.Key)
        {
            case "gold":
                // Parse range "25-75" or single value
                var gold = ParseGoldRange(effect.Value.ToString());
                // CRITICAL: Use GiveGoldAction, NOT ChangeHeroGold (see BLUEPRINT.md)
                if (gold > 0)
                {
                    GiveGoldAction.ApplyBetweenCharacters(null, Hero.MainHero, gold);
                }
                else if (gold < 0)
                {
                    // For fines/losses, take gold from player
                    GiveGoldAction.ApplyBetweenCharacters(Hero.MainHero, null, -gold);
                }
                break;

            case "skillXp":
                // Parse skill XP dictionary { "Perception": 15, "Athletics": 10 }
                var skillXpDict = effect.Value as Dictionary<string, object>;
                if (skillXpDict != null)
                {
                    foreach (var skillEntry in skillXpDict)
                    {
                        var skill = SkillCheckHelper.GetSkillByName(skillEntry.Key);
                        var xp = int.Parse(skillEntry.Value.ToString());
                        if (skill != null)
                        {
                            Hero.MainHero.AddSkillXp(skill, xp);
                            ModLogger.Debug(LogCategory, $"[E-PROMPT-009] Awarded {xp} XP to {skill.Name}");
                        }
                    }
                }
                break;
                
            case "lord_reputation":
                var rep = int.Parse(effect.Value.ToString());
                EscalationManager.Instance.ModifyReputation(ReputationType.Lord, rep, "prompt_outcome");
                break;

            case "party_casualties":
                var casualties = int.Parse(effect.Value.ToString());
                // Remove random troops from lord's party
                PartyHelper.RemoveCasualties(EnlistmentBehavior.Instance?.CurrentLord?.PartyBelongedTo, casualties);
                break;
                
            case "company_supplies":
                var supplies = int.Parse(effect.Value.ToString());
                EnlistmentBehavior.Instance?.CompanyNeeds?.SetNeed(CompanyNeed.Supplies, supplies);
                break;
                
            case "injury":
                InjurySystem.ApplyInjury(effect.Value.ToString(), "prompt_outcome");
                break;
                
            case "trigger_combat":
                // Queue combat encounter via MapIncidentManager
                var encounterType = effect.Value.ToString();
                MapIncidentManager.Instance?.TriggerCombatEncounter(encounterType);
                break;
        }
    }
}
```

### File Structure

```text
ModuleData/Enlisted/
├── Prompts/
│   ├── order_prompts.json          (NEW - prompt templates)
│   └── prompt_outcomes.json        (NEW - outcome pools)
├── Chains/
│   └── event_chains.json           (NEW - event chain definitions)
├── Orders/
│   ├── orders_t1_t3.json          (EXISTING - keep)
│   └── orders_t4_t6.json          (EXISTING - keep)
└── Events/
    ├── narrative-events.json       (EXISTING - convert to chains)
    └── escalation-events.json      (EXISTING - keep)
```

### Classes to Create

| Class | File | Purpose |
| :--- | :--- | :--- |
| `OrderPrompt` | `src/Features/Orders/Models/OrderPrompt.cs` | Prompt template model |
| `PromptOutcome` | `src/Features/Orders/Models/PromptOutcome.cs` | Outcome type enum |
| `PromptOutcomeData` | `src/Features/Orders/Models/PromptOutcomeData.cs` | Outcome effects data |
| `IgnoreConsequence` | `src/Features/Orders/Models/IgnoreConsequence.cs` | Ignore penalty data |
| `PendingConsequence` | `src/Features/Orders/Models/PendingConsequence.cs` | Scheduled consequence (saveable) |
| `DutyType` | `src/Features/Orders/Models/DutyType.cs` | Duty type enum |
| `EventChainState` | `src/Features/Content/Models/EventChainState.cs` | Active chain state (saveable) |
| `PromptCatalog` | `src/Features/Orders/PromptCatalog.cs` | Load/select prompts |
| `IgnoreConsequenceCatalog` | `src/Features/Orders/IgnoreConsequenceCatalog.cs` | Load/select consequences |
| `EventChainManager` | `src/Features/Content/EventChainManager.cs` | Chain state management |
| `CampaignContextTracker` | `src/Features/Content/CampaignContextTracker.cs` | Battle history tracking |

### EventChainState Model

```csharp
// src/Features/Content/Models/EventChainState.cs
public class EventChainState
{
    [SaveableField(1)]
    public string ChainId { get; set; }

    [SaveableField(2)]
    public int CurrentPhase { get; set; }

    [SaveableField(3)]
    public Dictionary<string, bool> Flags { get; set; } = new();

    [SaveableField(4)]
    public CampaignTime StartTime { get; set; }

    [SaveableField(5)]
    public CampaignTime? NextPhaseTime { get; set; }
}
```

**IMPORTANT:** After creating files, manually add to `Enlisted.csproj`:

```xml
<!-- Models -->
<Compile Include="src\Features\Orders\Models\OrderPrompt.cs"/>
<Compile Include="src\Features\Orders\Models\PromptOutcome.cs"/>
<Compile Include="src\Features\Orders\Models\PromptOutcomeData.cs"/>
<Compile Include="src\Features\Orders\Models\IgnoreConsequence.cs"/>
<Compile Include="src\Features\Orders\Models\PendingConsequence.cs"/>
<Compile Include="src\Features\Orders\Models\DutyType.cs"/>
<Compile Include="src\Features\Content\Models\EventChainState.cs"/>

<!-- Catalogs -->
<Compile Include="src\Features\Orders\PromptCatalog.cs"/>
<Compile Include="src\Features\Orders\IgnoreConsequenceCatalog.cs"/>

<!-- Managers -->
<Compile Include="src\Features\Content\EventChainManager.cs"/>
<Compile Include="src\Features\Content\CampaignContextTracker.cs"/>
```

### Save System Registration

**If any new classes need persistence, register in `EnlistedSaveDefiner`:**

```csharp
// Example:
AddClassDefinition(typeof(EventChainState), 1234);
AddEnumDefinition(typeof(PromptOutcome), 5678);
```

---

## Error Code Reference

Error codes used in this system follow format `E-PROMPT-XXX`:

| Code | Level | Message | Resolution |
|:-----|:------|:--------|:-----------|
| E-PROMPT-001 | Info | Loaded prompt templates | Normal startup |
| E-PROMPT-002 | Error | Failed to load prompt templates | Check JSON file path/format |
| E-PROMPT-003 | Warn | Prompts not loaded yet | LoadPrompts() not called |
| E-PROMPT-004 | Warn | No outcome data for {outcome} | Add outcome to prompt_outcomes.json |
| E-PROMPT-005 | Info | Prompt outcome: {outcome} | Normal operation |
| E-PROMPT-006 | Debug | Queued event chain | Normal chain trigger |
| E-PROMPT-007 | Warn | Event not found: {id} | Missing event in EventCatalog |
| E-PROMPT-008 | Error | Failed to fire event chain | Check exception details |
| E-PROMPT-009 | Debug | Awarded {xp} XP to {skill} | Normal XP grant |
| E-PROMPT-010 | Debug | Ignore consequence scheduled | Normal ignore check |

---

#### End of Order Prompt Model Specification
