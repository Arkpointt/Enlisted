using System;
using System.Collections.Generic;
using System.Linq;
using Enlisted.Features.Enlistment.Behaviors;
using Enlisted.Mod.Core.Logging;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.Core;
using TaleWorlds.Core.ImageIdentifiers;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace Enlisted.Features.Lances.Events
{
    /// <summary>
    /// Displays a secondary choice dialog after an event outcome, letting players customize their rewards.
    /// Provides player agency over which skills level up, gold vs reputation tradeoffs, etc.
    /// NOTE: This is a WIP feature. Some reward types are stubbed pending EnlistmentBehavior API additions.
    /// </summary>
    internal static class LanceLifeRewardChoiceInquiryScreen
    {
        private const string LogCategory = "LanceLifeEvents";

        /// <summary>
        /// Shows the reward choice dialog and applies the selected reward.
        /// Callback is invoked after the player makes their choice and rewards are applied.
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

            // Auto-select if only one option available
            if (availableOptions.Count == 1)
            {
                var result = ApplyRewardChoice(evt, selectedOption, availableOptions[0], enlistment);
                onComplete?.Invoke(result);
                return;
            }

            // Build inquiry data
            var title = GetPromptTitle(choices);
            var inquiryElements = BuildInquiryElements(availableOptions, enlistment);

            // Show inquiry with transparent background
            // NOTE: Using MBInformationManager (not InformationManager) for multi-selection inquiries
            MBInformationManager.ShowMultiSelectionInquiry(
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
                            var result = ApplyRewardChoice(evt, selectedOption, selected.RewardOption, enlistment);
                            onComplete?.Invoke(result);
                        }
                        else
                        {
                            onComplete?.Invoke(string.Empty);
                        }
                    },
                    negativeAction: null),
                pauseGameActiveState: false); // Keep map visible (transparent background)
        }

        /// <summary>
        /// Filter reward options based on conditions (formation, tier, gold requirements, etc.)
        /// </summary>
        private static List<LanceLifeRewardOption> FilterRewardOptions(
            List<LanceLifeRewardOption> options,
            EnlistmentBehavior enlistment)
        {
            var filtered = new List<LanceLifeRewardOption>();
            
            foreach (var opt in options)
            {
                if (opt == null)
                {
                    continue;
                }

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

        /// <summary>
        /// Evaluate a condition string like "formation:infantry", "tier >= 3", "gold >= 50"
        /// </summary>
        private static bool EvaluateCondition(string condition, EnlistmentBehavior enlistment)
        {
            if (string.IsNullOrWhiteSpace(condition))
            {
                return true;
            }

            try
            {
                // Format: "key:value" or "key >= value"
                if (condition.Contains(":"))
                {
                    var parts = condition.Split(new[] { ':' }, 2);
                    if (parts.Length == 2)
                    {
                        var key = parts[0].Trim().ToLowerInvariant();
                        var value = parts[1].Trim().ToLowerInvariant();

                        if (key == "formation")
                        {
                            // TODO: Add FormationAssignment property to EnlistmentBehavior
                            // For now, always allow formation-gated options
                            ModLogger.Debug(LogCategory, $"Formation condition '{value}' - allowing (formation check not yet implemented)");
                            return true;
                        }
                    }
                }

                // Format: "key >= value"
                if (condition.Contains(">="))
                {
                    var parts = condition.Split(new[] { ">=" }, StringSplitOptions.None);
                    if (parts.Length == 2 && int.TryParse(parts[1].Trim(), out var threshold))
                    {
                        var key = parts[0].Trim().ToLowerInvariant();
                        
                        if (key == "tier")
                        {
                            return enlistment.EnlistmentTier >= threshold;
                        }
                        
                        if (key == "gold" && Hero.MainHero != null)
                        {
                            return Hero.MainHero.Gold >= threshold;
                        }
                    }
                }

                // Format: "key > value"
                if (condition.Contains(">") && !condition.Contains(">="))
                {
                    var parts = condition.Split(new[] { '>' }, 2);
                    if (parts.Length == 2 && int.TryParse(parts[1].Trim(), out var threshold))
                    {
                        var key = parts[0].Trim().ToLowerInvariant();
                        
                        if (key == "tier")
                        {
                            return enlistment.EnlistmentTier > threshold;
                        }
                        
                        if (key == "gold" && Hero.MainHero != null)
                        {
                            return Hero.MainHero.Gold > threshold;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ModLogger.Warn(LogCategory, $"Failed to evaluate reward condition '{condition}': {ex.Message}");
            }

            // If we can't parse the condition, show the option (fail open)
            return true;
        }

        /// <summary>
        /// Get the prompt title based on reward choice type
        /// </summary>
        private static TextObject GetPromptTitle(LanceLifeRewardChoices choices)
        {
            // Use custom prompt if provided
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

        /// <summary>
        /// Build inquiry elements from reward options
        /// </summary>
        private static List<InquiryElement> BuildInquiryElements(
            List<LanceLifeRewardOption> options,
            EnlistmentBehavior enlistment)
        {
            var elements = new List<InquiryElement>();

            foreach (var opt in options)
            {
                var text = opt.Text;
                var tooltip = BuildOptionTooltip(opt);

                elements.Add(new RewardChoiceInquiryElement(
                    opt,
                    text,
                    null, // No image identifier needed
                    true,
                    tooltip));
            }

            return elements;
        }

        /// <summary>
        /// Build tooltip showing what the option provides
        /// </summary>
        private static string BuildOptionTooltip(LanceLifeRewardOption opt)
        {
            var lines = new List<string>();

            // Custom tooltip first
            if (!string.IsNullOrWhiteSpace(opt.Tooltip))
            {
                lines.Add(opt.Tooltip);
                lines.Add(""); // Blank line
            }

            // Build effects summary
            var parts = new List<string>();

            if (opt.Rewards != null)
            {
                if (opt.Rewards.Gold > 0)
                {
                    parts.Add($"+{opt.Rewards.Gold} gold");
                }

                if (opt.Rewards.FatigueRelief > 0)
                {
                    parts.Add($"+{opt.Rewards.FatigueRelief} rest");
                }

                // Enlistment XP
                if (opt.Rewards.SchemaXp != null && opt.Rewards.SchemaXp.Count > 0)
                {
                    foreach (var xp in opt.Rewards.SchemaXp)
                    {
                        parts.Add($"+{xp.Value} {xp.Key} XP");
                    }
                }

                // Skill XP
                if (opt.Rewards.SkillXp != null && opt.Rewards.SkillXp.Count > 0)
                {
                    foreach (var xp in opt.Rewards.SkillXp)
                    {
                        parts.Add($"+{xp.Value} {xp.Key}");
                    }
                }
            }

            if (opt.Effects != null)
            {
                if (opt.Effects.LanceReputation != 0)
                {
                    var sign = opt.Effects.LanceReputation > 0 ? "+" : "";
                    parts.Add($"{sign}{opt.Effects.LanceReputation} Lance Rep");
                }

                if (opt.Effects.Heat != 0)
                {
                    var sign = opt.Effects.Heat > 0 ? "+" : "";
                    parts.Add($"{sign}{opt.Effects.Heat} Heat");
                }

                if (opt.Effects.Discipline != 0)
                {
                    var sign = opt.Effects.Discipline > 0 ? "+" : "";
                    parts.Add($"{sign}{opt.Effects.Discipline} Discipline");
                }
            }

            if (parts.Count > 0)
            {
                lines.Add(string.Join(", ", parts));
            }

            // Risk warning
            if (opt.SuccessChance.HasValue && opt.SuccessChance < 1.0f)
            {
                var failChance = (int)((1.0f - opt.SuccessChance.Value) * 100);
                lines.Add($"Risk: {failChance}% chance of failure");
            }

            return lines.Count > 0 ? string.Join("\n", lines) : string.Empty;
        }

        /// <summary>
        /// Apply the selected reward choice and return result text
        /// </summary>
        private static string ApplyRewardChoice(
            LanceLifeEventDefinition evt,
            LanceLifeEventOptionDefinition selectedOption,
            LanceLifeRewardOption rewardChoice,
            EnlistmentBehavior enlistment)
        {
            ModLogger.Info(LogCategory, 
                $"Applying reward choice: event={evt.Id}, option={selectedOption.Id}, reward={rewardChoice.Id}");

            // Roll for success if risky
            bool succeeded = true;
            if (rewardChoice.SuccessChance.HasValue)
            {
                var roll = MBRandom.RandomFloat;
                succeeded = roll < rewardChoice.SuccessChance.Value;
                ModLogger.Debug(LogCategory, 
                    $"Reward choice risk roll: roll={roll:0.000}, threshold={rewardChoice.SuccessChance:0.000}, success={succeeded}");
            }

            if (succeeded)
            {
                // Apply rewards and effects
                ApplyRewards(rewardChoice.Rewards, enlistment);
                ApplyEffects(rewardChoice.Effects, enlistment);
                
                return BuildResultText(rewardChoice, true);
            }
            else
            {
                // Apply failure effects
                if (rewardChoice.Failure?.Effects != null)
                {
                    ApplyEffects(rewardChoice.Failure.Effects, enlistment);
                }
                
                return BuildResultText(rewardChoice, false);
            }
        }

        /// <summary>
        /// Apply rewards (gold, XP, fatigue relief)
        /// </summary>
        private static void ApplyRewards(LanceLifeEventRewards rewards, EnlistmentBehavior enlistment)
        {
            if (rewards == null)
            {
                return;
            }

            // Gold
            if (rewards.Gold > 0 && Hero.MainHero != null)
            {
                GiveGoldAction.ApplyBetweenCharacters(null, Hero.MainHero, rewards.Gold);
                InformationManager.DisplayMessage(new InformationMessage(
                    $"+{rewards.Gold} gold", Colors.Yellow));
            }

            // Enlistment XP
            if (rewards.SchemaXp != null && rewards.SchemaXp.Count > 0)
            {
                foreach (var xp in rewards.SchemaXp)
                {
                    if (xp.Value > 0 && enlistment != null)
                    {
                        // TODO: Add AddExperience method to EnlistmentBehavior for enlistment XP gains
                        ModLogger.Debug(LogCategory, $"Enlistment XP reward: +{xp.Value} (not yet implemented)");
                        InformationManager.DisplayMessage(new InformationMessage(
                            $"+{xp.Value} Enlistment XP", Color.FromUint(0xFF88FF88)));
                    }
                }
            }

            // Skill XP
            if (rewards.SkillXp != null && rewards.SkillXp.Count > 0 && Hero.MainHero != null)
            {
                foreach (var xp in rewards.SkillXp)
                {
                    if (xp.Value > 0)
                    {
                        var skill = GetSkillFromName(xp.Key);
                        if (skill != null)
                        {
                            Hero.MainHero.AddSkillXp(skill, xp.Value);
                            InformationManager.DisplayMessage(new InformationMessage(
                                $"+{xp.Value} {xp.Key} XP", Colors.Cyan));
                        }
                    }
                }
            }

            // Fatigue relief
            if (rewards.FatigueRelief > 0 && enlistment != null)
            {
                enlistment.TryConsumeFatigue(-rewards.FatigueRelief, "reward_choice_rest");
                InformationManager.DisplayMessage(new InformationMessage(
                    $"+{rewards.FatigueRelief} rest", Colors.Green));
            }
        }

        /// <summary>
        /// Apply effects (lance reputation, heat, discipline, etc.)
        /// NOTE: These properties are stubbed pending EnlistmentBehavior API additions.
        /// </summary>
        private static void ApplyEffects(LanceLifeEventEscalationEffects effects, EnlistmentBehavior enlistment)
        {
            if (effects == null || enlistment == null)
            {
                return;
            }

            // TODO: Add LanceReputation, Heat, Discipline properties to EnlistmentBehavior
            // For now, just log and display messages

            // Lance reputation
            if (effects.LanceReputation != 0)
            {
                var color = effects.LanceReputation > 0 ? Colors.Cyan : Colors.Red;
                var sign = effects.LanceReputation > 0 ? "+" : "";
                ModLogger.Debug(LogCategory, $"Lance reputation effect: {sign}{effects.LanceReputation} (property not yet implemented)");
                InformationManager.DisplayMessage(new InformationMessage(
                    $"{sign}{effects.LanceReputation} Lance Reputation", color));
            }

            // Heat
            if (effects.Heat != 0)
            {
                var color = effects.Heat > 0 ? Colors.Red : Colors.Green;
                var sign = effects.Heat > 0 ? "+" : "";
                ModLogger.Debug(LogCategory, $"Heat effect: {sign}{effects.Heat} (property not yet implemented)");
                InformationManager.DisplayMessage(new InformationMessage(
                    $"{sign}{effects.Heat} Heat", color));
            }

            // Discipline
            if (effects.Discipline != 0)
            {
                var color = effects.Discipline > 0 ? Colors.Red : Colors.Green;
                var sign = effects.Discipline > 0 ? "+" : "";
                ModLogger.Debug(LogCategory, $"Discipline effect: {sign}{effects.Discipline} (property not yet implemented)");
                InformationManager.DisplayMessage(new InformationMessage(
                    $"{sign}{effects.Discipline} Discipline", color));
            }
        }

        /// <summary>
        /// Build result text summarizing what the player received
        /// </summary>
        private static string BuildResultText(LanceLifeRewardOption option, bool succeeded)
        {
            if (!succeeded && option.Failure != null)
            {
                return option.Failure.Text;
            }

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

            return parts.Count > 0 ? $"You gained: {string.Join(", ", parts)}" : string.Empty;
        }

        /// <summary>
        /// Get skill object from skill name string
        /// </summary>
        private static SkillObject GetSkillFromName(string skillName)
        {
            if (string.IsNullOrWhiteSpace(skillName))
            {
                return null;
            }

            try
            {
                // Handle common variations by looking up in the game's skill registry
                var normalized = skillName.Trim().ToLowerInvariant();
                
                // Use Game.Current.ObjectManager to find skills
                if (Game.Current?.ObjectManager != null)
                {
                    foreach (var skill in Game.Current.ObjectManager.GetObjectTypeList<SkillObject>())
                    {
                        if (skill.StringId.ToLowerInvariant() == normalized ||
                            skill.Name.ToString().ToLowerInvariant() == normalized)
                        {
                            return skill;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ModLogger.Warn(LogCategory, $"Failed to get skill from name '{skillName}': {ex.Message}");
            }

            return null;
        }

        /// <summary>
        /// Custom inquiry element that holds the reward option
        /// </summary>
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
