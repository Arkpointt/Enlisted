# Lance Life Events — UI Integration Guide

This document specifies **which UI system to use** for each type of Lance Life event. Choosing the right UI affects timing, player flow, and immersion.

---

## Table of Contents

1. [UI Systems Overview](#ui-systems-overview)
2. [Decision Matrix](#decision-matrix)
3. [Map Incidents](#map-incidents)
   - [When to Use](#when-to-use-map-incidents)
   - [Native Triggers Available](#native-triggers-available)
   - [Implementation Pattern](#map-incident-implementation-pattern)
   - [Enlisted Incidents (Current)](#enlisted-incidents-current)
4. [Game Menus](#game-menus)
   - [When to Use](#when-to-use-game-menus)
   - [Menu Contexts](#menu-contexts)
   - [Implementation Pattern](#game-menu-implementation-pattern)
5. [Inquiry Prompts](#inquiry-prompts)
   - [When to Use](#when-to-use-inquiry-prompts)
   - [Implementation Pattern](#inquiry-prompt-implementation-pattern)
6. [Event-to-UI Mapping](#event-to-ui-mapping)
7. [Trigger-to-UI Mapping](#trigger-to-ui-mapping)
8. [Safety Checks](#safety-checks)
9. [Cooldown Management](#cooldown-management)

---

## UI Systems Overview

Bannerlord offers three main ways to present player choices:

| System | Feel | Timing | Complexity | Best For |
|--------|------|--------|------------|----------|
| **Map Incidents** | Formal, event-like | Specific trigger points (enter/leave settlement, after battle) | High | Major camp events, story beats, consequences |
| **Game Menus** | Integrated, contextual | While in a menu (town, camp, wait) | Medium | Location-specific choices, camp activities |
| **Inquiry Prompts** | Quick, interruptive | Any safe moment | Low | Urgent decisions, simple yes/no, fallbacks |

### Visual Comparison

**Map Incident:**
- Full-screen or large panel
- Artwork/illustration possible
- Multiple detailed options
- Feels like "something happened"

**Game Menu:**
- Menu option appears in existing menu
- Player chooses when to engage
- Can be ignored/deferred
- Feels like "something available"

**Inquiry Prompt:**
- Small popup dialog
- Immediate response required
- Simple options (usually 2–3)
- Feels like "quick decision needed"

---

## Decision Matrix

Use this to quickly determine which UI system fits your event:

| Question | If Yes → | If No → |
|----------|----------|---------|
| Does this feel like "something happened to you"? | Map Incident | → |
| Does this feel like "something you can do"? | Game Menu | → |
| Is this urgent/time-sensitive in the fiction? | Inquiry Prompt | → |
| Does it have 4+ detailed options? | Map Incident | → |
| Is it tied to a specific location/menu? | Game Menu | → |
| Is it a simple binary choice? | Inquiry Prompt | Map Incident or Game Menu |
| Should the player be able to ignore/defer it? | Game Menu | Map Incident or Inquiry |
| Is it a fallback for when incidents unavailable? | Inquiry Prompt | — |

### Quick Rules

1. **"The sergeant grabs you"** → Map Incident (something happens TO you)
2. **"You notice an opportunity"** → Game Menu (something you CAN do)
3. **"Quick, decide now"** → Inquiry Prompt (urgent, simple)
4. **Complex choices with consequences** → Map Incident
5. **Optional camp activities** → Game Menu
6. **Fallback when other UI unavailable** → Inquiry Prompt

---

## Map Incidents

### When to Use Map Incidents

✅ **Use Map Incidents for:**
- Events that "happen to" the player (not player-initiated)
- Story beats with multiple meaningful options
- Consequences of previous choices (escalation events)
- Events tied to campaign state changes (battle ended, siege started)
- Events with 3–4 detailed options
- Events that should feel significant/memorable

❌ **Don't use Map Incidents for:**
- Optional activities the player seeks out
- Simple yes/no decisions
- High-frequency events (incidents have cooldowns)
- Events while player is busy (battle, conversation, prisoner)

### Native Triggers Available

These are the trigger points where Map Incidents can fire:

| Trigger Flag | When It Fires | Enlisted Use Cases |
|--------------|---------------|-------------------|
| `EnteringTown` | Menu opens after entering town | Arrival events, quartermaster greets, pay muster notice |
| `LeavingTown` | Player selects leave option | Departure checks, "one more thing" moments |
| `EnteringVillage` | Menu opens after entering village | Foraging opportunities, local interactions |
| `LeavingVillage` | Player selects leave option | Post-raid events, supply collection |
| `EnteringCastle` | Menu opens after entering castle | Garrison events, lord interactions |
| `LeavingCastle` | Player selects leave option | Departure events |
| `LeavingSettlement` | Any settlement leave | General departure events |
| `LeavingBattle` | After player battle ends (field/hideout) | Post-battle events, loot discipline, casualty response |
| `LeavingEncounter` | After conversation while leaving | Encounter follow-up |
| `WaitingInSettlement` | Hourly tick while in wait menus | Camp downtime events, idle activities |
| `DuringSiege` | Hourly tick while besieged | Siege-specific events |

### Map Incident Implementation Pattern

```csharp
// In EnlistedIncidentsBehavior or LanceStoryBehavior

// 1. Register the incident (on campaign start)
private void RegisterLanceIncident(string incidentId, IncidentTriggerFlags trigger, int cooldownDays)
{
    var incident = Game.Current.ObjectManager.RegisterPresumedObject(new Incident(incidentId));
    incident.Initialize(
        trigger,
        IncidentType.Generic, // or appropriate type
        cooldownDays,
        () => CanIncidentFire(incidentId) // condition check
    );
    
    // Add options
    incident.AddOption(
        new TextObject("Option 1 text"),
        () => ApplyOption1Effects(),
        () => CanSelectOption1() // optional condition
    );
    // ... more options
}

// 2. Condition check
private bool CanIncidentFire(string incidentId)
{
    if (!IsEnlisted()) return false;
    if (Hero.MainHero.IsPrisoner) return false;
    if (!MeetsEventRequirements(incidentId)) return false;
    if (IsOnCooldown(incidentId)) return false;
    return true;
}

// 3. Apply effects
private void ApplyOption1Effects()
{
    // Skill XP
    Hero.MainHero.AddSkillXp(DefaultSkills.Leadership, 25);
    
    // Fatigue
    AddFatigue(2);
    
    // Heat/Discipline
    AddHeat(1);
    
    // Record for cooldown
    SetCooldown(incidentId);
}
```

### Enlisted Incidents (Current)

Currently implemented Map Incidents in Enlisted:

| Incident | Trigger | Purpose |
|----------|---------|---------|
| Bag Check | Custom (12h after enlist) | Equipment inspection at enlistment |

Planned for Lance Life:
- Post-battle events (`LeavingBattle`)
- Settlement arrival events (`EnteringTown/Castle`)
- Siege events (`DuringSiege`)
- Camp wait events (`WaitingInSettlement`)

---

## Game Menus

### When to Use Game Menus

✅ **Use Game Menus for:**
- Optional activities the player can choose to do
- Location-specific opportunities
- Activities that make sense "while you're here"
- Events the player might want to defer or ignore
- Camp/duty activities
- Training and drill options

❌ **Don't use Game Menus for:**
- Urgent events that shouldn't be ignorable
- Events that "happen to" the player
- Events not tied to a specific location/context

### Menu Contexts

Where Game Menu options can appear:

| Menu ID | Context | Enlisted Use Cases |
|---------|---------|-------------------|
| `town` | In a town | Visit quartermaster, tavern activities, trade |
| `town_wait_menus` | Waiting in town | Camp activities, training, socializing |
| `castle` | In a castle | Garrison activities, lord interactions |
| `castle_wait_menus` | Waiting in castle | Similar to town wait |
| `village` | In a village | Foraging, local errands |
| `village_wait_menus` | Waiting in village | Rest, minor activities |
| `encounter` | Generic encounter | — |
| `army_wait` | Waiting in army | Army-wide activities |

### Enlisted Custom Menus

Enlisted may add custom menu contexts:

| Menu ID | Context | Purpose |
|---------|---------|---------|
| `enlisted_camp` | "My Camp" menu | Lance-specific activities |
| `enlisted_quartermaster` | Quartermaster interaction | Buy/sell, requests |
| `enlisted_command_tent` | Command/status | View status, ledger |

### Game Menu Implementation Pattern

```csharp
// In appropriate behavior (e.g., LanceLifeBehavior)

// 1. Add menu option to existing menu
private void AddMenuOptions(CampaignGameStarter starter)
{
    // Add to town wait menu
    starter.AddGameMenuOption(
        "town_wait_menus",           // parent menu
        "lance_drill_option",         // option id
        "{=LANCE_DRILL}Join the drill circle",  // display text
        args => CanShowDrillOption(args),       // condition
        args => OnDrillOptionSelected(args),    // consequence
        false,                        // is leave
        4                             // priority/order
    );
}

// 2. Condition check
private bool CanShowDrillOption(MenuCallbackArgs args)
{
    args.optionLeaveType = GameMenuOption.LeaveType.Wait;
    
    if (!IsEnlisted()) return false;
    if (GetFatigue() > MaxFatigueForDrill) return false;
    if (IsOnCooldown("drill_event")) return false;
    
    return true;
}

// 3. Consequence - can open submenu or apply effects directly
private void OnDrillOptionSelected(MenuCallbackArgs args)
{
    // Option A: Open a submenu with choices
    GameMenu.SwitchToMenu("lance_drill_submenu");
    
    // Option B: Apply effects directly (simple case)
    // Hero.MainHero.AddSkillXp(DefaultSkills.Polearm, 20);
    // AddFatigue(2);
}

// 4. If using submenu, define it
private void DefineSubmenus(CampaignGameStarter starter)
{
    starter.AddGameMenu(
        "lance_drill_submenu",
        "{=DRILL_DESC}The drill sergeant is running formation practice. How hard do you push?",
        args => OnDrillMenuInit(args)
    );
    
    starter.AddGameMenuOption(
        "lance_drill_submenu",
        "drill_hard",
        "{=DRILL_HARD}Drill until you drop",
        args => true, // always available
        args => OnDrillHard(args),
        false
    );
    
    starter.AddGameMenuOption(
        "lance_drill_submenu",
        "drill_easy",
        "{=DRILL_EASY}Pace yourself",
        args => true,
        args => OnDrillEasy(args),
        false
    );
    
    // Back option
    starter.AddGameMenuOption(
        "lance_drill_submenu",
        "drill_back",
        "{=BACK}Never mind",
        args => true,
        args => GameMenu.SwitchToMenu("town_wait_menus"),
        true // is leave
    );
}
```

---

## Inquiry Prompts

### When to Use Inquiry Prompts

✅ **Use Inquiry Prompts for:**
- Simple binary choices (yes/no, accept/decline)
- Urgent decisions that need immediate response
- Fallback when Map Incidents unavailable
- Quick reactions to events
- Confirmations
- Events with 2–3 simple options

❌ **Don't use Inquiry Prompts for:**
- Complex choices with detailed consequences
- Events that benefit from atmosphere/presentation
- Choices the player should be able to consider carefully
- Events with 4+ options

### Inquiry Prompt Implementation Pattern

```csharp
// Simple inquiry
private void ShowSimpleInquiry()
{
    InformationManager.ShowInquiry(
        new InquiryData(
            "Drill Opportunity",                    // title
            "The sergeant is looking for volunteers for extra drill. Join in?",  // text
            true,                                   // affirmative available
            true,                                   // negative available
            "Yes, I'll drill",                      // affirmative text
            "No thanks",                            // negative text
            () => OnAcceptDrill(),                  // affirmative action
            () => OnDeclineDrill()                  // negative action
        ),
        true  // pause game
    );
}

// Multi-option inquiry
private void ShowMultiOptionInquiry()
{
    var options = new List<InquiryElement>
    {
        new InquiryElement(
            "drill_hard",                           // identifier
            "Drill hard (+3 Fatigue, +30 XP)",      // text
            true,                                   // is enabled
            "Push yourself to the limit"            // hint
        ),
        new InquiryElement(
            "drill_easy",
            "Take it easy (+1 Fatigue, +15 XP)",
            true,
            "Pace yourself"
        ),
        new InquiryElement(
            "drill_skip",
            "Skip it",
            true,
            "Find something else to do"
        )
    };
    
    InformationManager.ShowMultiSelectionInquiry(
        new MultiSelectionInquiryData(
            "Drill Time",                           // title
            "How do you approach the drill?",       // description
            options,                                // elements
            true,                                   // is exit shown
            1,                                      // min selections
            1,                                      // max selections
            "Confirm",                              // affirmative text
            "Cancel",                               // negative text
            selected => OnDrillSelected(selected),  // affirmative action
            null                                    // negative action
        ),
        true  // pause game
    );
}

private void OnDrillSelected(List<InquiryElement> selected)
{
    switch (selected[0].Identifier)
    {
        case "drill_hard":
            Hero.MainHero.AddSkillXp(DefaultSkills.Polearm, 30);
            AddFatigue(3);
            break;
        case "drill_easy":
            Hero.MainHero.AddSkillXp(DefaultSkills.Polearm, 15);
            AddFatigue(1);
            break;
        case "drill_skip":
            // nothing
            break;
    }
}
```

---

## Event Types and UI Systems

### The Three Event Types

| Event Type | How It Works | Primary UI | XP Source |
|------------|--------------|------------|-----------|
| **Duty Events** | Fire based on triggers when player has the duty | Map Incident or Inquiry | Duty skill XP |
| **Training Events** | Player opts in via menu | Game Menu | Combat skill XP |
| **General Events** | Fire based on campaign state | Map Incident | Various |

### Duty Events UI Pattern

Duty events are the **primary non-combat XP source**. They should feel like "your job demands attention."

**Recommended:** Map Incident for complex duty events, Inquiry for simple ones.

```
Trigger fires (e.g., entered_town + has_quartermaster_duty)
    ↓
Check: Does player have required duty?
    ↓
YES → Fire duty event (Map Incident or Inquiry based on complexity)
    ↓
Player chooses approach
    ↓
Apply XP + consequences
```

### Training Events UI Pattern

Training events are **opt-in**. Player sees menu option, chooses to participate.

**Recommended:** Always Game Menu (wait menus).

```
Player in wait menu (town_wait_menus, etc.)
    ↓
Check: Is training available for player's formation?
    ↓
YES → Show menu option: "Join the drill" / "Practice at the range" / etc.
    ↓
Player clicks option
    ↓
Show intensity choices (submenu or immediate options)
    ↓
Apply XP + fatigue
```

### General Events UI Pattern

General events "happen to" the player regardless of duty.

**Recommended:** Map Incident for significant events, Inquiry for quick decisions.

---

## Event-to-UI Mapping

This table maps event categories to recommended UI systems:

### Duty Events

| Duty | Event Example | UI System | Rationale |
|------|---------------|-----------|-----------|
| Quartermaster | Supply Inventory | Map Incident | Complex, multiple approaches |
| Quartermaster | Quick Restock | Inquiry | Simple, time-sensitive |
| Scout | Terrain Recon | Map Incident | Significant, multiple options |
| Scout | Quick Patrol | Inquiry | Simple duty check |
| Field Medic | Battle Triage | Map Incident | Complex choices, consequences |
| Field Medic | Sick Call | Game Menu | Routine, player initiates |
| Messenger | Dispatch Run | Map Incident | Assigned task |
| Armorer | Post-Battle Repairs | Map Incident | Multiple approaches |

### Training Events (Always Game Menu)

| Formation | Event | Menu Location |
|-----------|-------|---------------|
| Infantry | Shield Wall Drill | `town_wait_menus`, `castle_wait_menus` |
| Infantry | Sparring Circle | `town_wait_menus` |
| Cavalry | Mounted Drill | `town_wait_menus`, `castle_wait_menus` |
| Cavalry | Horse Rotation | `town_wait_menus`, `village_wait_menus` |
| Archer | Target Practice | `town_wait_menus`, `castle_wait_menus` |
| Archer | Hunting Party | `village_wait_menus` |
| Naval | Boarding Drill | Ship wait menu |
| Naval | Rigging Practice | Ship wait menu |

### General Events (Non-Duty)

### General Events (Non-Duty)

| Event | UI System | Rationale |
|-------|-----------|-----------|
| Quartermaster's Offer (corruption) | Map Incident | Complex, consequential |
| The Grumbling Lance (morale) | Map Incident | Multiple approaches |
| Cover for Lance Mate (discipline) | Inquiry | Quick decision |
| Merchant's Shortcut (trade) | Map Incident or Inquiry | Depends on stakes |
| Victory Drinks | Map Incident | Post-battle, multiple options |

### Logistics & Scrounging (Non-Quartermaster)

| Event | UI System | Rationale |
|-------|-----------|-----------|
| Ordered to forage | Map Incident | Assigned duty, multiple approaches |
| Volunteer for supply run | Game Menu | Player-initiated |
| Emergency ration decision | Inquiry Prompt | Urgent, binary |
| Quartermaster interaction | Game Menu | Location-specific |
| Supply wagon breakdown | Map Incident | Happens, requires response |

### Morale & Revelry

| Event | UI System | Rationale |
|-------|-----------|-----------|
| Lance mates drinking after battle | Map Incident (`LeavingBattle`) | Post-battle event |
| Tavern opportunity | Game Menu (town) | Location-specific activity |
| Someone offers contraband | Map Incident or Inquiry | Depending on complexity |
| Morale crisis in lance | Map Incident | Significant event |
| Quick toast invitation | Inquiry Prompt | Simple yes/no |

### Corruption & Theft

| Event | UI System | Rationale |
|-------|-----------|-----------|
| Quartermaster's offer | Map Incident | Complex, consequential |
| Opportunity during raid chaos | Map Incident (`LeavingBattle` after raid) | Happens, multiple options |
| Clerk approaches with bribe | Map Incident or Inquiry | Depending on stakes |
| Notice unguarded goods | Game Menu | Player chooses to act |
| Someone snitches on you | Map Incident | Consequence event |

### Discipline & Duty

| Event | UI System | Rationale |
|-------|-----------|-----------|
| Assigned night watch | Map Incident | Duty assignment |
| Volunteer for extra duty | Game Menu (wait) | Player-initiated |
| Caught sleeping on watch | Map Incident | Consequence |
| Cover for lance mate? | Inquiry Prompt | Quick decision |
| Formal discipline hearing | Map Incident | Significant event |

### Medical & Wounded

| Event | UI System | Rationale |
|-------|-----------|-----------|
| Surgeon needs help | Map Incident (`LeavingBattle`) | Post-battle |
| Volunteer at surgeon's tent | Game Menu (wait) | Player-initiated |
| Lance mate injured, quick help? | Inquiry Prompt | Urgent |
| Herb gathering opportunity | Game Menu (village) | Location-specific |

### Siege Operations

| Event | UI System | Rationale |
|-------|-----------|-----------|
| Trench duty assignment | Map Incident (`DuringSiege`) | Assigned |
| Volunteer for assault ladder | Game Menu (siege wait) | Player choice |
| Wall collapse, react! | Inquiry Prompt | Urgent |
| Post-assault loot discipline | Map Incident | Complex choices |

### Naval Operations

| Event | UI System | Rationale |
|-------|-----------|-----------|
| Deck duty assignment | Map Incident (at sea tick) | Assigned |
| Volunteer for rigging work | Game Menu (ship wait) | Player-initiated |
| Emergency rigging snap | Inquiry Prompt or Map Incident | Urgent |
| Navigation dispute | Map Incident | Multiple options |
| Boarding drill | Game Menu (ship wait) | Optional training |

---

## Trigger-to-UI Mapping

Cross-reference: which UI systems work with which triggers?

| Trigger | Map Incident | Game Menu | Inquiry |
|---------|--------------|-----------|---------|
| `siege_started` | ✅ Best | ❌ | ⚠️ Fallback |
| `siege_ongoing` | ✅ (`DuringSiege`) | ✅ (siege wait menu) | ⚠️ Fallback |
| `siege_ended` | ✅ (`LeavingBattle`) | ❌ | ⚠️ Fallback |
| `battle_won/lost` | ✅ (`LeavingBattle`) | ❌ | ⚠️ Fallback |
| `entered_town` | ✅ (`EnteringTown`) | ✅ (town menu) | ⚠️ Fallback |
| `waiting_in_town` | ✅ (`WaitingInSettlement`) | ✅ Best (wait menu) | ⚠️ Fallback |
| `entered_village` | ✅ (`EnteringVillage`) | ✅ (village menu) | ⚠️ Fallback |
| `left_settlement` | ✅ (`Leaving*`) | ❌ | ⚠️ Fallback |
| `raid_completed` | ✅ (`LeavingBattle`) | ❌ | ⚠️ Fallback |
| `days_from_settlement` | ⚠️ (via hourly tick) | ❌ | ✅ Best |
| `at_sea` | ✅ (custom tick) | ✅ (ship menu if exists) | ✅ |
| `pay_tension_high` | ✅ (on town enter) | ✅ (town menu) | ⚠️ Fallback |
| `morale_low` | ✅ (various) | ✅ (wait menus) | ⚠️ Fallback |
| `no_combat_extended` | ⚠️ (hourly tick) | ✅ Best (wait menus) | ⚠️ Fallback |

**Legend:**
- ✅ Best — Ideal match for this trigger
- ✅ — Works well
- ⚠️ — Possible but not ideal, or fallback only
- ❌ — Not applicable

---

## Safety Checks

Before firing ANY event UI, check these conditions:

### Universal Blockers (never fire if true)

```csharp
private bool IsPlayerBusy()
{
    // Never interrupt these states
    if (Hero.MainHero.IsPrisoner) return true;
    if (Campaign.Current.ConversationManager.IsConversationFlowActive) return true;
    if (MapState.IsInBattle) return true;
    if (MapState.IsInEncounter && !IsLeavingEncounter) return true;
    
    return false;
}
```

### Map Incident Specific

```csharp
private bool CanFireMapIncident()
{
    if (IsPlayerBusy()) return true;
    if (MapState.NextIncident != null) return true;  // incident already queued
    if (IsOnGlobalCooldown()) return true;
    
    // Settlement busy check (native pattern)
    if (Settlement.CurrentSettlement?.IsUnderSiege == true) return true;
    
    return false;
}
```

### Game Menu Specific

```csharp
private bool CanShowMenuOption(MenuCallbackArgs args)
{
    if (!IsEnlisted()) return false;
    
    // Check we're in the right menu context
    if (args.MenuContext?.GameMenu?.StringId != ExpectedMenuId) return false;
    
    return true;
}
```

### Inquiry Specific

```csharp
private bool CanShowInquiry()
{
    if (IsPlayerBusy()) return true;
    
    // Don't stack inquiries
    if (InformationManager.IsAnyInquiryActive) return true;
    
    return false;
}
```

---

## Cooldown Management

### Per-Event Cooldowns

Every event should track its own cooldown:

```csharp
private Dictionary<string, CampaignTime> _eventCooldowns = new();

private void SetCooldown(string eventId, int days)
{
    _eventCooldowns[eventId] = CampaignTime.Now + CampaignTime.Days(days);
}

private bool IsOnCooldown(string eventId)
{
    if (!_eventCooldowns.TryGetValue(eventId, out var cooldownEnd))
        return false;
    
    return CampaignTime.Now < cooldownEnd;
}
```

### Global Cooldowns

Prevent event spam with category-level cooldowns:

```csharp
private Dictionary<string, CampaignTime> _categoryCooldowns = new();

// After any training event fires
private void SetCategoryCooldown(string category, int minDays, int maxDays)
{
    int days = MBRandom.RandomInt(minDays, maxDays);
    _categoryCooldowns[category] = CampaignTime.Now + CampaignTime.Days(days);
}

private bool IsCategoryOnCooldown(string category)
{
    if (!_categoryCooldowns.TryGetValue(category, out var cooldownEnd))
        return false;
    
    return CampaignTime.Now < cooldownEnd;
}
```

### Recommended Cooldowns

| Category | Per-Event | Category Global | Weekly Limit |
|----------|-----------|-----------------|--------------|
| Training/Drills | 3–5 days | 1–2 days | 3–4 |
| Logistics | 5–7 days | 2–3 days | 2 |
| Morale/Revelry | 5–7 days | 2–3 days | 2 |
| Corruption | 7–10 days | 3–5 days | 1–2 |
| Discipline | 5–7 days | 2–3 days | 2 |
| Medical | 5–7 days | 2–3 days | 2 |
| Siege (while in siege) | 2–3 days | 1 day | 4–5 |
| Naval (while at sea) | 2–3 days | 1 day | 4–5 |

### Max Events Per Term

Track total events fired per enlistment term:

```csharp
private Dictionary<string, int> _eventsThisTerm = new();

private void OnEventFired(string eventId, string category)
{
    _eventsThisTerm[eventId] = _eventsThisTerm.GetValueOrDefault(eventId, 0) + 1;
    _eventsThisTerm[$"category:{category}"] = _eventsThisTerm.GetValueOrDefault($"category:{category}", 0) + 1;
}

private bool HasExceededTermLimit(string eventId, int maxPerTerm)
{
    return _eventsThisTerm.GetValueOrDefault(eventId, 0) >= maxPerTerm;
}

// Reset on new enlistment
private void OnNewEnlistment()
{
    _eventsThisTerm.Clear();
}
```

---

## Appendix: UI Decision Flowchart

```
START: New event to implement
    │
    ▼
┌─────────────────────────────────────┐
│ Does this "happen to" the player?   │
│ (assigned, discovered, consequence) │
└─────────────────────────────────────┘
    │
    ├─ YES ──► Is it complex (3+ options, significant stakes)?
    │              │
    │              ├─ YES ──► MAP INCIDENT
    │              │
    │              └─ NO ───► Is it urgent?
    │                             │
    │                             ├─ YES ──► INQUIRY PROMPT
    │                             │
    │                             └─ NO ───► MAP INCIDENT (simpler)
    │
    └─ NO ──► Is it tied to a specific location/menu?
                   │
                   ├─ YES ──► GAME MENU
                   │
                   └─ NO ───► Is player choosing to do something?
                                  │
                                  ├─ YES ──► GAME MENU (add to wait menu)
                                  │
                                  └─ NO ───► Is it a quick reaction?
                                                 │
                                                 ├─ YES ──► INQUIRY PROMPT
                                                 │
                                                 └─ NO ───► Reconsider design
```

---

## Appendix: Event JSON with UI Specification

When defining events in `lance_stories.json`, include UI preference:

```json
{
  "id": "trench_duty",
  "title": "Trench Duty",
  "ui_system": "map_incident",
  "ui_trigger": "DuringSiege",
  "ui_fallback": "inquiry",
  "setup": "The siege lines need digging...",
  "options": [...]
}
```

```json
{
  "id": "voluntary_drill",
  "title": "Join the Drill",
  "ui_system": "game_menu",
  "ui_menu": "town_wait_menus",
  "ui_fallback": null,
  "setup": "You notice a drill circle forming...",
  "options": [...]
}
```

```json
{
  "id": "quick_cover_decision",
  "title": "Cover for Him?",
  "ui_system": "inquiry",
  "ui_fallback": null,
  "setup": "Your lance mate is about to get caught...",
  "options": [
    {"id": "cover", "text": "Cover for him"},
    {"id": "stay_out", "text": "Stay out of it"}
  ]
}
```

---

*Document version: 1.0*
*Companion to: Lance Life Events — Master Documentation*
*For use with: Enlisted mod, War Sails expansion*
