# Systems Flow Diagram

**Visual guide showing how story blocks, systems, and threshold events interconnect.**

Last Updated: December 13, 2025

---

## System Architecture Overview

```
┌─────────────────────────────────────────────────────────────────┐
│                        PLAYER ACTIONS                           │
│                     (Story Block Choices)                       │
└────────────┬────────────────────────────────────┬───────────────┘
             │                                    │
             │                                    │
    ┌────────▼────────┐                  ┌───────▼────────┐
    │  STORY PACKS    │                  │  EVENT PACKS   │
    │                 │                  │                │
    │ • corruption    │                  │ • pay_tension  │
    │ • discipline    │                  │ • escalation   │
    │ • morale        │                  │ • duties       │
    │ • logistics     │                  │ • onboarding   │
    │ • medical       │                  │ • promotion    │
    │ • training      │                  │ • training     │
    └────────┬────────┘                  └───────┬────────┘
             │                                    │
             └──────────────┬─────────────────────┘
                            │
                    ┌───────▼────────┐
                    │   SYSTEMS      │
                    │                │
                    │ • Heat         │
                    │ • Discipline   │
                    │ • Pay Tension  │
                    │ • Lance Rep    │
                    │ • Medical Risk │
                    │ • Fatigue      │
                    └───────┬────────┘
                            │
                    ┌───────▼────────┐
                    │  THRESHOLD     │
                    │   CHECK        │
                    │                │
                    │ Is value ≥     │
                    │ threshold?     │
                    └───┬────────┬───┘
                        │        │
                   YES  │        │ NO
                        │        │
            ┌───────────▼─       └─────────────┐
            │                                   │
    ┌───────▼────────┐                 ┌───────▼────────┐
    │  QUEUE         │                 │  CONTINUE      │
    │  THRESHOLD     │                 │  TRACKING      │
    │  EVENT         │                 │                │
    └───────┬────────┘                 └────────────────┘
            │
            │ (cooldown check)
            │ (max 1 per day)
            │
    ┌───────▼────────┐
    │  FIRE          │
    │  THRESHOLD     │
    │  EVENT         │
    │                │
    │ Player makes   │
    │ consequence    │
    │ choice         │
    └───────┬────────┘
            │
            │ (may modify systems)
            │
    ┌───────▼────────┐
    │  SYSTEMS       │
    │  UPDATED       │
    │                │
    │ Loop back to   │
    │ threshold check│
    └────────────────┘
```

---

## Heat System Flow

```
┌─────────────────────────────────────────────────────────────────┐
│                      CORRUPTION CHOICES                         │
└─┬─────────────┬──────────────┬──────────────┬──────────────────┘
  │             │              │              │
  │ +2 Heat     │ +3 Heat      │ +4 Heat      │ +1 Heat
  │             │              │              │
  ▼             ▼              ▼              ▼
corruption.   pay_tension.   pay_tension.   pay_tension.
ledger_skim   join_theft     loot_everyone  take_weapon
  │             │              │              │
  └─────────────┴──────────────┴──────────────┘
                      │
                      ▼
            ┌─────────────────┐
            │   HEAT TRACK    │
            │    (0 - 10)     │
            └────────┬─────────┘
                     │
        ┌────────────┼────────────┐
        │            │            │            │
  Heat=3 │      Heat=5 │    Heat=7 │     Heat=10 │
        │            │            │            │
        ▼            ▼            ▼            ▼
  ┌─────────┐  ┌──────────┐ ┌─────────┐ ┌──────────┐
  │ WARNING │  │SHAKEDOWN │ │  AUDIT  │ │ EXPOSED  │
  │         │  │          │ │         │ │          │
  │ Lance   │  │ Kit      │ │ Formal  │ │ Caught   │
  │ leader  │  │ inspec-  │ │ audit   │ │ red-     │
  │ warns   │  │ tion     │ │ of      │ │ handed   │
  │ you     │  │          │ │ supplies│ │          │
  └────┬────┘  └─────┬────┘ └────┬────┘ └─────┬────┘
       │             │           │            │
       │ -1 Heat     │ -2 Heat   │ -3 Heat    │ Reset
       │             │           │            │
       └─────────────┴───────────┴────────────┘
                     │
                     ▼
            ┌─────────────────┐
            │   HEAT TRACK    │
            │   (reduced)     │
            └─────────────────┘

DECAY: -1 Heat per 7 days (passive)
```

---

## Discipline System Flow

```
┌─────────────────────────────────────────────────────────────────┐
│                    INFRACTIONS & CHOICES                        │
└─┬──────────────┬──────────────┬─────────────────────────────────┘
  │              │              │
  │ +1 Disc      │ +2 Disc      │ +3 Disc
  │              │              │
  ▼              ▼              ▼
Minor          Moderate       Major
infractions    infractions    infractions
(late, sloppy) (disobey)     (fight, theft)
  │              │              │
  └──────────────┴──────────────┘
                 │
                 ▼
       ┌──────────────────┐
       │ DISCIPLINE TRACK │
       │     (0 - 10)     │
       └────────┬──────────┘
                │
    ┌───────────┼───────────┬───────────┬────────────┐
    │           │           │           │            │
Disc=3 │    Disc=5 │    Disc=7 │    Disc=10 │         │
    │           │           │           │            │
    ▼           ▼           ▼           ▼            │
┌────────┐ ┌─────────┐ ┌─────────┐ ┌──────────┐     │
│ EXTRA  │ │ HEARING │ │ BLOCKED │ │DISCHARGE │     │
│ DUTY   │ │         │ │         │ │          │     │
│        │ │ Formal  │ │ Promo-  │ │ Facing   │     │
│Unpleas-│ │ hearing │ │ tion    │ │ dis-     │     │
│ant     │ │ defend  │ │ blocked │ │ charge   │     │
│duties  │ │ self    │ │ until   │ │ hearing  │     │
│assigned│ │         │ │ reduced │ │          │     │
└───┬────┘ └────┬────┘ └────┬────┘ └─────┬────┘     │
    │           │           │           │            │
    │ -1 Disc   │ varies    │ -1 to -2  │ varies     │
    │           │           │ Disc      │            │
    └───────────┴───────────┴───────────┴────────────┘
                            │
                            ▼
                  ┌──────────────────┐
                  │ DISCIPLINE TRACK │
                  │    (reduced)     │
                  └──────────────────┘

DECAY: -1 Discipline per 14 days (passive)
```

---

## Pay Tension System Flow

```
┌─────────────────────────────────────────────────────────────────┐
│                      PAYDAY SYSTEM                              │
│                   (Finance Behavior)                            │
└─────────────┬───────────────────────────────────────────────────┘
              │
              │ Pay delayed/missed
              │
              ▼
    ┌──────────────────┐
    │  PAY TENSION     │
    │  (0 - 100)       │
    └────────┬──────────┘
             │
   ┌─────────┼─────────┬─────────┬─────────┬─────────┐
   │         │         │         │         │         │
Tens=20  Tens=45  Tens=50  Tens=60  Tens=85  │      │
   │         │         │         │         │         │
   ▼         ▼         ▼         ▼         ▼         │
┌────────┐┌────────┐┌────────┐┌────────┐┌─────────┐ │
│GRUMB-  ││THEFT   ││LOOT    ││CONFRON-││MUTINY   │ │
│LING    ││INVITE  ││DEAD    ││TATION  ││         │ │
│        ││        ││        ││        ││         │ │
│Fire    ││Steal   ││Post-   ││Delega- ││Armed    │ │
│circle  ││supply  ││battle  ││tion to ││revolt   │ │
│talk    ││wagon   ││loot    ││pay-    ││against  │ │
│        ││        ││options ││master  ││officers │ │
└───┬────┘└───┬────┘└───┬────┘└───┬────┘└────┬────┘ │
    │         │         │         │         │        │
    │         │         │         │         │        │
    ├─────────┼─────────┼─────────┼─────────┤        │
    │         │         │         │         │        │
    ▼         ▼         ▼         ▼         ▼        │
┌──────────────────────────────────────────────────┐ │
│            THREE PATHS AVAILABLE                 │ │
│                                                  │ │
│  CORRUPT PATH    LOYAL PATH      DESERT PATH    │ │
│  ============    ==========      ============    │ │
│  +Heat           +Lord Rel       No Penalty     │ │
│  +Gold           -Tension        (if Tens≥60)   │ │
│  -Lance Rep      +Lance Rep                     │ │
│  (sometimes)                                     │ │
└──────────────────────────────────────────────────┘ │
                                                     │
             ┌───────────────────────────────────────┘
             │ Payday occurs
             │
             ▼
    ┌──────────────────┐
    │  PAY TENSION     │
    │  (resets to 0)   │
    └──────────────────┘
```

---

## Lance Reputation System Flow

```
┌─────────────────────────────────────────────────────────────────┐
│                   SOCIAL INTERACTIONS                           │
└─┬───────────┬───────────┬───────────┬──────────────────────────┘
  │           │           │           │
  │ +Social   │ +Help     │ -Selfish  │ -Betray
  │ bonding   │ mates     │ choices   │ actions
  │           │           │           │
  │ +1 to +3  │ +2 to +5  │ -2 to -5  │ -5 to -30
  │           │           │           │
  └─────┬─────┴─────┬─────┴─────┬─────┴─────┬───────
        │           │           │           │
        ▼           ▼           ▼           ▼
     morale.    pay_tension.  pay_tension. pay_tension.
     stories    solidarity    selfish     betrayal
                choices       choices     choices
        │           │           │           │
        └───────────┴───────────┴───────────┘
                        │
                        ▼
              ┌──────────────────┐
              │  LANCE REP TRACK │
              │   (-50 to +50)   │
              └────────┬──────────┘
                       │
          ┌────────────┼────────────┬────────────┐
          │            │            │            │
      Rep≥+40      Rep≥+20      Rep≤-20      Rep≤-40
          │            │            │            │
          ▼            ▼            ▼            ▼
     ┌────────┐   ┌────────┐   ┌────────┐   ┌────────┐
     │BONDED  │   │TRUSTED │   │ISOLATED│   │SABOTAGE│
     │        │   │        │   │        │   │        │
     │Lance   │   │Lance   │   │Excluded│   │Active  │
     │mates   │   │mate    │   │from    │   │enemy   │
     │cover   │   │shares  │   │fire    │   │action  │
     │for you │   │secret  │   │circle  │   │        │
     └────┬───┘   └────┬───┘   └────┬───┘   └────┬───┘
          │            │            │            │
          │ Benefits   │ Help       │ Isolation  │ Harm
          │ in events  │ available  │ in events  │ events
          │            │            │            │
          └────────────┴────────────┴────────────┘

DECAY: ±1 per 14 days toward 0 (neutral)
```

---

## Medical Risk System Flow

```
┌─────────────────────────────────────────────────────────────────┐
│                    INJURY OR ILLNESS                            │
│                 (from training, combat)                         │
└─────────────────────────┬───────────────────────────────────────┘
                          │
                          ▼
                 ┌──────────────────┐
                 │  PLAYER INJURED  │
                 │  or ILL          │
                 └────────┬──────────┘
                          │
            ┌─────────────┴─────────────┐
            │                           │
            ▼                           ▼
      ┌──────────┐               ┌──────────┐
      │  TREAT   │               │  IGNORE  │
      │          │               │          │
      │Seek      │               │Continue  │
      │medical   │               │activities│
      │help      │               │          │
      └────┬─────┘               └────┬─────┘
           │                          │
           │ Medical Risk = 0         │ Medical Risk +1/day
           │ (reset)                  │
           │                          │
           ▼                          ▼
    ┌──────────────┐          ┌──────────────┐
    │  RECOVERED   │          │ MEDICAL RISK │
    │              │          │  (1 - 5)     │
    └──────────────┘          └──────┬────────┘
                                     │
                       ┌─────────────┼─────────────┐
                       │             │             │
                   Risk=3        Risk=4        Risk=5
                       │             │             │
                       ▼             ▼             ▼
                  ┌─────────┐  ┌──────────┐  ┌─────────┐
                  │WORSENING│  │COMPLICA- │  │EMERGENCY│
                  │         │  │TION      │  │         │
                  │Condition│  │          │  │Collapse │
                  │severity │  │New       │  │forced   │
                  │increases│  │problem   │  │bed rest │
                  │         │  │(infection│  │         │
                  │Minor →  │  │fever)    │  │Lasting  │
                  │Moderate │  │          │  │effects  │
                  └────┬────┘  └────┬─────┘  └────┬────┘
                       │            │            │
                       │ Force      │ Force      │ Force
                       │ treatment  │ treatment  │ treatment
                       │            │            │
                       └────────────┴────────────┘
                                    │
                                    ▼
                           ┌──────────────┐
                           │  MEDICAL     │
                           │  RISK = 0    │
                           │  (treated)   │
                           └──────────────┘

DECAY: -1 per day with rest (does NOT decay untreated)
```

---

## Multi-System Event Example

### Pay Tension: Mutiny Event Flow

```
┌─────────────────────────────────────────────────────────────────┐
│  PAY TENSION ≥ 85                                               │
│  Trigger: LeavingBattle                                         │
└────────────────────────┬────────────────────────────────────────┘
                         │
                         ▼
            ┌────────────────────────┐
            │  pay_tension_mutiny_   │
            │  brewing EVENT FIRES   │
            │                        │
            │ "Armed revolt against  │
            │  the officers"         │
            └───────┬────────────────┘
                    │
        ┌───────────┼───────────┬───────────┬───────────┐
        │           │           │           │           │
        ▼           ▼           ▼           ▼           ▼
  ┌──────────┐┌──────────┐┌──────────┐┌──────────┐┌──────────┐
  │ JOIN     ││ STOP     ││ SLIP     ││ STAND    ││ [other]  │
  │ MUTINY   ││ MUTINY   ││ AWAY     ││ WITH     ││ options  │
  │          ││          ││          ││ OFFICERS ││          │
  └────┬─────┘└────┬─────┘└────┬─────┘└────┬─────┘└────┬─────┘
       │           │           │           │           │
       ▼           ▼           ▼           ▼           │
  ┌──────────────────────────────────────────────────┐ │
  │             SYSTEM EFFECTS                       │ │
  │                                                  │ │
  │ JOIN:                                            │ │
  │   Lance Rep: +30                                 │ │
  │   Heat: 0                                        │ │
  │   Discipline: 0                                  │ │
  │   Triggers: mutiny_resolution event              │ │
  │                                                  │ │
  │ STOP:                                            │ │
  │   Lance Rep: -40                                 │ │
  │   Heat: 0                                        │ │
  │   Discipline: 0                                  │ │
  │   XP: Leadership +30                             │ │
  │   Relation: lord +25                             │ │
  │   Risk: 40% failure (beaten, +Discipline)        │ │
  │                                                  │ │
  │ SLIP AWAY:                                       │ │
  │   Lance Rep: 0                                   │ │
  │   Heat: 0                                        │ │
  │   Discipline: 0                                  │ │
  │   XP: Roguery +20                                │ │
  │   Risk: 10% failure (caught)                     │ │
  │   Triggers: desertion_clean event                │ │
  │                                                  │ │
  │ STAND WITH OFFICERS:                             │ │
  │   Lance Rep: -50                                 │ │
  │   Heat: 0                                        │ │
  │   Discipline: 0                                  │ │
  │   Medical Risk: +0.2                             │ │
  │   XP: Leadership +20, One-handed +20             │ │
  │   Gold: +50                                      │ │
  │   Relation: lord +40                             │ │
  │   Risk: 30% failure (death)                      │ │
  └──────────────────────────────────────────────────┘ │
                         │                             │
                         └─────────────────────────────┘
                                     │
                                     ▼
                        ┌────────────────────────┐
                        │  SYSTEMS UPDATED       │
                        │                        │
                        │ Check thresholds:      │
                        │ - Lance Rep ≤ -40?     │
                        │   → lance_sabotage     │
                        │ - Medical Risk ≥ 3?    │
                        │   → medical_worsening  │
                        └────────────────────────┘
```

---

## Story Block to Threshold Event Chain

### Example: Corruption Path

```
STORY BLOCK         SYSTEM           THRESHOLD         CONSEQUENCE
   CHOICE           EFFECT            REACHED            EVENT

corruption.
ledger_skim
→ slip_a_tip ───► Heat +2 ───────┐
                                 │
pay_tension.                     │
join_theft   ───► Heat +3 ───────┼──► Heat = 5 ──► heat_shakedown
                                 │                  fires
pay_tension.                     │
loot_enemy   ───► Heat +2 ───────┘

                                 ├──► Heat = 7 ──► heat_audit
                                 │                  fires
                                 │
                                 └──► Heat = 10 ─► heat_exposed
                                                    fires

Each threshold event:
  - Has cooldown (7 days)
  - Max 1 per day
  - Can reduce Heat through options
  - May add Discipline
```

---

## Time-Based Event Flow

### Daily/Weekly Cycle

```
TIME                    AUTOMATIC CHECKS              PLAYER ACTIONS
────                    ────────────────              ──────────────

Every 6 hours           Lance Life Event
                        Evaluation
                            │
                            ├─ Check conditions
                            ├─ Check cooldowns
                            └─ Queue if eligible
                                                      ┌────────────┐
                                                      │ Story      │
Morning                                               │ Block      │
                        Time-of-Day Check             │ Choice     │
Dawn stories eligible   (dawn triggers)   ◄───────── │            │
                                                      └──────┬─────┘
                                                             │
Day                                                          │
Day stories eligible    Time-of-Day Check                   │
                        (day triggers)                      │
                                                             │
Dusk                                                         │
Dusk stories eligible   Time-of-Day Check                   │
                        (dusk triggers)                     │
                                                             │
Night                                                        │
Night stories eligible  Time-of-Day Check                   ▼
                        (night triggers)         ┌──────────────────┐
                                                  │ System Effects   │
                                                  │ Applied          │
Battle End                                        └────────┬─────────┘
LeavingBattle events    Incident Trigger                  │
queue automatically     (LeavingBattle)                   │
                                                           ▼
Every 7 days            Decay System           ┌──────────────────┐
Heat -1                 passive decay          │ Threshold Check  │
                                               └────────┬─────────┘
Every 14 days           Decay System                    │
Discipline -1           passive decay                   │
Lance Rep ±1 → 0                                        ▼
                                               ┌──────────────────┐
Every Payday            Finance System         │ Threshold Event? │
(12 days ±1)            Wage calculation       └────────┬─────────┘
Pay Tension update                                      │
                                                        │
                                                   YES  │  NO
                                                        │
                                         ┌──────────────┴──┐
                                         ▼                 ▼
                                 ┌────────────┐    ┌────────────┐
                                 │Queue Event │    │Continue    │
                                 │(if not on  │    │            │
                                 │ cooldown)  │    │            │
                                 └──────┬─────┘    └────────────┘
                                        │
                                        ▼
                               ┌─────────────────┐
                               │Fire Event       │
                               │(next safe       │
                               │ moment)         │
                               └─────────────────┘
```

---

## Cooldown System

```
┌─────────────────────────────────────────────────────────────────┐
│                     EVENT FIRES                                 │
└────────────────────────┬────────────────────────────────────────┘
                         │
                         ▼
          ┌──────────────────────────────┐
          │ Record Event Fire Timestamp  │
          │                              │
          │ - Event-specific cooldown    │
          │ - Category cooldown          │
          │ - Threshold cooldown (7d)    │
          └──────────────┬────────────────┘
                         │
                         ▼
          ┌──────────────────────────────┐
          │ Max Events Per Day Counter   │
          │                              │
          │ Lance Life: max 1/day        │
          │ Threshold: max 1/day         │
          └──────────────┬────────────────┘
                         │
                         │ Time passes...
                         │
                         ▼
          ┌──────────────────────────────┐
          │ Next Evaluation Cycle        │
          │                              │
          │ Check:                       │
          │ - Cooldown expired?          │
          │ - Daily limit reset?         │
          │ - Conditions still met?      │
          └──────────┬───────────────────┘
                     │
        ┌────────────┴────────────┐
        │                         │
        ▼                         ▼
  ┌──────────┐            ┌──────────┐
  │ ELIGIBLE │            │ BLOCKED  │
  │          │            │          │
  │ Can fire │            │ On       │
  │ again    │            │ cooldown │
  └──────────┘            └──────────┘
```

---

## Priority System

```
PRIORITY LEVELS         WHEN IT FIRES               EXAMPLES
───────────────         ─────────────               ────────

critical                Immediately, overrides      Discharge events
                        other events                Mutiny resolution

high                    Before normal events        Threshold events
                        Max 1 per day              Heat shakedown
                                                   Discipline hearing

normal                  Standard queue              Most story blocks
                        May be delayed by           Lance Life stories
                        higher priority             Pay tension grumbling

low                     After all others            Tutorial hints
                        Can be postponed            Flavor text
```

---

## System Interaction Web

```
                        ┌─────────────┐
                        │   PLAYER    │
                        │   CHOICES   │
                        └──────┬──────┘
                               │
              ┌────────────────┼────────────────┐
              │                │                │
              ▼                ▼                ▼
       ┌────────────┐   ┌────────────┐   ┌────────────┐
       │   HEAT     │   │ DISCIPLINE │   │  LANCE     │
       │            │   │            │   │  REP       │
       │ Corrupt    │   │ Infractions│   │            │
       │ activities │   │            │   │ Social     │
       └──────┬─────┘   └──────┬─────┘   └──────┬─────┘
              │                │                │
              │                │                │
       Heat≥5 │         Disc≥7 │         Rep≤-20│
              │                │                │
              ▼                ▼                ▼
       ┌────────────┐   ┌────────────┐   ┌────────────┐
       │ SHAKEDOWN  │   │ BLOCKED    │   │ ISOLATED   │
       │            │   │            │   │            │
       │ May find   │   │ Cannot     │   │ No help    │
       │ contraband │   │ promote    │   │ from lance │
       └──────┬─────┘   └──────┬─────┘   └──────┬─────┘
              │                │                │
       Found  │                │                │ Alone
              ▼                ▼                ▼
       ┌────────────┐   ┌────────────┐   ┌────────────┐
       │+DISCIPLINE │   │ Must clean │   │ Worse event│
       │+HEAT       │   │ record to  │   │ outcomes   │
       │            │   │ promote    │   │            │
       └────────────┘   └────────────┘   └────────────┘
              │                │                │
              └────────────────┼────────────────┘
                               │
                               ▼
                    ┌──────────────────┐
                    │  CUMULATIVE      │
                    │  PRESSURE ON     │
                    │  PLAYER          │
                    │                  │
                    │ Multiple systems │
                    │ requiring        │
                    │ attention        │
                    └──────────────────┘
```

---

## Event Cascades

### Example: Corruption Cascade

```
1. INITIAL CHOICE
   ↓
   corruption.ledger_skim → slip_a_tip
   Heat +2
   
2. OPPORTUNITY APPEARS
   ↓
   pay_tension ≥ 45 → theft_invitation unlocks
   ↓
   Player: join_theft
   Heat +3 (total: 5)
   
3. THRESHOLD REACHED
   ↓
   Heat = 5 → heat_shakedown fires
   ↓
   Player fails inspection (has contraband)
   Heat +2 (total: 7)
   Discipline +2
   
4. SECOND THRESHOLD
   ↓
   Heat = 7 → heat_audit fires
   Discipline = 2
   ↓
   Player tries to bribe auditor, fails
   Heat +3 (total: 10)
   Discipline +3 (total: 5)
   
5. MULTIPLE THRESHOLDS
   ↓
   Heat = 10 → heat_exposed fires (CRITICAL)
   Discipline = 5 → discipline_hearing fires
   ↓
   Player faces:
   - Confiscation of all contraband
   - Formal discipline hearing
   - Blocked promotion
   - Reduced pay
   - Lance Rep damage
   
6. LONG-TERM CONSEQUENCES
   ↓
   Recovery requires:
   - Clean choices for 7+ days (Heat decay)
   - Complete extra duties (Discipline reduction)
   - Time (14+ days for promotion block to clear)
```

---

## Adding New Content: Flow Checklist

```
NEW STORY BLOCK
       │
       ▼
┌──────────────┐
│ 1. Choose    │ What category? (corruption, morale, etc.)
│    Category  │ What systems affected?
└──────┬───────┘
       │
       ▼
┌──────────────┐
│ 2. Define    │ Time of day?
│    Triggers  │ Location?
└──────┬───────┘ Thresholds required?
       │
       ▼
┌──────────────┐
│ 3. Set       │ Fatigue: 0-3
│    Costs     │ Gold: varies
└──────┬───────┘ Risk: safe/risky/corrupt
       │
       ▼
┌──────────────┐
│ 4. Define    │ Which systems?
│    Effects   │ How much?
└──────┬───────┘ Check cross-effects
       │
       ▼
┌──────────────┐
│ 5. Check     │ Will this trigger thresholds?
│    Thresholds│ Are threshold events needed?
└──────┬───────┘
       │
       ▼
┌──────────────┐
│ 6. Set       │ Story cooldown
│    Cooldowns │ Category cooldown
└──────┬───────┘ Threshold cooldown
       │
       ▼
┌──────────────┐
│ 7. Test      │ JSON valid?
│    Integration│ Systems update correctly?
└──────┬───────┘ Events fire at right time?
       │
       ▼
┌──────────────┐
│ 8. Update    │ Add to SYSTEMS_AND_STORY_BLOCKS_REFERENCE.md
│    Docs      │ Add to SYSTEMS_QUICK_REFERENCE.md
└──────────────┘ Update this flow diagram if needed
```

---

## Document Navigation

- **Comprehensive Reference:** `SYSTEMS_AND_STORY_BLOCKS_REFERENCE.md`
- **Quick Lookup:** `SYSTEMS_QUICK_REFERENCE.md`
- **Flow Diagrams:** This document
- **System Design:** `docs/research/escalation_system.md`
- **Pay Tension:** `docs/research/pay_tension_events.md`
