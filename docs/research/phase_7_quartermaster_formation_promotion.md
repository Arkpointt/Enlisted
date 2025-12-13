# Phase 7 — Quartermaster, Formation & Promotion Rework

**Priority:** After Phase 5 (Content Conversion) and Phase 6 (Polish)
**Dependencies:** Phase 4.5 (Duty Alignment), Phase 5 (Events Working)
**Scope:** Unify formation choice, gear acquisition, duty assignment, and promotion flow

---

## Table of Contents

1. [Overview](#overview)
2. [Current State](#current-state)
3. [Target State](#target-state)
4. [The New Flow](#the-new-flow)
5. [Formation System Changes](#formation-system-changes)
6. [Quartermaster Changes](#quartermaster-changes)
7. [Promotion System Integration](#promotion-system-integration)
8. [Duty Assignment Changes](#duty-assignment-changes)
9. [Implementation Tasks](#implementation-tasks)
10. [Migration & Compatibility](#migration--compatibility)
11. [Testing Scenarios](#testing-scenarios)

---

## Overview

This phase unifies three interconnected systems:

| System | Current | Target |
|--------|---------|--------|
| **Formation** | Derived from troop selection menu | Chosen in T1→T2 proving event |
| **Gear** | Auto-equipped from selected troop | Purchased from Quartermaster |
| **Duty** | Player picks from menu anytime | Auto-assigned on formation choice, can request change later |
| **Promotion** | XP threshold → troop selection menu | XP + requirements → proving event → narrative choice |

### Design Goals

1. **More immersive** — Formation is a story moment, not a menu
2. **Better economy** — Gear costs money, progression feels earned
3. **Military feel** — You don't choose your job, but you can request transfer
4. **Unified flow** — Formation → Duty → Gear all connected

---

## Current State

### Formation Selection
- T1: Auto-assigned Infantry (based on lord's BasicTroop)
- T2+: Player opens Troop Selection menu, picks specific troop
- Formation derived from troop properties (`IsRanged`, `IsMounted`)

### Gear Acquisition
- Troop Selection: Equipment auto-applied from selected troop
- Quartermaster: Buy additional/replacement gear based on troop identity
- Troop identity drives QM inventory

### Duty Assignment
- Player opens duty menu anytime
- Picks from formation-filtered list
- No restrictions beyond formation

### Promotion
- Pure XP threshold → auto-promote
- Troop selection menu opens
- Generic ceremony popup

---

## Target State

### Formation Selection
- T1: Infantry (everyone starts as foot soldier)
- T1→T2: **Proving event** presents formation choice narratively
- T2+: Formation locked to choice (can't change)

### Gear Acquisition
- T1: Basic levy gear (from bag check or starter kit)
- T2+: **Quartermaster only** — buy gear for your formation/tier/culture
- No auto-equip on promotion
- Prompt: "Report to Quartermaster for your new kit"

### Duty Assignment
- T1: Auto-assigned Runner (grunt work)
- T2: Auto-assigned **starter duty** for chosen formation
- T2+: Can **request** different duty (lance leader approval, cooldown)
- T3+: Additional duties unlock (Engineer, etc.)

### Promotion
- XP + days + events + battles + reputation + discipline
- **Proving event** fires when eligible
- Narrative choice determines outcome
- Formation choice embedded in T1→T2 event

---

## The New Flow

### Complete Player Journey

```
ENLIST (T1 Levy)
├── Formation: Infantry (default)
├── Duty: Runner (auto-assigned)
├── Gear: Bag check → basic levy gear
└── Quartermaster: T1 Infantry gear available

ELIGIBLE FOR T2
├── Requirements met (XP, days, events, battles, reputation)
├── Proving event queued: "Finding Your Place"
└── Fires when ai_safe

T1→T2 PROVING EVENT
├── Narrative setup: "{LANCE_LEADER_SHORT} looks you over..."
├── Player choice:
│   ├── "Shield wall" → Infantry
│   ├── "Give me a bow" → Archer
│   ├── "Put me on a horse" → Cavalry
│   └── "Shoot from saddle" → Horse Archer (Aserai/Khuzait)
├── Formation set
├── Starter duty assigned (Lookout for Archer, etc.)
├── Tier → 2
└── Message: "Report to Quartermaster for your kit"

VISIT QUARTERMASTER
├── Inventory: Culture gear for your formation, T1-T2
├── Player buys what they want/can afford
└── No auto-equip, player choice

T2→T3 PROMOTION
├── Requirements met
├── Proving event: "The Sergeant's Test"
├── Narrative test of judgment
├── Tier → 3, duty slots → 2
├── Higher tier gear unlocks at QM
└── Can request duty change now

...continues through T6
```

---

## Formation System Changes

### Remove: Troop Selection Menu

**Current:** `TroopSelectionManager.cs` opens menu, player picks troop

**New:** Remove this menu entirely. Formation chosen in proving event.

### Add: Formation Choice in Proving Event

The T1→T2 proving event ("Finding Your Place") handles formation selection:

```json
{
  "id": "promotion_t1_t2_finding_your_place",
  "category": "promotion",
  "content": {
    "options": [
      {
        "id": "choose_infantry",
        "text": "\"The shield wall. I'll hold the line.\"",
        "effects": {
          "formation": "infantry",
          "promotes": true
        }
      },
      {
        "id": "choose_archer",
        "text": "\"Give me a bow. I'll strike from distance.\"",
        "effects": {
          "formation": "archer",
          "promotes": true
        }
      },
      {
        "id": "choose_cavalry",
        "text": "\"Put me on a horse.\"",
        "effects": {
          "formation": "cavalry",
          "promotes": true
        }
      },
      {
        "id": "choose_horse_archer",
        "text": "\"I can shoot from the saddle.\"",
        "condition": "faction_has_horse_archers",
        "effects": {
          "formation": "horsearcher",
          "promotes": true
        }
      }
    ]
  }
}
```

### Formation State

```csharp
// In EnlistedDutiesBehavior or new FormationManager
private string _playerFormation = "infantry"; // Default

public string PlayerFormation => _playerFormation;

public void SetFormation(string formation)
{
    _playerFormation = formation;
    OnFormationChanged?.Invoke(formation);
}

// Called by event effect handler
public void OnPromotionFormationChosen(string formation)
{
    SetFormation(formation);
    AssignStarterDuty(formation);
    NotifyQuartermasterUpdate();
    ShowMessage("Report to the Quartermaster for your new kit.");
}
```

### Formation Lock

Once chosen at T2, formation cannot be changed for the rest of enlistment:

```csharp
public bool CanChangeFormation => _enlistmentTier < 2;
```

---

## Quartermaster Changes

### Remove: Troop Identity Dependency

**Current:** QM inventory based on specific troop from Troop Selection

**New:** QM inventory based on Formation + Tier + Culture

### New Inventory Logic

```csharp
public List<ItemObject> GetAvailableEquipment()
{
    var culture = EnlistmentBehavior.Instance.EnlistedLord.Culture.StringId;
    var formation = EnlistedDutiesBehavior.Instance.PlayerFormation;
    var tier = EnlistmentBehavior.Instance.EnlistmentTier;
    
    // Find all troops matching criteria
    var matchingTroops = MBObjectManager.Instance
        .GetObjectTypeList<CharacterObject>()
        .Where(t => t.Culture?.StringId == culture)
        .Where(t => t.IsSoldier)
        .Where(t => t.Tier <= tier)
        .Where(t => MatchesFormation(t, formation))
        .ToList();
    
    // Extract all equipment from those troops
    var equipment = new HashSet<ItemObject>();
    foreach (var troop in matchingTroops)
    {
        foreach (var battleEquip in troop.BattleEquipments)
        {
            for (int i = 0; i < (int)EquipmentIndex.NumEquipmentSetSlots; i++)
            {
                var item = battleEquip.GetEquipmentFromSlot((EquipmentIndex)i).Item;
                if (item != null)
                    equipment.Add(item);
            }
        }
    }
    
    return equipment.ToList();
}

private bool MatchesFormation(CharacterObject troop, string formation)
{
    return formation switch
    {
        "infantry" => !troop.IsRanged && !troop.IsMounted,
        "archer" => troop.IsRanged && !troop.IsMounted,
        "cavalry" => !troop.IsRanged && troop.IsMounted,
        "horsearcher" => troop.IsRanged && troop.IsMounted,
        "naval" => false, // Handle separately with expansion check
        _ => false
    };
}
```

### Tier Progression at QM

| Tier | Available Gear |
|------|----------------|
| T1 | Culture T1 troops for Infantry only |
| T2 | Culture T1-T2 troops for chosen formation |
| T3 | Culture T1-T3 troops for chosen formation |
| T4 | Culture T1-T4 troops for chosen formation |
| T5 | Culture T1-T5 troops for chosen formation |
| T6 | Culture T1-T6 troops for chosen formation |

### QM Menu Updates

**After promotion, show prompt:**
```csharp
// In promotion effect handler
InformationManager.AddQuickInformation(
    new TextObject("You've been promoted! Report to the Quartermaster for your new kit."),
    0, Hero.MainHero.CharacterObject
);
```

**In QM menu, show new items:**
```csharp
// Highlight items player hasn't seen before
private HashSet<string> _seenItems = new();

public bool IsNewItem(ItemObject item)
{
    return !_seenItems.Contains(item.StringId);
}

public void MarkItemSeen(ItemObject item)
{
    _seenItems.Add(item.StringId);
}
```

---

## Promotion System Integration

### Remove: Auto-Promote on XP

**Current:** Hit XP threshold → instant promotion → troop selection

**New:** Hit requirements → proving event queues → fires when safe → player choice

### Promotion Flow

```csharp
// In daily tick or promotion checker
public void CheckPromotionEligibility()
{
    var nextTier = _enlistmentTier + 1;
    if (nextTier > 6) return; // T6 is max for enlisted
    
    var requirements = GetPromotionRequirements(nextTier);
    
    if (MeetsAllRequirements(requirements))
    {
        // Queue proving event instead of auto-promoting
        var eventId = $"promotion_t{_enlistmentTier}_t{nextTier}";
        LanceLifeEventManager.Instance.QueueEvent(eventId, priority: "critical");
    }
}

private bool MeetsAllRequirements(PromotionRequirements req)
{
    return _enlistmentXP >= req.XP
        && _daysInRank >= req.DaysInRank
        && _eventsCompleted >= req.EventsRequired
        && _battlesSurvived >= req.BattlesRequired
        && _lanceReputation >= req.MinLanceRep
        && _discipline < req.MaxDiscipline
        && GetLeaderRelation() >= req.MinLeaderRelation;
}
```

### Promotion Requirements (Reference)

| Promotion | XP | Days | Events | Battles | Lance Rep | Leader Rel | Max Disc |
|-----------|-----|------|--------|---------|-----------|------------|----------|
| T1→T2 | 700 | 14 | 5 | 2 | ≥0 | ≥0 | <8 |
| T2→T3 | 2,200 | 35 | 12 | 6 | ≥10 | ≥10 | <7 |
| T3→T4 | 4,400 | 56 | 25 | 12 | ≥20 | ≥20 | <6 |
| T4→T5 | 6,600 | 56 | 40 | 20 | ≥30 | ≥30 | <5 |
| T5→T6 | 8,800 | 56 | 55 | 30 | ≥40 | ≥15 (lord) | <4 |

### Proving Events Handle Promotion

```csharp
// In event effect handler
public void ApplyPromotionEffects(EventOption option)
{
    if (option.Effects.Promotes)
    {
        _enlistmentTier++;
        _daysInRank = 0;
        
        // Formation choice (T1→T2 only)
        if (option.Effects.Formation != null)
        {
            OnPromotionFormationChosen(option.Effects.Formation);
        }
        
        // Character tag (T3→T4)
        if (option.Effects.CharacterTag != null)
        {
            SetCharacterTag(option.Effects.CharacterTag);
        }
        
        // Loyalty tag (T5→T6)
        if (option.Effects.LoyaltyTag != null)
        {
            SetLoyaltyTag(option.Effects.LoyaltyTag);
        }
        
        // Update duty slots
        UpdateDutySlots();
        
        // Show ceremony
        ShowPromotionCeremony(_enlistmentTier);
    }
}
```

---

## Duty Assignment Changes

### Starter Duties by Formation

```json
{
  "starter_duties": {
    "infantry": "runner",
    "archer": "lookout",
    "cavalry": "messenger",
    "horsearcher": "scout",
    "naval": "boatswain"
  }
}
```

### Auto-Assignment on Formation Choice

```csharp
public void AssignStarterDuty(string formation)
{
    var starterDuty = _config.StarterDuties[formation];
    AssignDuty(starterDuty);
    
    InformationManager.AddQuickInformation(
        new TextObject($"You've been assigned to {GetDutyDisplayName(starterDuty)} duty."),
        0, Hero.MainHero.CharacterObject
    );
}
```

### Duty Request System (T2+)

**Current:** Player picks duty freely from menu

**New:** Player requests duty change, subject to approval

```csharp
public class DutyRequestResult
{
    public bool Approved { get; set; }
    public string Reason { get; set; }
}

public DutyRequestResult RequestDutyChange(string newDutyId)
{
    // Check cooldown
    if (_lastDutyChangeRequest != null && 
        CampaignTime.Now - _lastDutyChangeRequest < CampaignTime.Days(14))
    {
        return new DutyRequestResult 
        { 
            Approved = false, 
            Reason = "You must wait before requesting another duty change." 
        };
    }
    
    // Check lance reputation
    if (_lanceReputation < 10)
    {
        return new DutyRequestResult 
        { 
            Approved = false, 
            Reason = "{LANCE_LEADER_SHORT} doesn't think you've earned a transfer yet." 
        };
    }
    
    // Check tier requirements
    var duty = GetDutyDefinition(newDutyId);
    if (_enlistmentTier < duty.TierRequired)
    {
        var cultureId = EnlistmentBehavior.Instance.EnlistedLord.Culture.StringId;
        var requiredRank = GetRankTitle(duty.TierRequired, cultureId);
        return new DutyRequestResult 
        { 
            Approved = false, 
            Reason = $"{duty.DisplayName} requires rank {requiredRank} or higher." 
        };
    }
    
    // Approved
    _lastDutyChangeRequest = CampaignTime.Now;
    AssignDuty(newDutyId);
    
    return new DutyRequestResult 
    { 
        Approved = true, 
        Reason = $"{LANCE_LEADER_SHORT} approves your transfer to {duty.DisplayName}." 
    };
}
```

### Duty Menu Changes

**Current:** Direct selection

**New:** Request system with feedback

```
DUTY SELECTION MENU

Current Duty: Runner
Duty Slots: 1/2

Available Duties:
├── Quartermaster [Request Transfer]
├── Field Medic [Request Transfer]  
├── Armorer [Request Transfer]
├── Runner (Current)
└── Engineer [Requires {T3_RANK}]

[Request Transfer] → Shows approval/denial with reason
```

### Culture-Aware Rank Display

The duty menu and promotion UI must use culture-specific rank names:

```csharp
// When displaying tier requirements
var requiredRank = GetRankTitle(duty.TierRequired, cultureId);
var displayText = $"{duty.DisplayName} [Requires {requiredRank}]";

// Example outputs:
// Empire: "Engineer [Requires Immunes]"
// Vlandia: "Engineer [Requires Footman]"
// Sturgia: "Engineer [Requires Fyrdman]"
```

---

## Culture-Specific Rank Integration

All UI elements that display ranks must use culture-specific titles.

### Rank Resolution Helper

```csharp
public static class RankHelper
{
    private static readonly Dictionary<string, Dictionary<int, string>> CultureRanks = new()
    {
        ["empire"] = new() { [1]="Tiro", [2]="Miles", [3]="Immunes", [4]="Principalis", [5]="Evocatus", [6]="Centurion", [7]="Primus Pilus", [8]="Tribune", [9]="Legate" },
        ["vlandia"] = new() { [1]="Peasant", [2]="Levy", [3]="Footman", [4]="Man-at-Arms", [5]="Sergeant", [6]="Knight Bachelor", [7]="Cavalier", [8]="Banneret", [9]="Castellan" },
        ["sturgia"] = new() { [1]="Thrall", [2]="Ceorl", [3]="Fyrdman", [4]="Drengr", [5]="Huskarl", [6]="Varangian", [7]="Champion", [8]="Thane", [9]="High Warlord" },
        ["khuzait"] = new() { [1]="Outsider", [2]="Nomad", [3]="Noker", [4]="Warrior", [5]="Veteran", [6]="Bahadur", [7]="Arban", [8]="Zuun", [9]="Noyan" },
        ["battania"] = new() { [1]="Woodrunner", [2]="Clan Warrior", [3]="Skirmisher", [4]="Raider", [5]="Oathsworn", [6]="Fian", [7]="Highland Champion", [8]="Clan Chief", [9]="High King's Guard" },
        ["aserai"] = new() { [1]="Tribesman", [2]="Skirmisher", [3]="Footman", [4]="Veteran", [5]="Guard", [6]="Faris", [7]="Emir's Chosen", [8]="Sheikh", [9]="Grand Vizier" },
        ["mercenary"] = new() { [1]="Follower", [2]="Recruit", [3]="Free Sword", [4]="Veteran", [5]="Blade", [6]="Chosen", [7]="Captain", [8]="Commander", [9]="Marshal" }
    };
    
    public static string GetRankTitle(int tier, string cultureId)
    {
        if (CultureRanks.TryGetValue(cultureId?.ToLower() ?? "mercenary", out var ranks))
        {
            if (ranks.TryGetValue(tier, out var title))
                return title;
        }
        // Fallback to mercenary
        return CultureRanks["mercenary"][tier];
    }
    
    public static string GetCurrentRank(EnlistmentBehavior enlistment)
    {
        var tier = enlistment.EnlistmentTier;
        var culture = enlistment.EnlistedLord?.Culture?.StringId ?? "mercenary";
        return GetRankTitle(tier, culture);
    }
    
    public static string GetNextRank(EnlistmentBehavior enlistment)
    {
        var tier = enlistment.EnlistmentTier + 1;
        var culture = enlistment.EnlistedLord?.Culture?.StringId ?? "mercenary";
        return GetRankTitle(tier, culture);
    }
}
```

### UI Integration Points

| UI Element | What to Display | How |
|------------|-----------------|-----|
| Enlisted Status | Current rank | `RankHelper.GetCurrentRank(enlistment)` |
| Promotion UI | Next rank | `RankHelper.GetNextRank(enlistment)` |
| Duty Menu | Tier requirements | `RankHelper.GetRankTitle(duty.TierRequired, cultureId)` |
| Event Text | Placeholders | Replace `{PLAYER_RANK}`, `{NEXT_RANK}` at runtime |
| QM Menu | Tier labels | `RankHelper.GetRankTitle(tier, cultureId)` |

### Placeholder Resolution

Event text uses placeholders that resolve at runtime:

```csharp
public string ResolveRankPlaceholders(string text, EnlistmentBehavior enlistment)
{
    var culture = enlistment.EnlistedLord?.Culture?.StringId ?? "mercenary";
    var tier = enlistment.EnlistmentTier;
    
    return text
        .Replace("{PLAYER_RANK}", RankHelper.GetRankTitle(tier, culture))
        .Replace("{NEXT_RANK}", RankHelper.GetRankTitle(tier + 1, culture))
        .Replace("{T1_RANK}", RankHelper.GetRankTitle(1, culture))
        .Replace("{T2_RANK}", RankHelper.GetRankTitle(2, culture))
        .Replace("{T3_RANK}", RankHelper.GetRankTitle(3, culture))
        .Replace("{T4_RANK}", RankHelper.GetRankTitle(4, culture))
        .Replace("{T5_RANK}", RankHelper.GetRankTitle(5, culture))
        .Replace("{T6_RANK}", RankHelper.GetRankTitle(6, culture))
        .Replace("{T7_RANK}", RankHelper.GetRankTitle(7, culture))
        .Replace("{T8_RANK}", RankHelper.GetRankTitle(8, culture))
        .Replace("{T9_RANK}", RankHelper.GetRankTitle(9, culture))
        .Replace("{COMMANDER_TITLE}", GetCommanderTitle(culture));
}

private string GetCommanderTitle(string cultureId)
{
    return cultureId switch
    {
        "empire" => "Tribune",
        "vlandia" => "Captain",
        "sturgia" => "Thane",
        "khuzait" => "Noyan",
        "battania" => "Clan Chief",
        "aserai" => "Sheikh",
        _ => "Captain"
    };
}
```

---

## Implementation Tasks

**Status:** ✅ COMPLETE (December 2024)

### Task 0: Culture-Specific Rank System
- [x] Create `RankHelper` static class with culture rank dictionary
- [x] Add `GetRankTitle(tier, cultureId)` method
- [x] Add `GetCurrentRank()` and `GetNextRank()` convenience methods
- [x] Add placeholder resolution for event text
- [x] Update all UI elements to use culture-specific ranks

### Task 1: Remove Troop Selection Menu
- [x] Remove `TroopSelectionManager.cs` menu trigger on promotion
- [x] Keep troop data access for QM inventory
- [x] Remove auto-equip on promotion

### Task 2: Add Formation Choice to Proving Event
- [x] Update T1→T2 event with formation options
- [x] Add `formation` effect type to event system
- [x] Handle `faction_has_horse_archers` condition
- [x] Persist formation choice

### Task 3: Update Quartermaster Inventory
- [x] Replace troop identity with formation+tier+culture
- [x] Implement new `GetAvailableEquipment()` logic
- [x] Add "new item" indicators
- [x] Add post-promotion prompt

### Task 4: Implement Promotion Requirements
- [x] Add requirement checking beyond XP
- [x] Track days in rank, events completed, battles survived
- [x] Queue proving event instead of auto-promote
- [x] Display eligibility status in UI

### Task 5: Add Duty Request System
- [x] Implement starter duty assignment
- [x] Add request/approval flow
- [x] Add cooldown tracking
- [x] Update duty menu UI

### Task 6: Wire Up Proving Events
- [x] T1→T2: Formation choice + starter duty
- [x] T2→T3: Judgment test
- [x] T3→T4: Crisis + character tag
- [x] T4→T5: Lance vote
- [x] T5→T6: Lord audience + loyalty tag

### Task 7: Update Persistence
- [x] Add formation to save data
- [x] Add days_in_rank tracking
- [x] Add events_completed tracking
- [x] Add battles_survived tracking
- [x] Add duty_request_cooldown tracking
- [x] Add migration logic for existing saves

---

## Migration & Compatibility

### Existing Saves

Players with existing saves need migration:

```csharp
public void MigrateSaveData()
{
    // If player has troop selection but no formation
    if (_playerFormation == null && _selectedTroopId != null)
    {
        // Derive formation from existing troop
        var troop = MBObjectManager.Instance.GetObject<CharacterObject>(_selectedTroopId);
        if (troop != null)
        {
            _playerFormation = DeriveFormationFromTroop(troop);
        }
        else
        {
            _playerFormation = "infantry"; // Fallback
        }
    }
    
    // If player has no starter duty assigned
    if (_activeDuties.Count == 0 && _enlistmentTier >= 1)
    {
        AssignStarterDuty(_playerFormation);
    }
    
    // Initialize new tracking fields
    if (_daysInRank == 0 && _enlistmentTier > 1)
    {
        // Estimate from existing data
        _daysInRank = (int)(CampaignTime.Now - _lastPromotionDate).ToDays;
    }
}
```

### Feature Flag

```json
{
  "promotion_system": {
    "enabled": true,
    "use_proving_events": true,
    "use_legacy_troop_selection": false,
    "use_duty_request_system": true
  }
}
```

If issues arise, can revert to legacy system:
- `use_proving_events: false` → Old XP-only promotion
- `use_legacy_troop_selection: true` → Old troop menu
- `use_duty_request_system: false` → Old free duty selection

---

## Testing Scenarios

### Scenario 1: Fresh Enlistment
1. Enlist with lord
2. Verify: Formation = Infantry, Duty = Runner
3. Verify: QM shows T1 Infantry gear only
4. Reach T2 eligibility
5. Verify: Proving event fires
6. Choose Archer
7. Verify: Formation = Archer, Duty = Lookout
8. Verify: QM shows T1-T2 Archer gear
9. Verify: Message prompts QM visit

### Scenario 2: Duty Request
1. Reach T2 as Archer with Lookout duty
2. Request Scout duty
3. Verify: Approved (lance rep > 10)
4. Verify: Cooldown starts
5. Immediately request Lookout again
6. Verify: Denied (cooldown)
7. Wait 14 days
8. Request again
9. Verify: Approved

### Scenario 3: Promotion Requirements
1. Reach XP for T3
2. Verify: NOT promoted (missing other requirements)
3. Complete required events
4. Verify: Still not promoted (missing battles)
5. Survive required battles
6. Verify: Proving event fires
7. Complete event
8. Verify: Promoted to T3

### Scenario 4: Horse Archer (Khuzait)
1. Enlist with Khuzait lord
2. Reach T2 eligibility
3. Verify: Proving event shows Horse Archer option
4. Choose Horse Archer
5. Verify: Formation = Horse Archer, Duty = Scout
6. Verify: QM shows Horse Archer gear

### Scenario 5: Horse Archer (Vlandia - Should Not Appear)
1. Enlist with Vlandian lord
2. Reach T2 eligibility
3. Verify: Proving event does NOT show Horse Archer option
4. Only Infantry/Archer/Cavalry available

### Scenario 6: Save Migration
1. Load save with old troop selection system
2. Verify: Formation derived from existing troop
3. Verify: Starter duty assigned if missing
4. Verify: No errors or crashes
5. Verify: Can continue progression normally

---

## Files Affected

| File | Changes |
|------|---------|
| `TroopSelectionManager.cs` | Remove or gut (keep data access only) |
| `QuartermasterManager.cs` | New inventory logic |
| `EnlistedDutiesBehavior.cs` | Starter duty, request system |
| `EnlistmentBehavior.cs` | Promotion requirements, event triggers |
| `LanceLifeEventEffectHandler.cs` | Formation effect, promotion effects |
| `duties_system.json` | Add starter_duties config |
| `promotion_config.json` | New file with requirements |
| `enlisted_strings.xml` | New UI strings |

---

## Reference Documents

- `promotion_system.md` — Proving events content (T1-T6)
- `commander_track_schema.md` — T7-T9 framework
- `lance_life_schemas.md` — Event/duty schemas
- `phase_4_5_duty_alignment.md` — Duty system baseline

---

*Document Version: 1.0*
*Phase: 7 (Post-Content Conversion)*
*Dependencies: Phase 5 complete, events working*
