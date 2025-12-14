# Lance Menu Activities — Making Your Lance Mates Matter

**Problem:** Current "My Lance" menu only shows roster - nothing to actually DO  
**Solution:** Add meaningful interactions and activities that build relationships and provide gameplay benefits

---

## Table of Contents

1. [Problem Statement](#problem-statement)
2. [Design Philosophy](#design-philosophy)
3. [Menu Structure](#menu-structure)
4. [Activity Categories](#activity-categories)
5. [Detailed Activities](#detailed-activities)
6. [Integration with Reputation](#integration-with-reputation)
7. [Implementation Plan](#implementation-plan)
8. [Event Definitions](#event-definitions)

---

## Problem Statement

### Current State

**Menu: `enlisted_lance`**

```
— Your Lance Name —

Your Position: [rank]
Days with Lance: [days]

— ROSTER —
[10 lance mates listed]

OPTIONS:
1. View Full Roster (opens popup, same info)
2. Check on Wounded (disabled)
3. Back
```

**Problems:**
- ❌ Nothing to actually DO
- ❌ No way to interact with lance mates
- ❌ Lance Reputation exists but has no menu hooks
- ❌ Missed opportunity for social gameplay
- ❌ No resource sharing mechanics
- ❌ Can't build relationships through actions

### Player Questions

- "I have high reputation - now what?"
- "How do I actually interact with my lance mates?"
- "Can I do things WITH my lance?"
- "Why should I care about these characters?"

---

## Design Philosophy

### Core Principles

**1. "Your Lance Is Your Home"**
- Not just coworkers - your friends, rivals, mentors
- Activities should feel social and personal
- Build genuine connections through repeated interactions

**2. "Actions Build Bonds"**
- Every activity should impact lance reputation
- Choices matter - help or ignore, share or hoard
- Visible consequences of your relationships

**3. "Benefits Follow Bonds"**
- High rep = access to better activities
- Low rep = limited options, worse outcomes
- Mechanical rewards for investing in relationships

**4. "Variety and Discovery"**
- Different activities available at different times
- Some activities unlock at higher tiers
- Personality-based variance (different lance mates offer different things)

### Design Goals

✅ **Give players agency** - Multiple ways to interact  
✅ **Create stories** - Memorable moments with lance mates  
✅ **Reward investment** - High rep unlocks better content  
✅ **Integrate with systems** - Ties into rep, pay, equipment, events  
✅ **Respect player time** - Activities are optional, not grindy

---

## Menu Structure

### Expanded Lance Menu Layout

```
ENLISTED_LANCE
│
│   Header:
│   ═══════════════════════════════════════════════
│   — Your Lance Name —
│   
│   Your Position: [rank] (Slot [X]/10)
│   Lance Reputation: [value] ([status])
│   Days with Lance: [days] | Battles: [count]
│   
│   — ROSTER —
│   [Abbreviated list - top 3-4 key members shown]
│   ═══════════════════════════════════════════════
│
├── [ℹ] View Full Roster
│       └── Opens detailed popup with all 10 members + relationships
│
├── — SOCIAL ACTIVITIES —
│   ├── [🔥] Join Fire Circle (Evening/Dusk)
│   │       └── Stories, bonding, +Charm, +Rep
│   ├── [🍺] Drink with the Lads (Evening, ≥0 rep)
│   │       └── Camaraderie, +Rep, +Heat risk
│   ├── [🎲] Gamble (Evening, ≥10 rep)
│   │       └── Dice/cards, gold risk, +Rep on wins
│   └── [📖] Share a Story (≥15 rep)
│           └── Tell tale from your past, +Rep, +Charm
│
├── — LANCE MATES —
│   ├── [💬] Talk to {LANCE_LEADER_SHORT}
│   │       └── Leadership advice, duty tips, relationship building
│   ├── [💬] Talk to {SECOND_SHORT}
│   │       └── Lance gossip, intel, help with problems
│   ├── [💬] Talk to {VETERAN_1_NAME} (if rep ≥20)
│   │       └── Personal conversations, requests, deeper bonds
│   └── [💬] Talk to {VETERAN_2_NAME} (if rep ≥20)
│           └── Different personality, different interactions
│
├── — ASSISTANCE & FAVORS —
│   ├── [💰] Borrow Gold (≥15 rep)
│   │       └── Loan from lance emergency fund
│   ├── [⚔] Trade Equipment (≥10 rep)
│   │       └── Fair trades with lance mates
│   ├── [🎁] Share Your Rations (Always)
│   │       └── Help struggling lance mates, +Rep
│   └── [🛡] Help with Repairs (Always)
│           └── Fix lance mate's gear, +Engineering XP, +Rep
│
├── — GROUP ACTIVITIES — (Some tier/rep locked)
│   ├── [🏹] Hunting Party (≥20 rep, afternoon)
│   │       └── Hunt with 2-3 lance mates, +Food, +Scouting
│   ├── [⚔] Sparring Session (≥10 rep)
│   │       └── Train together, better XP than solo
│   ├── [🔍] Patrol Together (≥15 rep, scout duty)
│   │       └── Duo patrol, +Scouting, +Tactics, safer
│   └── [🔥] Lance Bonding Ritual (≥40 rep, unlock)
│           └── Special ceremony, permanent buff
│
├── — WOUNDED & FALLEN —
│   ├── [🏥] Check on Wounded (if any)
│   │       └── Visit recovering lance mates, +Rep
│   └── [⚰] Remember the Fallen (if any)
│           └── Honor memorial, temporary morale buff
│
└── [←] Back to Enlisted Status
```

---

## Activity Categories

### 1. Social Activities

**Purpose:** Build camaraderie, earn reputation, have fun

| Activity | Rep Required | Time | Benefits | Risks |
|----------|-------------|------|----------|-------|
| **Fire Circle** | None | Evening/Dusk | +15 Charm, +3 Rep, −1 Fatigue | None |
| **Drink with Lads** | 0+ | Evening | +10 Charm, +5 Rep, +1 Heat, −2 Fatigue | Heat gain, gold cost (5-10) |
| **Gamble** | 10+ | Evening | +5-15 Rep on win, gold gain/loss | Lose gold, rep loss on bad behavior |
| **Share a Story** | 15+ | Anytime | +20 Charm, +5-10 Rep, +Morale | None |

### 2. Lance Mate Conversations

**Purpose:** Build individual relationships, get help, receive quests

| Interaction | Rep Required | Outcome |
|-------------|-------------|---------|
| **Lance Leader** | Always | Advice on duties, promotion tips, wisdom |
| **Second** | Always | Lance gossip, camp intel, mediation |
| **Veteran 1** | 20+ | Personal friendship, deeper quests, favors |
| **Veteran 2** | 20+ | Different personality arc, unique benefits |
| **Random Soldier** | 5+ | Brief chat, small rep gain |
| **Recruit** | Always | Mentorship, teach them, feel helpful |

### 3. Assistance & Favors

**Purpose:** Resource sharing, mutual aid, practical benefits

| Activity | Rep Required | Mechanical Benefit |
|----------|-------------|-------------------|
| **Borrow Gold** | 15+ | Loan 50-500 gold based on rep |
| **Trade Equipment** | 10+ | Fair trades (no merchant markup) |
| **Share Rations** | Always | +5-10 Rep, help in food shortage |
| **Help with Repairs** | Always | +20 Engineering, +3 Rep, −1 Fatigue |

### 4. Group Activities

**Purpose:** Do things together, better rewards, team building

| Activity | Rep Required | Benefits |
|----------|-------------|----------|
| **Hunting Party** | 20+ | +Food (party), +30 Bow, +20 Scouting, +8 Rep |
| **Sparring Session** | 10+ | +40 combat XP (vs. 25 solo), +5 Rep |
| **Patrol Together** | 15+ Scout | +50 Scouting (vs. 30 solo), +10 Rep, safer |
| **Bonding Ritual** | 40+ (once) | Permanent: +5% morale, unlock special events |

### 5. Care & Memorial

**Purpose:** Show you care, honor loss, maintain morale

| Activity | Trigger | Benefit |
|----------|---------|---------|
| **Check on Wounded** | Wounded exist | +5 Rep per visit, speeds recovery |
| **Remember Fallen** | Deaths occurred | +10 Morale (party), honor memory, +3 Rep |

---

## Detailed Activities

### Social Activities

#### 🔥 Fire Circle (Evening/Dusk)

**Menu Entry:**
```
[🔥] Join the Fire Circle
    └── Gather with lance mates, share stories and warmth
    └── +15 Charm, +3 Lance Rep, −1 Fatigue
    └── Available: Evening/Dusk
```

**Implementation:**
```csharp
starter.AddGameMenuOption(
    "enlisted_lance",
    "lance_fire_circle",
    "{=lance_fire}Join the Fire Circle",
    args =>
    {
        args.optionLeaveType = GameMenuOption.LeaveType.Wait;
        
        var time = GetTimeOfDay();
        if (time != TimeOfDay.Evening && time != TimeOfDay.Dusk)
        {
            args.Tooltip = new TextObject("Fire circle only forms in the evening");
            args.IsEnabled = false;
        }
        else if (GetFatigue() >= GetMaxFatigue())
        {
            args.Tooltip = new TextObject("Too exhausted");
            args.IsEnabled = false;
        }
        else
        {
            args.Tooltip = new TextObject(
                "Sit with your lance mates around the fire.\n" +
                "+15 Charm, +3 Lance Reputation, −1 Fatigue"
            );
        }
        
        return true; // Always show, may be disabled
    },
    args => TriggerLanceEvent("fire_circle"),
    false, 10
);
```

**Event Flow:**
```json
{
  "id": "lance_fire_circle",
  "category": "lance_social",
  "delivery": { "method": "automatic", "channel": "inquiry" },
  "content": {
    "title": "Fire Circle",
    "setup": "The fire crackles. {LANCE_NAME} gathers - some cleaning gear, others just staring into flames. {VETERAN_1_NAME} is telling a story about a tavern brawl.\n\nThere's space next to {LANCE_MATE_NAME}. Or you could sit with the recruits.",
    "options": [
      {
        "id": "join_veterans",
        "text": "Sit with the veterans",
        "effects": { "lance_reputation": 5 },
        "rewards": { "xp": { "charm": 20 } },
        "outcome": "{VETERAN_1_NAME} nods as you sit. The story continues. Someone passes a flask. For a moment, the war feels far away."
      },
      {
        "id": "help_recruits",
        "text": "Sit with the recruits, teach them something",
        "effects": { "lance_reputation": 3 },
        "rewards": { "xp": { "charm": 15, "leadership": 10 } },
        "outcome": "The recruits listen eagerly. You share what you've learned. {RECRUIT_NAME} asks good questions. You remember when you were like them."
      },
      {
        "id": "listen_quietly",
        "text": "Just listen and enjoy the warmth",
        "effects": { "lance_reputation": 1 },
        "rewards": { "xp": { "charm": 10 } },
        "outcome": "You sit quietly, letting the conversation wash over you. The fire is warm. This is enough."
      }
    ]
  }
}
```

---

#### 🍺 Drink with the Lads (Evening, Rep ≥0)

**Menu Entry:**
```
[🍺] Drink with the Lads
    └── Share drinks and laughter with lance mates
    └── +10 Charm, +5 Lance Rep, +1 Heat, −2 Fatigue
    └── Cost: 5-10 gold | Requires: Neutral standing or better
```

**Implementation:**
```csharp
starter.AddGameMenuOption(
    "enlisted_lance",
    "lance_drinking",
    "{=lance_drink}Drink with the Lads",
    args =>
    {
        args.optionLeaveType = GameMenuOption.LeaveType.Wait;
        
        int lanceRep = GetLanceReputation();
        var time = GetTimeOfDay();
        int cost = GetDrinkingCost(); // 5-10 gold
        
        if (lanceRep < 0)
        {
            args.Tooltip = new TextObject("They don't want to drink with you (Lance Rep < 0)");
            args.IsEnabled = false;
        }
        else if (time != TimeOfDay.Evening && time != TimeOfDay.Dusk)
        {
            args.Tooltip = new TextObject("Drinking happens in the evening");
            args.IsEnabled = false;
        }
        else if (Hero.MainHero.Gold < cost)
        {
            args.Tooltip = new TextObject($"Not enough gold (need {cost})");
            args.IsEnabled = false;
        }
        else
        {
            args.Tooltip = new TextObject(
                "Share drinks with your lance mates.\n" +
                $"+10 Charm, +5 Lance Rep, +1 Heat\n" +
                $"Cost: {cost} gold, −2 Fatigue"
            );
        }
        
        return true;
    },
    args => TriggerLanceEvent("drinking_session"),
    false, 11
);
```

**Event Flow:**
Drinking events should include:
- Toast choices (to lord, to lance, to fallen, etc.)
- Opportunities to buy rounds (+rep)
- Risk of saying something stupid (−rep, +heat)
- Chance for deeper conversations
- Morning hangover consequences

---

#### 🎲 Gamble (Evening, Rep ≥10)

**Menu Entry:**
```
[🎲] Join the Dice Game
    └── Try your luck at dice or cards
    └── Win: +15 Rep, +gold | Lose: −5 Rep, −gold
    └── Requires: 10+ Lance Rep, Evening
```

**Implementation:**
```csharp
starter.AddGameMenuOption(
    "enlisted_lance",
    "lance_gambling",
    "{=lance_gamble}Join the Dice Game",
    args =>
    {
        args.optionLeaveType = GameMenuOption.LeaveType.Trade;
        
        int lanceRep = GetLanceReputation();
        var time = GetTimeOfDay();
        
        if (lanceRep < 10)
        {
            args.Tooltip = new TextObject("You need to be better known before they let you play (Rep < 10)");
            args.IsEnabled = false;
        }
        else if (time != TimeOfDay.Evening && time != TimeOfDay.Dusk)
        {
            args.Tooltip = new TextObject("Gambling happens in the evening");
            args.IsEnabled = false;
        }
        else if (Hero.MainHero.Gold < 20)
        {
            args.Tooltip = new TextObject("Need at least 20 gold to play");
            args.IsEnabled = false;
        }
        else
        {
            args.Tooltip = new TextObject(
                "Gamble with your lance mates.\n" +
                "Win: +gold, +Rep | Lose: −gold, maybe −Rep\n" +
                "Requires decent dice rolling or bluffing skills"
            );
        }
        
        return true;
    },
    args => TriggerLanceEvent("gambling_session"),
    false, 12
);
```

**Gambling Event Flow:**
- Bet amounts: 10/20/50 gold
- Success based on Charm or Roguery skill check
- Win: Gain gold + rep
- Lose gracefully: Lose gold, keep rep
- Lose and get angry: Lose gold + rep
- Option to cheat (Roguery check, huge heat risk)

---

#### 📖 Share a Story (Rep ≥15)

**Menu Entry:**
```
[📖] Share a Story from Your Past
    └── Tell your lance mates about your experiences
    └── +20 Charm, +5-10 Lance Rep, boost morale
    └── Requires: 15+ Rep, Afternoon/Evening
```

**Event Hooks:**
- If player has been in major battles → war stories
- If player has high roguery → bandit tales
- If player has high leadership → tactical lessons
- If player has high charm → romantic misadventures

---

### Lance Mate Conversations

#### 💬 Talk to Lance Leader

**Purpose:** Get advice, build mentor relationship, receive guidance

**Conversation Topics:**

1. **"How am I doing?"** → Get performance feedback
   - Leader tells you your reputation, strengths, weaknesses
   - May offer advice on improving standing

2. **"What's the word on {LORD_NAME}?"** → Intel on lord's situation
   - Reveals lord financial status hints
   - Warns about upcoming problems
   - Insider info on pay situation

3. **"Any advice on promotion?"** → Tips for advancement
   - What you need to do to reach next tier
   - Which duties are impressing officers
   - Reputation requirements

4. **"How's the lance doing?"** → Morale check
   - Learn about problems in the lance
   - Opportunity to help/intervene
   - Build leadership skills

**Implementation:**
```csharp
starter.AddGameMenuOption(
    "enlisted_lance",
    "lance_talk_leader",
    "{=lance_talk_leader}Speak with {LANCE_LEADER_SHORT}",
    args =>
    {
        args.optionLeaveType = GameMenuOption.LeaveType.Conversation;
        args.Tooltip = new TextObject("Have a word with your lance leader");
        return IsLanceLeaderAvailable();
    },
    args => StartLanceLeaderDialog(),
    false, 20
);
```

**Dialog Flow:**
```
Player → "Got a minute, {LANCE_LEADER_RANK}?"

Leader → [Response based on rep]
  Rep 40+: "For you? Always. What's on your mind?"
  Rep 20-39: "Sure. What do you need?"
  Rep 0-19: "Make it quick."
  Rep < 0: "What do you want?"

[Dialog branches based on player choice]
```

---

#### 💬 Talk to Second (Always Available)

**Purpose:** Lance gossip, mediation, camp intel

**Conversation Topics:**

1. **"What's the gossip?"** → Hear lance rumors
   - Who's feuding, who's friendly
   - Upcoming events, camp news
   - Lord's mood, pay status

2. **"I need help with something..."** → Request mediation
   - Help smooth over reputation issues
   - Ask for introductions
   - Get advice on lance dynamics

3. **"How can I help the lance?"** → Find ways to contribute
   - Identifies problems (someone needs gear, food, help)
   - Opportunity to gain rep by helping

---

#### 💬 Talk to Veterans (Rep ≥20)

**Purpose:** Deep friendships, personal quests, unique benefits

**Veteran 1: "The Reliable One"**
- Archetype: Loyal, steady, traditional
- Offers: Combat training tips, equipment maintenance help
- Personal Arc: Family trouble back home, needs advice/help
- Benefits: Covers for you in inspections, warns about danger

**Veteran 2: "The Wild Card"**
- Archetype: Unpredictable, talented, troubled
- Offers: Advanced tactics, unconventional solutions
- Personal Arc: Gambling debts, rivalry with another lance
- Benefits: Gets you into (and out of) trouble, amazing stories

**Progression:**
```
Rep 20: Unlock conversation
Rep 30: Personal quest offered ("Can you help me with...")
Rep 40: Deep friendship, permanent benefits unlock
Rep 50: "Blood brothers" - they have your back no matter what
```

---

### Assistance & Favors

#### 💰 Borrow Gold (Rep ≥15)

**Menu Entry:**
```
[💰] Borrow from Lance Emergency Fund
    └── Take a loan from the lance's pooled savings
    └── Amount based on reputation
    └── Must repay within 14 days or lose rep
```

**Implementation:**
```csharp
starter.AddGameMenuOption(
    "enlisted_lance",
    "lance_borrow_gold",
    "{=lance_borrow}Borrow Gold",
    args =>
    {
        args.optionLeaveType = GameMenuOption.LeaveType.Trade;
        
        int lanceRep = GetLanceReputation();
        int maxLoan = GetMaxLoanAmount(lanceRep);
        bool hasOutstanding = HasOutstandingLoan();
        
        if (lanceRep < 15)
        {
            args.Tooltip = new TextObject("Need better standing to borrow (Rep < 15)");
            args.IsEnabled = false;
        }
        else if (hasOutstanding)
        {
            args.Tooltip = new TextObject("You already have an outstanding loan");
            args.IsEnabled = false;
        }
        else
        {
            args.Tooltip = new TextObject(
                $"Borrow up to {maxLoan} gold from the lance.\n" +
                "Must repay within 14 days or lose reputation.\n" +
                "Interest-free among lance mates."
            );
        }
        
        return true;
    },
    args => ShowBorrowGoldDialog(),
    false, 30
);

private int GetMaxLoanAmount(int lanceRep)
{
    if (lanceRep >= 40) return 500;  // Bonded
    if (lanceRep >= 20) return 200;  // Trusted
    if (lanceRep >= 15) return 50;   // Accepted
    return 0;
}
```

**Borrow Dialog:**
```
How much do you need?

[Slider: 10 - {MAX_LOAN}]

{LANCE_LEADER_SHORT}: "We can spare that. Just pay us back within two weeks, yeah?"

[Confirm] [Cancel]

On confirmation:
- Receive gold
- Start 14-day repayment timer
- If not repaid: −15 rep, debt collectors event
- If repaid early: +5 rep bonus
```

---

#### ⚔ Trade Equipment (Rep ≥10)

**Menu Entry:**
```
[⚔] Trade Gear with Lance Mates
    └── Fair trades without merchant markup
    └── Lance mates have basic equipment available
    └── Requires: 10+ Rep
```

**Implementation:**
```csharp
starter.AddGameMenuOption(
    "enlisted_lance",
    "lance_trade_equipment",
    "{=lance_trade}Trade Equipment",
    args =>
    {
        args.optionLeaveType = GameMenuOption.LeaveType.Trade;
        
        int lanceRep = GetLanceReputation();
        
        if (lanceRep < 10)
        {
            args.Tooltip = new TextObject("Need better standing to trade (Rep < 10)");
            args.IsEnabled = false;
        }
        else
        {
            args.Tooltip = new TextObject(
                "Trade equipment with lance mates at fair prices.\n" +
                "No merchant markup, basic gear available."
            );
        }
        
        return true;
    },
    args => OpenLanceTradeScreen(),
    false, 31
);
```

**Trade Screen:**
- Lance mates have tier-appropriate gear
- Prices at 70% of merchant value (fair trade)
- Limited stock (not a full shop)
- Higher rep = better selection

---

#### 🎁 Share Your Rations (Always Available)

**Menu Entry:**
```
[🎁] Share Your Rations
    └── Give your personal rations to struggling lance mates
    └── +5-10 Lance Rep, helps in food shortage
    └── Cost: Lose 1 day of food
```

**When to Use:**
- Pay tension high (lord not feeding troops well)
- After battles (supplies low)
- Build goodwill
- Demonstrate selflessness

---

#### 🛡 Help with Repairs (Always Available)

**Menu Entry:**
```
[🛡] Help Repair Equipment
    └── Fix lance mate's damaged gear
    └── +20 Engineering XP, +3 Lance Rep, −1 Fatigue
    └── Available after battles
```

---

### Group Activities

#### 🏹 Hunting Party (Rep ≥20, Afternoon)

**Menu Entry:**
```
[🏹] Organize a Hunting Party
    └── Hunt with 2-3 lance mates
    └── +30 Bow, +20 Scouting, +food for party, +8 Rep
    └── Requires: 20+ Rep, Afternoon, near wilderness
    └── −3 Fatigue, 2-3 hours
```

**Implementation:**
```csharp
starter.AddGameMenuOption(
    "enlisted_lance",
    "lance_hunting",
    "{=lance_hunt}Organize a Hunting Party",
    args =>
    {
        args.optionLeaveType = GameMenuOption.LeaveType.Mission;
        
        int lanceRep = GetLanceReputation();
        var time = GetTimeOfDay();
        bool nearWilderness = IsNearWilderness();
        
        if (lanceRep < 20)
        {
            args.Tooltip = new TextObject("Need stronger bonds to organize group hunts (Rep < 20)");
            args.IsEnabled = false;
        }
        else if (time != TimeOfDay.Afternoon)
        {
            args.Tooltip = new TextObject("Hunting is done in the afternoon");
            args.IsEnabled = false;
        }
        else if (!nearWilderness)
        {
            args.Tooltip = new TextObject("No good hunting grounds nearby");
            args.IsEnabled = false;
        }
        else if (GetFatigue() > GetMaxFatigue() - 3)
        {
            args.Tooltip = new TextObject("Too fatigued for hunting");
            args.IsEnabled = false;
        }
        else
        {
            args.Tooltip = new TextObject(
                "Hunt with 2-3 lance mates.\n" +
                "+30 Bow, +20 Scouting, +food for party, +8 Rep\n" +
                "Takes 2-3 hours, −3 Fatigue"
            );
        }
        
        return true;
    },
    args => TriggerLanceEvent("hunting_party"),
    false, 40
);
```

**Event Flow:**
- Choose who to bring (personality-based outcomes)
- Track and kill game (skill checks)
- Distribute meat fairly (rep gain) or keep best cuts (gold, rep loss)
- Chance for bonding conversation during hunt

---

#### ⚔ Group Sparring Session (Rep ≥10)

**Menu Entry:**
```
[⚔] Sparring Session with Lance Mates
    └── Train together, learn from each other
    └── +40 combat XP (better than solo 25), +5 Rep
    └── Requires: 10+ Rep, Morning/Afternoon
    └── −2 Fatigue, 1 hour
```

**Benefits Over Solo Training:**
- +60% more XP (40 vs. 25)
- Build camaraderie (+5 rep)
- Learn from veterans (if high rep, +tip/tactic)
- More engaging event text

---

#### 🔍 Patrol Together (Rep ≥15, Scout Duty Only)

**Menu Entry:**
```
[🔍] Patrol with a Lance Mate
    └── Duo patrol instead of solo scouting
    └── +50 Scouting (vs. 30 solo), +10 Rep, safer
    └── Requires: 15+ Rep, Scout duty, Afternoon
    └── −2 Fatigue, 2 hours
```

**Benefits:**
- Better XP (+67% over solo)
- Safer (backup if ambushed)
- Bonding time (+10 rep)
- Different event outcomes (partner helps)

---

#### 🔥 Lance Bonding Ritual (Rep ≥40, One-Time)

**Menu Entry:**
```
[🔥] Perform Lance Bonding Ritual
    └── Ancient ceremony cementing bonds of brotherhood
    └── PERMANENT: +5% party morale, unlock special events
    └── Requires: 40+ Rep, Evening, Tier 5+
    └── One-time only per lance
```

**Description:**
This is a **milestone event** that marks your lance as truly bonded. It's a special ceremony that happens once you've proven yourself beyond doubt.

**Event:**
```json
{
  "id": "lance_bonding_ritual",
  "category": "lance_milestone",
  "one_time": true,
  "content": {
    "title": "The Brotherhood Ritual",
    "setup": "{LANCE_LEADER_RANK} stands before the fire. The entire lance surrounds it - no smiles, no jokes. This is serious.\n\n\"We've bled together. Fought together. Lost friends together. Tonight, we make it official.\"\n\n{VETERAN_1_NAME} produces a worn leather cord with ten knots. \"Each knot is one of us. When the fire takes it, we're bound. Not by orders. By choice.\"\n\nThe cord burns. Ash drifts upward.\n\n\"{LANCE_NAME} - we are one.\"\n\nYou feel it. Something has changed.",
    "options": [
      {
        "id": "accept",
        "text": "\"We are one.\"",
        "effects": { 
          "lance_reputation": 10,
          "flags_set": ["lance_bonded"]
        },
        "outcome": "The lance echoes: \"We are one.\"\n\nYou'll never forget this moment.\n\n[PERMANENT EFFECT: +5% party morale, special lance events unlocked]"
      }
    ]
  }
}
```

**Permanent Benefits:**
- +5% party morale (passive buff)
- Unlocks "lance_bonded" event category
- Special dialogue options in other events
- Lance mates will die for you (guaranteed rescue in battle)
- "Blood brothers" title

---

### Care & Memorial

#### 🏥 Check on Wounded

**Menu Entry:**
```
[🏥] Check on the Wounded
    └── Visit recovering lance mates
    └── +5 Rep per visit, boost their morale
    └── May speed recovery slightly
    └── Only appears if lance mates are wounded
```

**Implementation:**
```csharp
starter.AddGameMenuOption(
    "enlisted_lance",
    "lance_visit_wounded",
    "{=lance_wounded}Check on the Wounded",
    args =>
    {
        args.optionLeaveType = GameMenuOption.LeaveType.Manage;
        
        int woundedCount = GetWoundedLanceMateCount();
        
        if (woundedCount == 0)
        {
            return false; // Hide option if no wounded
        }
        
        args.Tooltip = new TextObject(
            $"{woundedCount} lance mates are recovering from wounds.\n" +
            "Visit them to boost morale and show you care.\n" +
            "+5 Lance Rep per visit"
        );
        
        return true;
    },
    args => ShowWoundedVisitEvent(),
    false, 50
);
```

---

#### ⚰ Remember the Fallen

**Menu Entry:**
```
[⚰] Honor the Fallen
    └── Pay respects to lost lance mates
    └── +10 Party Morale, +3 Lance Rep
    └── Temporary buff: +5% damage for 3 days
    └── Only appears after deaths in combat
```

**Event:**
Small ceremony where lance gathers to remember those lost. Player can:
- Say a few words (+Leadership XP)
- Silent vigil (+Charm XP)
- Share memory of fallen (+Rep)

**Benefits:**
- Morale boost (losing troops hurts morale, this helps)
- Vengeance buff (fight better for 3 days)
- Closure for player and lance

---

## Integration with Reputation

### Rep Gates

| Reputation | Unlocks |
|------------|---------|
| **< 0** | Only basic options (roster, back) |
| **0+** | Fire circle, drink, talk to leader/second |
| **10+** | Gambling, trade equipment, sparring |
| **15+** | Borrow gold, share story, patrol together |
| **20+** | Talk to veterans, hunting party, personal quests |
| **30+** | Large loans (500g), better trade inventory |
| **40+** | Bonding ritual, blood brothers benefits |

### Rep Gains/Losses

**Gain Reputation:**
- +1-3: Small helpful acts (share rations, help repairs)
- +5-8: Significant help (group activities, personal quests)
- +10-15: Major milestones (bonding ritual, save someone's life)

**Lose Reputation:**
- −1-3: Minor selfishness (take best loot, skip activities)
- −5-8: Betrayal (snitch, blame others)
- −10-15: Major betrayal (abandon in battle, steal)

---

## Implementation Plan

### Phase 1: Social Activities (6-8 hours)

**Core Social Menu:**
- Fire Circle event
- Drinking event
- Basic conversations (leader, second)

**Deliverables:**
- 3 new menu options
- 2-3 event JSON files
- Rep gain/loss logic

---

### Phase 2: Assistance & Favors (8-10 hours)

**Resource Sharing:**
- Borrow gold system
- Trade equipment screen
- Share rations logic
- Help with repairs

**Deliverables:**
- Loan tracking system
- Trade interface
- 4 new menu options
- Rep integration

---

### Phase 3: Lance Mate Conversations (10-12 hours)

**Deep Interactions:**
- Lance leader dialog tree
- Second dialog tree
- Veteran 1 personality & arc
- Veteran 2 personality & arc

**Deliverables:**
- Dialog system integration
- Personality data structure
- Quest trigger system
- 4-6 deep conversation chains

---

### Phase 4: Group Activities (8-10 hours)

**Team Events:**
- Hunting party
- Group sparring
- Patrol together
- Bonding ritual

**Deliverables:**
- 4 group activity events
- Rep/XP reward scaling
- Time/fatigue costs
- Special milestone tracking

---

### Phase 5: Care & Memorial (4-6 hours)

**Casualty System:**
- Wounded tracking
- Visit wounded events
- Fallen roster
- Memorial ceremony

**Deliverables:**
- Casualty tracking integration
- 2 new events
- Morale/buff system

---

### Phase 6: Polish & Balance (6-8 hours)

**Refinement:**
- Balance rep gains/losses
- Tune costs and benefits
- Add variety (multiple versions of events)
- Edge case handling
- UI polish

---

## Event Definitions

### Fire Circle Variants

**Variant 1: Story Time**
```json
{
  "id": "lance_fire_circle_story",
  "content": {
    "setup": "{VETERAN_1_NAME} is mid-story - something about a merchant, a pig, and a very angry wife. The lance is laughing.",
    "options": [
      { "id": "laugh", "text": "Laugh along", "effects": { "lance_reputation": 2 } },
      { "id": "add_detail", "text": "Add your own similar story", "effects": { "lance_reputation": 5 } },
      { "id": "quiet", "text": "Listen quietly", "effects": { "lance_reputation": 1 } }
    ]
  }
}
```

**Variant 2: Quiet Night**
```json
{
  "id": "lance_fire_circle_quiet",
  "content": {
    "setup": "Tonight, the fire is quiet. Everyone's lost in their own thoughts. {LANCE_MATE_NAME} stares into the flames.\n\nYou can sense the weight.",
    "options": [
      { "id": "break_silence", "text": "Try to lighten the mood", "risk": "risky" },
      { "id": "respect_silence", "text": "Respect the quiet", "effects": { "lance_reputation": 3 } },
      { "id": "comfort", "text": "Put a hand on {LANCE_MATE_NAME}'s shoulder", "effects": { "lance_reputation": 5 } }
    ]
  }
}
```

**Variant 3: Tension**
```json
{
  "id": "lance_fire_circle_argument",
  "content": {
    "setup": "{VETERAN_1_NAME} and {SOLDIER_NAME} are arguing. Something about who pulled who out of the last fight.\n\nIt's getting heated. The lance is uncomfortable.",
    "options": [
      { "id": "mediate", "text": "Step in and mediate", "risk": "risky", "effects": { "lance_reputation": 8 } },
      { "id": "side_vet", "text": "Back {VETERAN_1_NAME}", "effects": { "lance_reputation": 3 } },
      { "id": "stay_out", "text": "Stay out of it", "effects": { "lance_reputation": -2 } }
    ]
  }
}
```

---

### Gambling Event

```json
{
  "id": "lance_gambling_dice",
  "category": "lance_social",
  "content": {
    "title": "Dice Game",
    "setup": "Five soldiers around a worn blanket. Dice click. Coins clink.\n\n{VETERAN_1_NAME} looks up. \"In or out?\"\n\nThe pot's at 30 gold. You'll need to match to play.",
    "options": [
      {
        "id": "bet_low",
        "text": "Bet 10 gold (conservative)",
        "costs": { "gold": 10 },
        "risk": "risky",
        "risk_chance": 0.4,
        "outcome_success": "Your roll: good enough. You take 25 gold from the pot.\n\n{VETERAN_1_NAME}: \"Beginner's luck.\"\n\n+15 gold, +3 Rep",
        "outcome_failure": "Your roll: not good enough. {SOLDIER_NAME} takes the pot.\n\n\"Better luck next time.\"\n\n−10 gold"
      },
      {
        "id": "bet_high",
        "text": "Bet 30 gold (aggressive)",
        "costs": { "gold": 30 },
        "risk": "very_risky",
        "risk_chance": 0.6,
        "outcome_success": "Your roll: excellent. The table groans. You take 90 gold.\n\n{VETERAN_1_NAME} whistles. \"Well played.\"\n\n+60 gold, +10 Rep",
        "outcome_failure": "Your roll: terrible. You lose everything.\n\n{VETERAN_1_NAME} shakes their head. \"Rough.\"\n\n−30 gold, −3 Rep"
      },
      {
        "id": "watch",
        "text": "Just watch this round",
        "outcome": "You watch the game unfold. {SOLDIER_NAME} wins with a lucky roll. The banter is entertaining.\n\n+5 Charm (learning from observation)"
      }
    ]
  }
}
```

---

## Success Criteria

✅ **Players engage with lance mates** - Activities are used regularly  
✅ **Rep gain feels rewarding** - Players invest in relationships  
✅ **Benefits are meaningful** - High rep provides real advantages  
✅ **Variety prevents repetition** - Multiple event variants keep it fresh  
✅ **Integration is seamless** - Works with existing systems (rep, pay, events)

---

## Document History

| Version | Date | Author | Changes |
|---------|------|--------|---------|
| 1.0 | 2025-12-13 | AI Assistant | Initial comprehensive design |

---

**END OF DOCUMENT**
