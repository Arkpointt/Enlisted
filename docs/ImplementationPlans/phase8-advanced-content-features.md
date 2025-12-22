# Phase 8: Advanced Content Features Implementation

**Status**: Ready for Implementation  
**Last Updated**: December 22, 2025  
**Prerequisites**: Phases 1-6 Complete (Content System Core)  
**Target Game Version**: Bannerlord v1.3.11

---

## Engineering Standards

**Follow these while implementing all phases:**

### Code Quality
- **Follow ReSharper linter/recommendations.** Fix warnings; don't suppress them with pragmas.
- **Comments should be factual descriptions of current behavior.** Write them as a human developer would—professional and natural. Don't use "Phase" references, changelog-style framing ("Added X", "Changed from Y"), or mention legacy/migration in doc comments. Just describe what the code does.
- Reuse existing patterns (copy OrderCatalog → EventCatalog, etc.)

### API Verification
- **Use the local native decompile** to verify Bannerlord APIs before using them.
- Decompile location: `C:\Dev\Enlisted\Decompile\`
- Key namespaces: `TaleWorlds.CampaignSystem`, `TaleWorlds.Core`, `TaleWorlds.Library`
- Don't rely on external docs or AI assumptions - verify in decompile first.

### Data Files
- **XML** for player-facing text (localization via `ModuleData/Languages/enlisted_strings.xml`)
- **JSON** for content data (events, decisions, orders in `ModuleData/Enlisted/`)
- In code, use `TextObject("{=stringId}Fallback")` for localized strings.
- **CRITICAL:** In JSON, fallback fields (`title`, `setup`, `text`, `resultText`) must immediately follow their ID fields (`titleId`, `setupId`, `textId`, `resultTextId`) for proper parser association.

### Logging
- All logs go to: `<BannerlordInstall>/Modules/Enlisted/Debugging/`
- Use: `ModLogger.Info("Category", "message")`
- Log content loading counts, migration warnings, errors.

### Build
```bash
dotnet build -c "Enlisted RETAIL" /p:Platform=x64
```
Output: `Modules/Enlisted/bin/Win64_Shipping_Client/`

---

## Overview

This document covers three advanced content features that are already defined in JSON but not yet implemented in code:

| Feature | JSON Uses | Status | Priority |
|---------|-----------|--------|----------|
| **Reward Choices** | 4 decisions | Not implemented | HIGH |
| **Flag System** | 16 references | Not implemented | HIGH |
| **Chain Events** | 2 decisions | Partial (no delay) | MEDIUM |

**Why these matter:** Without these features, 6+ decisions in `events_decisions.json` and `events_player_decisions.json` are incomplete or broken.

---

## Table of Contents

1. [Reward Choices (Sub-Choices)](#1-reward-choices-sub-choices)
2. [Flag System](#2-flag-system)
3. [Chain Events with Delay](#3-chain-events-with-delay)
4. [Implementation Order](#4-implementation-order)
5. [Testing Checklist](#5-testing-checklist)

---

## 1. Reward Choices (Sub-Choices)

### What It Does

After a player selects an option, a second popup appears letting them choose HOW to receive their reward. Examples:

- **Dice Game**: Win → Choose: Keep winnings / Buy rounds / Split difference
- **Training**: Request training → Choose: One-Handed / Two-Handed / Polearm / Bow / etc.
- **Hunt**: Accept invitation → Choose: Take full share / Decline payment / Modest share

### Current JSON Structure

```json
{
  "id": "small_stakes",
  "textId": "player_dice_small",
  "text": "\"Who's up for a friendly game? Small stakes only.\"",
  "resultTextId": "player_dice_small_outcome",
  "resultText": "The dice favor you tonight...",
  "reward_choices": {
    "type": "compensation",
    "prompt": "What do you do with your winnings?",
    "options": [
      {
        "id": "keep_winnings",
        "text": "Keep the winnings (+15 gold)",
        "tooltip": "Pocket the coin - you earned it fair and square",
        "rewards": { "gold": 15 }
      },
      {
        "id": "buy_rounds",
        "text": "Buy rounds for everyone (+4 Camp Rep)",
        "tooltip": "Spend your winnings to treat the company",
        "effects": { "camp_reputation": 4 }
      }
    ]
  }
}
```

### Data Model Changes

**File: `src/Features/Content/EventDefinition.cs`**

Add to `EventOption` class:

```csharp
/// <summary>
/// Optional sub-choices presented after the main option is selected.
/// Used for branching rewards (training type, compensation method, etc.).
/// </summary>
public RewardChoices RewardChoices { get; set; }
```

Add new classes:

```csharp
/// <summary>
/// Represents a set of sub-choices for branching rewards.
/// </summary>
public class RewardChoices
{
    /// <summary>
    /// Type of choice: "compensation", "weapon_focus", "training_type", etc.
    /// Used for analytics and potential conditional logic.
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Prompt text shown to the player (e.g., "What do you do with your winnings?").
    /// </summary>
    public string Prompt { get; set; } = string.Empty;

    /// <summary>
    /// Available sub-choice options.
    /// </summary>
    public List<RewardChoiceOption> Options { get; set; } = [];
}

/// <summary>
/// A single sub-choice option within a RewardChoices block.
/// </summary>
public class RewardChoiceOption
{
    /// <summary>
    /// Unique identifier for this sub-choice.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Display text for this option.
    /// </summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// Tooltip explaining the consequences.
    /// </summary>
    public string Tooltip { get; set; } = string.Empty;

    /// <summary>
    /// Optional condition for when this option appears (e.g., "formation:ranged").
    /// </summary>
    public string Condition { get; set; }

    /// <summary>
    /// Optional additional costs for this sub-choice.
    /// </summary>
    public EventCosts Costs { get; set; }

    /// <summary>
    /// Rewards applied when this sub-choice is selected.
    /// </summary>
    public EventRewards Rewards { get; set; }

    /// <summary>
    /// Effects applied when this sub-choice is selected.
    /// </summary>
    public EventEffects Effects { get; set; }
}

/// <summary>
/// Reward values that can be applied from an option or sub-choice.
/// </summary>
public class EventRewards
{
    public int? Gold { get; set; }
    public int? FatigueRelief { get; set; }
    public Dictionary<string, int> Xp { get; set; } = [];
    public Dictionary<string, int> SkillXp { get; set; } = [];
}

/// <summary>
/// Costs that must be paid to select an option or sub-choice.
/// </summary>
public class EventCosts
{
    public int? Gold { get; set; }
    public int? Fatigue { get; set; }
    public int? TimeHours { get; set; }
}
```

### Parsing Changes

**File: `src/Features/Content/EventCatalog.cs`**

In the option parsing method, add:

```csharp
private static RewardChoices ParseRewardChoices(JToken rewardChoicesToken)
{
    if (rewardChoicesToken == null)
        return null;

    var rc = new RewardChoices
    {
        Type = rewardChoicesToken["type"]?.Value<string>() ?? string.Empty,
        Prompt = rewardChoicesToken["prompt"]?.Value<string>() ?? string.Empty,
        Options = []
    };

    var optionsArray = rewardChoicesToken["options"] as JArray;
    if (optionsArray != null)
    {
        foreach (var optToken in optionsArray)
        {
            var subOption = new RewardChoiceOption
            {
                Id = optToken["id"]?.Value<string>() ?? string.Empty,
                Text = optToken["text"]?.Value<string>() ?? string.Empty,
                Tooltip = optToken["tooltip"]?.Value<string>() ?? string.Empty,
                Condition = optToken["condition"]?.Value<string>(),
                Rewards = ParseRewards(optToken["rewards"]),
                Effects = ParseEffects(optToken["effects"]),
                Costs = ParseCosts(optToken["costs"])
            };
            rc.Options.Add(subOption);
        }
    }

    return rc;
}
```

### Delivery Changes

**File: `src/Features/Content/EventDeliveryManager.cs`**

After showing result text, check for reward_choices:

```csharp
private void ShowResultText(EventOption option)
{
    var resultText = ResolveText(option.ResultTextId, option.ResultTextFallback);

    // ... existing result popup code ...

    // After result popup closes, check for sub-choices
    if (option.RewardChoices != null && option.RewardChoices.Options.Count > 0)
    {
        // Queue sub-choice popup to show after result popup
        _pendingSubChoice = option.RewardChoices;
    }
}

private void ShowSubChoicePopup(RewardChoices choices)
{
    var options = new List<InquiryElement>();
    
    foreach (var subOption in choices.Options)
    {
        // Check condition if present
        bool enabled = CheckSubChoiceCondition(subOption.Condition);
        
        options.Add(new InquiryElement(
            identifier: subOption,
            title: subOption.Text,
            imageIdentifier: null,
            isEnabled: enabled,
            hint: subOption.Tooltip
        ));
    }

    var inquiry = new MultiSelectionInquiryData(
        titleText: choices.Type,
        descriptionText: choices.Prompt,
        inquiryElements: options,
        isExitShown: false,
        minSelectableOptionCount: 1,
        maxSelectableOptionCount: 1,
        affirmativeText: "Confirm",
        negativeText: null,
        affirmativeAction: OnSubChoiceSelected,
        negativeAction: null
    );

    MBInformationManager.ShowMultiSelectionInquiry(inquiry, true);
}

private void OnSubChoiceSelected(List<InquiryElement> selected)
{
    var subOption = selected[0].Identifier as RewardChoiceOption;
    if (subOption == null) return;

    // Apply costs
    ApplyCosts(subOption.Costs);
    
    // Apply rewards
    ApplyRewards(subOption.Rewards);
    
    // Apply effects
    ApplyEffects(subOption.Effects);
    
    ModLogger.Info(LogCategory, $"Sub-choice selected: {subOption.Id}");
}
```

---

## 2. Flag System

### What It Does

Flags are temporary boolean states that:
- Are set when certain options are chosen
- Gate access to other decisions/events
- Can auto-expire after X days

Examples:
- `qm_owes_favor` - Set when you help the QM, enables "Call in Favor" decision
- `lance_mate_owes_favor` - Set when you lend money, triggers repayment chain
- `corruption_pattern_detected` - Set when you skim money, increases audit risk

### Current JSON Structure

```json
{
  "id": "lend_generously",
  "textId": "decision_favor_generous",
  "costs": { "gold": 100 },
  "effects": { "camp_reputation": 5 },
  "set_flags": ["lance_mate_favor_active", "lance_mate_owes_favor"],
  "flag_duration_days": 30,
  "text": "\"Take what you need, friend. We look after our own.\""
}
```

Trigger condition in another event:
```json
"triggers": {
  "all": ["is_enlisted", "flag:lance_mate_owes_favor"]
}
```

### Data Model Changes

**File: `src/Features/Content/EventDefinition.cs`**

Add to `EventOption` class:

```csharp
/// <summary>
/// Flags to set when this option is chosen.
/// </summary>
public List<string> SetFlags { get; set; } = [];

/// <summary>
/// Flags to clear when this option is chosen.
/// </summary>
public List<string> ClearFlags { get; set; } = [];

/// <summary>
/// Duration in days for flags set by this option.
/// After this time, flags auto-expire. 0 = permanent until cleared.
/// </summary>
public int FlagDurationDays { get; set; }
```

### Storage Changes

**File: `src/Features/Escalation/EscalationState.cs`**

Add new properties:

```csharp
/// <summary>
/// Active flags and their expiration times.
/// Key: flag name, Value: expiration time (CampaignTime.Never for permanent).
/// </summary>
public Dictionary<string, CampaignTime> ActiveFlags { get; set; } = 
    new Dictionary<string, CampaignTime>(StringComparer.OrdinalIgnoreCase);

/// <summary>
/// Checks if a flag is currently active (set and not expired).
/// </summary>
public bool HasFlag(string flagName)
{
    if (string.IsNullOrEmpty(flagName))
        return false;

    ActiveFlags ??= new Dictionary<string, CampaignTime>(StringComparer.OrdinalIgnoreCase);

    if (!ActiveFlags.TryGetValue(flagName, out var expiryTime))
        return false;

    // Check if expired
    if (expiryTime != CampaignTime.Never && CampaignTime.Now >= expiryTime)
    {
        ActiveFlags.Remove(flagName);
        return false;
    }

    return true;
}

/// <summary>
/// Sets a flag with optional expiration.
/// </summary>
public void SetFlag(string flagName, int durationDays = 0)
{
    if (string.IsNullOrEmpty(flagName))
        return;

    ActiveFlags ??= new Dictionary<string, CampaignTime>(StringComparer.OrdinalIgnoreCase);

    var expiryTime = durationDays > 0 
        ? CampaignTime.DaysFromNow(durationDays) 
        : CampaignTime.Never;

    ActiveFlags[flagName] = expiryTime;
}

/// <summary>
/// Clears a flag.
/// </summary>
public void ClearFlag(string flagName)
{
    if (string.IsNullOrEmpty(flagName))
        return;

    ActiveFlags ??= new Dictionary<string, CampaignTime>(StringComparer.OrdinalIgnoreCase);
    ActiveFlags.Remove(flagName);
}
```

### Parsing Changes

**File: `src/Features/Content/EventCatalog.cs`**

In option parsing:

```csharp
option.SetFlags = ParseStringList(optToken["set_flags"]);
option.ClearFlags = ParseStringList(optToken["clear_flags"]);
option.FlagDurationDays = optToken["flag_duration_days"]?.Value<int>() ?? 0;

private static List<string> ParseStringList(JToken token)
{
    if (token == null)
        return [];

    if (token is JArray array)
        return array.Select(t => t.Value<string>()).Where(s => !string.IsNullOrEmpty(s)).ToList();

    return [];
}
```

### Effect Application

**File: `src/Features/Content/EventDeliveryManager.cs`**

In `ApplyEffects` or after option selected:

```csharp
private void ApplyFlagChanges(EventOption option)
{
    var state = EscalationManager.Instance?.State;
    if (state == null) return;

    // Set flags
    if (option.SetFlags != null)
    {
        foreach (var flag in option.SetFlags)
        {
            state.SetFlag(flag, option.FlagDurationDays);
            ModLogger.Debug(LogCategory, $"Set flag: {flag} (duration: {option.FlagDurationDays} days)");
        }
    }

    // Clear flags
    if (option.ClearFlags != null)
    {
        foreach (var flag in option.ClearFlags)
        {
            state.ClearFlag(flag);
            ModLogger.Debug(LogCategory, $"Cleared flag: {flag}");
        }
    }
}
```

### Requirement Checking

**File: `src/Features/Content/EventRequirementChecker.cs`**

Add flag checking to trigger conditions:

```csharp
public static bool CheckTriggerCondition(string condition)
{
    if (string.IsNullOrEmpty(condition))
        return true;

    // Check for flag: prefix
    if (condition.StartsWith("flag:", StringComparison.OrdinalIgnoreCase))
    {
        var flagName = condition.Substring(5);
        return EscalationManager.Instance?.State?.HasFlag(flagName) ?? false;
    }

    // ... other condition checks ...

    return true;
}
```

---

## 3. Chain Events with Delay

### What It Does

After choosing certain options, a follow-up event fires automatically after a delay:
- Lend money → 7 days later → Friend repays you
- Modest loan → 2 days later → Friend shows gratitude

### Current JSON Structure

```json
{
  "id": "lend_generously",
  "chains_to": "decision_lance_mate_favor_repayment",
  "chain_delay_hours": 168
}
```

### Current Code Issue

The existing `ChainEventId` in `EventEffects` queues the chain event **immediately**. We need delayed scheduling.

### Data Model Changes

**File: `src/Features/Content/EventDefinition.cs`**

Add to `EventOption` class:

```csharp
/// <summary>
/// ID of an event to trigger after this option, with a delay.
/// </summary>
public string ChainsTo { get; set; }

/// <summary>
/// Hours to wait before triggering the chained event.
/// </summary>
public int ChainDelayHours { get; set; }
```

### Storage Changes

**File: `src/Features/Escalation/EscalationState.cs`**

Add:

```csharp
/// <summary>
/// Pending chain events scheduled for future delivery.
/// Key: event ID, Value: scheduled delivery time.
/// </summary>
public Dictionary<string, CampaignTime> PendingChainEvents { get; set; } = 
    new Dictionary<string, CampaignTime>(StringComparer.OrdinalIgnoreCase);

/// <summary>
/// Schedules a chain event for future delivery.
/// </summary>
public void ScheduleChainEvent(string eventId, int delayHours)
{
    if (string.IsNullOrEmpty(eventId))
        return;

    PendingChainEvents ??= new Dictionary<string, CampaignTime>(StringComparer.OrdinalIgnoreCase);

    var deliveryTime = CampaignTime.HoursFromNow(delayHours);
    PendingChainEvents[eventId] = deliveryTime;
}

/// <summary>
/// Gets and removes any chain events that are ready to fire.
/// </summary>
public List<string> PopReadyChainEvents()
{
    PendingChainEvents ??= new Dictionary<string, CampaignTime>(StringComparer.OrdinalIgnoreCase);

    var ready = new List<string>();
    var toRemove = new List<string>();

    foreach (var kvp in PendingChainEvents)
    {
        if (CampaignTime.Now >= kvp.Value)
        {
            ready.Add(kvp.Key);
            toRemove.Add(kvp.Key);
        }
    }

    foreach (var key in toRemove)
    {
        PendingChainEvents.Remove(key);
    }

    return ready;
}
```

### Scheduling Logic

**File: `src/Features/Content/EventDeliveryManager.cs`**

Replace immediate chain handling with scheduled:

```csharp
private void HandleChainEvent(EventOption option)
{
    // Old immediate chain (keep for backwards compatibility)
    if (!string.IsNullOrEmpty(option.Effects?.ChainEventId))
    {
        var chainEvent = EventCatalog.GetEvent(option.Effects.ChainEventId);
        if (chainEvent != null)
        {
            QueueEvent(chainEvent);
        }
    }

    // New delayed chain
    if (!string.IsNullOrEmpty(option.ChainsTo) && option.ChainDelayHours > 0)
    {
        EscalationManager.Instance?.State?.ScheduleChainEvent(
            option.ChainsTo, 
            option.ChainDelayHours);
        
        ModLogger.Info(LogCategory, 
            $"Scheduled chain event: {option.ChainsTo} in {option.ChainDelayHours} hours");
    }
}
```

### Daily Tick Check

**File: `src/Features/Content/EventPacingManager.cs`**

Add chain event checking to daily tick:

```csharp
public void OnDailyTick()
{
    // Check for pending chain events first (highest priority)
    CheckPendingChainEvents();

    // ... existing pacing logic ...
}

private void CheckPendingChainEvents()
{
    var state = EscalationManager.Instance?.State;
    if (state == null) return;

    var readyEvents = state.PopReadyChainEvents();
    
    foreach (var eventId in readyEvents)
    {
        var evt = EventCatalog.GetEvent(eventId);
        if (evt != null)
        {
            ModLogger.Info("EventPacing", $"Firing scheduled chain event: {eventId}");
            EventDeliveryManager.Instance?.QueueEvent(evt);
        }
        else
        {
            ModLogger.Warn("EventPacing", $"Scheduled chain event not found: {eventId}");
        }
    }
}
```

---

## 4. Implementation Order

### Phase 8A: Flag System (Foundation)
**Estimated Time:** 2-3 hours

1. Add `SetFlags`, `ClearFlags`, `FlagDurationDays` to `EventOption`
2. Add `ActiveFlags` dictionary and methods to `EscalationState`
3. Update `EventCatalog` to parse flag fields
4. Update `EventDeliveryManager` to apply flag changes
5. Update `EventRequirementChecker` to check `flag:` conditions
6. Test with `decision_qm_favor` (sets `qm_owes_favor`)

### Phase 8B: Chain Events with Delay
**Estimated Time:** 1-2 hours

1. Add `ChainsTo`, `ChainDelayHours` to `EventOption`
2. Add `PendingChainEvents` dictionary and methods to `EscalationState`
3. Update `EventCatalog` to parse chain fields
4. Update `EventDeliveryManager` to schedule chain events
5. Add chain event check to `EventPacingManager.OnDailyTick`
6. Test with `decision_lance_mate_favor` chain

### Phase 8C: Reward Choices (Most Complex)
**Estimated Time:** 3-4 hours

1. Add `RewardChoices`, `RewardChoiceOption`, `EventRewards`, `EventCosts` classes
2. Add `RewardChoices` property to `EventOption`
3. Update `EventCatalog` to parse `reward_choices` block
4. Update `EventDeliveryManager` to show sub-choice popup after result
5. Implement condition checking for sub-choice options
6. Apply rewards/effects from sub-choice
7. Test with `player_organize_dice_game` and `player_request_training`

---

## 5. Testing Checklist

### Flag System Tests
- [ ] Set flag when option chosen
- [ ] Flag persists across save/load
- [ ] Flag expires after duration
- [ ] Clear flag works
- [ ] Decision gated by flag appears only when flag is set
- [ ] Decision gated by flag disappears when flag expires

### Chain Event Tests
- [ ] Chain event scheduled with correct delay
- [ ] Chain event fires after delay period
- [ ] Chain events persist across save/load
- [ ] Multiple pending chain events work correctly

### Reward Choices Tests
- [ ] Sub-choice popup appears after result text
- [ ] All sub-options display correctly
- [ ] Conditional sub-options (formation:ranged) hide when condition fails
- [ ] Rewards from sub-choice applied correctly
- [ ] Effects from sub-choice applied correctly
- [ ] Costs from sub-choice deducted correctly

### Integration Tests
- [ ] Dice game flow: Choose stakes → See result → Choose what to do with winnings
- [ ] Training flow: Request training → Choose weapon focus → Get XP
- [ ] Favor flow: Lend money → 7 days later → Repayment popup
- [ ] QM favor: Help QM → Flag set → "Call in Favor" decision appears

---

## Reference: Affected JSON Files

| File | Features Used |
|------|---------------|
| `events_decisions.json` | set_flags (7), clear_flags (5), chains_to (2), reward_choices (1) |
| `events_player_decisions.json` | reward_choices (2), clear_flags (1) |
| `events_training.json` | reward_choices (1) |

---

## Reference: Files to Modify

| File | Changes |
|------|---------|
| `src/Features/Content/EventDefinition.cs` | Add RewardChoices, SetFlags, ClearFlags, ChainsTo, Costs, Rewards |
| `src/Features/Content/EventCatalog.cs` | Parse new fields from JSON |
| `src/Features/Content/EventDeliveryManager.cs` | Sub-choice popup, flag application, chain scheduling |
| `src/Features/Content/EventPacingManager.cs` | Check pending chain events on daily tick |
| `src/Features/Content/EventRequirementChecker.cs` | Check flag: conditions |
| `src/Features/Escalation/EscalationState.cs` | ActiveFlags, PendingChainEvents storage |


