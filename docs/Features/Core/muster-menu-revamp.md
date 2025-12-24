# Muster Menu System

**Summary:** Multi-stage GameMenu sequence replacing the simple pay inquiry popup. Guides players through muster events, displays rank progression and period summary, fires inspection/contraband events as menu stages, and provides transitions to post-muster activities. Creates a complete muster experience with comprehensive reporting.

**Status:** 📋 Specification  
**Last Updated:** 2025-12-24  
**Related Docs:** [Pay System](pay-system.md), [Enlistment](enlistment.md), [Promotion System](promotion-system.md), [Quartermaster System](../Equipment/quartermaster-system.md), [Baggage Train Availability](../Equipment/baggage-train-availability.md), [News & Reporting](../UI/news-reporting-system.md)

---

## Index

- [Overview](#overview)
- [Current System Issues](#current-system-issues)
- [Proposed Solution](#proposed-solution)
- [Menu Flow](#menu-flow)
- [Menu Stages](#menu-stages)
- [Rank Progression Display](#rank-progression-display)
- [Event Integration](#event-integration)
- [Implementation](#implementation)
- [Dependencies](#dependencies)
- [Migration Strategy](#migration-strategy)
- [UI Patterns](#ui-patterns)
- [Data Requirements](#data-requirements)
- [Edge Cases](#edge-cases)
- [Acceptance Criteria](#acceptance-criteria)

---

## Overview

The pay muster occurs every 12 days (configurable: `payday_interval_days`) and is the primary checkpoint for wages, rations, equipment availability, inspections, and rank progression. Currently handled as a single inquiry popup with limited context and no comprehensive reporting.

This specification defines a multi-stage GameMenu sequence that:

1. **Summarizes the 12-day period** - Shows events, battles, gold earned, XP gained
2. **Displays service progression** - Current rank, XP to next rank, unlocks coming
3. **Guides through muster stages** - Pay → Inspections → Events → Summary
4. **Integrates existing events** - Baggage checks, equipment inspections, recruit events
5. **Provides context for decisions** - Shows pay status, supply levels, unit condition before choices
6. **Creates natural transitions** - Direct links to Quartermaster, Service Records after muster
7. **Generates comprehensive reports** - Final summary saved to news feed

---

## Current System Issues

### Current Implementation

**File:** `src/Features/Enlistment/Behaviors/EnlistedIncidentsBehavior.cs`  
**Method:** `ShowPayMusterInquiryFallback()`

```csharp
var inquiry = new MultiSelectionInquiryData(
    "Pay Muster",
    "The paymaster calls the muster. Step forward to receive your pay.",
    options,
    false, 1, 1,
    "Continue", "Cancel",
    OnOptionSelected, OnCancel);
```

**Current Flow:**
1. Day 12 arrives → Inquiry popup appears
2. Player picks pay option (Accept / Corruption / Side Deal / IOU)
3. Pay resolved → Popup closes
4. `OnMusterCycleComplete()` runs automatically:
   - **QM stock refresh** - `QuartermasterManager.RollStockAvailability()` re-rolls item availability based on supply level
   - Ration exchange - tier-appropriate ration issued
   - Baggage check (30% chance) - contraband inspection
   - News report generated - muster outcome recorded
5. Player returns to map

### Problems

1. **No Context** - Player sees pay popup with no reminder of what happened last 12 days
2. **No Progression Visibility** - XP gains, rank progress hidden until player checks status manually
3. **Events Hidden** - Baggage checks happen behind the scenes, not part of muster experience
4. **No Summary** - What happened during muster? Player has to check news feed manually
5. **Abrupt** - Popup → Choice → Map. No sense of occasion or comprehensive reporting
6. **Disconnected Events** - Equipment inspection and recruit events fire randomly in camp, not during muster
7. **No Review Period** - Can't see supply status, unit casualties, or equipment unlocks before deciding

---

## Proposed Solution

Replace the inquiry popup with a **multi-stage GameMenu** that creates a complete muster experience.

### Key Principles

1. **Show Before Tell** - Display context (pay status, supply, casualties) before asking for decisions
2. **Stage the Experience** - Break muster into logical phases (intro → pay → inspections → summary)
3. **Integrate Events** - Fire muster-related events as menu stages, not random popups
4. **Track Progress** - Show rank progression, XP earned, time to next rank
5. **Create Event** - Make muster feel important with comprehensive reporting
6. **Enable Transitions** - Direct access to Quartermaster/Records after muster

### Technical Approach

Use **GameMenu** system (like Camp Hub), not custom Gauntlet or inquiry popups.

**Why GameMenu:**
- Text-based with formatted displays (like Camp Hub status screens)
- Supports options with icons and tooltips
- Pauses game naturally
- Easy to stage (menu → option → next menu)
- Consistent with existing UI patterns
- No custom Gauntlet UI needed

---

## Menu Flow

```
[Map] → _payMusterPending = true
   ↓
[FATIGUE RESET] (automatic, no menu stage)
   • Fatigue restored to maximum (muster day = rest day)
   • Logged in period summary
   ↓
[1. MUSTER INTRO]
   • Shows strategic context flavor text
   • Shows orders summary (completed/failed this period)
   • Shows period summary (events, battles, XP)
   • Shows service record (rank, XP progress)
   • Shows health status (if wounded)
   • Shows pay/supply/unit status
   • Options: Proceed to Pay Line | Visit QM After | Skip Muster
   ↓
[2. PAY LINE]
   • Shows pay status (wages, backpay, lord wealth)
   • Options: Accept Pay | Recount | Side Deal | IOU (conditional)
   ↓
[RATION EXCHANGE] (automatic, no menu stage)
   • T1-T6: Current ration replaced with tier-appropriate ration
   • T7+: Officer rations (exempt)
   • Low supply (<30%): No ration, warning in summary
   ↓
[3. BAGGAGE CHECK] (30% chance, skip if not triggered)
   • If contraband: Shows item, QM rep, options to bribe/smuggle/surrender
   • If clear or rep 65+: Skip to next stage
   ↓
[4. EQUIPMENT INSPECTION] (12-day cooldown, skip if on cooldown)
   • evt_muster_inspection converted to menu stage
   • Options: Perfect Attention | Basic | Unprepared
   ↓
[5. GREEN RECRUIT] (10-day cooldown, T3+, skip if on cooldown)
   • evt_muster_new_recruit converted to menu stage
   • Options: Mentor | Ignore | Haze
   ↓
[6. PROMOTION RECAP] (only if tier-up achieved this period)
   • Acknowledges promotion that occurred during period
   • Shows when promoted, benefits unlocked
   • Reminder to visit QM if not already done
   ↓
[7. RETINUE MUSTER] (T7+ only, automatic)
   • Shows retinue strength, casualties, morale
   • Lists fallen soldiers this period
   • Options: Continue | Recruit After Muster
   ↓
[8. MUSTER COMPLETE]
   • Shows all outcomes (pay received, ration, inspections, XP)
   • Shows period summary (gold, skills, items, unit status)
   • Options: Dismiss | Visit QM | Review Records | Request Leave
   ↓
[Map] → _payMusterPending = false, OnMusterCycleComplete() called
```

### Stage Count by Tier

| Tier | Stages Seen | Notes |
|------|-------------|-------|
| T1-T2 | 4-6 | Intro → Pay → Baggage? → Inspection? → Complete |
| T3-T6 | 5-7 | Intro → Pay → Baggage? → Inspection? → Recruit? → Promotion? → Complete |
| T7-T9 | 6-8 | All stages including Retinue Muster |

**"?" = conditional** - only shows if triggered (cooldown ready, contraband found, tier increased, etc.)

---

## Menu Stages

**Note:** The mockups below use illustrative values (XP, gold, days). Actual values come from config files and runtime state. See [Data Requirements](#data-requirements) for sources.

### 1. Muster Intro Menu

**Menu ID:** `enlisted_muster_intro`

**Display:**

```
⚔  PAY MUSTER - DAY 24  ⚔
Army of Vlandia

The host prepares for the Grand Campaign. Spirits are high as the 
sergeants call the muster. Your company assembles in formation.

_____ YOUR SERVICE RECORD _____

Rank: Corporal (Tier 3)
Days Served This Term: 48 days
Experience: 2400 / 3000 XP (80% to Free Sword)
Health Status: Fit for duty
Baggage Access: Available (muster window)

This Period: +120 XP (3 battles, training, orders completed)
Fatigue: Restored to full (muster rest day)

_____ ORDERS THIS PERIOD _____

• Patrol Duty: Completed (+20 XP, +5 Officer Rep)
• Guard Watch: Completed (+15 XP)
• Camp Patrol: Failed (-8 Officer Rep, +3 Scrutiny)

Orders: 2 completed, 1 failed

_____ EVENTS SINCE LAST MUSTER _____

• Day 13: Dice game in camp - won 15 denars
• Day 16: Battle at Pen Cannoc - Victory (+50 XP)
• Day 18: Extra training session - gained weapon skill (+20 XP)
• Day 21: Comrade repaid borrowed coin
• Day 23: Completed patrol order (+20 XP)

[Pay: 156 denars | Supply: 78% | Unit: 2 lost, 1 sick | Battles: 3]
```

**Supply Crisis Warning:**

When supply drops below 20%, add warning line:

```
⚠️ CRITICAL SUPPLY - Quartermaster has locked all baggage
```

**Strategic Context Flavor Text:**

The intro text varies based on `ArmyContextAnalyzer.GetCurrentContext()`:

| Context | Flavor Text |
|---------|-------------|
| `coordinated_offensive` | "The host prepares for the Grand Campaign. Spirits are high as the sergeants call the muster." |
| `desperate_defense` | "The realm bleeds. Muster is brief—every sword is needed on the line." |
| `siege_operation` | "The walls loom above. Muster proceeds in the muddy siege lines." |
| `raid_operation` | "Smoke rises in the distance. The company assembles between raids." |
| `patrol_peacetime` | "A quiet day on the march. The sergeants call muster at the usual hour." |
| `garrison_duty` | "Within the castle walls, the garrison assembles for pay day." |
| `winter_camp` | "Snow blankets the camp. Men huddle close as the sergeants call muster." |
| `recruitment_drive` | "Fresh faces join the ranks. The company swells as muster is called." |
| *default* | "The sergeants call the muster. Your company assembles in formation." |

**Health Status Display:**

| Condition | Display |
|-----------|---------|
| Full health | "Health Status: Fit for duty" |
| Wounded 50-99% | "Health Status: Wounded (X%) - Light duties" |
| Wounded 30-49% | "Health Status: Badly Wounded (X%) - Limited duties" |
| Wounded <30% | "Health Status: Critically Wounded (X%) - Excused from inspection" |

**Options:**

| Icon | Text | Tooltip | Action |
|------|------|---------|--------|
| 💰 Continue | Proceed to Pay Line | Step forward to receive wages. | → Pay Line Menu |
| 🛒 Trade | Visit Quartermaster After Muster | Browse newly refreshed stock after pay. | → Pay Line Menu (flag QM visit if supply >= 15%) |
| ⏭ Leave | Skip This Muster (Defer) | Handle this later. Available tomorrow. | `DeferPayMuster()` → Map |

**Note:** If supply <15% when muster completes, the "Visit QM After" flag is cleared and player receives message: "Quartermaster unavailable due to critical supply shortage."

**Data Sources:**
- Service record: `EnlistmentBehavior` (rank, days, XP, XP needed)
- Strategic context: `ArmyContextAnalyzer.GetCurrentContext()` (flavor text selection)
- Orders: `OrderManager.GetOrderOutcomesSince(lastMusterDay)` (completed/failed)
- Events: `EnlistedNewsBehavior.PersonalFeed` (filter last 12 days)
- Health: `Hero.MainHero.HitPoints / Hero.MainHero.MaxHitPoints`
- Fatigue: `EnlistmentBehavior.FatigueCurrent` (reset to max at muster start)
- Pay: `EnlistmentBehavior.PendingMusterPay`
- Supply: `CompanySupplyManager.CurrentSupplyPercent`
- Unit status: `EnlistedNewsBehavior` (LostSinceLastMuster, SickSinceLastMuster)
- Battles: Count personal feed battle entries since last muster
- Strategic context: `ArmyContextAnalyzer.GetCurrentContext()` (for displaying operational status)
- Baggage access: `BaggageTrainManager.GetCurrentAccess()` (should return FullAccess during muster)

---

### 2. Pay Line Menu

**Menu ID:** `enlisted_muster_pay`

**Display:**

```
💰  PAYMASTER'S LINE  💰

You step forward to the paymaster's table. He opens his ledger.

_____ PAY STATUS _____

Wages Owed:           156 denars
Backpay Outstanding:  0 denars
Lord's Treasury:      Comfortable
```

**Options:**

| Icon | Text | Tooltip | Condition |
|------|------|---------|-----------|
| 💰 Continue | Accept Your Pay (156 denars) | Standard payment. Full wages owed. | Always |
| 🎲 TakeQuest | Demand a Recount | Roguery/Charm check to extract more coin through creative accounting. | Always |
| ⚔ Trade | Trade Pay for Select Gear | Take 60 denars + choice of premium equipment instead. | Always |
| 📜 Manage | Accept Promissory Note (IOU) | Defer payment 3 days to ease lord's finances. No tension increase. | `PayTensionHigh == true` |
| 🏴 Leave | Process Final Discharge | Receive final pay. Service ends. Pension activated. | `IsPendingDischarge == true` |
| ⚠ Leave | Smuggle Out (Deserter) | Keep all gear. Lose pension. Deserter penalties. | `IsPendingDischarge == true` |

**Actions:**
- Accept Pay → `ResolvePayMusterStandard()` → Next stage
- Recount → `ResolveCorruptionMuster()` → Next stage
- Side Deal → `ResolveSideDealMuster()` → Next stage
- IOU → `ResolvePromissoryMuster()` → Next stage
- Final Discharge → `FinalizePendingDischarge()` → Exit (service ends)
- Smuggle → `ResolveSmuggleDischarge()` → Exit (desertion)

---

### 3. Baggage Check Menu

**Menu ID:** `enlisted_muster_baggage`

**Trigger:** 30% random chance, only if contraband found

**Important:** This stage is a *contraband security inspection*, separate from the logistics-based baggage access system (see [Baggage Train Availability](../Equipment/baggage-train-availability.md)). All soldiers have FullAccess to their baggage during muster regardless of this inspection outcome. The inspection only affects whether contraband is discovered and confiscated.

**Display (Contraband Found):**

```
⚠️  CONTRABAND DISCOVERED  ⚠️

The quartermaster's hand stops in your pack. He pulls out an item
and raises an eyebrow.

Found: Noble Armor (value 1200 denars)

"This doesn't belong to a soldier of your rank," he says quietly.
"I can overlook it... for a price. Or we can do this by the book."

[QM Reputation: 45 - Neutral]
```

**Options (Rep 35-64 - Bribe Available):**

| Icon | Text | Tooltip | Condition |
|------|------|---------|-----------|
| 💰 Trade | Pay Him Off | Charm check. 50% success. Failure increases scrutiny. | QM Rep 35-64 |
| 🤐 TakeQuest | [Roguery 40+] Smuggle It Past | Sleight of hand while distracted. 70% success. Risky. | Roguery 40+ |
| ✋ Manage | Hand It Over | Accept confiscation. Fine + 2 Scrutiny. Keep dignity. | Always |

**Options (Rep <35 - Confiscation Only):**

| Icon | Text | Tooltip | Condition |
|------|------|---------|-----------|
| ✋ Manage | Accept Confiscation | Item confiscated. Fine + 2 Scrutiny. | Always |
| ⚠ TakeQuest | Protest the Seizure | 20% success. Failure adds scrutiny and discipline. | Always |

**Skip Conditions:**
- No contraband found → Skip to next stage
- QM Rep 65+ and contraband found → Auto-pass (QM looks away) → Skip to next stage
- No baggage check triggered (70% chance) → Skip to next stage

**Actions:**
- Fires existing `evt_baggage_*` events as menu instead of inquiry popup
- `EventDeliveryManager` applies effects based on option chosen
- `HandleBaggageCheckOutcome()` processes confiscation/bribe results

---

### 4. Equipment Inspection Menu

**Menu ID:** `enlisted_muster_inspection`

**Trigger:** 12-day cooldown, skip if on cooldown

**Display:**

```
⚔️  EQUIPMENT INSPECTION  ⚔️

The company forms ranks for morning inspection. The captain walks
the line, inspecting each soldier's equipment and bearing.

He stops in front of you, looking you up and down with a critical eye.
```

**Options:**

| Icon | Text | Tooltip | Condition |
|------|------|---------|-----------|
| ⚔ OrderTroopsToAttack | [OneHanded 30+] Stand at Perfect Attention | Flawless presentation. +10 OneHanded XP, +6 Officer Rep, +3 Soldier Rep. | OneHanded 30+ |
| ✅ Manage | Meet the Basic Requirements | Presentable enough. +5 OneHanded XP, +2 Officer Rep. | Always |
| ❌ Leave | You're Not Ready | Slovenly appearance. -8 Officer, -4 Soldier, +8 Scrutiny, +5 Discipline. | Always |

**Action:**
- Fires `evt_muster_inspection` as menu stage instead of camp event
- Effects applied via `EventDeliveryManager`

---

### 5. Green Recruit Menu

**Menu ID:** `enlisted_muster_recruit`

**Trigger:** 10-day cooldown, T3+, skip if on cooldown or T1-T2

**Display:**

```
👥  GREEN RECRUIT  👥

At morning muster, a terrified-looking recruit stands trembling in
the front rank. He's barely holding his spear correctly, and his eyes
dart nervously at every sound.

The veterans are already snickering. As a seasoned soldier, you could
step in—or let him learn the hard way.
```

**Options:**

| Icon | Text | Tooltip | Condition |
|------|------|---------|-----------|
| 🤝 Conversation | [Leadership 25+] Take Him Aside and Train Him | +15 Leadership XP, +30 Sergeant trait XP, +5 Officer, +6 Soldier, +30 troop XP. | Leadership 25+ |
| 🤷 Leave | Let Him Figure It Out | Not your problem. -2 Soldier Rep. | Always |
| 😈 OrderTroopsToAttack | Give Him the Traditional Welcome | Hazing builds character. +4 Soldier, -5 Officer, +8 Discipline. | Always |

**Action:**
- Fires `evt_muster_new_recruit` as menu stage instead of camp event
- Effects applied via `EventDeliveryManager`

---

### 6. Promotion Recap Menu

**Menu ID:** `enlisted_muster_promotion_recap`

**Trigger:** Only if player tier increased during this 12-day period (promotion already occurred)

**Important:** This stage **acknowledges** a promotion that already happened during the period. The actual promotion occurs immediately when requirements are met (via `PromotionBehavior` hourly check and proving event popup). This stage is a formal recap for the service record.

**Display:**

```
⭐  PROMOTION ACKNOWLEDGED  ⭐

The captain addresses the formation. Your promotion is formally 
recognized before the assembled company.

"On Day 20 of this campaign, this soldier was promoted to Sergeant
for distinguished service. The rank has been earned and noted."

The men salute. Your promotion is now part of the official record.

_____ PROMOTION DETAILS _____

Promoted From:       Corporal (Tier 3)
Current Rank:        Sergeant (Tier 4)
Date of Promotion:   Day 20 (8 days ago)

Benefits Unlocked at Promotion:
• Wages increased: 40 → 55 denars/day
• Tier 4 equipment available from Quartermaster
• New authority: Lead small patrols
• Enhanced reputation gains from successful orders

New Abilities Unlocked:
• Request Extra Training (camp decision)
• Organize Squad Drill (camp decision)
• Recommend Comrades for Promotion (social decision)

Current XP Progress: 850 / 6000 XP to Veteran (14%)

The promotion proving event occurred when you earned it. This is a 
formal acknowledgment for your service record.
```

**Options:**

| Icon | Text | Tooltip | Condition |
|------|------|---------|-----------|
| ✅ Continue | Acknowledge Promotion | Proceed with muster. | Always |
| 🛒 Trade | Visit Quartermaster Now | Review newly unlocked equipment. | Always |

**Actions:**
- Continue → Proceed to Retinue Muster (if T7+) or Muster Complete
- Visit QM → Flag `_visitQMAfter = true` → Proceed to next stage

**Detection Logic:**

```csharp
// At muster start, check if tier changed since last muster
int tierAtMusterStart = EnlistmentBehavior.Instance.EnlistmentTier;

if (tierAtMusterStart > _tierAtLastMuster)
{
    _musterState.PromotionOccurredThisPeriod = true;
    _musterState.PromotionDay = EnlistmentBehavior.Instance.DayOfLastPromotion; // NEW
    _musterState.PreviousTier = _tierAtLastMuster;
    _musterState.CurrentTier = tierAtMusterStart;
}
```

**Data Sources:**
- Previous tier: `_tierAtLastMuster` (cached from previous muster)
- Current tier: `EnlistmentBehavior.EnlistmentTier`
- Promotion day: `EnlistmentBehavior.DayOfLastPromotion` (NEW field needed)
- Promotion occurred via proving event popup earlier in period
- See [Promotion System](promotion-system.md#the-promotion-process) for how promotions actually trigger

---

### 7. Retinue Muster Menu (T7+ Only)

**Menu ID:** `enlisted_muster_retinue`

**Trigger:** Tier 7+ with active retinue. Automatic, cannot skip.

**Display:**

```
🏴  RETINUE MUSTER  🏴

Your soldiers form up for inspection. You walk the line, checking
each man's gear and bearing. They salute as you pass.

_____ RETINUE STRENGTH _____

Current Strength:    18 / 20 soldiers
Morale:              Steady (62)
Equipment:           Serviceable
Experience:          Veterans (avg 45 days service)

_____ THIS PERIOD _____

Casualties:          2 killed, 1 wounded
Recruits Added:      0
Desertions:          0

_____ FALLEN THIS PERIOD _____

• Otho the Axeman - Killed at Battle of Pen Cannoc
• Brand of Uxhal - Killed in skirmish near Sargot

[2 recruitment slots available]
```

**Options:**

| Icon | Text | Tooltip | Condition |
|------|------|---------|-----------|
| ✅ Continue | Dismiss Retinue | Proceed to muster summary. | Always |
| 👥 TroopSelection | Recruit After Muster | Open recruitment screen after muster. | `RetinueSize < RetinueCapacity` |

**Actions:**
- Continue → Proceed to Muster Complete stage
- Recruit After Muster → Flag `_recruitRetinueAfter = true` → Proceed to Complete → Open recruitment after muster

**Data Sources:**
- Retinue strength: `RetinueManager.GetRetinueCount()`, `RetinueManager.GetRetinueCapacity()`
- Morale: `RetinueManager.GetRetinueMorale()`
- Casualties: `RetinueManager.GetCasualtiesSince(lastMusterDay)`
- Fallen names: `EnlistedNewsBehavior.GetRetinueCasualties(lastMusterDay)`

**Skip Logic:**

```csharp
// After Promotion stage (or Recruit if no promotion)
if (EnlistmentBehavior.Instance.EnlistmentTier >= 7 && 
    RetinueManager.Instance?.HasRetinue == true)
{
    GameMenu.SwitchToMenu(MusterRetinueMenuId);
}
else
{
    GameMenu.SwitchToMenu(MusterCompleteMenuId);
}
```

**News Integration:**
- Record `RetinueCasualties` and `RetinueStrength` in `MusterOutcomeRecord`
- Fallen soldiers added to personal feed: "Your retinue mourns [Name], lost at [Battle]."

---

### 8. Muster Complete Menu

**Menu ID:** `enlisted_muster_complete`

**Display:**

```
⚔  MUSTER COMPLETE - DAY 24  ⚔

Muster is dismissed. The sergeants release the company.

_____ MUSTER OUTCOMES _____

Pay Received:         156 denars (Full Payment)
Ration Issued:        Field Ration (replaces old Hardtack)
Baggage Check:        Passed - No contraband found
Equipment Inspection: Passed with distinction (+6 Officer Rep)
Supply Status:        78% - Adequate condition

_____ RANK & PROGRESSION _____

Current Rank:         Corporal (Tier 3)
XP This Period:       +120 XP
Total Experience:     850 / 2000 XP to Sergeant (43%)
Status:               1150 XP needed (~10 days at current pace)

XP Sources This Period:
• Daily service: +60 XP (5 XP × 12 days)
• Battles won: +50 XP (3 engagements)
• Orders completed: +20 XP (patrol duty)
• Training: +20 XP (extra drill session)
• Officer reputation gains: +10 XP (inspection bonus)

_____ PERIOD SUMMARY (12 Days) _____

Net Gold:            +171 denars (156 pay + 15 gambling)
Reputation Changes:  +6 Officer, +3 Soldier
Skills Improved:     OneHanded +2, Tactics +1, Leadership +1
Items Acquired:      Vlandian Sword (looted), Leather Gloves (purchased)

Unit Status:         2 casualties, 1 sick, morale steady
Next Muster:         Day 36 (12 days)
```

**Options:**

| Icon | Text | Tooltip | Condition |
|------|------|---------|-----------|
| ✅ Leave | Dismiss | Return to map. Muster complete. | Always |
| 🛒 Trade | Visit Quartermaster | Browse newly refreshed equipment stock and access your baggage. | Supply >= 15% |
| 🛒 Trade | ~~Visit Quartermaster~~ | Quartermaster unavailable. Supplies critically low. Equipment requisitions suspended. | Supply < 15% (disabled) |
| 📋 Manage | Review Service Record | Check your full military history. | Always |
| 🏕 Submenu | Request Temporary Leave | Suspend service after muster. 14-day limit. | Not in combat/siege |

**Actions:**
- Dismiss → `OnMusterCycleComplete()` → Save report to news feed → Record muster outcome → Exit to map
- Visit QM → Open quartermaster conversation via `OpenQuartermasterConversation()` → Baggage also accessible via Camp Hub menu → Return to map after
- Review Records → Open service records menu → Return to map after
- Request Leave → `TemporaryLeaveManager.RequestLeave()` → Exit to map with party visible

**Opening QM Conversation from Muster:**
```csharp
private void OpenQuartermasterConversation()
{
    var enlistment = EnlistmentBehavior.Instance;
    if (enlistment?.IsEnlisted != true)
    {
        ModLogger.Warn("Muster", "Cannot open QM: player not enlisted");
        return;
    }
    
    var qm = enlistment.GetOrCreateQuartermaster();
    if (qm != null && qm.IsAlive)
    {
        ModLogger.Info("Muster", "Opening quartermaster conversation from muster complete menu");
        CampaignMapConversation.OpenConversation(
            new ConversationCharacterData(CharacterObject.PlayerCharacter, PartyBase.MainParty),
            new ConversationCharacterData(qm.CharacterObject, qm.PartyBelongedTo?.Party)
        );
    }
    else
    {
        ModLogger.Error("Muster", "Quartermaster hero unavailable");
        InformationManager.DisplayMessage(new InformationMessage("Quartermaster unavailable."));
    }
}
```

**Note:** Players can access their baggage throughout muster via the Camp Hub menu (separate from muster menu flow). The "Visit Quartermaster" option is a convenience shortcut. Baggage access remains available for 6 hours after muster completes.

**News Integration:**
- Create `MusterOutcomeRecord` and add to `EnlistedNewsBehavior._musterOutcomes`
- Record: `DayNumber`, `PayOutcome`, `PayAmount`, `RationOutcome`, `RationItemId`, `QmReputation`
- Call `AddPayMusterNews(outcome, amountPaid, amountOwed)` for personal feed
- Used for 7-day report archive and camp hub "Recent Activity" display
- See [News & Reporting System](../UI/news-reporting-system.md#musteroutcomerecord) for record structure

---

## Rank Progression Display

### Current Rank Info (Every Muster)

**Format:**
```
Rank: Corporal (Tier 3)
Days Served This Term: 48 days
Experience: 850 / 2000 XP (43% to Sergeant)

This Period: +120 XP (3 battles, training, orders completed)
```

**Data:**
- Current tier: `EnlistmentBehavior._enlistmentTier`
- Rank name: Map tier to culture-specific rank from `progression_config.json` (T1=Follower, T2=Recruit, T3=Free Sword, etc. for generic; Vlandian/Battanian/etc. have localized names)
- Days served: `EnlistmentBehavior.DaysServed`
- Current XP: `EnlistmentBehavior._enlistmentXP`
- XP needed: Config-driven per tier from `progression_config.json` (T2=800, T3=3000, T4=6000, T5=11000, T6=19000, T7=30000, T8=45000, T9=65000)
- Percentage: `(currentXP / xpNeeded) * 100`
- Period XP: Track XP at last muster, show delta

### XP Breakdown (Final Summary)

**Format:**
```
XP Sources This Period:
• Daily service: +60 XP (5 XP × 12 days)
• Battles won: +50 XP (3 engagements)
• Orders completed: +20 XP (patrol duty)
• Training: +20 XP (extra drill session)
• Officer reputation gains: +10 XP (inspection bonus)
```

**Tracking:**
- Create `_xpSourcesThisPeriod` dictionary to track XP sources
- `AddEnlistmentXP(amount, source)` → Store in dictionary
- Display top 5 sources at muster summary
- Reset dictionary after muster complete

### Promotion Detection

**Logic:**
```csharp
private int _tierAtLastMuster = 0;

// In OnMusterCycleComplete(), check:
if (_enlistmentTier > _tierAtLastMuster)
{
    _promotionThisMuster = true;
    _previousTier = _tierAtLastMuster;
    _tierAtLastMuster = _enlistmentTier;
}
```

**Trigger Promotion Menu:**
- If tier changed since last muster, insert promotion recap stage
- Show AFTER recruit, BEFORE retinue/summary
- Acknowledges promotion that already occurred via proving event

---

## Event Integration

### Current Random Events → Muster Stages

**Delivery Method Change:** These events currently use `MultiSelectionInquiryData` popups delivered via `EventDeliveryManager`. The revamp converts them to inline GameMenu stages within the muster sequence. Event effects (gold, XP, reputation, items) still apply via `EventDeliveryManager.ApplyEffects()`, but presentation shifts from popups to menu text.

| Event ID | Current Trigger | New Trigger | Menu Stage |
|----------|----------------|-------------|------------|
| `evt_baggage_lookaway` | 30% at muster, rep 65+ | Same | Baggage Check (auto-pass) |
| `evt_baggage_bribe` | 30% at muster, rep 35-64 | Same | Baggage Check |
| `evt_baggage_confiscate` | 30% at muster, rep <35 | Same | Baggage Check |
| `evt_muster_inspection` | Random camp, 12-day CD | **During muster**, 12-day CD | Equipment Inspection |
| `evt_muster_new_recruit` | Random camp, 10-day CD | **During muster**, 10-day CD | Green Recruit |

### Baggage Check Integration

**Current:** `ProcessBaggageCheck()` in `OnMusterCycleComplete()` queues event via `EventDeliveryManager`

**New:** Call `CheckBaggageInspection()` from menu stage:

```csharp
// In Baggage Check menu init
private void OnBaggageCheckMenuInit(MenuCallbackArgs args)
{
    var result = ContrabandChecker.ScanInventory(_enlistmentTier, playerRole);
    
    if (!result.HasContraband)
    {
        // Skip to next stage
        GameMenu.SwitchToMenu(MusterInspectionMenuId);
        return;
    }
    
    int qmRep = GetQMReputation();
    
    if (qmRep >= 65)
    {
        // QM looks away - auto-pass
        _musterState.BaggageOutcome = "Passed (QM favor)";
        GameMenu.SwitchToMenu(MusterInspectionMenuId);
        return;
    }
    
    // Show contraband options based on rep
    _musterState.ContrabandFound = result;
    _musterState.QMRep = qmRep;
    
    // Build menu text with contraband details
    BuildBaggageCheckText(result, qmRep);
}
```

### Equipment Inspection Integration

**Current:** `evt_muster_inspection` fires randomly from camp via `EventPacingManager`

**New:** Check cooldown in menu stage:

```csharp
// In muster flow, after baggage check
if (IsInspectionAvailable()) // Check 12-day cooldown
{
    GameMenu.SwitchToMenu(MusterInspectionMenuId);
}
else
{
    // Skip to next stage
    GameMenu.SwitchToMenu(MusterRecruitMenuId);
}
```

**Remove from Random Events:**
- Filter `evt_muster_inspection` from `EventPacingManager` selection
- Only trigger during muster menu sequence

---

## Implementation

### New File: MusterMenuHandler.cs

**Location:** `src/Features/Enlistment/Behaviors/MusterMenuHandler.cs`

**Responsibilities:**
1. Register all muster menu stages
2. Track muster state (outcomes, choices made)
3. Build menu text with formatted data
4. Handle menu transitions and stage skipping
5. Generate final summary report
6. Save report to news feed

**Key Classes:**

```csharp
public class MusterMenuHandler : CampaignBehaviorBase
{
    // Menu IDs (8 stages)
    private const string MusterIntroMenuId = "enlisted_muster_intro";
    private const string MusterPayMenuId = "enlisted_muster_pay";
    private const string MusterBaggageMenuId = "enlisted_muster_baggage";
    private const string MusterInspectionMenuId = "enlisted_muster_inspection";
    private const string MusterRecruitMenuId = "enlisted_muster_recruit";
    private const string MusterPromotionRecapMenuId = "enlisted_muster_promotion_recap";
    private const string MusterRetinueMenuId = "enlisted_muster_retinue";  // NEW: T7+
    private const string MusterCompleteMenuId = "enlisted_muster_complete";
    
    private MusterSessionState _currentMuster;
    
    public void BeginMusterSequence();
    
    // Menu initialization handlers
    private void OnMusterIntroInit(MenuCallbackArgs args);
    private void OnMusterPayInit(MenuCallbackArgs args);
    private void OnBaggageCheckInit(MenuCallbackArgs args);
    private void OnInspectionInit(MenuCallbackArgs args);
    private void OnRecruitInit(MenuCallbackArgs args);
    private void OnPromotionInit(MenuCallbackArgs args);
    private void OnRetinueInit(MenuCallbackArgs args);    // NEW: T7+
    private void OnMusterCompleteInit(MenuCallbackArgs args);
    
    // Menu condition handlers (always return true for muster menus)
    private bool OnMusterMenuCondition(MenuCallbackArgs args) => true;
    
    // Text builders
    private string BuildStrategicContextFlavor();         // NEW: Context-based intro
    private string BuildOrdersSummary();                  // NEW: Orders completed/failed
    private string BuildHealthStatus();                   // NEW: Wound status
    private string BuildPeriodSummary();
    private string BuildServiceRecord();
    private string BuildMusterOutcomes();
    private string BuildRankProgression();
    private string BuildRetinueStatus();                  // NEW: T7+ retinue info
    
    // Complete muster and trigger OnMusterCycleComplete()
    private void CompleteMusterSequence();
    
    // Open quartermaster conversation (for "Visit QM" option)
    private void OpenQuartermasterConversation();
}

private class MusterSessionState
{
    // Flow tracking (essential for save/load)
    public string CurrentStage { get; set; }              // Current menu stage ID for resume
    public int MusterDay { get; set; }                    // Day muster started
    public int LastMusterDay { get; set; }                // Previous muster for period calc
    
    // Strategic context (for intro flavor text)
    public string StrategicContext { get; set; }          // From ArmyContextAnalyzer
    
    // Orders summary
    public int OrdersCompleted { get; set; }
    public int OrdersFailed { get; set; }
    public List<string> OrderOutcomes { get; set; }       // Brief descriptions
    
    // Fatigue (reset at muster start)
    public int FatigueBeforeMuster { get; set; }          // For "restored" message
    
    // Outcomes (track for final summary)
    public int PayReceived { get; set; }
    public string PayOutcome { get; set; }                // "full", "partial", "iou", "corruption"
    public string RationOutcome { get; set; }             // "issued", "none", "officer_exempt"
    public string BaggageOutcome { get; set; }            // "passed", "confiscated", "bribed", "skipped"
    public string InspectionOutcome { get; set; }         // "perfect", "basic", "failed", "skipped"
    public string RecruitOutcome { get; set; }            // "mentored", "ignored", "hazed", "skipped"
    
    // Promotion tracking (promotion occurs via PromotionBehavior, recap shows at muster)
    public bool PromotionOccurredThisPeriod { get; set; } // True if tier changed since last muster
    public int PreviousTier { get; set; }                 // Tier at last muster
    public int CurrentTier { get; set; }                  // Tier at this muster
    public int PromotionDay { get; set; }                 // Campaign day when promotion occurred
    
    // Contraband (needed for baggage stage display)
    public ContrabandCheckResult ContrabandFound { get; set; }
    
    // Retinue (T7+ only)
    public int RetinueStrength { get; set; }
    public int RetinueCapacity { get; set; }
    public int RetinueCasualties { get; set; }
    public int RetinueWounded { get; set; }
    public List<string> FallenRetinueNames { get; set; }  // Names of fallen soldiers
    
    // Post-muster flags
    public bool VisitQMAfter { get; set; }
    public bool RecruitRetinueAfter { get; set; }         // Open retinue recruitment
    public bool RequestLeaveAfter { get; set; }           // Start temporary leave
    public List<string> PendingEscalationEvents { get; set; }  // Queue for after muster
}

// Note: Other data (lord gold, QM rep, supply%, scrutiny, health%) is queried
// on-demand from source systems rather than cached in session state.
// This keeps the state minimal and avoids stale data issues.
```

#### Menu Registration

**Pattern follows `EnlistedMenuBehavior.cs` conventions:**

```csharp
// In CampaignBehaviorBase.RegisterEvents()
public override void RegisterEvents()
{
    CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
}

// In OnSessionLaunched
private void OnSessionLaunched(CampaignGameStarter starter)
{
    RegisterMusterMenus(starter);
}

// Register all muster menus
private void RegisterMusterMenus(CampaignGameStarter starter)
{
    // Muster Intro - Wait menu with time control
    starter.AddWaitGameMenu(MusterIntroMenuId,
        "{MUSTER_INTRO_TEXT}",                       // Title + description
        OnMusterIntroInit,                            // Init handler (builds text)
        OnMusterMenuCondition,                        // Condition (always true)
        null,                                         // No consequence
        OnMusterMenuTick,                             // Tick handler (no-op)
        GameMenu.MenuAndOptionType.WaitMenuHideProgressAndHoursOption);
    
    // Intro options
    starter.AddGameMenuOption(MusterIntroMenuId, "muster_continue",
        "{=muster_continue}Proceed to Pay Line",
        args => {
            args.optionLeaveType = GameMenuOption.LeaveType.Continue;
            args.Tooltip = new TextObject("{=muster_continue_tt}Step forward to receive wages.");
            return true;
        },
        _ => GameMenu.SwitchToMenu(MusterPayMenuId),
        false, 1);
    
    // Repeat for all menu stages...
}

// No-op tick handler for wait menus
private static void OnMusterMenuTick(MenuCallbackArgs args, CampaignTime dt) { }
```

**Key Pattern Elements:**
- Use `AddWaitGameMenu()` for menus that show status/information
- Use `WaitMenuHideProgressAndHoursOption` to hide time progress bars
- Text variable placeholders in menu title (e.g., `{MUSTER_INTRO_TEXT}`)
- Build text in `OnInit` handlers, set via `MBTextManager.SetTextVariable()`
- Options added separately with `AddGameMenuOption()`
- `args.optionLeaveType` for icons, `args.Tooltip` for hover text
- All tooltips use `TextObject` with localization IDs

#### Integration Points

**EnlistmentBehavior.cs:**  
**Location:** `src/Features/Enlistment/Behaviors/EnlistmentBehavior.cs` (line ~4810)

```csharp
// OLD:
EnlistedIncidentsBehavior.Instance?.TriggerPayMusterIncident();

// NEW:
MusterMenuHandler.Instance?.BeginMusterSequence();
```

**EnlistmentBehavior.cs - Add Tracking:**

```csharp
private int _tierAtLastMuster = 0;
private int _xpAtLastMuster = 0;
private Dictionary<string, int> _xpSourcesThisPeriod = new();

public void AddEnlistmentXP(int amount, string source)
{
    _enlistmentXP += amount;
    
    if (_xpSourcesThisPeriod.ContainsKey(source))
        _xpSourcesThisPeriod[source] += amount;
    else
        _xpSourcesThisPeriod[source] = amount;
}

public Dictionary<string, int> GetXPSourcesThisPeriod() => _xpSourcesThisPeriod;
public void ResetXPSources() => _xpSourcesThisPeriod.Clear();
```

**EventPacingManager.cs - Filter Muster Events:**

```csharp
// In GetEligibleCandidates()
// Skip muster-specific events - they fire during muster menu now
if (evt.Id == "evt_muster_inspection" || evt.Id == "evt_muster_new_recruit")
    continue;
```

**QuartermasterManager Integration:**

The quartermaster system is already fully implemented and integrated. Key integration points:

```csharp
// In CompleteMusterSequence() or OnMusterCycleComplete():

// 1. Refresh quartermaster stock availability (already implemented)
QuartermasterManager.Instance?.RollStockAvailability();
ModLogger.Info("Muster", "QM stock availability refreshed based on supply level");

// 2. Process ration exchange (already implemented)
ProcessRationExchange();

// 3. Process baggage check if triggered (30% chance)
ProcessBaggageCheck();

// 4. Mark newly unlocked items for "Visit QM After" option
QuartermasterManager.Instance?.UpdateNewlyUnlockedItems();
```

**Stock Refresh Details:**
- `RollStockAvailability()` is called automatically at muster completion
- Re-rolls item availability based on current `CompanySupplyManager` supply level
- Low supply = higher chance of items being out of stock
- Out-of-stock items remain unavailable until next muster (12 days)
- Newly unlocked items (tier-up) are marked with [NEW] indicator
- Stock quantities refresh via `RefreshInventoryAtMuster()` internal method

### Logging Standards

Use `ModLogger` for all diagnostic output. Logs write to `<BannerlordInstall>\Modules\Enlisted\Debugging\`.

**Categories:**
```csharp
ModLogger.Info("Muster", $"Beginning muster sequence on day {day}");
ModLogger.Debug("Muster", $"Stage: {stage}, Outcome: {outcome}");
ModLogger.Warn("Muster", "Contraband found but QM reputation check failed");
ModLogger.Error("Muster", "Failed to transition to next stage", exception);
```

**Log Key Events:**
- Muster sequence start/complete
- Stage transitions
- Pay outcomes
- Baggage check results
- Promotion triggers
- Any errors with full exception details

**Error Codes:**
- `E-MUSTER-001` - Menu registration failed
- `E-MUSTER-002` - Stage transition failed
- `E-MUSTER-003` - State restoration failed (save/load)
- `E-MUSTER-004` - Effect application failed
- `E-MUSTER-005` - Unhandled exception in muster sequence

---

## Dependencies

### Required Systems

These systems must exist and be functional before implementing the muster menu:

| System | Purpose | File |
|--------|---------|------|
| `EnlistmentBehavior` | Pay, XP, tier, days served, fatigue | `EnlistmentBehavior.cs` |
| `EnlistedNewsBehavior` | Personal feed, muster outcome recording | `EnlistedNewsBehavior.cs` |
| `CompanySupplyManager` | Supply percentage | `CompanySupplyManager.cs` |
| `EscalationManager` | Scrutiny, discipline levels | `EscalationManager.cs` |
| `QuartermasterManager` | QM reputation, stock refresh | `QuartermasterManager.cs` |
| `ContrabandChecker` | Inventory scanning for contraband | `ContrabandChecker.cs` |
| `EventDeliveryManager` | Effect application for events | `EventDeliveryManager.cs` |
| `OrderManager` | Orders completed/failed tracking | `OrderManager.cs` |
| `ArmyContextAnalyzer` | Strategic context detection | `ArmyContextAnalyzer.cs` |
| `RetinueManager` | Retinue strength, casualties (T7+) | `RetinueManager.cs` |
| `TemporaryLeaveManager` | Leave request handling | `TemporaryLeaveManager.cs` |
| `BaggageTrainManager` | Baggage access state, muster grants FullAccess | `BaggageTrainManager.cs` |

### Required Changes to Existing Systems

**CRITICAL: BaggageTrainManager.cs - Add Muster Access Check:**

The baggage train spec says muster grants FullAccess, but this is **not yet implemented** in `BaggageTrainManager.GetCurrentAccess()`. Must add between existing Priority 7 (Siege) and Priority 8 (Settlement):

```csharp
// Priority 7.5: Muster Window - grants access for 6 hours
var enlistment = EnlistmentBehavior.Instance;
if (enlistment != null && enlistment.PayMusterPending)
{
    ModLogger.Debug(LogCategory, "GetCurrentAccess: Muster active, returning FullAccess");
    return BaggageAccessState.FullAccess;
}

// Check post-muster window (6 hours after muster completes)
if (enlistment != null && enlistment.LastMusterCompletionTime > CampaignTime.Zero)
{
    var hoursSinceMuster = (CampaignTime.Now.ToHours - enlistment.LastMusterCompletionTime.ToHours);
    if (hoursSinceMuster < 6.0f)
    {
        ModLogger.Debug(LogCategory, $"GetCurrentAccess: Post-muster window active ({hoursSinceMuster:F1}h remaining)");
        return BaggageAccessState.FullAccess;
    }
}
```

**EnlistmentBehavior.cs - Add Required Fields:**

```csharp
// Add to EnlistmentBehavior class
private CampaignTime _lastMusterCompletionTime = CampaignTime.Zero;

public bool PayMusterPending => _payMusterPending;
public CampaignTime LastMusterCompletionTime => _lastMusterCompletionTime;

// In OnMusterCycleComplete() or equivalent, after muster finishes:
_lastMusterCompletionTime = CampaignTime.Now;
ModLogger.Info("Muster", "Muster complete - 6h baggage access window begins");
```

**EnlistmentBehavior.cs - Track Promotion Date:**

Add new field to track when the last promotion occurred (for recap display):

```csharp
private int _dayOfLastPromotion = 0;  // Campaign day when last promotion occurred

public int DayOfLastPromotion => _dayOfLastPromotion;

// In SetTier() method, after tier is set:
_dayOfLastPromotion = (int)Campaign.Current.CampaignStartTime.ElapsedDaysUntilNow;
```

**PromotionBehavior.cs - No Changes Required:**

The existing promotion system (hourly checks, proving events) continues to work as-is. The muster menu only adds a recap stage to acknowledge promotions that already occurred.

### Required Data

| File | Required Content |
|------|------------------|
| `progression_config.json` | XP thresholds per tier, culture-specific rank names |
| `enlisted_config.json` | `payday_interval_days`, wage formula, army bonus |
| `enlisted_strings.xml` | All `muster_*` localization strings (titles, options, tooltips) |
| `muster_events.json` | Event definitions for inspection, recruit, baggage |

### New Files Required

| File | Purpose |
|------|---------|
| `src/Features/Enlistment/Behaviors/MusterMenuHandler.cs` | Main handler, menu registration, stage logic |
| `ModuleData/Languages/enlisted_strings.xml` | Add ~50 new `muster_*` string entries |

---

## Migration Strategy

### Phase 1: Parallel Implementation

**PREREQUISITE:** Implement muster access check in `BaggageTrainManager` (see Required Changes section above). Without this, muster won't grant baggage access as spec requires.

1. Implement `MusterMenuHandler` with all stages
2. Add feature flag `use_new_muster_menu` to `settings.json` (default: `false`)
3. Add `PayMusterPending` property and `LastMusterCompletionTime` field to `EnlistmentBehavior`
4. Update `BaggageTrainManager.GetCurrentAccess()` to check muster state (Priority 7.5)
5. Both systems coexist - flag controls which triggers:

```csharp
// In EnlistmentBehavior.TriggerPayMuster()
if (EnlistedSettings.UseNewMusterMenu)
    MusterMenuHandler.Instance?.BeginMusterSequence();
else
    EnlistedIncidentsBehavior.Instance?.TriggerPayMusterIncident();
```

### Phase 2: Testing

1. Set `use_new_muster_menu: true` in settings
2. Play through multiple muster cycles
3. Verify all edge cases (see Edge Cases section)
4. Compare outcomes with old system (pay amounts, events triggered)
5. Test save/load mid-muster

### Phase 3: Cutover

1. Remove feature flag, new system becomes default
2. Mark `ShowPayMusterInquiryFallback()` as `[Obsolete]`
3. Clean up old incident-based trigger code after 2-3 releases

### Rollback Plan

If critical issues discovered post-release:
1. Re-enable feature flag in settings
2. Set `use_new_muster_menu: false` to revert
3. Old system still functional as fallback

---

## UI Patterns

### Text Formatting

Use existing color scheme from `GUI/Brushes/EnlistedColors.xml` (see [Color Scheme](../UI/color-scheme.md)):

```csharp
var header = $"<span style=\"Header\">_____ {title} _____</span>";        // Cyan
var label = $"<span style=\"Label\">{label}:</span>";                     // Teal
var success = $"<span style=\"Success\">{value}</span>";                  // Light Green
var warning = $"<span style=\"Warning\">{value}</span>";                  // Gold
var alert = $"<span style=\"Alert\">{value}</span>";                      // Soft Red
var defaultText = $"<span style=\"Default\">{value}</span>";              // Warm Cream
```

### Menu Option Icons

Use `GameMenuOption.LeaveType` for icons:

| Type | Icon | Usage |
|------|------|-------|
| `Continue` | Arrow right | Proceed to next stage |
| `Leave` | Exit door | Dismiss, defer, exit |
| `Trade` | Coins | Quartermaster, side deals |
| `Manage` | Clipboard | Service records, inspect |
| `Conversation` | Speech bubble | Social options, mentoring |
| `TakeQuest` | Quest marker | Risky options, skill checks |
| `OrderTroopsToAttack` | Sword | Military actions, inspections |

### Tooltip Patterns

```csharp
args.Tooltip = new TextObject("Action description. Outcomes. Requirements.");

// Examples:
"Standard payment. Full wages owed."
"Roguery/Charm check to extract more coin through creative accounting."
"Flawless presentation. +10 OneHanded XP, +6 Officer Rep, +3 Soldier Rep."
```

### Localization

All menu text, option labels, and tooltips use `TextObject` with string IDs:

```csharp
// Menu titles and descriptions
var title = new TextObject("{=muster_intro_title}PAY MUSTER - DAY {DAY}");
title.SetTextVariable("DAY", (int)CampaignTime.Now.ToDays);

// Option labels
var optionText = new TextObject("{=muster_pay_accept}Accept Your Pay ({AMOUNT} denars)");
optionText.SetTextVariable("AMOUNT", pendingPay);

// Tooltips
args.Tooltip = new TextObject("{=muster_pay_accept_tooltip}Standard payment. Full wages owed.");
```

String IDs are defined in `ModuleData/Languages/enlisted_strings.xml` with `muster_` prefix for muster menu content.

### Audio Feedback

| Event | Sound | Implementation |
|-------|-------|----------------|
| Muster Start | None | Silent transition |
| Pay Received | Coin jingle | `InformationManager.DisplayMessage()` with `SoundEvent` |
| Promotion | Fanfare/cheer | Use native `SoundEvent.Play("event:/ui/notification/quest_finished")` |
| Contraband Found | Alert tone | Use native warning sound |
| Muster Dismissed | None | Silent exit |

**Note:** Use native Bannerlord `SoundEvent` system. No custom audio files required. Sounds are optional enhancement - system works without them.

### Skipped Stage Display

When stages are skipped, show in final summary:

| Stage | Skipped Reason | Display Text |
|-------|----------------|--------------|
| Baggage Check | No check triggered | "Baggage Check: Not conducted" |
| Baggage Check | Empty inventory | "Baggage Check: Nothing to inspect" |
| Baggage Check | QM favor | "Baggage Check: Passed (QM favor)" |
| Equipment Inspection | Cooldown | "Equipment Inspection: Not scheduled" |
| Equipment Inspection | Wounded | "Equipment Inspection: Excused (wounds)" |
| Green Recruit | Low tier | "Recruit Training: Not eligible (T3+ required)" |
| Green Recruit | Cooldown | "Recruit Training: No new recruits" |
| Promotion | No tier change | (Omit entirely - don't show) |

---

## Data Requirements

### Period Tracking

**Add to EnlistmentBehavior.cs:**

```csharp
private int _lastMusterDay = 0;

// At muster start:
_lastMusterDay = (int)CampaignTime.Now.ToDays;

// To get period length:
int daysSinceLastMuster = (int)CampaignTime.Now.ToDays - _lastMusterDay;
```

### Event Collection

**Query from EnlistedNewsBehavior:**

```csharp
public List<DispatchItem> GetPersonalFeedSince(int dayNumber)
{
    return _personalFeed
        .Where(item => item.Day >= dayNumber)
        .OrderBy(item => item.Day)
        .ToList();
}
```

### XP Sources Breakdown

**Track in AddEnlistmentXP():**

```csharp
// Callers provide source:
AddEnlistmentXP(5, "Daily Service");
AddEnlistmentXP(50, "Battle Victory");
AddEnlistmentXP(20, "Order Completed");
AddEnlistmentXP(20, "Training");
AddEnlistmentXP(10, "Reputation Gain");
```

### Unit Status Since Last

**From EnlistedNewsBehavior:**

```csharp
public int GetLostSinceLastMuster() => _lostSinceLastMuster;
public int GetSickSinceLastMuster() => _sickSinceLastMuster;
```

### Muster State Tracking (For Baggage Integration)

**Add to EnlistmentBehavior:**

```csharp
private CampaignTime _lastMusterCompletionTime = CampaignTime.Zero;

// Public properties for BaggageTrainManager
public bool PayMusterPending => _payMusterPending;
public CampaignTime LastMusterCompletionTime => _lastMusterCompletionTime;

// Update in OnMusterCycleComplete() or final muster stage:
private void CompleteMusterSequence()
{
    _payMusterPending = false;
    _lastMusterCompletionTime = CampaignTime.Now;
    // ... rest of completion logic
}
```

**Purpose:** Allows `BaggageTrainManager` to detect:
1. Active muster (`PayMusterPending == true`) → Grant FullAccess
2. Post-muster window (within 6h of `LastMusterCompletionTime`) → Grant FullAccess
3. Otherwise → Normal access evaluation

---

## Edge Cases

### Muster Trigger Edge Cases

| Scenario | Handling |
|----------|----------|
| **Player in combat** when muster due | Defer until combat ends. Set `_musterPendingAfterCombat = true`. |
| **Player captured/prisoner** | Muster skipped entirely. Wages accumulate as backpay. Resume on release. |
| **Player on temporary leave** | Muster triggers normally when player returns to camp. Leave ends at muster. |
| **Player in town/settlement** | Muster triggers normally. Town menu closes, muster menu opens. |
| **Player in conversation** | Wait for conversation end. Queue muster trigger. |
| **Player in another GameMenu** | Close current menu, open muster. Preserve return state if needed. |
| **Lord dies during muster period** | Complete muster with deceased lord's gold. Auto-discharge after muster. |
| **Lord changes parties** | Muster completes with original lord. New lord relationship starts fresh. |
| **Two musters queue** (edge timing) | Process first muster only. Reset timer. Second muster fires on schedule. |
| **First muster ever** | `_lastMusterDay = 0`. Period shows "Since Enlistment" instead of 12-day summary. |

### Pay Stage Edge Cases

| Scenario | Handling |
|----------|----------|
| **Lord has zero gold** | Show "Lord's Treasury: Empty". Only IOU option available. Add pay tension. |
| **Lord has partial gold** | Show partial payment option. "Accept Partial (X of Y denars)". Remainder becomes backpay. |
| **Backpay exceeds current wages** | Show backpay amount prominently. "Backpay Outstanding: X denars". Pay backpay first. |
| **Player on probation** | Wages reduced 50%. Show "(Probation Rate)" next to amount. |
| **Player in active army** | Wages +20%. Show "(Army Bonus)" next to amount. |
| **Pay tension at maximum (100)** | Show warning. "⚠️ MUTINY RISK". Discipline event triggers after muster if not resolved. |
| **Maximum gold cap** | Unlikely but handle overflow gracefully. Cap at game's max int. |
| **Pending discharge + pay** | Show discharge options. Calculate final pay + pension. |

### Baggage Check Edge Cases

| Scenario | Handling |
|----------|----------|
| **Empty inventory** | Skip baggage stage entirely. "Nothing to inspect." |
| **Multiple contraband items** | Check highest-value contraband first. One item per muster. |
| **Just bought from QM** | QM-purchased items exempt from contraband. Mark with `_isQMPurchase` flag. |
| **QM rep exactly 35** | Use `>=` comparison. Rep 35 gets bribe option. |
| **QM rep exactly 65** | Use `>=` comparison. Rep 65 auto-passes. |
| **Can't afford bribe** | Bribe option disabled (greyed out). Tooltip: "Requires X denars." |
| **Player paid bribe but failed roll** | Gold taken, item still confiscated, scrutiny increased. |
| **Contraband is equipped** | Force unequip before confiscation. Notify player. |

### Inspection Event Edge Cases

| Scenario | Handling |
|----------|----------|
| **Player wounded (hitpoints < 30%)** | Skip inspection. "Too wounded to stand formation." No penalty. |
| **Player has no weapon equipped** | Use highest melee skill for check. Fallback to Athletics if no combat skills. |
| **Skill exactly at threshold (30)** | Use `>=` comparison. 30 OneHanded qualifies for "Perfect Attention". |
| **Cooldown just reset vs mid-cycle** | Check `DaysSinceLastInspection >= 12`. Track in `_lastInspectionDay`. |
| **Failed inspection recently** | No special handling. Player can fail again. Consequences stack. |
| **Player promoted during muster** | Use tier at muster start for inspection. New tier benefits apply next muster. |

### Recruit Event Edge Cases

| Scenario | Handling |
|----------|----------|
| **Player is T2** (below T3 requirement) | Skip recruit stage. Not senior enough to mentor. |
| **Player just promoted to T3 this muster** | Use tier at muster start. If was T2, skip. If was T3, include. |
| **No Leadership skill (0)** | Mentor option unavailable. Only Ignore and Haze options. |
| **Leadership exactly 25** | Use `>=` comparison. 25 Leadership qualifies for mentoring. |
| **Recruit event already fired in camp** | Check cooldown. If camp event was within 10 days, skip menu stage. |

### Promotion Edge Cases

| Scenario | Handling |
|----------|----------|
| **Multi-tier promotion** (T5→T7 in one period) | Show single recap acknowledging all tiers gained. "Promoted from T5 Blade to T7 Captain." |
| **Promoted same day as muster** | Recap shows "0 days ago" or "earlier today". Still display recap for record. |
| **XP met but other requirements not** | No promotion. Show "Promotion Blocked" in status. List unmet requirements. |
| **Promoted then demoted** (discipline issue) | Show demotion warning, not promotion recap. Special demotion notice. |
| **T7 promotion (retinue grant)** | Recap acknowledges retinue grant. "You now command a retinue of 20 soldiers." |
| **T9 promotion (max rank)** | Final promotion recap. "You have reached the pinnacle of military service." No "XP to next" shown. |
| **Promotion requirements include battles** | If battles < required, show "Needs X more battles before promotion eligible." |

### Data & Display Edge Cases

| Scenario | Handling |
|----------|----------|
| **No events since last muster** | Show "A quiet period. Nothing of note occurred." |
| **No battles fought** | Show "No engagements." in battle summary. |
| **Personal feed empty** | Graceful fallback. "No personal dispatches recorded." |
| **XP sources dictionary empty** | Show "Daily Service" as default source with all XP. |
| **Very long event text** | Truncate at 80 characters with "...". Full text in tooltip. |
| **Large numbers (10000+ gold)** | Use thousands separator. "10,000 denars". |
| **Negative reputation** | Show with minus sign and Alert color. "-15 Lord Reputation". |
| **Missing localization string** | Fallback to string ID. Log warning. "{=missing_string_id}". |

### Post-Muster Transition Edge Cases

| Scenario | Handling |
|----------|----------|
| **"Visit QM" but camp unavailable** | Disable option. Tooltip: "Must be encamped to access Quartermaster." |
| **Lord moved during muster** | Teleport player to new location. Log event. |
| **Party state changed** (lord disbanded) | Force discharge. Show emergency discharge message. |
| **Player equipped new contraband during muster** | Already inspected. Next muster will catch it. |
| **Game saved mid-muster** | Save `MusterSessionState` to save file. Resume from current stage on load. |

### Escalation Edge Cases

| Scenario | Handling |
|----------|----------|
| **High scrutiny during muster** | Show warning banner: "⚠️ Under Scrutiny - Conduct yourself carefully." |
| **Muster event pushes scrutiny over threshold** | Apply threshold event AFTER muster completes. Queue for next hour tick. |
| **Discipline discharge threshold reached** | Show discharge warning in summary. Trigger discharge after muster complete. |
| **Both high scrutiny AND high discipline** | Handle most severe first. Discharge takes priority over warnings. |

### Orders Summary Edge Cases

| Scenario | Handling |
|----------|----------|
| **No orders this period** | Show "No orders issued this period." |
| **All orders completed** | Show count with positive tone: "Orders: 3 completed, 0 failed - Exemplary service." |
| **All orders failed** | Show count with warning: "Orders: 0 completed, 2 failed - Command is displeased." |
| **OrderManager unavailable** | Show "Order records unavailable." Log warning. |

### Fatigue & Health Edge Cases

| Scenario | Handling |
|----------|----------|
| **Fatigue already at max** | Show "Fatigue: Already well-rested" instead of "Restored to full." |
| **Fatigue was critical (0-4)** | Show "Fatigue: Restored from exhaustion (was 3/24)." |
| **Player at full health** | Show "Health Status: Fit for duty." |
| **Player wounded 50-99%** | Show "Health Status: Wounded (X%) - Light duties only." |
| **Player wounded <30%** | Show "Health Status: Critically Wounded - Excused from inspection." |
| **FatigueManager unavailable** | Skip fatigue display. Log warning. |

### Strategic Context Edge Cases

| Scenario | Handling |
|----------|----------|
| **ArmyContextAnalyzer unavailable** | Use default flavor: "The sergeants call the muster." |
| **Context changes mid-muster** | Use context captured at muster start. Don't update mid-flow. |
| **Unknown context tag** | Use default flavor. Log warning with unknown tag. |

### Leave Request Edge Cases

| Scenario | Handling |
|----------|----------|
| **Request Leave during siege** | Option disabled. Tooltip: "Cannot request leave during active siege." |
| **Request Leave during battle** | Option disabled. Tooltip: "Cannot request leave in combat zone." |
| **Already on probation** | Option disabled. Tooltip: "Leave denied - currently on probation." |
| **Leave timer from previous leave** | Show cooldown if applicable. "Leave available in X days." |
| **TemporaryLeaveManager unavailable** | Option disabled. Tooltip: "Leave system unavailable." |

### Baggage Access Edge Cases

| Scenario | Handling |
|----------|----------|
| **Supply <20% during muster** | Show warning banner: "⚠️ CRITICAL SUPPLY - Quartermaster has locked all baggage". Muster grants access but baggage is Locked state (Priority 3 overrides muster). |
| **Baggage delayed when muster triggers** | **IMPLEMENTATION NEEDED:** Add muster check to `BaggageTrainManager.GetCurrentAccess()` between Priority 7 (Siege) and Priority 8 (Settlement). Check `EnlistmentBehavior._payMusterPending` or time within 6h of last muster. Overrides delay temporarily. |
| **Player defers muster** | Baggage access window also deferred. No special access granted until player proceeds with muster. Delay countdown continues normally. |
| **Player accesses baggage mid-muster** | Allowed via Camp Hub menu. Doesn't interrupt muster flow. Return to muster menu after stash screen closes. |
| **Muster triggers while stash screen open** | Native inventory screen remains open. Muster inquiry will queue until screen closes. Baggage access persists. |
| **Emergency baggage request during muster** | Unnecessary (already have FullAccess). QM dialogue should detect muster window and skip rep cost. Context: `is_muster_window: true`. |
| **Baggage "caught up" event fires during muster** | Skip event or auto-succeed. Don't queue event while `_payMusterPending == true`. Muster already provides access. |
| **NCO daily window (T5+) overlaps with muster** | Both grant access. NCO window timer doesn't reset. Multiple access sources are fine (they don't stack or conflict). |
| **Commander (T7+) uses halt column at muster** | QM dialogue option should be disabled. Tooltip: "Already have access during muster." |
| **Temporary access window expires mid-muster** | Muster access takes precedence. After muster window ends, revert to normal evaluation. |
| **Supply drops below 20% mid-muster** | Baggage becomes Locked immediately (Priority 3 overrides). Show warning if player tries to access. Contraband inspection still proceeds (separate system). |
| **Baggage delay expires during muster** | Delay clears normally. After muster window ends, access remains available (delay is gone). |
| **Contraband inspection vs access** | These are separate systems. Security inspection checks for illegal items; access system determines availability. Both operate independently. Inspection proceeds even if baggage is Locked. |
| **Supply <30% = no ration, <20% = locked baggage** | Different thresholds for different systems. Show both warnings when applicable. Ration exchange happens regardless of baggage state (goes to inventory). |
| **Muster access window (6h) boundary** | Track `_lastMusterCompletionTime` in `EnlistmentBehavior`. `BaggageTrainManager` checks if `CampaignTime.Now < _lastMusterCompletionTime + 6 hours`. |
| **Cross-faction transfer at muster** | Baggage courier system handles transfer. Muster access window applies to old faction's baggage until transfer complete. New faction's baggage accessible immediately (FullAccess during grace period). |

### Quartermaster Integration Edge Cases

| Scenario | Handling |
|----------|----------|
| **"Visit QM After" from intro** | Sets `_visitQMAfter = true` flag. After muster completes, call `OpenQuartermasterConversation()` automatically. |
| **"Visit QM" from complete stage** | Immediately opens QM conversation. Player can browse, purchase, then returns to map (not back to muster menu). |
| **Stock refresh during muster** | `RollStockAvailability()` called in `CompleteMusterSequence()` before allowing QM access. Stock reflects new supply level. |
| **Supply <15% at muster** | QM service blocked entirely (critical supply gate). "Visit QM" option disabled. Tooltip: "Quartermaster unavailable - supplies critically low." |
| **Newly unlocked items (tier-up)** | `UpdateNewlyUnlockedItems()` called after stock refresh. Items marked [NEW] in QM conversation. |
| **Multiple tier-up (T5→T7)** | All skipped tiers' items marked as newly unlocked. Large batch of [NEW] items available. |
| **QM hero dies/unavailable** | Fallback: Show error message. Don't open conversation. Log error. Player can dismiss muster without accessing QM. |
| **Time control during QM from muster** | `CaptureTimeStateBeforeMenuActivation()` preserves player's time mode. Restored after QM conversation ends. |
| **Player promoted during muster period** | Stock refresh includes new tier's items. Newly available items marked [NEW]. Previous tier items remain available. |
| **Out-of-stock items at muster** | Stock roll can mark items out-of-stock based on supply level. Remain unavailable until next muster (12 days). |
| **Stock quantities depleted** | Inventory refresh at muster restores quantities. High-tier items get limited quantities (2-5). Common items get more (5-10). |
| **QM conversation mid-muster** | Player can access QM via Camp Hub menu while muster menu is open. Stock not refreshed yet (happens at completion). |

### Retinue Edge Cases (T7+ Commanders)

| Scenario | Handling |
|----------|----------|
| **Just got retinue (T7 promotion)** | Show retinue status in summary. "Retinue: 20/20 soldiers assigned." |
| **Retinue casualties this period** | Show losses: "Retinue Losses: 3 soldiers killed, 1 wounded." |
| **Retinue is full (20/20)** | Show "Retinue at capacity." Disable recruit option. |
| **Retinue has openings** | Show "Retinue: 17/20 - 3 slots available." Enable recruit option. |
| **Retinue morale low** | Show retinue morale in summary if < 50. "Retinue Morale: Low (42)". |
| **Retinue wiped out (0 soldiers)** | Show "Your retinue has been lost. All soldiers have fallen." Special tone. |
| **RetinueManager unavailable** | Skip retinue stage entirely. Log warning. |
| **No casualties, no changes** | Show "Your retinue stands ready. No losses this period." |

### Save/Load Edge Cases

| Scenario | Handling |
|----------|----------|
| **Game loaded mid-muster** | Restore `MusterSessionState` from save. Resume at saved stage. |
| **Save corrupted / state missing** | Abort muster. Reset `_payMusterPending = false`. Trigger fresh muster next cycle. |
| **Old save without muster tracking** | Initialize all tracking to defaults. First muster shows "Since Enlistment". |
| **Version upgrade changes muster format** | Migration logic in `SyncData()`. Convert old format to new. |

### Error Handling & Fallbacks

| Error | Fallback |
|-------|----------|
| **Null EnlistmentBehavior** | Log error, skip muster entirely. Reset pending flag. |
| **Null CompanySupplyManager** | Show supply as "Unknown". Log warning. |
| **Null EnlistedNewsBehavior** | Show "No events recorded." Proceed with muster. |
| **Menu registration fails** | Fall back to old inquiry popup (`ShowPayMusterInquiryFallback()`). |
| **Stage transition fails** | Log error, jump to Muster Complete stage. Ensure player isn't stuck. |
| **Effect application fails** | Log error, continue muster. Show warning in summary: "Some effects may not have applied." |
| **QM conversation fails** | Return to map with message: "Quartermaster unavailable." |
| **Service records fails** | Return to map with message: "Records unavailable." |

**Critical Rule:** Player must NEVER get stuck in muster menu. Any unhandled exception should:
1. Log full error details to `ModLogger.Error()`
2. Force close muster menu
3. Reset `_payMusterPending = false`
4. Show player an information message: "Muster interrupted. Will resume next cycle."

---

## Acceptance Criteria

### Core Flow

- [ ] Muster intro menu displays on day 12 instead of inquiry popup
- [ ] Strategic context flavor text varies by campaign situation
- [ ] Orders summary shows completed/failed orders this period
- [ ] Health status displayed if player is wounded
- [ ] Fatigue reset to maximum at muster start
- [ ] Service record shows current rank, XP, and progress percentage
- [ ] Period summary lists events from last 12 days with XP gains
- [ ] Pay line menu shows all payment options with conditional visibility
- [ ] Baggage check fires 30% of the time with rep-based outcomes
- [ ] Equipment inspection fires if 12-day cooldown ready
- [ ] Green recruit event fires if 10-day cooldown ready and T3+
- [ ] Promotion recap shows if tier increased during period (acknowledges past promotion)
- [ ] Retinue muster shows for T7+ commanders with strength/casualties
- [ ] Final summary shows all outcomes and period statistics
- [ ] Request Leave option available in final summary
- [ ] Report saved to news feed after muster complete

### Progression Display

- [ ] Current rank and tier shown at intro
- [ ] XP earned this period displayed with percentage to next rank
- [ ] XP sources breakdown shown in final summary
- [ ] Estimated days to promotion calculated and shown
- [ ] Promotion recap shows wage increase, unlocked abilities, and promotion date
- [ ] XP reset and new requirements shown after promotion

### Event Integration

- [ ] Baggage check integrated as menu stage (not popup)
- [ ] Equipment inspection integrated as menu stage (not random camp event)
- [ ] Green recruit integrated as menu stage (not random camp event)
- [ ] All event outcomes tracked in muster state
- [ ] Event effects applied correctly (gold, rep, XP, scrutiny)
- [ ] Contraband confiscation/bribe handled correctly

### System Integrations

- [ ] Orders summary queries `OrderManager.GetOrderOutcomesSince()`
- [ ] Fatigue reset calls `EnlistmentBehavior.ResetFatigue()` at muster start
- [ ] Health status queries `Hero.MainHero.HitPoints / MaxHitPoints`
- [ ] Strategic context queries `ArmyContextAnalyzer.GetCurrentContext()`
- [ ] Leave request calls `TemporaryLeaveManager.RequestLeave()`
- [ ] Retinue data queries `RetinueManager` for strength/casualties/morale
- [ ] **CRITICAL:** Muster sets `PayMusterPending` flag checked by `BaggageTrainManager.GetCurrentAccess()`
- [ ] **CRITICAL:** Muster completion sets `LastMusterCompletionTime` for 6h access window tracking
- [ ] Baggage access state queries `BaggageTrainManager.GetCurrentAccess()` (returns FullAccess during muster)
- [ ] Supply crisis warning shown when `CompanySupplyManager.TotalSupply < 20` (baggage locked even at muster)
- [ ] Baggage events suppressed during muster window (check `_payMusterPending` in event trigger logic)
- [ ] **CRITICAL:** Quartermaster stock refreshed via `QuartermasterManager.RollStockAvailability()` at muster completion
- [ ] Newly unlocked items marked via `QuartermasterManager.UpdateNewlyUnlockedItems()` after tier-up
- [ ] QM conversation opened via `CampaignMapConversation.OpenConversation()` with QM hero from `GetOrCreateQuartermaster()`
- [ ] Supply <15% blocks QM access entirely (critical supply gate enforced)
- [ ] Time control state captured via `CaptureTimeStateBeforeMenuActivation()` when accessing QM from muster

### Retinue Muster (T7+)

- [ ] Retinue muster stage appears only for T7+ with active retinue
- [ ] Shows current strength vs capacity
- [ ] Shows casualties this period with names
- [ ] Shows morale and equipment status
- [ ] "Recruit After Muster" option opens recruitment screen
- [ ] Fallen retinue members added to personal feed

### UI Polish

- [ ] All menus use consistent color scheme (`Header`, `Label`, `Success`, `Warning`, `Alert`, `Default` styles)
- [ ] All options have appropriate icons via `GameMenuOption.LeaveType`
- [ ] **CRITICAL:** All options have tooltips (action + side effects + restrictions) - no null tooltips allowed
- [ ] Tooltips follow format: "Action. Outcomes. Requirements." (factual, concise, brief)
- [ ] Menu transitions smooth (no flashing or delays)
- [ ] Text formatting readable and professional
- [ ] Long text sections properly line-wrapped
- [ ] All text uses `TextObject` with string IDs from `enlisted_strings.xml`

### Post-Muster Options

- [ ] "Visit Quartermaster" opens QM conversation after muster via `CampaignMapConversation.OpenConversation()`
- [ ] "Review Service Record" opens service records after muster
- [ ] "Dismiss" returns to map and completes muster cycle
- [ ] Quartermaster stock refreshed before QM access via `RollStockAvailability()`
- [ ] Newly unlocked items marked via `UpdateNewlyUnlockedItems()`
- [ ] Time control state preserved throughout menu sequence via `QuartermasterManager.CaptureTimeStateBeforeMenuActivation()`

### Edge Case Handling

- [ ] Combat/prisoner state defers muster appropriately
- [ ] Empty inventory skips baggage check
- [ ] Multiple contraband items handled (highest value first)
- [ ] Skill threshold comparisons use `>=` consistently
- [ ] Multi-tier promotions show single recap with all tiers gained
- [ ] T7 promotion shows retinue grant message
- [ ] T9 promotion shows "max rank reached" message
- [ ] Escalation thresholds queue events for after muster
- [ ] Wounded player skips inspection without penalty
- [ ] No-gold lord shows only IOU option
- [ ] QM-purchased items exempt from contraband
- [ ] Save/load mid-muster restores state correctly
- [ ] All errors have fallback paths (never stuck in menu)
- [ ] No orders this period shows "No orders issued" message
- [ ] Fatigue already at max shows "Fatigue: Already rested"
- [ ] Strategic context fallback if analyzer unavailable
- [ ] Retinue at zero shows "Retinue lost - all soldiers fallen"
- [ ] Leave request blocked during active siege/combat
- [ ] Retinue recruitment disabled if already at capacity
- [ ] Baggage access displays "Available (muster window)" in intro stage
- [ ] Supply <20% shows "CRITICAL SUPPLY - Baggage locked" warning
- [ ] Baggage check stage clarifies it's separate from access system
- [ ] Muster overrides baggage delay temporarily (6h window)
- [ ] Supply <15% disables "Visit Quartermaster" option with critical supply tooltip
- [ ] QM stock refresh happens at muster completion, not during menu flow
- [ ] Newly unlocked items marked [NEW] after promotion during muster period
- [ ] QM conversation uses actual hero from `GetOrCreateQuartermaster()`, not synthetic NPC

---

## Future Enhancements

### Additional Muster Stages

- **Mail Call** - Letters from home, personal correspondence, orders from high command
- **Promotion Review** - Captain's formal assessment before officer promotion (T6→T7 transition)
- **Medical Report** - Surgeon's detailed update on wounded soldiers, disease in camp
- **Disciplinary Actions** - Formal punishments if scrutiny/discipline thresholds exceeded

### Enhanced Displays

- **Skill Unlocks** - Announce when skills reach major thresholds (50, 100, 150)
- **Trait Progress** - Display role-specific trait advancement (Scout, Medic, NCO, etc.)
- **Battle Performance** - Personal combat statistics (kills, assists, survival rate)
- **Commendations** - Formal recognition for exceptional service this period

### Dynamic Events

- **Desertion Hearing** - If pay tension very high, NPC desertions reported at muster
- **Supply Crisis** - If supplies <30%, emergency rationing announced with options
- **Recruitment Drive** - If unit under-strength, new recruitment opportunities
- **Equipment Upgrade** - If tier just increased, preview new equipment available from QM
- **Companion Events** - If companions in party, brief status update at muster

### Retinue Enhancements (T7+)

- **Individual Retinue Member Status** - Click to see details on specific soldiers
- **Retinue Morale Events** - Low morale triggers events during retinue muster
- **Veteran Recognition** - Announce retinue members reaching service milestones
- **Retinue Equipment Inspection** - Option to review/upgrade retinue gear

---

**Last Updated:** 2025-12-24 (Baggage Train & Quartermaster Integration)

