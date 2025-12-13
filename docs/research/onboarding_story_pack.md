# Onboarding Story Pack

This document contains all onboarding events for new enlistment, transfers, and returns across all tier levels.

---

## Table of Contents

1. [Overview](#overview)
2. [Detection Logic](#detection-logic)
3. [Enlisted Track (T1-4)](#enlisted-track-t1-4)
4. [Officer Track (T5-6)](#officer-track-t5-6)
5. [Commander Track (T7-9)](#commander-track-t7-9)
6. [Implementation Notes](#implementation-notes)

---

## Overview

### Delivery Metadata — All Onboarding Events

| Field | Value |
|-------|-------|
| **Delivery** | Automatic |
| **Menu** | None |
| **Triggered By** | System fires when `days_since_enlistment` + `onboarding_stage_X` flags match |
| **Presentation** | Inquiry Popup |
| **Priority** | High (fires before regular Lance Life events) |

**Implementation:** Onboarding events fire automatically via `LanceLifeEventManager.CheckOnboardingEvents()`. System checks:
1. Is player enlisted?
2. Is `ai_safe` true?
3. What onboarding stage are we on? (`onboarding_stage_1`, `_2`, `_3`, or `onboarding_complete`)
4. Does current time match event requirements?

Events fire as inquiry popups. Player does NOT initiate these from any menu. After each event completes, the stage flag advances (`stage_1` → `stage_2` → `stage_3` → `complete`).

### Onboarding Matrix

| Entry Tier | First Time | Transfer | Return |
|------------|------------|----------|--------|
| T1-4 (Enlisted) | Full introduction to lance life | Know the life, new unit | Rusty veteran, prove yourself |
| T5-6 (Officer) | First time leading | New lance to earn trust | Returning to command |
| T7-9 (Commander) | First real command | New troops, new lord | Experienced commander, new army |

### Event Structure

Each track has 3 events fired over ~3 days:
1. **Introduction** — Meet your superior/unit
2. **Social** — Establish relationships
3. **Prove Yourself** — First meaningful task/decision

### Variant System

Each event has three text variants based on situation:
- `first_time` — Full introduction, everything is new
- `transfer` — Know army life, new people
- `return` — Experienced, have to prove yourself again

Options are mostly shared, with occasional bonus options for experienced players.

---

## Detection Logic

```
ON_ENLISTMENT:
    tier = GetEntryTier()
    
    IF tier <= 4:
        track = "enlisted"
    ELSE IF tier <= 6:
        track = "officer"  
    ELSE:
        track = "commander"
    
    IF HasNeverServed():
        variant = "first_time"
    ELSE IF PreviousLord() != CurrentLord():
        variant = "transfer"
    ELSE:
        variant = "return"
    
    SetOnboardingState(track, variant)
    QueueOnboardingEvents(track, variant)
```

---

## Enlisted Track (T1-4)

> **Delivery:** Automatic inquiry popup | **Menu:** None | **Trigger:** Stage flags + time conditions

### Event: enlisted_onboard_01_meet_lance

**Fires:** Within 6 hours of enlistment
**Priority:** High (fires before random events)
**One-time:** Yes (per enlistment)

#### First Time Variant

```json
{
  "id": "enlisted_onboard_01_meet_lance",
  "category": "onboarding",
  "track": "enlisted",
  "variant": "first_time",
  "priority": "high",
  "cooldown_days": 0,
  "one_time": true,
  
  "triggers": {
    "all": ["is_enlisted", "ai_safe", "onboarding_stage_1", "days_since_enlistment < 1"]
  },
  
  "setup": "{LANCE_LEADER_RANK} {LANCE_LEADER_SHORT} looks you over like they're inspecting a horse they're not sure about buying.\n\n\"{PLAYER_NAME}, is it? You're with {LANCE_NAME} now. That means you do what I say, when I say it. You eat when I say eat. You sleep when I say sleep. You shit when I say shit.\"\n\nThe rest of the lance watches from a distance. Some smirk. Some look bored. One recruit looks as terrified as you feel.\n\n\"Understood?\"",
  
  "options": [
    {
      "id": "respectful",
      "text": "\"Understood, {LANCE_LEADER_RANK}.\"",
      "risk": "safe",
      "outcome": "{LANCE_LEADER_SHORT} gives a curt nod. \"Good. We'll see if you last the week.\" They walk off. Not warm, but not hostile. You've made no enemies today.",
      "effects": {
        "lance_reputation": 3,
        "relationship": { "lance_leader": 5 }
      }
    },
    {
      "id": "eager",
      "text": "\"Ready to prove myself, {LANCE_LEADER_RANK}.\"",
      "risk": "safe",
      "outcome": "A flicker of something — amusement? approval? — crosses {LANCE_LEADER_SHORT}'s face. \"Keen one, are you? We'll see how long that lasts.\" Someone in the lance chuckles. But it's not unfriendly.",
      "effects": {
        "lance_reputation": 5,
        "relationship": { "lance_leader": 3 }
      }
    },
    {
      "id": "quiet",
      "text": "Just nod.",
      "risk": "safe",
      "outcome": "{LANCE_LEADER_SHORT} stares at you a moment longer, then shrugs. \"Quiet type. Fine. Just don't be stupid.\" They leave. You've made no impression at all. That might be good. That might be bad.",
      "effects": {}
    },
    {
      "id": "cocky",
      "text": "\"I've handled worse than this.\"",
      "risk": "risky",
      "outcome": "The lance goes quiet. {LANCE_LEADER_SHORT}'s eyes harden. \"Have you now.\" It's not a question. \"We'll see about that.\"\n\nAs they walk away, you hear snickering from the recruits. {RECRUIT_NAME} grins at you. The veterans just shake their heads.",
      "effects": {
        "lance_reputation": -3,
        "relationship": { 
          "lance_leader": -5,
          "recruits": 5
        }
      }
    }
  ],
  
  "on_complete": {
    "set_flag": "onboarding_stage_2",
    "clear_flag": "onboarding_stage_1"
  }
}
```

#### Transfer Variant

```json
{
  "id": "enlisted_onboard_01_meet_lance",
  "category": "onboarding",
  "track": "enlisted",
  "variant": "transfer",
  "priority": "high",
  "cooldown_days": 0,
  "one_time": true,
  
  "triggers": {
    "all": ["is_enlisted", "ai_safe", "onboarding_stage_1", "days_since_enlistment < 1"]
  },
  
  "setup": "Another lord, another lance. You know how this goes.\n\n{LANCE_LEADER_RANK} {LANCE_LEADER_SHORT} looks you over. They've seen transfers before too. \"You're with {LANCE_NAME} now. I don't care who you served before or what you did there. Here, you prove yourself fresh.\"\n\nThe lance watches. Sizing you up. Wondering if you're an asset or a liability.",
  
  "options": [
    {
      "id": "respectful",
      "text": "\"Understood, {LANCE_LEADER_RANK}. Fresh start.\"",
      "risk": "safe",
      "outcome": "A hint of approval. They've dealt with transfers who brought baggage. You're not going to be one of those. \"Good. Find a spot in the tent line. We march at dawn.\"",
      "effects": {
        "lance_reputation": 5,
        "relationship": { "lance_leader": 5 }
      }
    },
    {
      "id": "professional",
      "text": "\"I know the work. Point me at it.\"",
      "risk": "safe",
      "outcome": "{LANCE_LEADER_SHORT} nods slowly. \"Experienced, are you? Good. Less hand-holding.\" They point toward the supply wagons. \"Gear check in an hour. Don't be late.\"",
      "effects": {
        "lance_reputation": 3,
        "relationship": { "lance_leader": 3 }
      },
      "rewards": {
        "xp": { "steward": 10 }
      }
    },
    {
      "id": "mention_experience",
      "text": "\"Served three years under Lord {PREVIOUS_LORD}. I know how a lance works.\"",
      "risk": "risky",
      "outcome": "\"Did you now.\" {LANCE_LEADER_SHORT}'s tone is flat. \"And yet here you are, starting over.\" The implication hangs in the air. Maybe that wasn't the right thing to say.",
      "effects": {
        "lance_reputation": -2,
        "relationship": { "lance_leader": -3 }
      }
    },
    {
      "id": "quiet",
      "text": "Just nod. You know the drill.",
      "risk": "safe",
      "outcome": "{LANCE_LEADER_SHORT} accepts your silence for what it is — a professional who doesn't need the speech. \"Tent line. Dawn. Don't make me come find you.\"",
      "effects": {
        "relationship": { "lance_leader": 2 }
      }
    }
  ],
  
  "on_complete": {
    "set_flag": "onboarding_stage_2",
    "clear_flag": "onboarding_stage_1"
  }
}
```

#### Return Variant

```json
{
  "id": "enlisted_onboard_01_meet_lance",
  "category": "onboarding",
  "track": "enlisted",
  "variant": "return",
  "priority": "high",
  "cooldown_days": 0,
  "one_time": true,
  
  "triggers": {
    "all": ["is_enlisted", "ai_safe", "onboarding_stage_1", "days_since_enlistment < 1"]
  },
  
  "setup": "Back in harness. The smell of camp, the sound of soldiers, the weight of a soldier's life settling back onto your shoulders.\n\n{LANCE_LEADER_RANK} {LANCE_LEADER_SHORT} looks you over — and pauses. They see it. The way you stand. The way you're already scanning the camp, reading the layout.\n\n\"Served before, have you?\"\n\nThe lance watches with new interest. A veteran returning to the ranks. That's either wisdom or desperation.",
  
  "options": [
    {
      "id": "honest",
      "text": "\"Aye. Took some time away. Ready to be back.\"",
      "risk": "safe",
      "outcome": "{LANCE_LEADER_SHORT} considers this. \"Time away.\" They don't ask why. Good sergeants don't. \"Well, you know what's expected then. Don't make me teach you twice.\"",
      "effects": {
        "lance_reputation": 5,
        "relationship": { "lance_leader": 5 }
      }
    },
    {
      "id": "humble",
      "text": "\"Some time ago. I'm rusty. Treat me like a recruit.\"",
      "risk": "safe",
      "outcome": "Respect flickers in {LANCE_LEADER_SHORT}'s eyes. A veteran who doesn't assume they know everything. Rare. \"Fair enough. But I'll lean on you when I need experience. That's the deal.\"",
      "effects": {
        "lance_reputation": 7,
        "relationship": { "lance_leader": 7 }
      }
    },
    {
      "id": "confident",
      "text": "\"Three campaigns. I could run a lance in my sleep.\"",
      "risk": "risky",
      "outcome": "{LANCE_LEADER_SHORT}'s expression cools. \"Could you now. Then you won't mind showing the recruits how it's done.\" They've just assigned you extra work. Well done.",
      "effects": {
        "lance_reputation": -2,
        "relationship": { "lance_leader": -5 }
      },
      "costs": { "fatigue": 2 }
    },
    {
      "id": "private",
      "text": "\"Rather not talk about before. I'm here now.\"",
      "risk": "safe",
      "outcome": "A long look. {LANCE_LEADER_SHORT} doesn't push. \"Fine. Your business. Just do your job.\" They walk off. You've got secrets. That's fine. Everyone does.",
      "effects": {}
    }
  ],
  
  "on_complete": {
    "set_flag": "onboarding_stage_2",
    "clear_flag": "onboarding_stage_1"
  }
}
```

---

### Event: enlisted_onboard_02_fire_circle

**Fires:** First evening after enlistment
**Priority:** High
**One-time:** Yes

#### First Time Variant

```json
{
  "id": "enlisted_onboard_02_fire_circle",
  "category": "onboarding",
  "track": "enlisted",
  "variant": "first_time",
  "priority": "high",
  "cooldown_days": 0,
  "one_time": true,
  
  "triggers": {
    "all": ["is_enlisted", "ai_safe", "onboarding_stage_2"],
    "time_of_day": ["evening", "dusk"]
  },
  
  "setup": "Evening falls. Cook fires bloom across the camp like orange flowers.\n\n{LANCE_NAME} gathers at their spot — a fire ringed by logs worn smooth by use. Stories overlap. Someone laughs. The smell of food makes your stomach growl.\n\nYou're standing at the edge. {VETERAN_1_NAME} notices you hovering. \"Well? You eating or not?\"\n\nThere's space near the other recruits, {RECRUIT_NAME} among them. There's an open spot closer to the veterans. And there's always the darkness beyond the firelight.",
  
  "options": [
    {
      "id": "recruits",
      "text": "Sit with the other recruits",
      "risk": "safe",
      "outcome": "You find a spot next to {RECRUIT_NAME}. They give you a nervous nod. The food is decent. The company is... green. But there's safety in shared inexperience.\n\n\"First day?\" {RECRUIT_NAME} asks. You nod. \"Same. Thought I was going to piss myself when the {LANCE_LEADER_RANK} yelled.\"\n\nYou eat. You listen. You're not alone in being new.",
      "effects": {
        "lance_reputation": 3,
        "relationship": { "recruits": 5 }
      },
      "costs": { "fatigue": -1 }
    },
    {
      "id": "veterans",
      "text": "Take the spot near the veterans",
      "risk": "risky",
      "success_chance": 0.6,
      "outcome_success": "You sit. {VETERAN_1_NAME} glances at you, then back at the fire. Nobody tells you to move. The conversation continues around you — battles you weren't part of, people you don't know.\n\nBut you're there. Listening. Learning. {SOLDIER_NAME} passes you the bread without being asked.\n\nSmall victories.",
      "outcome_failure": "\"{SOLDIER_NAME} sits there,\" someone says flatly. You look up. A veteran you don't know is staring at you. \"Find another spot.\"\n\nYou move to the recruits' section. {RECRUIT_NAME} makes room, carefully not meeting your eyes. Awkward.",
      "effects_success": {
        "lance_reputation": 5,
        "relationship": { "veterans": 5 }
      },
      "effects_failure": {
        "lance_reputation": -2
      },
      "costs": { "fatigue": -1 }
    },
    {
      "id": "help_serve",
      "text": "\"Need a hand with the food?\"",
      "risk": "safe",
      "outcome": "The soldier on cook duty looks surprised, then relieved. \"Grab that pot. Don't spill it.\"\n\nYou serve. Everyone gets a good look at you. Some nod. Some ignore you. But they all know your face now.\n\nYou eat last, but you eat well — cook's privilege. And you've met everyone without having to pick sides.",
      "effects": {
        "lance_reputation": 5
      },
      "rewards": {
        "xp": { "steward": 10, "charm": 5 }
      },
      "costs": { "fatigue": 1 }
    },
    {
      "id": "alone",
      "text": "Take your food into the darkness",
      "risk": "safe",
      "outcome": "You take your portion and find a quiet spot away from the fire. The laughter fades to murmurs. The stars are bright.\n\nYou eat alone. Watch the camp. Learn its rhythms from the outside.\n\nSome soldiers notice. They'll remember you as the one who doesn't join in. That's a reputation too.",
      "effects": {
        "lance_reputation": -3
      },
      "rewards": {
        "xp": { "roguery": 10, "scouting": 5 }
      }
    }
  ],
  
  "on_complete": {
    "set_flag": "onboarding_stage_3",
    "clear_flag": "onboarding_stage_2"
  }
}
```

#### Transfer Variant

```json
{
  "id": "enlisted_onboard_02_fire_circle",
  "category": "onboarding",
  "track": "enlisted",
  "variant": "transfer",
  "priority": "high",
  "cooldown_days": 0,
  "one_time": true,
  
  "triggers": {
    "all": ["is_enlisted", "ai_safe", "onboarding_stage_2"],
    "time_of_day": ["evening", "dusk"]
  },
  
  "setup": "Evening. The fire circle.\n\nYou know this ritual. Every lance has one — the spot where stories are told and bonds form. {LANCE_NAME}'s fire is no different. Just unfamiliar faces around familiar flames.\n\n{VETERAN_1_NAME} catches your eye. The look says: we know you're a transfer. Question is, what kind?\n\nThere's space available. Where you sit matters.",
  
  "options": [
    {
      "id": "earn_spot",
      "text": "Sit at the edge, don't presume",
      "risk": "safe",
      "outcome": "You take a modest spot. Not with the raw recruits — you're past that. But not pushing into the veteran circle either.\n\n{VETERAN_1_NAME} watches. Nods slightly. You know the game. Respect is earned, not transferred.",
      "effects": {
        "lance_reputation": 5,
        "relationship": { "veterans": 3 }
      },
      "costs": { "fatigue": -1 }
    },
    {
      "id": "tell_stories",
      "text": "Offer a story from your old unit",
      "risk": "risky",
      "success_chance": 0.7,
      "outcome_success": "You wait for a lull. Then: \"Reminds me of a time with my old lance...\"\n\nThey listen. It's a good story — you've told it before. Laughter at the right moments. {SOLDIER_NAME} slaps their knee. \"Not bad, transfer. Not bad.\"\n\nYou're still an outsider. But an interesting one.",
      "outcome_failure": "You share a story. It falls flat. Different unit, different references, jokes that don't land.\n\n\"Guess you had to be there,\" someone mutters.\n\nYou eat in uncomfortable silence.",
      "effects_success": {
        "lance_reputation": 7,
        "relationship": { "veterans": 5 }
      },
      "effects_failure": {
        "lance_reputation": -2
      },
      "rewards_success": {
        "xp": { "charm": 15 }
      },
      "costs": { "fatigue": -1 }
    },
    {
      "id": "listen",
      "text": "Listen more than you talk",
      "risk": "safe",
      "outcome": "You sit. You eat. You laugh when others laugh. You keep your mouth shut about how your old lance did things.\n\nSmart. By the end of the night, you know who matters, who to avoid, and where the real power in {LANCE_NAME} lies.\n\nInformation is worth more than friendship. For now.",
      "effects": {
        "lance_reputation": 3
      },
      "rewards": {
        "xp": { "scouting": 10, "roguery": 5 }
      },
      "costs": { "fatigue": -1 }
    },
    {
      "id": "bring_drink",
      "text": "Produce a hidden flask. \"Brought something from my last posting.\"",
      "risk": "risky",
      "success_chance": 0.8,
      "outcome_success": "Eyes light up. The flask makes the rounds. It's not great stuff, but it's stuff, and that matters.\n\n\"Alright, transfer,\" {VETERAN_1_NAME} says, wiping their mouth. \"You can stay.\"\n\nBribery works.",
      "outcome_failure": "The flask makes the rounds. Comes back empty. A few nods, but the warmth doesn't last.\n\n\"Takes more than booze,\" someone mutters.\n\nFair enough.",
      "effects_success": {
        "lance_reputation": 8,
        "relationship": { "veterans": 5 }
      },
      "effects_failure": {
        "lance_reputation": 2
      },
      "costs_success": { "heat": 1 }
    }
  ],
  
  "on_complete": {
    "set_flag": "onboarding_stage_3",
    "clear_flag": "onboarding_stage_2"
  }
}
```

#### Return Variant

```json
{
  "id": "enlisted_onboard_02_fire_circle",
  "category": "onboarding",
  "track": "enlisted",
  "variant": "return",
  "priority": "high",
  "cooldown_days": 0,
  "one_time": true,
  
  "triggers": {
    "all": ["is_enlisted", "ai_safe", "onboarding_stage_2"],
    "time_of_day": ["evening", "dusk"]
  },
  
  "setup": "The fire circle. You've sat at a hundred of these. The warmth, the smoke, the lies soldiers tell each other.\n\nYou'd almost missed it. Almost.\n\n{LANCE_NAME} has gathered. They know you're a veteran — it shows in how you move, how you don't ask obvious questions. {VETERAN_1_NAME} gestures to a spot. \"Sit. Eat.\"\n\nNot a command. An offer. They're curious about you.",
  
  "options": [
    {
      "id": "modest",
      "text": "Take the offered spot. Listen.",
      "risk": "safe",
      "outcome": "You sit where indicated. The conversation flows around you — recent battles, complaints about pay, gossip about officers.\n\nSome things never change. You find yourself smiling.\n\n{VETERAN_1_NAME} catches it. \"Good to be back?\"\n\n\"Yeah,\" you admit. \"It is.\"",
      "effects": {
        "lance_reputation": 5,
        "relationship": { "veterans": 5 }
      },
      "costs": { "fatigue": -2 }
    },
    {
      "id": "share_wisdom",
      "text": "When asked, share a story from your old campaigns",
      "risk": "safe",
      "outcome": "They ask. Veterans always do — sizing up the new old-timer.\n\nYou tell one. Not the best one. Not showing off. Just... a story. Real enough to ring true.\n\nWhen you finish, {VETERAN_1_NAME} nods slowly. \"You've seen some things.\"\n\n\"Haven't we all.\"\n\nYou're not one of them yet. But you speak the same language.",
      "effects": {
        "lance_reputation": 7,
        "relationship": { "veterans": 7 }
      },
      "rewards": {
        "xp": { "charm": 15 }
      },
      "costs": { "fatigue": -1 }
    },
    {
      "id": "mentor",
      "text": "Notice {RECRUIT_NAME} struggling. Offer quiet advice.",
      "risk": "safe",
      "outcome": "The recruit is holding their spoon wrong. Small thing. But you remember being that green.\n\n\"Like this,\" you say quietly, demonstrating. \"Keeps the heat off your fingers.\"\n\n{RECRUIT_NAME} blinks. Adjusts. \"Thanks.\"\n\n{LANCE_LEADER_SHORT} notices the exchange. Doesn't say anything. But they notice.",
      "effects": {
        "lance_reputation": 5,
        "relationship": { 
          "recruits": 7,
          "lance_leader": 3
        }
      },
      "rewards": {
        "xp": { "leadership": 10 }
      }
    },
    {
      "id": "ghosts",
      "text": "Stare into the fire. Some memories don't need sharing.",
      "risk": "safe",
      "outcome": "The fire crackles. Voices fade to background noise.\n\nYou've seen men die at fires like this. Celebrated victories that cost too much. Mourned friends whose names you're starting to forget.\n\nYou eat. You don't talk.\n\nThe lance gives you space. Veterans understand silence.",
      "effects": {
        "lance_reputation": 2
      },
      "costs": { "fatigue": -1 }
    }
  ],
  
  "on_complete": {
    "set_flag": "onboarding_stage_3",
    "clear_flag": "onboarding_stage_2"
  }
}
```

---

### Event: enlisted_onboard_03_prove_yourself

**Fires:** First morning after evening event
**Priority:** High
**One-time:** Yes

#### First Time Variant

```json
{
  "id": "enlisted_onboard_03_prove_yourself",
  "category": "onboarding",
  "track": "enlisted",
  "variant": "first_time",
  "priority": "high",
  "cooldown_days": 0,
  "one_time": true,
  
  "triggers": {
    "all": ["is_enlisted", "ai_safe", "onboarding_stage_3"],
    "time_of_day": ["dawn", "morning"]
  },
  
  "setup": "{LANCE_LEADER_SHORT} finds you before the morning meal. The look on their face says this isn't social.\n\n\"New blood gets the worst jobs. That's how it works. That's how it's always worked. That's how it'll work long after both of us are worm food.\"\n\nThey gesture at three unpleasant realities of camp life: a pile of gear that needs maintenance, a stack of wood that needs hauling, and a latrine trench that needs... attention.\n\n\"Pick one. Do it right. Don't make me come check on you.\"",
  
  "options": [
    {
      "id": "gear",
      "text": "\"I'll handle the equipment.\"",
      "risk": "safe",
      "outcome": "You spend the morning with oil, leather, and rust. It's mindless work, but there's skill in it — knowing what needs replacing, what can be salvaged, what's good enough.\n\nBy midday, the pile is sorted. {SOLDIER_NAME}'s buckle won't fail in battle because of you. Small victories.\n\n{LANCE_LEADER_SHORT} inspects your work. Says nothing. That's approval.",
      "effects": {
        "lance_reputation": 3,
        "relationship": { "lance_leader": 2 }
      },
      "rewards": {
        "xp": { "smithing": 20 }
      },
      "costs": { "fatigue": 2 }
    },
    {
      "id": "wood",
      "text": "\"I'll get the firewood.\"",
      "risk": "safe",
      "outcome": "Hauling wood. Simple. Exhausting. Invisible.\n\nYou make trip after trip. Your shoulders burn. Your hands blister. By midday, the pile is tall enough.\n\nNobody thanks you. Nobody notices. But tonight, when the fires burn warm, that's your work.\n\n{LANCE_LEADER_SHORT} glances at the wood pile. Nods once. Moves on.",
      "effects": {},
      "rewards": {
        "xp": { "athletics": 20 }
      },
      "costs": { "fatigue": 3 }
    },
    {
      "id": "latrine",
      "text": "\"I'll dig.\"",
      "risk": "safe",
      "outcome": "Latrine duty. The job nobody wants.\n\nThe smell is exactly as bad as you'd expect. Worse, maybe. But you dig. You don't complain. You do it right.\n\nWord gets around. The new blood took latrine duty without being forced. That means something.\n\n{LANCE_LEADER_SHORT} finds you at midday, covered in dirt and worse. \"Didn't expect you to pick that one.\" Almost respect in their voice. Almost.",
      "effects": {
        "lance_reputation": 7,
        "relationship": { "lance_leader": 5 }
      },
      "rewards": {
        "xp": { "steward": 15 }
      },
      "costs": { "fatigue": 2 }
    },
    {
      "id": "ask",
      "text": "\"What does the lance need most?\"",
      "risk": "safe",
      "outcome": "{LANCE_LEADER_SHORT} pauses. Considers you. \"Huh. One that thinks.\"\n\nThey assign you the gear — it's most urgent. But they remember that you asked. That you thought about the unit, not just the task.\n\n\"Maybe you'll last after all,\" they mutter, walking away.\n\nHigh praise.",
      "effects": {
        "lance_reputation": 5,
        "relationship": { "lance_leader": 7 }
      },
      "rewards": {
        "xp": { "smithing": 15, "leadership": 10 }
      },
      "costs": { "fatigue": 2 }
    }
  ],
  
  "on_complete": {
    "set_flag": "onboarding_complete",
    "clear_flag": "onboarding_stage_3"
  }
}
```

#### Transfer Variant

```json
{
  "id": "enlisted_onboard_03_prove_yourself",
  "category": "onboarding",
  "track": "enlisted",
  "variant": "transfer",
  "priority": "high",
  "cooldown_days": 0,
  "one_time": true,
  
  "triggers": {
    "all": ["is_enlisted", "ai_safe", "onboarding_stage_3"],
    "time_of_day": ["dawn", "morning"]
  },
  
  "setup": "{LANCE_LEADER_SHORT} catches you at dawn. \"Transfer, you know how this works. New to the lance means bottom of the pile. Doesn't matter what rank you held before.\"\n\nThey're testing you. Seeing if you'll pull the \"but in my old unit\" card.\n\nGear maintenance. Wood hauling. Latrine duty. The eternal trinity of grunt work.\n\n\"Pick one. Show me you're not too good for the basics.\"",
  
  "options": [
    {
      "id": "gear",
      "text": "\"Gear. I know equipment.\"",
      "risk": "safe",
      "outcome": "You work efficiently. You've done this a hundred times. The difference shows — you're faster, more thorough, spot problems others would miss.\n\n{LANCE_LEADER_SHORT} inspects your work. Finds nothing to criticize. \"Your old unit taught you right, at least.\" Grudging respect.",
      "effects": {
        "lance_reputation": 5,
        "relationship": { "lance_leader": 5 }
      },
      "rewards": {
        "xp": { "smithing": 25 }
      },
      "costs": { "fatigue": 2 }
    },
    {
      "id": "hardest",
      "text": "\"Give me the worst one.\"",
      "risk": "safe",
      "outcome": "\"Latrine it is.\" {LANCE_LEADER_SHORT} almost smiles.\n\nYou dig. You've dug before. The smell doesn't bother you anymore — or rather, you've learned to ignore it.\n\nWhen you're done, cleaner than necessary, the word spreads. Transfer took the shit job. Literally. Didn't complain.\n\nRespect is earned, not transferred. You know that.",
      "effects": {
        "lance_reputation": 10,
        "relationship": { "lance_leader": 7 }
      },
      "rewards": {
        "xp": { "steward": 15 }
      },
      "costs": { "fatigue": 2 }
    },
    {
      "id": "efficient",
      "text": "\"I'll do all three. By midday.\"",
      "risk": "risky",
      "success_chance": 0.7,
      "outcome_success": "You work like a demon. Gear, wood, latrine. Moving fast, working smart, proving a point.\n\nMidway through the morning, {VETERAN_1_NAME} stops to watch. By noon, you're done. Exhausted. But done.\n\n{LANCE_LEADER_SHORT} says nothing. But they don't assign you grunt work again.",
      "outcome_failure": "You try. God, you try. But there's not enough time, and you're not as young as you used to be.\n\nMidway through the latrine, {LANCE_LEADER_SHORT} finds you. \"Overreached, did you?\" Not cruel. Just... noting it.\n\nYou finish what you can. The point was made. Just not the one you intended.",
      "effects_success": {
        "lance_reputation": 12,
        "relationship": { "lance_leader": 5, "veterans": 5 }
      },
      "effects_failure": {
        "lance_reputation": 2,
        "relationship": { "lance_leader": -2 }
      },
      "rewards_success": {
        "xp": { "athletics": 30, "smithing": 15, "steward": 10 }
      },
      "costs": { "fatigue": 4 },
      "costs_failure": { "fatigue": 5 }
    },
    {
      "id": "delegate",
      "text": "\"I'll take gear. {RECRUIT_NAME} can help with wood.\"",
      "risk": "risky",
      "success_chance": 0.5,
      "outcome_success": "{LANCE_LEADER_SHORT}'s eyebrow rises. You're delegating? Bold.\n\nBut {RECRUIT_NAME} jumps to help — they're eager to follow someone who seems to know what they're doing. The work gets done faster.\n\n\"Huh.\" {LANCE_LEADER_SHORT} watches. \"Got some leader in you, transfer?\"",
      "outcome_failure": "\"You don't give orders here,\" {LANCE_LEADER_SHORT} says flatly. \"Not yet. You're as new as they are.\"\n\n{RECRUIT_NAME} looks away, embarrassed for you.\n\nYou do the gear alone. Message received.",
      "effects_success": {
        "lance_reputation": 5,
        "relationship": { "lance_leader": 3, "recruits": 5 }
      },
      "effects_failure": {
        "lance_reputation": -3,
        "relationship": { "lance_leader": -5 }
      },
      "rewards": {
        "xp": { "smithing": 20 }
      },
      "rewards_success": {
        "xp": { "leadership": 15 }
      },
      "costs": { "fatigue": 2 }
    }
  ],
  
  "on_complete": {
    "set_flag": "onboarding_complete",
    "clear_flag": "onboarding_stage_3"
  }
}
```

#### Return Variant

```json
{
  "id": "enlisted_onboard_03_prove_yourself",
  "category": "onboarding",
  "track": "enlisted",
  "variant": "return",
  "priority": "high",
  "cooldown_days": 0,
  "one_time": true,
  
  "triggers": {
    "all": ["is_enlisted", "ai_safe", "onboarding_stage_3"],
    "time_of_day": ["dawn", "morning"]
  },
  
  "setup": "{LANCE_LEADER_SHORT} approaches you at dawn. But there's no lecture about new blood and grunt work.\n\n\"I know you've done this before. Question is, are you still sharp, or did peace make you soft?\"\n\nThey gesture at the camp. \"Something needs doing. You pick. Show me what kind of soldier came back.\"",
  
  "options": [
    {
      "id": "grunt_work",
      "text": "\"Grunt work. Same as any recruit. I'm not above it.\"",
      "risk": "safe",
      "outcome": "You take the latrine duty. The worst one. Deliberately.\n\n{LANCE_LEADER_SHORT} watches you work — not like a soldier proving a point, but like a man who knows that pride is a luxury.\n\n\"Alright,\" they say, when you're done. \"You're still one of us.\"",
      "effects": {
        "lance_reputation": 10,
        "relationship": { "lance_leader": 10 }
      },
      "rewards": {
        "xp": { "steward": 15 }
      },
      "costs": { "fatigue": 2 }
    },
    {
      "id": "skills",
      "text": "\"Put me where my experience helps. Gear inspection.\"",
      "risk": "safe",
      "outcome": "You work the gear pile. But you do more than maintain — you teach. {RECRUIT_NAME} watches you identify a stress crack invisible to untrained eyes.\n\n\"That would have broken in battle,\" you explain. \"Learn to see it.\"\n\n{LANCE_LEADER_SHORT} observes. \"Maybe we got lucky with this one,\" they mutter to {SECOND_SHORT}.",
      "effects": {
        "lance_reputation": 7,
        "relationship": { "lance_leader": 5, "recruits": 5 }
      },
      "rewards": {
        "xp": { "smithing": 25, "leadership": 15 }
      },
      "costs": { "fatigue": 2 }
    },
    {
      "id": "problem",
      "text": "\"What's the real problem right now? Not the busy work — the actual issue.\"",
      "risk": "safe",
      "outcome": "{LANCE_LEADER_SHORT} pauses. Studies you.\n\n\"Supply count's off. Someone's skimming or the quartermasters can't count. Either way, I need it fixed before the captain notices.\"\n\nA real problem. Not grunt work. They're trusting you.\n\nYou spend the morning in the supply tents. By noon, you've found the discrepancy — honest mistake, not theft. The quartermaster owes you a favor. {LANCE_LEADER_SHORT} owes you more.\n\n\"Good to have you back,\" they admit.",
      "effects": {
        "lance_reputation": 10,
        "relationship": { "lance_leader": 10 }
      },
      "rewards": {
        "xp": { "steward": 30, "trade": 15 }
      },
      "costs": { "fatigue": 2 }
    },
    {
      "id": "all",
      "text": "\"Dawn to dusk. Whatever needs doing. All of it.\"",
      "risk": "risky",
      "success_chance": 0.8,
      "outcome_success": "You work. And work. And work.\n\nGear. Wood. Latrine. Supply count. Training the recruits on basic maintenance. Helping the cook.\n\nBy dusk, you're dead on your feet. But the message is clear: you didn't come back to coast.\n\n{LANCE_NAME} noticed. They all noticed.\n\n\"Trying to make us look bad?\" {VETERAN_1_NAME} jokes. But they're smiling.",
      "outcome_failure": "You push too hard. Halfway through the afternoon, your body reminds you that you're not twenty anymore.\n\n{LANCE_LEADER_SHORT} finds you sitting, breathing hard. \"Pace yourself, old-timer. No one doubts you. Don't break yourself proving nothing.\"",
      "effects_success": {
        "lance_reputation": 15,
        "relationship": { "lance_leader": 7, "veterans": 5 }
      },
      "effects_failure": {
        "lance_reputation": 5,
        "relationship": { "lance_leader": 3 }
      },
      "rewards_success": {
        "xp": { "athletics": 25, "smithing": 20, "steward": 20 }
      },
      "costs": { "fatigue": 5 },
      "costs_failure": { "fatigue": 4 }
    }
  ],
  
  "on_complete": {
    "set_flag": "onboarding_complete",
    "clear_flag": "onboarding_stage_3"
  }
}
```

---

## Officer Track (T5-6)

> **Delivery:** Automatic inquiry popup | **Menu:** None | **Trigger:** Stage flags + tier check

### Event: officer_onboard_01_shoulder_tabs

**Fires:** Within 6 hours of promotion/enlistment at officer tier
**Priority:** High
**One-time:** Yes

#### First Time Variant (Promoted from Within)

```json
{
  "id": "officer_onboard_01_shoulder_tabs",
  "category": "onboarding",
  "track": "officer",
  "variant": "first_time",
  "priority": "high",
  "cooldown_days": 0,
  "one_time": true,
  
  "triggers": {
    "all": ["is_enlisted", "ai_safe", "tier >= 5", "tier <= 6", "onboarding_stage_1", "days_since_promotion < 1"]
  },
  
  "setup": "The shoulder tabs feel heavier than they should. Just cloth and thread, but they change everything.\n\n{LANCE_LEADER_SHORT} — no, they're {SECOND_SHORT} now, your sergeant — stands before you. The same person who shouted at you as a recruit. Who worked you until you dropped.\n\nNow they salute.\n\n\"Orders, {PLAYER_RANK}?\"\n\nThe lance watches. Your lance now. {VETERAN_1_NAME}. {SOLDIER_NAME}. {RECRUIT_NAME}. Men and women you ate with, dug latrines with, bled beside.\n\nNow they wait for you to tell them what to do.",
  
  "options": [
    {
      "id": "steady",
      "text": "\"At ease, {SECOND_RANK}. We know each other. That doesn't change.\"",
      "risk": "safe",
      "outcome": "The tension eases slightly. {SECOND_SHORT} nods. \"Aye, {PLAYER_RANK}. But it does change. That's how it works.\"\n\nThey're right. It does. But you've signaled that you're not going to be an ass about it.\n\nSmall mercies.",
      "effects": {
        "lance_reputation": 7,
        "relationship": { "second": 5, "veterans": 3 }
      }
    },
    {
      "id": "formal",
      "text": "\"Carry on with morning duties, {SECOND_RANK}. Report to me after.\"",
      "risk": "safe",
      "outcome": "Crisp. Professional. By the book.\n\n{SECOND_SHORT}'s expression doesn't flicker. \"Aye, {PLAYER_RANK}.\"\n\nThe lance disperses. You've drawn a line. Clear boundaries. That's not a bad thing. But it's a thing.",
      "effects": {
        "relationship": { "second": 0 }
      },
      "rewards": {
        "xp": { "leadership": 15 }
      }
    },
    {
      "id": "acknowledge",
      "text": "\"This is strange for both of us. We'll figure it out.\"",
      "risk": "safe",
      "outcome": "{SECOND_SHORT} almost smiles. \"Strange is one word for it.\"\n\nThe lance relaxes. You've acknowledged the elephant. Some things will be awkward. That's fine. You're human.\n\n\"I'll have the duty roster for you within the hour, {PLAYER_RANK},\" {SECOND_SHORT} says. Back to business. But the air is clearer.",
      "effects": {
        "lance_reputation": 10,
        "relationship": { "second": 7, "veterans": 5, "recruits": 3 }
      },
      "rewards": {
        "xp": { "charm": 15 }
      }
    },
    {
      "id": "uncertain",
      "text": "Freeze. You don't know what to say.",
      "risk": "risky",
      "outcome": "The silence stretches. {SECOND_SHORT} watches. Waits.\n\nFinally, they take pity. \"Perhaps we should discuss the roster, {PLAYER_RANK}. Privately.\"\n\nThey're covering for you. The lance pretends not to notice your hesitation.\n\nYou'll have to do better. But not everyone does better on the first day.",
      "effects": {
        "lance_reputation": -3,
        "relationship": { "second": 5 }
      }
    }
  ],
  
  "on_complete": {
    "set_flag": "onboarding_stage_2",
    "clear_flag": "onboarding_stage_1"
  }
}
```

#### Transfer Variant

```json
{
  "id": "officer_onboard_01_shoulder_tabs",
  "category": "onboarding",
  "track": "officer",
  "variant": "transfer",
  "priority": "high",
  "cooldown_days": 0,
  "one_time": true,
  
  "triggers": {
    "all": ["is_enlisted", "ai_safe", "tier >= 5", "tier <= 6", "onboarding_stage_1", "days_since_enlistment < 1"]
  },
  
  "setup": "New lord. New lance. But the same shoulder tabs.\n\n{SECOND_SHORT} salutes you. A stranger. Their eyes measure you the way all sergeants measure new officers: will this one get us killed?\n\n\"Welcome to {LANCE_NAME}, {PLAYER_RANK}. I'm {SECOND_RANK} {SECOND_SHORT}. I've been running this lance for two years.\"\n\nSubtext: I know these men. You don't. Tread carefully.\n\nThe lance watches. Your lance now. Men who don't know you. Don't trust you. Not yet.",
  
  "options": [
    {
      "id": "respect_experience",
      "text": "\"Two years. Good. I'll need your experience, {SECOND_RANK}.\"",
      "risk": "safe",
      "outcome": "The right answer. {SECOND_SHORT}'s shoulders relax slightly.\n\n\"I'll have reports for you within the hour, {PLAYER_RANK}. Roster, readiness, any problems you should know about.\"\n\nYou've signaled that you're not going to override two years of experience on day one. Smart.",
      "effects": {
        "lance_reputation": 5,
        "relationship": { "second": 10 }
      },
      "rewards": {
        "xp": { "leadership": 15, "charm": 10 }
      }
    },
    {
      "id": "assert",
      "text": "\"I've led lances before. Walk me through your current procedures.\"",
      "risk": "safe",
      "outcome": "Professional. Clear. You're not dismissing their experience, but you're not deferring either.\n\n{SECOND_SHORT} nods slowly. \"This way, {PLAYER_RANK}.\"\n\nThey spend the morning showing you how {LANCE_NAME} operates. By noon, you know who to trust and what needs fixing.",
      "effects": {
        "relationship": { "second": 5 }
      },
      "rewards": {
        "xp": { "leadership": 20 }
      }
    },
    {
      "id": "prove_yourself",
      "text": "\"I know I need to earn your trust. Just give me a chance.\"",
      "risk": "risky",
      "success_chance": 0.7,
      "outcome_success": "{SECOND_SHORT} studies you. Then: \"Fair enough, {PLAYER_RANK}. Fair enough.\"\n\nHonesty buys more than bluster. The lance saw you admit you're the new one. That takes some courage.",
      "outcome_failure": "\"Due respect, {PLAYER_RANK}, but officers don't ask for chances. They take command.\"\n\n{SECOND_SHORT}'s tone isn't hostile, but it's not warm either. You've shown uncertainty where you should've shown confidence.",
      "effects_success": {
        "lance_reputation": 7,
        "relationship": { "second": 7 }
      },
      "effects_failure": {
        "lance_reputation": -2,
        "relationship": { "second": -3 }
      },
      "rewards_success": {
        "xp": { "charm": 15 }
      }
    },
    {
      "id": "inspect",
      "text": "\"Form the lance for inspection, {SECOND_RANK}. Now.\"",
      "risk": "risky",
      "success_chance": 0.5,
      "outcome_success": "The order comes out crisp. {SECOND_SHORT}'s eyebrows rise, but they turn and bellow orders.\n\nMinutes later, {LANCE_NAME} stands in formation. You walk the line. Check gear. Ask names. Make eye contact.\n\nIt's a power move. But you do it well. By the end, they know you're not just a name on a roster.",
      "outcome_failure": "The order comes out crisp. Too crisp. Too eager.\n\n{SECOND_SHORT} obeys. But the inspection is awkward — you don't know what to look for in this unit's gear, and it shows.\n\n\"Perhaps a brief orientation first, {PLAYER_RANK}?\" {SECOND_SHORT} suggests quietly.\n\nYou've shown your hand too early.",
      "effects_success": {
        "lance_reputation": 5,
        "relationship": { "second": 3 }
      },
      "effects_failure": {
        "lance_reputation": -5,
        "relationship": { "second": -5 }
      },
      "rewards_success": {
        "xp": { "leadership": 20 }
      }
    }
  ],
  
  "on_complete": {
    "set_flag": "onboarding_stage_2",
    "clear_flag": "onboarding_stage_1"
  }
}
```

#### Return Variant

```json
{
  "id": "officer_onboard_01_shoulder_tabs",
  "category": "onboarding",
  "track": "officer",
  "variant": "return",
  "priority": "high",
  "cooldown_days": 0,
  "one_time": true,
  
  "triggers": {
    "all": ["is_enlisted", "ai_safe", "tier >= 5", "tier <= 6", "onboarding_stage_1", "days_since_enlistment < 1"]
  },
  
  "setup": "The shoulder tabs again. You'd thought you'd left them behind.\n\n{SECOND_SHORT} salutes. They don't know you, but they've been told: this one's commanded before. Came back for some reason.\n\n\"Welcome to {LANCE_NAME}, {PLAYER_RANK}. I'm {SECOND_RANK} {SECOND_SHORT}.\"\n\nNo subtext about experience. They're giving you the benefit of the doubt. For now.\n\nThe lance watches. Wondering why someone would come back to this life.",
  
  "options": [
    {
      "id": "honest",
      "text": "\"I've done this before. Been away. Needed to come back.\"",
      "risk": "safe",
      "outcome": "{SECOND_SHORT} nods slowly. They don't ask why. Good sergeants never do.\n\n\"Then you know how it works, {PLAYER_RANK}. I'll bring you up to speed on {LANCE_NAME}'s particulars.\"\n\nProfessional. Clean. A good start.",
      "effects": {
        "relationship": { "second": 7 }
      },
      "rewards": {
        "xp": { "leadership": 10 }
      }
    },
    {
      "id": "rusty",
      "text": "\"It's been a while. I might be rusty. Lean on me when I need it.\"",
      "risk": "safe",
      "outcome": "Rare for an officer to admit weakness. {SECOND_SHORT}'s respect visibly increases.\n\n\"We'll get you back up to speed, {PLAYER_RANK}. That's what sergeants are for.\"\n\nA partnership, not a hierarchy. That can work.",
      "effects": {
        "lance_reputation": 5,
        "relationship": { "second": 10 }
      },
      "rewards": {
        "xp": { "charm": 15 }
      }
    },
    {
      "id": "sharp",
      "text": "\"I'm not rusty. I'm ready. Show me what you've got.\"",
      "risk": "safe",
      "outcome": "Confidence. Maybe even true.\n\n{SECOND_SHORT} accepts it at face value. \"Then let's get to work, {PLAYER_RANK}.\"\n\nYou spend the morning proving you remember how this works. You do. Muscle memory kicks in. The commands come naturally.\n\nYou're back.",
      "effects": {
        "relationship": { "second": 5 }
      },
      "rewards": {
        "xp": { "leadership": 20 }
      }
    },
    {
      "id": "private",
      "text": "\"Just... needed to be useful again. We'll leave it at that.\"",
      "risk": "safe",
      "outcome": "{SECOND_SHORT} holds your gaze for a moment. Then nods.\n\n\"Understood, {PLAYER_RANK}. Your reasons are your own.\"\n\nSome things don't need explaining. You're here. That's what matters.",
      "effects": {
        "relationship": { "second": 5 }
      }
    }
  ],
  
  "on_complete": {
    "set_flag": "onboarding_stage_2",
    "clear_flag": "onboarding_stage_1"
  }
}
```

---

### Event: officer_onboard_02_first_command

**Fires:** Day 1-2
**Priority:** High
**One-time:** Yes

```json
{
  "id": "officer_onboard_02_first_command",
  "category": "onboarding",
  "track": "officer",
  "variant": "all",
  "priority": "high",
  "cooldown_days": 0,
  "one_time": true,
  
  "triggers": {
    "all": ["is_enlisted", "ai_safe", "tier >= 5", "tier <= 6", "onboarding_stage_2"],
    "time_of_day": ["morning", "afternoon"]
  },
  
  "setup": "First decision that's actually yours to make.\n\nThe army's moving. {SECOND_SHORT} approaches with a question: where does {LANCE_NAME} make camp tonight?\n\n\"There's a spot by the stream — good water, but exposed. Or there's the tree line — sheltered, but further from the main camp.\"\n\nThey have an opinion. You can see it in their eyes. But they're waiting for you to decide.\n\nThe lance watches. Your first real order.",
  
  "options": [
    {
      "id": "stream",
      "text": "\"The stream. Water's more important.\"",
      "risk": "safe",
      "outcome": "You give the order. {SECOND_SHORT} nods — they agree, you can tell.\n\nBy evening, {LANCE_NAME} has the best water access in the company. Some grumbling about the exposed position, but full waterskins quiet most complaints.\n\n\"Good call, {PLAYER_RANK},\" {SECOND_SHORT} says. Quietly. Privately. But they said it.",
      "effects": {
        "lance_reputation": 3,
        "relationship": { "second": 5 }
      },
      "rewards": {
        "xp": { "tactics": 20 }
      }
    },
    {
      "id": "trees",
      "text": "\"The tree line. I don't like being exposed.\"",
      "risk": "safe",
      "outcome": "{SECOND_SHORT}'s expression flickers — they would've picked the stream. But they relay the order without comment.\n\nThat night, the lance hauls water from further away. Grumbling. But when a sudden rain blows through, {LANCE_NAME} stays dry while the stream camp gets soaked.\n\nLucky? Good instincts? Either way, you made the call.",
      "effects": {
        "lance_reputation": 3
      },
      "rewards": {
        "xp": { "tactics": 15, "scouting": 10 }
      }
    },
    {
      "id": "ask_sergeant",
      "text": "\"What do you recommend, {SECOND_RANK}?\"",
      "risk": "safe",
      "outcome": "\"Stream, {PLAYER_RANK}. Water's worth the exposure.\"\n\nYou nod. \"Stream it is.\"\n\nYou've shown you value their experience. Some officers would see that as weakness. {SECOND_SHORT} sees it as wisdom.\n\n\"I'll get the lance moving,\" they say. Pleased.",
      "effects": {
        "lance_reputation": 5,
        "relationship": { "second": 7 }
      },
      "rewards": {
        "xp": { "tactics": 15, "charm": 10 }
      }
    },
    {
      "id": "ask_then_opposite",
      "text": "\"What would you do?\" Then pick the opposite.",
      "risk": "risky",
      "outcome": "\"Stream,\" {SECOND_SHORT} says.\n\n\"Tree line,\" you decide.\n\n{SECOND_SHORT}'s jaw tightens slightly. You've made a point: you're in command. Maybe too obviously.\n\nThe lance notices. They always notice.",
      "effects": {
        "relationship": { "second": -5 }
      },
      "rewards": {
        "xp": { "leadership": 15 }
      }
    }
  ],
  
  "on_complete": {
    "set_flag": "onboarding_stage_3",
    "clear_flag": "onboarding_stage_2"
  }
}
```

---

### Event: officer_onboard_03_tested

**Fires:** Day 2-3
**Priority:** High
**One-time:** Yes

```json
{
  "id": "officer_onboard_03_tested",
  "category": "onboarding",
  "track": "officer",
  "variant": "all",
  "priority": "high",
  "cooldown_days": 0,
  "one_time": true,
  
  "triggers": {
    "all": ["is_enlisted", "ai_safe", "tier >= 5", "tier <= 6", "onboarding_stage_3"],
    "time_of_day": ["morning", "afternoon", "evening"]
  },
  
  "setup": "It was bound to happen. Someone testing the new officer.\n\n{SOLDIER_NAME} didn't show for morning muster. When {SECOND_SHORT} found them, they were still asleep — or pretending to be.\n\nNow they're standing before you. Not quite at attention. Not quite disrespectful. Walking the line.\n\n\"Overslept, {PLAYER_RANK}.\" No apology. No excuse. Just the flat statement.\n\n{SECOND_SHORT} watches. The lance watches. Your move.",
  
  "options": [
    {
      "id": "by_the_book",
      "text": "\"Extra duty. Three days. Dismissed.\"",
      "risk": "safe",
      "outcome": "By the book. Fair. {SOLDIER_NAME} knows the rules; they broke them.\n\n\"Aye, {PLAYER_RANK}.\" They salute — properly this time — and go.\n\n{SECOND_SHORT} nods slightly. \"That's how it's done.\"\n\nThe lance knows where the line is now. Good.",
      "effects": {
        "lance_reputation": 5,
        "relationship": { "second": 5 }
      },
      "rewards": {
        "xp": { "leadership": 25 }
      }
    },
    {
      "id": "harsh",
      "text": "\"Extra duty. A week. And you're on latrine until I say otherwise.\"",
      "risk": "risky",
      "success_chance": 0.6,
      "outcome_success": "Harsh. But clear.\n\n{SOLDIER_NAME}'s eyes harden, but they don't argue. \"Aye, {PLAYER_RANK}.\"\n\nThe lance got the message. Nobody tests you twice.",
      "outcome_failure": "{SECOND_SHORT} clears their throat. \"Perhaps a bit much for a first offense, {PLAYER_RANK}?\"\n\nThey're covering for you. In front of everyone. You've overreached, and now you either back down or dig in.\n\nEither way, you've lost something.",
      "effects_success": {
        "relationship": { "soldier": -10 }
      },
      "effects_failure": {
        "lance_reputation": -5,
        "relationship": { "second": -5 }
      },
      "rewards_success": {
        "xp": { "leadership": 20 }
      }
    },
    {
      "id": "understand",
      "text": "\"This the first time, {SECOND_RANK}?\"",
      "risk": "safe",
      "outcome": "{SECOND_SHORT} shakes their head. \"Second this month.\"\n\nYou turn back to {SOLDIER_NAME}. \"Sounds like a pattern. Extra duty. Three days. Third time, it won't be me you're answering to.\"\n\nFair. Warned. Clear consequences.\n\n{SOLDIER_NAME} swallows. \"Aye, {PLAYER_RANK}.\"",
      "effects": {
        "lance_reputation": 7,
        "relationship": { "second": 7 }
      },
      "rewards": {
        "xp": { "leadership": 20, "charm": 10 }
      }
    },
    {
      "id": "let_sergeant",
      "text": "\"Handle it, {SECOND_RANK}.\"",
      "risk": "risky",
      "success_chance": 0.5,
      "outcome_success": "{SECOND_SHORT} nods and turns to {SOLDIER_NAME} with a look that could strip paint.\n\nWhat follows is a masterclass in sergeant-level discipline. You watch. Learn.\n\nSometimes delegating is the right call. You trusted your sergeant. They delivered.",
      "outcome_failure": "{SECOND_SHORT} hesitates. They expected you to handle it.\n\n\"Aye, {PLAYER_RANK}.\"\n\nThey deal with it, but the lance saw you pass the buck. Officers make decisions. That's the job.\n\nNot a good look.",
      "effects_success": {
        "relationship": { "second": 5 }
      },
      "effects_failure": {
        "lance_reputation": -5,
        "relationship": { "second": -3 }
      },
      "rewards_success": {
        "xp": { "leadership": 15 }
      }
    }
  ],
  
  "on_complete": {
    "set_flag": "onboarding_complete",
    "clear_flag": "onboarding_stage_3"
  }
}
```

---

## Commander Track (T7-9)

> **Delivery:** Automatic inquiry popup | **Menu:** None | **Trigger:** Stage flags + tier check

### Event: commander_onboard_01_commission

**Fires:** Within 6 hours of promotion/enlistment at commander tier
**Priority:** High
**One-time:** Yes

#### First Time Variant

```json
{
  "id": "commander_onboard_01_commission",
  "category": "onboarding",
  "track": "commander",
  "variant": "first_time",
  "priority": "high",
  "cooldown_days": 0,
  "one_time": true,
  
  "triggers": {
    "all": ["is_enlisted", "ai_safe", "tier >= 7", "onboarding_stage_1", "days_since_promotion < 1"]
  },
  
  "setup": "{LORD_TITLE} {LORD_NAME}'s command tent. You've been summoned.\n\nThey don't waste time with pleasantries. A map spread on the table. Markers showing positions.\n\n\"I'm giving you soldiers. Not many — {TROOP_COUNT} to start. Prove you can handle them, and there will be more.\"\n\nThey look up from the map. Eyes weighing you.\n\n\"These men will live or die by your decisions. Not mine. Yours. Do you understand what that means?\"\n\nThis isn't a lance anymore. This is real command. Real soldiers in your party. Real consequences.",
  
  "options": [
    {
      "id": "ready",
      "text": "\"I understand, {LORD_TITLE}. I won't waste them.\"",
      "risk": "safe",
      "outcome": "{LORD_NAME} nods slowly. \"See that you don't.\"\n\nThey return to the map, pointing out your area of responsibility. You listen. You learn. When you leave, you're carrying the weight of {TROOP_COUNT} lives.\n\nIt feels heavier than you expected.",
      "effects": {
        "relationship": { "lord": 5 }
      },
      "rewards": {
        "xp": { "leadership": 25 }
      }
    },
    {
      "id": "questions",
      "text": "\"What resources do I have? What's the chain of command?\"",
      "risk": "safe",
      "outcome": "Practical questions. {LORD_NAME} approves — or at least doesn't disapprove.\n\nThey spend an hour explaining the structure. Who you report to. What authority you actually have. What lines not to cross.\n\nYou leave with knowledge, not just soldiers.",
      "effects": {
        "relationship": { "lord": 3 }
      },
      "rewards": {
        "xp": { "leadership": 20, "tactics": 15 }
      }
    },
    {
      "id": "confident",
      "text": "\"I've been waiting for this, {LORD_TITLE}. I won't let you down.\"",
      "risk": "risky",
      "success_chance": 0.7,
      "outcome_success": "Confidence. Some lords want that.\n\n{LORD_NAME}'s expression doesn't change, but something shifts. \"Confidence is good. Overconfidence gets men killed. Remember the difference.\"\n\nA warning wrapped in acceptance. You'll take it.",
      "outcome_failure": "\"Waiting for it?\" {LORD_NAME}'s voice cools. \"This isn't a reward. It's a burden. The men you command don't exist for your ambition.\"\n\nYou've misjudged your audience. Badly.\n\n\"Dismissed. Prove me wrong about you.\"",
      "effects_success": {
        "relationship": { "lord": 3 }
      },
      "effects_failure": {
        "relationship": { "lord": -7 }
      },
      "rewards_success": {
        "xp": { "leadership": 15 }
      }
    },
    {
      "id": "honest",
      "text": "\"I'll do my best, {LORD_TITLE}. That's all I can promise.\"",
      "risk": "safe",
      "outcome": "A hint of something — respect? — flickers across {LORD_NAME}'s face.\n\n\"Honest. I like that. Most commanders promise victory. Few deliver.\"\n\nThey tap the map. \"Your best will have to be enough. Now let me show you what I need.\"",
      "effects": {
        "relationship": { "lord": 7 }
      },
      "rewards": {
        "xp": { "charm": 15, "leadership": 15 }
      }
    }
  ],
  
  "on_complete": {
    "set_flag": "onboarding_stage_2",
    "clear_flag": "onboarding_stage_1"
  }
}
```

#### Transfer Variant

```json
{
  "id": "commander_onboard_01_commission",
  "category": "onboarding",
  "track": "commander",
  "variant": "transfer",
  "priority": "high",
  "cooldown_days": 0,
  "one_time": true,
  
  "triggers": {
    "all": ["is_enlisted", "ai_safe", "tier >= 7", "onboarding_stage_1", "days_since_enlistment < 1"]
  },
  
  "setup": "New lord. New army. Same rank.\n\n{LORD_TITLE} {LORD_NAME} looks you over. They know you commanded for someone else. They're wondering why you're here now.\n\n\"I don't know what your previous lord tolerated. Here, I have expectations.\"\n\nThe map on the table. {TROOP_COUNT} soldiers to command. Familiar weight, unfamiliar circumstances.\n\n\"Prove yourself. I don't inherit trust.\"",
  
  "options": [
    {
      "id": "professional",
      "text": "\"I understand, {LORD_TITLE}. I'll earn my place.\"",
      "risk": "safe",
      "outcome": "The right answer. {LORD_NAME} nods.\n\n\"See that you do. Your soldiers are waiting. Don't keep them.\"\n\nYou're dismissed. The relationship starts at zero. But at least it's not negative.",
      "effects": {
        "relationship": { "lord": 3 }
      },
      "rewards": {
        "xp": { "leadership": 15 }
      }
    },
    {
      "id": "record",
      "text": "\"My record speaks for itself, {LORD_TITLE}. Ask around.\"",
      "risk": "risky",
      "success_chance": 0.5,
      "outcome_success": "\"I already did.\" {LORD_NAME}'s expression is unreadable. \"Why do you think you're getting soldiers instead of a tent and a latrine?\"\n\nThey've done their homework. Good. That means they know what you're worth.",
      "outcome_failure": "\"Records are paper. Battle is blood.\" {LORD_NAME}'s voice hardens. \"Your past means nothing here. Your future is what I'm buying.\"\n\nYou've reminded them that you have history elsewhere. That cuts both ways.",
      "effects_success": {
        "relationship": { "lord": 5 }
      },
      "effects_failure": {
        "relationship": { "lord": -5 }
      },
      "rewards_success": {
        "xp": { "charm": 15 }
      }
    },
    {
      "id": "humble",
      "text": "\"Fresh start. Tell me how you want things done.\"",
      "risk": "safe",
      "outcome": "{LORD_NAME} relaxes slightly. You're not going to be difficult.\n\n\"Discipline. Results. No surprises.\" Simple expectations. Clear.\n\n\"Meet your officers. Learn the structure. We march in two days.\"\n\nYou've got two days to become part of this army.",
      "effects": {
        "relationship": { "lord": 5 }
      },
      "rewards": {
        "xp": { "leadership": 20 }
      }
    },
    {
      "id": "why_here",
      "text": "\"You're wondering why I left my last lord. Ask.\"",
      "risk": "risky",
      "success_chance": 0.6,
      "outcome_success": "{LORD_NAME} studies you. Then: \"Alright. Why?\"\n\nYou tell them. Whatever the truth is. They listen.\n\n\"Fair enough,\" they say when you finish. \"Your reasons are your own. Just don't make me one of them.\"\n\nRespect, maybe. Or at least understanding.",
      "outcome_failure": "\"Your past is your business. Your future is mine.\" {LORD_NAME} isn't interested.\n\n\"Get to your soldiers, Commander.\"\n\nThe door closes. You've offered vulnerability to someone who didn't want it.",
      "effects_success": {
        "relationship": { "lord": 7 }
      },
      "effects_failure": {
        "relationship": { "lord": -3 }
      },
      "rewards_success": {
        "xp": { "charm": 20 }
      }
    }
  ],
  
  "on_complete": {
    "set_flag": "onboarding_stage_2",
    "clear_flag": "onboarding_stage_1"
  }
}
```

#### Return Variant

```json
{
  "id": "commander_onboard_01_commission",
  "category": "onboarding",
  "track": "commander",
  "variant": "return",
  "priority": "high",
  "cooldown_days": 0,
  "one_time": true,
  
  "triggers": {
    "all": ["is_enlisted", "ai_safe", "tier >= 7", "onboarding_stage_1", "days_since_enlistment < 1"]
  },
  
  "setup": "You've done this before. Stood in a lord's tent. Been given soldiers. Carried that weight.\n\nYou'd put it down. Walked away. Tried to be something else.\n\nNow here you are again.\n\n{LORD_TITLE} {LORD_NAME} watches you. They know your history — someone with your experience doesn't appear from nowhere.\n\n\"Back in harness, Commander?\"\n\n{TROOP_COUNT} soldiers. Yours again.",
  
  "options": [
    {
      "id": "needed",
      "text": "\"Couldn't stay away. Some men aren't meant for peace.\"",
      "risk": "safe",
      "outcome": "{LORD_NAME} nods slowly. \"I know the type.\"\n\nDo they? Maybe. Or maybe they just know soldiers.\n\n\"Your men are waiting. Try not to get them killed.\"\n\nThe old weight settles onto your shoulders. Familiar. Heavy. Home.",
      "effects": {
        "relationship": { "lord": 5 }
      },
      "rewards": {
        "xp": { "leadership": 20 }
      }
    },
    {
      "id": "rusty",
      "text": "\"I might be rusty, {LORD_TITLE}. It's been a while.\"",
      "risk": "safe",
      "outcome": "\"Rust comes off.\" {LORD_NAME} gestures at the map. \"Battle has a way of polishing.\"\n\nThey spend time going over the situation. What they need. What you'll be doing. It's coming back. The language, the thinking, the weight.\n\nRust comes off. They're right.",
      "effects": {
        "relationship": { "lord": 7 }
      },
      "rewards": {
        "xp": { "tactics": 20, "leadership": 15 }
      }
    },
    {
      "id": "sharp",
      "text": "\"Sharp as ever, {LORD_TITLE}. Just needed a purpose.\"",
      "risk": "safe",
      "outcome": "\"Purpose.\" {LORD_NAME} considers the word. \"I can provide that. Victory, defeat, meaning — that's for you to find.\"\n\nPhilosophical for a commander giving orders. But not wrong.\n\n\"Your soldiers await, Commander. Give them something to believe in.\"",
      "effects": {
        "relationship": { "lord": 5 }
      },
      "rewards": {
        "xp": { "leadership": 25 }
      }
    },
    {
      "id": "reasons",
      "text": "\"Let's just say I have unfinished business.\"",
      "risk": "safe",
      "outcome": "{LORD_NAME} doesn't ask what business. Lords don't, usually.\n\n\"Finish it, then. But not at my army's expense.\"\n\nFair enough. Your ghosts are your problem. The soldiers are the job.",
      "effects": {
        "relationship": { "lord": 3 }
      }
    }
  ],
  
  "on_complete": {
    "set_flag": "onboarding_stage_2",
    "clear_flag": "onboarding_stage_1"
  }
}
```

---

### Event: commander_onboard_02_meet_troops

**Fires:** Day 1-2
**Priority:** High
**One-time:** Yes

```json
{
  "id": "commander_onboard_02_meet_troops",
  "category": "onboarding",
  "track": "commander",
  "variant": "all",
  "priority": "high",
  "cooldown_days": 0,
  "one_time": true,
  
  "triggers": {
    "all": ["is_enlisted", "ai_safe", "tier >= 7", "onboarding_stage_2"],
    "time_of_day": ["morning", "afternoon"]
  },
  
  "setup": "Your soldiers. Formed up. Waiting.\n\n{TROOP_COUNT} faces looking at you. Some veterans, some green, all uncertain. They've had commanders before. Some good. Some got men killed.\n\nThey're wondering which kind you'll be.\n\nYour sergeant — {OFFICER_NAME} — stands beside you. \"They're ready for inspection, Commander.\"\n\nThis is the moment. First impressions last.",
  
  "options": [
    {
      "id": "walk_line",
      "text": "Walk the line. Look each soldier in the eye.",
      "risk": "safe",
      "outcome": "You walk slowly. Pause at each soldier. Check their gear. Ask their names. Some meet your eyes. Some look away.\n\nBy the end, they know your face. You know theirs.\n\n\"Good men,\" you tell {OFFICER_NAME} afterward. Loud enough for them to hear.\n\nIt's a start.",
      "effects": {
        "lance_reputation": 7
      },
      "rewards": {
        "xp": { "leadership": 25 }
      },
      "costs": { "fatigue": 1 }
    },
    {
      "id": "speech",
      "text": "Address them as a group. Set expectations.",
      "risk": "risky",
      "success_chance": 0.7,
      "outcome_success": "\"I won't promise you glory. I won't promise you'll live. I'll promise you this: I won't waste your lives.\"\n\nSilence. Then a nod from one veteran. Then another.\n\nNo cheers. This isn't that kind of army. But something shifts. They're listening.",
      "outcome_failure": "You speak. The words sound hollow even to you. Borrowed phrases from better commanders.\n\nThe soldiers listen politely. When you finish, they disperse. No change. No connection.\n\n{OFFICER_NAME} says nothing. That says everything.",
      "effects_success": {
        "lance_reputation": 10
      },
      "effects_failure": {
        "lance_reputation": -3
      },
      "rewards_success": {
        "xp": { "leadership": 30, "charm": 20 }
      }
    },
    {
      "id": "defer",
      "text": "\"Tell me about them, {OFFICER_NAME}. Who are these men?\"",
      "risk": "safe",
      "outcome": "{OFFICER_NAME} walks you through the formation. Names, histories, strengths, problems.\n\nThat one's reliable. That one drinks. Those two hate each other. The veteran in the back has forgotten more than most soldiers learn.\n\nBy the end, you know your force. Not faces — people.",
      "effects": {
        "relationship": { "officer": 10 }
      },
      "rewards": {
        "xp": { "leadership": 20, "scouting": 10 }
      }
    },
    {
      "id": "train",
      "text": "\"No speeches. Formation drill. Now.\"",
      "risk": "safe",
      "outcome": "Words are cheap. Action tells.\n\nYou put them through their paces. Basic formations. Reactions to commands. Nothing fancy — just competence.\n\nBy the end, you know who's sharp and who needs work. They know you care about the basics.\n\n\"Again tomorrow,\" you tell {OFFICER_NAME}. \"Until it's reflex.\"",
      "effects": {
        "lance_reputation": 5
      },
      "rewards": {
        "xp": { "leadership": 20, "tactics": 20 }
      },
      "costs": { "fatigue": 2 }
    }
  ],
  
  "on_complete": {
    "set_flag": "onboarding_stage_3",
    "clear_flag": "onboarding_stage_2"
  }
}
```

---

### Event: commander_onboard_03_first_blood

**Fires:** Day 2-3 (or after first battle if sooner)
**Priority:** High
**One-time:** Yes

```json
{
  "id": "commander_onboard_03_first_blood",
  "category": "onboarding",
  "track": "commander",
  "variant": "all",
  "priority": "high",
  "cooldown_days": 0,
  "one_time": true,
  
  "triggers": {
    "all": ["is_enlisted", "ai_safe", "tier >= 7", "onboarding_stage_3"],
    "any": ["after_battle", "days_since_enlistment >= 2"]
  },
  
  "setup_after_battle": "The fighting's done. The counting begins.\n\nYour soldiers — some of them — made it. {CASUALTIES_DESC}.\n\nYou gave the orders. They followed them. And now some of them are dead.\n\n{OFFICER_NAME} approaches with the butcher's bill. \"Casualty report, Commander.\"\n\nThe weight of command isn't the decisions. It's the aftermath.",
  
  "setup_no_battle": "No battle yet. But there will be.\n\n{OFFICER_NAME} finds you staring at the camp. At your soldiers going about their lives.\n\n\"First command?\" they ask. They've seen the look before.\n\n\"Thinking about what happens when the fighting starts. When some of them don't come back.\"\n\nThe weight of command isn't the decisions. It's knowing what they'll cost.",
  
  "options": [
    {
      "id": "face_it",
      "text": "Face it. Learn their names. Remember them.",
      "risk": "safe",
      "outcome": "You go through the list. Every name. Every face you can remember.\n\nSome commanders don't. It's easier not to. But easy isn't right.\n\nYou'll carry these names. All the names, for all the battles to come. That's part of the job nobody mentions.",
      "effects": {
        "lance_reputation": 10
      },
      "rewards": {
        "xp": { "leadership": 30 }
      },
      "costs": { "fatigue": 1 }
    },
    {
      "id": "practical",
      "text": "Focus on what can be fixed. Wounded. Replacements. Next battle.",
      "risk": "safe",
      "outcome": "Grief is a luxury. There's work to do.\n\nYou organize care for the wounded. Send requests for replacements. Review what went wrong, what went right.\n\nThe dead are dead. The living need you focused.\n\n{OFFICER_NAME} watches you work. \"Cold,\" they say quietly. \"But effective.\"\n\nMaybe that's all command is.",
      "effects": {},
      "rewards": {
        "xp": { "leadership": 25, "steward": 15 }
      }
    },
    {
      "id": "drink",
      "text": "Find a bottle. Tonight, you're not a commander.",
      "risk": "risky",
      "success_chance": 0.6,
      "outcome_success": "You drink. Alone. The weight gets lighter for a few hours.\n\nBy morning, it's back. But you needed the break. Everyone does, sometimes.\n\n{OFFICER_NAME} doesn't comment on your state. Good sergeant.",
      "outcome_failure": "You drink. Too much. {OFFICER_NAME} finds you slurring about men who died under your command.\n\n\"Get some sleep, Commander.\" Their voice is carefully neutral.\n\nThe soldiers saw. Word spreads. First battle, and their commander fell apart.\n\nNot a good look.",
      "effects_success": {},
      "effects_failure": {
        "lance_reputation": -10,
        "relationship": { "officer": -5 }
      },
      "costs_success": { "fatigue": -2, "heat": 1 },
      "costs_failure": { "fatigue": -1, "discipline": 2 }
    },
    {
      "id": "talk",
      "text": "Talk to {OFFICER_NAME}. They've been here before.",
      "risk": "safe",
      "outcome": "You find them after dark. \"How do you do it? How do you keep giving orders knowing what they cost?\"\n\n{OFFICER_NAME} is quiet for a long moment. \"You don't get used to it. You shouldn't. The day you stop feeling it is the day you start getting men killed for nothing.\"\n\n\"So you just... carry it?\"\n\n\"You carry it. And you make sure it's worth what you're carrying.\"\n\nNot comforting. But true.",
      "effects": {
        "relationship": { "officer": 15 }
      },
      "rewards": {
        "xp": { "leadership": 20, "charm": 15 }
      }
    }
  ],
  
  "on_complete": {
    "set_flag": "onboarding_complete",
    "clear_flag": "onboarding_stage_3"
  }
}
```

---

## Implementation Notes

### Flags and State

```
onboarding_stage_1    → First event ready to fire
onboarding_stage_2    → Second event ready to fire  
onboarding_stage_3    → Third event ready to fire
onboarding_complete   → Onboarding done, normal events can fire
```

### Detection Variables Needed

```
days_since_enlistment    → Days since current enlistment began
days_since_promotion     → Days since last promotion
has_ever_served          → Boolean: ever been enlisted before
previous_lord            → Reference to last lord served (if any)
current_lord             → Reference to current lord
troop_count              → Number of soldiers in party (T7-9)
```

### Track Selection Logic

```csharp
public OnboardingTrack DetermineTrack(int tier)
{
    if (tier <= 4) return OnboardingTrack.Enlisted;
    if (tier <= 6) return OnboardingTrack.Officer;
    return OnboardingTrack.Commander;
}

public OnboardingVariant DetermineVariant()
{
    if (!HasEverServed()) return OnboardingVariant.FirstTime;
    if (PreviousLord() != CurrentLord()) return OnboardingVariant.Transfer;
    return OnboardingVariant.Return;
}
```

### Event Priority

Onboarding events should fire before regular Lance Life events:

```csharp
if (!IsOnboardingComplete())
{
    // Only fire onboarding events
    return GetNextOnboardingEvent();
}
else
{
    // Normal event selection
    return GetNextRegularEvent();
}
```

### Placeholder Requirements

These placeholders must be available for onboarding events:

| Placeholder | Enlisted | Officer | Commander |
|-------------|----------|---------|-----------|
| `{LANCE_LEADER_RANK}` | ✅ | — | — |
| `{LANCE_LEADER_SHORT}` | ✅ | — | — |
| `{SECOND_SHORT}` | — | ✅ | — |
| `{SECOND_RANK}` | — | ✅ | — |
| `{OFFICER_NAME}` | — | — | ✅ |
| `{LORD_NAME}` | ✅ | ✅ | ✅ |
| `{LORD_TITLE}` | ✅ | ✅ | ✅ |
| `{LANCE_NAME}` | ✅ | ✅ | — |
| `{TROOP_COUNT}` | — | — | ✅ |
| `{VETERAN_1_NAME}` | ✅ | ✅ | — |
| `{RECRUIT_NAME}` | ✅ | — | — |
| `{SOLDIER_NAME}` | ✅ | ✅ | — |
| `{PLAYER_RANK}` | — | ✅ | ✅ |
| `{PREVIOUS_LORD}` | — | Transfer | Transfer |
| `{CASUALTIES_DESC}` | — | — | After battle |

---

## Summary

### Event Count

| Track | Events | Variants | Total Variations |
|-------|--------|----------|------------------|
| Enlisted | 3 | 3 | 9 |
| Officer | 3 | 3* | 7** |
| Commander | 3 | 3* | 7** |
| **Total** | **9** | — | **23** |

*Officer and Commander Event 2 and 3 are shared across variants (same content, different entry context)

**Approximate — some events use shared content with variant-specific setup text

### Feature Flags

```json
{
  "onboarding": {
    "enabled": true,
    "enlisted_track_enabled": true,
    "officer_track_enabled": true,
    "commander_track_enabled": true,
    "skip_for_veterans": false
  }
}
```

---

*Document version: 1.0*
*Part of: Lance Life System*
*Integrates with: Lance Career System, Escalation System*
