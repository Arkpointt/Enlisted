# Lance Reputation System — Gameplay Impact Design

**Status:** Enhancement Proposal  
**Current State:** Tracked (-50 to +50) with threshold events, minimal mechanical impact  
**Goal:** Make lance reputation feel meaningful in moment-to-moment gameplay

---

## Table of Contents

1. [Problem Statement](#problem-statement)
2. [Current System Analysis](#current-system-analysis)
3. [Design Philosophy](#design-philosophy)
4. [Mechanical Systems](#mechanical-systems)
5. [Social Systems](#social-systems)
6. [Combat Systems](#combat-systems)
7. [Event Integration](#event-integration)
8. [Reputation Tiers](#reputation-tiers)
9. [Implementation Phases](#implementation-phases)
10. [Balance Considerations](#balance-considerations)

---

## Problem Statement

### Current State

**What's Tracked:**
- Range: -50 to +50
- Changes from event choices
- Threshold events at -40, -20, +20, +40
- One mechanical gate: duty change requests (need 10+ rep)

**The Problem:**
> Players gain/lose lance reputation but don't **feel** it in gameplay. It's just a number that occasionally triggers events.

**Player Questions:**
- "Why should I care about lance reputation?"
- "What does +30 rep actually DO for me?"
- "Is it worth sacrificing gold/gear to gain rep?"

**Missing:**
- Tangible gameplay benefits
- Clear feedback loops
- Moment-to-moment impact
- Strategic tradeoffs

---

## Current System Analysis

### What Exists

**Location:** `EscalationManager.cs` + `EscalationState.cs`

**Current Mechanics:**
```csharp
public int LanceReputation { get; set; }  // Range: -50 to +50
public const int MinLanceReputationForDutyChange = 10;  // Only mechanical gate
```

**Status Labels:**
- Bonded: 40-50
- Trusted: 20-39
- Accepted: 5-19
- Neutral: -4 to +4
- Disliked: -5 to -19
- Outcast: -20 to -39
- Hated: -40 to -50

**Decay:** Trends toward 0 by ±1 every 14 days

### What's Missing

❌ **No combat benefits** - High rep doesn't help you in battle  
❌ **No economic impact** - Can't borrow from or trade with lance mates  
❌ **No social access** - Reputation doesn't unlock activities  
❌ **No information sharing** - Lance mates don't warn or help based on rep  
❌ **No failure cushioning** - High rep doesn't protect you from consequences  
❌ **No desertion impact** - Low rep doesn't make others pressure you to leave

---

## Design Philosophy

### Core Principles

**1. "Your Lance Is Your Lifeline"**
- High rep = lance mates have your back
- Low rep = you're on your own (or worse, actively undermined)

**2. "Reputation Is Survival"**
- Not just social niceness
- Directly impacts your ability to succeed and survive

**3. "Trust Is Currency"**
- High rep unlocks favors, warnings, cover
- Low rep makes everything harder

**4. "Visible Feedback"**
- Players should SEE rep impact in daily interactions
- Not just event text — actual mechanical differences

### Design Goals

✅ **Meaningful Choices** - Player must balance rep vs. other resources  
✅ **Clear Feedback** - Obvious when rep helps or hurts  
✅ **Strategic Depth** - Different playstyles favor different rep levels  
✅ **Narrative Integration** - Mechanics reinforce the story  
✅ **No Mandatory Path** - High or low rep are both viable (with tradeoffs)

---

## Mechanical Systems

### 1. Combat Benefits

**High Reputation Bonuses (Rep ≥ 20):**

```csharp
// Battle Deployment
if (lanceReputation >= 20)
{
    // Lance mates position near you in formation
    // Bonus: They're more likely to assist if you're in trouble
    // Effect: +5% morale when fighting alongside lance mates
}

if (lanceReputation >= 40)
{
    // "Bonded" - Lance mates actively cover you
    // Effect: Reduced friendly fire, lance mates prioritize protecting you
    // Bonus: When wounded, lance mate may drag you to safety (auto-retreat)
}
```

**Low Reputation Penalties (Rep ≤ -20):**

```csharp
// Battle Isolation
if (lanceReputation <= -20)
{
    // Lance mates position AWAY from you
    // Effect: No morale bonus from friendly troops nearby
    // Risk: If you call for help, they're "too busy" to assist
}

if (lanceReputation <= -40)
{
    // "Hated" - Active endangerment
    // Risk: "Accidental" friendly fire more likely
    // Effect: No one covers your retreat
    // Danger: Equipment might "malfunction" at critical moments
}
```

**Implementation:**
- Check rep in `EnlistedFormationAssignmentBehavior`
- Modify formation positioning based on rep
- Add rep-based morale modifiers to party morale calculations

---

### 2. Information & Warnings

**Intelligence Network (Rep ≥ 20):**

```csharp
// Lance mates share information
public class LanceIntelligenceSystem
{
    // At Rep 20+: Lance mates warn you about:
    // - Incoming shakedowns/inspections
    // - Officer's bad mood (avoid today)
    // - Safe times to do risky things
    // - Rumors about pay, orders, deployment
    
    // At Rep 40+: Lance mates actively cover for you:
    // - "Haven't seen them, sir" when you're missing
    // - Tip you off BEFORE heat/discipline checks
    // - Share loot/supplies quietly
}
```

**Example Event:**
```
[Rep 25+ Only]
{VETERAN_1_NAME} pulls you aside before muster.

"Heads up — {LANCE_LEADER_SHORT} is doing kit inspections today. 
If you've got anything you shouldn't, stash it now."

[Options]
1. Thank them (+5 rep, hide contraband)
2. Ignore the warning (no change, risk discovery)
```

**Blindness (Rep ≤ -20):**

```csharp
// No warnings, no information
// Lance mates actively mislead you:
// - "Officer's in a good mood" (lie)
// - Don't warn about inspections
// - Give false info about orders
// - Set you up to fail
```

---

### 3. Economic/Resource Benefits

**Favor Economy (Rep-Based):**

```csharp
public class LanceFavorSystem
{
    // BORROW from lance mates
    public bool CanBorrowGold(int amount)
    {
        return lanceReputation >= 15 && amount <= GetBorrowLimit();
    }
    
    private int GetBorrowLimit()
    {
        if (lanceReputation >= 40) return 500;  // Bonded: large loans
        if (lanceReputation >= 20) return 200;  // Trusted: moderate loans
        if (lanceReputation >= 10) return 50;   // Decent: small loans
        return 0;
    }
    
    // TRADE/SHARE equipment
    public bool CanTradeWithLanceMates()
    {
        return lanceReputation >= 10;
    }
    
    // POOL resources during crisis
    public bool CanAccessLanceEmergencyFund()
    {
        return lanceReputation >= 25;
    }
}
```

**Example Mechanics:**

| Reputation | Economic Access |
|------------|-----------------|
| **40+ (Bonded)** | Borrow 500 gold, trade equipment freely, share rations |
| **20+ (Trusted)** | Borrow 200 gold, limited trades, ask for supplies |
| **10+ (Accepted)** | Borrow 50 gold, basic trades only |
| **0-9 (Neutral)** | No borrowing, trade at penalty |
| **-20- (Outcast)** | No one trades with you, must buy everything at markup |
| **-40- (Hated)** | Actively sabotaged — your gear goes "missing" |

**Implementation Example:**

```csharp
// In camp activities menu
if (lanceReputation >= 15)
{
    AddMenuOption("Borrow from {LANCE_MATE_NAME}", () =>
    {
        int limit = GetBorrowLimit();
        // Show slider/inquiry to borrow up to limit
        // Repayment required within X days or rep penalty
    });
}

if (lanceReputation >= 10)
{
    AddMenuOption("Trade gear with lance mates", () =>
    {
        // Open special trade screen
        // Lance mates offer fair prices (vs. merchant markup)
    });
}
```

---

### 4. Social Access Gates

**Camp Activities Gated by Reputation:**

```csharp
public class CampSocialGates
{
    // Fire circle (social events, story sharing)
    public bool CanJoinFireCircle()
    {
        return lanceReputation >= -10;  // Outcasts excluded
    }
    
    // Drinking/gambling with lance mates
    public bool CanJoinDrinking()
    {
        return lanceReputation >= 0;  // Must be at least neutral
    }
    
    // Lance mate personal quests/favors
    public bool LanceMateOffersPersonalQuest()
    {
        return lanceReputation >= 20;  // Only trusted soldiers get asked
    }
    
    // Private conversations (deeper relationship events)
    public bool CanHavePrivateConversations()
    {
        return lanceReputation >= 15;
    }
}
```

**Event Availability:**

| Activity | Min Rep | Description |
|----------|---------|-------------|
| **Fire Circle Stories** | -10 | Basic social interaction |
| **Drinking/Gambling** | 0 | Fun social events |
| **Lance Mate Favors** | +15 | Help someone with a problem |
| **Personal Quests** | +20 | Deeper story events |
| **Brotherhood Moments** | +35 | Emotional bonding events |

**Exclusion Feedback:**

```
[Rep -15]
You approach the fire circle. {VETERAN_1_NAME} sees you coming.

"Fire's full tonight." It's not. There's plenty of room.

{LANCE_MATE_NAME} won't meet your eyes.

[Options]
1. Walk away (no change)
2. "I belong here too" (Charm check, -5 rep if failed)
3. Sit down anyway (Force it, -10 rep, creates confrontation)
```

---

### 5. Crisis Support System

**High Rep = Safety Net:**

```csharp
public class LanceCrisisSupport
{
    // When you're in trouble, lance mates help
    public bool LanceMatesWillCoverForYou(int heatLevel, int disciplineLevel)
    {
        if (lanceReputation >= 40)
            return true;  // Bonded: Always cover
        
        if (lanceReputation >= 20 && heatLevel < 8)
            return true;  // Trusted: Cover for minor stuff
        
        return false;  // You're on your own
    }
    
    // Financial emergency support
    public int GetEmergencyGoldFromLance()
    {
        if (lanceReputation >= 40) return 300;  // Bonded: Major help
        if (lanceReputation >= 25) return 150;  // Trusted: Moderate help
        return 0;
    }
    
    // Equipment replacement (if yours is confiscated/lost)
    public bool LanceMatesReplaceYourGear()
    {
        return lanceReputation >= 30;
    }
}
```

**Example Scenarios:**

**Scenario 1: Heat Shakedown (Rep 35)**
```
Officers are doing surprise inspections. You have contraband.

{VETERAN_1_NAME} spots them coming.

"Quick — give it here." They palm your hidden goods. 
"I'll stash it. You're clean."

[Result: Inspection passes. Your contraband is safe. Rep -5 to {VETERAN_1_NAME} for taking the risk.]
```

**Scenario 2: Heat Shakedown (Rep -25)**
```
Officers are doing surprise inspections.

You see {LANCE_MATE_NAME} point in your direction and whisper to the sergeant.

The inspection team heads straight for you.

[Result: They knew exactly where to look. Someone tipped them off.]
```

---

### 6. Failure Cushioning

**High Rep = Second Chances:**

```csharp
public class ReputationFailsafe
{
    // When you fail a duty or make a mistake
    public bool LanceMatesHelp()
    {
        if (lanceReputation >= 25)
        {
            // Lance mates cover your failure
            // "I'll finish that for you"
            // "Don't worry, I fixed it"
            return true;
        }
        return false;
    }
    
    // When you're late to muster
    public bool SomeoneCoveredForYou()
    {
        return lanceReputation >= 20;
    }
    
    // When you mess up in training/battle
    public int GetMistakeForgivenessThreshold()
    {
        if (lanceReputation >= 35) return 3;  // 3 strikes before real trouble
        if (lanceReputation >= 20) return 2;  // 2 strikes
        if (lanceReputation >= 10) return 1;  // 1 strike
        return 0;  // No forgiveness
    }
}
```

**Implementation:**

```csharp
// In duty failure resolution
private void ProcessDutyFailure(string dutyId)
{
    if (lanceReputation >= 25 && MBRandom.RandomFloat < 0.6f)
    {
        // 60% chance lance mate covers for you
        ShowCoverageEvent();
        return;  // No discipline penalty
    }
    
    // Normal failure consequences
    ApplyDisciplinePenalty(2);
}
```

---

### 7. Desertion Pressure

**Low Rep = Pressure to Leave:**

```csharp
public class DeserionPressure
{
    // When pay tension is high AND rep is low
    public bool LanceMatesSuggestYouLeave()
    {
        return lanceReputation <= -15 && payTension >= 50;
    }
    
    // Hostile environment creates desertion incentive
    public int GetDesertionPressureModifier()
    {
        if (lanceReputation <= -40) return +20;  // Heavy pressure
        if (lanceReputation <= -25) return +10;  // Moderate pressure
        if (lanceReputation >= 30) return -10;   // Less likely to leave (bonds)
        return 0;
    }
}
```

**Event Example (Rep -30, Pay Tension 60):**

```
{VETERAN_1_NAME} corners you after muster.

"Look — nobody wants you here. Pay's shit, we're starving, 
and you're dead weight. Do us all a favor and leave."

{LANCE_MATE_NAME} nods agreement. Several others watch, silent.

[Options]
1. "Maybe you're right" (Free desertion option, no penalty)
2. "I have as much right to be here as you" (Confrontation, -5 rep)
3. "I'll leave when I'm ready" (Defiant, creates ongoing tension)
```

---

## Social Systems

### Lance Mate Relationships

**Individual Rep Tracking (Future Enhancement):**

```csharp
// Instead of one aggregate score, track individual relationships
public class LanceMateRelationship
{
    public string LanceMateId { get; set; }
    public int PersonalReputation { get; set; }  // -100 to +100
    public List<string> SharedExperiences { get; set; }
    public CampaignTime LastInteraction { get; set; }
    
    // Different lance mates offer different benefits
    public LanceMateType Type { get; set; }
    // - Veteran: Offers tactical advice, combat tips
    // - Merchant: Can get better gear prices
    // - Scrounger: Finds extra supplies
    // - Storyteller: Boosts morale in camp
    // - Brawler: Protects you in fights
}
```

**Simplified Version (Phase 1):**
- Use aggregate lance reputation
- Flavor text references 3-4 named lance mates
- Benefits apply from "the lance" as a collective

**Enhanced Version (Phase 2+):**
- Track top 3-4 individual lance mate relationships
- Different personalities offer different benefits
- Personal quests unlock at high individual rep

---

### Reputation Visibility

**UI Indicators:**

```
Enlisted Status Menu:
├── Pay Tension: 45 (Stressed)
├── Lance Reputation: 28 (Trusted)      ← Clear visibility
│   └── Status: Your lance mates trust you and have your back.
├── Heat: 3 (Watched)
└── Discipline: 2 (Minor marks)

Camp Activities Menu:
├── [✓] Fire Circle Stories (Rep: 28/10 required)
├── [✓] Borrow from {LANCE_MATE_NAME} (Rep: 28/15 required)
├── [✗] Brotherhood Ceremony (Rep: 28/35 required) ← Locked, shows requirement
```

**In-World Feedback:**

```csharp
// Regular notifications when rep changes
if (repChange > 0)
{
    InformationManager.DisplayMessage(
        $"{lanceMate} nods approvingly. " +
        $"Lance Reputation +{repChange} (Now: {totalRep})",
        Colors.Cyan
    );
}

// Milestone notifications
if (newTier != oldTier)
{
    ShowMilestoneNotification(newTier);
    // "Your lance mates now see you as TRUSTED"
}
```

---

## Combat Systems

### Formation Behavior Modifications

**Implementation in FormationAssignmentBehavior:**

```csharp
public class ReputationBasedFormationBehavior
{
    public void ModifyFormationPlacement(Mission mission)
    {
        var playerAgent = mission.MainAgent;
        var lanceReputation = EscalationManager.Instance.State.LanceReputation;
        
        if (lanceReputation >= 20)
        {
            // Place friendly lance mates closer to player
            var lanceMates = GetLanceMateAgents(mission);
            foreach (var mate in lanceMates)
            {
                mate.SetWantsToYell(true);  // Call out to player
                mate.SetMovementSpeedBonus(1.05f);  // Slight speed boost to stay close
            }
        }
        
        if (lanceReputation <= -20)
        {
            // Lance mates actively avoid player
            var lanceMates = GetLanceMateAgents(mission);
            foreach (var mate in lanceMates)
            {
                mate.SetBehaviorWeight<BehaviorAvoidPlayer>(100);
            }
        }
    }
    
    public float GetMoraleModifier()
    {
        var rep = EscalationManager.Instance.State.LanceReputation;
        
        if (rep >= 40) return 1.10f;  // +10% morale when bonded
        if (rep >= 20) return 1.05f;  // +5% morale when trusted
        if (rep <= -20) return 0.95f; // -5% morale when outcast
        if (rep <= -40) return 0.90f; // -10% morale when hated
        
        return 1.0f;
    }
}
```

### Combat Event Triggers

**Rescue Mechanic (Rep 30+):**

```csharp
// When player is knocked down in battle
private void OnPlayerKnockedDown(Agent player)
{
    if (lanceReputation >= 30 && MBRandom.RandomFloat < 0.7f)
    {
        var nearestLanceMate = FindNearestLanceMate(player.Position);
        if (nearestLanceMate != null && nearestLanceMate.Health > 30)
        {
            // Lance mate attempts rescue
            nearestLanceMate.SetScriptedTargetEntityAndPosition(
                player, player.Position, true
            );
            
            // If successful, player auto-retreats instead of dying
            ShowRescueEvent(nearestLanceMate.Name);
        }
    }
}
```

**Sabotage Mechanic (Rep -40):**

```csharp
// At battle start, chance of equipment "malfunction"
private void OnBattleStart()
{
    if (lanceReputation <= -40 && MBRandom.RandomFloat < 0.25f)
    {
        // 25% chance your gear is sabotaged
        var player = Hero.MainHero;
        
        // Break a random equipment piece
        var equipment = player.BattleEquipment;
        int slot = MBRandom.RandomInt(0, 4);  // Weapon/armor slot
        
        if (!equipment[slot].IsEmpty)
        {
            // Reduce hit points to near-broken
            equipment[slot].HitPoints = 1;
            
            ShowSabotageEvent();
            // "{LANCE_MATE_NAME} 'inspected' your gear last night..."
        }
    }
}
```

---

## Event Integration

### Rep-Gated Event Options

**Existing Events Get Rep-Specific Options:**

```json
{
  "id": "pay_tension_confrontation",
  "options": [
    {
      "id": "call_for_backup",
      "text": "Call on {VETERAN_1_NAME} for support",
      "tooltip": "Your lance mates have your back",
      "condition": "lance_reputation_min:25",
      "risk": "safe",
      "effects": { "lance_reputation": -5 }
    }
  ]
}
```

**New Rep-Specific Events:**

1. **"They've Got Your Back" (Rep 30+)**
   - Trigger: Heat inspection coming
   - Lance mate warns you, offers to hide contraband
   - Options: Accept help (survive inspection) or decline (maintain honor)

2. **"Cold Shoulder" (Rep -15 to -25)**
   - Trigger: Daily camp life
   - Increasingly isolated, excluded from social activities
   - Options: Try to reconcile or embrace loner status

3. **"Set Up to Fail" (Rep -35)**
   - Trigger: Duty assignment
   - Someone sabotages your task (missing supplies, false info)
   - Must overcome extra obstacles or fail publicly

4. **"Blood Brothers" (Rep 45+)**
   - Trigger: After major battle
   - Lance mates perform bonding ceremony, become family
   - Permanent benefits unlock

---

## Reputation Tiers

### Detailed Tier System

| Tier | Range | Label | Mechanical Benefits | Social Access | Risk/Penalty |
|------|-------|-------|---------------------|---------------|--------------|
| **Brotherhood** | 45-50 | "Family" | All benefits max, rescue guaranteed, unlimited loans | All events, private conversations, ceremonies | None |
| **Bonded** | 35-44 | "Trusted Deeply" | Combat support, warnings, 500 gold loans, gear trades | Most events unlocked | None |
| **Trusted** | 20-34 | "Solid" | Information sharing, 200 gold loans, morale bonus | Social events open | None |
| **Accepted** | 10-19 | "One of Us" | Basic trades, 50 gold loans | Fire circle, basic socializing | None |
| **Neutral** | -9 to +9 | "Unknown" | Standard treatment | Limited social access | None |
| **Disliked** | -19 to -10 | "Cold Shoulder" | No help offered, no loans | Excluded from optional events | Mild |
| **Outcast** | -34 to -20 | "Isolated" | Actively avoided, trade penalties | Barred from social events | Moderate |
| **Hated** | -44 to -35 | "Enemy" | Info withheld, no warnings | Completely isolated | Severe |
| **Sabotaged** | -50 to -45 | "Target" | Active sabotage, gear goes missing | Hostile encounters | Critical |

### Tier Transition Events

**Crossing Thresholds Triggers Events:**

```csharp
private void CheckReputationMilestone(int oldRep, int newRep)
{
    // Positive milestones
    if (oldRep < 20 && newRep >= 20)
        TriggerEvent("lance_gained_trust");
    
    if (oldRep < 35 && newRep >= 35)
        TriggerEvent("lance_bonded");
    
    if (oldRep < 45 && newRep >= 45)
        TriggerEvent("lance_brotherhood");
    
    // Negative milestones
    if (oldRep > -20 && newRep <= -20)
        TriggerEvent("lance_outcast");
    
    if (oldRep > -35 && newRep <= -35)
        TriggerEvent("lance_hated");
    
    if (oldRep > -45 && newRep <= -45)
        TriggerEvent("lance_targeted");
}
```

---

## Related Documents

📄 **[Lance Menu Activities](lance_menu_activities.md)** - Comprehensive menu design with all lance interactions

---

## Implementation Phases

### Phase 0: Prerequisites (2-3 hours)

**Goal:** Fix foundational issues before building reputation system

**Tasks:**

1. **Fix Lance Name Uniqueness**
   - **Problem:** Multiple lance mates can have same name ("Bran", "Bran", "Bran")
   - **Impact:** Breaks immersion when events reference multiple lance mates
   - **Solution:** Implement name deduplication during roster generation
   - **See:** `docs/research/lance_name_uniqueness_fix.md` for full implementation plan
   
   ```csharp
   // Track used names during generation
   var usedNames = new HashSet<string>();
   
   // Force epithets or roman numerals on collision
   // "Bran" → "Bran the Bold" → "Bran II" (if needed)
   ```
   
   **Success Criteria:**
   - ✅ All 10 lance mates have unique display names
   - ✅ Same seed produces same roster (deterministic)
   - ✅ Old saves load correctly

---

### Phase 1: Foundation (2-3 hours)

**Goal:** Add visible mechanical gates (requires Phase 0 complete)

**Tasks:**
1. **Camp Activity Gates**
   ```csharp
   // Block fire circle if rep < -10
   if (lanceRep < -10)
   {
       option.IsDisabled = true;
       option.Tooltip = "Your lance mates don't want you around.";
   }
   ```

2. **Borrow System**
   ```csharp
   if (lanceRep >= 15)
   {
       AddMenuOption("Borrow gold from {VETERAN_1_NAME}", () => 
       {
           int limit = GetBorrowLimit(lanceRep);
           ShowBorrowUI(limit);
       });
   }
   ```

3. **Rep Status Display**
   - Add to Enlisted Status UI
   - Show tier label and benefits summary

**Success Criteria:**
- ✅ Players can see their rep and tier
- ✅ Some camp activities locked by low rep
- ✅ Borrowing system functional
- ✅ Clear feedback when rep blocks action

---

### Phase 2: Information & Warnings (3-4 hours)

**Goal:** High rep = advance warnings

**Tasks:**
1. **Warning System**
   ```csharp
   // Before heat/discipline checks
   if (lanceRep >= 20 && heatCheck imminent)
   {
       TriggerWarningEvent("inspection_warning");
   }
   ```

2. **Rep-Gated Event Options**
   - Add "call for backup" options to existing events
   - Require rep 20+ or 30+

3. **Intelligence Sharing**
   - Lance mates share rumors about pay, orders
   - Only at rep 20+

**Success Criteria:**
- ✅ Players get warnings before trouble
- ✅ High rep prevents some bad outcomes
- ✅ Events have rep-specific options

---

### Phase 3: Combat & Crisis (4-6 hours)

**Goal:** Rep matters in battle and emergencies

**Tasks:**
1. **Morale Modifiers**
   ```csharp
   float GetPartyMoraleModifier()
   {
       if (lanceRep >= 40) return 1.10f;
       if (lanceRep >= 20) return 1.05f;
       if (lanceRep <= -20) return 0.95f;
       return 1.0f;
   }
   ```

2. **Crisis Support**
   - Emergency gold from lance mates (rep 25+)
   - Gear replacement if confiscated (rep 30+)

3. **Sabotage System**
   - Low rep = gear "goes missing"
   - Rep < -30 triggers sabotage events

**Success Criteria:**
- ✅ Combat feels different at high/low rep
- ✅ High rep provides safety net in crisis
- ✅ Low rep creates real problems

---

### Phase 4: Social Depth (6-8 hours)

**Goal:** Rich social system with individual relationships

**Tasks:**
1. **Named Lance Mates**
   - Generate 3-4 persistent lance mate characters
   - Track individual rep with each

2. **Personal Quests**
   - Lance mates ask for help (rep 20+)
   - Multi-step quest chains

3. **Brotherhood Events**
   - Bonding ceremonies at rep 40+
   - Deep story moments

**Success Criteria:**
- ✅ Lance mates feel like individuals
- ✅ Personal relationships develop
- ✅ Memorable bonding moments

---

### Phase 5: Polish & Balance (2-3 hours)

**Goal:** Tune numbers, add juice

**Tasks:**
1. **Balance Pass**
   - Adjust rep gain/loss amounts
   - Tune benefit thresholds
   - Test edge cases

2. **UI Polish**
   - Better rep visualization
   - Tooltips explaining benefits
   - Milestone notifications

3. **Audio/Visual Feedback**
   - Sound cues for rep changes
   - Visual indicators in camp

---

## Balance Considerations

### Gaining Reputation

**Easy Gains (+1 to +3):**
- Share supplies in crisis
- Help with camp duties
- Join social activities
- Fight well in battle

**Moderate Gains (+5 to +8):**
- Cover for lance mate's mistake
- Save someone in battle
- Personal favor/quest completion

**Major Gains (+10 to +15):**
- Take punishment meant for someone else
- Heroic combat action
- Resolve major lance conflict

**Decay:** -1 toward 0 every 14 days (prevents permanent max/min)

### Losing Reputation

**Easy Loss (-1 to -3):**
- Selfish choice (take best gear)
- Miss social events
- Cowardly action

**Moderate Loss (-5 to -8):**
- Blame someone else for your mistake
- Refuse to help when asked
- Betray minor confidence

**Major Loss (-10 to -15):**
- Snitch to officers
- Abandon lance mate in danger
- Steal from lance mates

### Progression Rate

**Target Pacing:**
- New player starts at 0 (Neutral)
- Reach Trusted (20) after ~10-15 days of good choices
- Reach Bonded (35) after ~30-40 days of consistent loyalty
- Brotherhood (45+) requires major story events

**Prevents:**
- Too fast: Don't want instant max rep
- Too slow: Must feel progress in reasonable time
- Too volatile: Single bad choice shouldn't undo weeks of work

---

## Quick Reference: What Rep Does

### At a Glance

| Reputation | Can Do | Can't Do |
|------------|--------|----------|
| **-50 to -35** | Survive (barely) | Everything else, actively sabotaged |
| **-34 to -20** | Basic duties | Social events, help, warnings, trades |
| **-19 to -10** | Work, fight | Social events, favors |
| **-9 to +9** | Standard access | Special benefits, loans |
| **+10 to +19** | Small loans, basic trades | Big favors, warnings |
| **+20 to +34** | Warnings, 200g loans, morale bonus | Combat support, big loans |
| **+35 to +44** | Combat support, 500g loans, all trades | Brotherhood ceremonies |
| **+45 to +50** | Everything, rescue guaranteed | Nothing locked |

---

## Implementation Priority

### Must-Have (Phase 1-2)

✅ **Activity Gates** - Low rep locks social events  
✅ **Borrow System** - High rep enables loans  
✅ **Warning System** - High rep = advance warnings  
✅ **Status Display** - Always visible in UI

### Should-Have (Phase 3)

✅ **Morale Impact** - Rep affects party morale  
✅ **Crisis Support** - High rep = safety net  
✅ **Sabotage** - Low rep = active problems

### Nice-to-Have (Phase 4-5)

⭐ **Individual Tracking** - Named lance mate relationships  
⭐ **Personal Quests** - Deep relationship content  
⭐ **Formation Behavior** - Combat positioning based on rep

---

## Testing Checklist

### Functional Tests

- [ ] Rep changes trigger notifications
- [ ] Activity gates work (blocked at low rep)
- [ ] Borrow system functional
- [ ] Rep displays in UI
- [ ] Tier transitions trigger events
- [ ] Morale modifiers apply in combat
- [ ] Warning system fires before trouble
- [ ] Sabotage triggers at low rep
- [ ] Save/load preserves rep
- [ ] Decay works correctly

### Balance Tests

- [ ] Can reach Trusted in ~15 days
- [ ] Can reach Bonded in ~40 days
- [ ] Single bad choice doesn't destroy rep
- [ ] Benefits feel meaningful
- [ ] Penalties feel fair
- [ ] Low rep is playable (hard but viable)

### User Experience Tests

- [ ] Players understand what rep does
- [ ] Clear feedback when rep helps/hurts
- [ ] Obvious when rep gates content
- [ ] Tooltips explain requirements
- [ ] Milestones feel rewarding

---

## Prerequisites

### Lance Name Uniqueness

**MUST FIX FIRST:** Before implementing reputation system, ensure lance mates have unique names.

**Why Critical:**
- Reputation benefits reference specific lance mates
- Events like "lance_mate_helps" need clear attribution
- "{VETERAN_1_NAME} warns you" should never be the same person as "{LANCE_MATE_NAME}"

**Implementation:**
See `docs/research/lance_name_uniqueness_fix.md` for complete fix.

**Estimated Time:** 2-3 hours (should be completed in Phase 0)

---

## Open Questions

1. **Should rep be per-lance or universal?**
   - Per-lance: More realistic, rep resets on transfer
   - Universal: Simpler, "reputation precedes you"
   - **Recommendation:** Per-lance (more realistic)

2. **Should officers be separate from lance mates?**
   - Separate: Officers are authority figures, not friends
   - Combined: Simpler, one relationship system
   - **Recommendation:** Officers separate (different dynamics)

3. **How visible should sabotage be?**
   - Obvious: "Someone sabotaged your gear"
   - Subtle: Gear mysteriously breaks, implied
   - **Recommendation:** Mix (some obvious, some subtle)

4. **Should extremely high rep create problems?**
   - Yes: Lance mates become dependent, demanding
   - No: High rep is purely positive
   - **Recommendation:** No (high rep is reward for investment)

---

## Document History

| Version | Date | Author | Changes |
|---------|------|--------|---------|
| 1.0 | 2025-12-13 | AI Assistant | Initial design document |

---

**END OF DOCUMENT**
