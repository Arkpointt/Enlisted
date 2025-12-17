using System;
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
            if (option == null)
            {
                return;
            }

            // Gold rewards
            if (option.Rewards?.Gold > 0)
            {
                InformationManager.DisplayMessage(new InformationMessage(
                    $"Received {option.Rewards.Gold} gold", Colors.Yellow));
            }

            // XP rewards (both SchemaXp and SkillXp, distinguishing enlistment from skill XP)
            ShowXpFeedback(option.Rewards?.SchemaXp);
            ShowXpFeedback(option.Rewards?.SkillXp);

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

        /// <summary>
        /// Shows feedback for XP rewards, distinguishing between enlistment XP and skill XP.
        /// </summary>
        private static void ShowXpFeedback(Dictionary<string, int> xpDict)
        {
            if (xpDict == null || xpDict.Count == 0)
            {
                return;
            }

            foreach (var kvp in xpDict)
            {
                if (kvp.Value <= 0 || string.IsNullOrWhiteSpace(kvp.Key))
                {
                    continue;
                }

                var key = kvp.Key.Trim();

                // Check if this is enlistment XP (show differently from skill XP)
                if (string.Equals(key, "enlisted", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(key, "enlistment", StringComparison.OrdinalIgnoreCase))
                {
                    InformationManager.DisplayMessage(new InformationMessage(
                        $"+{kvp.Value} Enlistment experience", Color.FromUint(0xFF88FF88))); // Light green
                }
                else
                {
                    InformationManager.DisplayMessage(new InformationMessage(
                        $"+{kvp.Value} {key} experience", Colors.Cyan));
                }
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

            // Process XP rewards: both enlistment XP and Bannerlord skill XP
            // SchemaXp from JSON is merged into SkillXp by the catalog loader, so we check both
            ApplyXpRewards(evt, option, enlistment);

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

            // Phase 3 (Decision Events): Handle chain events and story flags
            // This is done here in the applier to ensure consistent behavior regardless of which
            // presentation path (VM, Inquiry, Incident) fires the event.
            ApplyDecisionEventEffects(evt, option);

            // Phase 4/5: Onboarding stage advancement (data-driven via option flag).
            // This runs on option selection so "close without choosing" cannot accidentally advance stages.
            if (string.Equals(evt.Category, "onboarding", StringComparison.OrdinalIgnoreCase) &&
                option.AdvancesOnboarding)
            {
                LanceLifeOnboardingBehavior.Instance?.AdvanceStage($"option:{evt.Id}:{option.Id}");
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

        /// <summary>
        /// Applies Phase 3 Decision Event effects: chain events and story flags.
        /// Moved here from LanceLifeEventVM to ensure consistent handling across all presentation paths.
        /// </summary>
        private static void ApplyDecisionEventEffects(LanceLifeEventDefinition evt, LanceLifeEventOptionDefinition option)
        {
            if (option == null)
            {
                return;
            }

            try
            {
                var decisionBehavior = Decisions.DecisionEventBehavior.Instance;
                if (decisionBehavior == null)
                {
                    // Decision events system not active - skip flag/chain handling
                    return;
                }

                // Handle chain events: queue the next event in the chain
                if (!string.IsNullOrWhiteSpace(option.ChainsTo))
                {
                    // Use specified delay or default to random 12-36 hours
                    var delayHours = option.ChainDelayHours > 0 
                        ? option.ChainDelayHours 
                        : MBRandom.RandomFloatRanged(12f, 36f);

                    decisionBehavior.QueueChainEvent(option.ChainsTo, delayHours);

                    ModLogger.Info(LogCategory, 
                        $"Chain event queued: {option.ChainsTo} (delay: {delayHours:F1}h) from {evt?.Id}");
                }

                // Handle story flags to set
                if (option.SetFlags != null && option.SetFlags.Count > 0)
                {
                    foreach (var flag in option.SetFlags)
                    {
                        if (!string.IsNullOrWhiteSpace(flag))
                        {
                            decisionBehavior.SetFlag(flag, option.FlagDurationDays);
                            ModLogger.Debug(LogCategory, $"Flag set: {flag} (expires in {option.FlagDurationDays} days)");
                        }
                    }
                }

                // Handle story flags to clear
                if (option.ClearFlags != null && option.ClearFlags.Count > 0)
                {
                    foreach (var flag in option.ClearFlags)
                    {
                        if (!string.IsNullOrWhiteSpace(flag))
                        {
                            decisionBehavior.ClearFlag(flag);
                            ModLogger.Debug(LogCategory, $"Flag cleared: {flag}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ModLogger.Warn(LogCategory, 
                    $"Failed to apply decision event effects for {evt?.Id}: {ex.Message}");
            }
        }

        /// <summary>
        /// Applies XP rewards from the event option, handling both:
        /// - "enlisted" key: Awards mod enlistment XP (for progression/promotions)
        /// - All other keys: Treated as Bannerlord skill names (Athletics, OneHanded, etc.)
        /// 
        /// This fixes the bug where "enlisted" XP was silently dropped because ResolveSkill
        /// returned null for non-existent Bannerlord skills.
        /// </summary>
        private static void ApplyXpRewards(
            LanceLifeEventDefinition evt,
            LanceLifeEventOptionDefinition option,
            EnlistmentBehavior enlistment)
        {
            if (option?.Rewards == null)
            {
                return;
            }

            // Combine both SchemaXp (from rewards.xp in JSON) and SkillXp (from rewards.skillXp)
            // The catalog loader normalizes SchemaXp into SkillXp, but we handle both for safety
            var allXp = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            if (option.Rewards.SchemaXp != null)
            {
                foreach (var kvp in option.Rewards.SchemaXp)
                {
                    if (!string.IsNullOrWhiteSpace(kvp.Key) && kvp.Value > 0)
                    {
                        allXp[kvp.Key] = kvp.Value;
                    }
                }
            }

            if (option.Rewards.SkillXp != null)
            {
                foreach (var kvp in option.Rewards.SkillXp)
                {
                    if (!string.IsNullOrWhiteSpace(kvp.Key) && kvp.Value > 0)
                    {
                        // SkillXp takes precedence if both are present (unlikely but handle it)
                        allXp[kvp.Key] = kvp.Value;
                    }
                }
            }

            if (allXp.Count == 0)
            {
                return;
            }

            var eventSource = $"Event:{evt?.Id ?? "unknown"}";

            foreach (var kvp in allXp)
            {
                var key = kvp.Key.Trim();
                var amount = kvp.Value;

                if (amount <= 0)
                {
                    continue;
                }

                // Check if this is enlistment XP (the mod's internal progression system)
                if (string.Equals(key, "enlisted", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(key, "enlistment", StringComparison.OrdinalIgnoreCase))
                {
                    if (enlistment != null)
                    {
                        enlistment.AddEnlistmentXP(amount, eventSource);
                        ModLogger.Info(LogCategory, $"Awarded {amount} enlistment XP from {eventSource}");
                    }
                    else
                    {
                        ModLogger.Warn(LogCategory, $"Cannot award enlistment XP - EnlistmentBehavior not available");
                    }
                }
                else
                {
                    // Treat as Bannerlord skill XP
                    var skill = ResolveSkill(key);
                    if (skill != null)
                    {
                        Hero.MainHero?.AddSkillXp(skill, amount);
                        ModLogger.Debug(LogCategory, $"Awarded {amount} {skill.Name} XP from {eventSource}");
                    }
                    else
                    {
                        ModLogger.Warn(LogCategory, 
                            $"Unknown XP key '{key}' in event {evt?.Id} - not 'enlisted' and not a valid Bannerlord skill");
                    }
                }
            }
        }

        private static SkillObject ResolveSkill(string skillName)
        {
            if (string.IsNullOrWhiteSpace(skillName))
            {
                return null;
            }

            // Normalize skill name to Bannerlord's PascalCase SkillObject IDs
            var normalizedName = NormalizeSkillName(skillName);

            try
            {
                return MBObjectManager.Instance.GetObject<SkillObject>(normalizedName);
            }
            catch
            {
                ModLogger.LogOnce(
                    key: $"ll_evt_unknown_skill:{skillName}",
                    category: LogCategory,
                    message: $"Unknown skill referenced by Lance Life Event content: {skillName} (normalized: {normalizedName})");
                return null;
            }
        }

        /// <summary>
        /// Normalizes skill names from various formats (snake_case, lowercase, PascalCase)
        /// to Bannerlord's expected SkillObject string IDs (PascalCase).
        /// </summary>
        private static string NormalizeSkillName(string raw)
        {
            var s = (raw ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(s))
            {
                return string.Empty;
            }

            // Map common formats to Bannerlord skill IDs
            return s.ToLowerInvariant() switch
            {
                // Combat skills
                "one_handed" or "onehanded" => "OneHanded",
                "two_handed" or "twohanded" => "TwoHanded",
                "polearm" => "Polearm",
                "throwing" => "Throwing",
                "bow" => "Bow",
                "crossbow" => "Crossbow",
                
                // Movement skills
                "riding" => "Riding",
                "athletics" => "Athletics",
                
                // Cunning skills
                "scouting" => "Scouting",
                "tactics" => "Tactics",
                "roguery" => "Roguery",
                
                // Social skills
                "charm" => "Charm",
                "leadership" => "Leadership",
                "trade" => "Trade",
                
                // Intelligence skills
                "steward" => "Steward",
                "medicine" => "Medicine",
                "engineering" => "Engineering",
                "smithing" or "crafting" => "Smithing",
                
                // Passthrough for PascalCase or already correct IDs
                _ => s
            };
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


