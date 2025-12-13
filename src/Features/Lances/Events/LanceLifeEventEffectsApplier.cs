using System.Collections.Generic;
using Enlisted.Features.Conditions;
using Enlisted.Features.Enlistment.Behaviors;
using Enlisted.Features.Escalation;
using Enlisted.Mod.Core.Logging;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;

namespace Enlisted.Features.Lances.Events
{
    internal static class LanceLifeEventEffectsApplier
    {
        private const string LogCategory = "LanceLifeEvents";

        /// <summary>
        /// Applies an event option and returns the best-effort result text for UI surfaces that need it (e.g. incidents).
        /// When <paramref name="showResultMessage"/> is true, we will also display the result as an InformationMessage (legacy behavior).
        /// </summary>
        public static string ApplyAndGetResultText(
            LanceLifeEventDefinition evt,
            LanceLifeEventOptionDefinition option,
            EnlistmentBehavior enlistment,
            bool showResultMessage)
        {
            var resultText = ApplyInternal(evt, option, enlistment);
            if (showResultMessage && !string.IsNullOrWhiteSpace(resultText))
            {
                InformationManager.DisplayMessage(new InformationMessage(resultText, Colors.White));
            }

            // Show detailed effect feedback in combat log
            ShowEffectFeedback(option);

            return resultText ?? string.Empty;
        }

        /// <summary>
        /// Show detailed feedback in the combat log for effects applied by the option.
        /// </summary>
        private static void ShowEffectFeedback(LanceLifeEventOptionDefinition option)
        {
            if (option == null) return;

            // Gold rewards
            if (option.Rewards?.Gold > 0)
            {
                InformationManager.DisplayMessage(new InformationMessage(
                    $"Received {option.Rewards.Gold} gold", Colors.Yellow));
            }

            // Skill XP
            if (option.Rewards?.SkillXp != null)
            {
                foreach (var kvp in option.Rewards.SkillXp)
                {
                    if (kvp.Value > 0)
                    {
                        InformationManager.DisplayMessage(new InformationMessage(
                            $"+{kvp.Value} {kvp.Key} experience", Colors.Cyan));
                    }
                }
            }

            // Heat changes
            var totalHeat = (option.Costs?.Heat ?? 0) + (option.Effects?.Heat ?? 0);
            if (totalHeat > 0)
            {
                InformationManager.DisplayMessage(new InformationMessage(
                    $"Heat increased (+{totalHeat})", Colors.Red));
            }
            else if (totalHeat < 0)
            {
                InformationManager.DisplayMessage(new InformationMessage(
                    $"Heat decreased ({totalHeat})", Colors.Green));
            }

            // Discipline changes
            var totalDisc = (option.Costs?.Discipline ?? 0) + (option.Effects?.Discipline ?? 0);
            if (totalDisc > 0)
            {
                InformationManager.DisplayMessage(new InformationMessage(
                    $"Discipline risk increased (+{totalDisc})", new Color(1f, 0.5f, 0f)));
            }
            else if (totalDisc < 0)
            {
                InformationManager.DisplayMessage(new InformationMessage(
                    $"Discipline improved ({totalDisc})", Colors.Green));
            }

            // Lance reputation
            if (option.Effects?.LanceReputation > 0)
            {
                InformationManager.DisplayMessage(new InformationMessage(
                    $"Lance reputation improved (+{option.Effects.LanceReputation})", Colors.Cyan));
            }
            else if (option.Effects?.LanceReputation < 0)
            {
                InformationManager.DisplayMessage(new InformationMessage(
                    $"Lance reputation decreased ({option.Effects.LanceReputation})", new Color(1f, 0.5f, 0f)));
            }

            // Fatigue relief
            if (option.Rewards?.FatigueRelief > 0)
            {
                InformationManager.DisplayMessage(new InformationMessage(
                    $"Rested (+{option.Rewards.FatigueRelief} fatigue recovered)", Colors.Green));
            }

            // Gold cost
            if (option.Costs?.Gold > 0)
            {
                InformationManager.DisplayMessage(new InformationMessage(
                    $"Spent {option.Costs.Gold} gold", new Color(1f, 0.5f, 0f)));
            }
        }

        public static void Apply(LanceLifeEventDefinition evt, LanceLifeEventOptionDefinition option, EnlistmentBehavior enlistment)
        {
            _ = ApplyAndGetResultText(evt, option, enlistment, showResultMessage: true);
        }

        private static string ApplyInternal(LanceLifeEventDefinition evt, LanceLifeEventOptionDefinition option, EnlistmentBehavior enlistment)
        {
            if (evt == null || option == null)
            {
                return string.Empty;
            }

            // Phase 7: Track event completion for promotion requirements
            enlistment?.IncrementEventsCompleted();

            // If this option is risky, roll success once and reuse it for outcomes + conditional effects.
            bool? success = null;
            if (option.SuccessChance.HasValue)
            {
                var roll = MBRandom.RandomFloat;
                success = roll < MathF.Clamp(option.SuccessChance.Value, 0f, 1f);
                ModLogger.Debug(LogCategory,
                    $"Event outcome roll: event={evt.Id}, opt={option.Id}, roll={roll:0.000}, p={option.SuccessChance:0.000}, success={success}");
            }

            // Costs (fatigue / gold) are enforced here so the inquiry can disable options cheaply without duplicating logic.
            if (option.Costs == null)
            {
                option.Costs = new LanceLifeEventCosts();
            }

            if (option.Costs.Fatigue > 0)
            {
                var ok = enlistment?.TryConsumeFatigue(option.Costs.Fatigue, $"lance_life_event:{evt.Id}") != false;
                if (!ok)
                {
                    var msg = new TextObject("{=ll_evt_not_enough_fatigue}You are too exhausted.");
                    InformationManager.DisplayMessage(new InformationMessage(msg.ToString(), Colors.Red));
                    return string.Empty;
                }
            }

            if (option.Costs.Gold > 0)
            {
                if (Hero.MainHero?.Gold >= option.Costs.Gold)
                {
                    GiveGoldAction.ApplyBetweenCharacters(Hero.MainHero, null, option.Costs.Gold);
                }
                else
                {
                    InformationManager.DisplayMessage(new InformationMessage(
                        new TextObject("{=ll_not_enough_coin}You don't have enough coin.").ToString(),
                        Colors.Red));
                    return string.Empty;
                }
            }

            // Escalation “costs” are treated as deltas when escalation is enabled.
            var escalation = EscalationManager.Instance;
            if (escalation?.IsEnabled() == true)
            {
                var reason = $"lance_life_event_cost:{evt.Id}:{option.Id}";
                if (option.Costs.Heat != 0)
                {
                    escalation.ModifyHeat(option.Costs.Heat, reason);
                }
                if (option.Costs.Discipline != 0)
                {
                    escalation.ModifyDiscipline(option.Costs.Discipline, reason);
                }
            }

            // Rewards
            if (option.Rewards == null)
            {
                option.Rewards = new LanceLifeEventRewards();
            }

            if (option.Rewards.Gold > 0)
            {
                GiveGoldAction.ApplyBetweenCharacters(null, Hero.MainHero, option.Rewards.Gold);
            }

            if (option.Rewards.FatigueRelief > 0)
            {
                enlistment?.RestoreFatigue(option.Rewards.FatigueRelief, $"lance_life_event:{evt.Id}");
            }

            if (option.Rewards.SkillXp != null && option.Rewards.SkillXp.Count > 0)
            {
                foreach (var kvp in option.Rewards.SkillXp)
                {
                    var skill = ResolveSkill(kvp.Key);
                    var amount = kvp.Value;
                    if (skill != null && amount > 0)
                    {
                        Hero.MainHero?.AddSkillXp(skill, amount);
                    }
                }
            }

            // Escalation effects
            if (escalation?.IsEnabled() == true && option.Effects != null)
            {
                var reason = $"lance_life_event_effect:{evt.Id}:{option.Id}";
                if (option.Effects.Heat != 0)
                {
                    escalation.ModifyHeat(option.Effects.Heat, reason);
                }
                if (option.Effects.Discipline != 0)
                {
                    escalation.ModifyDiscipline(option.Effects.Discipline, reason);
                }
                if (option.Effects.LanceReputation != 0)
                {
                    escalation.ModifyLanceReputation(option.Effects.LanceReputation, reason);
                }
                if (option.Effects.MedicalRisk != 0)
                {
                    escalation.ModifyMedicalRisk(option.Effects.MedicalRisk, reason);
                }
            }

            // Optional success/failure effect overrides (Phase 5)
            if (escalation?.IsEnabled() == true && success.HasValue)
            {
                var chosen = success.Value ? option.EffectsSuccess : option.EffectsFailure;
                if (chosen != null)
                {
                    var reason = $"lance_life_event_effect_roll:{evt.Id}:{option.Id}:{(success.Value ? "success" : "failure")}";
                    if (chosen.Heat != 0)
                    {
                        escalation.ModifyHeat(chosen.Heat, reason);
                    }
                    if (chosen.Discipline != 0)
                    {
                        escalation.ModifyDiscipline(chosen.Discipline, reason);
                    }
                    if (chosen.LanceReputation != 0)
                    {
                        escalation.ModifyLanceReputation(chosen.LanceReputation, reason);
                    }
                    if (chosen.MedicalRisk != 0)
                    {
                        escalation.ModifyMedicalRisk(chosen.MedicalRisk, reason);
                    }
                }
            }

            // Schema effects: fatigue_relief can be expressed under effects (in addition to rewards.fatigueRelief).
            if (option.Effects?.FatigueRelief > 0)
            {
                enlistment?.RestoreFatigue(option.Effects.FatigueRelief, $"lance_life_event:{evt.Id}");
            }

            // Schema effects: tags/formation (stored as safe state; do not mutate formation training directly).
            LanceLifeEventsStateBehavior.Instance?.ApplySchemaEffectTags(option.Effects, $"event_effect:{evt.Id}:{option.Id}");

            // Phase 5: injury / illness rolls (feature-flagged).
            var conditions = PlayerConditionBehavior.Instance;
            if (conditions?.IsEnabled() == true)
            {
                TryApplyInjuryRoll(evt, option, conditions);
                TryApplyIllnessRoll(evt, option, conditions);
            }

            // Result/outcome text (best-effort).
            var resultText = ResolveOptionResultText(option, enlistment, success);
            return resultText ?? string.Empty;
        }

        private static string ResolveOptionResultText(LanceLifeEventOptionDefinition option, EnlistmentBehavior enlistment, bool? success)
        {
            if (option == null)
            {
                return string.Empty;
            }

            // Phase 5a: schema outcome_failure support (raw text) using the same success_chance roll.
            if (success.HasValue && !string.IsNullOrWhiteSpace(option.SchemaOutcomeFailure))
            {
                return success.Value
                    ? LanceLifeEventText.Resolve(option.OutcomeTextId, option.OutcomeTextFallback, string.Empty, enlistment)
                    : option.SchemaOutcomeFailure;
            }

            // If the option has a success chance, we may have distinct success/failure strings.
            if (success.HasValue &&
                !string.IsNullOrWhiteSpace(option.OutcomeSuccessTextId) &&
                !string.IsNullOrWhiteSpace(option.OutcomeFailureTextId))
            {
                return success.Value
                    ? LanceLifeEventText.Resolve(option.OutcomeSuccessTextId, string.Empty, string.Empty, enlistment)
                    : LanceLifeEventText.Resolve(option.OutcomeFailureTextId, string.Empty, string.Empty, enlistment);
            }

            return LanceLifeEventText.Resolve(option.OutcomeTextId, option.OutcomeTextFallback, string.Empty, enlistment);
        }

        private static SkillObject ResolveSkill(string skillName)
        {
            if (string.IsNullOrWhiteSpace(skillName))
            {
                return null;
            }

            try
            {
                return MBObjectManager.Instance.GetObject<SkillObject>(skillName);
            }
            catch
            {
                ModLogger.LogOnce(
                    key: $"ll_evt_unknown_skill:{skillName}",
                    category: LogCategory,
                    message: $"Unknown skill referenced by Lance Life Event content: {skillName}");
                return null;
            }
        }

        private static void TryApplyInjuryRoll(LanceLifeEventDefinition evt, LanceLifeEventOptionDefinition option, PlayerConditionBehavior conditions)
        {
            if (option?.Injury == null || conditions == null)
            {
                return;
            }

            if (option.Injury.Chance <= 0f || option.Injury.Types == null || option.Injury.Types.Count == 0)
            {
                return;
            }

            if (MBRandom.RandomFloat >= option.Injury.Chance)
            {
                return;
            }

            var type = option.Injury.Types[MBRandom.RandomInt(option.Injury.Types.Count)];
            var severity = PickInjurySeverity(option.Injury.SeverityWeights);
            var days = conditions.GetBaseRecoveryDaysForInjury(type, severity);
            conditions.TryApplyInjury(type, severity, days, $"event:{evt?.Id}:{option.Id}");
        }

        private static void TryApplyIllnessRoll(LanceLifeEventDefinition evt, LanceLifeEventOptionDefinition option, PlayerConditionBehavior conditions)
        {
            if (option?.Illness == null || conditions == null)
            {
                return;
            }

            if (option.Illness.Chance <= 0f || option.Illness.Types == null || option.Illness.Types.Count == 0)
            {
                return;
            }

            if (MBRandom.RandomFloat >= option.Illness.Chance)
            {
                return;
            }

            var type = option.Illness.Types[MBRandom.RandomInt(option.Illness.Types.Count)];
            var severity = PickIllnessSeverity(option.Illness.SeverityWeights);
            var days = conditions.GetBaseRecoveryDaysForIllness(type, severity);
            conditions.TryApplyIllness(type, severity, days, $"event:{evt?.Id}:{option.Id}");
        }

        private static InjurySeverity PickInjurySeverity(Dictionary<string, float> weights)
        {
            var wMinor = GetWeight(weights, "minor", 1f);
            var wModerate = GetWeight(weights, "moderate", 1f);
            var wSevere = GetWeight(weights, "severe", 1f);
            var wCritical = GetWeight(weights, "critical", 0.25f);

            var total = wMinor + wModerate + wSevere + wCritical;
            if (total <= 0f)
            {
                return InjurySeverity.Minor;
            }

            var r = MBRandom.RandomFloat * total;
            if (r < wMinor)
            {
                return InjurySeverity.Minor;
            }
            r -= wMinor;
            if (r < wModerate)
            {
                return InjurySeverity.Moderate;
            }
            r -= wModerate;
            if (r < wSevere)
            {
                return InjurySeverity.Severe;
            }
            return InjurySeverity.Critical;
        }

        private static IllnessSeverity PickIllnessSeverity(Dictionary<string, float> weights)
        {
            var wMild = GetWeight(weights, "mild", 1f);
            var wModerate = GetWeight(weights, "moderate", 1f);
            var wSevere = GetWeight(weights, "severe", 1f);
            var wCritical = GetWeight(weights, "critical", 0.25f);

            var total = wMild + wModerate + wSevere + wCritical;
            if (total <= 0f)
            {
                return IllnessSeverity.Mild;
            }

            var r = MBRandom.RandomFloat * total;
            if (r < wMild)
            {
                return IllnessSeverity.Mild;
            }
            r -= wMild;
            if (r < wModerate)
            {
                return IllnessSeverity.Moderate;
            }
            r -= wModerate;
            if (r < wSevere)
            {
                return IllnessSeverity.Severe;
            }
            return IllnessSeverity.Critical;
        }

        private static float GetWeight(Dictionary<string, float> weights, string key, float defaultValue)
        {
            if (weights == null || string.IsNullOrWhiteSpace(key))
            {
                return defaultValue;
            }

            return weights.TryGetValue(key, out var v) ? MathF.Max(0f, v) : defaultValue;
        }
    }
}


