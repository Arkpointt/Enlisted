# News & Reporting System

**Summary:** The news and reporting system tracks game events and generates narrative feedback for the player. It manages two feed types (kingdom-wide and personal), a daily brief with company/player/kingdom context, and a company needs status report. Event and order outcomes are displayed in Recent Activities using a queue system instead of popups to reduce UI interruption. All text uses immersive Bannerlord military flavor instead of raw statistics.

**Status:** ✅ Current  
**Last Updated:** 2025-12-30 (Added order event integration reference)  
**Related Docs:** [Core Gameplay](../Core/core-gameplay.md), [UI Systems Master](ui-systems-master.md), [Color Scheme](color-scheme.md), [Order Events Master](../../AFEATURE/order-events-master.md)

---

## Index

- [Overview](#overview)
- [System Architecture](#system-architecture)
- [Event & Order Outcome Queue System](#event--order-outcome-queue-system)
- [Kingdom Feed](#kingdom-feed)
- [Personal Feed](#personal-feed)
- [Daily Brief](#daily-brief)
- [Company Status Report](#company-status-report)
- [Retinue Integration](#retinue-integration-t7-commanders)
- [Promotion Integration](#promotion-integration)
- [Pay Muster Integration](#pay-muster-integration)
- [Tracking Records](#tracking-records)
- [Data Structures](#data-structures)
- [Localization](#localization)
- [Implementation Files](#implementation-files)

---

## Overview

The news system operates as a read-only observer of campaign events. It listens to Bannerlord campaign events (battles, sieges, declarations, etc.) and generates localized narrative entries.

**Two Feed Types:**
1. **Kingdom Feed** - Kingdom-wide events (wars, battles, settlements, captures)
2. **Personal Feed** - Your enlisted lord's events and your direct participation

**Two Report Types:**
1. **Daily Brief** - Once-per-day narrative paragraph combining company situation, player status, and kingdom news
2. **Company Status Report** - Five company needs with context-aware descriptions

**Key Behavior:**
- All feeds use `DispatchItem` struct (primitives only for save compatibility)
- Daily Brief caches at daily tick (stable for 24 hours to prevent jitter)
- Company Status generates on-demand when menu opens
- Records track outcomes for display in reports (orders, events, reputation changes)

---

## System Architecture

### Core Components

```
EnlistedNewsBehavior (singleton CampaignBehaviorBase)
├── Feed Management
│   ├── _kingdomFeed (List<DispatchItem>)          - Max 60 items
│   ├── _personalFeed (List<DispatchItem>)         - Max 20 items
│   └── Event listeners for battles/sieges/wars
│
├── Daily Brief (cached once per day)
│   ├── _dailyBriefCompany (string)                - Company situation
│   ├── _dailyBriefUnit (string)                   - Player status
│   ├── _dailyBriefKingdom (string)                - Kingdom context
│   └── BuildDailyBriefSection()                   - Assembles flowing paragraph
│
├── Company Status (generated on-demand)
│   └── BuildCompanyStatusReport()                 - Five needs with context
│
├── Tracking Records (for detailed reports)
│   ├── _orderOutcomes (List<OrderOutcomeRecord>)
│   ├── _eventOutcomes (List<EventOutcomeRecord>)
│   ├── _reputationChanges (List<ReputationChangeRecord>)
│   ├── _companyNeedChanges (List<CompanyNeedChangeRecord>)
│   ├── _pendingEvents (List<PendingEventRecord>)
│   └── _musterOutcomes (List<MusterOutcomeRecord>)
│
└── State Persistence
    ├── CampNewsState                               - 7-day report archive
    └── SyncData()                                  - Save/load integration
```

### Registered Campaign Events

The behavior subscribes to these Bannerlord campaign events:

| Event | Purpose |
|-------|---------|
| `DailyTickEvent` | Generate daily brief, detect army formations |
| `MapEventStarted` | Cache battle strengths for pyrrhic detection |
| `MapEventEnded` | Generate battle outcome dispatch |
| `OnSiegeEventStartedEvent` | Generate siege started dispatch |
| `HeroPrisonerTaken` | Generate capture dispatch |
| `HeroPrisonerReleased` | Generate release dispatch |
| `HeroKilledEvent` | Generate death/execution dispatch |
| `OnSettlementOwnerChangedEvent` | Generate settlement captured dispatch |
| `ArmyDispersed` | Generate army dispersal dispatch (personal feed) |
| `VillageBeingRaided` | Generate village raid dispatch |
| `WarDeclared` | Generate war declaration dispatch |
| `MakePeace` | Generate peace treaty dispatch |

---

## Event & Order Outcome Queue System

**Purpose:** Display event and order outcome narratives in Recent Activities instead of popup interruptions, using a queue to show one outcome at a time for better UX.

### Design Goals

1. **Reduce UI Interruption:** Event and order outcomes no longer show as popups after making choices
2. **Maintain Immersion:** Full narrative result text preserved and displayed in Recent Activities
3. **Prevent Clutter:** Only one outcome visible at a time (events and orders share the feed)
4. **Natural Discovery:** Players check their status menu to see what happened

### What Still Uses Popups

- **Initial event choices** - Event setup and option selection still show as popups (for progression)
- **Multi-phase events** - Each phase shows a new event popup (chains work normally)
- **Reward choices** - Training focus selection and other sub-choices still use popups
- Only the **outcome narrative** after making a choice goes to Recent Activities

### Queue Behavior

**Display Rules:**
- ONE event outcome shows in Recent Activities at any given time
- Each outcome displays for exactly 1 day
- Multiple events on the same day queue automatically (FIFO)
- Daily tick removes expired outcomes and promotes next in queue

**Example Flow:**
```
Day 1, Morning:
  - Event A fires → Shows immediately in Recent Activities
  - Event B fires later → Queued (hidden)

Day 2, Morning:
  - Event A expires (shown for 1 day) → Removed from display
  - Event B promoted → Now visible in Recent Activities
  - Event C fires → Queued

Day 3, Morning:
  - Event B expires → Removed
  - Event C promoted → Now visible
```

**Implementation:**
- `EventOutcomeRecord` tracks `IsCurrentlyShown` and `DayShown` for each outcome
- `AddEventOutcome()` checks if an outcome is already showing; if yes, queues new one
- `ProcessEventOutcomeQueue()` runs on daily tick to expire and promote outcomes
- Personal feed entries are added/removed dynamically as queue processes

**Display Format:**
```
Recent Activities:
• Crossing Gone Bad: You wait on the bank with the others, 
  watching the pioneers work. They know their business, but 
  the current is strong and the footing treacherous. It will 
  take time.

• ✗ Inspect the Picket Line: You missed a gap in the 
  pickets. The sergeant finds it on his own rounds.
```

**Technical Note:** The outcome popup system has been removed (dead code: `ShowResultText()` and `ShowOrderResult()` deleted). Event outcomes are recorded with their full `ResultNarrative` text, and order outcomes use their detailed summary text. Both are added to the personal feed only when actively shown. This reduces cognitive load and allows players to review outcomes at their own pace.

### Order Event Integration

Order events (fired during duty via `OrderProgressionBehavior`) use this same queue system:

```csharp
// In OrderProgressionBehavior, after player makes event choice:
EnlistedNewsBehavior.Instance.AddEventOutcome(
    eventTitle: orderEvent.Title,
    resultNarrative: selectedOption.ResultText,
    severity: orderEvent.Severity,
    dayNumber: (int)CampaignTime.Now.ToDays
);
```

**See Also:** [Order Events Master](../../AFEATURE/order-events-master.md) for order event definitions and result text variants.

---

## Kingdom Feed

Kingdom-wide events visible to all enlisted lords in your kingdom.

**Storage:** `List<DispatchItem>` limited to 60 items (oldest pruned)

**Dispatch Categories:**
- `"war"` - War declarations and peace treaties
- `"battle"` - Victory/defeat/pyrrhic/inconclusive battle outcomes
- `"siege"` - Siege starts and conclusions
- `"settlement"` - Settlement ownership changes
- `"prisoner"` - Lord captures, releases, executions
- `"army"` - Army formations and dispersals
- `"village"` - Village raids

**Key Implementation Detail - Pyrrhic Victory Detection:**

The system tracks battle initial strengths in `_battleSnapshots` dictionary:
1. `OnMapEventStarted`: Cache attacker/defender troop counts
2. `OnMapEventEnded`: Compare final vs initial strength
3. If winner lost > 40% of initial strength → classify as pyrrhic

**Access:** `EnlistedNewsBehavior.Instance.KingdomFeed` (read-only)

---

## Personal Feed

Events specific to your enlisted lord and your direct participation.

**Storage:** `List<DispatchItem>` limited to 20 items (oldest pruned)

**Personal Events:**
- Lord's army formation/dispersal
- Battles where you participated
- Orders you completed
- Events you chose

**Access:** `EnlistedNewsBehavior.Instance.PersonalFeed` (read-only)

---

## Daily Brief

A once-per-day narrative paragraph that combines multiple systems. Generated at daily tick and cached for 24 hours.

**Color Coding:** Uses semantic color spans from the enlisted color scheme to emphasize important elements:
- **Alert (Red):** Casualties, critical supply warnings, raids
- **Warning (Gold):** Low supplies, delays, wounded counts
- **Success (Green):** Arrivals, skill progress, positive outcomes

**Display Location:** Main enlisted menu

**Current Structure (Pre-Orchestrator):**
- Single "COMPANY REPORT" section with combined content

**Future Structure (With Orchestrator - Phase 5):**
- **"COMPANY REPORT"** section: Orchestrator rhythm flavor (see below)
- **"DAILY NEWS"** section: Kingdom context, casualties, events (existing Daily Brief content)

---

### Company Report Section (Future - Orchestrator Phase 5)

**Purpose:** Player-facing transparency showing what the orchestrator is thinking.

**Generated by:** `BuildCompanyReportSection()` (new method)

**Content:**
- Current military life rhythm icon and name (Garrison, Campaign, Siege, etc.)
- Activity level (Quiet, Active, Intense, Crisis)
- Observable world facts explaining the situation
- Implicit signal about expected event frequency

**NOT included:**
- ❌ Soldier counts (Daily News already reports casualties)
- ❌ Supply/Morale/Rest bars (those are in Reports submenu)
- ❌ Redundant information

**Example Output:**
```
⚙️ CAMP STATUS: Garrison - Quiet

Your lord holds at Pravend with no threats on the horizon. 
The camp has settled into routine. Little disturbs the 
daily rhythm.
```

**Technical Implementation:**
```csharp
public string BuildCompanyReportSection()
{
    if (ContentOrchestrator.Instance == null) 
        return string.Empty;
    
    var rhythmIcon = GetRhythmIcon();
    var rhythmName = GetRhythmName();
    var activityLevel = GetActivityLevelName();
    var flavorText = ContentOrchestrator.Instance.GetOrchestratorRhythmFlavor();
    
    return $"<span style=\"Header\">{rhythmIcon} CAMP STATUS: {rhythmName} - {activityLevel}</span>\n\n{flavorText}";
}
```

**Alignment with Daily News:**
Both sections pull from the same world state sources, ensuring no contradictions:
- Same party state checks (battle, siege, settlement, army)
- Same kingdom state checks (wars, peace, threats)
- Orchestrator context explains "why," Daily News provides "what happened"

See **[Content Orchestrator Plan](../../Features/Content/content-orchestrator-plan.md#phase-5-refinement--ui-integration-week-5)** for full implementation details.

---

### Daily News Section (Current: "Daily Brief")

**Current name:** "COMPANY REPORT" section (will be renamed to "DAILY NEWS" in Phase 5)

**Generated by:** `BuildDailyNewsSection()` (refactored from existing `BuildDailyBriefSection()`)

**Refactoring Note:** The existing `BuildDailyBriefSection()` will be split into two methods:
1. `BuildCompanyReportSection()` - Orchestrator rhythm status (new, see above)
2. `BuildDailyNewsSection()` - Kingdom context, casualties, events (refactored from existing logic)

### Generation Flow

```
OnDailyTick() called
    ├→ EnsureDailyBriefGenerated()
    │   ├→ Check if already generated for current day
    │   ├→ If not, build each section:
    │   │   ├→ BuildDailyCompanyLine()      → _dailyBriefCompany
    │   │   ├→ BuildDailyUnitLine()         → _dailyBriefUnit
    │   │   └→ BuildDailyKingdomLine()      → _dailyBriefKingdom
    │   └→ Store in _lastDailyBriefDayNumber
    │
    └→ BuildDailyBriefSection() (when UI requests)
        ├→ Start with _dailyBriefCompany
        ├→ Add BuildSupplyContextLine() if supplies < 70% (color-coded by severity)
        ├→ Add BuildCasualtyReportLine() if losses/wounded (color-coded)
        ├→ Add BuildRecentEventLine() if events in last 24h
        ├→ Add BuildPendingEventsLine() if chain events pending
        ├→ Add BuildFlagContextLine() (random reputation flavor)
        ├→ Add BuildSkillProgressLine() if skill near levelup (color-coded)
        ├→ Add BuildBaggageStatusLine() if baggage events (color-coded by status)
        ├→ Add _dailyBriefUnit
        └→ Add _dailyBriefKingdom
            └→ Return as single flowing paragraph with embedded color spans
```

### Section Builders

#### BuildDailyCompanyLine()
**Checks:** `party.Party.MapEvent`, `party.Party.SiegeEvent`, `party.CurrentSettlement`, `party.Army`, `party.TargetSettlement`

**Outputs:**
- Battle: Enemy leader, atmospheric combat description
- Siege: Attacker vs defender, siege engines, enemy camps
- Settlement: Type-specific (town/castle/village) with sensory details
- Army: Army size, marshal, your position in column
- March: Target destination + `GetTerrainFlavor()` (terrain-aware, time-of-day aware)

**Context factors:** Party size descriptor (small/sizeable/large/formidable), terrain type, hour of day

#### BuildDailyUnitLine()
Uses the localized "brief" string system with tier/time-of-day variants.

**Priority order:**
1. Recent battle (within 24h): `brief_{battletype}_{won|lost}_{tierkey}_N`
2. Injuries: `brief_injury_{tierkey}_N`
3. Illness: `brief_illness_{tierkey}_N`
4. Low fatigue: `brief_exhausted_{tierkey}_N` or `brief_tired_{tierkey}_N`
5. Good condition: `brief_good_{tierkey}_{timekey}_N`

**Tier keys:** `recruit` (T1-2), `soldier` (T3-4), `nco` (T5-6), `veteran` (T7+)  
**Time keys:** `night` (<6 or >=20), `morning` (6-10), `evening` (17-20), `day` (default)  
**N:** Random 1-3 for variety

Implemented via `PickRandomLocalizedString(prefix, count, fallbackId)` with `FlavorRng`.

#### BuildDailyKingdomLine()
**Priority:** If kingdom feed has recent headline, use it. Otherwise generate strategic summary.

**Checks:** Active wars (via `FactionManager.IsAtWarAgainstFaction`), active sieges count

**Outputs:**
- Multi-front wars with sieges
- Multi-front wars without sieges
- Single war with siege
- Single war without siege
- Peace time

#### BuildSupplyContextLine()
Only shown when `CompanyNeeds.Supplies < 70`.

**Thresholds:**
- `< 30`: Critical (stores locked) - **Color: Alert (Red)**
- `< 50`: Low (rations cut) - **Color: Warning (Gold)**
- `< 70`: Moderate (dwindling) - **Color: Warning (Gold)**

#### BuildCasualtyReportLine()
Uses `_lostSinceLastMuster` (reset at muster) and current wounded count.

**Variations:**
- Heavy losses (10+): Somber tone - **Lost count in Alert (Red)**
- Moderate losses (5-9): Names spoken at fire - **Lost count in Alert (Red)**
- Few losses (2-4): Cost of march - **Lost count in Alert (Red)**
- Single loss: Lads are quiet - **Lost count in Alert (Red)**
- Wounded only: Scaled by count - **Wounded count in Warning (Gold)**

#### BuildRecentEventLine()
Checks `_eventOutcomes` for entries with `dayNumber` within 1 day of current.

**Event ID matching:**
- Dice/gambling/wager → dice game aftermath
- Training/drill/practice → sore muscles, improved skills
- Hunt → fresh game
- Lend/loan (not repay) → comrade owes debt
- Tavern/drinking → hangover
- Loot/battlefield → spoils weigh on you

#### BuildPendingEventsLine()
Checks `_pendingEvents` for scheduled chain events.

**Chain ID matching:**
- Repay/return/debt → debt reminder with days elapsed
- Gratitude/thank/favor → kindness remembered
- Revenge/grudge → enemy watching

Returns only the first match (avoids spam).

#### BuildFlagContextLine()
Reads `EscalationManager.Instance.State` flags and picks one random entry daily.

**Flags checked:**
- `has_helped_comrade` / `helped_comrade`
- `dice_winner` / `gambling_winner`
- `shared_winnings` / `generous`
- `officer_attention` / `noticed_by_officers`
- `training_focused` / `dedicated_training`
- `good_hunter` / `skilled_hunter`
- `drinks_with_soldiers` / `tavern_regular`

Uses `MBRandom.RandomInt()` for selection.

#### BuildSkillProgressLine()
Cached for 6 hours. Checks main combat skills (`OneHanded`, `TwoHanded`, `Polearm`, `Bow`, `Crossbow`) for level-up proximity.

Uses `CharacterDevelopmentModel.GetXpRequiredForLevel()` to calculate percentage to next level. Shows hint if any skill >= 80% to levelup.

**Color:** Skill name wrapped in **Success (Green)** to emphasize progress.

#### BuildBaggageStatusLine()
Reports on the baggage train status based on `BaggageTrainManager` state.

**Status Types:**
- **Raids/Locked:** "Baggage was raided" - **Color: Alert (Red)**
- **Delays:** "Baggage delayed X days" - **Color: Warning (Gold), days count also in Warning**
- **Arrivals:** "Baggage caught up" - **Color: Success (Green)**

### Player Battle Tracking

The system tracks player battle participation for aftermath flavor:
- `_lastPlayerBattleTime`: When last battle occurred
- `_lastPlayerBattleType`: "bandit", "army", "siege"
- `_lastPlayerBattleWon`: Victory status

Updated in `CheckPlayerBattleParticipation()` called from `OnMapEventEnded()`.

---

## Company Status Report

Generated on-demand when Reports menu opens. Does NOT cache.

**Display Location:** Reports menu only (accessed via "Service Records" → "Company Reports")

**NOT shown on main menu** - The main enlisted menu only shows the Daily Brief and Recent Actions.

### BuildCompanyStatusReport()

Entry point that calls five specialized builders:

```csharp
BuildReadinessLine(value, isMarching, isInCombat, lowMorale)
BuildMoraleLine(value, enlistment, isInCombat, isInSiege)
BuildSuppliesLine(value, isMarching, isInSiege)
BuildEquipmentLine(value, isInCombat, isMarching, party)
BuildRestLine(value, isMarching, isInSettlement, isInArmy)
```

### Context Detection

**Checks performed:**
- `party.IsMoving && party.CurrentSettlement == null` → isMarching
- `party.MapEvent != null` → isInCombat
- `party.Party.SiegeEvent != null` → isInSiege
- `party.CurrentSettlement != null` → isInSettlement
- `party.Army != null` → isInArmy
- `Campaign.Current.MapSceneWrapper.GetFaceTerrainType()` → terrain
- `CampaignTime.Now.GetHourOfDay` → time of day
- `enlistment.PayTension` → pay status

### Status Levels

Each need uses 5 thresholds:
- **Excellent**: 80-100
- **Good**: 60-79
- **Fair**: 40-59
- **Poor**: 20-39
- **Critical**: 0-19

### Context Appending

Each builder returns:
- Base status description (selected by value threshold)
- Optional context sentence (selected by detected conditions)
- Concatenated: `"{status} {context}"`

**Example:** `"READINESS: The company is prepared for action, though some drills have been skipped. The march wears on the men."`

**Note:** Context sentence only appended when relevant condition detected.

---

## Retinue Integration (T7+ Commanders)

The news system tracks retinue activities for commanders (T7+).

### Personal Feed Entries

**Retinue Events:**
- All 10 retinue narrative events logged via `AddEventOutcome()`
- Format: "[Event Title]: [Option Chosen] - [Outcome]"
- Priority: 2-3 (normal)

**Veteran Activities:**
- Emergence: "[Veteran Name] has distinguished themselves in battle." (Priority 2)
- Death: "[Veteran Name], who served N battles, has fallen." (Priority 4, high)

**Casualties:**
- "Your retinue suffered losses: N killed, M wounded."
- Reported separately from lord's casualties
- Added via `AddRetinueCasualtyReport()` after battles

### Daily Brief Integration

**Company Context Section:**

`BuildRetinueContextLine()` adds retinue status when notable:
- **Under-strength** (<70% capacity): "Your retinue is at X of Y. Z replacements needed."
- **Low loyalty** (<30): "Your men are restless. Morale among your retinue is worryingly low."
- **High loyalty** (>80): "Your soldiers are devoted. They would follow you into any fight."

Called from `BuildDailyBriefSection()` after skill progress line.

**Casualty Report Section:**

Retinue casualties included in `BuildCasualtyReportLine()`:
- Distinguished from lord's troop casualties
- Shows retinue losses separately: "Your retinue lost N soldiers..."
- Uses `_retinueLostSinceLastMuster` counter (separate from main force)

### Reputation Changes

Retinue loyalty changes recorded as reputation changes:

```csharp
ReputationChangeRecord {
    Target: "Retinue",
    Delta: loyalty_change,
    NewValue: new_loyalty_value,
    Message: source_description,
    DayNumber: current_day
}
```

**Handled in `RecordReputationChange()`:**
- Target = "Retinue" alongside "Lord", "Officer", "Soldier"
- Appears in service record ledger
- Tracked for long-term analysis

### Veteran News

**Emergence:**
```csharp
AddVeteranEmergence(NamedVeteran veteran)
{
    var headline = "{NAME}, a soldier in your retinue, has distinguished themselves.";
    AddPersonalNews("retinue", headline, priority: 2);
}
```

**Death:**
```csharp
AddVeteranDeath(NamedVeteran veteran)
{
    var headline = "{NAME}, who served {BATTLES} battles, has fallen.";
    AddPersonalNews("retinue", headline, priority: 4);
}
```

### Implementation

**Methods Added to EnlistedNewsBehavior:**
- `BuildRetinueContextLine()` - Returns context string for Daily Brief
- `AddRetinueCasualtyReport(int killed, int wounded)` - Logs retinue casualties
- `AddVeteranEmergence(NamedVeteran veteran)` - Logs veteran emergence
- `AddVeteranDeath(NamedVeteran veteran)` - Logs veteran death

**Localization Strings:**
```xml
<string id="brief_retinue_understrength" text="Your retinue is at {CURRENT} of {CAPACITY}. {MISSING} replacements needed." />
<string id="brief_retinue_loyalty_low" text="Your men are restless. Morale among your retinue is worryingly low." />
<string id="brief_retinue_loyalty_high" text="Your soldiers are devoted. They would follow you into any fight." />
<string id="news_retinue_casualties" text="Your retinue suffered losses: {KILLED} killed, {WOUNDED} wounded." />
<string id="news_veteran_emerge" text="{NAME}, a soldier in your retinue, has distinguished themselves in battle." />
<string id="news_veteran_death" text="{NAME}, who served {BATTLES} battles under your command, has fallen." />
```

---

## Promotion Integration

Promotions are recorded to the Personal Feed when players advance in rank.

### Personal Feed Entries

Called from `PromotionBehavior.TriggerPromotionNotification()`:

```csharp
EnlistedNewsBehavior.Instance?.AddPromotionNews(newTier, rankName, retinueSoldiers);
```

**Tier-Specific Headlines:**
- T2-T6: Immersive rank recognition text
- T7-T9: Commander promotion with retinue size mentioned

**Localization Strings:**
```xml
<string id="News_Promotion_T2" text="The sergeant's stripe is sewn to your sleeve. You are now {RANK}." />
<string id="News_Promotion_T7" text="Twenty soldiers salute their new commander. You are {RANK}, with a retinue of your own." />
```

---

## Pay Muster Integration

Pay muster outcomes are recorded to the Personal Feed for payment history tracking.

### Personal Feed Entries

Called from `EnlistmentBehavior` payment processing:

```csharp
// ProcessFullPayment, ProcessPartialPayment, ProcessPayDelay
EnlistedNewsBehavior.Instance?.AddPayMusterNews(outcome, amountPaid, amountOwed);
```

**Outcome Types:**
- `full` / `backpay`: Full payment received
- `partial`: Partial payment with backpay still owed
- `delayed`: No payment, backpay accumulating
- `promissory`: IOU accepted
- `corruption` / `side_deal`: Alternative payment methods

**Localization Strings:**
```xml
<string id="News_PayMuster_Full" text="The paymaster counts out your coin. {AMOUNT} denars received in full." />
<string id="News_PayMuster_Delayed" text="The paymaster shakes his head. No coin today. {OWED} denars now owed." />
```

---

## Tracking Records

The system maintains several record lists for detailed report displays. These are NOT used in the Daily Brief but available for service record views.

### OrderOutcomeRecord

Tracks order completions for service record.

**Fields:**
- `OrderTitle`: Order name
- `Success`: Completed successfully?
- `BriefSummary`: One-line outcome
- `DetailedSummary`: Full outcome text
- `Issuer`: Who gave the order
- `DayNumber`: When completed

**Added via:** `RecordOrderOutcome()` (called by OrderManager)

**Retrieved via:** `GetRecentOrderOutcomes(maxDaysOld)`

### EventOutcomeRecord

Tracks event choices for personal feed using a queue system to display one outcome at a time.

**Note:** Orders also feed into the Recent Activities display via `AddOrderOutcome()`, but use a simpler system without queuing.

**Fields:**
- `EventId`: Event identifier
- `EventTitle`: Localized title
- `OptionChosen`: Which option selected
- `OutcomeSummary`: Mechanical effects summary (legacy)
- `ResultNarrative`: Full RP narrative outcome text from option's `resultText` field
- `DayNumber`: When event occurred
- `EffectsApplied`: Dictionary<string, int> of effects
- `Severity`: Color coding level (0=Normal, 1=Positive, 2=Attention, 3=Urgent, 4=Critical)
- `IsCurrentlyShown`: True if outcome is displayed in Recent Activities, false if queued
- `DayShown`: Day this outcome was first displayed (-1 if still in queue)

**Queue System Behavior:**
- Only ONE event outcome is shown in Recent Activities at any given time
- When multiple events fire, subsequent outcomes queue automatically
- Each outcome displays for exactly 1 day
- Daily tick automatically removes expired outcomes and promotes next queued outcome
- Queue processes first-in-first-out (FIFO)

**Display:**
- Outcome narrative replaces popup interruption to reduce UI overhead
- Players check Recent Activities section in status menu to see what happened
- Format: `"{EventTitle}: {ResultNarrative}"`
- Example: *"Crossing Gone Bad: You wait on the bank with the others, watching the pioneers work. They know their business, but the current is strong and the footing treacherous. It will take time."*

**Added via:** `AddEventOutcome()` (called by EventDeliveryManager)

**Retrieved via:** `GetRecentEventOutcomes(maxDaysOld)`

### ReputationChangeRecord

Tracks reputation changes for reports.

**Fields:**
- `Target`: "Lord", "Officer", or "Soldier"
- `Delta`: Change amount
- `NewValue`: Resulting reputation
- `Message`: Explanation text
- `DayNumber`: When occurred

**Added via:** `RecordReputationChange()` (called by EscalationManager)

**Retrieved via:** `GetRecentReputationChanges(maxDaysOld)`

### CompanyNeedChangeRecord

Tracks company need changes for reports.

**Fields:**
- `Need`: Need name (string)
- `Delta`: Change amount
- `OldValue`: Previous value
- `NewValue`: New value
- `Message`: Explanation
- `DayNumber`: When occurred

**Added via:** `RecordCompanyNeedChange()` (called by CompanyNeedsManager)

**Retrieved via:** `GetRecentCompanyNeedChanges(maxDaysOld)`

### PendingEventRecord

Tracks scheduled chain events for Daily Brief hints.

**Fields:**
- `SourceEventId`: Original event
- `ChainEventId`: Follow-up event identifier
- `ContextHint`: Text hint for brief
- `ScheduledDay`: When it will fire
- `CreatedDay`: When scheduled

**Added via:** `SchedulePendingChainEvent()` (called by EventDeliveryManager)

**Cleaned up:** Events > 7 days overdue are skipped in `BuildPendingEventsLine()`

### MusterOutcomeRecord

Tracks muster outcomes for camp news.

**Fields:**
- `DayNumber`: Muster day
- `StrategicContext`: Context tag for flavor text (e.g., "coordinated_offensive")
- `PayOutcome`: "paid", "partial", "delayed", "promissory", "corruption"
- `PayAmount`: Gold received
- `RationOutcome`: "issued", "none_low_supply", "none_critical", "officer_exempt"
- `RationItemId`: Item received
- `QmReputation`: QM rep at issue
- `SupplyLevel`: Company supply 0-100
- `LostSinceLast`: Casualties since previous muster
- `SickSinceLast`: Sick since previous muster
- `OrdersCompleted`: Count of orders completed this period
- `OrdersFailed`: Count of orders failed this period
- `BaggageOutcome`: "passed", "confiscated", "bribed", "skipped"
- `InspectionOutcome`: "perfect", "basic", "failed", "skipped"
- `RecruitOutcome`: "mentored", "ignored", "hazed", "skipped"
- `PromotionTier`: New tier if promoted (0 if no promotion)
- `RetinueStrength`: Current retinue size (T7+ only, 0 otherwise)
- `RetinueCasualties`: Retinue losses this period (T7+ only)
- `FallenRetinueNames`: List of fallen retinue member names (T7+ only)

**Added via:** `AddMusterOutcome()` (called by `MusterMenuHandler.CompleteMusterSequence()`)

**Retrieved via:** `GetLastMusterOutcome()`

---

## Data Structures

### DispatchItem (struct)

Used for both kingdom and personal feeds. Primitive-friendly for save compatibility.

**Fields:**
```csharp
int DayCreated           // Campaign day when created
string Category          // "war", "battle", "prisoner", "siege", etc.
string TemplateId        // Localization key (e.g., "News_BattleVictory")
string Headline          // Formatted headline text
string DetailText        // Optional detailed text
int Priority             // Display priority (higher = more important)
```

**Creation:** `AddKingdomNews()` and `AddPersonalNews()` helper methods

**Formatting:** `FormatDispatchItem()` applies placeholders and returns display string

### DailyReportSnapshot (class)

Factual snapshot with bands/tags. Populated by fact producers.

**Current Status:** Partially implemented. The producer pattern exists but Daily Brief primarily uses direct builders.

**Fields:**
- `DayNumber`: Day of snapshot
- Delta fields: `WoundedDelta`, `SickDelta`, `DeadDelta`, `ReplacementsDelta`
- Band enums: `ThreatBand`, `FoodBand`, `MoraleBand`, `HealthDeltaBand`
- Tag strings: `ObjectiveTag`, `BattleTag`, `AttachedArmyTag`, `DisciplineTag`, `StrategicContextTag`, `TrainingTag`

**Producers:**
- `CompanyMovementObjectiveProducer`: Populates objective/army tags
- `UnitStatusFactProducer`: Populates health deltas
- `KingdomHeadlineFactProducer`: (purpose unclear from names)

**Interface:** `IDailyReportFactProducer.Contribute(snapshot, context)`

**Note:** This appears to be infrastructure for future template-based report generation. Current Daily Brief does not use snapshots.

### CampNewsState (class)

Persisted state for report archive and ledger.

**Storage:**
- `_archive`: Ring buffer of `DailyReportRecord[7]`
- `_archiveHeadIndex`: Current write position
- `_ledger`: `CampLifeLedger` for 30-day rolling stats
- `_lastGeneratedDayNumber`: Gate to prevent duplicate generation

**Methods:**
- `AppendReport(record)`: Add to ring buffer
- `TryGetReportForDay(day)`: Retrieve specific day
- `TryGetLatestReport()`: Get most recent
- `GetBaselineRosterCounts()`: For delta calculation

**Save/Load:** Synced via `EnlistedNewsBehavior.SyncData()`

### BattleSnapshot (struct)

Temporary tracking for pyrrhic victory detection.

**Fields:**
```csharp
string MapEventId                    // Hash code as string
int AttackerInitialStrength
int DefenderInitialStrength
```

**Lifecycle:**
1. Created in `OnMapEventStarted()`
2. Stored in `_battleSnapshots` dictionary
3. Retrieved in `OnMapEventEnded()` for classification
4. Removed after battle ends

---

## Localization

All text uses `TextObject` with string IDs and placeholders.

### String ID Conventions

| Prefix | Purpose | Example |
|--------|---------|---------|
| `brief_*` | Daily Brief sections | `brief_march_army`, `brief_casualties_heavy` |
| `status_*` | Company Status Report | `status_readiness_excellent`, `status_morale_combat` |
| `News_*` | Kingdom dispatch templates | `News_BattleVictory`, `News_SiegeStarted` |

### Placeholder Usage

```csharp
var text = new TextObject("{=brief_march_army}{ARMY_DESC} marches under the banner of {LEADER}.");
text.SetTextVariable("ARMY_DESC", armySizeDesc);
text.SetTextVariable("LEADER", armyLeader);
return text.ToString();
```

### Common Placeholders

- `{LORD}`, `{LORD_NAME}` - Enlisted lord
- `{ENEMY}` - Enemy faction/leader
- `{KINGDOM}` - Player's kingdom
- `{SETTLEMENT}` - Settlement name
- `{COUNT}`, `{DEAD}`, `{WOUNDED}` - Casualty counts
- `{DAYS}` - Day counters
- `{TERRAIN}` - Terrain flavor text
- `{ARMY_DESC}`, `{SIZE}` - Army/party size descriptors

### File Location

`ModuleData/Languages/enlisted_strings.xml` contains ~120 news system strings.

The template includes fallback text for development:
```xml
<string id="brief_casualties_heavy" text="The company has paid dearly — {COUNT} souls lost since last muster." />
```

---

## Implementation Files

### Core Behavior

| File | Lines | Purpose |
|------|-------|---------|
| `src/Features/Interface/Behaviors/EnlistedNewsBehavior.cs` | ~4,280 | Main news system singleton |
| `src/Features/Interface/Behaviors/EnlistedMenuBehavior.cs` | Partial | Company Status Report builders |

### Data Models

| File | Purpose |
|------|---------|
| `src/Features/Interface/News/Models/DailyReportModels.cs` | `DailyReportSnapshot`, band enums |
| `src/Features/Interface/News/Models/CampLifeLedger.cs` | 30-day rolling stats |
| `src/Features/Interface/News/State/CampNewsState.cs` | Report archive, persistence |

### Generation (Partial Implementation)

| File | Purpose |
|------|---------|
| `src/Features/Interface/News/Generation/Producers/IDailyReportFactProducer.cs` | Producer interface |
| `src/Features/Interface/News/Generation/Producers/CompanyMovementObjectiveProducer.cs` | Objective/army tags |
| `src/Features/Interface/News/Generation/Producers/UnitStatusFactProducer.cs` | Health deltas |
| `src/Features/Interface/News/Generation/Producers/KingdomHeadlineFactProducer.cs` | Kingdom headlines |
| `src/Features/Interface/News/Generation/Producers/CampNewsContext.cs` | Context for producers |
| `src/Features/Interface/News/Generation/DailyReportGenerator.cs` | Generator infrastructure |
| `src/Features/Interface/News/Templates/NewsTemplates.cs` | Template system |

**Note:** The producer/template system appears to be infrastructure for future development. Current Daily Brief uses direct builders in `EnlistedNewsBehavior`.

### Localization

| File | Purpose |
|------|---------|
| `ModuleData/Languages/enlisted_strings.xml` | All localized strings |

### Related Systems Integration

| System | Integration Point |
|--------|-------------------|
| **Orders** | `OrderManager` calls `RecordOrderOutcome()` |
| **Events** | `EventDeliveryManager` calls `AddEventOutcome()`, `SchedulePendingChainEvent()` |
| **Escalation** | `EscalationManager` calls `RecordReputationChange()` |
| **Company Needs** | `CompanyNeedsManager` provides current values |
| **Pay System** | `EnlistmentBehavior` calls `AddPayMusterNews()` after each muster resolution |
| **Promotion** | `PromotionBehavior` calls `AddPromotionNews()` after tier advancement |
| **Retinue** | `RetinueCasualtyTracker` calls `AddRetinueCasualtyReport()`, `AddVeteranEmergence()`, `AddVeteranDeath()` |

---

**End of Document**
