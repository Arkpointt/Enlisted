# Promotion & Rank Progression System

**Summary:** Complete guide to military rank progression from T1 (Follower) to T9 (Marshal). Covers XP requirements, multi-factor promotion criteria, proving events, culture-specific rank titles, and the mechanics of advancement. This system rewards consistent service, combat performance, and maintaining good standing with superiors and comrades.

**Status:** ✅ Current  
**Last Updated:** 2026-01-01 (XP system fix - enlistment XP now properly tracked from all sources)  
**Related Docs:** [Enlistment System](enlistment.md), [Training System](../Combat/training-system.md), [Pay System](pay-system.md), [Order Progression System](../../AFEATURE/order-progression-system.md)

**CRITICAL FIX (2026-01-01):** Fixed XP tracking bug where skill XP was awarded but enlistment XP (used for rank progression) was not. All XP-granting activities now properly update both systems. See [XP Sources](#xp-sources) for details.

---

## Index

1. [Overview](#overview)
2. [Tier Progression Table](#tier-progression-table)
3. [XP Requirements](#xp-requirements)
4. [Multi-Factor Promotion Requirements](#multi-factor-promotion-requirements)
5. [XP Sources](#xp-sources)
6. [The Promotion Process](#the-promotion-process)
7. [Proving Events](#proving-events)
8. [Culture-Specific Ranks](#culture-specific-ranks)
9. [Promotion Blocking](#promotion-blocking)
10. [Equipment & Benefits](#equipment--benefits)
11. [Configuration](#configuration)
12. [News Integration](#news-integration)

---

## Overview

The promotion system transforms you from a raw recruit (T1) into a veteran commander (T9) through nine tiers of military rank. Promotion requires more than just experience—you must prove your worth through battle, maintain relationships with your superiors, earn respect from your fellow soldiers, and avoid disciplinary problems.

**Three Career Tracks:**
- **Enlisted Track (T1-T4)**: Raw recruit to veteran soldier
- **Officer Track (T5-T6)**: Elite soldier to NCO leader
- **Commander Track (T7-T9)**: Captain to strategic commander with retinue

Each promotion unlocks better equipment from the Quartermaster, higher wages, and increased authority within the company.

---

## Tier Progression Table

| Tier | Generic Rank | XP Required | Typical Duration | Track | Notes |
|------|-------------|-------------|------------------|-------|-------|
| **T1** | Follower | 0 | Starting | Enlisted | Starting rank, basic equipment |
| **T2** | Recruit | 800 | 1-2 weeks | Enlisted | Formation selection event |
| **T3** | Free Sword | 3,000 | 1-2 months | Enlisted | Basic combat veteran |
| **T4** | Veteran | 6,000 | 2-3 months | Enlisted | Experienced fighter |
| **T5** | Blade | 11,000 | 3-4 months | Officer | Elite soldier status |
| **T6** | Chosen | 19,000 | 4-6 months | Officer | Senior NCO equivalent |
| **T7** | Captain | 30,000 | 6+ months | Commander | 20-soldier retinue granted |
| **T8** | Commander | 45,000 | Extended service | Commander | 30-soldier retinue (expanded) |
| **T9** | Marshal | 65,000 | Endgame | Commander | 40-soldier retinue (maximum) |

**Note:** XP requirements are cumulative totals, not per-tier amounts. T3 requires 3,000 total XP, not 3,000 additional XP beyond T2.

---

## XP Requirements

### Base XP Thresholds

XP thresholds are the foundation of progression, but meeting the XP requirement alone does not guarantee promotion. These values come from `progression_config.json`:

```json
{
  "tier": 2,
  "xp_required": 800
}
```

The system checks your total accumulated enlistment XP against these thresholds every in-game hour. When you reach a threshold AND meet all other requirements, a promotion proving event is triggered.

### XP Display

Your XP progress is logged with each XP gain:
```
+25 XP from Daily Service | Total: 2,150/3,000 (72% to Tier 3)
```

This shows:
- XP gained this tick
- Source of XP
- Total accumulated XP
- Next tier threshold
- Progress percentage

---

## Multi-Factor Promotion Requirements

Meeting the XP threshold is necessary but not sufficient for promotion. You must also satisfy five additional requirements:

### Complete Requirements Table

| Promotion | XP | Days in Rank | Battles | Soldier Rep | Leader Relation | Max Discipline |
|-----------|-----|--------------|---------|-------------|-----------------|----------------|
| **T1→T2** | 800 | 14 days | 2 | ≥0 | ≥0 | <8 |
| **T2→T3** | 3,000 | 35 days | 6 | ≥10 | ≥10 | <7 |
| **T3→T4** | 6,000 | 56 days | 12 | ≥20 | ≥20 | <6 |
| **T4→T5** | 11,000 | 56 days | 20 | ≥30 | ≥30 | <5 |
| **T5→T6** | 19,000 | 56 days | 30 | ≥40 | ≥15 | <4 |
| **T6→T7** | 30,000 | 70 days | 40 | ≥50 | ≥20 | <3 |
| **T7→T8** | 45,000 | 84 days | 50 | ≥60 | ≥25 | <2 |
| **T8→T9** | 65,000 | 112 days | 60 | ≥70 | ≥30 | <1 |

### Requirement Explanations

**1. Days in Rank**
- Minimum time you must serve at your current rank
- Prevents rapid advancement without proving yourself
- Represents the time needed to learn your role

**2. Battles Survived**
- Combat experience requirement
- Only counts battles where you participated (not reserve duty)
- Incremented after each battle completion
- Shows you can survive under fire

**3. Soldier Reputation**
- Your standing with the rank-and-file troops (scale: -50 to +100)
- Gained through: completing orders successfully, helping comrades, fair treatment
- Lost through: order failures, selfish choices, ignoring troop welfare
- Managed by the Escalation system
- Higher ranks require respect from those you'll lead

**4. Leader Relation**
- Your personal relationship with your enlisted lord (Bannerlord native scale)
- Gained through: order success, loyalty, combat performance
- Lost through: order failures, disobedience, poor performance
- Critical for officer and commander promotions
- The lord must trust you before granting authority

**5. Maximum Discipline**
- Discipline is a "trouble counter" (scale: 0-10, where 10 is discharge)
- Must be BELOW the threshold (e.g., <4 means discipline must be 3 or lower)
- Gained from: order failures, insubordination, criminal activity
- Reduced over time with good behavior
- Cannot promote with active disciplinary problems

### Checking Your Status

When you're blocked from promotion but close (≥75% progress), the system logs the blocking factors:

```
Promotion to T3 blocked (82% progress): Days in rank: 28/35, Soldier reputation: 8/10
```

This tells you exactly what you need to improve to advance.

---

## XP Sources

**How XP Works:** The mod tracks two types of XP:
1. **Skill XP** - Individual character skills (Athletics, Tactics, etc.) via `Hero.AddSkillXp()`
2. **Enlistment XP** - Military rank progression via `EnlistmentBehavior.AddEnlistmentXP()`

Both are awarded simultaneously from all XP-granting activities. Enlistment XP is what advances your tier and appears in muster reports.

### Order Event XP

**Primary XP Source:** Events that occur during order execution  
**Amount:** 10-32 XP per event option (varies by skill and difficulty)  
**When:** Event resolution during order "slot" phases  
**Frequency:** 15-35% chance per slot (4 slots per day during active orders)

**Skills Awarded:**
- Guard/Sentry duty → Athletics, Tactics, Scouting
- Patrol → Scouting, Athletics, Roguery
- Medical → Medicine
- Equipment tasks → Crafting
- Leadership orders → Leadership, Tactics

**Example:**
```
Order Event: Drunk Soldier at Guard Post
Option: Turn him away
+12 Athletics XP (skill progression)
+12 Enlistment XP (rank progression)
```

All order events grant XP. Failed skill checks typically award 50% of success XP.

### Camp Activities XP

**Source:** Free time activities and camp opportunities  
**Amount:** 8-25 XP per activity  
**When:** Player-initiated camp decisions

**Activities:**
- Training sessions (weapon skills, athletics)
- Medical self-treatment
- Muster interactions (bribe, smuggle, protest)

**Example:**
```
Camp Activity: Self-Treatment
+25 Medicine XP (skill)
+25 Enlistment XP (rank)
```

### Combat & Battle XP

**Source:** Skill XP awarded during combat (native Bannerlord system)  
**Amount:** Varies by enemy tier, damage dealt, and killing blows  
**When:** During battle participation

Native Bannerlord awards skill XP for combat actions using a tier-scaled formula:
```
baseXP = 0.4 × (attackerPower + 0.5) × (victimPower + 0.5) × damage × multiplier
```

**Enemy tier scaling:**
- Higher tier enemies have higher "victimPower" values
- Killing a T5 veteran grants significantly more XP than a T1 recruit
- Killing blows add the victim's full HP to the damage component (bonus XP)
- XP is awarded to the weapon skill used (OneHanded, Bow, Polearm, etc.)

**Integration with enlistment:**
- The mod intercepts native skill XP awards during combat
- Accumulated combat skill XP is converted to enlistment XP at battle end
- This appears in muster reports under the "Combat" source
- All combat skills count: melee weapons, ranged weapons, and Athletics (unarmed)

### Narrative Event XP

**Source:** General narrative events (escalation, pay, promotion, etc.)  
**Amount:** 10-30 XP per event  
**When:** Event triggers based on context

Events outside the order system can also grant XP through their effects and rewards.

### XP Flow Architecture

**Technical Implementation:**
1. Activity grants skill XP via `Hero.AddSkillXp(skill, amount)`
2. Same activity calls `EnlistmentBehavior.AddEnlistmentXP(amount, source)`
3. Enlistment XP is tracked for rank progression and muster reports
4. Both types contribute to character development

**Source Tracking:**
The system tracks XP sources for muster period summaries:
```
XP Sources This Period:
• Order: Guard Post Duty: +48 XP
• Order: Camp Patrol: +36 XP
• Camp: Athletics: +20 XP
• Self-Treatment: +25 XP
• Training: OneHanded: +15 XP
```

### Progression Timeline

Typical XP rates per 12-day muster period:

| Activity Level | Orders | Events | Camp | Total/Period | T1→T4 Time |
|---------------|--------|--------|------|--------------|------------|
| Quiet Garrison | 2 orders | 0-1 events | 1-2 activities | 40-80 XP | 6-12 months |
| Routine Campaign | 3 orders | 2-3 events | 2-3 activities | 100-150 XP | 3-6 months |
| Active Campaign | 4 orders | 4-6 events | 3-4 activities | 180-250 XP | 2-3 months |

**Key Insight:** Players who engage with order events and camp activities progress 3-4x faster than those who ignore them.

### Estimated Progression Timeline

With typical play (2-3 battles per week, daily service):
- **Weekly XP:** ~350-450 (175 daily + 100-150 battle + kills)
- **T1→T2:** 2-3 weeks
- **T2→T3:** 5-7 weeks (also need 35 days in rank)
- **T3→T4:** 8-10 weeks (also need 56 days, 12 battles)
- **T4→T5:** 10-14 weeks (also need reputation ≥30)
- **T5→T6:** 18-24 weeks (also need reputation ≥40)

The time gates (days in rank, battle requirements) ensure you can't rush through promotions even with high XP gain.

---

## The Promotion Process

### Step-by-Step Flow

**1. Hourly Eligibility Check**
- `PromotionBehavior` checks every in-game hour if you're ready
- Runs the full requirements check: XP, days, battles, reputation, relation, discipline
- This ensures responsive promotions without performance overhead

**2. Requirements Validation**
- If ANY requirement fails, promotion is blocked
- System logs the reason if you're close (≥75% progress)
- You can see what's blocking you in the logs

**3. Proving Event Queued**
- If all requirements pass, the system queues a narrative proving event
- Event priority is "critical" (displays as soon as safe)
- The event ID follows the pattern: `promotion_t{from}_t{to}_{name}`

**4. Proving Event Display**
- A popup presents the proving scenario
- Multiple choice options reflecting different approaches
- Each option affects discipline, reputation, or character tags
- All options marked `"promotes": true` grant the promotion

**5. Promotion Granted**
- Your tier increases via `EnlistmentBehavior.SetTier(targetTier)`
- Quartermaster inventory updates with newly unlocked equipment via `QuartermasterManager.UpdateNewlyUnlockedItems()`
- Days in rank counter resets to 0
- Battles survived counter persists (cumulative)
- Promotion notification displays with your culture-specific rank title
- **Personal Feed updated** with tier-specific narrative entry (see [News Integration](#news-integration))

**6. Quartermaster Prompt**
After each promotion, the system displays:
```
"Report to the Quartermaster for your new kit."
```
This reminds players to visit the Quartermaster to:
- Purchase new tier equipment now available
- Upgrade existing equipment to better quality
- At T5+, transition from issued rations to officer provisions

**7. Player Actions**
- Visit the Quartermaster to get new equipment (see [Quartermaster System](../Equipment/quartermaster-system.md))
- Note your new wage rate (increases with tier)
- At T7+, select your retinue formation type
- Continue service at your new rank

**8. Muster Recap**
- At the next muster (12-day pay cycle), a **Promotion Recap** stage displays
- This formal acknowledgment shows when the promotion occurred during the period
- Recap appears as stage 6 in the muster sequence (after inspections/recruit, before retinue/complete)
- **Formal acknowledgment before assembled company:** Captain addresses formation with official recognition
- **Promotion details displayed:**
  - Previous rank → Current rank
  - Date of promotion (e.g., "Day 20 (8 days ago)")
  - Wage increase (old rate → new rate)
  - Equipment tier unlocked (e.g., "Tier 4 equipment available from Quartermaster")
  - New authorities granted (e.g., "Lead small patrols")
  - New camp decisions/orders unlocked
  - Current XP progress toward next tier
- **Options:**
  - Continue with muster
  - Visit Quartermaster immediately (flag for after muster completion)
- **Special notes:**
  - For T7 promotions: Acknowledges retinue grant
  - For T9 promotions: Shows "pinnacle of service" message
  - If multiple tiers skipped (rare): Shows all aggregate benefits
- **See:** [Muster System - Promotion Recap](muster-system.md#6-promotion-recap) for complete stage details

### Fallback Mechanism

If proving events are missing or the event system is disabled, the system falls back to direct promotion:
- Promotion is granted automatically when requirements are met
- No narrative event displays
- Quartermaster still updates
- Notification still displays

This ensures the core progression system never breaks even if content files are missing.

---

## Proving Events

Each tier transition includes a unique narrative event that tests your character and leadership philosophy. These events shape your emergent identity through your choices.

### T1→T2: Finding Your Place

**Theme:** Battle formation selection and specialization  
**Situation:** The sergeant asks what role you're trained for  

**Choices:**
- **Infantry** → Shield wall, sword & shield, hold the line
- **Archer** → Ranged combat, bow/crossbow, strike from distance
- **Cavalry** → Mounted warfare, lance and sword, ride with lancers
- **Horse Archer** → Mounted archery, mobile skirmisher (if faction supports)

**Effect:** This choice provides narrative flavor and affects:
- Equipment the Quartermaster offers (category filtering)
- Training events available (skill-based content)

**Note:** Your actual **battle formation assignment** (T1-T6 only) is detected dynamically from your equipped weapons at battle start: bow → Ranged, horse → Cavalry, both → Horse Archer, melee → Infantry. This T2 choice represents your "stated specialty" for equipment and training purposes, but doesn't lock you into a formation. At T7+ you control your own party and formation assignment is skipped entirely.

**Location:** `ModuleData/Enlisted/Events/events_promotion.json` → `promotion_t1_t2_finding_your_place`

---

### T2→T3: The Sergeant's Test

**Theme:** Leadership philosophy and discipline approach  
**Situation:** A recruit lost his rations gambling. The sergeant asks how you'd handle it.

**Choices:**
1. **"Discipline him publicly"** → Hard but fair approach
   - Effect: +1 Discipline risk, gains "Disciplinarian" character tag
   
2. **"Give him a chance to earn it back"** → Merciful approach
   - Effect: +5 Soldier Reputation, gains "Merciful" character tag
   
3. **"Make him work extra duties"** → Practical approach
   - Effect: No special modifiers, recognized as pragmatic

**What This Tests:** Your approach to authority and how you balance discipline with compassion. This sets the tone for your leadership style as you advance.

**Location:** `ModuleData/Enlisted/Events/events_promotion.json` → `promotion_t2_t3_sergeants_test`

---

### T3→T4: Crisis of Command

**Theme:** Battlefield leadership under pressure  
**Situation:** The sergeant falls wounded in battle. The line wavers. You must lead. Afterward, your lord asks what you did.

**Choices:**
1. **"I led the charge"** → Aggressive offense
   - Effect: Gains "Aggressive" character tag
   
2. **"I held the line and protected the wounded"** → Defensive protection
   - Effect: +5 Soldier Reputation, gains "Defender" character tag
   
3. **"I ordered a tactical withdrawal"** → Calculated retreat
   - Effect: Gains "Tactician" character tag

**What This Tests:** Your decision-making under fire and whether you prioritize aggression, defense, or tactics. Shows the lord your command instincts.

**Location:** `ModuleData/Enlisted/Events/events_promotion.json` → `promotion_t3_t4_crisis_of_command`

---

### T4→T5: The Veterans' Vote

**Theme:** Earning trust and respect from those you'll lead  
**Situation:** The veteran squad gathers. The sergeant says some think you're ready for command stripes, but rank must be earned by the trust of those you lead.

**Choices:**
1. **"I'm nothing without this squad"** → Humble deference
   - Effect: +10 Soldier Reputation
   
2. **"I've bled with you. I'm ready to lead you"** → Confident assertion
   - Effect: -1 Discipline risk (confidence, not arrogance)
   
3. **"I swear no one goes hungry or forgotten"** → Protective promise
   - Effect: +5 Soldier Reputation, gains "Protector" character tag

**What This Tests:** How you relate to those who will serve under your command. The veterans decide if they trust you with authority.

**Location:** `ModuleData/Enlisted/Events/events_promotion.json` → `promotion_t4_t5_squad_vote`

---

### T5→T6: Audience with the Lord

**Theme:** Loyalty and where your allegiance truly lies  
**Situation:** Your lord summons you to the pavilion. You've served well. The rank of T6 is theirs to give, but first they must know where your loyalty lies.

**Choices:**
1. **"My sword belongs to you, my lord"** → Personal loyalty to the lord
   - Effect: Gains "Lord Sworn" loyalty tag
   
2. **"My loyalty is to [Kingdom] and its people"** → Loyalty to the realm
   - Effect: Gains "Realm Loyal" loyalty tag
   
3. **"My loyalty is to the soldiers beside me"** → Loyalty to comrades
   - Effect: +10 Soldier Reputation, gains "Lance Bound" loyalty tag

**What This Tests:** Your fundamental allegiance. This shapes how NPCs perceive your motivations and may affect future story events. Each choice is valid—personal loyalty, political loyalty, or soldier brotherhood.

**Location:** `ModuleData/Enlisted/Events/events_promotion.json` → `promotion_t5_t6_lord_audience`

---

### T6→T7: Commander's Commission

**Theme:** Transition from soldier to commander of troops  
**Situation:** Your lord offers you command of twenty soldiers. Maps spread across the table show troop positions. This is real command authority.

**Choices:**
1. **"I'm ready, my lord. Give me the command"** → Accept command
   - Effect: Gains "Leader" character tag, grants T7 and retinue
   
2. **"I'm not ready to lead men yet"** → Decline (honest self-assessment)
   - Effect: Gains "Cautious" tag, NO promotion (must request later via dialog)

**What This Tests:** Whether you're ready for the responsibility of command. This is the first promotion you can decline. If you decline, you must manually request it later—the system will not auto-promote you to T7.

**Note:** This is the transition point to the Commander Track. From T7 onward, you command your own retinue of soldiers who fight under your banner. At T7, you also select your **retinue formation type** (Infantry, Archers, Cavalry, or Horse Archers)—this is separate from your personal battle formation chosen at T2.

**Location:** `ModuleData/Enlisted/Events/events_promotion.json` → `promotion_t6_t7_commanders_commission`

**See:** [Retinue System](retinue-system.md) for complete T7+ retinue mechanics including formation selection, loyalty, reinforcements, and named veterans.

---

### T7→T8 & T8→T9: Expanded Command

These promotions expand your retinue:
- **T7→T8:** +10 soldiers (20→30 total) — "Expanded Command" event
- **T8→T9:** +10 soldiers (30→40 total, maximum) — "Elite Commander" event

Both follow similar proving event structures where the lord acknowledges your command performance and offers expanded responsibility. You can accept or defer.

**See:** [Retinue System - Command Progression](retinue-system.md#command-progression) for complete details.

### Event Option Effects

Each proving event option includes effects that modify your character. The critical effect is `"promotes": true`:

**Effect Types Used in Proving Events:**
- `"promotes": true` — Triggers promotion to next tier (resolved at runtime)
- `"promotes": 5` — Explicit promotion to tier 5 (alternative syntax)
- `"formation": "infantry"` — Sets your battle formation (T1→T2 only)
- `"character_tag": "tactician"` — Adds a character tag for identity tracking
- `"loyalty_tag": "lord_sworn"` — Records loyalty declaration (T5→T6)
- `"soldierRep": 10` — Modifies soldier reputation
- `"discipline": -1` — Reduces discipline risk

**Example from T5→T6:**
```json
{
  "id": "loyal_to_soldiers",
  "text": "\"My loyalty is to the soldiers beside me.\"",
  "effects": {
    "loyalty_tag": "lance_bound",
    "soldierRep": 10,
    "promotes": true
  }
}
```

**How `"promotes": true` Works:**
When the parser encounters `"promotes": true`, it converts this to a sentinel value (-1). At runtime, `EventDeliveryManager.ApplyPromotesEffect()` resolves -1 to `currentTier + 1`, ensuring the player advances exactly one tier regardless of their current position.

Options without `"promotes": true` will NOT grant the promotion (e.g., declining T6→T7).

---

## Culture-Specific Ranks

Generic rank names (Follower, Recruit, etc.) are replaced by culture-specific titles matching the faction you serve. This provides cultural flavor and immersion.

### Empire Ranks (Legion/Discipline)

| Tier | Rank | Translation |
|------|------|-------------|
| T1 | Tiro | Recruit |
| T2 | Miles | Soldier |
| T3 | Immunes | Specialist |
| T4 | Principalis | Junior Officer |
| T5 | Evocatus | Veteran |
| T6 | Centurion | Company Commander |
| T7 | Primus Pilus | First Spear (Elite Centurion) |
| T8 | Tribune | Senior Commander |
| T9 | Legate | Legion Commander |

### Vlandia Ranks (Feudal/Chivalry)

| Tier | Rank |
|------|------|
| T1 | Peasant |
| T2 | Levy |
| T3 | Footman |
| T4 | Man-at-Arms |
| T5 | Sergeant |
| T6 | Knight Bachelor |
| T7 | Cavalier |
| T8 | Banneret |
| T9 | Castellan |

### Sturgia Ranks (Tribal/Shield Wall)

| Tier | Rank |
|------|------|
| T1 | Thrall |
| T2 | Ceorl |
| T3 | Fyrdman |
| T4 | Drengr |
| T5 | Huskarl |
| T6 | Varangian |
| T7 | Champion |
| T8 | Thane |
| T9 | High Warlord |

### Khuzait Ranks (Steppe/Horde)

| Tier | Rank |
|------|------|
| T1 | Outsider |
| T2 | Nomad |
| T3 | Noker |
| T4 | Warrior |
| T5 | Veteran |
| T6 | Bahadur |
| T7 | Arban |
| T8 | Zuun |
| T9 | Noyan |

### Battania Ranks (Celtic/Guerrilla)

| Tier | Rank |
|------|------|
| T1 | Woodrunner |
| T2 | Clan Warrior |
| T3 | Skirmisher |
| T4 | Raider |
| T5 | Oathsworn |
| T6 | Fian |
| T7 | Highland Champion |
| T8 | Clan Chief |
| T9 | High King's Guard |

### Aserai Ranks (Desert/Mercantile)

| Tier | Rank |
|------|------|
| T1 | Tribesman |
| T2 | Skirmisher |
| T3 | Footman |
| T4 | Veteran |
| T5 | Guard |
| T6 | Faris |
| T7 | Emir's Chosen |
| T8 | Sheikh |
| T9 | Grand Vizier |

### Mercenary/Generic Ranks (Universal)

| Tier | Rank |
|------|------|
| T1 | Follower |
| T2 | Recruit |
| T3 | Free Sword |
| T4 | Veteran |
| T5 | Blade |
| T6 | Chosen |
| T7 | Captain |
| T8 | Commander |
| T9 | Marshal |

**Note:** Rank titles are localized in `ModuleData/Languages/enlisted_strings.xml` with keys like `{=Enlisted_Rank_Empire_T1}`. The system automatically selects the appropriate culture based on your enlisted lord's faction.

---

## Promotion Blocking

Promotions can be blocked even when XP requirements are met. The system handles several blocking scenarios:

### Discipline Blocking

If your discipline risk is at or above the tier's maximum discipline threshold, promotion is blocked:

```
Promotion blocked: your discipline is under review. 
Reduce discipline risk before advancing.
```

**How It Works:**
- Discipline ranges from 0-10 (10 is discharge)
- Each tier has a maximum discipline threshold (e.g., T2→T3 requires <7)
- If your discipline is 7 or higher, you cannot promote to T3
- XP continues to accumulate while blocked
- Once discipline drops below threshold, promotion resumes automatically

**Reducing Discipline:**
- Time passage (discipline naturally decays)
- Successful order completion
- Avoiding further infractions
- Completing certain events with discipline-reducing choices

### Declined Promotion

If you decline a proving event (like T6→T7), promotion is permanently blocked for that tier until you manually request it:

```
Promotion to T7 previously declined - must request via dialog
```

**How to Resume:**
- Speak with your lord or a superior officer
- Use a specific dialogue option to request the promotion
- You must still meet all requirements
- The proving event may be offered again, or promotion may be granted directly

### Missing Requirements

The most common block is simply not meeting all requirements yet:

```
Promotion to T4 blocked (82% progress): 
  Days in rank: 48/56
  Battles: 10/12
```

**What to Do:**
- Check the log to see what you're missing
- Focus on the specific requirements blocking you
- Battles: Participate in more combat
- Days: Wait for time to pass (continue daily service)
- Reputation: Complete orders, make reputation-positive event choices
- Relation: Succeed at orders, avoid failures
- Discipline: Avoid trouble, complete orders successfully

### Relief Valve Design

The system is designed with a "relief valve" philosophy:
- XP never stops accumulating
- You can be over-qualified for a tier (e.g., T3 XP but still T2 rank)
- When blocks are removed, promotion triggers immediately
- This prevents dead-end scenarios where you can't progress

---

## Equipment & Benefits

### Equipment Tier Unlocks

Each promotion unlocks new equipment tiers at the Quartermaster:

| Promotion | Standard Equipment | Officers' Armory Access |
|-----------|-------------------|------------------------|
| **T1** | Tier 1 only | - |
| **T2** | Tiers 1-2 | - |
| **T3** | Tiers 1-3 | - |
| **T4** | Tiers 1-4 | - |
| **T5** | Tiers 1-5 | - |
| **T6** | Tiers 1-6 | - |
| **T7+** | Tiers 1-6 | T8-T9 with Rep 60+ |

**How It Works:**
- After promotion, visit the Quartermaster
- New items appear in the equipment grid
- You're not automatically equipped—you must purchase/request items
- Higher tiers = better armor, weapons, and horses
- Equipment quality gates represent supply chain limitations (the lord won't waste elite gear on unproven recruits)

**Officers' Armory (T7+ Only):**
At T7+ with Quartermaster Reputation 60+, you gain access to the Officers' Armory which provides:
- T8-T9 tier equipment (beyond your normal access)
- Better quality modifiers (Fine/Masterwork/Legendary)
- Higher reputation = better tier bonus and quality rolls

See [Quartermaster System](../Equipment/quartermaster-system.md#officers-armory) for complete details.

### Wage Increases

Your daily wage increases with tier through the formula:

```
Base Formula:
  daily_base = 10
  tier_bonus = tier × 5
  
Wage = (10 + tier×5) × multipliers
```

**Example Progression:**
- T1: ~10-15 denars/day
- T3: ~25-30 denars/day
- T5: ~35-45 denars/day
- T7: ~50-65 denars/day (+ retinue management responsibility)

Assignment multipliers and in-army bonuses further increase wages. See [Pay System](pay-system.md) for complete wage calculation.

### Authority & Responsibilities

Higher tiers unlock new interactions and responsibilities:

**T1-T2 (Enlisted):**
- Take orders from sergeants
- Basic camp activities
- No command authority

**T3-T4 (Veteran):**
- Can train lower-ranked troops (if that system is implemented)
- More complex orders
- Input on tactical decisions

**T5-T6 (Officer):**
- Officer-level orders
- Strategic decision participation
- NCO equivalent authority

**T7-T9 (Commander):**
- Command your own retinue (20-40 soldiers)
- Strategic orders from the lord
- Post-battle retinue management
- Reinforcement requests
- Named veteran soldiers under your command

---

## Configuration

### XP Source Values

File: `ModuleData/Enlisted/progression_config.json`

```json
"xp_sources": {
  "daily_base": 25,
  "battle_participation": 25,
  "xp_per_kill": 2
}
```

**Adjusting XP Gain:**
- XP rates are now defined per-order in `ModuleData/Enlisted/Orders/*.json`
- Order completion XP is in the `consequences.success.skill_xp` section
- Event XP is in individual event definitions in `ModuleData/Enlisted/Events/*.json`
- No global daily/battle XP config (those systems were removed)

### Tier Requirements

File: `ModuleData/Enlisted/progression_config.json`

```json
{
  "tier": 3,
  "xp_required": 3000,
  "name": "{=Enlisted_Rank_FreeSword}Free Sword",
  "duration": "1-2 months"
}
```

**Adjusting Thresholds:**
- Modify `xp_required` to change how much XP is needed
- Values are cumulative totals, not deltas
- Lower values = faster progression
- Higher values = longer career arcs

### Multi-Factor Requirements

File: `src/Features/Ranks/Behaviors/PromotionBehavior.cs`

```csharp
2 => new PromotionRequirements { 
  XP = 700, 
  DaysInRank = 14, 
  BattlesRequired = 2, 
  MinSoldierReputation = 0, 
  MinLeaderRelation = 0, 
  MaxDiscipline = 8 
}
```

**Adjusting Requirements:**
- Reduce `DaysInRank` for faster time progression
- Reduce `BattlesRequired` for less combat dependency
- Lower reputation/relation requirements for easier advancement
- Raise `MaxDiscipline` to be more forgiving of trouble

**Warning:** These are code-based values. Changes require recompiling the mod.

### Proving Event Content

File: `ModuleData/Enlisted/Events/events_promotion.json`

Event content can be edited directly:
- Change dialogue text
- Add new choice options
- Modify effect tags
- Adjust reputation/discipline impacts

All promotion events have `"timing": { "one_time": true }` to ensure they only fire once per tier.

---

## News Integration

Promotions are recorded to the Personal Feed for historical review. Players can look back at their career progression with immersive military flavor text.

### Personal Feed Entries

Each promotion generates a tier-specific headline:

| Tier | Personal Feed Entry |
|------|---------------------|
| T2 | "The sergeant's stripe is sewn to your sleeve. You are now {RANK}." |
| T3 | "Your service has been recognized. You stand among the {RANK}s now." |
| T4 | "The men call you {RANK}. Years of blood and iron have earned that name." |
| T5 | "Officers' council admits you to their ranks. You are {RANK}." |
| T6 | "Your lord has raised you to {RANK}. The camp speaks of nothing else." |
| T7 | "Twenty soldiers salute their new commander. You are {RANK}, with a retinue of your own." |
| T8 | "The banner grows. Thirty soldiers now march under your command as {RANK}." |
| T9 | "Forty hardened warriors answer to you. {RANK} is a title few ever earn." |

### Implementation

Called from `PromotionBehavior.TriggerPromotionNotification()`:

```csharp
EnlistedNewsBehavior.Instance?.AddPromotionNews(newTier, rankName, retinueSoldiers);
```

Localization strings use `News_Promotion_T{N}` keys in `enlisted_strings.xml`.

---

## Implementation Details

### Source Files

| File | Purpose |
|------|---------|
| `src/Features/Ranks/Behaviors/PromotionBehavior.cs` | Hourly eligibility checks, proving event queuing, requirements validation |
| `src/Features/Ranks/RankHelper.cs` | Culture-specific rank title lookup |
| `src/Features/Enlistment/Behaviors/EnlistmentBehavior.cs` | XP tracking, tier storage, `SetTier()` method |
| `src/Features/Escalation/EscalationManager.cs` | Reputation and discipline tracking for requirements |
| `src/Features/Content/EventDeliveryManager.cs` | Processes `"promotes": true` effect from event options |
| `src/Features/Equipment/Behaviors/QuartermasterManager.cs` | `UpdateNewlyUnlockedItems()` after promotions |
| `ModuleData/Enlisted/Events/events_promotion.json` | Proving event definitions (8 events) |
| `ModuleData/Enlisted/progression_config.json` | XP thresholds, culture ranks, XP sources |

### Key Methods

**Promotion Check (hourly):**
```csharp
// PromotionBehavior.CheckForPromotion()
var (canPromote, failureReasons) = CanPromote();
if (canPromote)
{
    var eventId = GetProvingEventId(currentTier, targetTier);
    EventDeliveryManager.Instance.QueueEvent(provingEvent);
}
```

**XP Award:**
```csharp
// EnlistmentBehavior.AddEnlistmentXP(amount, source)
_enlistmentXP += xp;
CheckPromotionNotification(previousXP, _enlistmentXP);
```

**Tier Advancement:**
```csharp
// EnlistmentBehavior.SetTier(newTier)
_enlistmentTier = newTier;
_daysInRank = 0;
QuartermasterManager.Instance?.UpdateNewlyUnlockedItems();
```

### Save/Load Compatibility

All promotion data persists through saves:
- `_enlistmentTier` — Current tier (1-9)
- `_enlistmentXP` — Total accumulated XP
- `_daysInRank` — Days at current rank
- `_battlesSurvived` — Total battles completed
- `_pendingPromotionTier` — Tracks queued proving event

---

**End of Document**

