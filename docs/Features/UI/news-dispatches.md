# News / Dispatches (UI Feed)

## Status: ✅ **IMPLEMENTED**
The news/dispatches system is fully functional as of the latest version.

## Overview
A lightweight **dispatch board** shown inside the **custom enlisted menus** that reports **real in-game campaign events** (wars, battles, sieges, captures, executions, raids, politics) in a diegetic "scribe/messenger" tone.

This is **not** a popup event system. It's a **read-only news feed** surfaced in two locations based on relevance.

## Purpose
- Make enlisted service feel embedded in a living kingdom: *"what's happening beyond my immediate party?"*
- Provide believable military "sitreps" without rewriting Bannerlord AI/economy.
- Keep costs low and stability high by using **CampaignEvents + event-driven generation**, not per-tick world scanning.

## Design goals
- **Believable, not random**: headlines should follow campaign events (battle ended, lord captured, fief fell).
- **Low overhead**: event-driven ingestion + **2-day bulletin snapshot**.
- **Rate-limited**: avoid spam; publish top items, roll up the rest.
- **Localization-friendly**: all player-facing text uses `{=string_id}` + placeholders.
- **Mod-safe**: no invasive patches; no menu activation loops; no fragile tick spam.

## Read-only contract (mod compatibility)
This system must remain **read-only**:
- It may **observe** campaign state (events, heroes, parties, map events, wars).
- It may **store its own** small, save-safe history (dispatch entries, cached initial strengths for battles, last bulletin day).
- It must **not** change campaign outcomes.

Concrete "do not" list:
- Do **not** move parties, change encounters, alter prisoners, change relations, change wars/peace, alter settlement ownership.
- Do **not** call actions like `GiveGoldAction`, `ChangeRelationAction`, `KillCharacterAction`, etc. from the news system.
- Do **not** patch core campaign models purely to "get data". Prefer decompile-confirmed APIs + fallbacks.

Why this matters:
- Read-only systems are naturally **other-mod friendly**, because multiple mods can observe the same events without stepping on each other.

## Where it appears (two-feed architecture)
The news system is split into **two separate feeds** based on relevance and context:

### 1. Kingdom News (main enlisted menu)
- **Location**: `enlisted_status` menu (main enlisted status screen)
- **Integration**: Appended to the **main status text** (the descriptive area showing Party Objective, Army, Rank, Formation, etc.)
- **Placement**: After core status info (Objective/Army/Rank/Formation/Fatigue), before escalation tracks or footer
- **Scope**: Kingdom-wide strategic events (wars, major battles, settlements falling, lord captures/executions, politics)
- **Display**: Top 3–5 headlines as a "Kingdom News" section within the status text
- **Optional submenu**: dedicated `enlisted_news` screen for full 10–30 item history

### 2. Personal & Army News (camp menu)
- **Location**: `enlisted_activities` menu (camp activities screen)
- **Integration**: Prepended to the **top of the menu text** (before camp activities list)
- **Scope**: Your immediate service context
  - Personal mentions (your party's participation in battles/sieges)
  - Your lord's army movements (forming, dispersing, regrouping)
  - Your immediate unit (retinue casualties, camp conditions)
  - Direct orders or changes to your posting
- **Display**: Top 2–3 items, more immediate/actionable than kingdom news

**Visual example (Personal News in `enlisted_activities`):**
```
--- Army Orders ---
Host forming under Lord Derthert's banner.
Our forces helped secure victory at Marunath.

[Camp Activities menu options follow below...]
```

This follows the established Menu Interface system patterns (tooltips, icons via `LeaveType`, localized strings in `ModuleData/Languages/enlisted_strings.xml`).

## Implementation Details

### News Prioritization & Display Duration
News items have a **priority system** based on importance:
- **Important news** (wars, major battles, sieges, executions, army movements): shows for **2 days minimum**
- **Minor news** (village raids, prisoner events, player battle participation): shows for **1 day minimum**
- News items are **"sticky"** - once displayed, they stay visible for their full duration even if new news arrives
- After minimum duration expires, news ages out naturally as newer items appear
- Items with same `StoryKey` update existing entries rather than creating duplicates

### Display Selection Logic
The feed prioritizes:
1. **Already-shown items** within their minimum display window (sticky)
2. **Higher importance items** (2-day news over 1-day news)
3. **Most recent items** among same priority

This creates a natural flow where important news dominates the feed, but new urgent news can still appear.

## System shape (matches existing doc conventions)
### Inputs
#### Native campaign events (Bannerlord)
Examples (names may vary by version; validate against 1.3.4 API/decompile):
- `CampaignEvents.MapEventEnded` (battle completion)
- Siege started/ended events
- `CampaignEvents.HeroPrisonerTaken`, prisoner release/escape equivalents
- `CampaignEvents.HeroKilledEvent` (death / execution detail)
- `CampaignEvents.ArmyDispersed`
- `CampaignEvents.VillageLooted`
- War/peace events
- Settlement owner change events

#### Enlisted-local context
- Current enlisted lord / kingdom context from `EnlistmentBehavior`
- "Recent history" timestamps/counters (prefer existing trackers where possible)
  - e.g. `CampaignTriggerTrackerBehavior` times (if useful)
  - Camp-life meters (`CampLifeBehavior`) for bulletin flavor lines

### Outputs
- A capped list of **Dispatch Items** displayed in menus.
- A periodic **Kingdom Bulletin** item summarizing key changes since the last bulletin.

## Scope: what gets reported (by feed)

### Kingdom News Feed (`enlisted_status`)
Reports kingdom-wide strategic events:
- **Wars and diplomacy**: declarations, peace treaties, alliances
- **Major battles**: kingdom vs kingdom clashes, army-scale engagements
- **Settlements**: sieges, captures, ownership changes
- **Lord fates**: captures, executions, escapes (kingdom lords only)
- **Politics**: clan defections, succession, fief grants
- **Frontier** (optional, config): neighboring kingdom events

### Personal & Army News Feed (`enlisted_activities`)
Reports your immediate service context:
- **Personal participation**: your party's role in battles/sieges
- **Your lord's army**: forming, dispersing, moving, regrouping after costly battles
- **Your unit**: retinue casualties, camp conditions, supply issues
- **Direct orders**: reassignments, special duties, urgent calls
- **Army-local events**: your army's skirmishes, foraging results, discipline issues

**Why split?**
- Kingdom news is *strategic context* — "what's the big picture?"
- Personal news is *tactical and immediate* — "what affects me right now?"
- Keeps both feeds focused and prevents spam in either location.

**Visual example (Kingdom News in `enlisted_status`):**
```
Party Objective : Following Lord Derthert
Army : Derthert (8 parties, 547 men)
Days Enlisted : 23
Rank : Man-at-Arms (Tier 2)
Formation : Infantry
Fatigue : 12/100

--- Kingdom News ---
War declared: Vlandia vs Battania.
Victory: Lord Aldric defeated Lord Calatild near Marunath.
Ormidore has fallen.
```

## Filtering and anti-spam rules

### Kingdom News (`enlisted_status`)
Baseline:
- Only generate while enlisted (`EnlistmentBehavior.Instance?.IsEnlisted == true`).
- Only include items where involved heroes/parties belong to your **lord's kingdom** (or faction if no kingdom).
- **Filters out bandit/looter battles** - only kingdom vs kingdom conflicts are reported

Anti-spam:
- Dedupe by `StoryKey` - updates existing items instead of creating duplicates
- Display shows top 3 items (configurable in menu integration)
- Feed maintains max 60 items in history (trimmed automatically)
- News items naturally age out after their minimum display duration expires

### Personal News (`enlisted_activities`)
Baseline:
- Only generate while enlisted.
- Focus on **player party**, **player lord**, and **player's army** exclusively.
- **Filters out bandit/looter battles** - only reports significant kingdom battles where player participated

Anti-spam:
- Display shows top 2 items (configurable in menu integration)
- Feed maintains max 20 items in history (trimmed automatically)
- News items age out after 1-2 days based on their minimum display duration
- Army movement detection uses day-to-day comparison to avoid duplicate "army forming" messages

## Localization Implementation

### Known Issue: Bannerlord Localization System
Bannerlord's `MBTextManager.GetLocalizedText()` has special behavior for English that prevents XML-based localization from working correctly:
- For English language, it immediately returns the fallback text without checking the translation database
- This means XML strings in `enlisted_strings.xml` are never looked up, even when properly formatted

### Current Workaround
The system uses **hardcoded text templates** in C# code:
- Templates are defined in `GetNewsTemplate()` method in `EnlistedNewsBehavior.cs`
- Placeholders (`{WINNER}`, `{LOSER}`, `{PLACE}`, etc.) are still processed correctly via `TextObject.SetTextVariable()`
- XML localization strings remain in `enlisted_strings.xml` for future use if/when a proper solution is found

### Placeholder style
Bannerlord-style variables in templates:
- Template: `"{WINNER} defeated {LOSER} near {PLACE}."`
- Fill: set variables (`WINNER`, `LOSER`, `PLACE`, `KINGDOM`, etc.) at runtime via `TextObject.SetTextVariable()`

### Core placeholder dictionary
- **Hero/lord**: `{LORD}`, `{WINNER}`, `{LOSER}`, `{CAPTOR}`, `{EXECUTOR}`, `{LEADER}`
- **Kingdom/faction**: `{KINGDOM}`, `{KINGDOM_A}`, `{KINGDOM_B}`, `{CLAN}`, `{FACTION}`
- **Location**: `{SETTLEMENT}`, `{PLACE}`, `{REGION}`, `{ROAD}`, `{TOWN}`, `{CASTLE}`, `{VILLAGE}`
- **Military**: `{ARMY}`, `{PARTIES}`, `{MEN}`, `{COUNT}`, `{LOSSES}`
- **Time**: `{DAY}`, `{DAYS}`, `{AGE}`
- **Culture**: `{CULTURE}`

### Fallback rules
If a placeholder can't be resolved:
- `{PLACE}` → "the countryside"
- `{SETTLEMENT}` → `{PLACE}`
- `{KINGDOM}` → "the realm"
- `{LORD}` → "a noble"
- `{TOWN}` / `{CASTLE}` / `{VILLAGE}` → `{SETTLEMENT}`

Never emit broken fragments like `near ` with nothing after it.

## Required localization strings (XML templates)
Add these to `ModuleData/Languages/enlisted_strings.xml`. All use Bannerlord `{PLACEHOLDER}` syntax.

### Section Headers
```xml
<string id="News_SectionHeader_Kingdom" text="--- Kingdom News ---" />
<string id="News_SectionHeader_Personal" text="--- Army Orders ---" />
```

### War and Diplomacy
```xml
<string id="News_War" text="War declared: {KINGDOM_A} vs {KINGDOM_B}." />
<string id="News_Peace" text="Peace signed with {KINGDOM}." />
<string id="News_Alliance" text="Alliance formed: {KINGDOM_A} and {KINGDOM_B}." />
<string id="News_Truce" text="Truce arranged with {KINGDOM}." />
```

### Battles (Kingdom Feed)
```xml
<string id="News_Victory" text="Victory: {WINNER} defeated {LOSER} near {PLACE}." />
<string id="News_Costly" text="Costly victory: {WINNER} drove off {LOSER} near {PLACE}." />
<string id="News_Pyrrhic" text="Pyrrhic victory: {WINNER} defeated {LOSER} near {PLACE}." />
<string id="News_Butchery" text="Bloodbath near {PLACE}: both sides were mauled." />
<string id="News_Defeat" text="Defeat: {LOSER} driven from {PLACE} by {WINNER}." />
<string id="News_Inconclusive" text="Skirmish near {PLACE} ended inconclusively." />
```

### Battles (Personal Feed)
```xml
<string id="News_PlayerBattle" text="Our forces helped secure victory at {PLACE}." />
<string id="News_PlayerDefeat" text="Our forces fought at {PLACE} but were driven back." />
<string id="News_PlayerSiege" text="Our company participated in the siege of {SETTLEMENT}." />
<string id="News_PlayerLosses" text="Heavy losses in recent engagement: {COUNT} retinue lost." />
<string id="News_PlayerDecisive" text="Your leadership proved decisive at {PLACE}." />
<string id="News_PlayerMinor" text="Your unit played a minor role in the battle at {PLACE}." />
```

### Sieges and Settlements
```xml
<string id="News_Siege" text="Siege laid to {SETTLEMENT}." />
<string id="News_SiegeLifted" text="Siege lifted at {SETTLEMENT}." />
<string id="News_Fallen" text="{SETTLEMENT} has fallen." />
<string id="News_Captured" text="{SETTLEMENT} captured by {KINGDOM}." />
<string id="News_GarrisonRebuilt" text="New garrison being assembled for {SETTLEMENT}." />
<string id="News_Refugees" text="Refugees fleeing {SETTLEMENT} spotted on roads." />
```

### Prisoners and Executions
```xml
<string id="News_PrisonerTaken" text="{LORD} taken prisoner." />
<string id="News_PrisonerCapturedBy" text="{LORD} captured by {CAPTOR}." />
<string id="News_PrisonerEscaped" text="{LORD} escaped captivity." />
<string id="News_PrisonerReleased" text="{LORD} released from captivity." />
<string id="News_Executed" text="{LORD} executed by {EXECUTOR}." />
<string id="News_ExecutedNoExecutor" text="{LORD} executed." />
<string id="News_Transport" text="Report: {LORD} being transported toward {SETTLEMENT}." />
```

### Prisoner Rumors
```xml
<string id="News_RumorExecution" text="Rumor: captors mean to execute {LORD}." />
<string id="News_RumorRansom" text="Ransom negotiations rumored for {LORD}." />
<string id="News_RumorRescue" text="Rescue attempt planned for {LORD}." />
<string id="News_RumorTransport" text="Rumor: {LORD} being marched toward {CASTLE}." />
```

### Army Movements (Personal Feed)
```xml
<string id="News_ArmyForming" text="Host forming under {LORD}'s banner." />
<string id="News_ArmyDisbanded" text="{LORD}'s army has dispersed." />
<string id="News_ArmyRegrouping" text="Forces regrouping after recent engagement." />
<string id="News_ArmyMarching" text="Army on the march toward {REGION}." />
<string id="News_ArmyRecruits" text="Reinforcements arriving at {SETTLEMENT}." />
```

### Raids and Villages
```xml
<string id="News_Raid" text="Raid reported near {SETTLEMENT}." />
<string id="News_VillageLooted" text="{VILLAGE} looted by hostile forces." />
<string id="News_Bandits" text="Banditry worsening along {REGION} roads." />
```

### Politics and Kingdom Events
```xml
<string id="News_ClanDefection" text="{CLAN} has switched allegiance to {KINGDOM}." />
<string id="News_FiefGranted" text="{LORD} granted lordship of {SETTLEMENT}." />
<string id="News_Succession" text="{LORD} now leads {KINGDOM}." />
<string id="News_HeirDeclared" text="{LORD} declared heir to {KINGDOM}." />
```

### Economy and Supply (Bulletin Flavor)
```xml
<string id="News_Trade" text="Trade falters; prices rise in {TOWN}." />
<string id="News_Famine" text="Food shortages worsen in {SETTLEMENT}." />
<string id="News_Supply" text="Supply wagons arrived; rations improved." />
<string id="News_Disease" text="Sickness reported in {SETTLEMENT}'s garrison." />
```

### Weather and Natural Events (Bulletin Flavor)
```xml
<string id="News_Storm" text="Severe storms delay campaigning in {REGION}." />
<string id="News_Winter" text="Winter campaign begins; fighting continues despite snow." />
<string id="News_Harvest" text="Harvest season approaching; armies may stand down." />
```

### Society and Peace-Time (Bulletin Flavor)
```xml
<string id="News_Tournament" text="A tournament draws nobles to {TOWN}." />
<string id="News_Wedding" text="Wedding talks between {CLAN_A} and {CLAN_B}." />
<string id="News_Festival" text="Festival of {CULTURE} underway in {SETTLEMENT}." />
<string id="News_Pilgrimage" text="Pilgrims traveling to {SETTLEMENT}." />
<string id="News_Caravan" text="Rich merchant caravan spotted near {SETTLEMENT}." />
```

### Direct Orders (Personal Feed)
```xml
<string id="News_OrderReassignment" text="Reassignment orders received: report to {LORD}." />
<string id="News_OrderDuty" text="Special duty assigned: {DUTY}." />
<string id="News_OrderUrgent" text="Urgent call: all units to muster at {SETTLEMENT}." />
<string id="News_OrderStandDown" text="Stand down orders: campaign suspended." />
```

### Consequences and Follow-ups
```xml
<string id="News_PostBattleRegrouping" text="{WINNER}'s forces regrouping after costly battle." />
<string id="News_PostSiegeFamine" text="Famine worsens in besieged {SETTLEMENT}." />
<string id="News_PostCaptureUnrest" text="Unrest reported in newly-captured {SETTLEMENT}." />
```

### Generic Fallbacks
```xml
<string id="News_Generic" text="Report from the field: {TEXT}." />
<string id="News_Rumor" text="Rumor: {TEXT}." />
<string id="News_Bulletin" text="Kingdom Bulletin: {TEXT}." />
<string id="News_NoNews" text="No news to report." />
```

### Placeholder Resolution Examples (C# side)
When generating news items in code:
```csharp
var newsText = new TextObject("{=News_Victory}Victory: {WINNER} defeated {LOSER} near {PLACE}.");
newsText.SetTextVariable("WINNER", winnerLord.Name);
newsText.SetTextVariable("LOSER", loserLord.Name);
newsText.SetTextVariable("PLACE", settlement?.Name ?? new TextObject("the countryside"));
```

## Detection strategy (how we catch "all kinds of things")
To detect lots of types **without expensive scanning**, combine:

- **Event-driven ingestion (facts)**
  - Best for: battle ended, capture, execution, war/peace, village looted, army dispersed, settlement owner changed.

- **2-day bulletin snapshot (bounded state checks)**
  - Best for: current wars, ongoing sieges, notable prisoners, recent settlement losses, economy/banditry flavor.
  - Snapshot scanning should be **kingdom-only** and infrequent.

This matches the same "event-driven + daily aggregation" philosophy used by Camp Life Simulation.

## Detection matrix (what to report)
> Event names vary by version; verify against 1.3.4 API/decompile.

### War and diplomacy (Kingdom feed)
- War declared / peace made
  - `"{=News_War}War declared: {KINGDOM_A} vs {KINGDOM_B}."`
  - `"{=News_Peace}Peace signed with {KINGDOM}."`

### Prisoners (capture/transport/execution) (Kingdom feed)
This is the core of:
- "Rumor has it he'll be executed"
- "He's on the way to this castle"

Facts:
- Capture: `"{=News_Captured}{LORD} taken prisoner."`
- Release/escape: `"{=News_Escaped}{LORD} escaped captivity."`
- Execution: `"{=News_Executed}{LORD} executed by {EXECUTOR}."` (if executor known)

Transport (semi-fact):
- If the campaign exposes a stable holder/party/settlement: `"{=News_Transport}Report: {LORD} transported toward {SETTLEMENT}."`
- Otherwise as rumor with confidence.

### Battles and outcomes (victory/defeat/pyrrhic)

#### Kingdom-scale battles (Kingdom feed)
Major army-level engagements between kingdoms.

#### Player participation (Personal feed)
- `"{=News_PlayerBattle}Our forces helped secure victory at {PLACE}."`
- `"{=News_PlayerSiege}Our company participated in the siege of {SETTLEMENT}."`
- `"{=News_PlayerLosses}Heavy losses in recent engagement: {COUNT} retinue lost."`

#### Winner (fact)
Prefer a direct winner flag:
- `mapEvent.HasWinner` + a winner side/result
- attacker/defender side winner flags

If no winner can be determined:
- classify as "inconclusive" / "contact broken".

#### Losses (best-effort)
Ideal approach for accurate pyrrhic detection:
- On battle start: cache `initialStrength` per side.
- On battle end: compute `losses = initial - remaining`.

Fallbacks:
- Use casualty fields if exposed on map event sides.
- Approximate from involved parties at start/end (kingdom-only, battle-only).

#### Classification (recommended)
Let `lossRate = losses / max(1, initialStrength)`.

Suggested labels:
- **Clean victory**: winner lossRate < 0.20
- **Costly victory**: winner lossRate 0.20–0.45
- **Pyrrhic victory**: winner lossRate ≥ 0.45 AND loser lossRate ≥ 0.55
- **Mutual ruin**: both sides lossRate ≥ 0.60

Templates:
- `"{=News_Victory}Victory: {WINNER} defeated {LOSER} near {PLACE}."`
- `"{=News_Costly}Costly victory: {WINNER} drove off {LOSER} near {PLACE}."`
- `"{=News_Pyrrhic}Pyrrhic victory: {WINNER} defeated {LOSER} near {PLACE}."`
- `"{=News_Butchery}Bloodbath near {PLACE}: both sides were mauled."`

### Sieges and settlements (Kingdom feed)
- Siege laid: `"{=News_Siege}Siege laid to {SETTLEMENT}."`
- Siege lifted: `"{=News_SiegeLifted}Siege lifted at {SETTLEMENT}."`
- Settlement taken: `"{=News_Fallen}{SETTLEMENT} has fallen."`

### Raids (Kingdom feed)
- Village looted: `"{=News_Raid}Raid reported near {SETTLEMENT}."`

### Army movements (Personal feed)
Your lord's army specifically:
- `"{=News_ArmyForming}Host forming under {LORD}'s banner."`
- `"{=News_ArmyDisbanded}{LORD}'s army has dispersed."`
- `"{=News_ArmyRegrouping}Forces regrouping after recent engagement."`

### Economy / logistics / security (bulletin lines, both feeds)
Prefer bulletin-only (avoid per-event spam):
- "Trade falters; prices rise in {TOWN}."
- "Banditry worsening along {REGION} roads."
- "Supply wagons arrived; rations improved."

If Camp Life Simulation is active, these lines can be *informed* by its daily snapshot (logistics strain, morale shock, pay tension).

### Society / peace-time (Kingdom feed, bulletin flavor)
Often not hard-exposed; treat as bulletin flavor or rumors:
- "A tournament draws nobles to {TOWN}."
- "Wedding talks between {CLAN_A} and {CLAN_B}."

## Rumors vs Reports (matches existing content philosophy)
Dispatch items come in three "types":
- **Report**: derived directly from an event or stable state.
- **Rumor**: generated probabilistically and later confirmed/denied.
- **Bulletin**: periodic digest.

Suggested fields per item (save-safe primitives):
- day
- type (report/rumor/bulletin)
- category
- headline/body
- confidence (rumor only)
- storyKey
- feed (kingdom or personal)

Example chain:
- Report: "{LORD} taken prisoner."
- Rumor (low): "Rumor: the captors mean to execute him."
- Rumor (medium): "Rumor: he is being marched toward {CASTLE}."
- Later bulletin:
  - Confirmed or denied.

## Edge cases and safety
Align with existing menu safety guidance:
- **Do not** activate/switch menus as a side-effect of dispatch generation.
- Keep dispatch updates to **text variable refresh** in existing menu ticks/refresh paths.
- If any menu tick handler is used, validate time deltas (`dt > 0`) like other menu systems.
- Avoid doing heavy work during siege/encounter transitions; bulletin generation should be safe and bounded.

## Mod-compatibility checklist
- [ ] **Read-only**: no world-state writes (actions, patches, forced outcomes).
- [ ] **Event-driven**: use `CampaignEvents` + infrequent bulletin snapshots; avoid per-tick scanning.
- [ ] **Bounded snapshots**: only scan within the player's kingdom/faction and only on the bulletin cadence.
- [ ] **Defensive fallbacks**: if an API field isn't available (because another mod changed behavior), downgrade text ("skirmish ended", "location unknown") instead of failing.
- [ ] **No menu side effects**: news generation must never trigger menu activation/switching; only update text variables when menus refresh.

## Configuration (proposed)
Follow the existing pattern of feature flags in `ModuleData/Enlisted/enlisted_config.json`:

```json
{
  "news_dispatches": {
    "enabled": true,
    "interval_days": 2,
    "max_entries_kingdom": 60,
    "max_entries_personal": 20,
    "kingdom_feed": {
      "enabled": true,
      "include_frontier": false
    },
    "personal_feed": {
      "enabled": true,
      "include_army": true,
      "include_retinue": true
    },
    "rumors": {
      "enabled": true,
      "max_active": 6
    }
  }
}
```

## File locations (implemented)
- **Core behavior**: `src/Features/Interface/Behaviors/EnlistedNewsBehavior.cs`
  - Singleton pattern with `Instance` property for menu access
  - Event-driven news generation (subscribes to 12+ campaign events)
  - Manual save/load via `SyncData()` for `DispatchItem` and `BattleSnapshot` data structures
  - Hardcoded template system in `GetNewsTemplate()` (workaround for localization issue)
  - Priority/backlog system for news display selection
  - Bandit battle filtering
- **Kingdom feed integration**: `src/Features/Interface/Behaviors/EnlistedMenuBehavior.cs`
  - Kingdom news appended to status display in `RefreshEnlistedStatusDisplay()` (replaces old formation training description)
  - Calls `EnlistedNewsBehavior.Instance?.BuildKingdomNewsSection(3)`
- **Personal feed integration**: `src/Features/Camp/CampMenuHandler.cs`
  - Personal news prepended to camp activities menu text in `OnCampActivitiesMenuInit()`
  - Uses text variable `{PERSONAL_NEWS}` in localized menu intro string
  - Calls `EnlistedNewsBehavior.Instance?.BuildPersonalNewsSection(2)`
  - Camp overview redesigned to prioritize news (shows top 2 personal + top 2 kingdom)
- **Localization**: `ModuleData/Languages/enlisted_strings.xml`
  - Contains XML string definitions (currently unused due to localization system issue)
  - Section headers: `News_SectionHeader_Kingdom`, `News_SectionHeader_Personal`
  - Menu integration: `act_menu_intro_with_news`
  - All headline template IDs documented but templates are in C# code

## Content expansion ideas (optional)
The following are **potential additions** that could make the feed richer. Evaluate each against your feature priorities.

### ✅ Natural fits (align with existing systems)
These integrate cleanly with what you already have:

- **Personal mentions** (Personal feed): when player party participates in reported events, add flavor.
  - `"{=News_PlayerBattle}Our forces helped secure victory at {PLACE}."`
  - Already tracked: you have kill tracking, retinue participation, enlistment context.

- **Consequences & follow-ups** (Kingdom feed): after major events, generate cascading headlines.
  - Settlement falls → refugees / garrison / economic impact lines.
  - Capture → ransom/rescue rumors.
  - Fits your **rumor→confirmed** model perfectly.

- **Strategic intel (army movements)** (Personal feed): you already subscribe to `ArmyDispersed`; add `ArmyCreated` for:
  - `"{=News_ArmyForming}Large host gathering under {LORD}'s banner."`

- **Kingdom politics** (Kingdom feed): if events exist (clan defects, fief granted, succession), easy to add.

- **Categories/tags**: straightforward organization; can filter in UI or via config.
  - Kingdom: wars/battles/politics/diplomacy
  - Personal: orders/army/unit/participation

- **Source reliability** tiers (official/scout/merchant/tavern): maps directly to your existing `confidence` field.

- **Aging/staleness**: fade older items by day age (simple timestamp logic).
  - Kingdom feed: 7-day retention
  - Personal feed: 2-day retention (assume player checks regularly)

- **Interconnected story arcs**: your `StoryKey` system already supports this (track siege lifecycle, prisoner journey).

- **Commander personality flavoring** (Kingdom feed): you already have lord context; add tone modifiers based on traits.
  - Honorable: "Glorious victory achieved with honor at {PLACE}."
  - Cruel: "{PLACE} taken. Survivors scattered."

- **Weather/natural events** (bulletin flavor, both feeds): "Severe storms delay campaigning." / "Sickness in {SETTLEMENT}."

- **Cultural/flavor events** (peace-time, Kingdom feed): festivals, pilgrimages, merchant caravans.

- **Contribution tracking** (Personal feed): show player's role tier in reported battles ("minor role" / "decisive").
  - You already have kill tracking and retinue casualty tracking; extracting participation tier is straightforward.

### ⚠ Moderate complexity (possible, but consider ROI)
These are feasible but add non-trivial work:

- **Actionable intelligence**: dispatches with associated actions.
  - **Safe approach**: link to **existing** systems only (ex: "reinforcements needed" → vanilla fast-travel, or link to an existing duty/camp action).
  - **Risk**: don't create new gameplay loops (raid caravans, rescue missions) just for dispatches—those need their own design.

- **Dispatch pins/priorities**: let players filter or highlight certain news types.
  - Adds UI state, menu options, and persistence.

- **Save/export to file**: generate campaign journal text file.
  - Moderate work; good for community storytelling/AARs.

- **Map integration**: clicking dispatch highlights location.
  - Significant UI work; you don't currently have map marker systems.

- **Statistics dashboard**: aggregates over time (most active lord, win/loss ratio).
  - Data collection is easy; presenting it is moderate.

### ❌ Scope creep / conflicts (avoid or defer)
These either break the read-only contract, duplicate existing systems, or add heavy complexity:

- **Allied kingdom news**: more world scanning; harder to filter/prioritize. Defer unless it's a key player request.

- **Historical callbacks**: interesting flavor ("one year since…") but low ROI; low priority.

- **"Breaking news" interrupt**: **conflicts with your existing Lance Life Events incident system**, which already has popup priority/queue logic and handles critical alerts. Don't duplicate. If you want critical news to feel urgent, integrate with that system instead (tag certain dispatch items to also fire an inquiry if they cross a threshold).

### Recommended first expansions (if any)
If you decide to expand beyond the baseline spec:
1. **Personal mentions** (Personal feed, already have data)
2. **Army movements** (Personal feed, already have `ArmyDispersed`)
3. **Consequences/follow-ups** (Kingdom feed, fits rumor model)
4. **Commander tone flavoring** (Kingdom feed, cheap, high immersion)
5. **Categories/source tiers** (both feeds, easy config work)

### Future: Lance Duty Coverage System (Social Feature)
A separate feature that uses the Personal News feed but is primarily a **social/lance relationship mechanic**:

**Concept:**
- **Player asks lance mate to cover duty** → allows training different skill for the day
- **Lance mate asks player to cover** → builds relationship, fatigue cost

**Integration with News:**
- Reports coverage requests in Personal feed: *"{LANCEMATE} asks if you can cover their duty."*
- Reports outcomes: *"{LANCEMATE} covering your duty today."* or *"You covered for {LANCEMATE} today."*

**Full details:** See [news-dispatches-implementation-plan.md](./news-dispatches-implementation-plan.md) Phase 9, item 6.

**Why this matters:**
- Adds **social gameplay loop** to lance system
- Creates **skill training flexibility** without breaking duty restrictions
- Uses Personal News feed as **communication channel** for lance interactions
- Deepens **lance relationships** through favors and reciprocity

## Acceptance criteria

### Kingdom News Feed
- [x] While enlisted, `enlisted_status` shows top 3 headlines that update over time.
- [x] Formation training RP description (old flavor text) has been removed from the status display.
- [x] Captured lords generate a report headline.
- [x] Battles generate correct winner/loser phrasing; pyrrhic labeling triggers only on high-loss outcomes.
- [x] War/peace changes are reported immediately when events fire.
- [x] The feed is deduped by StoryKey (no duplicate entries for same event).
- [x] Bandit battles are filtered out (only kingdom vs kingdom conflicts).
- [x] News items have priority system (2-day important news, 1-day minor news).

### Personal News Feed
- [x] While enlisted, `enlisted_activities` shows top 2 personal headlines at the top of the menu.
- [x] Player participation in battles generates a personal mention (victory or defeat).
- [x] Player lord's army movements (forming, dispersing) are detected and reported.
- [x] Feed auto-expires items after their minimum display duration.
- [x] Camp overview redesigned to prioritize news display (shows personal + kingdom top items).

### Both Feeds
- [x] All text uses TextObject with runtime placeholders (hardcoded templates due to localization issue).
- [x] Purely event-driven: no tick scanning, only CampaignEvents subscriptions.
- [x] Read-only: no world-state writes, no action calls, no forced outcomes.
- [x] Graceful save/load handling with manual serialization.
- [x] Bandit/looter battles filtered out automatically.

### Known Limitations
- ⚠ Localization uses hardcoded templates instead of XML due to Bannerlord's localization system behavior
- ⚠ Rumor/followup system not yet implemented (just direct reports)
- ⚠ Bulletin system not implemented (news is event-driven only, no periodic digests)
- ⚠ Some optional features from spec not implemented: prisoner transport rumors, consequence chains, commander tone flavoring
