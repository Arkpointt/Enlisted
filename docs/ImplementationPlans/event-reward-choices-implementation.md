# Implementation Plan: Event Reward Choices

## Scope Analysis

**Total Events**: ~200-300 events across 22 event pack files
**Events per file** (from grep count):
- events_onboarding.json: ~40 events (121 IDs ÷ 3 = ~40)
- events_general.json: ~30 events
- events_training.json: ~25 events
- events_escalation_thresholds.json: ~25 events
- events_decisions.json: ~12 events
- events_lance_simulation.json: ~12 events
- events_lance_leader_reactions.json: ~12 events
- events_promotion.json: ~7 events
- events_pay_tension.json: ~9 events
- events_pay_mutiny.json: ~8 events
- events_pay_loyal.json: ~5 events
- events_player_decisions.json: ~7 events
- events_duty_* (10 files): ~5 events each = ~50 events total

**Problem**: This is too many events to convert manually in one pass.

**Solution**: Phased rollout with backward compatibility.

---

## Implementation Strategy

### Phase 1: Core Infrastructure (Week 1)
**Goal**: Make system work for new events without breaking existing ones.

#### 1A: Extend Schema (Backward Compatible)
Add optional `reward_choices` field to `LanceLifeEventOptionDefinition`:

```csharp
// In LanceLifeEventCatalog.cs, add new classes:

public sealed class LanceLifeRewardChoices
{
    [JsonProperty("type")] 
    public string Type { get; set; } = "skill_focus"; // skill_focus | compensation | weapon_focus | risk_level | rest_focus
    
    [JsonProperty("prompt")] 
    public string Prompt { get; set; } = string.Empty;
    
    [JsonProperty("auto_select_preference")] 
    public string AutoSelectPreference { get; set; } = string.Empty; // formation | last_choice | gold_focus | xp_focus
    
    [JsonProperty("options")] 
    public List<LanceLifeRewardOption> Options { get; set; } = new List<LanceLifeRewardOption>();
}

public sealed class LanceLifeRewardOption
{
    [JsonProperty("id")] 
    public string Id { get; set; } = string.Empty;
    
    [JsonProperty("text")] 
    public string Text { get; set; } = string.Empty;
    
    [JsonProperty("textId")] 
    public string TextId { get; set; } = string.Empty;
    
    [JsonProperty("tooltip")] 
    public string Tooltip { get; set; } = string.Empty;
    
    [JsonProperty("condition")] 
    public string Condition { get; set; } = string.Empty; // formation:infantry | tier >= 3 | gold >= 50
    
    [JsonProperty("success_chance")] 
    public float? SuccessChance { get; set; }
    
    [JsonProperty("rewards")] 
    public LanceLifeEventRewards Rewards { get; set; } = new LanceLifeEventRewards();
    
    [JsonProperty("effects")] 
    public LanceLifeEventEscalationEffects Effects { get; set; } = new LanceLifeEventEscalationEffects();
    
    [JsonProperty("failure_outcome")] 
    public LanceLifeRewardFailure Failure { get; set; }
}

public sealed class LanceLifeRewardFailure
{
    [JsonProperty("text")] 
    public string Text { get; set; } = string.Empty;
    
    [JsonProperty("textId")] 
    public string TextId { get; set; } = string.Empty;
    
    [JsonProperty("effects")] 
    public LanceLifeEventEscalationEffects Effects { get; set; } = new LanceLifeEventEscalationEffects();
}

// Then add to LanceLifeEventOptionDefinition:
public sealed class LanceLifeEventOptionDefinition
{
    // ... existing fields ...
    
    [JsonProperty("reward_choices")] 
    public LanceLifeRewardChoices RewardChoices { get; set; }
}
```

**Backward Compatibility**: If `reward_choices` is null, apply rewards directly as before.

#### 1B: Create Reward Selection Dialog
Create `LanceLifeRewardChoiceInquiryScreen.cs`:

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using Enlisted.Features.Enlistment.Behaviors;
using Enlisted.Mod.Core.Logging;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace Enlisted.Features.Lances.Events
{
    /// <summary>
    /// Displays a secondary choice dialog after an event outcome, letting players customize their rewards.
    /// </summary>
    internal static class LanceLifeRewardChoiceInquiryScreen
    {
        private const string LogCategory = "LanceLifeEvents";

        /// <summary>
        /// Shows the reward choice dialog and applies the selected reward.
        /// </summary>
        public static void Show(
            LanceLifeEventDefinition evt,
            LanceLifeEventOptionDefinition selectedOption,
            EnlistmentBehavior enlistment,
            Action<string> onComplete)
        {
            if (evt == null || selectedOption?.RewardChoices == null || enlistment == null)
            {
                onComplete?.Invoke(string.Empty);
                return;
            }

            var choices = selectedOption.RewardChoices;
            
            // Filter choices by condition (formation, tier, gold, etc.)
            var availableOptions = FilterRewardOptions(choices.Options, enlistment);
            
            if (availableOptions.Count == 0)
            {
                ModLogger.Warn(LogCategory, $"No available reward choices for event={evt.Id}, option={selectedOption.Id}");
                onComplete?.Invoke(string.Empty);
                return;
            }

            // Auto-select if only one option or if auto-select is enabled
            if (availableOptions.Count == 1)
            {
                ApplyRewardChoice(evt, selectedOption, availableOptions[0], enlistment);
                onComplete?.Invoke(GetResultText(availableOptions[0]));
                return;
            }

            // Build inquiry data
            var title = GetPromptTitle(choices);
            var inquiryElements = BuildInquiryElements(availableOptions, enlistment);

            // Show inquiry
            InformationManager.ShowMultiSelectionInquiry(
                new MultiSelectionInquiryData(
                    titleText: title.ToString(),
                    descriptionText: string.Empty,
                    inquiryElements: inquiryElements,
                    isExitShown: false,
                    maxSelectableOptionCount: 1,
                    minSelectableOptionCount: 1,
                    affirmativeText: new TextObject("{=ll_reward_choice_confirm}Confirm").ToString(),
                    negativeText: null,
                    affirmativeAction: (selectedElements) =>
                    {
                        var selected = selectedElements.FirstOrDefault() as RewardChoiceInquiryElement;
                        if (selected != null)
                        {
                            ApplyRewardChoice(evt, selectedOption, selected.RewardOption, enlistment);
                            onComplete?.Invoke(GetResultText(selected.RewardOption));
                        }
                        else
                        {
                            onComplete?.Invoke(string.Empty);
                        }
                    },
                    negativeAction: null),
                true);
        }

        private static List<LanceLifeRewardOption> FilterRewardOptions(
            List<LanceLifeRewardOption> options,
            EnlistmentBehavior enlistment)
        {
            var filtered = new List<LanceLifeRewardOption>();
            
            foreach (var opt in options)
            {
                if (string.IsNullOrWhiteSpace(opt.Condition))
                {
                    filtered.Add(opt);
                    continue;
                }

                // Evaluate condition
                if (EvaluateCondition(opt.Condition, enlistment))
                {
                    filtered.Add(opt);
                }
            }

            return filtered;
        }

        private static bool EvaluateCondition(string condition, EnlistmentBehavior enlistment)
        {
            if (string.IsNullOrWhiteSpace(condition))
            {
                return true;
            }

            // Parse conditions like "formation:infantry", "tier >= 3", "gold >= 50"
            var parts = condition.Split(':');
            if (parts.Length == 2)
            {
                var key = parts[0].Trim();
                var value = parts[1].Trim();

                if (key.Equals("formation", StringComparison.OrdinalIgnoreCase))
                {
                    return enlistment.FormationAssignment.ToString().Equals(value, StringComparison.OrdinalIgnoreCase);
                }
            }

            // Handle comparison operators (tier >= 3, gold >= 50)
            if (condition.Contains(">="))
            {
                parts = condition.Split(new[] { ">=" }, StringSplitOptions.None);
                if (parts.Length == 2)
                {
                    var key = parts[0].Trim();
                    if (int.TryParse(parts[1].Trim(), out var threshold))
                    {
                        if (key.Equals("tier", StringComparison.OrdinalIgnoreCase))
                        {
                            return enlistment.EnlistmentTier >= threshold;
                        }
                        if (key.Equals("gold", StringComparison.OrdinalIgnoreCase))
                        {
                            return Hero.MainHero?.Gold >= threshold;
                        }
                    }
                }
            }

            return true; // Default to showing option if condition can't be parsed
        }

        private static TextObject GetPromptTitle(LanceLifeRewardChoices choices)
        {
            if (!string.IsNullOrWhiteSpace(choices.Prompt))
            {
                return new TextObject(choices.Prompt);
            }

            // Default prompts by type
            switch (choices.Type.ToLowerInvariant())
            {
                case "skill_focus":
                    return new TextObject("{=ll_reward_prompt_skill}How do you want to improve?");
                case "compensation":
                    return new TextObject("{=ll_reward_prompt_compensation}How do you want to benefit?");
                case "weapon_focus":
                    return new TextObject("{=ll_reward_prompt_weapon}Which weapon do you want to train?");
                case "risk_level":
                    return new TextObject("{=ll_reward_prompt_risk}How aggressive should you be?");
                case "rest_focus":
                    return new TextObject("{=ll_reward_prompt_rest}How do you want to spend your time?");
                default:
                    return new TextObject("{=ll_reward_prompt_default}Choose your reward:");
            }
        }

        private static List<InquiryElement> BuildInquiryElements(
            List<LanceLifeRewardOption> options,
            EnlistmentBehavior enlistment)
        {
            var elements = new List<InquiryElement>();

            foreach (var opt in options)
            {
                var text = opt.Text;
                var tooltip = string.IsNullOrWhiteSpace(opt.Tooltip) ? text : opt.Tooltip;

                elements.Add(new RewardChoiceInquiryElement(
                    opt,
                    text,
                    new ImageIdentifier(),
                    true,
                    tooltip));
            }

            return elements;
        }

        private static void ApplyRewardChoice(
            LanceLifeEventDefinition evt,
            LanceLifeEventOptionDefinition selectedOption,
            LanceLifeRewardOption rewardChoice,
            EnlistmentBehavior enlistment)
        {
            ModLogger.Info(LogCategory, 
                $"Applying reward choice: event={evt.Id}, option={selectedOption.Id}, reward={rewardChoice.Id}");

            // Apply rewards and effects using the existing applier
            // We create a temporary option with the selected rewards
            var tempOption = new LanceLifeEventOptionDefinition
            {
                Id = $"{selectedOption.Id}_reward_{rewardChoice.Id}",
                Rewards = rewardChoice.Rewards,
                Effects = rewardChoice.Effects,
                SuccessChance = rewardChoice.SuccessChance
            };

            LanceLifeEventEffectsApplier.Apply(evt, tempOption, enlistment);
        }

        private static string GetResultText(LanceLifeRewardOption option)
        {
            // Build a summary of what the player received
            var parts = new List<string>();

            if (option.Rewards?.Gold > 0)
            {
                parts.Add($"+{option.Rewards.Gold} gold");
            }

            if (option.Rewards?.SchemaXp != null)
            {
                foreach (var xp in option.Rewards.SchemaXp)
                {
                    parts.Add($"+{xp.Value} {xp.Key} XP");
                }
            }

            if (option.Rewards?.SkillXp != null)
            {
                foreach (var xp in option.Rewards.SkillXp)
                {
                    parts.Add($"+{xp.Value} {xp.Key}");
                }
            }

            if (option.Effects?.LanceReputation != 0)
            {
                var sign = option.Effects.LanceReputation > 0 ? "+" : "";
                parts.Add($"{sign}{option.Effects.LanceReputation} Lance Rep");
            }

            return parts.Count > 0 ? string.Join(", ", parts) : string.Empty;
        }

        private class RewardChoiceInquiryElement : InquiryElement
        {
            public LanceLifeRewardOption RewardOption { get; }

            public RewardChoiceInquiryElement(
                LanceLifeRewardOption rewardOption,
                string text,
                ImageIdentifier imageIdentifier,
                bool isEnabled,
                string hint)
                : base(rewardOption, text, imageIdentifier, isEnabled, hint)
            {
                RewardOption = rewardOption;
            }
        }
    }
}
```

#### 1C: Update Effects Applier to Check for Reward Choices
Modify `LanceLifeEventEffectsApplier.ApplyInternal()`:

```csharp
// In LanceLifeEventEffectsApplier.cs, modify ApplyInternal():

private static string ApplyInternal(
    LanceLifeEventDefinition evt, 
    LanceLifeEventOptionDefinition option, 
    EnlistmentBehavior enlistment)
{
    if (evt == null || option == null)
    {
        return string.Empty;
    }

    // NEW: Check if this option has reward choices
    if (option.RewardChoices != null && option.RewardChoices.Options.Count > 0)
    {
        // Show reward choice dialog instead of applying immediately
        string resultText = string.Empty;
        LanceLifeRewardChoiceInquiryScreen.Show(evt, option, enlistment, (text) =>
        {
            resultText = text;
        });
        
        // Return the base outcome narrative (reward details shown in dialog)
        return GetOutcomeText(option);
    }

    // EXISTING: Apply rewards directly if no reward choices
    // ... rest of existing code ...
}
```

---

### Phase 2: High-Value Event Conversions (Week 2)
**Goal**: Convert 15-20 high-impact events that players see most often.

#### Priority Targets (Ranked by Player Exposure)

**Tier S (Convert First - ~10 events)**
1. **Training events** (player-initiated, events_training.json) - 5 events
   - Shield wall drill
   - Cavalry drill  
   - Archery practice
   - Melee sparring
   - Formation training

2. **Player-initiated decisions** (events_player_decisions.json) - 3 events
   - Request training (weapon selection)
   - Organize dice game (stakes selection)
   - Ask for duty change (if applicable)

3. **Common general events** (events_general.json) - 2 events
   - Morning muster (leadership vs discipline choice)
   - Evening socializing (rest vs socializing vs study)

**Tier A (Convert Second - ~10 events)**
4. **Social decision events** (events_decisions.json) - 5 events
   - Lord's hunt invitation (gold vs reputation)
   - Dice game (stakes + gold vs reputation after win)
   - Training offer (skill focus)
   - Lance mate request (gold vs reputation)
   - Camp politics (who to support)

5. **Duty-related events** (events_duty_*.json) - 3 events
   - Scout dangerous territory (risk level)
   - Field medic emergency (skill focus)
   - Messenger urgent delivery (risk level)

6. **Pay system events** (events_pay_loyal.json) - 2 events
   - Loyal mission offer (reward preference)
   - Pay negotiation (gold vs reputation)

**Tier B (Convert Later - ~20 events)**
7. **Onboarding events** - Consider adding choices to 3-5 key onboarding moments
8. **Lance simulation events** - Add choices to dramatic moments
9. **Escalation threshold events** - Risk-level choices for high-stakes moments

---

### Phase 3: Conversion Templates & Documentation (Week 3)
**Goal**: Make it easy for you (or future modders) to convert remaining events.

#### Template Library

**Template 1: Training Event Skill Focus**
```json
{
  "id": "inf_train_shield_wall",
  "content": {
    "title": "Shield Wall Drill",
    "setup": "{SERGEANT_NAME} forms up the lance for shield wall training...",
    "options": [
      {
        "id": "train_hard",
        "text": "Push yourself to the limit",
        "outcome": "You hold the line for hours. The drill is brutal but effective.",
        "costs": {
          "fatigue": 2
        },
        "reward_choices": {
          "type": "skill_focus",
          "prompt": "How do you want to improve from this training?",
          "options": [
            {
              "id": "focus_polearm",
              "text": "Focus on polearm technique (+50 Polearm)",
              "tooltip": "Master the thrusting and bracing techniques",
              "rewards": {
                "skillXp": {
                  "Polearm": 50,
                  "Athletics": 10
                }
              }
            },
            {
              "id": "focus_shield",
              "text": "Focus on shield work (+50 One-Handed)",
              "tooltip": "Perfect your shield positioning and sword strikes",
              "rewards": {
                "skillXp": {
                  "OneHanded": 50,
                  "Athletics": 10
                }
              }
            },
            {
              "id": "balanced",
              "text": "Train everything equally (+25 Polearm, +25 One-Handed)",
              "tooltip": "Develop well-rounded skills",
              "rewards": {
                "skillXp": {
                  "Polearm": 25,
                  "OneHanded": 25,
                  "Athletics": 10
                }
              }
            }
          ]
        }
      },
      {
        "id": "take_it_easy",
        "text": "Go through the motions",
        "outcome": "You do the minimum. {SERGEANT_NAME} glares at you.",
        "rewards": {
          "skillXp": {
            "Polearm": 10
          }
        },
        "effects": {
          "discipline": 1
        }
      }
    ]
  }
}
```

**Template 2: Gold vs Reputation Tradeoff**
```json
{
  "id": "decision_lord_hunt_invitation",
  "options": [
    {
      "id": "accept",
      "text": "\"I am honored, my Lord. I shall attend.\"",
      "outcome": "You spend the day riding with {LORD_NAME}'s party. The hunt is successful.",
      "costs": {
        "fatigue": 2
      },
      "reward_choices": {
        "type": "compensation",
        "prompt": "How do you want to benefit from the hunt?",
        "options": [
          {
            "id": "take_spoils",
            "text": "Accept your share of the catch (+25 gold)",
            "tooltip": "Take the meat and pelts as payment",
            "rewards": {
              "gold": 25
            }
          },
          {
            "id": "build_favor",
            "text": "Decline payment to build goodwill (+4 Lance Rep, +2 Lord Relation)",
            "tooltip": "Show loyalty and ambition over coin",
            "effects": {
              "lance_reputation": 4
            }
          },
          {
            "id": "balanced",
            "text": "Take modest payment (+12 gold, +2 Lance Rep)",
            "tooltip": "A middle path - some gold, some goodwill",
            "rewards": {
              "gold": 12
            },
            "effects": {
              "lance_reputation": 2
            }
          }
        ]
      }
    }
  ]
}
```

**Template 3: Weapon Selection**
```json
{
  "id": "player_request_training",
  "options": [
    {
      "id": "weapon_training",
      "text": "\"Sergeant, I'd like weapon training.\"",
      "outcome": "The drillmaster agrees to work with you for the afternoon.",
      "costs": {
        "fatigue": 3
      },
      "reward_choices": {
        "type": "weapon_focus",
        "prompt": "Which weapon do you want to train?",
        "options": [
          {
            "id": "one_handed",
            "text": "One-Handed Weapons (+40 One-Handed)",
            "rewards": {
              "skillXp": { "OneHanded": 40 }
            }
          },
          {
            "id": "two_handed",
            "text": "Two-Handed Weapons (+40 Two-Handed)",
            "rewards": {
              "skillXp": { "TwoHanded": 40 }
            }
          },
          {
            "id": "polearm",
            "text": "Polearms (+40 Polearm)",
            "rewards": {
              "skillXp": { "Polearm": 40 }
            }
          },
          {
            "id": "bow",
            "text": "Archery (+40 Bow)",
            "condition": "formation:ranged",
            "rewards": {
              "skillXp": { "Bow": 40 }
            }
          },
          {
            "id": "crossbow",
            "text": "Crossbow (+40 Crossbow)",
            "condition": "formation:ranged",
            "rewards": {
              "skillXp": { "Crossbow": 40 }
            }
          }
        ]
      }
    }
  ]
}
```

**Template 4: Risk Level Selection**
```json
{
  "id": "duty_scout_dangerous_patrol",
  "options": [
    {
      "id": "accept_patrol",
      "text": "\"I'll do it, Sergeant.\"",
      "outcome": "You set out on the patrol route...",
      "reward_choices": {
        "type": "risk_level",
        "prompt": "How cautious should you be on patrol?",
        "options": [
          {
            "id": "cautious",
            "text": "Play it safe (90% success, modest rewards)",
            "tooltip": "Stick to covered approaches, avoid detection",
            "success_chance": 0.9,
            "rewards": {
              "xp": { "enlisted": 15 },
              "skillXp": { "Scouting": 20 }
            },
            "effects": {
              "lance_reputation": 1
            }
          },
          {
            "id": "balanced",
            "text": "Balanced approach (70% success, good rewards)",
            "tooltip": "Take calculated risks for better intelligence",
            "success_chance": 0.7,
            "rewards": {
              "xp": { "enlisted": 30 },
              "skillXp": { "Scouting": 35 }
            },
            "effects": {
              "lance_reputation": 2
            },
            "failure_outcome": {
              "text": "You're spotted! You take a wound escaping.",
              "effects": {
                "medical_risk": 1
              }
            }
          },
          {
            "id": "aggressive",
            "text": "Take big risks (50% success, excellent rewards)",
            "tooltip": "Get dangerously close for detailed intelligence",
            "success_chance": 0.5,
            "rewards": {
              "xp": { "enlisted": 60 },
              "skillXp": { "Scouting": 60 },
              "gold": 30
            },
            "effects": {
              "lance_reputation": 5
            },
            "failure_outcome": {
              "text": "You're caught! A fierce fight leaves you injured.",
              "effects": {
                "medical_risk": 3,
                "heat": 2
              }
            }
          }
        ]
      }
    }
  ]
}
```

---

### Phase 4: Optional Enhancements (Week 4)
**Goal**: Quality-of-life features for players who don't want micromanagement.

#### Auto-Selection System
Add to `enlisted_config.json`:

```json
{
  "event_preferences": {
    "enabled": false,
    "auto_select_rewards": false,
    "training_focus": "formation_appropriate",
    "gold_vs_reputation": "balanced",
    "risk_tolerance": "balanced"
  }
}
```

---

## Testing Strategy

### Phase 1 Testing
- [ ] Create 1 test event with reward choices
- [ ] Verify dialog shows correctly
- [ ] Verify rewards apply correctly
- [ ] Verify old events still work (no regression)
- [ ] Test with AI-unsafe moments (should queue/defer)

### Phase 2 Testing
- [ ] Convert 5 training events, verify all work
- [ ] Convert 3 social events, verify gold/rep tradeoffs
- [ ] Test formation-specific conditions work
- [ ] Playtest for 2 hours, check player feedback

### Phase 3 Testing
- [ ] Have someone else convert 2 events using templates
- [ ] Verify templates are clear enough
- [ ] Check that conversion doesn't break existing events

---

## Rollout Plan

### Week 1: Core System
1. Extend schema (Monday)
2. Create reward choice dialog (Tuesday-Wednesday)
3. Update effects applier (Thursday)
4. Test with 1-2 pilot events (Friday)

### Week 2: High-Value Conversions
1. Convert 5 training events (Monday-Tuesday)
2. Convert 5 social decision events (Wednesday-Thursday)
3. Playtest and balance (Friday)

### Week 3: Templates & Documentation
1. Create conversion templates (Monday-Tuesday)
2. Document conversion process (Wednesday)
3. Convert 5-10 more events using templates (Thursday-Friday)

### Week 4: Polish & Optional Features
1. Balance pass on all converted events (Monday)
2. Implement auto-selection (if desired) (Tuesday-Wednesday)
3. Final playtest and iteration (Thursday-Friday)

---

## Success Metrics

### Coverage
- [ ] 20+ events with reward choices in first month
- [ ] 50+ events with reward choices in 3 months
- [ ] All training events have skill focus choices
- [ ] All social events have gold/rep tradeoffs

### Quality
- [ ] No single reward choice dominates (>60% pickrate)
- [ ] Players report feeling they have meaningful choices
- [ ] Build diversity increases (different formations choose differently)

### Technical
- [ ] No crashes or bugs from reward choice system
- [ ] Backward compatibility maintained (old events work)
- [ ] Performance impact negligible (<5ms per dialog)

---

## Migration Path for Existing Events

**Rule**: Old events work as-is. No breaking changes.

**Optional Upgrade**: Add `reward_choices` to any event option:
1. Copy the old `rewards` and `effects` as one reward option
2. Add 1-2 alternative reward options
3. Test that all options feel balanced

**Example Migration**:
```json
// OLD (still works):
{
  "id": "some_option",
  "text": "Do the thing",
  "rewards": {
    "skillXp": { "OneHanded": 30 }
  }
}

// NEW (adds choices):
{
  "id": "some_option",
  "text": "Do the thing",
  "outcome": "You do the thing successfully.",
  "reward_choices": {
    "type": "skill_focus",
    "options": [
      {
        "id": "one_handed",
        "text": "Focus on one-handed (+30 One-Handed)",
        "rewards": { "skillXp": { "OneHanded": 30 } }
      },
      {
        "id": "two_handed",
        "text": "Focus on two-handed (+30 Two-Handed)",
        "rewards": { "skillXp": { "TwoHanded": 30 } }
      }
    ]
  }
}
```

---

## Long-Term Vision

**6 Months**: 80%+ of events have meaningful reward choices
**12 Months**: Full auto-selection system with learned preferences
**Future**: Event chains that remember player choices and reference them

This creates a living world that responds to player agency and playstyle.

